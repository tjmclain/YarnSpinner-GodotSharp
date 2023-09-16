using System.Collections.Generic;
using System.Linq;
using Yarn.Compiler;
using System.IO;
using System;
using Godot;
using Godot.Collections;

namespace Yarn.GodotEngine.Editor
{
	public partial class YarnProjectImporter : EditorImportPlugin
	{
		[Serializable]
		public partial class SerializedDeclaration : RefCounted
		{
			internal static List<IType> BuiltInTypesList = new List<IType> {
				BuiltinTypes.String,
				BuiltinTypes.Boolean,
				BuiltinTypes.Number,
			};

			[Export]
			public string name = "$variable";

			[Export]
			public string typeName = BuiltinTypes.String.Name;

			[Export]
			public bool defaultValueBool;
			[Export]
			public float defaultValueNumber;

			[Export]
			public string defaultValueString;
			[Export]
			public string description;

			[Export]
			public bool isImplicit;

			public SerializedDeclaration(Declaration decl)
			{
				name = decl.Name;
				typeName = decl.Type.Name;
				description = decl.Description;
				isImplicit = decl.IsImplicit;

				if (typeName == BuiltinTypes.String.Name)
				{
					defaultValueString = Convert.ToString(decl.DefaultValue);
				}
				else if (typeName == BuiltinTypes.Boolean.Name)
				{
					defaultValueBool = Convert.ToBoolean(decl.DefaultValue);
				}
				else if (typeName == BuiltinTypes.Number.Name)
				{
					defaultValueNumber = Convert.ToSingle(decl.DefaultValue);
				}
				else
				{
					throw new InvalidOperationException($"Invalid declaration type {decl.Type.Name}");
				}
			}
		}

		//public Project GetProject()
		//{
		//	try
		//	{
		//		return Project.LoadFromFile(assetPath);
		//	}
		//	catch
		//	{
		//		return null;
		//	}
		//}

		[Export]
		public ProjectImportData importData;

		public ProjectImportData ImportData => importData;

		//public bool GetProjectReferencesYarnFile(YarnImporter yarnImporter)
		//{
		//	try
		//	{
		//		var project = Project.LoadFromFile(assetPath);
		//		var scriptFile = yarnImporter.assetPath;

		//		var projectRelativeSourceFiles = project.SourceFiles.Select(GetRelativePath);

		//		return projectRelativeSourceFiles.Contains(scriptFile);
		//	}
		//	catch
		//	{
		//		return false;
		//	}
		//}

		public override string _GetImporterName()
		{
			return "Yarn.Godot.YarnProject";
		}

		public override string _GetVisibleName()
		{
			return "Yarn Project";
		}

		public override Error _Import(
			string sourceFile,
			string savePath,
			Dictionary options,
			Array<string> platformVariants,
			Array<string> genFiles
		)
		{
			GD.Print("Import project " + sourceFile);

			YarnProject projectAsset = null;

			try
			{
				projectAsset = ResourceLoader.Load<YarnProject>(sourceFile);
			}
			catch (Exception ex)
			{
				GD.PushError(ex);
				return Error.InvalidData;
			}

			// Start by creating the asset - no matter what, we need to
			// produce an asset, even if it doesn't contain valid Yarn
			// bytecode, so that other assets don't lose their references.

			importData = new ProjectImportData();

			// Attempt to load the JSON project file.
			Project project = null;
			try
			{
				string sourceFileSystemPath = ProjectSettings.GlobalizePath(sourceFile);
				project = Project.LoadFromFile(sourceFileSystemPath);
			}
			catch (Exception ex)
			{
				GD.PushError(ex);
				return Error.InvalidData;
			}

			importData.sourceFilePaths.AddRange(project.SourceFilePatterns);

			importData.baseLanguageName = project.BaseLanguage;

			foreach (var loc in project.Localisation)
			{
				var locInfo = new ProjectImportData.LocalizationEntry
				{
					languageID = loc.Key,
					stringsFile = "", // TODO
					assetsFolder = "", // TODO
				};
				importData.localizations.Add(locInfo);
			}

			if (project.Localisation.ContainsKey(project.BaseLanguage) == false)
			{
				importData.localizations.Add(new ProjectImportData.LocalizationEntry
				{
					languageID = project.BaseLanguage,
				});
			}

			CompilationResult compilationResult;

			if (project.SourceFiles.Any())
			{
				// This project depends upon this script
				foreach (var scriptPath in project.SourceFiles)
				{
					importData.yarnFiles.Add(scriptPath);
				}

				var library = Actions.GetLibrary();

				// Now to compile the scripts associated with this project.
				var job = CompilationJob.CreateFromFiles(project.SourceFiles);

				job.Library = library;

				compilationResult = Compiler.Compiler.Compile(job);

				var errors = compilationResult.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

				if (errors.Count() > 0)
				{
					var errorGroups = errors.GroupBy(e => e.FileName);
					foreach (var errorGroup in errorGroups)
					{
						var errorMessages = errorGroup.Select(e => e.ToString());

						var relativePath = ProjectSettings.LocalizePath(errorGroup.Key);

						foreach (var error in errorGroup)
						{
							var relativeErrorFileName = ProjectSettings.LocalizePath(error.FileName);
							GD.PushError($"Error compiling <a href=\"{relativeErrorFileName}\">{relativeErrorFileName}</a> line {error.Range.Start.Line + 1}: {error.Message}");
						}

						importData.diagnostics.Add(new ProjectImportData.DiagnosticEntry
						{
							yarnFile = relativePath,
							errorMessages = new(errorMessages),
						});
					}
					importData.ImportStatus = ProjectImportData.ImportStatusCode.CompilationFailed;
					return Error.InvalidData;
				}

				if (compilationResult.Program == null)
				{
					GD.PushError("Internal error: Failed to compile: resulting program was null, but compiler did not report errors.");
					return Error.InvalidData;
				}

				importData.containsImplicitLineIDs = compilationResult.ContainsImplicitStringTags;

				// Store _all_ declarations - both the ones in this .yarnproject
				// file, and the ones inside the .yarn files.

				// While we're here, filter out any declarations that begin with
				// our Yarn internal prefix. These are synthesized variables
				// that are generated as a result of the compilation, and are
				// not declared by the user.
				var declarations = compilationResult.Declarations
					.Where(decl => !decl.Name.StartsWith("$Yarn.Internal."))
					.Where(decl => !(decl.Type is FunctionType))
					.Select(decl => new SerializedDeclaration(decl)).ToList();

				importData.serializedDeclarations.Clear();
				importData.serializedDeclarations.AddRange(declarations);

				// MYNA TODO: handle localization
				// CreateYarnInternalLocalizationAssets(ctx, projectAsset, compilationResult, importData);
				projectAsset.localizationType = LocalizationType.YarnInternal;

				// Store the compiled program
				byte[] compiledBytes = null;

				using (var memoryStream = new MemoryStream())
				using (var outputStream = new Google.Protobuf.CodedOutputStream(memoryStream))
				{
					// Serialize the compiled program to memory
					compilationResult.Program.WriteTo(outputStream);
					outputStream.Flush();

					compiledBytes = memoryStream.ToArray();
				}

				projectAsset.compiledYarnProgram = compiledBytes;
			}

			importData.ImportStatus = ProjectImportData.ImportStatusCode.Succeeded;
			string filename = savePath + "." + _GetSaveExtension();
			return ResourceSaver.Save(projectAsset, filename);
		}

		//private void CreateYarnInternalLocalizationAssets(AssetImportContext ctx, YarnProject projectAsset, CompilationResult compilationResult, ProjectImportData importData)
		//{
		//	// Will we need to create a default localization? This variable
		//	// will be set to false if any of the languages we've
		//	// configured in languagesToSourceAssets is the default
		//	// language.
		//	var shouldAddDefaultLocalization = true;

		//	foreach (var localisationInfo in importData.localizations)
		//	{
		//		// Don't create a localization if the language ID was not
		//		// provided
		//		if (string.IsNullOrEmpty(localisationInfo.languageID))
		//		{
		//			GD.PushWarning($"Not creating a localization for {projectAsset.name} because the language ID wasn't provided.");
		//			continue;
		//		}

		//		IEnumerable<StringTableEntry> stringTable;

		//		// Where do we get our strings from? If it's the default
		//		// language, we'll pull it from the scripts. If it's from
		//		// any other source, we'll pull it from the CSVs.
		//		if (localisationInfo.languageID == importData.baseLanguageName)
		//		{
		//			// No strings file needed - we'll use the program-supplied string table.
		//			stringTable = GenerateStringsTable();

		//			// We don't need to add a default localization.
		//			shouldAddDefaultLocalization = false;
		//		}
		//		else
		//		{
		//			// No strings file provided
		//			if (localisationInfo.stringsFile == null)
		//			{
		//				GD.PushWarning($"Not creating a localisation for {localisationInfo.languageID} in the Yarn project {projectAsset.name} because a strings file was not specified, and {localisationInfo.languageID} is not the project's base language");
		//				continue;
		//			}
		//			try
		//			{
		//				stringTable = StringTableEntry.ParseFromCSV(localisationInfo.stringsFile.text);
		//			}
		//			catch (ArgumentException e)
		//			{
		//				GD.PushWarning($"Not creating a localization for {localisationInfo.languageID} in the Yarn Project {projectAsset.name} because an error was encountered during text parsing: {e}");
		//				continue;
		//			}
		//		}

		//		var newLocalization = ScriptableObject.CreateInstance<Localization>();
		//		newLocalization.LocaleCode = localisationInfo.languageID;

		//		// Add these new lines to the localisation's asset
		//		foreach (var entry in stringTable)
		//		{
		//			newLocalization.AddLocalisedStringToAsset(entry.ID, entry.Text);
		//		}

		//		projectAsset.localizations.Add(newLocalization);
		//		newLocalization.name = localisationInfo.languageID;

		//		if (localisationInfo.assetsFolder != null)
		//		{
		//			newLocalization.ContainsLocalizedAssets = true;

		//			// Get the line IDs.
		//			IEnumerable<string> lineIDs = stringTable.Select(s => s.ID);

		//			// Map each line ID to its asset path.
		//			var stringIDsToAssetPaths = YarnProjectUtility.FindAssetPathsForLineIDs(lineIDs, AssetDatabase.GetAssetPath(localisationInfo.assetsFolder));

		//			// Load the asset, so we can assign the reference.
		//			var assetPaths = stringIDsToAssetPaths
		//				.Select(a => new KeyValuePair<string, Object>(a.Key, AssetDatabase.LoadAssetAtPath<Object>(a.Value)));

		//			newLocalization.AddLocalizedObjects(assetPaths);
		//		}

		//		ctx.AddObjectToAsset("localization-" + localisationInfo.languageID, newLocalization);

		//		if (localisationInfo.languageID == importData.baseLanguageName)
		//		{
		//			// If this is our default language, set it as such
		//			projectAsset.baseLocalization = newLocalization;

		//			// Since this is the default language, also populate the line metadata.
		//			projectAsset.lineMetadata = new LineMetadata(LineMetadataTableEntriesFromCompilationResult(compilationResult));
		//		}
		//		else if (localisationInfo.stringsFile != null)
		//		{
		//			// This localization depends upon a source asset. Make
		//			// this asset get re-imported if this source asset was
		//			// modified
		//			ctx.DependsOnSourceAsset(AssetDatabase.GetAssetPath(localisationInfo.stringsFile));
		//		}
		//	}

		//	if (shouldAddDefaultLocalization)
		//	{
		//		// We didn't add a localization for the default language.
		//		// Create one for it now.
		//		var stringTableEntries = GetStringTableEntries(compilationResult);

		//		var developmentLocalization = ScriptableObject.CreateInstance<Localization>();
		//		developmentLocalization.name = $"Default ({importData.baseLanguageName})";
		//		developmentLocalization.LocaleCode = importData.baseLanguageName;

		//		// Add these new lines to the development localisation's asset
		//		foreach (var entry in stringTableEntries)
		//		{
		//			developmentLocalization.AddLocalisedStringToAsset(entry.ID, entry.Text);
		//		}

		//		projectAsset.baseLocalization = developmentLocalization;
		//		projectAsset.localizations.Add(projectAsset.baseLocalization);
		//		ctx.AddObjectToAsset("default-language", developmentLocalization);

		//		// Since this is the default language, also populate the line metadata.
		//		projectAsset.lineMetadata = new LineMetadata(LineMetadataTableEntriesFromCompilationResult(compilationResult));
		//	}
		//}

		/// <summary>
		/// Gets a value indicating whether this Yarn Project contains any
		/// compile errors.
		/// </summary>
		//internal bool HasErrors
		//{
		//	get
		//	{
		//		var importData = AssetDatabase.LoadAssetAtPath<ProjectImportData>(assetPath);

		//		if (importData == null)
		//		{
		//			// If we have no import data, then a problem has occurred
		//			// when importing this project, so indicate 'true' as
		//			// signal.
		//			return true;
		//		}
		//		return importData.HasCompileErrors;
		//	}
		//}

		/// <summary>
		/// Gets a value indicating whether this Yarn Project is able to
		/// generate a strings table - that is, it has no compile errors,
		/// it has at least one script, and all scripts are fully tagged.
		/// </summary>
		/// <inheritdoc path="exception"
		/// cref="GetScriptHasLineTags(TextAsset)"/>
		//internal bool CanGenerateStringsTable
		//{
		//	get
		//	{
		//		var importData = AssetDatabase.LoadAssetAtPath<ProjectImportData>(assetPath);

		//		if (importData == null)
		//		{
		//			return false;
		//		}

		//		return importData.HasCompileErrors == false && importData.containsImplicitLineIDs == false;
		//	}
		//}

		//private CompilationResult? CompileStringsOnly()
		//{
		//	var paths = GetProject().SourceFiles;

		//	var job = CompilationJob.CreateFromFiles(paths);
		//	job.CompilationType = CompilationJob.Type.StringsOnly;

		//	return Compiler.Compiler.Compile(job);
		//}

		//internal IEnumerable<string> GetErrorsForScript(TextAsset sourceScript)
		//{
		//	if (ImportData == null)
		//	{
		//		return Enumerable.Empty<string>();
		//	}
		//	foreach (var errorCollection in ImportData.diagnostics)
		//	{
		//		if (errorCollection.yarnFile == sourceScript)
		//		{
		//			return errorCollection.errorMessages;
		//		}
		//	}
		//	return Enumerable.Empty<string>();
		//}

		/// <summary>
		/// Generates a collection of <see cref="StringTableEntry"/>
		/// objects, one for each line in this Yarn Project's scripts.
		/// </summary>
		/// <returns>An IEnumerable containing a <see
		/// cref="StringTableEntry"/> for each of the lines in the Yarn
		/// Project, or <see langword="null"/> if the Yarn Project contains
		/// errors.</returns>
		//internal IEnumerable<StringTableEntry> GenerateStringsTable()
		//{
		//	CompilationResult? compilationResult = CompileStringsOnly();

		//	if (!compilationResult.HasValue)
		//	{
		//		// We only get no value if we have no scripts to work with.
		//		// In this case, return an empty collection - there's no
		//		// error, but there's no content either.
		//		return new List<StringTableEntry>();
		//	}

		//	var errors = compilationResult.Value.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

		//	if (errors.Count() > 0)
		//	{
		//		GD.PushError($"Can't generate a strings table from a Yarn Project that contains compile errors", null);
		//		return null;
		//	}

		//	return GetStringTableEntries(compilationResult.Value);
		//}

		//internal IEnumerable<LineMetadataTableEntry> GenerateLineMetadataEntries()
		//{
		//	CompilationResult? compilationResult = CompileStringsOnly();

		//	if (!compilationResult.HasValue)
		//	{
		//		// We only get no value if we have no scripts to work with.
		//		// In this case, return an empty collection - there's no
		//		// error, but there's no content either.
		//		return new List<LineMetadataTableEntry>();
		//	}

		//	var errors = compilationResult.Value.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

		//	if (errors.Count() > 0)
		//	{
		//		GD.PushError($"Can't generate line metadata entries from a Yarn Project that contains compile errors", null);
		//		return null;
		//	}

		//	return LineMetadataTableEntriesFromCompilationResult(compilationResult.Value);
		//}

		//private IEnumerable<StringTableEntry> GetStringTableEntries(CompilationResult result)
		//{
		//	return result.StringTable.Select(x => new StringTableEntry
		//	{
		//		ID = x.Key,
		//		Language = GetProject().BaseLanguage,
		//		Text = x.Value.text,
		//		File = x.Value.fileName,
		//		Node = x.Value.nodeName,
		//		LineNumber = x.Value.lineNumber.ToString(),
		//		Lock = YarnImporter.GetHashString(x.Value.text, 8),
		//		Comment = GenerateCommentWithLineMetadata(x.Value.metadata),
		//	});
		//}

		private IEnumerable<LineMetadataTableEntry> LineMetadataTableEntriesFromCompilationResult(CompilationResult result)
		{
			return result.StringTable.Select(x => new LineMetadataTableEntry
			{
				ID = x.Key,
				File = x.Value.fileName,
				Node = x.Value.nodeName,
				LineNumber = x.Value.lineNumber.ToString(),
				Metadata = RemoveLineIDFromMetadata(x.Value.metadata).ToArray(),
			}).Where(x => x.Metadata.Length > 0);
		}

		/// <summary>
		/// Generates a string with the line metadata. This string is intended
		/// to be used in the "comment" column of a strings table CSV. Because
		/// of this, it will ignore the line ID if it exists (which is also
		/// part of the line metadata).
		/// </summary>
		/// <param name="metadata">The metadata from a given line.</param>
		/// <returns>A string prefixed with "Line metadata: ", followed by each
		/// piece of metadata separated by whitespace. If no metadata exists or
		/// only the line ID is part of the metadata, returns an empty string
		/// instead.</returns>
		private string GenerateCommentWithLineMetadata(string[] metadata)
		{
			var cleanedMetadata = RemoveLineIDFromMetadata(metadata);

			if (cleanedMetadata.Count() == 0)
			{
				return string.Empty;
			}

			return $"Line metadata: {string.Join(" ", cleanedMetadata)}";
		}

		/// <summary>
		/// Removes any line ID entry from an array of line metadata.
		/// Line metadata will always contain a line ID entry if it's set. For
		/// example, if a line contains "#line:1eaf1e55", its line metadata
		/// will always have an entry with "line:1eaf1e55".
		/// </summary>
		/// <param name="metadata">The array with line metadata.</param>
		/// <returns>An IEnumerable with any line ID entries removed.</returns>
		private IEnumerable<string> RemoveLineIDFromMetadata(string[] metadata)
		{
			return metadata.Where(x => !x.StartsWith("line:"));
		}
	}
}
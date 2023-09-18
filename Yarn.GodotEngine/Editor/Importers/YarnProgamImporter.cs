#if TOOLS
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Godot.Collections;
using Yarn.Compiler;
using System.IO;
using System;
using System.Threading.Tasks.Sources;

namespace Yarn.GodotEngine.Editor.Importers
{
	using FileAccess = Godot.FileAccess;

	[Tool]
	public partial class YarnProgramImporter : EditorImportPlugin
	{
		private const string _exportTranslationOption = "export_translation_file";

		public override string _GetImporterName()
		{
			return typeof(YarnProgramImporter).FullName;
		}

		public override string _GetVisibleName()
		{
			return typeof(YarnProgramImporter).Name;
		}

		public override string[] _GetRecognizedExtensions()
		{
			return new string[] { "yarn" };
		}

		public override string _GetSaveExtension()
		{
			return "tres";
		}

		public override string _GetResourceType()
		{
			return "Resource";
		}

		public override int _GetPresetCount()
		{
			return 1;
		}

		public override string _GetPresetName(int presetIndex)
		{
			return "Default";
		}

		public override float _GetPriority()
		{
			return 1f;
		}

		public override int _GetImportOrder()
		{
			// 0 is default, and we want this to run
			// before the YarnProject importer, which is a default Resource
			return -1;
		}

		public override Array<Dictionary> _GetImportOptions(string path, int presetIndex)
		{
			return new Array<Dictionary>
			{
				new Dictionary
				{
					{ "name", _exportTranslationOption },
					{ "default_value", false },
				},
			};
		}

		public override bool _GetOptionVisibility(
			string path,
			StringName optionName,
			Dictionary options
		)
		{
			return true;
		}

		public override Error _Import(
			string sourceFile,
			string savePath,
			Dictionary options,
			Array<string> platformVariants,
			Array<string> genFiles
		)
		{
			Error ExportTranslationFile(
				IEnumerable<StringTableEntry> stringTableEntries,
				out string filePath
			)
			{
				filePath = string.Empty;
				if (!stringTableEntries.Any())
				{
					GD.PushError("stringTableEntries.Count() == 0");
					return Error.InvalidData;
				}

				var dummyScript = new EditorScript();
				var editorInterface = dummyScript.GetEditorInterface();
				var editorSettings = editorInterface.GetEditorSettings();

				var translationsDirSetting = editorSettings.Get(EditorSettings.TranslationsDirectoryProperty);
				string translationsDir = translationsDirSetting.AsString();
				if (string.IsNullOrEmpty(translationsDir))
				{
					GD.PushError("string.IsNullOrEmpty(translationsDir)");
					return Error.InvalidData;
				}

				// remove trailing slash for consistency
				if (translationsDir.EndsWith("/"))
				{
					translationsDir = translationsDir.Left(translationsDir.Length - 1);
				}

				GD.Print("translationsDir = " + translationsDir);

				var baseLocaleSetting = editorSettings.Get(EditorSettings.BaseLocaleProperty);
				string baseLanguage = baseLocaleSetting.AsString();
				if (string.IsNullOrEmpty(baseLanguage))
				{
					GD.PushError("string.IsNullOrEmpty(baseLanguage)");
					return Error.InvalidData;
				}

				GD.Print("baseLanguage = " + baseLanguage);

				string fileName = string.Empty;
				try
				{
					fileName = Path.GetFileNameWithoutExtension(sourceFile);
				}
				catch (Exception ex)
				{
					GD.PushError(ex);
					return Error.InvalidData;
				}

				if (string.IsNullOrEmpty(fileName))
				{
					GD.PushError("string.IsNullOrEmpty(fileName)");
					return Error.InvalidData;
				}

				if (!DirAccess.DirExistsAbsolute(translationsDir))
				{
					var makeDirErr = DirAccess.MakeDirAbsolute(translationsDir);
					if (makeDirErr != Error.Ok)
					{
						GD.PushError($"!DirAccess.MakeDirAbsolute ({translationsDir}); error = {makeDirErr}");
						return makeDirErr;
					}
				}

				filePath = $"{translationsDir}/{fileName}.csv";
				using (var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write))
				{
					if (file == null)
					{
						GD.PushError($"file == null ({filePath}); error = {FileAccess.GetOpenError()}");
						return FileAccess.GetOpenError();
					}

					// TODO: read existing translations and compare against string table entries
					var headers = new HashSet<string>
					{
						"key",
						baseLanguage
					};

					var localesSetting = editorSettings.Get(EditorSettings.SupportedLocalesProperty);
					var locales = localesSetting.AsStringArray();
					foreach (var lc in locales)
					{
						headers.Add(lc);
					}

					GD.Print("locales = " + locales.Join(", "));

					file.StoreCsvLine(headers.ToArray());
					foreach (var entry in stringTableEntries)
					{
						string[] line = new string[headers.Count];
						line[0] = entry.Id;
						line[1] = entry.Text;
						for (int i = 2; i < line.Length; i++)
						{
							// TODO: populate additional locales with temp strings
							line[i] = entry.Text;
						}

						file.StoreCsvLine(line);
					}

					GD.Print($"Exported translation at '{filePath}'");
				}

				// NOTE: if I don't include this, AppendImportExternalResource 
				// below returns a 'FileNotFound' error
				var fs = editorInterface.GetResourceFilesystem();
				fs.UpdateFile(filePath);

				var appendResult = AppendImportExternalResource(filePath, customImporter: "CSV Translation");
				if (appendResult != Error.Ok)
				{
					GD.PushError($"!AppendImportExternalResource '{filePath}'; error = {appendResult}");
					return appendResult;
				}

				return Error.Ok;
			}

			using var file = FileAccess.Open(sourceFile, FileAccess.ModeFlags.Read);
			if (file.GetError() != Error.Ok)
			{
				return Error.Failed;
			}

			var yarnProgram = new YarnProgram();
			string text = file.GetAsText();

			var job = CompilationJob.CreateFromString(sourceFile, text);
			var compilation = Compiler.Compiler.Compile(job);
			var errors = compilation.Diagnostics.Where(
				x => x.Severity == Diagnostic.DiagnosticSeverity.Error
			);

			// Import errors
			foreach (var error in errors)
			{
				GD.PushError(error.Message);
			}

			yarnProgram.Errors = errors.Select(x => x.Message).ToArray();

			// Import variable declarations
			var declarations = new List<Variable>();
			foreach (var declaration in compilation.Declarations)
			{
				if (declaration.Name.StartsWith("$Yarn.Internal."))
				{
					continue;
				}

				if (declaration.Type is FunctionType)
				{
					continue;
				}

				if (!Variable.TryCreateFromDeclaration(declaration, out var variable))
				{
					continue;
				}

				declarations.Add(variable);
			}

			yarnProgram.Declarations = new(declarations);

			// Import string table entries
			var stringTableEntries = compilation.StringTable
				.Select(x =>
				{
					return new StringTableEntry()
					{
						Id = x.Key,
						Text = x.Value.text,
						File = x.Value.fileName,
						Node = x.Value.nodeName,
						LineNumber = x.Value.lineNumber.ToString(),
						Lock = GetHashString(x.Value.text, 8),
						Comment = GenerateCommentWithLineMetadata(x.Value.metadata),
						MetaData = RemoveLineIDFromMetadata(x.Value.metadata).ToArray()
					};
				});

			yarnProgram.StringTableEntries = new(stringTableEntries);

			// Export translations file
			var exportTranslations = options[_exportTranslationOption];
			if (exportTranslations.AsBool())
			{
				var exportTranslationsResult = ExportTranslationFile(
					stringTableEntries,
					out string translationsFile
				);

				if (exportTranslationsResult != Error.Ok)
				{
					return exportTranslationsResult;
				}

				yarnProgram.TranslationsFile = translationsFile;
			}

			// Save yarn program resource file
			string fileName = $"{savePath}.{_GetSaveExtension()}";
			var saveResult = ResourceSaver.Save(yarnProgram, fileName);
			if (saveResult != Error.Ok)
			{
				GD.PushError($"!ResourceSaver.Save '{fileName}'; err = " + saveResult);
				return saveResult;
			}

			return Error.Ok;
		}

		/// <summary>
		/// Returns a byte array containing a SHA-256 hash of <paramref
		/// name="inputString"/>.
		/// </summary>
		/// <param name="inputString">The string to produce a hash value
		/// for.</param>
		/// <returns>The hash of <paramref name="inputString"/>.</returns>
		private static byte[] GetHash(string inputString)
		{
			using var algorithm = System.Security.Cryptography.SHA256.Create();
			return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
		}

		/// <summary>
		/// Returns a string containing the hexadecimal representation of a
		/// SHA-256 hash of <paramref name="inputString"/>.
		/// </summary>
		/// <param name="inputString">The string to produce a hash
		/// for.</param>
		/// <param name="limitCharacters">The length of the string to
		/// return. The returned string will be at most <paramref
		/// name="limitCharacters"/> characters long. If this is set to -1,
		/// the entire string will be returned.</param>
		/// <returns>A string version of the hash.</returns>
		private static string GetHashString(string inputString, int limitCharacters = -1)
		{
			var sb = new StringBuilder();
			foreach (byte b in GetHash(inputString))
			{
				sb.Append(b.ToString("x2"));
			}

			if (limitCharacters == -1)
			{
				// Return the entire string
				return sb.ToString();
			}
			else
			{
				// Return a substring (or the entire string, if
				// limitCharacters is longer than the string)
				return sb.ToString(0, Mathf.Min(sb.Length, limitCharacters));
			}
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
		private static string GenerateCommentWithLineMetadata(string[] metadata)
		{
			var cleanedMetadata = RemoveLineIDFromMetadata(metadata);
			return cleanedMetadata.Any()
				? $"Line metadata: {string.Join(" ", cleanedMetadata)}"
				: string.Empty;
		}

		/// <summary>
		/// Removes any line ID entry from an array of line metadata.
		/// Line metadata will always contain a line ID entry if it's set. For
		/// example, if a line contains "#line:1eaf1e55", its line metadata
		/// will always have an entry with "line:1eaf1e55".
		/// </summary>
		/// <param name="metadata">The array with line metadata.</param>
		/// <returns>An IEnumerable with any line ID entries removed.</returns>
		private static IEnumerable<string> RemoveLineIDFromMetadata(string[] metadata)
		{
			return metadata.Where(x => !x.StartsWith("line:"));
		}


	}
}
#endif

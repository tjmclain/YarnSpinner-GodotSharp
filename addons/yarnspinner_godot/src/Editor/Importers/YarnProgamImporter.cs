#if TOOLS

using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Yarn.Compiler;
using System.IO;

namespace Yarn.GodotSharp.Editor.Importers
{
	using FileAccess = Godot.FileAccess;

	[Tool]
	public partial class YarnProgramImporter : EditorImportPlugin
	{
		protected ImporterStringSubstitutions Substitutions { get; private set; }

		protected virtual Error ExportStringTableFile(
			string sourceFile,
			Dictionary importOptions,
			out string stringTableFile
		)
		{
			stringTableFile = string.Empty;

			if (!importOptions.TryGetValue(OptionName.ExportStringsForTranslation, out var option))
			{
				GD.PushWarning("!importOptions.TryGetValue OptionName.ExportTranslations");
				return Error.Ok;
			}

			if (!option.AsBool())
			{
				return Error.Ok;
			}

			var compileError = YarnProgram.Compile(sourceFile, out var compilationResult);
			if (compileError != Error.Ok)
			{
				GD.PushError($"!YarnProgram.Compile '{sourceFile}'");
				return compileError;
			}

			var stringInfo = compilationResult.StringTable;
			if (!stringInfo.Any())
			{
				GD.PushWarning("!stringInfoTable.Any");
				return Error.Ok;
			}

			if (!importOptions.TryGetValue(OptionName.StringTableFilePath, out var tranlationFilePath))
			{
				GD.PushWarning("!importOptions.TryGetValue OptionName.TranslationFilePath");
				return Error.Ok;
			}

			stringTableFile = tranlationFilePath.AsString();
			stringTableFile = Substitutions.InterpolateString(stringTableFile);

			if (!stringTableFile.EndsWith(".csv"))
			{
				stringTableFile += ".csv";
			}

			string globalPath = ProjectSettings.GlobalizePath(stringTableFile);
			string globalDir = Path.GetDirectoryName(globalPath);

			DirAccess.MakeDirRecursiveAbsolute(globalDir);

			stringTableFile = GodotEditorUtility.LocalizePath(stringTableFile);
			GD.Print("stringTableFile = " + stringTableFile);

			// create a table of entries from our compiled string table
			var stringTable = new StringTable();
			stringTable.CreateEntriesFrom(stringInfo);

			// create a hashset of headers, starting with the entry's 'key'
			// each column after the first is a translated string
			var headers = new HashSet<string>(StringTable.GetDefaultCsvHeaders());

			//Try to load an existing table at out desired path and merge it with our new table
			static bool TryLoadExistingStringTable(string filePath, out StringTable existingTable)
			{
				existingTable = default;
				if (!FileAccess.FileExists(filePath))
				{
					return false;
				}

				var importError = YarnStringTableImporter.ImportStringTable(filePath, out existingTable);
				return importError == Error.Ok;
			}

			if (TryLoadExistingStringTable(stringTableFile, out var existingTable))
			{
				foreach (var header in existingTable.CsvHeaders)
				{
					headers.Add(header);
				}
				stringTable.MergeFrom(existingTable);
			}

			// Write translations from string entries
			using (var file = FileAccess.Open(stringTableFile, FileAccess.ModeFlags.Write))
			{
				if (file == null)
				{
					var openError = FileAccess.GetOpenError();
					GD.PushError($"!FileAccess.Open '{stringTableFile}'; openError = {openError}");
					return openError;
				}

				// Write header row
				var keys = headers.ToArray();
				file.StoreCsvLine(keys);

				// Read rows from entries
				foreach (var entry in stringTable.Values)
				{
					string[] line = entry.ToCsvLine(headers);
					file.StoreCsvLine(line);
				}

				GD.Print($"Exported translation at '{stringTableFile}'");
			}

			// NOTE: if I don't include this, AppendImportExternalResource
			// below returns a 'FileNotFound' error
			var fs = GodotEditorUtility.GetEditorInterface()?.GetResourceFilesystem();
			if (fs == null)
			{
				GD.PushWarning("GodotEditorUtility.GetResourceFilesystem() == null");
				return Error.Failed;
			}

			fs.UpdateFile(stringTableFile);

			GD.Print("customImporter: " + YarnStringTableImporter.ImporterName);

			var appendResult = AppendImportExternalResource(
				stringTableFile,
				customImporter: YarnStringTableImporter.ImporterName
			);

			if (appendResult != Error.Ok)
			{
				GD.PushError(
					$"!AppendImportExternalResource '{stringTableFile}'; error = {appendResult}"
				);
				return appendResult;
			}

			return Error.Ok;
		}

		protected virtual Error AddLineIdTags(string sourceFile, Dictionary options)
		{
			options.TryGetValue(OptionName.AddLineIdTags, out var addLineIdTags);
			if (!addLineIdTags.AsBool())
			{
				return Error.Ok;
			}

			string globalPath = ProjectSettings.GlobalizePath(sourceFile);
			string contents = File.ReadAllText(globalPath);
			if (string.IsNullOrEmpty(contents))
			{
				GD.PushError("AddLineIdTags: File.ReadAllText encounted an error");
				return Error.InvalidData;
			}

			// Produce a version of this file that contains line
			// tags added where they're needed.
			var taggedContents = Utility.AddTagsToLines(contents);

			// if the file has an error it returns null
			// we want to bail out then otherwise we'd wipe the yarn file
			if (string.IsNullOrEmpty(taggedContents))
			{
				GD.PushError("AddLineIdTags: Utility.AddTagsToLines encountered an error");
				return Error.InvalidData;
			}

			// If this produced a modified version of the file,
			// write it out and re-import it.
			if (contents != taggedContents)
			{
				File.WriteAllText(globalPath, taggedContents);
			}

			return Error.Ok;
		}

		public static class OptionName
		{
			public const string AddLineIdTags = "add_line_id_tags";
			public const string ExportStringsForTranslation = "export_strings_for_translation";
			public const string StringTableFilePath = "string_table_file_path";
		}

		#region EditorImportPlugin

		public override string _GetImporterName()
		{
			return typeof(YarnProgramImporter).FullName;
		}

		public override string _GetVisibleName()
		{
			return "Yarn Program";
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
					{ GodotEditorPropertyInfo.NameKey, OptionName.AddLineIdTags },
					{ GodotEditorPropertyInfo.DefaultValueKey, false },
				},
				new Dictionary
				{
					{ GodotEditorPropertyInfo.NameKey, OptionName.ExportStringsForTranslation },
					{ GodotEditorPropertyInfo.DefaultValueKey, false },
				},
				new Dictionary
				{
					{ GodotEditorPropertyInfo.NameKey, OptionName.StringTableFilePath },
					{ GodotEditorPropertyInfo.DefaultValueKey,  "$(SourceDir)/$(SourceFileName).csv" },
					{ GodotEditorPropertyInfo.HintKey, Variant.From(PropertyHint.File) }
				}
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
			// Start import
			GD.Print($"Importing '{sourceFile}'");

			Substitutions = new ImporterStringSubstitutions(sourceFile);

			var yarnProgram = new YarnProgram() { SourceFile = sourceFile };

			var addLineIdTagsResult = AddLineIdTags(sourceFile, options);
			if (addLineIdTagsResult != Error.Ok)
			{
				return addLineIdTagsResult;
			}

			var exportStringTableResult = ExportStringTableFile(
				sourceFile,
				options,
				out string stringTableFile
			);

			if (exportStringTableResult != Error.Ok)
			{
				return exportStringTableResult;
			}

			yarnProgram.StringTableFile = stringTableFile;

			// Save yarn program resource file
			string saveFile = $"{savePath}.{_GetSaveExtension()}";
			var saveResult = ResourceSaver.Save(yarnProgram, saveFile);
			if (saveResult != Error.Ok)
			{
				GD.PushError($"!ResourceSaver.Save '{saveFile}'; err = " + saveResult);
				return saveResult;
			}

			GD.Print($"Saved yarn program resource @ '{saveFile}'");

			return Error.Ok;
		}

		#endregion EditorImportPlugin
	}
}

#endif
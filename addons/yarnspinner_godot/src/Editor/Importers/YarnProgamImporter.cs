#if TOOLS

using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Yarn.Compiler;
using System.IO;
using System;
using CsvHelper.Configuration.Attributes;
using System.Diagnostics;

namespace Yarn.GodotSharp.Editor.Importers
{
	using FileAccess = Godot.FileAccess;

	[Tool]
	public partial class YarnProgramImporter : EditorImportPlugin
	{
		public static class OptionName
		{
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

			var yarnProgram = new YarnProgram() { SourceFile = sourceFile };

			// Compile yarn program source file
			var error = YarnProgram.Compile(sourceFile, out var compilationResult);
			if (error != Error.Ok)
			{
				GD.PushError($"!yarnProgram.SetSourceFile '{sourceFile}'");
				return error;
			}

			var exportTranslationsResult = ExportTranslationFile(
				sourceFile,
				compilationResult,
				options,
				out string stringTableFile
			);

			if (exportTranslationsResult != Error.Ok)
			{
				return exportTranslationsResult;
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

			//if (!string.IsNullOrEmpty(stringTableFile))
			//{
			//	var appendResult = AppendImportExternalResource(
			//		stringTableFile,
			//		customImporter: YarnStringTableImporter.ImporterName
			//	);

			//	if (appendResult != Error.Ok)
			//	{
			//		GD.PushError($"AppendImportExternalResource: {appendResult}; translationFile = {stringTableFile}");
			//		return appendResult;
			//	}

			//	GD.Print($"Exported string table @ '{stringTableFile}'");
			//}

			return Error.Ok;
		}

		#endregion EditorImportPlugin

		private Error ExportTranslationFile(
			string sourceFile,
			CompilationResult compilationResult,
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

			sourceFile = ProjectSettings.GlobalizePath(sourceFile);

			string sourceDir = Path.GetDirectoryName(sourceFile);
			stringTableFile = stringTableFile.Replace("$(SourceDir)", sourceDir);

			string sourceFileName = Path.GetFileNameWithoutExtension(sourceFile);
			stringTableFile = stringTableFile.Replace("$(SourceFileName)", sourceFileName);

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
			var headers = new HashSet<string>(StringTableEntry.GetCsvHeaders());

			// Try to load an existing table at out desired path and merge it with our new table
			var existingTable = ResourceLoader.Load<StringTable>(stringTableFile);
			if (existingTable != null)
			{
				foreach (var header in existingTable.CsvHeaders)
				{
					headers.Add(header);
				}
				stringTable.MergeTranslationsFrom(existingTable);
			}

			// Write translations from string entries
			using (var file = FileAccess.Open(stringTableFile, FileAccess.ModeFlags.Write))
			{
				if (file == null)
				{
					var error = FileAccess.GetOpenError();
					GD.PushError($"!FileAccess.Open '{stringTableFile}'; error = {error}");
					return error;
				}

				// Write header row
				var keys = headers.ToArray();
				file.StoreCsvLine(keys);

				// Read rows from entries
				foreach (var entry in stringTable.Values)
				{
					var row = entry.ToCsvRow();

					string[] line = new string[keys.Length];
					for (int i = 0; i < keys.Length; i++)
					{
						string key = keys[i];
						row.TryGetValue(key, out line[i]);
					}
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
	}
}

#endif

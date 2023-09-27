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
			public const string ExportTranslations = "export_translations";
			public const string TranslationFilePath = "translation_file_path";
		}

		#region EditorImportPlugin

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
					{ GodotEditorPropertyInfo.NameKey, OptionName.ExportTranslations },
					{ GodotEditorPropertyInfo.DefaultValueKey, false },
				},
				new Dictionary
				{
					{ GodotEditorPropertyInfo.NameKey, OptionName.TranslationFilePath },
					{ GodotEditorPropertyInfo.DefaultValueKey,  "$(SourceDir)/translations/$(SourceFileName).csv" },
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

			var yarnProgram = new YarnProgram();

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
				out string translationFile
			);

			if (exportTranslationsResult != Error.Ok)
			{
				return exportTranslationsResult;
			}

			yarnProgram.TranslationsFile = translationFile;

			// Save yarn program resource file
			string fileName = $"{savePath}.{_GetSaveExtension()}";
			var saveResult = ResourceSaver.Save(yarnProgram, fileName);
			if (saveResult != Error.Ok)
			{
				GD.PushError($"!ResourceSaver.Save '{fileName}'; err = " + saveResult);
				return saveResult;
			}

			GD.Print($"Saved yarn program resource '{fileName}'");

			if (!string.IsNullOrEmpty(translationFile))
			{
				var appendResult = AppendImportExternalResource(
					translationFile,
					customImporter: YarnLocalizationImporter.ImporterName
				);
			}

			return Error.Ok;
		}

		#endregion EditorImportPlugin

		private Error ExportTranslationFile(
			string sourceFile,
			CompilationResult compilationResult,
			Dictionary importOptions,
			out string translationFile
		)
		{
			translationFile = string.Empty;

			if (!importOptions.TryGetValue(OptionName.ExportTranslations, out var option))
			{
				GD.PushWarning("!importOptions.TryGetValue OptionName.ExportTranslations");
				return Error.Ok;
			}

			if (!option.AsBool())
			{
				return Error.Ok;
			}

			var stringInfoTable = compilationResult.StringTable;
			if (!stringInfoTable.Any())
			{
				GD.PushWarning("!stringInfoTable.Any");
				return Error.Ok;
			}

			if (!importOptions.TryGetValue(OptionName.TranslationFilePath, out var tranlationFilePath))
			{
				GD.PushWarning("!importOptions.TryGetValue OptionName.TranslationFilePath");
				return Error.Ok;
			}

			translationFile = tranlationFilePath.AsString();

			sourceFile = ProjectSettings.GlobalizePath(sourceFile);

			string sourceDir = Path.GetDirectoryName(sourceFile);
			translationFile = translationFile.Replace("$(SourceDir)", sourceDir);

			string sourceFileName = Path.GetFileNameWithoutExtension(sourceFile);
			translationFile = translationFile.Replace("$(SourceFileName)", sourceFileName);

			string globalPath = ProjectSettings.GlobalizePath(translationFile);
			string globalDir = Path.GetDirectoryName(globalPath);

			DirAccess.MakeDirRecursiveAbsolute(globalDir);

			translationFile = GodotUtility.LocalizePath(translationFile);
			GD.Print("translationFile = " + translationFile);

			// create a hashset of headers, starting with the entry's 'key'
			// each column after the first is a translated string
			var headers = new HashSet<string>(StringTableEntry.GetCsvHeaders());

			// read the existing table, if it exists
			GodotUtility.ReadCsv(translationFile, out var existingHeaders, out var existingTable);

			foreach (var header in existingHeaders)
			{
				headers.Add(header);
			}

			// create a table of entries from the existing csv
			var existingEntries = new Godot.Collections.Dictionary<string, StringTableEntry>();
			foreach (var kvp in existingTable)
			{
				var entry = new StringTableEntry(kvp.Value);
				existingEntries[kvp.Key] = entry;
			}

			// create a table of entries from our compiled string table
			var entries = new Godot.Collections.Dictionary<string, StringTableEntry>();
			foreach (var kvp in stringInfoTable)
			{
				var entry = new StringTableEntry(kvp.Key, kvp.Value);
				entries[kvp.Key] = entry;
			}

			foreach (var kvp in existingEntries)
			{
				var existingEntry = kvp.Value;
				if (!entries.TryGetValue(kvp.Key, out var entry))
				{
					existingEntry.Lock = string.Empty;
					entries[kvp.Key] = existingEntry;
					continue;
				}

				entry.MergeTranslationsFrom(existingEntry);
			}

			// Write translations from string entries
			using (var file = FileAccess.Open(translationFile, FileAccess.ModeFlags.Write))
			{
				if (file == null)
				{
					var error = FileAccess.GetOpenError();
					GD.PushError($"!FileAccess.Open '{translationFile}'; error = {error}");
					return error;
				}

				// Write header row
				var headerRow = headers.ToArray();
				file.StoreCsvLine(headerRow);

				// Read rows from entries
				foreach (var entry in entries.Values)
				{
					var row = entry.ToDictionary();

					string[] line = new string[headerRow.Length];
					for (int i = 0; i < headerRow.Length; i++)
					{
						string key = headerRow[i];
						row.TryGetValue(key, out line[i]);
					}
					file.StoreCsvLine(line);
				}

				GD.Print($"Exported translation at '{translationFile}'");
			}

			// NOTE: if I don't include this, AppendImportExternalResource
			// below returns a 'FileNotFound' error
			var fs = GodotEditorUtility.GetEditorInterface()?.GetResourceFilesystem();
			if (fs == null)
			{
				GD.PushWarning("GodotEditorUtility.GetResourceFilesystem() == null");
				return Error.Failed;
			}

			fs.UpdateFile(translationFile);

			GD.Print("customImporter: " + YarnLocalizationImporter.ImporterName);

			//var appendResult = AppendImportExternalResource(
			//	translationFile,
			//	customImporter: YarnLocalizationImporter.ImporterName
			//);

			//if (appendResult != Error.Ok)
			//{
			//	GD.PushError(
			//		$"!AppendImportExternalResource '{translationFile}'; error = {appendResult}"
			//	);
			//	return appendResult;
			//}

			return Error.Ok;
		}
	}
}

#endif

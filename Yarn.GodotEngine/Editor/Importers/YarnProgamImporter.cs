#if TOOLS

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Godot.Collections;
using Yarn.Compiler;
using System.IO;
using System;

namespace Yarn.GodotEngine.Editor.Importers
{
	using FileAccess = Godot.FileAccess;
	using StringDict = System.Collections.Generic.Dictionary<string, string>;

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
			// Start import
			GD.Print($"Importing '{sourceFile}'");

			var yarnProgram = new YarnProgram()
			{
				SourceFile = sourceFile
			};

			// Compile yarn program source file
			var error = yarnProgram.CompileSourceFile();
			if (error != Error.Ok)
			{
				GD.PushError($"!yarnProgram.SetSourceFile '{sourceFile}'");
				return error;
			}

			// Export translations file
			string translationsFile = string.Empty;
			var exportTranslations = options[_exportTranslationOption];
			if (exportTranslations.AsBool())
			{
				var stringTableEntries = yarnProgram.StringTable.Values.ToArray();

				var exportTranslationsResult = ExportTranslationFile(
					sourceFile,
					stringTableEntries,
					out translationsFile
				);

				if (exportTranslationsResult != Error.Ok)
				{
					return exportTranslationsResult;
				}

				// Append import of translations file
				var appendResult = AppendImportExternalResource(translationsFile);
				if (appendResult != Error.Ok)
				{
					GD.PushError($"!AppendImportExternalResource '{translationsFile}'; error = {appendResult}");
					return appendResult;
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

			GD.Print($"Saved yarn program resource '{fileName}'");

			// After save, add translations to project settings
			if (exportTranslations.AsBool())
			{
				AddTranslationsToTranslationServer(translationsFile);
			}

			return Error.Ok;
		}

		private static Error ExportTranslationFile(
			string sourceFile,
			StringTableEntry[] stringTableEntries,
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

			string fileName;
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

			var rows = new System.Collections.Generic.Dictionary<string, StringDict>();
			var headers = new HashSet<string>
				{
					"key",
					baseLanguage
				};

			filePath = $"{translationsDir}/{fileName}.csv";

			// Try to read existing translations and load them into our rows dict
			if (FileAccess.FileExists(filePath))
			{
				using (var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read))
				{
					string[] headerValues = file.GetCsvLine();
					foreach (string header in headerValues)
					{
						headers.Add(header);
					}

					while (file.GetPosition() < file.GetLength())
					{
						var row = new StringDict();
						string[] values = file.GetCsvLine();
						for (int i = 0; i < headerValues.Length; i++)
						{
							string key = headerValues[i];
							string value = values[i];
							row[key] = value;
						}

						string id = values[0];
						rows[id] = row;
					}
				}
			}

			// Write translations from string entries
			using (var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write))
			{
				if (file == null)
				{
					var error = FileAccess.GetOpenError();
					GD.PushError($"!FileAccess.Open '{filePath}'; error = {error}");
					return error;
				}

				var localesSetting = editorSettings.Get(EditorSettings.SupportedLocalesProperty);
				var locales = localesSetting.AsStringArray();
				foreach (var lc in locales)
				{
					headers.Add(lc);
				}

				GD.Print("locales = " + locales.Join(", "));

				// Reconcile new and existing entries
				// TODO: cleanup ids that don't exist in incoming entries
				foreach (var entry in stringTableEntries)
				{
					if (rows.TryGetValue(entry.Id, out var row))
					{
						row[baseLanguage] = entry.Text;
						continue;
					}

					rows[entry.Id] = new StringDict()
						{
							{ "key", entry.Id },
							{ baseLanguage, entry.Text }
						};
				}

				// Write header row
				file.StoreCsvLine(headers.ToArray());

				var enumerator = rows.GetEnumerator();
				while (enumerator.MoveNext())
				{
					var row = enumerator.Current.Value;
					var values = new List<string>(headers.Count);
					foreach (string key in headers)
					{
						row.TryGetValue(key, out string value);
						values.Add(value);
					}

					file.StoreCsvLine(values.ToArray());
				}

				GD.Print($"Exported translation at '{filePath}'");
			}

			// NOTE: if I don't include this, AppendImportExternalResource
			// below returns a 'FileNotFound' error
			var fs = editorInterface.GetResourceFilesystem();
			fs.UpdateFile(filePath);

			return Error.Ok;
		}

		private static void AddTranslationsToTranslationServer(string translationsSourceFile)
		{
			if (string.IsNullOrEmpty(translationsSourceFile))
			{
				GD.PushError("string.IsNullOrEmpty(translationsSourceFile)");
				return;
			}

			string globalPath = ProjectSettings.GlobalizePath(translationsSourceFile);
			string dir = Path.GetDirectoryName(globalPath);
			string baseFileName = Path.GetFileNameWithoutExtension(translationsSourceFile);
			string searchPattern = $"{baseFileName}.*.translation";

			var files = Directory.GetFiles(dir, searchPattern);
			if (files.Length == 0)
			{
				GD.Print($"No translation files found; dir = {dir}, searchPattern = {searchPattern}");
				return;
			}

			// there's no exposed property for this, so we have to get it via name
			string settingName = "internationalization/locale/translations";

			var translationsSetting = ProjectSettings.GetSetting(settingName, System.Array.Empty<string>());
			var translations = new List<string>(translationsSetting.AsStringArray());

			bool changed = false;
			foreach (var file in files)
			{
				// NOTE: LocalizePath isn't working for some reason. It's just giving me back the
				//       global path. So, I'm using my own utility method. I should ask about this
				//       on Discord or Github and get a better answer. This works, but it's not ideal.
				// string localPath = ProjectSettings.LocalizePath(file);
				string localPath = GodotUtility.LocalizePath(file);

				if (translations.Contains(localPath))
				{
					continue;
				}

				translations.Add(localPath);
				changed = true;
				GD.Print($"Add translation: " + localPath);
			}

			if (!changed)
			{
				return;
			}

			ProjectSettings.SetSetting(settingName, translations.ToArray());
			ProjectSettings.Save();
		}
	}
}

#endif

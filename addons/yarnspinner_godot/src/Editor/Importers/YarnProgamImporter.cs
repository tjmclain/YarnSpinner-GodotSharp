#if TOOLS

using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Yarn.Compiler;
using System.IO;
using System;

namespace Yarn.GodotSharp.Editor.Importers
{
	using FileAccess = Godot.FileAccess;
	using StringDict = System.Collections.Generic.Dictionary<string, string>;
	using PropertyInfo = GodotUtility.PropertyInfo;

	[Tool]
	public partial class YarnProgramImporter : EditorImportPlugin
	{
		private const string _exportTranslationOption = "export_translation_file";
		private const string _translationsDirProperty = "yarn_spinner/translations_directory";
		private const string _baseLocaleProperty = "yarn_spinner/base_locale";

		private readonly PropertyInfo[] _properties = new PropertyInfo[]
		{
			new PropertyInfo()
			{
				Name = _translationsDirProperty,
				Type = Variant.Type.String,
				Hint = PropertyHint.Dir,
				DefaultValue = "res://translations/"
			},
			new PropertyInfo()
			{
				Name = _baseLocaleProperty,
				Type = Variant.Type.String,
				Hint = PropertyHint.LocaleId,
				DefaultValue = "en"
			},
		};

		#region Public Methods

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

		public YarnProgramImporter()
		{
			foreach (var property in _properties)
			{
				property.AddToProjectSettings();
			}
		}

		//protected override void Dispose(bool disposing)
		//{
		//	foreach (var property in _properties)
		//	{
		//		property.RemoveFromProjectSettings();
		//	}

		//	base.Dispose(disposing);
		//}

		public override Array<Dictionary> _GetImportOptions(string path, int presetIndex)
		{
			return new Array<Dictionary>
			{
				new Dictionary
				{
					{ PropertyInfo.NameKey, "export_translation_file" },
					{ PropertyInfo.DefaultValueKey, false },
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

			var yarnProgram = new YarnProgram();

			// Compile yarn program source file
			var error = yarnProgram.Compile(sourceFile, out var compilationResult);
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
				var exportTranslationsResult = ExportTranslationFile(
					sourceFile,
					compilationResult,
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

		public virtual Error ExportTranslationFile(
			string programSourceFile,
			CompilationResult programComplilationResult,
			out string translationFile
		)
		{
			translationFile = string.Empty;

			var stringTable = programComplilationResult.StringTable;
			if (!stringTable.Any())
			{
				GD.PushError("!stringTableEntries.Any == 0");
				return Error.InvalidData;
			}

			var translationsDirSetting = Variant.From(string.Empty); //ProjectSettings.GetSetting(_translationsDirProperty);
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

			var baseLocaleSetting = Variant.From(string.Empty); // ProjectSettings.GetSetting(_baseLocaleProperty);
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
				fileName = Path.GetFileNameWithoutExtension(programSourceFile);
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

			translationFile = $"{translationsDir}/{fileName}.csv";

			// read the existing table, if it exists
			GodotUtility.ReadCsv(translationFile, out var existingHeaders, out var existingTable);

			// create a hashset of headers, starting with the entry's 'key'
			// each column after the first is a translated string
			const string keyHeader = "key";
			var headers = new HashSet<string>()
			{
				keyHeader,
				baseLanguage
			};

			foreach (var header in existingHeaders)
			{
				headers.Add(header);
			}

			// create a table of entries from our compiled string table
			var table = new System.Collections.Generic.Dictionary<string, StringDict>();
			foreach (var stringTableEntry in stringTable)
			{
				string key = stringTableEntry.Key;
				var row = new StringDict()
				{
					{ keyHeader, key },
					{ baseLanguage, stringTableEntry.Value.text }
				};
				table[key] = row;

				if (!existingTable.TryGetValue(key, out var otherRow))
				{
					continue;
				}

				// add existing translations to our table entries
				foreach (var kvp in otherRow)
				{
					if (kvp.Key == keyHeader)
					{
						continue;
					}

					if (kvp.Key == baseLanguage)
					{
						continue;
					}

					row[kvp.Key] = kvp.Value;
				}
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
				foreach (var row in table.Values)
				{
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
			var fs = GetResourceFilesystem();
			if (fs == null)
			{
				GD.PushWarning("GodotEditorUtility.GetResourceFilesystem() == null");
			}
			else
			{
				fs.UpdateFile(translationFile);
			}

			return Error.Ok;
		}

		#endregion Public Methods

		protected virtual EditorFileSystem GetResourceFilesystem()
		{
			// this is weird, but it works. I'll keep looking for a better way to do this
			var dummyScript = new EditorScript();
			return dummyScript.GetEditorInterface()?.GetResourceFilesystem();
		}

		#region Private Methods

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

			var translations = GodotUtility.TranslationsProjectSetting.Get().ToList();

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

				GD.Print($"Add translation: " + localPath);
				translations.Add(localPath);
				changed = true;
			}

			if (!changed)
			{
				return;
			}

			GodotUtility.TranslationsProjectSetting.Set(translations.ToArray());
		}

		#endregion Private Methods
	}
}

#endif

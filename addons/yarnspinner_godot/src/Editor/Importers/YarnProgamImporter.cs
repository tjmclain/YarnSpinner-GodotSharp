#if TOOLS

using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Yarn.Compiler;
using System.IO;
using System;
using System.Runtime.CompilerServices;

namespace Yarn.GodotSharp.Editor.Importers
{
	using FileAccess = Godot.FileAccess;
	using StringDict = System.Collections.Generic.Dictionary<string, string>;

	[Tool]
	public partial class YarnProgramImporter : EditorImportPlugin
	{
		public const string ExportTranslationOption = "export_translation";
		public const string OverrideTranslationsDirOption = "override_translation_directory";

		// "C:\Users\thoma\Projects\YarnSpinner-GodotSharp\addons\yarnspinner_godot\src\Editor\Importers\YarnProgramTranslationImporter.cs"
		//private const string _defaultTranslationsImporterScriptPath = "res://addons/yarnspinner_godot/src/Editor/Importers/YarnProgramTranslationImporter.cs";

		//private const string _defaultTranslationsImporterType = "Yarn.GodotSharp.Editor.Importers.YarnProgramTranslationImporter";

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
					{ GodotEditorPropertyInfo.NameKey, ExportTranslationOption },
					{ GodotEditorPropertyInfo.DefaultValueKey, false },
				},
				new Dictionary
				{
					{ GodotEditorPropertyInfo.NameKey, OverrideTranslationsDirOption },
					{ GodotEditorPropertyInfo.DefaultValueKey, "" },
					{ GodotEditorPropertyInfo.HintKey,  Variant.From(PropertyHint.Dir) }
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
			var error = YarnProgram.Compile(sourceFile, out var compilationResult);
			if (error != Error.Ok)
			{
				GD.PushError($"!yarnProgram.SetSourceFile '{sourceFile}'");
				return error;
			}

			var exportTranslations = options[ExportTranslationOption];
			if (exportTranslations.AsBool())
			{
				//Export translations file
				var exportTranslationsResult = ExportTranslationFile(
					sourceFile,
					compilationResult,
					options,
					out string translationsFile
				);

				if (exportTranslationsResult != Error.Ok)
				{
					return exportTranslationsResult;
				}

				//Append import of translations file
				var appendResult = AppendImportExternalResource(translationsFile);
				if (appendResult != Error.Ok)
				{
					GD.PushError($"!AppendImportExternalResource '{translationsFile}'; error = {appendResult}");
					return appendResult;
				}

				yarnProgram.TranslationsFile = translationsFile;
			}

			//if (ImportTranslations(sourceFile, options, out string translationsFile))
			//{
			//	//Append import of translations file
			//	var appendResult = AppendImportExternalResource(translationsFile);
			//	if (appendResult != Error.Ok)
			//	{
			//		GD.PushError($"!AppendImportExternalResource '{translationsFile}'; error = {appendResult}");
			//		return appendResult;
			//	}

			//	yarnProgram.TranslationsFile = translationsFile;
			//}

			// Save yarn program resource file
			string fileName = $"{savePath}.{_GetSaveExtension()}";
			var saveResult = ResourceSaver.Save(yarnProgram, fileName);
			if (saveResult != Error.Ok)
			{
				GD.PushError($"!ResourceSaver.Save '{fileName}'; err = " + saveResult);
				return saveResult;
			}

			GD.Print($"Saved yarn program resource '{fileName}'");

			return Error.Ok;
		}

		#endregion EditorImportPlugin

		private static bool ImportTranslations(
				string sourceFile,
				Dictionary importOptions,
				out string translationsFile
			)
		{
			translationsFile = string.Empty;

			var exportTranslations = importOptions[ExportTranslationOption];
			if (!exportTranslations.AsBool())
			{
				return false;
			}

			// TODO: allow overriding this from project settings
			//string scriptPath = _defaultTranslationsImporterScriptPath;
			//if (string.IsNullOrEmpty(scriptPath))
			//{
			//	GD.PushError("ImportTranslations: string.IsNullOrEmpty(scriptPath)");
			//	return false;
			//}

			//var script = ResourceLoader.Load<CSharpScript>(scriptPath);
			//if (script == null)
			//{
			//	GD.PushError("ImportTranslations: string.IsNullOrEmpty(scriptPath)");
			//	return false;
			//}

			//var result = script.Call(YarnProgramTranslationImporter.MethodName.Import, sourceFile, importOptions);
			//translationsFile = result.AsString();

			//string typeString = _defaultTranslationsImporterType;
			//if (string.IsNullOrEmpty(typeString))
			//{
			//	GD.PushError("ImportTranslations: string.IsNullOrEmpty(typeString)");
			//	return false;
			//}

			//var type = Type.GetType(typeString);
			//if (type == null)
			//{
			//	GD.PushError("ImportTranslations: type == null");
			//	return false;
			//}

			//var method = type.GetMethod("Import");
			//if (method == null)
			//{
			//	GD.PushError("ImportTranslations: method == null");
			//	return false;
			//}

			//object result;
			//var args = new object[] { sourceFile, importOptions };
			//if (method.IsStatic)
			//{
			//	result = method.Invoke(null, args);
			//}
			//else
			//{
			//	var instance = Activator.CreateInstance(type);
			//	result = method.Invoke(instance, args);
			//}

			//if (result == null)
			//{
			//	GD.PushError("ImportTranslations: result == null");
			//	return false;
			//}

			//translationsFile = result.ToString();

			//GD.Print("ImportTranslations: result = " + translationsFile);

			return true;
		}

		protected virtual Error ExportTranslationFile(
			string sourceFile,
			CompilationResult compilationResult,
			Dictionary options,
			out string translationFile
		)
		{
			static string GetTranslationsDirectory(Dictionary importOptions)
			{
				if (importOptions.TryGetValue(OverrideTranslationsDirOption, out var value))
				{
					string dir = value.AsString();
					if (!string.IsNullOrEmpty(dir))
					{
						return dir;
					}
				}

				var translationsDirSetting = ProjectSettings.GetSetting(Plugin.TranslationsDirProperty);
				return translationsDirSetting.AsString();
			}

			static string GetBaseLocale()
			{
				var baseLocaleSetting = ProjectSettings.GetSetting(Plugin.BaseLocaleProperty);
				return baseLocaleSetting.AsString();
			}

			translationFile = string.Empty;

			var stringTable = compilationResult.StringTable;
			if (!stringTable.Any())
			{
				GD.PushError("!stringTableEntries.Any == 0");
				return Error.InvalidData;
			}

			string translationsDir = GetTranslationsDirectory(options);
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

			string baseLocale = GetBaseLocale();
			if (string.IsNullOrEmpty(baseLocale))
			{
				GD.PushError("string.IsNullOrEmpty(baseLocale)");
				return Error.InvalidData;
			}
			GD.Print("baseLanguage = " + baseLocale);

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

			translationFile = $"{translationsDir}/{fileName}.csv";

			// read the existing table, if it exists
			GodotUtility.ReadCsv(translationFile, out var existingHeaders, out var existingTable);

			// create a hashset of headers, starting with the entry's 'key'
			// each column after the first is a translated string
			const string keyHeader = "key";
			var headers = new HashSet<string>()
			{
				keyHeader,
				baseLocale
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
					{ baseLocale, stringTableEntry.Value.text }
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

					if (kvp.Key == baseLocale)
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
				return Error.Failed;
			}
			else
			{
				fs.UpdateFile(translationFile);
			}

			string globalPath = ProjectSettings.GlobalizePath(translationFile);
			string dir = Path.GetDirectoryName(globalPath);
			string baseFileName = Path.GetFileNameWithoutExtension(translationFile);
			string searchPattern = $"{baseFileName}.*.translation";

			var files = Directory.GetFiles(dir, searchPattern);
			if (files.Length == 0)
			{
				GD.PushError($"No translation files found; dir = {dir}, searchPattern = {searchPattern}");
				return Error.Failed;
			}

			var translations = new HashSet<string>(GodotEditorUtility.GetTranslationsSetting());

			bool changed = false;
			foreach (var file in files)
			{
				// NOTE: LocalizePath isn't working for some reason. It's just giving me back the
				//       global path. So, I'm using my own utility method. I should ask about this
				//       on Discord or Github and get a better answer. This works, but it's not ideal.
				// string localPath = ProjectSettings.LocalizePath(file);
				string localPath = GodotUtility.LocalizePath(file);
				if (translations.Add(localPath))
				{
					GD.Print($"Add translation: " + localPath);
					changed = true;
				}
			}

			if (changed)
			{
				GodotEditorUtility.SetTranslations(translations.ToArray());
			}

			return Error.Ok;
		}

		protected virtual EditorFileSystem GetResourceFilesystem()
		{
			// this is weird, but it works. I'll keep looking for a better way to do this
			var dummyScript = new EditorScript();
			return dummyScript.GetEditorInterface()?.GetResourceFilesystem();
		}
	}
}

#endif

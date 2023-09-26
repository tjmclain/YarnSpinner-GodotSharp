#if TOOLS

using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Yarn.GodotSharp.Editor.Importers;

using FileAccess = Godot.FileAccess;
using StringDict = System.Collections.Generic.Dictionary<string, string>;

[Tool]
public partial class YarnProgramTranslationImporter : EditorImportPlugin
{
	public static readonly string ImporterName = typeof(YarnProgramTranslationImporter).FullName;

	public override string _GetImporterName()
	{
		return ImporterName;
	}

	public override string _GetVisibleName()
	{
		return typeof(YarnProgramTranslationImporter).Name;
	}

	public override string[] _GetRecognizedExtensions()
	{
		return new string[] { "csv" };
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
		return 2f;
	}

	public override int _GetImportOrder()
	{
		// 0 is default, and we want this to run
		// before the YarnProject importer, which is a default Resource
		return -1;
	}

	public override Error _Import(
			string sourceFile,
			string savePath,
			Dictionary options,
			Array<string> platformVariants,
			Array<string> genFiles
		)
	{
		return Error.Ok;

		//static string GetTranslationsDirectory(Dictionary importOptions)
		//{
		//	if (importOptions.TryGetValue(YarnProgramImporter.OverrideTranslationsDirOption, out var value))
		//	{
		//		string dir = value.AsString();
		//		if (!string.IsNullOrEmpty(dir))
		//		{
		//			return dir;
		//		}
		//	}

		//	var translationsDirSetting = ProjectSettings.GetSetting(Plugin.TranslationsDirProperty);
		//	return translationsDirSetting.AsString();
		//}

		//static string GetBaseLocale()
		//{
		//	var baseLocaleSetting = ProjectSettings.GetSetting(Plugin.BaseLocaleProperty);
		//	return baseLocaleSetting.AsString();
		//}

		//var error = YarnProgram.Compile(sourceFile, out var compilationResult);
		//if (error != Error.Ok)
		//{
		//	GD.PushError($"!yarnProgram.SetSourceFile '{sourceFile}'");
		//	return string.Empty;
		//}

		//var stringTable = compilationResult.StringTable;
		//if (!stringTable.Any())
		//{
		//	GD.PushError("!stringTableEntries.Any == 0");
		//	return string.Empty;
		//}

		//string dirName = GetTranslationsDirectory(options);
		//if (string.IsNullOrEmpty(dirName))
		//{
		//	GD.PushError("string.IsNullOrEmpty(translationsDir)");
		//	return string.Empty;
		//}

		//// remove trailing slash for consistency
		//if (dirName.EndsWith("/"))
		//{
		//	dirName = dirName.Left(dirName.Length - 1);
		//}

		//GD.Print("translationsDir = " + dirName);

		//string baseLocale = GetBaseLocale();
		//if (string.IsNullOrEmpty(baseLocale))
		//{
		//	GD.PushError("string.IsNullOrEmpty(baseLocale)");
		//	return string.Empty;
		//}
		//GD.Print("baseLanguage = " + baseLocale);

		//string fileName;
		//try
		//{
		//	fileName = Path.GetFileNameWithoutExtension(sourceFile);
		//}
		//catch (Exception ex)
		//{
		//	GD.PushError(ex);
		//	return string.Empty;
		//}

		//if (string.IsNullOrEmpty(fileName))
		//{
		//	GD.PushError("string.IsNullOrEmpty(fileName)");
		//	return string.Empty;
		//}

		//if (!DirAccess.DirExistsAbsolute(dirName))
		//{
		//	var makeDirErr = DirAccess.MakeDirAbsolute(dirName);
		//	if (makeDirErr != Error.Ok)
		//	{
		//		GD.PushError($"!DirAccess.MakeDirAbsolute ({dirName}); error = {makeDirErr}");
		//		return string.Empty;
		//	}
		//}

		//string translationFile = $"{dirName}/{fileName}.csv";

		//// read the existing table, if it exists
		//GodotUtility.ReadCsv(translationFile, out var existingHeaders, out var existingTable);

		//// create a hashset of headers, starting with the entry's 'key'
		//// each column after the first is a translated string
		//const string keyHeader = "key";
		//var headers = new HashSet<string>()
		//	{
		//		keyHeader,
		//		baseLocale
		//	};

		//foreach (var header in existingHeaders)
		//{
		//	headers.Add(header);
		//}

		//// create a table of entries from our compiled string table
		//var table = new System.Collections.Generic.Dictionary<string, StringDict>();
		//foreach (var stringTableEntry in stringTable)
		//{
		//	string key = stringTableEntry.Key;
		//	var row = new StringDict()
		//		{
		//			{ keyHeader, key },
		//			{ baseLocale, stringTableEntry.Value.text }
		//		};
		//	table[key] = row;

		//	if (!existingTable.TryGetValue(key, out var otherRow))
		//	{
		//		continue;
		//	}

		//	// add existing translations to our table entries
		//	foreach (var kvp in otherRow)
		//	{
		//		if (kvp.Key == keyHeader)
		//		{
		//			continue;
		//		}

		//		if (kvp.Key == baseLocale)
		//		{
		//			continue;
		//		}

		//		row[kvp.Key] = kvp.Value;
		//	}
		//}

		//// Write translations from string entries
		//using (var file = FileAccess.Open(translationFile, FileAccess.ModeFlags.Write))
		//{
		//	if (file == null)
		//	{
		//		GD.PushError($"!FileAccess.Open '{translationFile}'; error = {FileAccess.GetOpenError()}");
		//		return string.Empty;
		//	}

		//	// Write header row
		//	var headerRow = headers.ToArray();
		//	file.StoreCsvLine(headerRow);

		//	// Read rows from entries
		//	foreach (var row in table.Values)
		//	{
		//		string[] line = new string[headerRow.Length];
		//		for (int i = 0; i < headerRow.Length; i++)
		//		{
		//			string key = headerRow[i];
		//			row.TryGetValue(key, out line[i]);
		//		}
		//		file.StoreCsvLine(line);
		//	}

		//	GD.Print($"Exported translation at '{translationFile}'");
		//}

		//// NOTE: if I don't include this, AppendImportExternalResource
		//// below returns a 'FileNotFound' error
		//var fs = GetEditorInterface()?.GetResourceFilesystem();
		//if (fs == null)
		//{
		//	GD.PushWarning("GodotEditorUtility.GetResourceFilesystem() == null");
		//	return string.Empty;
		//}
		//else
		//{
		//	fs.UpdateFile(translationFile);
		//}

		//string globalPath = ProjectSettings.GlobalizePath(translationFile);
		//string dir = Path.GetDirectoryName(globalPath);
		//string baseFileName = Path.GetFileNameWithoutExtension(translationFile);
		//string searchPattern = $"{baseFileName}.*.translation";

		//var files = Directory.GetFiles(dir, searchPattern);
		//if (files.Length == 0)
		//{
		//	GD.PushError($"No translation files found; dir = {dir}, searchPattern = {searchPattern}");
		//	return string.Empty;
		//}

		//var translations = new HashSet<string>(GodotEditorUtility.GetTranslationsSetting());

		//bool changed = false;
		//foreach (var file in files)
		//{
		//	// NOTE: LocalizePath isn't working for some reason. It's just giving me back the
		//	//       global path. So, I'm using my own utility method. I should ask about this
		//	//       on Discord or Github and get a better answer. This works, but it's not ideal.
		//	// string localPath = ProjectSettings.LocalizePath(file);
		//	string localPath = GodotUtility.LocalizePath(file);
		//	if (translations.Add(localPath))
		//	{
		//		GD.Print($"Add translation: " + localPath);
		//		changed = true;
		//	}
		//}

		//if (changed)
		//{
		//	GodotEditorUtility.SetTranslations(translations.ToArray());
		//}

		//// TODO;
		//return sourceFile;
	}
}

#endif

#if TOOLS

using Godot;
using Godot.Collections;

namespace Yarn.GodotSharp.Editor.Importers;

[Tool]
public partial class YarnStringTableImporter : EditorImportPlugin
{
	public static class OptionNames
	{
		public static readonly string ExportGodotTranslations = "export_godot_translations";
	}

	public static readonly string ImporterName = typeof(YarnStringTableImporter).FullName.ToLower();

	public override string _GetImporterName()
	{
		return ImporterName;
	}

	public override string _GetVisibleName()
	{
		return "Yarn String Table";
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
		return 0f;
	}

	public override int _GetImportOrder()
	{
		return 0;
	}

	public override Array<Dictionary> _GetImportOptions(string path, int presetIndex)
	{
		return new Array<Dictionary>()
		{
			new Dictionary()
			{
				{ GodotEditorPropertyInfo.NameKey, OptionNames.ExportGodotTranslations },
				{ GodotEditorPropertyInfo.TypeKey, Variant.From(Variant.Type.Bool) },
				{ GodotEditorPropertyInfo.DefaultValueKey, Variant.From(false) }
			}
		};
	}

	public override bool _GetOptionVisibility(string path, StringName optionName, Dictionary options)
	{
		// options always visible
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
		var fileError = GodotUtility.ReadCsv(sourceFile, out var headers, out var data);
		if (fileError != Error.Ok)
		{
			return fileError;
		}

		var stringTable = new StringTable();
		stringTable.CsvHeaders = headers;

		foreach (var row in data)
		{
			var entry = new StringTableEntry(row);
			stringTable[entry.Id] = entry;
		}

		options.TryGetValue(OptionNames.ExportGodotTranslations, out var exportGodotTranslations);
		if (exportGodotTranslations.AsBool())
		{
			// TODO;
		}

		stringTable.UseGodotTranslations = exportGodotTranslations.AsBool();

		string saveFile = $"{savePath}.{_GetSaveExtension()}";
		return ResourceSaver.Save(stringTable, saveFile);
	}
}

#endif

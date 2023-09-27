#if TOOLS

using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Yarn.GodotSharp.Editor.Importers;

using FileAccess = Godot.FileAccess;
using StringDict = System.Collections.Generic.Dictionary<string, string>;

[Tool]
public partial class YarnLocalizationImporter : EditorImportPlugin
{
	public static readonly string ImporterName = typeof(YarnLocalizationImporter).FullName.ToLower();

	public override string _GetImporterName()
	{
		return ImporterName;
	}

	public override string _GetVisibleName()
	{
		return "Yarn Localization";
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
				{ GodotEditorPropertyInfo.NameKey, "export_godot_translations" },
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
		var fileError = GodotUtility.ReadCsv(sourceFile, out var headers, out var table);
		if (fileError != Error.Ok)
		{
			return fileError;
		}

		var stringTable = new StringTable();
		foreach (var kvp in table)
		{
			var entry = new StringTableEntry(kvp.Value);
			stringTable.Entries[kvp.Key] = entry;
		}

		string saveFile = $"{savePath}.{_GetSaveExtension()}";
		return ResourceSaver.Save(stringTable, saveFile);
	}
}

#endif

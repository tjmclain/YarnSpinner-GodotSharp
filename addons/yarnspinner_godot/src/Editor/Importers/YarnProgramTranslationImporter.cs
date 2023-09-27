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
public partial class YarnLocalizationImporter : EditorImportPlugin
{
	public static readonly string ImporterName = "yarn_localization"; //typeof(YarnLocalizationImporter).FullName.ToLower();
	public const string VisibleName = "Yarn Localization";

	public override string _GetImporterName()
	{
		return ImporterName;
	}

	public override string _GetVisibleName()
	{
		return VisibleName;
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

	public override Array<Dictionary> _GetImportOptions(string path, int presetIndex)
	{
		return new Array<Dictionary>();
	}

	public override Error _Import(
			string sourceFile,
			string savePath,
			Dictionary options,
			Array<string> platformVariants,
			Array<string> genFiles
		)
	{
		var stringTable = new StringTable();
		return ResourceSaver.Save(stringTable, savePath);
	}
}

#endif

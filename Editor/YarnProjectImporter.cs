#if TOOLS
using Godot;
using Godot.Collections;

namespace Yarn.Godot.Editor
{
	[Tool]
	public partial class YarnProjectImporter : EditorImportPlugin
	{
		public override string _GetImporterName()
		{
			return typeof(YarnProjectImporter).FullName;
		}

		public override string _GetVisibleName()
		{
			return typeof(YarnProjectImporter).Name;
		}

		public override string[] _GetRecognizedExtensions()
		{
			return new string[] { "yarnproject" };
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

		public override Error _Import(
			string sourceFile, 
			string savePath, 
			Dictionary options, 
			Array<string> platformVariants, 
			Array<string> genFiles
		)
		{
			using var file = FileAccess.Open(sourceFile, FileAccess.ModeFlags.Read);
			if (file.GetError() != Error.Ok)
			{
				return Error.Failed;
			}

			var yarnProject = new YarnProject();
			// TODO

			string filename = $"{savePath}.{_GetSaveExtension()}";
			return ResourceSaver.Save(yarnProject, filename);

		}
	}
}
#endif

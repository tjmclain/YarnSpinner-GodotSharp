#if TOOLS
using Godot;
using System;
using Yarn.GodotEngine.Editor.Importers;

namespace Yarn.GodotEngine
{
	[Tool]
	public partial class Plugin : EditorPlugin
	{
		private YarnProgramImporter _yarnProgramImporter;

		public override void _EnterTree()
		{
			// Initialization of the plugin goes here.
			GD.Print("_EnterTree");

			_yarnProgramImporter = new YarnProgramImporter();
			AddImportPlugin(_yarnProgramImporter);
		}

		public override void _ExitTree()
		{
			// Clean-up of the plugin goes here.
			GD.Print("_ExitTree");

			RemoveImportPlugin(_yarnProgramImporter);
		}
	}
}
#endif

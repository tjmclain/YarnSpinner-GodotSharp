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
			GD.Print("Yarn.GodotEngine.Plugin: _EnterTree");

			// Initialize custom project settings
			var editorInterface = GetEditorInterface();
			Editor.EditorSettings.AddProperties(editorInterface);

			// Initialize custom importers
			_yarnProgramImporter = new YarnProgramImporter();
			AddImportPlugin(_yarnProgramImporter);
		}

		public override void _ExitTree()
		{
			// Clean-up of the plugin goes here.
			GD.Print("Yarn.GodotEngine.Plugin: _ExitTree");

			// Deinitiliaze custom project settings
			var editorInterface = GetEditorInterface();
			Editor.EditorSettings.RemoveProperties(editorInterface);

			// Deinitialize custom importers
			RemoveImportPlugin(_yarnProgramImporter);
		}
	}
}
#endif

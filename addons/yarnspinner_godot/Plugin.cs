#if TOOLS
using Godot;
using System;
using Yarn.GodotEngine.Editor.Importers;
using Yarn.GodotEngine.Editor.Tests;

namespace Yarn.GodotEngine
{
	[Tool]
	public partial class Plugin : EditorPlugin
	{
		// importers
		private YarnProgramImporter _yarnProgramImporter;

		// commands
		private ProjectSettingsTranslationTest _translationTestsCommand;

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

			// Initialize commands
			_translationTestsCommand = new ProjectSettingsTranslationTest();
			_translationTestsCommand.AddToCommandPalette();
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

			// Deinitialize
			_translationTestsCommand?.RemoveFromCommandPalette();
		}
	}
}
#endif

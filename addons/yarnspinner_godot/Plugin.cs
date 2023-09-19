#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using Yarn.GodotEngine.Editor;
using Yarn.GodotEngine.Editor.Importers;
using Yarn.GodotEngine.Editor.Inspectors;
using Yarn.GodotEngine.Editor.Tests;

namespace Yarn.GodotEngine
{
	[Tool]
	public partial class Plugin : EditorPlugin
	{
		private readonly List<EditorImportPlugin> _importPlugins = new();
		private readonly List<EditorInspectorPlugin> _inspectorPlugins = new();
		private readonly List<CommandEditorScript> _commandEditorScripts = new();

		public override void _EnterTree()
		{
			// Initialization of the plugin goes here.
			GD.Print("Yarn.GodotEngine.Plugin: _EnterTree");

			// Initialize custom project settings
			var editorInterface = GetEditorInterface();
			Editor.EditorSettings.AddProperties(editorInterface);

			// Initialize custom importers
			_importPlugins.Clear();
			_importPlugins.Add(new YarnProgramImporter());

			foreach(var plugin in _importPlugins)
			{
				AddImportPlugin(plugin);
			}

			// Initialize custom inspectors
			_inspectorPlugins.Clear();
			_inspectorPlugins.Add(new ActionLibraryInspector());
			foreach (var plugin in _inspectorPlugins)
			{
				AddInspectorPlugin(plugin);
			}

			// Initialize command scripts
			_commandEditorScripts.Clear();
			_commandEditorScripts.Add(new ProjectSettingsTranslationTest());
			foreach(var script in _commandEditorScripts)
			{
				script.AddToCommandPalette();
			}			
		}

		public override void _ExitTree()
		{
			// Clean-up of the plugin goes here.
			GD.Print("Yarn.GodotEngine.Plugin: _ExitTree");

			// Deinitiliaze custom project settings
			var editorInterface = GetEditorInterface();
			Editor.EditorSettings.RemoveProperties(editorInterface);

			// Deinitialize custom importers
			foreach (var plugin in _importPlugins)
			{
				RemoveImportPlugin(plugin);
			}

			// Deinitialize custom inspectors
			foreach (var plugin in _inspectorPlugins)
			{
				RemoveInspectorPlugin(plugin);
			}

			// Deinitialize command scripts
			foreach (var script in _commandEditorScripts)
			{
				script.RemoveFromCommandPalette();
			}
		}
	}
}
#endif

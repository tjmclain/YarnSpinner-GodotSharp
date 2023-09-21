#if TOOLS

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Yarn.GodotSharp.Editor;
using Yarn.GodotSharp.Editor.Importers;
using Yarn.GodotSharp.Editor.Inspectors;
using Yarn.GodotSharp.Editor.Tests;

namespace Yarn.GodotSharp
{
	[Tool]
	public partial class Plugin : EditorPlugin
	{
		private List<EditorImportPlugin> _importPlugins = new();
		private List<EditorInspectorPlugin> _inspectorPlugins = new();
		private List<CommandEditorScript> _commandEditorScripts = new();

		public override void _EnterTree()
		{
			// Initialization of the plugin goes here.
			GD.Print("Yarn.GodotEngine.Plugin: _EnterTree");

			// Initialize custom importers
			_importPlugins = GetInstancesOfEditorTypes<EditorImportPlugin>();
			foreach (var plugin in _importPlugins)
			{
				AddImportPlugin(plugin);
			}

			// Initialize custom inspectors
			_inspectorPlugins = GetInstancesOfEditorTypes<EditorInspectorPlugin>();
			foreach (var plugin in _inspectorPlugins)
			{
				AddInspectorPlugin(plugin);
			}

			// Initialize command scripts
			_commandEditorScripts = GetInstancesOfEditorTypes<CommandEditorScript>();
			foreach (var script in _commandEditorScripts)
			{
				script.AddToCommandPalette();
			}
		}

		public override void _ExitTree()
		{
			// Clean-up of the plugin goes here.
			GD.Print("Yarn.GodotEngine.Plugin: _ExitTree");

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

		private static List<T> GetInstancesOfEditorTypes<T>()
		{
			return Assembly.GetExecutingAssembly().GetTypes()
				.Where(x => !x.IsAbstract)
				.Where(x => x.Namespace.Contains(nameof(Editor)))
				.Where(x => typeof(T).IsAssignableFrom(x))
				.Select(x => (T)Activator.CreateInstance(x))
				.ToList();
		}
	}
}

#endif

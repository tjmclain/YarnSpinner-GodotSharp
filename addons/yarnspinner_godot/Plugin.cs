#if TOOLS

using Godot;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

namespace Yarn.GodotSharp.Editor;

[Tool]
public partial class Plugin : EditorPlugin
{
	private List<EditorImportPlugin> _importPlugins = new();
	private List<EditorInspectorPlugin> _inspectorPlugins = new();
	private List<EditorScript> _editorScripts = new();

	public override void _EnterTree()
	{
		// Initialization of the plugin goes here.
		//GD.Print("Yarn.GodotSharp.Plugin: _EnterTree - BEGIN");

		// Initialize custom importers
		_importPlugins = GetInstancesOfEditorTypes<EditorImportPlugin>().ToList();
		foreach (var importer in _importPlugins)
		{
			if (importer == null)
			{
				//GD.PushError("importer == null");
				continue;
			}
			AddImportPlugin(importer);
		}
		//GD.Print($"- added {_importPlugins.Count} {nameof(EditorImportPlugin)}(s)");

		// Initialize custom inspectors
		_inspectorPlugins = GetInstancesOfEditorTypes<EditorInspectorPlugin>().ToList();
		foreach (var inspector in _inspectorPlugins)
		{
			if (inspector == null)
			{
				GD.PushError("inspector == null");
				continue;
			}

			AddInspectorPlugin(inspector);
		}
		//GD.Print($"- added {_inspectorPlugins.Count} {nameof(EditorInspectorPlugin)}(s)");

		// Initialize editor nodes
		_editorScripts = GetInstancesOfEditorTypes<EditorScript>().ToList();
		//GD.Print($"- added {_editorScripts.Count} {nameof(Godot.Node)}(s)");

		//GD.Print("Yarn.GodotSharp.Plugin: _EnterTree - END");
	}

	public override void _ExitTree()
	{
		// Clean-up of the plugin goes here.
		//GD.Print("Yarn.GodotEngine.Plugin: _ExitTree - BEGIN");

		// Deinitialize custom importers
		foreach (var importer in _importPlugins)
		{
			if (importer == null)
			{
				//GD.PushError("importer == null");
				continue;
			}
			RemoveImportPlugin(importer);
			importer.Dispose();
		}
		_importPlugins.Clear();

		// Deinitialize custom inspectors
		foreach (var inspector in _inspectorPlugins)
		{
			if (inspector == null)
			{
				//GD.PushError("script == null");
				return;
			}
			RemoveInspectorPlugin(inspector);
			inspector.Dispose();
		}
		_inspectorPlugins.Clear();

		// Deinitialize editor nodes
		foreach (var script in _editorScripts)
		{
			if (script == null)
			{
				//GD.PushError("script == null");
				return;
			}
			script.Dispose();
		}
		_editorScripts.Clear();

		//GD.Print("Yarn.GodotEngine.Plugin: _ExitTree - END");
	}

	private static IEnumerable<T> GetInstancesOfEditorTypes<T>()
	{
		const string editorNamespace = "Yarn.GodotSharp.Editor";

		try
		{
			return Assembly.GetExecutingAssembly().GetTypes()
				.Where(x => !x.IsAbstract)
				.Where(x => !typeof(EditorPlugin).IsAssignableFrom(x)) // exclude the plugin script in our search
				.Where(x => x.Namespace.Contains(editorNamespace))
				.Where(x => typeof(T).IsAssignableFrom(x))
				.Select(x => (T)Activator.CreateInstance(x));
		}
		catch (Exception ex)
		{
			GD.PushError(ex);
			return Enumerable.Empty<T>();
		}
	}
}

#endif

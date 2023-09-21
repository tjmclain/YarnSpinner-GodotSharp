#if TOOLS

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Yarn.GodotSharp;

using GodotNode = Godot.Node;

[Tool]
public partial class Plugin : EditorPlugin
{
	private List<EditorImportPlugin> _importPlugins = new();
	private List<EditorInspectorPlugin> _inspectorPlugins = new();
	private List<GodotNode> _editorNodes = new();

	public override void _EnterTree()
	{
		// Initialization of the plugin goes here.
		GD.Print("Yarn.GodotSharp.Plugin: _EnterTree");

		// Initialize custom importers
		_importPlugins = GetInstancesOfEditorTypes<EditorImportPlugin>().ToList();
		foreach (var plugin in _importPlugins)
		{
			AddImportPlugin(plugin);
		}
		GD.Print($"- added {_importPlugins.Count} {nameof(EditorImportPlugin)}(s)");

		// Initialize custom inspectors
		_inspectorPlugins = GetInstancesOfEditorTypes<EditorInspectorPlugin>().ToList();
		foreach (var plugin in _inspectorPlugins)
		{
			AddInspectorPlugin(plugin);
		}
		GD.Print($"- added {_inspectorPlugins.Count} {nameof(EditorInspectorPlugin)}(s)");

		// Initialize editor nodes
		_editorNodes = GetInstancesOfEditorTypes<GodotNode>().ToList();
		var mainControl = GetEditorInterface()?.GetBaseControl();
		foreach (var node in _editorNodes)
		{
			mainControl?.AddChild(node);
		}
		GD.Print($"- added {_editorNodes.Count} {nameof(Godot.Node)}(s)");
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
		_importPlugins.Clear();

		// Deinitialize custom inspectors
		foreach (var plugin in _inspectorPlugins)
		{
			RemoveInspectorPlugin(plugin);
		}
		_inspectorPlugins.Clear();

		// Deinitialize editor nodes
		var mainControl = GetEditorInterface()?.GetBaseControl();
		foreach (var node in _editorNodes)
		{
			mainControl?.RemoveChild(node);
		}
		_editorNodes.Clear();
	}

	private static IEnumerable<T> GetInstancesOfEditorTypes<T>()
	{
		const string editorNamespace = "Yarn.GodotSharp.Editor";

		return Assembly.GetExecutingAssembly().GetTypes()
			.Where(x => !x.IsAbstract)
			.Where(x => x.Namespace.Contains(editorNamespace))
			.Where(x => typeof(T).IsAssignableFrom(x))
			.Select(x => (T)Activator.CreateInstance(x));
	}
}

#endif

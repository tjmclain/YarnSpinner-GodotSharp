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
	private IEnumerable<EditorImportPlugin> _importPlugins;
	private IEnumerable<EditorInspectorPlugin> _inspectorPlugins;
	private IEnumerable<CommandPaletteScript> _commandScripts;

	// IMPORTANT! Be careful of using GD.Print in _EnterTree and _ExitTree
	// In this version, GD.Print will crash the editor when reloading a project
	// https://github.com/godotengine/godot/issues/74549
	public override void _EnterTree()
	{
		// Initialize custom importers
		_importPlugins = GetInstancesOfEditorTypes<EditorImportPlugin>();
		if (_importPlugins != null)
		{
			foreach (var importer in _importPlugins)
			{
				if (importer == null)
				{
					continue;
				}
				AddImportPlugin(importer);
			}
		}

		// Initialize custom inspectors
		_inspectorPlugins = GetInstancesOfEditorTypes<EditorInspectorPlugin>();
		if (_inspectorPlugins != null)
		{
			foreach (var inspector in _inspectorPlugins)
			{
				if (inspector == null)
				{
					continue;
				}

				AddInspectorPlugin(inspector);
			}
		}

		// Initialize editor nodes
		_commandScripts = GetInstancesOfEditorTypes<CommandPaletteScript>().ToList();
		if (_commandScripts != null)
		{
			foreach (var script in _commandScripts)
			{
				if (script == null)
				{
					continue;
				}
				script.RegisterCommand();
			}
		}
	}

	// IMPORTANT! Be careful of using GD.Print in _EnterTree and _ExitTree
	// In this version, GD.Print will crash the editor when reloading a project
	// https://github.com/godotengine/godot/issues/74549
	public override void _ExitTree()
	{
		// Deinitialize custom importers
		if (_importPlugins != null)
		{
			foreach (var importer in _importPlugins)
			{
				if (importer == null)
				{
					continue;
				}
				RemoveImportPlugin(importer);
			}
			_importPlugins = null;
		}

		// Deinitialize custom inspectors
		if (_inspectorPlugins != null)
		{
			foreach (var inspector in _inspectorPlugins)
			{
				if (inspector == null)
				{
					continue;
				}
				RemoveInspectorPlugin(inspector);
			}
			_inspectorPlugins = null;
		}

		// Deinitialize editor nodes
		if (_commandScripts != null)
		{
			foreach (var script in _commandScripts)
			{
				if (script == null)
				{
					continue;
				}
				script.UnregisterCommand();
			}
			_commandScripts = null;
		}
	}

	public override void _EnablePlugin()
	{
		SetTextFileExtensionsSetting(true);
	}

	public override void _DisablePlugin()
	{
		SetTextFileExtensionsSetting(false);
	}

	private static IEnumerable<T> GetInstancesOfEditorTypes<T>()
	{
		return Assembly.GetExecutingAssembly().GetTypes()
			.Where(x => !x.IsAbstract)
			.Where(x => x.Namespace.Contains("Yarn.GodotSharp.Editor"))
			.Where(x => typeof(T).IsAssignableFrom(x))
			.Select(x => (T)Activator.CreateInstance(x));
	}

	private void SetTextFileExtensionsSetting(bool addYarnExtension)
	{
		var editorSettings = GetEditorInterface()?.GetEditorSettings();
		if (editorSettings == null)
		{
			return;
		}

		const string textFileExtensionsKey = "docks/filesystem/textfile_extensions";
		const string yarnExt = "yarn";
		const string delim = ",";

		var textFileExtensions = editorSettings.Get(textFileExtensionsKey);
		var list = textFileExtensions.AsString().Split(delim).ToHashSet();
		if (addYarnExtension)
		{
			list.Add(yarnExt);
		}
		else
		{
			list.Remove(yarnExt);
		}

		editorSettings.Set(textFileExtensionsKey, string.Join(delim, list));
	}
}

#endif

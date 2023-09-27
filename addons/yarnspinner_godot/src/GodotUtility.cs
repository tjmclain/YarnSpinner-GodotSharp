using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using GodotNode = Godot.Node;
using Path = System.IO.Path;

namespace Yarn.GodotSharp;

public static class GodotUtility
{
	public static SceneTree GetSceneTree()
	{
		if (Engine.GetMainLoop() is not SceneTree sceneTree)
		{
			throw new ArgumentException("Engine.GetMainLoop() is not SceneTree sceneTree");
		}

		return sceneTree;
	}

	public static GodotNode GetCurrentScene()
	{
		var sceneTree = GetSceneTree();
		var currentScene = sceneTree.CurrentScene;
		if (currentScene == null)
		{
			GD.PrintErr("currentScene == null");
			return null;
		}

		return currentScene;
	}

	public static GodotNode GetNode(NodePath path)
	{
		var currentScene = GetCurrentScene();
		return currentScene?.GetNode(path);
	}

	public static T GetNode<T>(NodePath path) where T : GodotNode
	{
		var currentScene = GetCurrentScene();
		return currentScene?.GetNode<T>(path);
	}

	public static GodotNode GetChild(GodotNode node, Type type)
	{
		if (node == null)
		{
			GD.PrintErr("node == null");
			return null;
		}

		int count = node.GetChildCount();
		for (int i = 0; i < count; i++)
		{
			var child = node.GetChild(i);
			if (type.IsAssignableFrom(child.GetType()))
			{
				return child;
			}
		}

		return null;
	}

	// NOTE (2023-09-18): ProjectSettings.LocalizePath was not working for me. I would pass in a
	// valid global path to an asset under the project directory, but I would get the global path
	// back, not a 'res://' relative path. So, for now, I have to do the replacement manually.
	public static string LocalizePath(string globalPath)
	{
		string projectRoot = ProjectSettings.GlobalizePath("res://");
		return globalPath
			.Replace('\\', '/')
			.Replace(projectRoot, "res://");
	}

	public static string GetLocalDirectoryName(string path)
	{
		string globalPath = ProjectSettings.GlobalizePath(path);
		string dir = Path.GetDirectoryName(globalPath);
		return LocalizePath(dir);
	}

	public static bool TryTranslateString(StringName key, out StringName value)
		=> TryTranslateString(key, null, out value);

	public static bool TryTranslateString(StringName key, string context, out StringName value)
	{
		value = TranslationServer.Translate(key, context);
		return !string.IsNullOrEmpty(value) && value != key;
	}

	public static Error ReadCsv(string csvFile, out string[] headers, out Dictionary<string, Dictionary<string, string>> table)
	{
		// don't out null values
		headers = Array.Empty<string>();
		table = new();

		if (!FileAccess.FileExists(csvFile))
		{
			return Error.FileNotFound;
		}

		using var file = FileAccess.Open(csvFile, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			var error = FileAccess.GetOpenError();
			GD.PushError($"file == null; csvFile = {csvFile}; error = {error}");
			return error;
		}

		if (file.GetLength() == 0)
		{
			GD.PushError($"file.GetLength() == 0; csvFile = {csvFile}");
			return Error.InvalidData;
		}

		headers = file.GetCsvLine();

		while (file.GetPosition() < file.GetLength())
		{
			var values = new Dictionary<string, string>();
			string[] line = file.GetCsvLine();
			if (line == null || line.Length == 0)
			{
				continue;
			}

			string id = line[0];
			if (string.IsNullOrEmpty(id))
			{
				continue;
			}

			for (int i = 0; i < headers.Length; i++)
			{
				string key = headers[i];
				values[key] = line[i];
			}

			table[id] = values;
		}

		return Error.Ok;
	}
}

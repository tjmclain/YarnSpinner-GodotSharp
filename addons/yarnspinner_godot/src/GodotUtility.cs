using System;
using System.Collections.Generic;
using Godot;
using System.Threading.Tasks;

namespace Yarn.GodotSharp;

using GodotNode = Godot.Node;

public static class GodotUtility
{
	public static SceneTree GetSceneTree()
	{
		if (Engine.GetMainLoop() is not SceneTree sceneTree)
		{
			GD.PushError("GodotUtility.GetSceneTree: Engine.GetMainLoop() is not SceneTree");
			return null;
		}

		return sceneTree;
	}

	public static GodotNode GetCurrentScene()
	{
		var currentScene = GetSceneTree()?.CurrentScene;
		if (currentScene == null)
		{
			GD.PrintErr("GodotUtility.GetCurrentScene: currentScene == null");
			return null;
		}

		return currentScene;
	}

	public static GodotNode GetNode(NodePath path)
	{
		return GetCurrentScene()?.GetNode(path);
	}

	public static T GetNode<T>(NodePath path) where T : GodotNode
	{
		return GetCurrentScene()?.GetNode<T>(path);
	}

	public static GodotNode GetChild<T>(GodotNode node) where T : GodotNode
	{
		if (node == null)
		{
			GD.PushError("node == null");
			return default;
		}

		int count = node.GetChildCount();
		for (int i = 0; i < count; i++)
		{
			var child = node.GetChild(i);
			if (child is T result)
			{
				return result;
			}
		}

		return default;
	}

	public static async Task WaitForProcessFrame()
	{
		var sceneTree = GetSceneTree();
		if (sceneTree == null)
		{
			GD.PushError("GodotUtility.WaitForProcessFrame: sceneTree == null");
			return;
		}

		await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
	}

	public static Error ReadCsv(string csvFile, out string[] headers, out List<Dictionary<string, string>> data)
	{
		// don't out null values
		headers = Array.Empty<string>();
		data = new();

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

			data.Add(values);
		}

		return Error.Ok;
	}
}
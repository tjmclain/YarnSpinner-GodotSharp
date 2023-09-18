using System;
using System.Text;
using Godot;

using GodotNode = Godot.Node;

namespace Yarn.GodotEngine
{
	public static class GodotUtility
	{
		private const char NullChar = '\0';

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
		// valid global path to an asset under the project directory, but I would get the global
		// path back, not a 'res://' relative path. So, for now, I have to do the replacement manually.
		public static string LocalizePath(string globalPath)
		{
			string projectRoot = ProjectSettings.GlobalizePath("res://");
			return globalPath
				.Replace('\\', '/')
				.Replace(projectRoot, "res://");
		}

		// Godot prefers snake case names, but C# uses pascal and camel case names
		public static string CSharpNameToGodotName(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				GD.PushError("string.IsNullOrEmpty(value)");
				return string.Empty;
			}

			char prev = NullChar;

			var sb = new StringBuilder();
			foreach (char c in value)
			{
				if (char.IsUpper(c))
				{
					// insert underscore before uppercase characters
					if (prev != NullChar && prev != '.')
					{
						sb.Append('_');
					}

					prev = char.ToLower(c);
					sb.Append(prev);
					continue;
				}

				if (c == '.')
				{
					// dots (e.g. in namespaces) should become slashes (e.g. paths) this delimiter
					// marks the start of a new word, so reset our 'startOfWord' flag
					prev = '/';
					sb.Append(prev);
					continue;
				}

				prev = c;
				sb.Append(prev);
			}

			string result = sb.ToString();
			GD.Print($"{value} -> {result}");
			return result;
		}

		public static string VariableNameToFriendlyName(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				GD.PushError("string.IsNullOrEmpty(value)");
				return string.Empty;
			}

			char prev = NullChar;
			var sb = new StringBuilder();
			foreach (char c in value)
			{
				if (char.IsLower(c))
				{
					prev = (prev == NullChar || prev == ' ')
						? char.ToUpper(c)
						: char.ToLower(c);

					sb.Append(prev);
					continue;
				}

				if (c == '_')
				{
					if (prev != NullChar && prev != ' ')
					{
						prev = ' ';
						sb.Append(prev);
					}
					continue;
				}

				if (char.IsUpper(c))
				{
					if (prev != NullChar && !char.IsUpper(prev))
					{
						sb.Append(' ');
					}
				}

				prev = c;
				sb.Append(prev);
			}

			string result = sb.ToString();
			GD.Print($"{value} -> {result}");
			return result;
		}
	}
}

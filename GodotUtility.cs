using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

using GodotNode = Godot.Node;

namespace Yarn.Godot
{
	public static class GodotUtility
	{
		public static GodotNode GetCurrentScene()
		{
			if (Engine.GetMainLoop() is not SceneTree sceneTree)
			{
				GD.PrintErr("Engine.GetMainLoop() is not SceneTree sceneTree");
				return null;
			}

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
	}
}
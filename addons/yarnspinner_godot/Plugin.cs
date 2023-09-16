#if TOOLS
using Godot;
using System;

namespace Yarn.GodotEngine
{
	[Tool]
	public partial class Plugin : EditorPlugin
	{
		public override void _EnterTree()
		{
			// Initialization of the plugin goes here.
			GD.Print("_EnterTree");
		}

		public override void _ExitTree()
		{
			// Clean-up of the plugin goes here.
			GD.Print("_ExitTree");
		}
	}
}
#endif

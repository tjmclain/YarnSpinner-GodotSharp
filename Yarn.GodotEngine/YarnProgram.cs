using System;
using Godot;

namespace Yarn.GodotEngine
{
	[GlobalClass, Icon("res://addons/yarnspinner_godot/Icons/Asset Icons/YarnScript Icon.png")]
	public partial class YarnProgram : Resource
	{
		[Export]
		public Godot.Collections.Array<Variable> Declarations { get; set; } = new();

		[Export]
		public Godot.Collections.Array<StringTableEntry> StringTableEntries { get; set; } = new();

		[Export]
		public string[] Errors { get; set; } = Array.Empty<string>();
	}
}

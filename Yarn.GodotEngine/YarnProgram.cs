using System;
using Godot;

namespace Yarn.GodotEngine
{
	[GlobalClass, Icon("res://addons/yarnspinner_godot/icons/YarnScriptIcon.png")]
	public partial class YarnProgram : Resource
	{
		[Export]
		public string SourceFile { get; set; } = string.Empty;

		[Export]
		public string TranslationsFile { get; set; } = string.Empty;

		[Export]
		public Godot.Collections.Array<Variable> Declarations { get; set; } = new();

		[Export]
		public Godot.Collections.Array<StringTableEntry> StringTable { get; set; } = new();

		[Export]
		public string[] Errors { get; set; } = Array.Empty<string>();
	}
}

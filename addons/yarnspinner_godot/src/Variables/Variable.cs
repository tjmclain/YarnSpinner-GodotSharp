using Godot;

namespace Yarn.GodotSharp.Variables
{
	public partial class Variable : Resource
	{
		[Export]
		public string Name = "";

		[Export]
		public Variant.Type VariableType = Variant.Type.String;

		[Export]
		public Variant Value = "";
	}
}

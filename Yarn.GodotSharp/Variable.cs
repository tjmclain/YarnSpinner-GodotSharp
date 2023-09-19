using Godot;
using System;
using Yarn.Compiler;

namespace Yarn.GodotSharp
{
	public partial class Variable : Resource
	{
		[Export]
		public string Name = "";

		[Export]
		public VariableType Type = VariableType.String;

		[Export]
		public string Value = "";

		public static bool TryCreateFromDeclaration(
			Declaration declaration,
			out Variable variable
		)
		{
			static bool TryGetVariableType(Declaration declaration, out VariableType type)
			{
				string typeName = declaration.Type.Name;
				if (typeName == BuiltinTypes.String.Name)
				{
					type = VariableType.String;
					return true;
				}

				if (typeName == BuiltinTypes.Number.Name)
				{
					type = VariableType.Number;
					return true;
				}

				if (typeName == BuiltinTypes.Boolean.Name)
				{
					type = VariableType.Boolean;
					return true;
				}

				GD.PushError($"invalid variable type: {declaration.Name} = {typeName}");
				type = VariableType.Invalid;
				return false;
			}

			if (!TryGetVariableType(declaration, out VariableType type))
			{
				variable = default;
				return false;
			}

			variable = new Variable()
			{
				Name = declaration.Name,
				Type = type,
				Value = declaration.DefaultValue.ToString()
			};
			return true;
		}

		public float ToNumber()
		{
			return float.Parse(Value);
		}

		public bool ToBoolean()
		{
			return bool.Parse(Value);
		}
	}
}

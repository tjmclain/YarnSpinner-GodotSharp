using Godot;
using System;
using Yarn.Compiler;

namespace Yarn.GodotSharp.Variables
{
	public partial class Variable : Resource
	{
		public enum Type
		{
			Invalid = -1,
			String,
			Number,
			Boolean
		}

		[Export]
		public string Name = "";

		[Export]
		public Type VariableType = Type.String;

		[Export]
		public string Value = "";

		public static bool TryCreateFromDeclaration(Declaration declaration, out Variable variable)
		{
			static bool TryGetVariableType(Declaration declaration, out Type type)
			{
				string typeName = declaration.Type.Name;
				if (typeName == BuiltinTypes.String.Name)
				{
					type = Type.String;
					return true;
				}

				if (typeName == BuiltinTypes.Number.Name)
				{
					type = Type.Number;
					return true;
				}

				if (typeName == BuiltinTypes.Boolean.Name)
				{
					type = Type.Boolean;
					return true;
				}

				GD.PushError($"invalid variable type: {declaration.Name} = {typeName}");
				type = Type.Invalid;
				return false;
			}

			if (!TryGetVariableType(declaration, out Type type))
			{
				variable = default;
				return false;
			}

			variable = new Variable()
			{
				Name = declaration.Name,
				VariableType = type,
				Value = declaration.DefaultValue.ToString(),
			};
			return true;
		}
	}
}

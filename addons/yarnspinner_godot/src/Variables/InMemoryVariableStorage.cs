using System.Collections;
using Godot;
using System;

using GodotCollections = Godot.Collections;

namespace Yarn.GodotSharp.Variables
{
	[GlobalClass]
	public partial class InMemoryVariableStorage : VariableStorage
	{
		[Export]
		public GodotCollections.Dictionary<string, Variable> Variables = new();

		public void SetValue(string variableName, Variable.Type type, string value)
		{
			ValidateVariableName(variableName);

			if (!Variables.TryGetValue(variableName, out var variable))
			{
				variable = new Variable()
				{
					Name = variableName
				};
				Variables[variableName] = variable;
			}

			variable.VariableType = type;
			variable.Value = value;
		}

		#region VariableStorageBehaviour

		public override void SetValue(string variableName, string stringValue)
			=> SetValue(variableName, Variable.Type.String, stringValue);

		public override void SetValue(string variableName, float floatValue)
			=> SetValue(variableName, Variable.Type.Number, floatValue.ToString());

		public override void SetValue(string variableName, bool boolValue)
			=> SetValue(variableName, Variable.Type.Boolean, boolValue.ToString());

		public override bool TryGetValue<T>(string variableName, out T result)
		{
			static Type VariableTypeToSystemType(Variable.Type type)
			{
				return type switch
				{
					Variable.Type.String => typeof(string),
					Variable.Type.Number => typeof(float),
					Variable.Type.Boolean => typeof(bool),
					_ => throw new ArgumentException("invalid YarnVariableType; type = " + type),
				};
			}

			ValidateVariableName(variableName);

			if (!Variables.TryGetValue(variableName, out var variable))
			{
				GD.PushWarning("!_variables.TryGetValue; variableName = " + variableName);
				result = default;
				return false;
			}

			var variableType = VariableTypeToSystemType(variable.VariableType);
			if (!typeof(T).IsAssignableFrom(variableType))
			{
				throw new InvalidCastException($"Variable '{variableName}' is the wrong type; expected '{typeof(T)}', got '{variableType}'");
			}

			result = (T)Convert.ChangeType(variable.Value, typeof(T));
			return true;
		}

		public override void Clear()
		{
			Variables.Clear();
		}

		public override bool Contains(string variableName)
		{
			return Variables.ContainsKey(variableName);
		}

		#endregion VariableStorageBehaviour

		/// <summary>
		/// Throws a <see cref="System.ArgumentException"/> if <paramref name="variableName"/> is
		/// not a valid Yarn Spinner variable name.
		/// </summary>
		/// <param name="variableName">The variable name to test.</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown when <paramref name="variableName"/> is not a valid variable name.
		/// </exception>
		private static void ValidateVariableName(string variableName)
		{
			if (variableName.StartsWith("$") == false)
			{
				throw new ArgumentException($"{variableName} is not a valid variable name: Variable names must start with a '$'. (Did you mean to use '${variableName}'?)");
			}
		}
	}
}

using System.Collections;
using Godot;
using System;

using GodotCollections = Godot.Collections;

namespace Yarn.Godot
{
	/// <summary>
	/// A simple implementation of VariableStorageBehaviour.
	/// </summary>
	/// <remarks>
	/// <para>This class stores variables in memory, and is erased when the game
	/// exits.</para>
	///
	/// <para>This class also has basic serialization and save/load example functions.</para>
	///
	/// <para>You can also enumerate over the variables by using a <c>foreach</c>
	/// loop:</para>
	///
	/// <code lang="csharp">
	/// // 'storage' is an InMemoryVariableStorage
	/// foreach (var variable in storage) {
	///     string name = variable.Key;
	///     System.Object value = variable.Value;
	/// }
	/// </code>
	///
	/// <para>Note that as of v2.0, this class no longer uses Yarn.Value, to
	/// enforce static typing of declared variables within the Yarn
	/// Program.</para>
	/// </remarks>
	// https://yarnspinner.dev/docs/unity/components/variable-storage/
	public partial class InMemoryVariableStorage : VariableStorageBehaviour
	{
		[Export]
		public GodotCollections.Dictionary<string, YarnVariable> Variables = new();

		public void SetValue(string variableName, YarnVariableType type, string value)
		{
			ValidateVariableName(variableName);

			if (!Variables.TryGetValue(variableName, out var variable))
			{
				variable = new YarnVariable()
				{
					Name = variableName
				};
				Variables[variableName] = variable;
			}

			variable.Type = type;
			variable.Value = value;
		}

		#region VariableStorageBehaviour
		public override void SetValue(string variableName, string stringValue)
			=> SetValue(variableName, YarnVariableType.String, stringValue);

		public override void SetValue(string variableName, float floatValue)
			=> SetValue(variableName, YarnVariableType.Number, floatValue.ToString());

		public override void SetValue(string variableName, bool boolValue)
			=> SetValue(variableName, YarnVariableType.Boolean, boolValue.ToString());

		/// <summary>
		/// Retrieves a <see cref="Value"/> by name.
		/// </summary>
		/// <param name="variableName">The name of the variable to retrieve
		/// the value of. Don't forget to include the "$" at the
		/// beginning!</param>
		/// <returns>The <see cref="Value"/>. If a variable by the name of
		/// <paramref name="variableName"/> is not present, returns a value
		/// representing `null`.</returns>
		/// <exception cref="ArgumentException">Thrown when
		/// variableName is not a valid variable name.</exception>
		public override bool TryGetValue<T>(string variableName, out T result)
		{
			static Type VariableTypeToSystemType(YarnVariableType type)
			{
				return type switch
				{
					YarnVariableType.String => typeof(string),
					YarnVariableType.Number => typeof(float),
					YarnVariableType.Boolean => typeof(bool),
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

			var variableType = VariableTypeToSystemType(variable.Type);
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
		/// Throws a <see cref="System.ArgumentException"/> if <paramref
		/// name="variableName"/> is not a valid Yarn Spinner variable
		/// name.
		/// </summary>
		/// <param name="variableName">The variable name to test.</param>
		/// <exception cref="System.ArgumentException">Thrown when
		/// <paramref name="variableName"/> is not a valid variable
		/// name.</exception>
		private static void ValidateVariableName(string variableName)
		{
			if (variableName.StartsWith("$") == false)
			{
				throw new ArgumentException($"{variableName} is not a valid variable name: Variable names must start with a '$'. (Did you mean to use '${variableName}'?)");
			}
		}
	}
}

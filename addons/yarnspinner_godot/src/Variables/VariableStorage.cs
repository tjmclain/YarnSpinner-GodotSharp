using Godot;

namespace Yarn.GodotSharp.Variables
{
	[GlobalClass]
	public partial class VariableStorage : Resource, IVariableStorage
	{
		[Export]
		public Godot.Collections.Dictionary<string, Variable> Variables { get; set; } = new();

		public virtual void SetValue(string variableName, Variant value)
		{
			variableName = ValidateVariableName(variableName);

			if (!Variables.TryGetValue(variableName, out var variable))
			{
				variable = new Variable() { Name = variableName };
				Variables[variableName] = variable;
			}

			variable.VariableType = value.VariantType;
			variable.Value = value;
		}

		#region IVariableStorage

		public virtual void SetValue(string variableName, string stringValue)
			=> SetValue(variableName, stringValue);

		public virtual void SetValue(string variableName, float floatValue)
			=> SetValue(variableName, floatValue);

		public virtual void SetValue(string variableName, bool boolValue)
			=> SetValue(variableName, boolValue);

		public virtual bool TryGetValue<[MustBeVariant] T>(string variableName, out T result)
		{
			variableName = ValidateVariableName(variableName);

			if (!Variables.TryGetValue(variableName, out var variable))
			{
				GD.PushWarning("!_variables.TryGetValue; variableName = " + variableName);
				result = default;
				return false;
			}

			result = variable.Value.As<T>();
			return true;
		}

		public virtual void Clear()
		{
			Variables.Clear();
		}

		public virtual bool Contains(string variableName)
		{
			return Variables.ContainsKey(variableName);
		}

		#endregion IVariableStorage

		protected static string ValidateVariableName(string variableName)
		{
			return variableName.StartsWith("$") ? variableName : $"${variableName}";
		}
	}
}

using Godot;
using Godot.Collections;

namespace Yarn.GodotSharp.Editor
{
	public class GodotEditorSettingsProperty
	{
		#region Fields

		public const string NameKey = "name";
		public const string TypeKey = "type";
		public const string HintKey = "hint";
		public const string DefaultValueKey = "default_value";

		#endregion Fields

		#region Properties

		public string Name { get; set; } = string.Empty;
		public Variant.Type Type { get; set; } = Variant.Type.String;
		public PropertyHint Hint { get; set; } = PropertyHint.None;
		public string DefaultValue { get; set; } = string.Empty;

		#endregion Properties

		#region Public Methods

		public Dictionary ToDictionary()
		{
			if (string.IsNullOrEmpty(Name))
			{
				GD.PushError("string.IsNullOrEmpty(Name)");
				return default;
			}

			return new Dictionary()
				{
					{ NameKey, Name },
					{ TypeKey, Variant.From(Type) },
					{ HintKey, Variant.From(Hint) },
					{ DefaultValueKey, DefaultValue }
				};
		}

		#endregion Public Methods
	}
}

#if TOOLS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Google.Protobuf.WellKnownTypes;

namespace Yarn.GodotSharp.Editor
{
	using GodotDictionary = Godot.Collections.Dictionary;

	public class GodotEditorPropertyInfo : Godot.Collections.Dictionary<string, Variant>
	{
		#region Fields

		public const string NameKey = "name";
		public const string TypeKey = "type";
		public const string HintKey = "hint";
		public const string HintStringKey = "hint_string";
		public const string DefaultValueKey = "default_value";

		#endregion Fields

		#region Properties

		public string Name
		{
			get => TryGetValue(NameKey, out var value) ? value.AsString() : string.Empty;
			set => this[NameKey] = Variant.From(value);
		}

		public Variant.Type Type
		{
			get => TryGetValue(TypeKey, out var value) ? value.VariantType : Variant.Type.Nil;
			set => this[TypeKey] = Variant.From(value);
		}

		public PropertyHint Hint
		{
			get => TryGetValue(HintKey, out var value) ? value.As<PropertyHint>() : PropertyHint.None;
			set => this[HintKey] = Variant.From(value);
		}

		public string HintString
		{
			get => TryGetValue(HintStringKey, out var value) ? value.AsString() : string.Empty;
			set => this[HintStringKey] = Variant.From(value);
		}

		public Variant DefaultValue
		{
			get => TryGetValue(DefaultValueKey, out var value) ? value.AsString() : null;
			set => this[DefaultValueKey] = Variant.From(value);
		}

		#endregion Properties

		public bool AddToProjectSettings()
		{
			string name = Name;
			if (string.IsNullOrEmpty(name))
			{
				GD.PushError("string.IsNullOrEmpty(Name)");
				return false;
			}

			if (!ProjectSettings.HasSetting(name))
			{
				var defaultValue = DefaultValue;
				if (defaultValue.VariantType == Variant.Type.Nil)
				{
					GD.PushError("defaultValue.VariantType == Variant.Type.Nil");
					return false;
				}
				ProjectSettings.SetSetting(name, defaultValue);
			}

			ProjectSettings.AddPropertyInfo((GodotDictionary)this);
			return true;
		}

		public bool RemoveFromProjectSettings()
		{
			string name = Name;
			if (string.IsNullOrEmpty(name))
			{
				GD.PushError("string.IsNullOrEmpty(Name)");
				return false;
			}

			ProjectSettings.SetSetting(name, Variant.From<GodotObject>(null));
			return true;
		}
	}
}

#endif

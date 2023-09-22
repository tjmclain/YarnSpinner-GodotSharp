using System;
using System.Collections.Generic;
using Godot;

namespace Yarn.GodotSharp.Editor
{
	public partial class YarnEditorSettings : Godot.Node
	{
		public const string TranslationsDirProperty = "yarn_spinner/translations_directory";
		public const string BaseLocaleProperty = "yarn_spinner/base_locale";

		private static readonly GodotEditorSettingsProperty[] _properties = new GodotEditorSettingsProperty[]
		{
			new GodotEditorSettingsProperty()
			{
				Name = TranslationsDirProperty,
				Type = Variant.Type.String,
				Hint = PropertyHint.Dir,
				DefaultValue = "res://translations/"
			},
			new GodotEditorSettingsProperty()
			{
				Name = BaseLocaleProperty,
				Type = Variant.Type.String,
				Hint = PropertyHint.LocaleId,
				DefaultValue = "en"
			},
		};

		public override void _EnterTree()
		{
			List<string> propertyNames = new();
			foreach (var property in _properties)
			{
				if (GodotEditorUtility.AddEditorSettingsProperty(property))
				{
					propertyNames.Add(property.Name);
				}
			}

			GD.Print($"YarnEditorSettings: registered {propertyNames.Count} properties.");
			foreach (var name in propertyNames)
			{
				GD.Print($"- {name}");
			}
		}

		public override void _ExitTree()
		{
			List<string> propertyNames = new();
			foreach (var property in _properties)
			{
				if (GodotEditorUtility.RemoveEditorSettingsProperty(property))
				{
					propertyNames.Add(property.Name);
				}
			}

			GD.Print($"YarnEditorSettings: unregistered {propertyNames.Count} properties.");
			foreach (var name in propertyNames)
			{
				GD.Print($"- {name}");
			}
		}
	}
}

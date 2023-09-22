using System;
using Godot;
using Godot.Collections;

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
			base._EnterTree();

			foreach (var property in _properties)
			{
				GodotEditorUtility.AddEditorSettingsProperty(property);
			}
		}

		public override void _ExitTree()
		{
			base._ExitTree();
			foreach (var property in _properties)
			{
				GodotEditorUtility.RemoveEditorSettingsProperty(property);
			}
		}
	}
}

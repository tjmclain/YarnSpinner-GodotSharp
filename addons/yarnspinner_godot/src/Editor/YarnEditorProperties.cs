using Godot;

namespace Yarn.GodotSharp.Editor
{
	public static class YarnEditorProperties
	{
		public const string TranslationsDirProperty = "yarn_spinner/translations_directory";
		public const string BaseLocaleProperty = "yarn_spinner/base_locale";

		private static readonly EditorPropertyInfo[] _properties = new EditorPropertyInfo[]
		{
			new EditorPropertyInfo()
			{
				Name = TranslationsDirProperty,
				Type = Variant.Type.String,
				Hint = PropertyHint.Dir,
				DefaultValue = "res://translations/"
			},
			new EditorPropertyInfo()
			{
				Name = BaseLocaleProperty,
				Type = Variant.Type.String,
				Hint = PropertyHint.LocaleId,
				DefaultValue = "en"
			},
		};

		public static void AddProperties()
		{
			foreach (var property in _properties)
			{
				property.AddToProjectSettings();
			}
			ProjectSettings.Save();
		}

		public static void RemoveProperties()
		{
			foreach (var property in _properties)
			{
				property.RemoveFromProjectSettings();
			}

			ProjectSettings.Save();
		}
	}
}

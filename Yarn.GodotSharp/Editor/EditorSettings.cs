#if TOOLS

using Godot;

namespace Yarn.GodotSharp.Editor
{
	[Tool]
	public static class EditorSettings
	{
		public const string TranslationsDirectoryProperty = "yarn_spinner/translations_directory";
		public const string BaseLocaleProperty = "yarn_spinner/base_locale";
		public const string SupportedLocalesProperty = "yarn_spinner/supported_locales";

		private static readonly Godot.Collections.Dictionary[] _properties = new Godot.Collections.Dictionary[]
		{
			new Godot.Collections.Dictionary
			{
				{ "name", TranslationsDirectoryProperty },
				{ "type", Variant.From(Variant.Type.String) },
				{ "hint", Variant.From(PropertyHint.Dir) },
				{ "default_value", "res://translations/" }
			},
			new Godot.Collections.Dictionary
			{
				{ "name", BaseLocaleProperty },
				{ "type", Variant.From(Variant.Type.String) },
				{ "hint", Variant.From(PropertyHint.LocaleId) },
				{ "default_value", "en" }
			},
		};

		public static void AddProperties(EditorInterface editorInterface)
		{
			var editorSettings = editorInterface.GetEditorSettings();
			foreach (var property in _properties)
			{
				if (!property.TryGetValue("name", out var name))
				{
					GD.PushWarning("!property.TryGetValue('name')");
					continue;
				}

				property.TryGetValue("default_value", out var value);
				editorSettings.Set(name.ToString(), value);
				editorSettings.AddPropertyInfo(property);
				editorSettings.SetInitialValue(name.ToString(), value, false);
			}
		}

		public static void RemoveProperties(EditorInterface editorInterface)
		{
			var editorSettings = editorInterface.GetEditorSettings();
			var nullVar = Variant.From<GodotObject>(null);
			foreach (var property in _properties)
			{
				if (!property.TryGetValue("name", out var name))
				{
					GD.PushWarning("!property.TryGetValue('name')");
					continue;
				}

				if (!editorSettings.HasSetting(name.ToString()))
				{
					continue;
				}

				editorSettings.SetSetting(name.ToString(), nullVar);
			}
		}
	}
}

#endif

#if TOOLS

using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace Yarn.GodotSharp.Editor
{
	[Tool]
	public partial class GodotEditorUtility : EditorScript
	{
		#region Classes

		public class EditorProperty
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

		#endregion Classes

		#region Fields

		private static readonly Variant _nullVariant = default;

		#endregion Fields

		#region Public Methods

		public static GodotEditorUtility GetSingleton()
		{
			if (Engine.HasSingleton(nameof(GodotEditorUtility)))
			{
				return (GodotEditorUtility)Engine.GetSingleton(nameof(GodotEditorUtility));
			}

			var singleton = new GodotEditorUtility();
			Engine.RegisterSingleton(nameof(GodotEditorUtility), singleton);
			return singleton;
		}

		public EditorSettings GetEditorSettings()
		{
			return GetEditorInterface()?.GetEditorSettings();
		}

		public EditorFileSystem GetResourceFilesystem()
		{
			return GetEditorInterface()?.GetResourceFilesystem();
		}

		public Variant GetEditorSettingsProperty(StringName propertyName)
		{
			var editorSettings = GetEditorSettings();
			if (editorSettings == null)
			{
				GD.PushError("editorSettings == null");
				return _nullVariant;
			}

			return editorSettings.Get(propertyName);
		}

		public void AddEditorSettingsProperty(EditorProperty property)
			=> AddEditorSettingsProperty(property?.ToDictionary());

		public void AddEditorSettingsProperty(Dictionary property)
		{
			if (property == null)
			{
				GD.PushError("property == null");
				return;
			}

			if (!property.TryGetValue(EditorProperty.NameKey, out var name))
			{
				GD.PushError("!property.TryGetValue('name')");
				return;
			}

			var editorSettings = GetEditorSettings();
			if (editorSettings == null)
			{
				GD.PushError("editorSettings == null");
				return;
			}

			property.TryGetValue(EditorProperty.DefaultValueKey, out var value);
			editorSettings.Set(name.ToString(), value);
			editorSettings.AddPropertyInfo(property);
			editorSettings.SetInitialValue(name.ToString(), value, false);
		}

		public void RemoveEditorSettingsProperty(EditorProperty property)
		{
			if (property == null)
			{
				GD.PushError("property == null");
				return;
			}

			RemoveEditorSettingsProperty(property.Name);
		}

		public void RemoveEditorSettingsProperty(Dictionary property)
		{
			if (property == null)
			{
				GD.PushError("property == null");
				return;
			}

			property.TryGetValue(EditorProperty.NameKey, out var propertyName);
			RemoveEditorSettingsProperty(propertyName.AsString());
		}

		public void RemoveEditorSettingsProperty(string propertyName)
		{
			if (string.IsNullOrEmpty(propertyName))
			{
				GD.PushError("string.IsNullOrEmpty(propertyName)");
				return;
			}

			var editorSettings = GetEditorSettings();
			if (editorSettings == null)
			{
				GD.PushError("editorSettings == null");
				return;
			}

			if (!editorSettings.HasSetting(propertyName.ToString()))
			{
				return;
			}

			editorSettings.SetSetting(propertyName.ToString(), _nullVariant);
		}

		#endregion Public Methods
	}
}

#endif

#if TOOLS

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

			public const string Name = "name";
			public const string Type = "type";
			public const string Hint = "hint";
			public const string DefaultValue = "default_value";

			#endregion Fields
		}

		#endregion Classes

		#region Fields

		private static readonly Variant _nullVariant = default;

		#endregion Fields

		#region Public Methods

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

		public void AddEditorSettingsProperty(Dictionary property)
		{
			if (!property.TryGetValue("name", out var name))
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

			property.TryGetValue("default_value", out var value);
			editorSettings.Set(name.ToString(), value);
			editorSettings.AddPropertyInfo(property);
			editorSettings.SetInitialValue(name.ToString(), value, false);
		}

		public void RemoveEditorSettingsProperty(Dictionary property)
		{
			if (property == null)
			{
				GD.PushError("property == null");
				return;
			}

			property.TryGetValue("name", out var propertyName);
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

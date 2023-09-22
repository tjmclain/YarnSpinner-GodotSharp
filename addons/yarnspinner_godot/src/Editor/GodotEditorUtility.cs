#if TOOLS

using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Godot.NativeInterop;

namespace Yarn.GodotSharp.Editor
{
	public static class GodotEditorUtility
	{
		#region Fields

		private static readonly EditorScript _dummyEditorScript = new();
		private static readonly Variant _nullVariant = Variant.From(Variant.Type.Nil);

		#endregion Fields

		#region Public Methods

		public static EditorInterface GetEditorInterface()
		{
			return _dummyEditorScript?.GetEditorInterface();
		}

		public static EditorSettings GetEditorSettings()
		{
			return GetEditorInterface()?.GetEditorSettings();
		}

		public static EditorFileSystem GetResourceFilesystem()
		{
			return GetEditorInterface()?.GetResourceFilesystem();
		}

		public static Variant GetEditorSettingsProperty(StringName propertyName)
		{
			var editorSettings = GetEditorSettings();
			if (editorSettings == null)
			{
				GD.PushError("editorSettings == null");
				return _nullVariant;
			}

			return editorSettings.Get(propertyName);
		}

		public static void AddEditorSettingsProperty(GodotEditorSettingsProperty property)
			=> AddEditorSettingsProperty(property?.ToDictionary());

		public static void AddEditorSettingsProperty(Dictionary property)
		{
			if (property == null)
			{
				GD.PushError("property == null");
				return;
			}

			// NOTE: reading from a dictionary can sometimes fail
			if (!property.TryGetValue(GodotEditorSettingsProperty.NameKey, out var name))
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

			property.TryGetValue(GodotEditorSettingsProperty.DefaultValueKey, out var value);

			editorSettings.Set(name.ToString(), value);
			editorSettings.AddPropertyInfo(property);
			editorSettings.SetInitialValue(name.ToString(), value, false);
		}

		public static void RemoveEditorSettingsProperty(GodotEditorSettingsProperty property)
			=> RemoveEditorSettingsProperty(property?.Name);

		public static void RemoveEditorSettingsProperty(Dictionary property)
		{
			if (property == null)
			{
				GD.PushError("property == null");
				return;
			}

			property.TryGetValue(GodotEditorSettingsProperty.NameKey, out var propertyName);
			RemoveEditorSettingsProperty(propertyName.AsString());
		}

		public static void RemoveEditorSettingsProperty(string propertyName)
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

#if TOOLS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Editor
{
	public static class GodotEditorUtility
	{
		private const string _translationsSettingName = "internationalization/locale/translations";

		public static EditorInterface GetEditorInterface()
		{
			var dummyScript = new EditorScript();
			return dummyScript.GetEditorInterface();
		}

		public static string[] GetTranslationsSetting()
		{
			var translationsSetting = ProjectSettings.GetSetting(_translationsSettingName, Array.Empty<string>());
			return translationsSetting.AsStringArray();
		}

		public static void SetTranslations(IEnumerable<string> files)
		{
			ProjectSettings.SetSetting(_translationsSettingName, files.ToArray());
			ProjectSettings.Save();
		}

		public static void AddTranslation(string file)
		{
			var translations = new HashSet<string>(GetTranslationsSetting());
			if (translations.Add(file))
			{
				SetTranslations(translations);
			}
		}

		public static void RemoveTranslation(string file)
		{
			var translations = new List<string>(GetTranslationsSetting());
			if (translations.Remove(file))
			{
				SetTranslations(translations);
			}
		}

		// NOTE (2023-09-18): ProjectSettings.LocalizePath was not working for me. I would pass in a
		// valid global path to an asset under the project directory, but I would get the global path
		// back, not a 'res://' relative path. So, for now, I have to do the replacement manually.
		public static string LocalizePath(string globalPath)
		{
			string projectRoot = ProjectSettings.GlobalizePath("res://");
			return globalPath
				.Replace('\\', '/')
				.Replace(projectRoot, "res://");
		}
	}
}

#endif

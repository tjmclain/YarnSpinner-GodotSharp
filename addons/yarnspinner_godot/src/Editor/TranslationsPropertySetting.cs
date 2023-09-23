using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Editor
{
	public static class TranslationsPropertySetting
	{
		#region Fields

		private const string _propertyName = "internationalization/locale/translations";

		#endregion Fields

		#region Public Methods

		public static string[] Get()
		{
			var translationsSetting = ProjectSettings.GetSetting(_propertyName, Array.Empty<string>());
			return translationsSetting.AsStringArray();
		}

		public static void Set(IEnumerable<string> files)
		{
			ProjectSettings.SetSetting(_propertyName, files.ToArray());
			ProjectSettings.Save();
		}

		public static void Add(string file)
		{
			var translations = new HashSet<string>(Get());
			if (translations.Add(file))
			{
				Set(translations);
			}
		}

		public static void Remove(string file)
		{
			var translations = new List<string>(Get());
			if (translations.Remove(file))
			{
				Set(translations);
			}
		}

		#endregion Public Methods
	}
}

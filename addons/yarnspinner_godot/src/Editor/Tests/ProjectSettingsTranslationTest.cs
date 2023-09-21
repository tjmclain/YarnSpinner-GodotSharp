#if TOOLS

using Godot;
using System;

namespace Yarn.GodotSharp.Editor.Tests
{
	[Tool]
	public partial class ProjectSettingsTranslationTest : CommandPaletteScript
	{
		public override void Execute()
		{
			string setting = "internationalization/locale/translations";

			var translations = ProjectSettings.GetSetting(setting, Array.Empty<string>());
			var array = translations.AsStringArray();
			foreach (var item in array)
			{
				GD.Print(item);
			}
		}
	}
}

#endif

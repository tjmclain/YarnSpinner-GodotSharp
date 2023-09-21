#if TOOLS

using Godot;
using System;

namespace Yarn.GodotSharp.Editor.Tests
{
	[Tool]
	public partial class ProjectSettingsTranslationTest : CommandEditorScript
	{
		// Called when the script is executed (using File -> Run in Script Editor).
		public override void _Run()
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

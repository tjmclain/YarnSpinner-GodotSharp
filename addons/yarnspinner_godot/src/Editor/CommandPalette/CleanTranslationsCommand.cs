#if TOOLS

using System.Linq;
using Godot;

namespace Yarn.GodotSharp.Editor.CommandPalette
{
	[Tool]
	public partial class CleanTranslationsCommand : CommandPaletteScript
	{
		public override void _Run()
		{
			GD.Print("Clean Translations");

			bool changed = false;
			var translations = GodotEditorUtility.GetTranslationsSetting().ToList();
			for (int i = 0; i < translations.Count; i++)
			{
				var file = translations[i];
				if (!FileAccess.FileExists(file))
				{
					GD.Print("- remove missing translation file: " + file);
					translations.RemoveAt(i);
					changed = true;
					continue;
				}
			}

			if (!changed)
			{
				return;
			}

			GodotEditorUtility.SetTranslations(translations);
		}
	}
}

#endif

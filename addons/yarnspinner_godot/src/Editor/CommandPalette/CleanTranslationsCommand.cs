#if TOOLS

using System.Linq;
using Godot;

namespace Yarn.GodotSharp.Editor.CommandPalette
{
	[Tool]
	public partial class CleanTranslationsCommand : CommandEditorScript
	{
		public CleanTranslationsCommand()
		{
			_Run();
		}

		public override void _Run()
		{
			bool changed = false;
			var translations = GodotUtility.TranslationsProjectSetting.Get().ToList();
			for (int i = 0; i < translations.Count; i++)
			{
				var file = translations[i];
				if (!FileAccess.FileExists(file))
				{
					GD.Print("remove missing translation file: " + file);
					translations.RemoveAt(i);
					changed = true;
					continue;
				}
			}

			if (!changed)
			{
				return;
			}

			GodotUtility.TranslationsProjectSetting.Set(translations);
		}
	}
}

#endif

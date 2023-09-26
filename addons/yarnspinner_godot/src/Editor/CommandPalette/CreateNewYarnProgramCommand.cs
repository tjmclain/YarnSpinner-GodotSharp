using System.IO;
using Godot;

namespace Yarn.GodotSharp.Editor.CommandPalette
{
	using FileAccess = Godot.FileAccess;

	public partial class CreateNewYarnProgramCommand : CommandPaletteScript
	{
		public override void _Run()
		{
			void CreateYarnProgram(string path)
			{
				GD.Print($"CreateYarnProgram at {path}");

				if (string.IsNullOrEmpty(path))
				{
					GD.PushError("string.IsNullOrEmpty(path)");
					return;
				}

				string absoluteDirPath = ProjectSettings.GlobalizePath(path);
				absoluteDirPath = Path.GetDirectoryName(absoluteDirPath);
				if (!DirAccess.DirExistsAbsolute(absoluteDirPath))
				{
					var error = DirAccess.MakeDirRecursiveAbsolute(absoluteDirPath);
					if (error != Error.Ok)
					{
						GD.PushError("!DirAccess.MakeDirRecursiveAbsolute; absoluteDirPath = " + absoluteDirPath);
						return;
					}
				}

				using (var file = FileAccess.Open(path, FileAccess.ModeFlags.Write))
				{
					file.StoreString("title: Start\n---\n\n===");
				}

				var fs = GetEditorInterface()?.GetResourceFilesystem();
				if (fs != null)
				{
					// TODO
				}
			}

			var editorInterface = GetEditorInterface();
			if (editorInterface == null)
			{
				GD.PushError("editorInterface == null");
				return;
			}

			var fileDialog = new FileDialog()
			{
				FileMode = FileDialog.FileModeEnum.SaveFile,
				Filters = new string[]
				{
					"*.yarn ; Yarn Programs"
				}
			};

			fileDialog.FileSelected += CreateYarnProgram;

			editorInterface.PopupDialogCenteredRatio(fileDialog);
		}
	}
}

#if TOOLS

using System.IO;
using Godot;

namespace Yarn.GodotSharp.Editor.CommandPalette
{
	using FileAccess = Godot.FileAccess;

	public partial class CreateNewYarnProgramCommand : CommandPaletteScript
	{
		public override void _Run()
		{
			var editorInterface = GetEditorInterface();
			if (editorInterface == null)
			{
				GD.PushError("editorInterface == null");
				return;
			}

			var fileDialog = new FileDialog()
			{
				CurrentFile = "new_program.yarn",
				FileMode = FileDialog.FileModeEnum.SaveFile,
				Filters = new string[]
				{
					"*.yarn ; Yarn Programs"
				}
			};

			fileDialog.FileSelected += CreateYarnProgram;

			editorInterface.PopupDialogCenteredRatio(fileDialog, 0.5f);
		}

		private void CreateYarnProgram(string path)
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
			if (fs == null)
			{
				GD.PushError("fs == null");
				return;
			}

			fs.UpdateFile(path);
			fs.ReimportFiles(new string[] { path });
		}
	}
}

#endif

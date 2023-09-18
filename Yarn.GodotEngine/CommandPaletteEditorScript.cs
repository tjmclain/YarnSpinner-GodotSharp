#if TOOLS

using Godot;

namespace Yarn.GodotEngine.Editor
{
	public abstract partial class CommandEditorScript : EditorScript
	{
		private string _keyName = string.Empty;

		public void AddToCommandPalette()
		{
			var editorInterface = GetEditorInterface();
			if (editorInterface == null)
			{
				GD.PushError("editorInterface == null");
				return;
			}

			var commandPalette = editorInterface.GetCommandPalette();
			if (commandPalette == null)
			{
				GD.PushError("commandPalette == null");
				return;
			}

			string commandName = GodotUtility.VariableNameToFriendlyName(GetType().Name);
			_keyName = GodotUtility.CSharpNameToGodotName(GetType().FullName);
			var callable = new Callable(this, EditorScript.MethodName._Run);
			commandPalette.AddCommand(commandName, _keyName, callable);

			GD.Print("add command: " + _keyName);
		}

		public void RemoveFromCommandPalette()
		{
			if (string.IsNullOrEmpty(_keyName))
			{
				GD.Print($"string.IsNullOrEmpty(_keyName) for type '{GetType().Name}'; no command to remove");
				return;
			}

			var editorInterface = GetEditorInterface();
			if (editorInterface == null)
			{
				GD.PushError("editorInterface == null");
				return;
			}

			var commandPalette = editorInterface.GetCommandPalette();
			if (commandPalette == null)
			{
				GD.PushError("commandPalette == null");
				return;
			}

			commandPalette.RemoveCommand(_keyName);
			GD.Print("remove command: " + _keyName);
		}
	}
}
#endif

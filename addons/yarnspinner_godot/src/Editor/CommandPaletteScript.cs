#if TOOLS

using Godot;

namespace Yarn.GodotSharp.Editor
{
	public abstract partial class CommandPaletteScript : Godot.Node
	{
		#region Fields

		private string _keyName = string.Empty;

		#endregion Fields

		#region Public Methods

		public abstract void Execute();

		public override void _EnterTree()
		{
			base._EnterTree();

			var editorInterface = GodotEditorUtility.GetSingleton()?.GetEditorInterface();
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
			var callable = new Callable(this, MethodName.Execute);
			commandPalette.AddCommand(commandName, _keyName, callable);

			GD.Print("add command: " + _keyName);
		}

		public override void _ExitTree()
		{
			base._ExitTree();

			if (string.IsNullOrEmpty(_keyName))
			{
				GD.Print($"string.IsNullOrEmpty(_keyName) for type '{GetType().Name}'; no command to remove");
				return;
			}

			var editorInterface = GodotEditorUtility.GetSingleton()?.GetEditorInterface();
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

		#endregion Public Methods
	}
}

#endif

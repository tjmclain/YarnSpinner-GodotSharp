#if TOOLS

using Godot;

namespace Yarn.GodotSharp.Editor
{
	public abstract partial class CommandPaletteScript : EditorScript
	{
		#region Fields

		private string _keyName = string.Empty;

		#endregion Fields

		protected virtual string CommandName => GodotUtility.VariableNameToFriendlyName(GetType().Name);
		protected virtual string CommandKey => $"yarn_spinner/{CommandName}";

		#region Public Methods

		public void RegisterCommand()
		{
			var commandPalette = GetCommandPalette();
			if (commandPalette == null)
			{
				GD.PushError("commandPalette == null");
				return;
			}

			commandPalette.RemoveCommand(CommandKey);

			var callable = new Callable(this, EditorScript.MethodName._Run);
			commandPalette.AddCommand(CommandName, _keyName, callable);
		}

		public void UnregisterCommand()
		{
			var commandPalette = GetCommandPalette();
			if (commandPalette == null)
			{
				//GD.PushError("commandPalette == null");
				return;
			}

			commandPalette.RemoveCommand(_keyName);
		}

		private EditorCommandPalette GetCommandPalette()
			=> GetEditorInterface()?.GetCommandPalette();

		#endregion Public Methods
	}
}

#endif

#if TOOLS

using Godot;

namespace Yarn.GodotSharp.Editor
{
	public abstract partial class CommandPaletteScript : EditorScript
	{
		#region Properties

		protected virtual string CommandName => GodotUtility.VariableNameToFriendlyName(GetType().Name);
		protected virtual string CommandKey => $"yarn_spinner/{CommandName}";

		#endregion Properties

		#region Public Methods

		public void RegisterCommand()
		{
			var commandPalette = GetCommandPalette();
			if (commandPalette == null)
			{
				return;
			}

			var callable = new Callable(this, EditorScript.MethodName._Run);
			commandPalette.AddCommand(CommandName, CommandKey, callable);
		}

		public void UnregisterCommand()
		{
			var commandPalette = GetCommandPalette();
			if (commandPalette == null)
			{
				return;
			}

			commandPalette.RemoveCommand(CommandKey);
		}

		#endregion Public Methods

		#region Private Methods

		private EditorCommandPalette GetCommandPalette()
			=> GetEditorInterface()?.GetCommandPalette();

		#endregion Private Methods
	}
}

#endif

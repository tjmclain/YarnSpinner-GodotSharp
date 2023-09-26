#if TOOLS

using System.Text;
using Godot;

namespace Yarn.GodotSharp.Editor
{
	public abstract partial class CommandPaletteScript : EditorScript
	{
		protected virtual string CommandName
			=> ToFriendlyName(GetType().Name.ReplaceN("Command", ""));

		protected virtual string CommandKey
			=> $"yarn_spinner/{GetType().Name.ToSnakeCase()}";

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

		protected EditorCommandPalette GetCommandPalette()
			=> GetEditorInterface()?.GetCommandPalette();

		protected static string ToFriendlyName(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				GD.PushError("string.IsNullOrEmpty(value)");
				return string.Empty;
			}

			const char nullChar = '\0';
			char prev = nullChar;
			var sb = new StringBuilder();
			foreach (char c in value)
			{
				if (char.IsLower(c))
				{
					prev = (prev == nullChar || prev == ' ')
						? char.ToUpper(c) : c;

					sb.Append(prev);
					continue;
				}

				if (c == '_')
				{
					if (prev != nullChar && prev != ' ')
					{
						prev = ' ';
						sb.Append(prev);
					}
					continue;
				}

				if (char.IsUpper(c))
				{
					if (prev != nullChar && !char.IsUpper(prev))
					{
						sb.Append(' ');
					}
				}

				prev = c;
				sb.Append(prev);
			}

			string result = sb.ToString();
			return result;
		}
	}
}

#endif

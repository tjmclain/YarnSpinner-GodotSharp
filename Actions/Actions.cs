using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotEngine.Actions
{
	public class Actions
	{
		private readonly Dictionary<string, Command> _commands = new();

		public Library Library { get; }
		public DialogueRunner DialogueRunner { get; }

		public IEnumerable<Command> Commands => _commands.Values;

		public Actions(DialogueRunner dialogueRunner, Library library)
		{
			Library = library;
			DialogueRunner = dialogueRunner;
		}

		public void AddCommandHandler(string commandName, Delegate handler)
		{
			if (_commands.ContainsKey(commandName))
			{
				GD.PrintErr($"Failed to register command {commandName}: a command by this name has already been registered.");
				return;
			}
			else
			{
				_commands.Add(commandName, new Command(commandName, handler));
			}
		}

		public void AddFunction(string name, Delegate implementation)
		{
			if (Library.FunctionExists(name))
			{
				GD.PrintErr($"Cannot add function {name}: one already exists");
				return;
			}
			Library.RegisterFunction(name, implementation);
		}

		public void AddCommandHandler(string commandName, MethodInfo methodInfo)
		{
			if (_commands.ContainsKey(commandName))
			{
				GD.PrintErr($"Failed to register command {commandName}: a command by this name has already been registered.");
				return;
			}

			_commands[commandName] = new Command(commandName, methodInfo);
		}

		public void RemoveCommandHandler(string commandName)
		{
			if (_commands.Remove(commandName) == false)
			{
				GD.PrintErr($"Can't remove command {commandName}, because no command with this name is currently registered.");
			}
		}

		public void RemoveFunction(string name)
		{
			if (Library.FunctionExists(name) == false)
			{
				GD.PrintErr($"Cannot remove function {name}: no function with that name exists in the library");
				return;
			}
			Library.DeregisterFunction(name);
		}

		public CommandDispatchResult DispatchCommand(string command, out Task commandTask)
		{
			var commandPieces = new List<string>(DialogueRunner.SplitCommandText(command));

			if (commandPieces.Count == 0)
			{
				// No text was found inside the command, so we won't be able to
				// find it.
				commandTask = default;
				return new CommandDispatchResult
				{
					Status = CommandDispatchResult.StatusType.CommandUnknown
				};
			}

			if (_commands.TryGetValue(commandPieces[0], out var registration))
			{
				// The first part of the command is the command name itself. Remove
				// it to get the collection of parameters that were passed to the
				// command.
				commandPieces.RemoveAt(0);

				return registration.Invoke(commandPieces, out commandTask);
			}
			else
			{
				commandTask = default;
				return new CommandDispatchResult
				{
					Status = CommandDispatchResult.StatusType.CommandUnknown
				};
			}
		}
	}
}

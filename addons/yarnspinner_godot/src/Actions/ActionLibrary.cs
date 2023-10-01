using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Godot;

namespace Yarn.GodotSharp.Actions
{
	[GlobalClass]
	public partial class ActionLibrary : Resource
	{
		#region Exports

		[Export]
		public Godot.Collections.Dictionary<string, CommandInfo> Commands { get; private set; } = new();

		[Export]
		public Godot.Collections.Array<ActionInfo> Functions { get; private set; } = new();

		[Export]
		public bool UseOverrideAssemblies { get; private set; } = false;

		[Export]
		public string[] OverrideAssemblyNames { get; private set; } = Array.Empty<string>();

		#endregion Exports

		public virtual void RefreshActions()
		{
			Commands.Clear();
			Functions.Clear();

			var assemblies = !UseOverrideAssemblies
				? new Assembly[] { Assembly.GetExecutingAssembly() }
				: OverrideAssemblyNames.Select(x => Assembly.Load(x));

			foreach (var assembly in assemblies)
			{
				var types = assembly.GetTypes();
				var methods = types.SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public));
				foreach (var method in methods)
				{
					TryGetActionInfoFromMethodInfo(method);
				}
			}

			GD.Print(
				$"{GetType().Name}.{nameof(RefreshActions)}: found ",
				$"{Commands.Count} commands and {Functions.Count} functions ",
				$"in {assemblies.Count()} assemblies"
			);
		}

		public CommandDispatchResult DispatchCommand(string commandText, out Task commandTask)
		{
			var split = SplitCommandText(commandText);
			var parameters = new List<string>(split);

			if (parameters.Count == 0)
			{
				// No text was found inside the command, so we won't be able to find it.
				commandTask = default;
				return new CommandDispatchResult
				{
					Status = CommandDispatchResult.StatusType.CommandUnknown
				};
			}

			if (!Commands.TryGetValue(parameters[0], out var commandInfo))
			{
				commandTask = default;
				return new CommandDispatchResult
				{
					Status = CommandDispatchResult.StatusType.CommandUnknown
				};
			}

			// The first part of the command is the command name itself. Remove it to get the
			// collection of parameters that were passed to the command.
			parameters.RemoveAt(0);

			return commandInfo.Invoke(parameters, out commandTask);
		}

		protected static IEnumerable<string> SplitCommandText(string input)
		{
			var reader = new System.IO.StringReader(input.Normalize());

			int c;

			var results = new List<string>();
			var currentComponent = new System.Text.StringBuilder();

			while ((c = reader.Read()) != -1)
			{
				if (char.IsWhiteSpace((char)c))
				{
					if (currentComponent.Length > 0)
					{
						// We've reached the end of a run of visible characters. Add this run to the
						// result list and prepare for the next one.
						results.Add(currentComponent.ToString());
						currentComponent.Clear();
					}
					else
					{
						// We encountered a whitespace character, but didn't have any characters
						// queued up. Skip this character.
					}

					continue;
				}
				else if (c == '\"')
				{
					// We've entered a quoted string!
					while (true)
					{
						c = reader.Read();
						if (c == -1)
						{
							results.Add(currentComponent.ToString());
							return results;
						}
						else if (c == '\\')
						{
							// Possibly an escaped character!
							var next = reader.Peek();
							if (next == '\\' || next == '\"')
							{
								// It is! Skip the \ and use the character after it.
								reader.Read();
								currentComponent.Append((char)next);
							}
							else
							{
								// Oops, an invalid escape. Add the \ and whatever is after it.
								currentComponent.Append((char)c);
							}
						}
						else if (c == '\"')
						{
							// The end of a string!
							break;
						}
						else
						{
							// Any other character. Add it to the buffer.
							currentComponent.Append((char)c);
						}
					}

					results.Add(currentComponent.ToString());
					currentComponent.Clear();
				}
				else
				{
					currentComponent.Append((char)c);
				}
			}

			if (currentComponent.Length > 0)
			{
				results.Add(currentComponent.ToString());
			}

			return results;
		}

		protected virtual void TryGetActionInfoFromMethodInfo(MethodInfo method)
		{
			var commandAttribute = method.GetCustomAttribute<YarnCommandAttribute>();
			if (commandAttribute != null)
			{
				string name = commandAttribute.Name;
				var command = new CommandInfo(name, method);
				Commands[name] = command;
			}

			var functionAttribute = method.GetCustomAttribute<YarnFunctionAttribute>();
			if (functionAttribute != null)
			{
				string name = commandAttribute.Name;
				var action = new ActionInfo(name, method);
				Functions.Add(action);
			}
		}
	}
}
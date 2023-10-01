using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Actions
{
	using CommandDispatchResult = CommandInfo.CommandDispatchResult;

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

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var assemblies = !UseOverrideAssemblies
				? new Assembly[] { Assembly.GetExecutingAssembly() }
				: OverrideAssemblyNames.Select(x => Assembly.Load(x));

			const string godotNamespace = "Godot";
			foreach (var assembly in assemblies)
			{
				var types = assembly.GetTypes();
				var methods = types
					.Where(x => !x.Namespace.StartsWith(godotNamespace))
					.SelectMany(
						x => x.GetMethods(
							BindingFlags.Public
							| BindingFlags.Instance
							| BindingFlags.Static
							| BindingFlags.DeclaredOnly
						)
					);

				foreach (var method in methods)
				{
					TryGetActionInfoFromMethodInfo(method);
				}
			}

			stopwatch.Stop();

			GD.PrintS(
				$"{GetType().Name}.{nameof(RefreshActions)}: found",
				$"{Commands.Count} commands and {Functions.Count} functions",
				$"in {assemblies.Count()} assemblies in {stopwatch.ElapsedMilliseconds} ms."
			);
		}

		public CommandDispatchResult DispatchCommand(string commandText, out Task commandTask)
		{
			var split = YarnUtility.SplitCommandText(commandText);
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

		protected virtual void TryGetActionInfoFromMethodInfo(MethodInfo method)
		{
			TryGetCommandFromMethodInfo(method);
			TryGetFunctionFromMethodInfo(method);
		}

		protected virtual void TryGetCommandFromMethodInfo(MethodInfo method)
		{
			var attribute = method.GetCustomAttribute<YarnCommandAttribute>();
			if (attribute == null)
			{
				return;
			}

			string name = attribute.Name;
			var command = new CommandInfo(name, method);
			Commands[name] = command;

			GD.Print($"{nameof(TryGetCommandFromMethodInfo)}: {name} ({method.Name})");
		}

		protected virtual void TryGetFunctionFromMethodInfo(MethodInfo method)
		{
			var attribute = method.GetCustomAttribute<YarnFunctionAttribute>();
			if (attribute == null)
			{
				return;
			}

			string name = attribute.Name;
			var action = new ActionInfo(name, method);
			Functions.Add(action);

			GD.Print($"{nameof(TryGetFunctionFromMethodInfo)}: {name} ({method.Name})");
		}
	}
}
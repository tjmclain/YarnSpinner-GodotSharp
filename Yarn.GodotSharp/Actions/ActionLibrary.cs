using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace Yarn.GodotSharp.Actions
{
	[GlobalClass]
	public partial class ActionLibrary : Resource
	{
		[Export]
		public Godot.Collections.Array<ActionInfo> Commands { get; private set; } = new();

		[Export]
		public Godot.Collections.Array<ActionInfo> Functions { get; private set; } = new();

		[Export]
		public bool UseOverrideAssemblyNames { get; private set; } = false;

		[Export]
		public string[] OverrideAssemblyNames { get; private set; } = Array.Empty<string>();

		public void Refresh()
		{
			Commands.Clear();
			Functions.Clear();

			var assemblies = new List<Assembly>();
			if (UseOverrideAssemblyNames)
			{
				assemblies = AppDomain.CurrentDomain
					.GetAssemblies()
					.Where(x => OverrideAssemblyNames.Contains(x.FullName))
					.ToList();
			}
			else
			{
				assemblies = new List<Assembly> { Assembly.GetExecutingAssembly() };
			}

			foreach (var assembly in assemblies)
			{
				var types = assembly.GetTypes();
				var methods = types.SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public));
				foreach (var method in methods)
				{
					var commandAttribute = method.GetCustomAttribute<CommandAttribute>();
					if (commandAttribute != null)
					{
						string name = commandAttribute.Name;
						var action = new ActionInfo(name, method);
						Commands.Add(action);
						continue;
					}

					var functionAttribute = method.GetCustomAttribute<FunctionAttribute>();
					if (functionAttribute != null)
					{
						string name = commandAttribute.Name;
						var action = new ActionInfo(name, method);
						Functions.Add(action);
						continue;
					}
				}
			}

			GD.Print($"{GetType().Name} found {Commands.Count} commands and {Functions.Count} functions in {assemblies.Count} assemblies");
		}
	}
}

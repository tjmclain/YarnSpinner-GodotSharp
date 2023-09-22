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
		#region Properties

		#region Exports

		[Export]
		public Godot.Collections.Array<ActionInfo> Commands { get; private set; } = new();

		[Export]
		public Godot.Collections.Array<ActionInfo> Functions { get; private set; } = new();

		[Export]
		public bool UseOverrideAssemblyNames { get; private set; } = false;

		[Export]
		public string[] OverrideAssemblyNames { get; private set; } = Array.Empty<string>();

		#endregion Exports

		#endregion Properties

		#region Public Methods

		public virtual void RefreshActions()
		{
			Commands.Clear();
			Functions.Clear();

			var assemblies = !UseOverrideAssemblyNames
				? new Assembly[] { Assembly.GetExecutingAssembly() }
				: AppDomain.CurrentDomain.GetAssemblies()
					.Where(x => OverrideAssemblyNames.Contains(x.FullName))
					.ToArray();

			foreach (var assembly in assemblies)
			{
				var types = assembly.GetTypes();
				var methods = types.SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public));
				foreach (var method in methods)
				{
					EvaluateMethodInfo(method);
				}
			}

			GD.Print($"{GetType().Name} found {Commands.Count} commands and {Functions.Count} functions in {assemblies.Length} assemblies");
		}

		#endregion Public Methods

		protected virtual void EvaluateMethodInfo(MethodInfo method)
		{
			var commandAttribute = method.GetCustomAttribute<CommandAttribute>();
			if (commandAttribute != null)
			{
				string name = commandAttribute.Name;
				var action = new ActionInfo(name, method);
				Commands.Add(action);
			}

			var functionAttribute = method.GetCustomAttribute<FunctionAttribute>();
			if (functionAttribute != null)
			{
				string name = commandAttribute.Name;
				var action = new ActionInfo(name, method);
				Functions.Add(action);
			}
		}
	}
}
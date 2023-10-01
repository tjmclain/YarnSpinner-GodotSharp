using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Godot;

namespace Yarn.GodotSharp.Actions
{
	using Converter = Func<string, object>;
	using GodotNode = Godot.Node;

	[GlobalClass]
	public partial class CommandInfo : ActionInfo
	{
		private Converter[] _converters = null;

		public CommandInfo(string name, MethodInfo methodInfo) : base(name, methodInfo)
		{
		}

		public Converter[] Converters => _converters ?? CreateConverters();

		protected Dictionary<string, GodotNode> CachedTargets { get; private set; } = new();

		public CommandDispatchResult Invoke(List<string> parameters, out Task commandTask)
		{
			commandTask = default;

			if (!TryParseArgs(parameters.ToArray(), out var args, out var errorMessage))
			{
				return new CommandDispatchResult
				{
					CommandName = Name,
					Status = CommandDispatchResult.StatusType.InvalidParameterCount,
					Message = errorMessage,
				};
			}

			bool TryGetTarget(out GodotNode target)
			{
				target = null;
				if (IsStatic)
				{
					return true;
				}

				if (args.Length == 0)
				{
					return false;
				}

				string targetName = args[0].ToString();
				return TryFindTargetInScene(targetName, out target);
			}

			if (!TryGetTarget(out GodotNode target))
			{
				return new CommandDispatchResult()
				{
					CommandName = Name,
					Status = CommandDispatchResult.StatusType.NoTargetFound,
					Message = new StringBuilder()
						.Append("CommandInfo.Invoke: ")
						.Append("!TryGetTarget; ")
						.Append($"Name = {Name}")
						.Append($"target = {(args.Length > 0 ? args[0] : null)}")
						.ToString()
				};
			}

			var returnValue = MethodInfo.Invoke(target, args);
			if (returnValue is Task task)
			{
				commandTask = task;
				return new CommandDispatchResult
				{
					CommandName = Name,
					Status = CommandDispatchResult.StatusType.SucceededAsync
				};
			}
			else
			{
				return new CommandDispatchResult
				{
					CommandName = Name,
					Status = CommandDispatchResult.StatusType.SucceededSync
				};
			}
		}

		protected virtual Converter[] CreateConverters()
		{
			static Converter CreateConverter(ParameterInfo parameter, int index)
			{
				var targetType = parameter.ParameterType;

				// well, I mean...
				if (targetType == typeof(string))
				{
					return arg => arg;
				}

				// find the GodotNode.
				if (typeof(GodotNode).IsAssignableFrom(targetType))
				{
					return arg => GodotUtility.GetNode(arg);
				}

				// bools can take "true" or "false", or the parameter name.
				if (typeof(bool).IsAssignableFrom(targetType))
				{
					return arg =>
					{
						// If the argument is the name of the parameter, interpret the argument as 'true'.
						if (arg.Equals(parameter.Name, StringComparison.InvariantCultureIgnoreCase))
						{
							return true;
						}

						// If the argument can be parsed as boolean true or false, return that result.
						if (bool.TryParse(arg, out bool res))
						{
							return res;
						}

						// We can't parse the argument.
						throw new ArgumentException(
							new StringBuilder()
							.Append("Can't convert the given parameter at ")
							.Append($"position {index + 1} '{arg}' ")
							.Append($"to parameter {parameter.Name} ")
							.Append($"of type {typeof(bool).FullName}")
							.ToString()
						);
					};
				}

				// Fallback: try converting using IConvertible.
				return arg =>
				{
					try
					{
						return Convert.ChangeType(arg, targetType, CultureInfo.InvariantCulture);
					}
					catch (Exception ex)
					{
						throw new ArgumentException(
							new StringBuilder()
							.Append("Can't convert the given parameter at ")
							.Append($"position {index + 1} (\"{arg}\") ")
							.Append($"to parameter {parameter.Name} ")
							.Append($"of type {targetType.FullName}: ")
							.Append(ex.Message)
							.ToString(),
							ex
						);
					}
				};
			}

			if (MethodInfo == null)
			{
				GD.PushError($"{nameof(CreateConverters)}: {nameof(MethodInfo)} == null");
				_converters = null;
				return null;
			}

			ParameterInfo[] parameterInfos = MethodInfo.GetParameters();
			Converter[] result = new Converter[parameterInfos.Length];

			for (int i = 0; i < parameterInfos.Length; i++)
			{
				var info = parameterInfos[i];
				result[i] = CreateConverter(info, i);
			}

			_converters = result;
			return result;
		}

		protected virtual bool TryFindTargetInScene(string name, out GodotNode target)
		{
			if (CachedTargets.TryGetValue(name, out target))
			{
				if (target != null)
				{
					return true;
				}

				CachedTargets.Remove(name);
			}

			target = GodotUtility.GetNode(name);
			if (target != null)
			{
				CachedTargets[name] = target;
				return true;
			}

			return false;
		}

		private (int Min, int Max) GetParameterCount()
		{
			var parameters = MethodInfo.GetParameters();
			int optional = 0;
			foreach (var parameter in parameters)
			{
				if (parameter.IsOptional)
				{
					optional += 1;
				}
			}

			int min = parameters.Length - optional;
			int max = parameters.Length;
			return (min, max);
		}

		/// <summary>
		/// Attempt to parse the arguments with cached converters.
		/// </summary>
		private bool TryParseArgs(string[] args, out object[] result, out string message)
		{
			var parameters = MethodInfo.GetParameters();

			var (min, max) = GetParameterCount();

			int argumentCount = args.Length;
			if (argumentCount < min || argumentCount > max)
			{
				// Wrong number of arguments.
				string requirementDescription;
				if (min == 0)
				{
					requirementDescription = $"at most {max} {EnglishPluraliseNounCount(max, "parameter")}";
				}
				else if (min != max)
				{
					requirementDescription = $"between {min} and {max} {EnglishPluraliseNounCount(max, "parameter")}";
				}
				else
				{
					requirementDescription = $"{min} {EnglishPluraliseNounCount(max, "parameter")}";
				}
				message = $"{Name} requires {requirementDescription}, but {argumentCount} {EnglishPluraliseWasVerb(argumentCount)} provided.";
				result = default;
				return false;
			}

			var finalArgs = new object[parameters.Length];

			for (int i = 0; i < argumentCount; i++)
			{
				string arg = args[i];
				if (Converters[i] == null)
				{
					finalArgs[i] = arg;
				}
				else
				{
					try
					{
						finalArgs[i] = Converters[i].Invoke(arg);
					}
					catch (Exception e)
					{
						message = $"Can't convert parameter {i} to {parameters[i].ParameterType.Name}: {e.Message}";
						result = default;
						return false;
					}
				}
			}
			for (int i = argumentCount; i < finalArgs.Length; i++)
			{
				finalArgs[i] = System.Type.Missing;
			}
			result = finalArgs;
			message = default;
			return true;
		}

		/// <summary>
		/// Represents the result of attempting to locate and call a command.
		/// </summary>
		public struct CommandDispatchResult
		{
			public string CommandName;

			public StatusType Status;

			public string Message;

			public enum StatusType
			{
				SucceededSync,
				SucceededAsync,
				NoTargetFound,
				TargetMissingComponent,
				InvalidParameterCount,
				CommandUnknown,
			};

			public readonly bool IsSuccess
			{
				get
				{
					return Status == StatusType.SucceededAsync || Status == StatusType.SucceededSync;
				}
			}

			public override string ToString()
			{
				return $"{CommandName}: {Status}; {Message}";
			}
		}

		#region Diagnostic Utility Methods

		private static string EnglishPluraliseNounCount(int count, string name, bool prefixCount = false)
		{
			string result;
			if (count == 1)
			{
				result = name;
			}
			else
			{
				result = name + "s";
			}
			if (prefixCount)
			{
				return count.ToString() + " " + result;
			}
			else
			{
				return result;
			}
		}

		private static string EnglishPluraliseWasVerb(int count)
		{
			if (count == 1)
			{
				return "was";
			}
			else
			{
				return "were";
			}
		}

		private static string GetFullMethodName(MethodInfo method)
		{
			return $"{method.DeclaringType.FullName}.{method.Name}";
		}

		#endregion Diagnostic Utility Methods
	}
}
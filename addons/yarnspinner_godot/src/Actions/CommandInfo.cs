using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.GodotSharp.Actions
{
	using Converter = Func<string, object>;
	using GodotNode = Godot.Node;

	public partial class CommandInfo : ActionInfo
	{
		public CommandInfo(string name, MethodInfo method) : base(name, method)
		{
			Target = null;
			Converters = CreateConverters(method);
		}

		public CommandInfo(ActionInfo other) : base(other)
		{
			Target = null;
			Converters = CreateConverters(MethodInfo);
		}

		public enum CommandType
		{
			Invalid = -1,
			IsVoid,
			IsTask,
		}

		#region Properties

		public object Target { get; private set; }
		public Converter[] Converters { get; private set; } = Array.Empty<Converter>();

		public CommandType Type
		{
			get
			{
				Type returnType = ReturnType;

				if (typeof(void).IsAssignableFrom(returnType))
				{
					return CommandType.IsVoid;
				}
				if (typeof(Task).IsAssignableFrom(returnType))
				{
					return CommandType.IsTask;
				}
				return CommandType.Invalid;
			}
		}

		#endregion Properties

		#region Public Methods

		public CommandDispatchResult Invoke(List<string> parameters, out Task commandTask)
		{
			object target = !IsStatic ? Target : null;
			if (!IsStatic && target == null)
			{
				throw new ArgumentException("!IsStatic && target == null; method = " + GetFullMethodName(MethodInfo));
			}

			if (TryParseArgs(parameters.ToArray(), out var finalParameters, out var errorMessage) == false)
			{
				commandTask = default;
				return new CommandDispatchResult
				{
					Status = CommandDispatchResult.StatusType.InvalidParameterCount,
					Message = errorMessage,
				};
			}

			var returnValue = MethodInfo.Invoke(target, finalParameters);
			if (returnValue is Task task)
			{
				commandTask = task;
				return new CommandDispatchResult
				{
					Status = CommandDispatchResult.StatusType.SucceededAsync
				};
			}
			else
			{
				commandTask = default;
				return new CommandDispatchResult
				{
					Status = CommandDispatchResult.StatusType.SucceededSync
				};
			}
		}

		#endregion Public Methods

		#region Private Methods

		private static Converter[] CreateConverters(MethodInfo method)
		{
			static Converter CreateConverter(ParameterInfo parameter, int index)
			{
				var targetType = parameter.ParameterType;

				// well, I mean...
				if (targetType == typeof(string))
				{
					return arg => arg;
				}

				// find the GameObject.
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

			ParameterInfo[] parameterInfos = method.GetParameters();
			Converter[] result = new Converter[parameterInfos.Length];

			for (int i = 0; i < parameterInfos.Length; i++)
			{
				var info = parameterInfos[i];
				result[i] = CreateConverter(info, i);
			}
			return result;
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

		#endregion Private Methods
	}
}

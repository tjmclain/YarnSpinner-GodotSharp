using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;

namespace Yarn.GodotEngine.Actions
{
	using Converter = Func<string, object>;

	public class Command
	{
		public Command(string name, Delegate implementation)
		{
			Name = name;
			Method = implementation.Method;
			Target = implementation.Target;
			Converters = CreateConverters(Method);
		}

		public Command(string name, MethodInfo method)
		{
			if (!method.IsStatic)
			{
				throw new ArgumentException($"Cannot register method {GetFullMethodName(method)} as a command; methods must be static");
			}

			Name = name;
			Method = method;
			Target = null;

			Converters = CreateConverters(method);
		}

		public enum CommandType
		{
			/// <summary>
			/// The method returns <see cref="void"/>.
			/// </summary>
			IsVoid,

			/// <summary> The method returns a <see cref="Task"/> object. </summary> <remarks>
			IsTask,

			/// <summary>
			/// The method is not a valid command (that is, it does not return <see cref="void"/> or
			/// <see cref="Task"/>.)
			/// </summary>
			Invalid,
		}

		public Converter[] Converters { get; private set; } = Array.Empty<Converter>();
		public Type DeclaringType => Method.DeclaringType;
		public bool IsStatic => Method.IsStatic;
		public MethodInfo Method { get; private set; }
		public object Target { get; private set; }
		public string Name { get; private set; }
		public Type ReturnType => Method.ReturnType;

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

		public CommandDispatchResult Invoke(List<string> parameters, out Task commandTask)
		{
			object target = !IsStatic ? Target : null;
			if (!IsStatic && target == null)
			{
				throw new ArgumentException("!IsStatic && target == null; method = " + GetFullMethodName(Method));
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

			var returnValue = Method.Invoke(target, finalParameters);

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
				commandTask = null;
				return new CommandDispatchResult
				{
					Status = CommandDispatchResult.StatusType.SucceededSync
				};
			}
		}

		private static Converter[] CreateConverters(MethodInfo method)
		{
			static Converter CreateConverter(ParameterInfo parameter, int index)
			{
				var targetType = parameter.ParameterType;

				// well, I mean...
				if (targetType == typeof(string))
				{ return arg => arg; }

				// find the GameObject.
				if (typeof(Node).IsAssignableFrom(targetType))
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
							$"Can't convert the given parameter at position {index + 1} (\"{arg}\") to parameter " +
							$"{parameter.Name} of type {typeof(bool).FullName}.");
					};
				}

				// Fallback: try converting using IConvertible.
				return arg =>
				{
					try
					{
						return Convert.ChangeType(arg, targetType, CultureInfo.InvariantCulture);
					}
					catch (Exception e)
					{
						throw new ArgumentException(
							$"Can't convert the given parameter at position {index + 1} (\"{arg}\") to parameter " +
							$"{parameter.Name} of type {targetType.FullName}: {e}", e);
					}
				};
			}

			ParameterInfo[] parameterInfos = method.GetParameters();

			Converter[] result = (Func<string, object>[])Array.CreateInstance(
				typeof(Func<string, object>),
				parameterInfos.Length
			);

			int i = 0;

			foreach (var parameterInfo in parameterInfos)
			{
				result[i] = CreateConverter(parameterInfo, i);
				i++;
			}
			return result;
		}

		private (int Min, int Max) GetParameterCount()
		{
			var parameters = Method.GetParameters();
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
			var parameters = Method.GetParameters();

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
	}
}

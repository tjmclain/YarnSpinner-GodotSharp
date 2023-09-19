using System;
using System.Linq;
using System.Reflection;
using Godot;

namespace Yarn.GodotEngine.Actions
{
	using Expression = System.Linq.Expressions.Expression;

	[Tool]
	public partial class ActionInfo : Resource
	{
		#region Fields

		private MethodInfo _methodInfo;

		#endregion Fields

		#region Constructors

		public ActionInfo()
		{
		}

		public ActionInfo(ActionInfo other)
		{
			Name = other.Name;
			MethodInfoName = other.MethodInfoName;
			DeclaringTypeName = other.DeclaringTypeName;

			CreateMethodInfo();
		}

		public ActionInfo(string name, MethodInfo methodInfo)
		{
			Name = name;
			MethodInfoName = methodInfo?.Name;
			DeclaringTypeName = methodInfo?.DeclaringType.AssemblyQualifiedName;

			_methodInfo = methodInfo;
		}

		#endregion Constructors

		#region Exports

		[Export]
		public string Name { get; private set; }

		[Export]
		public string MethodInfoName { get; private set; }

		[Export]
		public string DeclaringTypeName { get; private set; }

		#endregion Exports

		#region Properties

		public MethodInfo MethodInfo => _methodInfo ?? CreateMethodInfo();
		public Type ReturnType => MethodInfo.ReturnType;
		public bool IsStatic => MethodInfo.IsStatic;

		#endregion Properties

		#region Public Methods

		public MethodInfo CreateMethodInfo()
		{
			if (string.IsNullOrEmpty(DeclaringTypeName))
			{
				GD.PushError("string.IsNullOrEmpty(DeclaringTypeName)");
				return null;
			}

			if (string.IsNullOrEmpty(MethodInfoName))
			{
				GD.PushError("string.IsNullOrEmpty(MethodInfoName)");
				return null;
			}

			var type = Type.GetType(DeclaringTypeName);
			if (type == null)
			{
				GD.PushError("type == null");
				return null;
			}

			_methodInfo = type.GetMethod(MethodInfoName);
			return _methodInfo;
		}

		// Construct a delegate from a methodinfo https://stackoverflow.com/questions/940675/getting-a-delegate-from-methodinfo
		public Delegate CreateDelegate(object target = null)
		{
			var methodInfo = MethodInfo;
			if (methodInfo == null)
			{
				GD.PushError("methodInfo == null");
				return null;
			}

			Func<Type[], Type> getType;
			var isAction = methodInfo.ReturnType.Equals(typeof(void));
			var types = methodInfo.GetParameters().Select(p => p.ParameterType);

			if (isAction)
			{
				getType = Expression.GetActionType;
			}
			else
			{
				getType = Expression.GetFuncType;
				types = types.Concat(new[] { methodInfo.ReturnType });
			}

			return methodInfo.IsStatic
				? Delegate.CreateDelegate(getType(types.ToArray()), methodInfo)
				: Delegate.CreateDelegate(getType(types.ToArray()), target, methodInfo.Name);
		}

		#endregion Public Methods
	}
}

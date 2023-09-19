using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GodotNode = Godot.Node;

namespace Yarn.GodotSharp
{
	/// <summary>
	/// A <see cref="MonoBehaviour"/> that a <see cref="DialogueRunner"/>
	/// uses to store and retrieve variables.
	/// </summary>
	/// <remarks>
	/// This abstract class inherits from <see cref="MonoBehaviour"/>,
	/// which means that subclasses of this class can be attached to <see
	/// cref="GameObject"/>s.
	/// </remarks>
	public abstract partial class VariableStorageBehaviour : GodotNode, IVariableStorage
	{
		/// <inheritdoc/>
		public abstract bool TryGetValue<T>(string variableName, out T result);

		/// <inheritdoc/>
		public abstract void SetValue(string variableName, string stringValue);

		/// <inheritdoc/>
		public abstract void SetValue(string variableName, float floatValue);

		/// <inheritdoc/>
		public abstract void SetValue(string variableName, bool boolValue);

		/// <inheritdoc/>
		public abstract void Clear();

		public abstract bool Contains(string variableName);
	}
}

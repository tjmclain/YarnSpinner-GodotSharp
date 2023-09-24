using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Variables
{
	public partial class VariableStorage : Resource, IVariableStorage
	{
		public virtual void Clear()
		{
			throw new NotImplementedException();
		}

		public virtual void SetValue(string variableName, string stringValue)
		{
			throw new NotImplementedException();
		}

		public virtual void SetValue(string variableName, float floatValue)
		{
			throw new NotImplementedException();
		}

		public virtual void SetValue(string variableName, bool boolValue)
		{
			throw new NotImplementedException();
		}

		public virtual bool TryGetValue<T>(string variableName, out T result)
		{
			throw new NotImplementedException();
		}

		public virtual bool Contains(string variableName)
		{
			throw new NotImplementedException();
		}
	}
}

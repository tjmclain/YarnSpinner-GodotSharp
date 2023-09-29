using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.GodotSharp.Actions
{
	/// <summary>
	/// Represents the result of attempting to locate and call a command.
	/// </summary>
	public struct CommandDispatchResult
	{
		public enum StatusType
		{
			SucceededSync,
			SucceededAsync,
			NoTargetFound,
			TargetMissingComponent,
			InvalidParameterCount,
			CommandUnknown,
		};

		public string CommandName;

		public StatusType Status;

		public string Message;

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
}

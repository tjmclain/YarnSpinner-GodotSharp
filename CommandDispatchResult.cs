using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarn.Godot
{
	/// <summary>
	/// Represents the result of attempting to locate and call a command.
	/// </summary>
	/// <seealso cref="DispatchCommandToGameObject(Command, Action)"/>
	/// <seealso cref="DispatchCommandToRegisteredHandlers(Command, Action)"/>
	public struct CommandDispatchResult
	{
		public enum StatusType
		{
			SucceededAsync,

			SucceededSync,

			NoTargetFound,

			TargetMissingComponent,

			InvalidParameterCount,

			/// <summary>
			/// The command could not be found.
			/// </summary>
			CommandUnknown,

			/// <summary>
			/// The command was located and successfully called.
			/// </summary>
			[Obsolete("Use a more specific enum case", true)]
			Success,

			/// <summary>
			/// The command was located, but failed to be called.
			/// </summary>
			[Obsolete("Use a more specific enum case", true)]
			Failed,
		};

		public StatusType Status;

		public string Message;

		public readonly bool IsSuccess
		{
			get
			{
				return Status == StatusType.SucceededAsync || Status == StatusType.SucceededSync;
			}
		}
	}
}
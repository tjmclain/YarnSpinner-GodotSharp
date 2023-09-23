using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yarn.GodotSharp.Views
{
	public interface IRunLineHandler
	{
		#region Public Methods

		Task RunLine(LocalizedLine line, Action interruptLine);

		Task DismissLine(LocalizedLine line);

		#endregion Public Methods
	}
}

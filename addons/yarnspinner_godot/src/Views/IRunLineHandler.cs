using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yarn.GodotSharp.Views
{
	public interface IRunLineHandler
	{
		Task RunLine(LocalizedLine line, Action interruptLine, CancellationToken token);

		Task DismissLine(LocalizedLine line);
	}
}
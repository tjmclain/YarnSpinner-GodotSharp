using System.Threading;
using System.Threading.Tasks;

namespace Yarn.GodotSharp.Views
{
	public interface ITransitionOutHandler
	{
		Task TransitionOut(CancellationToken token);
	}
}
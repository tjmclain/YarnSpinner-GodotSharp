using System.Threading;
using System.Threading.Tasks;

namespace Yarn.GodotSharp.Views
{
	public interface ITransitionInHandler
	{
		Task TransitionIn(CancellationToken token);
	}
}
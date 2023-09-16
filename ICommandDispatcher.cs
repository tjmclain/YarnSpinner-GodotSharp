using System.Collections.Generic;
using System.Threading.Tasks;

namespace Yarn.GodotEngine
{
	public interface ICommandDispatcher : IActionRegistration
	{
		CommandDispatchResult DispatchCommand(string command, out Task commandTask);

		void SetupForProject(YarnProject yarnProject);

		IEnumerable<ICommand> Commands { get; }
	}
}
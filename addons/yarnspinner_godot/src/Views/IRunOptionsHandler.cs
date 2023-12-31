using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yarn.GodotSharp.Views
{
	public interface IRunOptionsHandler
	{
		Task RunOptions(DialogueOption[] options, Action<int> selectOption, CancellationToken token);

		Task DismissOptions(DialogueOption[] options, int selectedOptionIndex);
	}
}
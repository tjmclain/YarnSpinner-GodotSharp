using System;
using System.Threading.Tasks;

namespace Yarn.GodotSharp.Views
{
	public interface IRunOptionsHandler
	{
		#region Public Methods

		Task RunOptions(DialogueOption[] options, Action<int> selectOption);

		Task DismissOptions(DialogueOption[] options, int selectedOptionIndex);

		#endregion Public Methods
	}
}

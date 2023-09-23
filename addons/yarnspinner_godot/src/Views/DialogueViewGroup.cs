using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Views
{
	[GlobalClass]
	public partial class DialogueViewGroup : Godot.Node, IDialogueStartedHandler, IDialogueCompleteHandler, IRunLineHandler, IRunOptionsHandler
	{
		[Export]
		public virtual Godot.Collections.Array<Godot.Node> DialogueViews { get; set; } = new();

		public virtual void DialogueStarted()
		{
			var views = DialogueViews
				.Select(x => x as IDialogueStartedHandler)
				.Where(x => x != null);

			foreach (var view in views)
			{
				view.DialogueStarted();
			}
		}

		public virtual void DialogueComplete()
		{
			var views = DialogueViews
				.Select(x => x as IDialogueCompleteHandler)
				.Where(x => x != null);

			foreach (var view in views)
			{
				view.DialogueComplete();
			}
		}

		public virtual async Task RunLine(LocalizedLine line, Action interruptLine)
		{
			using var cts = new CancellationTokenSource();

			// Send line to all active dialogue views
			var views = DialogueViews
				.Select(x => x as IRunLineHandler)
				.Where(x => x != null);

			var tasks = new List<Task>();
			foreach (var view in views)
			{
				if (view == null)
				{
					continue;
				}

				var task = Task.Run(
					() => view.RunLine(line, () => cts.Cancel()),
					cts.Token
				);
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);

			interruptLine?.Invoke();
		}

		public virtual async Task DismissLine(LocalizedLine line)
		{
			var views = DialogueViews
				.Select(x => x as IRunLineHandler)
				.Where(x => x != null);

			var tasks = new List<Task>();
			foreach (var view in views)
			{
				var task = view.DismissLine(line);
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		public virtual async Task RunOptions(DialogueOption[] options, Action<int> selectOption)
		{
			using var cts = new CancellationTokenSource();

			int selectedOptionIndex = -1;

			var views = DialogueViews
				.Select(x => x as IRunOptionsHandler)
				.Where(x => x != null);

			var tasks = new List<Task>();
			foreach (var view in views)
			{
				var task = Task.Run(
					() => view.RunOptions(options, (index) =>
					{
						selectedOptionIndex = index;
						cts.Cancel();
					}),
					cts.Token
				);
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);

			selectOption?.Invoke(selectedOptionIndex);
		}

		public virtual async Task DismissOptions(DialogueOption[] options, int selectedOptionIndex)
		{
			var views = DialogueViews
				.Select(x => x as IRunOptionsHandler)
				.Where(x => x != null);

			var tasks = new List<Task>();

			foreach (var view in views)
			{
				var task = view.DismissOptions(options, selectedOptionIndex);
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Yarn.GodotSharp.Actions;
using Yarn.GodotSharp.Views.Effects;

namespace Yarn.GodotSharp.Views
{
	[GlobalClass]
	public partial class DialogueViewGroup : AsyncViewControl, IDialogueStartedHandler, IDialogueCompleteHandler, IRunLineHandler, IRunOptionsHandler, ITransitionInHandler, ITransitionOutHandler
	{
		private Godot.Collections.Array<Godot.Node> _dialogueViews = new();

		[Export]
		public virtual Godot.Collections.Array<Godot.Node> DialogueViews
		{
			get => _dialogueViews;
			set => SetDialougeViews(value);
		}

		[Export]
		public ControlEffect TransitionInEffect { get; set; } = null;

		[Export]
		public ControlEffect TransitionOutEffect { get; set; } = null;

		public virtual IEnumerable<Godot.Node> GetActiveDialogueViews()
		{
			return DialogueViews;
		}

		public virtual void SetDialougeViews(Godot.Collections.Array<Godot.Node> value)
		{
			_dialogueViews.Clear();
			_dialogueViews.AddRange(value);
		}

		public virtual void ShowAll()
		{
			foreach (var view in DialogueViews)
			{
				if (view is not CanvasItem canvasItem)
				{
					continue;
				}

				canvasItem.Show();
			}
		}

		public virtual void HideAll()
		{
			foreach (var view in DialogueViews)
			{
				if (view is not CanvasItem canvasItem)
				{
					continue;
				}

				canvasItem.Hide();
			}
		}

		#region IDialogueStartedHandler

		public virtual void DialogueStarted()
		{
			var views = GetActiveDialogueViews()
				.Select(x => x as IDialogueStartedHandler)
				.Where(x => x != null);

			foreach (var view in views)
			{
				view.DialogueStarted();
			}
		}

		#endregion IDialogueStartedHandler

		#region IDialogueCompleteHandler

		public virtual void DialogueComplete()
		{
			var views = GetActiveDialogueViews()
				.Select(x => x as IDialogueCompleteHandler)
				.Where(x => x != null);

			foreach (var view in views)
			{
				view.DialogueComplete();
			}
		}

		#endregion IDialogueCompleteHandler

		#region IRunLineHandler

		public virtual async Task RunLine(
			LocalizedLine line,
			Action interruptLine,
			CancellationToken externalToken
		)
		{
			GD.Print($"DialogueViewGroup.RunLine - Begin; Name = {Name}");
			SafeDisposeInternalTokenSource();

			if (externalToken.IsCancellationRequested)
			{
				//externalToken.ThrowIfCancellationRequested();
				return;
			}

			var views = GetActiveDialogueViews()
				.Select(x => x as IRunLineHandler)
				.Where(x => x != null);

			if (!views.Any())
			{
				GD.Print($"RunLine: no views found");
				return;
			}

			using (var cts = CreateLinkedTokenSource(externalToken))
			{
				// Send line to all active dialogue views
				var tasks = new List<Task>();
				foreach (var view in views)
				{
					if (view == null)
					{
						continue;
					}

					var task = view.RunLine(line, interruptLine, externalToken);
					tasks.Add(task);
				}

				await Task.WhenAll(tasks);

				// if we were interrupted from an external source, exit task here
				if (externalToken.IsCancellationRequested)
				{
					SafeDisposeInternalTokenSource();
					//externalToken.ThrowIfCancellationRequested();
					return;
				}
			}

			SafeDisposeInternalTokenSource();
			GD.Print($"DialogueViewGroup.RunLine - End; Name = {Name}");
		}

		public virtual async Task DismissLine(LocalizedLine line)
		{
			GD.Print($"DialogueViewGroup.DismissLine - Begin; Name = {Name}");
			SafeDisposeInternalTokenSource();

			var views = GetActiveDialogueViews()
				.Select(x => x as IRunLineHandler)
				.Where(x => x != null);

			if (!views.Any())
			{
				GD.Print($"DismissLine: no views found");
				return;
			}

			var tasks = new List<Task>();
			foreach (var view in views)
			{
				var task = Task.Run(() => view.DismissLine(line));
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		#endregion IRunLineHandler

		#region IRunOptionsHandler

		public virtual async Task RunOptions(
			DialogueOption[] options,
			Action<int> selectOption,
			CancellationToken externalToken
		)
		{
			SafeDisposeInternalTokenSource();

			int selectedOptionIndex = -1;

			var views = GetActiveDialogueViews()
				.Select(x => x as IRunOptionsHandler)
				.Where(x => x != null);

			if (!views.Any())
			{
				throw new TaskCanceledException();
			}

			using (var cts = CreateLinkedTokenSource(externalToken))
			{
				var tasks = new List<Task>();
				foreach (var view in views)
				{
					var task = view.RunOptions(options, (index) =>
					{
						selectedOptionIndex = index;
						SafeCancelInternalTokenSource();
					}, cts.Token);

					tasks.Add(task);
				}

				await Task.WhenAll(tasks);

				// if we were interrupted from an external source, exit task here
				if (externalToken.IsCancellationRequested)
				{
					SafeDisposeInternalTokenSource();
					//externalToken.ThrowIfCancellationRequested();
					return;
				}
			}

			SafeDisposeInternalTokenSource();

			GD.Print("RunOptions: selectedOptionIndex = " + selectedOptionIndex);
			selectOption?.Invoke(selectedOptionIndex);
		}

		public virtual async Task DismissOptions(DialogueOption[] options, int selectedOptionIndex)
		{
			SafeDisposeInternalTokenSource();

			var views = GetActiveDialogueViews()
				.Select(x => x as IRunOptionsHandler)
				.Where(x => x != null);

			if (!views.Any())
			{
				throw new TaskCanceledException();
			}

			var tasks = new List<Task>();

			foreach (var view in views)
			{
				var task = view.DismissOptions(options, selectedOptionIndex);
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);

			SafeDisposeInternalTokenSource();
		}

		#endregion IRunOptionsHandler

		#region ITransitionInHandler

		[YarnCommand("transition_in_group")]
		public async Task TransitionIn() => await TransitionIn(CancellationToken.None);

		public async Task TransitionIn(CancellationToken token)
		{
			if (TransitionInEffect == null)
			{
				return;
			}

			await TransitionInEffect.Animate(this, token);
		}

		#endregion ITransitionInHandler

		#region ITransitionOutHandler

		[YarnCommand("transition_out_group")]
		public async Task TransitionOut() => await TransitionOut(CancellationToken.None);

		public async Task TransitionOut(CancellationToken token)
		{
			if (TransitionOutEffect == null)
			{
				return;
			}

			await TransitionOutEffect.Animate(this, token);
		}

		#endregion ITransitionOutHandler
	}
}
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace Yarn.GodotSharp.Views
{
	public partial class DialogueViewController : DialogueViewGroup
	{
		private Godot.Collections.Dictionary<string, Godot.Node> _dialogueViewMap = new();

		public Godot.Collections.Dictionary<string, Godot.Node> DialogueViewMap => _dialogueViewMap;

		#region Exports

		public string ActiveViewName { get; private set; } = string.Empty;

		#endregion Exports

		[Actions.YarnCommand("switch_to_view")]
		public void SwitchToView(string viewName)
		{
			ToggleViewVisibility(ActiveViewName, false);
			ToggleViewVisibility(viewName, true);
			ActiveViewName = viewName;
		}

		[Actions.YarnCommand("transition_to_view")]
		public async Task TransitionToView(string viewName)
		{
			await TransitionOutView(ActiveViewName);

			SwitchToView(viewName);

			await TransitionInView(viewName);
		}

		protected virtual async Task TransitionInView(string viewName)
		{
			if (!TryGetView(viewName, out var view))
			{
				return;
			}

			if (view is not ITransitionInHandler transitionInHandler)
			{
				return;
			}

			await transitionInHandler.TransitionIn(CancellationToken.None);
		}

		protected virtual async Task TransitionOutView(string viewName)
		{
			if (!TryGetView(viewName, out var view))
			{
				return;
			}

			if (view is not ITransitionOutHandler transitionOutHandler)
			{
				return;
			}

			await transitionOutHandler.TransitionOut(CancellationToken.None);
		}

		protected virtual void ToggleViewVisibility(string viewName, bool visible)
		{
			if (!TryGetView(viewName, out var view))
			{
				return;
			}

			if (view is not CanvasItem canvasItem)
			{
				return;
			}

			canvasItem.Visible = visible;
		}

		protected virtual bool TryGetView(string viewName, out Godot.Node view)
		{
			if (!string.IsNullOrEmpty(viewName))
			{
				view = default;
				return false;
			}

			return DialogueViewMap.TryGetValue(viewName, out view);
		}

		protected virtual void RefreshDialogueViewDict()
		{
			DialogueViewMap.Clear();
			foreach (var view in DialogueViews)
			{
				if (view == null)
				{
					GD.PushError("RefreshDialogueViewDict: view == null");
					continue;
				}

				DialogueViewMap[view.Name] = view;
			}
		}

		#region Godot.Node

		public override void _Ready()
		{
			base._Ready();

			if (DialogueViews.Count == 0)
			{
				GD.PushError("DialogueViewGroupController._Ready: DialogueViews.Count == 0");
				return;
			}

			// hide all views
			foreach (var view in DialogueViews)
			{
				if (view is not CanvasItem canvasItem)
				{
					continue;
				}

				canvasItem.Hide();
			}

			// switch to the first view in the list
			var startView = DialogueViews.FirstOrDefault();
			if (startView == null)
			{
				GD.PushError("DialogueViewGroupController._Ready: DialogueViews.Count == 0");
				return;
			}

			SwitchToView(startView.Name);
		}

		#endregion Godot.Node

		#region DialogueViewGroup

		public override void SetDialougeViews(Array<Godot.Node> value)
		{
			base.SetDialougeViews(value);
			RefreshDialogueViewDict();
		}

		public override IEnumerable<Godot.Node> GetActiveDialogueViews()
		{
			if (string.IsNullOrEmpty(ActiveViewName))
			{
				GD.PushError("GetActiveDialogueViews: string.IsNullOrEmpty(ActiveView)");
				return Enumerable.Empty<Godot.Node>();
			}

			if (!DialogueViewMap.TryGetValue(ActiveViewName, out var dialogueView))
			{
				GD.PushError($"GetActiveDialogueViews: !DialogueViewDict.TryGetValue '{ActiveViewName}'");
				return Enumerable.Empty<Godot.Node>();
			}

			return new Godot.Node[] { dialogueView };
		}

		#endregion DialogueViewGroup
	}
}
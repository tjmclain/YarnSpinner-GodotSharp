using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace Yarn.GodotSharp.Views
{
	public partial class DialogueViewSwitchGroup : DialogueViewGroup
	{
		private Godot.Collections.Dictionary<string, Godot.Node> _dialogueViewDict = new();

		public Godot.Collections.Dictionary<string, Godot.Node> DialogueViewDict => _dialogueViewDict;

		#region Exports

		[Export]
		public string ActiveViewName { get; set; } = string.Empty;

		#endregion Exports

		[Actions.Command("switch_to_view")]
		public void SwitchToView(string viewName)
		{
			TryToggleViewVisibility(ActiveViewName, false);
			TryToggleViewVisibility(viewName, true);
			ActiveViewName = viewName;
		}

		[Actions.Command("transition_to_view")]
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

		protected virtual bool TryToggleViewVisibility(string viewName, bool visible)
		{
			if (!TryGetView(viewName, out var view))
			{
				return false;
			}

			if (view is not CanvasItem canvasItem)
			{
				return false;
			}

			canvasItem.Visible = visible;
			return true;
		}

		protected virtual bool TryGetView(string viewName, out Godot.Node view)
		{
			if (!string.IsNullOrEmpty(viewName))
			{
				view = default;
				return false;
			}

			return DialogueViewDict.TryGetValue(viewName, out view);
		}

		protected virtual void RefreshDialogueViewDict()
		{
			DialogueViewDict.Clear();
			foreach (var view in DialogueViews)
			{
				if (view == null)
				{
					GD.PushError("RefreshDialogueViewDict: view == null");
					continue;
				}

				DialogueViewDict[view.Name] = view;
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

			if (string.IsNullOrEmpty(ActiveViewName))
			{
				foreach (var view in DialogueViews)
				{
					if (view == null)
					{
						GD.PushError("DialogueViewGroupController._Ready: view == null");
						continue;
					}

					ActiveViewName = view.Name;
					break;
				}
			}

			foreach (var view in DialogueViews)
			{
				if (view is not CanvasItem canvasItem)
				{
					GD.PushWarning("DialogueViewGroupController._Ready: view is not CanvasItem");
					continue;
				}

				canvasItem.Hide();
			}
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

			if (!DialogueViewDict.TryGetValue(ActiveViewName, out var dialogueView))
			{
				GD.PushError($"GetActiveDialogueViews: !DialogueViewDict.TryGetValue '{ActiveViewName}'");
				return Enumerable.Empty<Godot.Node>();
			}

			return new Godot.Node[] { dialogueView };
		}

		#endregion DialogueViewGroup
	}
}
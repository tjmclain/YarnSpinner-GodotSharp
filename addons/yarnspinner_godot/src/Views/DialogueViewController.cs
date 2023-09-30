using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace Yarn.GodotSharp.Views
{
	public partial class DialogueViewController : DialogueViewGroup
	{
		private Array<Godot.Node> _dialogueViews = new();

		#region Exports

		[Export]
		public string ActiveView { get; set; } = string.Empty;

		[Export]
		public override Array<Godot.Node> DialogueViews
		{
			get => _dialogueViews;
			set
			{
				_dialogueViews = value;
				RefreshDialogueViewDict();
			}
		}

		#endregion Exports

		protected Godot.Collections.Dictionary<string, Godot.Node> DialogueViewDict { get; set; } = new();

		#region Godot.Node

		public override void _Ready()
		{
			base._Ready();

			RefreshDialogueViewDict();

			if (DialogueViews.Count == 0)
			{
				GD.PushError("DialogueViewGroupController._Ready: DialogueViews.Count == 0");
				return;
			}

			if (string.IsNullOrEmpty(ActiveView) || !DialogueViewDict.ContainsKey(ActiveView))
			{
				foreach (var view in DialogueViews)
				{
					if (view == null)
					{
						GD.PushError("DialogueViewGroupController._Ready: view == null");
						continue;
					}

					ActiveView = view.Name;
					break;
				}
			}
		}

		#endregion Godot.Node

		protected override IEnumerable<Godot.Node> GetActiveDialogueViews()
		{
			if (string.IsNullOrEmpty(ActiveView))
			{
				GD.PushError("DialogueViewGroupController.GetActiveDialogueViews: string.IsNullOrEmpty(ActiveView)");
				return Enumerable.Empty<Godot.Node>();
			}

			if (!DialogueViewDict.TryGetValue(ActiveView, out var dialogueView))
			{
				GD.PushError($"DialogueViewGroupController.GetActiveDialogueViews: !DialogueViewDict.TryGetValue '{ActiveView}'");
				return Enumerable.Empty<Godot.Node>();
			}

			return new Godot.Node[] { dialogueView };
		}

		protected virtual void RefreshDialogueViewDict()
		{
			DialogueViewDict.Clear();
			foreach (var view in DialogueViews)
			{
				DialogueViewDict[view.Name] = view;
			}
		}
	}
}
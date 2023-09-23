using Godot;

namespace Yarn.GodotSharp.Views
{
	[GlobalClass]
	public partial class OptionView : Control
	{
		[Signal]
		public delegate void OptionSelectedEventHandler(int optionIndex);

		[Export]
		public RichTextLabel Label { get; set; }

		[Export]
		public BaseButton Button { get; set; }

		protected int OptionIndex { get; private set; }

		public override void _Ready()
		{
			base._Ready();

			if (Button == null)
			{
				GD.PushError("Button == null");
				return;
			}

			Button.Pressed -= OnButtonPressed;
			Button.Pressed += OnButtonPressed;
		}

		public virtual void SetOption(DialogueOption option, int optionIndex)
		{
			if (Label == null)
			{
				GD.PushError("Label == null");
				return;
			}

			if (option == null)
			{
				GD.PushError("option == null");
				return;
			}

			if (option.Line == null)
			{
				GD.PushError("option.Line == null");
				return;
			}

			Label.Text = option.Line.Text.Text;
			OptionIndex = optionIndex;
		}

		protected virtual void OnButtonPressed()
		{
			EmitSignal(SignalName.OptionSelected, OptionIndex);
		}
	}
}

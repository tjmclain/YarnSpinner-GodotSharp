using Godot;

namespace Yarn.GodotSharp.Views
{
	[GlobalClass]
	public partial class OptionView : Button
	{
		[Signal]
		public delegate void OptionSelectedEventHandler(int optionIndex);

		protected int OptionIndex { get; private set; }

		public virtual void SetOption(DialogueOption option, int optionIndex)
		{
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

			Text = option.Line.Text.Text;
			OptionIndex = optionIndex;

			GD.Print($"SetOption ({optionIndex}): {Text}");
		}

		public override void _Pressed()
		{
			EmitSignal(SignalName.OptionSelected, OptionIndex);
		}
	}
}
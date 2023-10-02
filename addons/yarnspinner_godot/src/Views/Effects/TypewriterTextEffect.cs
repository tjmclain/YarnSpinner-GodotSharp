using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Views.Effects
{
	[GlobalClass]
	public partial class TypewriterTextEffect : TextEffect
	{
		[Export]
		public int CharactersPerSecond { get; set; } = 60;

		public override async Task Animate(RichTextLabel label, CancellationToken token)
		{
			if (label == null)
			{
				GD.PrintErr("TypewriterTextEffect.Animate: label == null");
				return;
			}

			if (token.IsCancellationRequested)
			{
				return;
			}

			float secondsPerCharacter = 1f / CharactersPerSecond;
			int ms = Convert.ToInt32(secondsPerCharacter * 1000);

			label.SetDeferred(RichTextLabel.PropertyName.VisibleCharacters, 0);

			await GodotUtility.WaitForProcessFrame();

			int count = label.GetTotalCharacterCount();
			GD.Print($"TypewriterTextEffect.Animate: label.GetTotalCharacterCount = {count}");

			for (int i = 0; i < count; i++)
			{
				label.SetDeferred(RichTextLabel.PropertyName.VisibleCharacters, i + 1);
				await Task.Delay(ms, token);
			}

			label.SetDeferred(RichTextLabel.PropertyName.VisibleCharacters, -1);
		}

		public override void CancelAnimation(RichTextLabel label)
		{
			if (label == null)
			{
				GD.PrintErr("TypewriterTextEffect.CancelAnimation: label == null");
				return;
			}

			label.VisibleCharacters = -1;
		}
	}
}
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
				GD.PrintErr("label == null");
				return;
			}

			var sceneTree = label.GetTree();
			if (sceneTree == null)
			{
				GD.PrintErr("label.GetTree == null");
				return;
			}

			if (token.IsCancellationRequested)
			{
				//token.ThrowIfCancellationRequested();
				return;
			}

			float secondsPerCharacter = 1f / CharactersPerSecond;
			int ms = Convert.ToInt32(secondsPerCharacter * 1000);

			label.SetDeferred(RichTextLabel.PropertyName.VisibleCharacters, 0);

			await ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);

			int count = label.GetTotalCharacterCount();
			GD.Print($"TypewriterTextEffect.Animate: label.GetTotalCharacterCount = {count}");

			for (int i = 0; i < count; i++)
			{
				if (token.IsCancellationRequested)
				{
					label.SetDeferred(RichTextLabel.PropertyName.VisibleCharacters, -1);
					//token.ThrowIfCancellationRequested();
					return;
				}

				label.SetDeferred(RichTextLabel.PropertyName.VisibleCharacters, i + 1);

				await Task.Delay(ms, token);
			}

			label.SetDeferred(RichTextLabel.PropertyName.VisibleCharacters, -1);
		}
	}
}

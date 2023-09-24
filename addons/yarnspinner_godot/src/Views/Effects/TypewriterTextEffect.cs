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
				throw new ArgumentNullException(nameof(label));
			}

			if (token.IsCancellationRequested)
			{
				token.ThrowIfCancellationRequested();
			}

			float secondsPerCharacter = 1f / CharactersPerSecond;
			int ms = Convert.ToInt32(secondsPerCharacter * 1000);

			label.VisibleCharacters = 0;
			int count = label.GetTotalCharacterCount();
			while (label.VisibleCharacters > count)
			{
				label.VisibleCharacters++;
				await Task.Delay(ms, token);

				if (token.IsCancellationRequested)
				{
					label.VisibleCharacters = -1;
				}
			}

			label.VisibleCharacters = -1;
		}
	}
}

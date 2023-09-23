using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Views.Effects
{
	[GlobalClass]
	public partial class TypewriterTextEffect : TextEffect
	{
		[Export]
		public int CharactersPerSecond { get; set; } = 60;

		public override async Task Animate(RichTextLabel label)
		{
			if (label == null)
			{
				GD.PushError("label == null");
				return;
			}

			float secondsPerCharacter = 1f / CharactersPerSecond;
			int ms = Convert.ToInt32(secondsPerCharacter * 1000);

			label.VisibleCharacters = 0;
			int count = label.GetTotalCharacterCount();
			while (label.VisibleCharacters > count)
			{
				label.VisibleCharacters++;
				await Task.Delay(ms);
			}

			label.VisibleCharacters = -1;
		}

		public override void Interrupt(RichTextLabel label)
		{
			if (label == null)
			{
				return;
			}

			label.VisibleCharacters = -1;
		}
	}
}

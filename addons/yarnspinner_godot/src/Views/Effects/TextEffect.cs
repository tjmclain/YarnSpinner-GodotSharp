using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Views.Effects
{
	[GlobalClass]
	public partial class TextEffect : Resource
	{
		public virtual async Task Animate(RichTextLabel label, CancellationToken token)
		{
			GD.PushWarning(
				$"{nameof(TextEffect)} does nothing by default. ",
				$"Override {nameof(Animate)} in a subclass."
			);

			await Task.CompletedTask;
		}
	}
}
using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Views.Effects
{
	[GlobalClass]
	public partial class TextEffect : Resource
	{
		public virtual Task Animate(RichTextLabel label, CancellationToken token)
		{
			throw new NotImplementedException($"{nameof(TextEffect)} does nothing by default. Override {nameof(Animate)} in a subclass.");
		}
	}
}

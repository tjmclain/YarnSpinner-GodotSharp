using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Views.Effects
{
	[GlobalClass]
	public partial class ControlEffect : Resource
	{
		public virtual async Task Animate(Control control, CancellationToken token)
		{
			GD.PushWarning(
				$"{nameof(ControlEffect)} does nothing by default. ",
				$"Override {nameof(Animate)} in a subclass."
			);

			await Task.CompletedTask;
		}
	}
}
using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Views.Effects
{
	[GlobalClass]
	public partial class FadeEffect : Resource
	{
		[Export(PropertyHint.Range, "0,1")]
		public float TargetAlpha { get; set; } = 1f;

		[Export(PropertyHint.Range, "0,or_greater")]
		public double Duration { get; set; } = 0.5;

		[Export]
		public Tween.TransitionType TransitionType { get; set; } = Tween.TransitionType.Linear;

		public virtual async Task Animate(Control control, CancellationToken token)
		{
			if (control == null)
			{
				GD.PushError("FadeEffect.Animate: control == null");
			}

			var modulate = new Color(control.Modulate, TargetAlpha);

			var tcs = new TaskCompletionSource<bool>();
			token.Register(() => tcs.SetResult(false));

			// we want to change the alpha value of 'modulate'
			// https://docs.godotengine.org/en/stable/classes/class_canvasitem.html#class-canvasitem-property-modulate
			var tween = control.GetTree().CreateTween();
			tween.TweenProperty(control, new NodePath(CanvasItem.PropertyName.Modulate), modulate, Duration);
			tween.TweenCallback(Callable.From(() => tcs.SetResult(true)));

			await tcs.Task;

			if (token.IsCancellationRequested)
			{
				GD.Print("FadeEffect.Animate: token.IsCancellationRequested == true");
				tween?.CallDeferred(Tween.MethodName.Stop);
				tween?.CallDeferred(GodotObject.MethodName.Free);
			}

			control?.CallDeferred(CanvasItem.MethodName.SetModulate, modulate);
		}
	}
}
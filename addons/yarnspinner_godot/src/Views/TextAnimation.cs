using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Views
{
	public abstract partial class TextAnimation : Resource
	{
		public virtual async Task Animate(RichTextLabel label)
		{
			await Task.CompletedTask;
		}

		public virtual void InterruptAnimation(RichTextLabel label)
		{
		}
	}
}

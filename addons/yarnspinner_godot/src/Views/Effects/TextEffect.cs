using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Views.Effects
{
	public abstract partial class TextEffect : Resource
	{
		public virtual async Task Animate(RichTextLabel label)
		{
			await Task.CompletedTask;
		}

		public virtual void Interrupt(RichTextLabel label)
		{
		}
	}
}

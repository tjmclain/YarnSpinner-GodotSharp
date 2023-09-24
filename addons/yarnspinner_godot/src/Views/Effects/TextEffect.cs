using System;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp.Views.Effects
{
	public partial class TextEffect : Resource
	{
		public virtual Task Animate(RichTextLabel label)
		{
			throw new NotImplementedException();
		}

		public virtual void Interrupt(RichTextLabel label)
		{
			throw new NotImplementedException();
		}
	}
}

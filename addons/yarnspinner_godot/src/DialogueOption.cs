using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp
{
	public partial class DialogueOption : RefCounted
	{
		/// <summary>
		/// The ID of this dialogue option
		/// </summary>
		public int DialogueOptionID;

		/// <summary>
		/// The ID of the dialogue option's text
		/// </summary>
		public string TextID;

		/// <summary>
		/// The line for this dialogue option
		/// </summary>
		public LocalizedLine Line;

		/// <summary>
		/// Indicates whether this value should be presented as available or not.
		/// </summary>
		public bool IsAvailable;

		public DialogueOption()
		{
		}

		public DialogueOption(OptionSet.Option option, LocalizedLine localizedLine)
		{
			TextID = option.Line.ID;
			DialogueOptionID = option.ID;
			Line = localizedLine;
			IsAvailable = option.IsAvailable;
		}
	}
}
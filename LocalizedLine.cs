using System;
using Godot;

namespace Yarn.Godot
{
	[Serializable]
	public partial class LocalizedLine : RefCounted
	{
		/// <summary>
		/// DialogueLine's ID
		/// </summary>
		[Export]
		public string TextID = "";

		/// <summary>
		/// DialogueLine's inline expression's substitution
		/// </summary>
		[Export]
		public string[] Substitutions = Array.Empty<string>();

		/// <summary>
		/// DialogueLine's text
		/// </summary>
		[Export]
		public string RawText = "";

		/// <summary>
		/// Any metadata associated with this line.
		/// </summary>
		[Export]
		public string[] Metadata = Array.Empty<string>();

		/// <summary>
		/// The name of the character, if present.
		/// </summary>
		/// <remarks>
		/// This value will be <see langword="null"/> if the line does not have a character name.
		/// </remarks>
		public string CharacterName
		{
			get
			{
				if (Text.TryGetAttributeWithName("character", out var characterNameAttribute))
				{
					if (characterNameAttribute.Properties.TryGetValue("name", out var value))
					{
						return value.StringValue;
					}
				}
				return null;
			}
		}

		/// <summary>
		/// The asset associated with this line, if any.
		/// </summary>
		public Resource Asset;

		/// <summary>
		/// The underlying <see cref="Yarn.Markup.MarkupParseResult"/> for this line.
		/// </summary>
		public Markup.MarkupParseResult Text { get; set; }

		/// <summary>
		/// The underlying <see cref="Yarn.Markup.MarkupParseResult"/> for this line, with any
		/// `character` attribute removed.
		/// </summary>
		/// <remarks>
		/// If the line has no `character` attribute, this method returns the same value as <see cref="Text"/>.
		/// </remarks>
		public Markup.MarkupParseResult TextWithoutCharacterName
		{
			get
			{
				// If a 'character' attribute is present, remove its text
				if (Text.TryGetAttributeWithName("character", out var characterNameAttribute))
				{
					return Text.DeleteRange(characterNameAttribute);
				}
				else
				{
					return Text;
				}
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotSharp
{
	public partial class StringTableEntry : Resource
	{
		/// <summary>
		/// The language that the line is written in.
		/// </summary>
		[Export]
		public string Language;

		/// <summary>
		/// The line ID for this line. This value will be the same across
		/// all localizations.
		/// </summary>
		[Export]
		public string Id;

		/// <summary>
		/// The text of this line, in the language specified by <see
		/// cref="Language"/>.
		/// </summary>
		[Export]
		public string Text;

		/// <summary>
		/// The name of the Yarn script in which this line was originally
		/// found.
		/// </summary>
		[Export]
		public string File;

		/// <summary>
		/// The name of the node in which this line was originally found.
		/// </summary>
		/// <remarks>
		/// This node can be found in the file indicated by <see
		/// cref="File"/>.
		/// </remarks>
		[Export]
		public string Node;

		/// <summary>
		/// The line number in the file indicated by <see cref="File"/> at
		/// which the original version of this line can be found.
		/// </summary>
		[Export]
		public string LineNumber;

		/// <summary>
		/// A string used as part of a mechanism for checking if translated
		/// versions of this string are out of date.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This field contains the first 8 characters of the SHA-256 hash of
		/// the line's text as it appeared in the base localization CSV file.
		/// </para>
		/// <para>
		/// When a new StringTableEntry is created in a localized CSV file for a
		/// .yarn file, the Lock value is copied over from the base CSV file,
		/// and used for the translated entry. 
		/// </para>
		/// <para>
		/// Because the base localization CSV is regenerated every time the
		/// .yarn file is imported, the base localization Lock value will change
		/// if a line's text changes. This means that if the base lock and
		/// translated lock differ, the translated line is out of date, and
		/// needs to be updated.
		/// </para>
		/// </remarks>
		[Export]
		public string Lock;

		/// <summary>
		/// A comment used to describe this line to translators.
		/// </summary>
		[Export]
		public string Comment;

		/// <summary>
		/// Additional metadata included in this line.
		/// </summary>
		[Export]
		public string[] MetaData = Array.Empty<string>();
	}
}

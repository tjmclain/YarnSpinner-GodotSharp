#if TOOLS

using System.IO;
using Godot;

namespace Yarn.GodotSharp.Editor.Importers
{
	public class ImporterStringSubstitutions : System.Collections.Generic.Dictionary<string, string>
	{
		public static class ReplacementPatterns
		{
			public const string SourceDir = "$(SourceDir)";
			public const string SourceFileName = "$(SourceFileName)";
		}

		public ImporterStringSubstitutions(string sourceFile)
		{
			sourceFile = ProjectSettings.GlobalizePath(sourceFile);
			this[ReplacementPatterns.SourceDir] = Path.GetDirectoryName(sourceFile);
			this[ReplacementPatterns.SourceFileName] = Path.GetFileNameWithoutExtension(sourceFile);
		}

		public string InterpolateString(string value)
		{
			foreach (var substitution in this)
			{
				value = value.Replace(substitution.Key, substitution.Value);
			}

			return value;
		}
	}
}

#endif

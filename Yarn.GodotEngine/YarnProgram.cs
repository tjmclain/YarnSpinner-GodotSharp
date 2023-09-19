using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Yarn.Compiler;

namespace Yarn.GodotEngine
{
	[Tool]
	[GlobalClass, Icon("res://addons/yarnspinner_godot/icons/YarnScriptIcon.png")]
	public partial class YarnProgram : Resource
	{
		[Export(PropertyHint.File)]
		public string SourceFile { get; set; }

		// Do I need to cache this here?
		[Export(PropertyHint.File)]
		public string TranslationsFile { get; set; } = string.Empty;

		[Export]
		public Godot.Collections.Array<Variable> Declarations { get; set; } = new();

		[Export]
		public Godot.Collections.Dictionary<string, StringTableEntry> StringTable { get; set; } = new();

		[Export]
		public string[] Errors { get; set; } = Array.Empty<string>();

		public Error CompileSourceFile()
		{
			static IEnumerable<string> RemoveLineIdFromMetadata(string[] metadata)
			{
				return metadata.Where(x => !x.StartsWith("line:"));
			}

			static string GenerateCommentWithLineMetadata(string[] metadata)
			{
				var cleanedMetadata = RemoveLineIdFromMetadata(metadata);
				return cleanedMetadata.Any()
					? $"Line metadata: {string.Join(" ", cleanedMetadata)}"
					: string.Empty;
			}

			if (string.IsNullOrEmpty(SourceFile))
			{
				GD.PushError("string.IsNullOrEmpty(_sourceFile)");
				return Error.InvalidData;
			}

			// Open file
			using var file = FileAccess.Open(SourceFile, FileAccess.ModeFlags.Read);
			if (file == null)
			{
				var fileError = FileAccess.GetOpenError();
				GD.PushError($"!FileAccess.Open '{SourceFile}'; error = {fileError}");
				return fileError;
			}

			// Compile text
			string text = file.GetAsText();

			var job = CompilationJob.CreateFromString(SourceFile, text);
			var compilation = Compiler.Compiler.Compile(job);
			var errors = compilation.Diagnostics.Where(
				x => x.Severity == Diagnostic.DiagnosticSeverity.Error
			);

			// Import errors
			foreach (var error in errors)
			{
				GD.PushError(error.Message);
			}

			Errors = errors.Select(x => x.Message).ToArray();

			if (errors.Any())
			{
				return Error.InvalidData;
			}

			// Import variable declarations
			var declarations = new List<Variable>();
			foreach (var declaration in compilation.Declarations)
			{
				if (declaration.Name.StartsWith("$Yarn.Internal."))
				{
					continue;
				}

				if (declaration.Type is FunctionType)
				{
					continue;
				}

				if (!Variable.TryCreateFromDeclaration(declaration, out var variable))
				{
					continue;
				}

				declarations.Add(variable);
			}

			Declarations = new(declarations);

			// Import string table entries
			var stringTable = compilation.StringTable
				.Select(x =>
				{
					return new StringTableEntry()
					{
						Id = x.Key,
						Text = x.Value.text,
						File = x.Value.fileName,
						Node = x.Value.nodeName,
						LineNumber = x.Value.lineNumber.ToString(),
						Lock = GetHashString(x.Value.text, 8),
						Comment = GenerateCommentWithLineMetadata(x.Value.metadata),
						MetaData = RemoveLineIdFromMetadata(x.Value.metadata).ToArray()
					};
				})
				.ToDictionary(x => x.Id, x => x);

			StringTable = new(stringTable);

			GD.Print($"Compiled yarn program from source '{SourceFile}'");

			return Error.Ok;
		}

		/// <summary>
		/// Returns a byte array containing a SHA-256 hash of <paramref name="inputString"/>.
		/// </summary>
		/// <param name="inputString">The string to produce a hash value for.</param>
		/// <returns>The hash of <paramref name="inputString"/>.</returns>
		private static string GetHashString(string inputString, int limitCharacters = -1)
		{
			static byte[] GetHash(string inputString)
			{
				using var algorithm = System.Security.Cryptography.SHA256.Create();
				return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
			}

			var sb = new StringBuilder();
			foreach (byte b in GetHash(inputString))
			{
				sb.Append(b.ToString("x2"));
			}

			if (limitCharacters == -1)
			{
				// Return the entire string
				return sb.ToString();
			}
			else
			{
				// Return a substring (or the entire string, if limitCharacters is longer than the string)
				return sb.ToString(0, Mathf.Min(sb.Length, limitCharacters));
			}
		}
	}
}

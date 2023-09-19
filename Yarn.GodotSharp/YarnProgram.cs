using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Yarn.Compiler;

namespace Yarn.GodotSharp
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
		public Godot.Collections.Array<StringTableEntry> StringTable { get; set; } = new();

		[Export]
		public string[] Errors { get; set; } = Array.Empty<string>();

		public Error CompileSourceFile()
		{
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
				.Select(x => new StringTableEntry(x.Key, x.Value));

			StringTable = new(stringTable);

			GD.Print($"Compiled yarn program from source '{SourceFile}'");

			return Error.Ok;
		}
	}
}

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
		public string SourceFile { get; set; } = string.Empty;

		// Do I need to cache this here?
		[Export(PropertyHint.File)]
		public string StringTableFile { get; set; } = string.Empty;

		public static Error Compile(string sourceFile, out CompilationResult compilationResult)
		{
			compilationResult = default;

			if (string.IsNullOrEmpty(sourceFile))
			{
				GD.PushError("string.IsNullOrEmpty(_sourceFile)");
				return Error.InvalidData;
			}

			// Open file
			using var file = FileAccess.Open(sourceFile, FileAccess.ModeFlags.Read);
			if (file == null)
			{
				var fileError = FileAccess.GetOpenError();
				GD.PushError($"!FileAccess.Open '{sourceFile}'; error = {fileError}");
				return fileError;
			}

			// Compile text
			string text = file.GetAsText();

			var job = CompilationJob.CreateFromString(sourceFile, text);
			compilationResult = Compiler.Compiler.Compile(job);

			var errors = compilationResult.Diagnostics
				.Where(x => x.Severity == Diagnostic.DiagnosticSeverity.Error);

			foreach (var error in errors)
			{
				GD.PushError($"{error.Message} ({error.FileName} [{error.Range.Start.Line}])");
			}

			if (errors.Any())
			{
				return Error.InvalidData;
			}

			GD.Print($"Compiled yarn program from source '{sourceFile}'");

			return Error.Ok;
		}
	}
}
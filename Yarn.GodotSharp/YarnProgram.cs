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
		#region Properties

		// Do I need to cache this here?
		[Export(PropertyHint.File)]
		public string TranslationsFile { get; set; } = string.Empty;

		[Export]
		public string[] Errors { get; set; } = Array.Empty<string>();

		#endregion Properties

		#region Public Methods

		public Error Compile(out CompilationResult compilationResult)
			=> Compile(ResourcePath, out compilationResult);

		public virtual Error Compile(string sourceFile, out CompilationResult compilationResult)
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
			var errors = compilationResult.Diagnostics.Where(
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

			GD.Print($"Compiled yarn program from source '{ResourcePath}'");

			return Error.Ok;
		}

		#endregion Public Methods
	}
}

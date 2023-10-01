using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Godot;
using Yarn.Compiler;

namespace Yarn.GodotSharp
{
	using NodeHeaders = Dictionary<string, List<string>>;
	using FileAccess = Godot.FileAccess;

	// https://yarnspinner.dev/docs/unity/components/yarn-programs/
	[GlobalClass, Icon("res://addons/yarnspinner_godot/icons/YarnProjectIcon.png")]
	public partial class YarnProject : Resource
	{
		private Program _program = new();

		private readonly Dictionary<string, NodeHeaders> _nodeHeaders = new();

		[Export]
		public Godot.Collections.Array<YarnProgram> Programs { get; set; } = new();

		[Export]
		public byte[] CompiledProgram { get; set; } = Array.Empty<byte>();

		[Export]
		public StringTable StringTable { get; set; } = new();

		/// <summary>
		/// Gets the Yarn Program stored in this project.
		/// </summary>
		/// <remarks>
		/// The first time this is called, the program stored in <see cref="CompiledYarnProgram"/>
		/// is deserialized and cached. Future calls to this method will return the cached value.
		/// </remarks>
		public Program Program => _program ?? CompileProgram();

		/// <summary>
		/// The names of all nodes contained within the <see cref="Program"/>.
		/// </summary>
		public string[] NodeNames => Program != null
					? Program.Nodes.Keys.ToArray()
					: Array.Empty<string>();

		/// <summary>
		/// Gets the headers for the requested node.
		/// </summary>
		/// <remarks>
		/// The first time this is called, the values are extracted from <see cref="Program"/> and
		/// cached inside <see cref="_nodeHeaders"/>. Future calls will then return the cached values.
		/// </remarks>
		public virtual NodeHeaders GetHeaders(string nodeName)
		{
			// if the headers have already been extracted just return that
			if (_nodeHeaders.TryGetValue(nodeName, out var existingValues))
			{
				return existingValues;
			}

			// headers haven't been extracted so we look inside the program
			if (!Program.Nodes.TryGetValue(nodeName, out Node rawNode))
			{
				return new NodeHeaders();
			}

			var rawHeaders = rawNode.Headers;

			// this should NEVER happen because there will always be at least the title, right?
			if (rawHeaders == null || rawHeaders.Count == 0)
			{
				return new NodeHeaders();
			}

			// ok so this is an array of (string, string) tuples with potentially duplicated keys
			// inside the array we'll convert it all into a dict of string arrays
			var headers = new NodeHeaders();
			foreach (var pair in rawHeaders)
			{
				if (headers.TryGetValue(pair.Key, out List<string> values))
				{
					values.Add(pair.Value);
				}
				else
				{
					values = new List<string> { pair.Value };
				}
				headers[pair.Key] = values;
			}

			_nodeHeaders[nodeName] = headers;
			return headers;
		}

		/// <summary>
		/// Returns a node with the specified name.
		/// </summary>
		public virtual Node GetNode(string nodeName)
		{
			return Program.Nodes.TryGetValue(nodeName, out Node node) ? node : null;
		}

		public virtual Program CompileProgram()
		{
			return Engine.IsEditorHint()
				? CompileProgramFromScriptFiles()
				: ParseProgramFromCachedData();
		}

		protected virtual Program CompileProgramFromScriptFiles()
		{
			_nodeHeaders.Clear();

			if (Programs.Count == 0)
			{
				GD.PushError("CompileProgramFromScriptFiles: Programs.Count == 0");
				return _program;
			}

			// Compile program
			var filePaths = Programs
				.Select(x => x.SourceFile)
				.Select(x => ProjectSettings.GlobalizePath(x));

			var job = CompilationJob.CreateFromFiles(filePaths);
			var compilationResult = Compiler.Compiler.Compile(job);

			var errorSeverity = Diagnostic.DiagnosticSeverity.Error;
			var errors = compilationResult.Diagnostics.Where(x => x.Severity == errorSeverity);

			if (errors.Any())
			{
				foreach (var error in errors)
				{
					GD.PushError($"CompileProgramFromScriptFiles: {error.Message} ({error.FileName})");
				}
				return _program;
			}

			_program = compilationResult.Program;

			using (var memoryStream = new MemoryStream())
			using (var outputStream = new Google.Protobuf.CodedOutputStream(memoryStream))
			{
				// Serialize the compiled program to memory
				compilationResult.Program.WriteTo(outputStream);
				outputStream.Flush();

				CompiledProgram = memoryStream.ToArray();
			}

			// Populate string table
			StringTable ??= new StringTable();
			StringTable.Clear();
			StringTable.CreateEntriesFrom(compilationResult.StringTable);

			// Create a list of unique string table file paths
			var stringTableFiles = new HashSet<string>();
			foreach (var program in Programs)
			{
				if (string.IsNullOrEmpty(program.StringTableFile))
				{
					continue;
				}

				stringTableFiles.Add(program.StringTableFile);
			}

			// load and merge individual string tables into our central table
			foreach (string file in stringTableFiles)
			{
				if (!FileAccess.FileExists(file))
				{
					GD.PushWarning($"!FileAccess.FileExists '{file}'");
					continue;
				}

				if (ResourceLoader.Load(file) is not StringTable stringTable)
				{
					GD.PushWarning($"Could not load StringTable at '{file}'");
					continue;
				}

				StringTable.MergeFrom(stringTable);
			}

			return _program;
		}

		protected virtual Program ParseProgramFromCachedData()
		{
			_nodeHeaders.Clear();

			if (CompiledProgram.Length == 0)
			{
				GD.PushWarning(
					$"{nameof(ParseProgramFromCachedData)}: " +
					$"{nameof(CompiledProgram)}.Length == 0; ",
					$"fallback to {nameof(CompileProgramFromScriptFiles)}"
				);
				return CompileProgramFromScriptFiles();
			}

			_program = Program.Parser.ParseFrom(CompiledProgram);
			return _program;
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Yarn.GodotSharp
{
	using NodeHeaders = Dictionary<string, List<string>>;

	// https://yarnspinner.dev/docs/unity/components/yarn-programs/
	[GlobalClass, Icon("res://addons/yarnspinner_godot/icons/YarnProjectIcon.png")]
	public partial class YarnProject : Resource
	{
		private Program _program = null;

		private readonly Dictionary<string, NodeHeaders> _nodeHeaders = new();

		[Export]
		public Godot.Collections.Array<YarnProgram> Programs { get; set; } = new();

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
		public NodeHeaders GetHeaders(string nodeName)
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
		public Node GetNode(string nodeName)
		{
			return Program.Nodes.TryGetValue(nodeName, out Node node) ? node : null;
		}

		// TODO: allow this work to be done offline
		public Program CompileProgram()
		{
			if (Programs.Count == 0)
			{
				GD.PushError("CompileProgram: Programs.Count == 0");
				_program = new Program();
				return _program;
			}

			// Compile program
			var filePaths = Programs
				.Select(x => x.SourceFile)
				.Select(x => ProjectSettings.GlobalizePath(x));

			var job = Compiler.CompilationJob.CreateFromFiles(filePaths);
			var compilation = Compiler.Compiler.Compile(job);

			_program = compilation.Program;

			// Populate string table
			StringTable ??= new StringTable();
			StringTable.Clear();
			StringTable.CreateEntriesFrom(compilation.StringTable);

			foreach (var program in Programs)
			{
				if (string.IsNullOrEmpty(program.StringTableFile))
				{
					continue;
				}

				var stringTable = ResourceLoader.Load<StringTable>(program.StringTableFile);
				if (stringTable == null)
				{
					GD.PushWarning($"Could not load StringTable at '{program.StringTableFile}'");
					continue;
				}

				StringTable.MergeTranslationsFrom(stringTable);
			}

			return _program;
		}
	}
}

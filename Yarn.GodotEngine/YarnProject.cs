using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Yarn.GodotEngine
{
	using NodeHeaders = Dictionary<string, List<string>>;

	// https://yarnspinner.dev/docs/unity/components/yarn-programs/
	[GlobalClass, Icon("res://Editor/Icons/Asset Icons/YarnProject Icon.png")]
	public partial class YarnProject : Resource
	{
		[Export]
		public byte[] CompiledYarnProgram { get; set; } = Array.Empty<byte>();

		[Export]
		public Godot.Collections.Dictionary<string, string[]> LineMetadata { get; set; } = new();

		private readonly Dictionary<string, NodeHeaders> _nodeHeaders = new();

		/// <summary>
		/// The cached result of deserializing <see cref="CompiledYarnProgram"/>.
		/// </summary>
		private Program _cachedProgram = null;

		/// <summary>
		/// The cached result of reading the default values from the <see cref="Program"/>.
		/// </summary>
		private Dictionary<string, IConvertible> _initialValues;

		/// <summary>
		/// Gets the Yarn Program stored in this project.
		/// </summary>
		/// <remarks>
		/// The first time this is called, the program stored in <see cref="CompiledYarnProgram"/>
		/// is deserialized and cached. Future calls to this method will return the cached value.
		/// </remarks>
		public Program Program
			=> _cachedProgram ??= Program.Parser.ParseFrom(CompiledYarnProgram);

		/// <summary>
		/// The names of all nodes contained within the <see cref="Program"/>.
		/// </summary>
		public string[] NodeNames => Program != null
					? Program.Nodes.Keys.ToArray()
					: Array.Empty<string>();

		/// <summary>
		/// The default values of all declared or inferred variables in the <see cref="Program"/>.
		/// Organised by their name as written in the yarn files.
		/// </summary>
		public Dictionary<string, IConvertible> GetInitialValues()
		{
			if (_initialValues != null)
			{
				return _initialValues;
			}

			_initialValues = new Dictionary<string, IConvertible>();

			foreach (var pair in Program.InitialValues)
			{
				var value = pair.Value;
				switch (value.ValueCase)
				{
					case Operand.ValueOneofCase.StringValue:
						{
							_initialValues[pair.Key] = value.StringValue;
							break;
						}
					case Operand.ValueOneofCase.BoolValue:
						{
							_initialValues[pair.Key] = value.BoolValue;
							break;
						}
					case Operand.ValueOneofCase.FloatValue:
						{
							_initialValues[pair.Key] = value.FloatValue;
							break;
						}
					default:
						{
							GD.PrintErr(
								$"{pair.Key} is of an invalid type: {value.ValueCase}"
							);
							break;
						}
				}
			}
			return _initialValues;
		}

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
	}
}

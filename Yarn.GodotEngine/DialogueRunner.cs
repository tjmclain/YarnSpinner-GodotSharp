using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Yarn.GodotEngine.LineProviders;
using Yarn.GodotEngine.Actions;

namespace Yarn.GodotEngine
{
	using Expression = System.Linq.Expressions.Expression;

	/// <summary>
	/// The DialogueRunner component acts as the interface between your game and Yarn Spinner.
	/// </summary>
	// https://yarnspinner.dev/docs/unity/components/dialogue-runner/
	public partial class DialogueRunner : Godot.Node
	{
		/// <summary>
		/// The underlying object that executes Yarn instructions and provides lines, options and commands.
		/// </summary>
		/// <remarks>Automatically created on first access.</remarks>
		private Dialogue _dialogue;

		private readonly Dictionary<string, CommandInfo> _commands = new();

		#region Signals
		/// <summary>
		/// A Unity event that is called when a node starts running.
		/// </summary>
		/// <remarks>
		/// This event receives as a parameter the name of the node that is about to start running.
		/// </remarks>
		/// <seealso cref="Dialogue.NodeStartHandler"/>
		[Signal]
		public delegate void OnNodeStartEventHandler(string nodeName);

		/// <summary>
		/// A Unity event that is called when a node is complete.
		/// </summary>
		/// <remarks>
		/// This event receives as a parameter the name of the node that just finished running.
		/// </remarks>
		/// <seealso cref="Dialogue.NodeCompleteHandler"/>
		[Signal]
		public delegate void OnNodeCompleteEventHandler(string nodeName);

		/// <summary>
		/// A Unity event that is called when the dialogue starts running.
		/// </summary>
		[Signal]
		public delegate void OnDialogueStartEventHandler();

		/// <summary>
		/// A Unity event that is called once the dialogue has completed.
		/// </summary>
		/// <seealso cref="Dialogue.DialogueCompleteHandler"/>
		[Signal]
		public delegate void OnDialogueCompleteEventHandler();

		/// <summary>
		/// A <see cref="StringUnityEvent"/> that is called when a <see cref="Command"/> is received.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Use this method to dispatch a command to other parts of your game. This method is only
		/// called if the <see cref="Command"/> has not been handled by a command handler that has
		/// been added to the <see cref="DialogueRunner"/>, or by a method on a <see
		/// cref="MonoBehaviour"/> in the scene with the attribute <see cref="YarnCommandAttribute"/>.
		/// </para>
		/// <para style="hint">
		/// When a command is delivered in this way, the <see cref="DialogueRunner"/> will not pause
		/// execution. If you want a command to make the DialogueRunner pause execution, see <see
		/// cref="AddCommandHandler(string, CommandHandler)"/>.
		/// </para>
		/// <para>
		/// This method receives the full text of the command, as it appears between the
		/// <c>&lt;&lt;</c> and <c>&gt;&gt;</c> markers.
		/// </para>
		/// </remarks>
		/// <seealso cref="AddCommandHandler(string, CommandHandler)"/>
		/// <seealso cref="AddCommandHandler(string, CommandHandler)"/>
		/// <seealso cref="YarnCommandAttribute"/>
		[Signal]
		public delegate void OnCommandEventHandler(string commandName);
		#endregion

		#region Exports
		[Export]
		public YarnProject YarnProject { get; private set; }

		[Export]
		public LineProviderBehaviour LineProvider { get; private set; }

		[Export]
		public VariableStorageBehaviour VariableStorage { get; private set; }

		[Export]
		public Godot.Collections.Array<DialogueViewBase> DialogueViews { get; set; } = new();

		[Export]
		public string StartNode { get; set; } = Dialogue.DefaultStartNodeName;

		[Export]
		public bool RunSelectedOptionAsLine { get; set; } = false;

		[Export(PropertyHint.None, "If true, will print GD.Print messages every time it enters a node, and other frequent events")]
		public bool VerboseLogging { get; set; }
		#endregion

		/// <summary>
		/// Gets a value that indicates if the dialogue is actively running.
		/// </summary>
		public bool IsDialogueRunning => Dialogue.IsActive;

		/// <summary>
		/// Gets the name of the current node that is being run.
		/// </summary>
		/// <seealso cref="Dialogue.currentNode"/>
		public string CurrentNodeName => Dialogue.CurrentNode;

		/// <summary>
		/// Gets the underlying <see cref="Dialogue"/> object that runs the Yarn code.
		/// </summary>
		public Dialogue Dialogue => _dialogue ??= CreateDialogueInstance();

		/// <summary>
		/// The <see cref="LocalizedLine"/> currently being displayed on the dialogue views.
		/// </summary>
		public LocalizedLine CurrentLine { get; private set; }

		#region Getter Methods
		/// <summary>
		/// Returns `true` when a node named `nodeName` has been loaded.
		/// </summary>
		/// <param name="nodeName">The name of the node.</param>
		/// <returns>`true` if the node is loaded, `false` otherwise/</returns>
		public bool NodeExists(string nodeName)
			=> Dialogue.NodeExists(nodeName);

		/// <summary>
		/// Returns the collection of tags that the node associated with the node named `nodeName`.
		/// </summary>
		/// <param name="nodeName">The name of the node.</param>
		/// <returns>
		/// The collection of tags associated with the node, or `null` if no node with that name exists.
		/// </returns>
		public IEnumerable<string> GetTagsForNode(string nodeName)
			=> Dialogue.GetTagsForNode(nodeName);
		#endregion

		#region Setter Methods
		/// <summary>
		/// Replaces this DialogueRunner's yarn project with the provided project.
		/// </summary>
		public void SetProject(YarnProject newProject)
		{
			YarnProject = newProject;

			Dialogue.SetProgram(newProject.Program);

			if (LineProvider != null)
			{
				LineProvider.YarnProject = newProject;
			}
		}

		/// <summary>
		/// Sets the dialogue views and makes sure the callback <see
		/// cref="DialogueViewBase.MarkLineComplete"/> will respond correctly.
		/// </summary>
		/// <param name="views">The array of views to be assigned.</param>
		public void SetDialogueViews(DialogueViewBase[] views)
		{
			DialogueViews.Clear();
			DialogueViews.AddRange(views);
		}

		/// <summary>
		/// Loads any initial variables declared in the program and loads that variable with its
		/// default declaration value into the variable storage. Any variable that is already in the
		/// storage will be skipped, the assumption is that this means the value has been overridden
		/// at some point and shouldn't be otherwise touched. Can force an override of the existing
		/// values with the default if that is desired.
		/// </summary>
		public void SetInitialVariables(bool overrideExistingValues = false)
		{
			if (YarnProject == null)
			{
				GD.PushError("Unable to set default values, there is no project set");
				return;
			}

			// grabbing all the initial values from the program and inserting them into the storage
			// we first need to make sure that the value isn't already set in the storage
			var values = YarnProject.Program.InitialValues;
			foreach (var pair in values)
			{
				if (!overrideExistingValues && VariableStorage.Contains(pair.Key))
				{
					continue;
				}
				var value = pair.Value;
				switch (value.ValueCase)
				{
					case Operand.ValueOneofCase.StringValue:
						{
							VariableStorage.SetValue(pair.Key, value.StringValue);
							break;
						}
					case Operand.ValueOneofCase.BoolValue:
						{
							VariableStorage.SetValue(pair.Key, value.BoolValue);
							break;
						}
					case Operand.ValueOneofCase.FloatValue:
						{
							VariableStorage.SetValue(pair.Key, value.FloatValue);
							break;
						}
					default:
						{
							GD.PushError($"{pair.Key} is of an invalid type: {value.ValueCase}");
							break;
						}
				}
			}
		}
		#endregion

		#region Godot.Node Methods
		public override void _EnterTree()
		{
			// Register Commands and Functions
			_commands.Clear();
			RegisterActionsInAssembly();

			// Create a line provider if we're missing one
			if (LineProvider == null)
			{
				LineProvider = new TextLineProvider();
				AddChild(LineProvider);

				// Let the user know what we're doing.
				if (VerboseLogging)
				{
					GD.Print($"Dialogue Runner has no LineProvider; creating a {typeof(TextLineProvider).Name}.", this);
				}
			}

			// Initialize our yarn project
			if (YarnProject != null)
			{
				if (Dialogue.IsActive)
				{
					GD.PushError($"DialogueRunner wanted to load a Yarn Project in its Start method, but the Dialogue was already running one. The Dialogue Runner may not behave as you expect.");
				}

				// Load this new Yarn Project.
				SetProject(YarnProject);
			}
		}
		#endregion

		#region Dialogue Control Methods
		/// <summary>
		/// Start the dialogue from a specific node.
		/// </summary>
		/// <param name="startNode">The name of the node to start running from.</param>
		public async void StartDialogue(string startNode)
		{
			// If the dialogue is currently executing instructions, then calling ContinueDialogue()
			// at the end of this method will cause confusing results. Report an error and stop here.
			if (Dialogue.IsActive)
			{
				GD.PushError($"Can't start dialogue from node {startNode}: the dialogue is currently in the middle of running. Stop the dialogue first.");
				return;
			}

			if (!IsNodeReady())
			{
				GD.Print("!IsNodeReady; await Ready signal");
				await ToSignal(this, Godot.Node.SignalName.Ready);
			}

			if (YarnProject.NodeNames.Contains(startNode) == false)
			{
				GD.Print($"Can't start dialogue from node {startNode}: the Yarn Project {YarnProject.ResourceName} does not contain a node named \"{startNode}\"", YarnProject);
				return;
			}

			SetInitialVariables();

			EmitSignal(SignalName.OnDialogueStart);

			// Signal that we're starting up.
			foreach (var dialogueView in DialogueViews)
			{
				if (dialogueView == null || dialogueView.CanProcess() == false)
				{
					continue;
				}

				dialogueView.DialogueStarted();
			}

			// Request that the dialogue select the current node. This will prepare the dialogue for
			// running; as a side effect, our prepareForLines delegate may be called.
			Dialogue.SetNode(startNode);

			var waitForLines = LineProvider.WaitForLines();
			await WaitForTask(waitForLines, () => ContinueDialogue());
		}

		/// <summary>
		/// Stops the <see cref="Dialogue"/>.
		/// </summary>
		public void Stop()
		{
			Dialogue.Stop();
		}
		#endregion

		/// <summary>
		/// Unloads all nodes from the <see cref="Dialogue"/>.
		/// </summary>
		public void Clear()
		{
			if (IsDialogueRunning)
			{
				GD.PushError("You cannot clear the dialogue system while a dialogue is running.");
				return;
			}
			Dialogue.UnloadAll();
		}

		#region Action Registration
		public void RegisterActionsInAssembly()
		{
			var assembly = Assembly.GetExecutingAssembly();
			var types = assembly.GetTypes();
			var methods = types.SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public));
			foreach (var method in methods)
			{
				var commandAttribute = method.GetCustomAttribute<CommandAttribute>();
				if (commandAttribute != null)
				{
					RegisterCommand(commandAttribute.Name, method);
					continue;
				}

				var functionAttribute = method.GetCustomAttribute<FunctionAttribute>();
				if (functionAttribute != null)
				{
					RegisterFunction(functionAttribute.Name, method);
					continue;
				}
			}
		}

		public void RegisterCommand(string commandName, Delegate handler)
		{
			if (_commands.ContainsKey(commandName))
			{
				GD.PrintErr($"Failed to register command {commandName}: a command by this name has already been registered.");
				return;
			}
			else
			{
				_commands.Add(commandName, new CommandInfo(commandName, handler));
			}
		}

		public void RegisterCommand(string commandName, MethodInfo methodInfo)
		{
			if (_commands.ContainsKey(commandName))
			{
				GD.PrintErr($"Failed to register command {commandName}: a command by this name has already been registered.");
				return;
			}

			_commands[commandName] = new CommandInfo(commandName, methodInfo);
		}

		public void RegisterFunction(string name, Delegate implementation)
		{
			var library = Dialogue.Library;
			if (library.FunctionExists(name))
			{
				GD.PrintErr($"Cannot add function {name}: one already exists");
				return;
			}
			library.RegisterFunction(name, implementation);
		}

		public void RegisterFunction(string name, MethodInfo methodInfo)
		{
			var library = Dialogue.Library;
			if (library.FunctionExists(name))
			{
				GD.PrintErr($"Cannot add function {name}: one already exists");
				return;
			}

			// Construct a delegate from a methodinfo
			// NOTE: this will ONLY work with STATIC METHODS
			// SEE: https://stackoverflow.com/questions/940675/getting-a-delegate-from-methodinfo
			var returnType = methodInfo.ReturnType;
			bool isAction = returnType.Equals(typeof(void));
			Func<Type[], Type> getType = isAction
				? Expression.GetActionType
				: Expression.GetFuncType;

			var types = methodInfo.GetParameters().Select(x => x.ParameterType);
			if (!isAction)
			{
				types = types.Concat(new[] { returnType });
			}

			var type = getType(types.ToArray());
			var implementation = Delegate.CreateDelegate(type, methodInfo);
			library.RegisterFunction(name, implementation);
		}

		public void UnregisterCommand(string commandName)
		{
			if (_commands.Remove(commandName) == false)
			{
				GD.PrintErr($"Can't remove command {commandName}, because no command with this name is currently registered.");
			}
		}

		public void UnregisterFunction(string name)
		{
			var library = Dialogue.Library;
			if (library.FunctionExists(name) == false)
			{
				GD.PrintErr($"Cannot remove function {name}: no function with that name exists in the library");
				return;
			}
			library.DeregisterFunction(name);
		}
		#endregion

		#region Dialogue Callback Handlers
		private void HandleCommand(Command command)
		{
			CommandDispatchResult DispatchCommand(string commandText, out Task commandTask)
			{
				var split = SplitCommandText(commandText);
				var parameters = new List<string>(split);

				if (parameters.Count == 0)
				{
					// No text was found inside the command, so we won't be able to
					// find it.
					commandTask = default;
					return new CommandDispatchResult
					{
						Status = CommandDispatchResult.StatusType.CommandUnknown
					};
				}

				if (_commands.TryGetValue(parameters[0], out var commandInfo))
				{
					// The first part of the command is the command name itself. Remove
					// it to get the collection of parameters that were passed to the
					// command.
					parameters.RemoveAt(0);

					return commandInfo.Invoke(parameters, out commandTask);
				}
				else
				{
					commandTask = default;
					return new CommandDispatchResult
					{
						Status = CommandDispatchResult.StatusType.CommandUnknown
					};
				}
			}

			var dispatchResult = DispatchCommand(command.Text, out Task commandTask);

			switch (dispatchResult.Status)
			{
				case CommandDispatchResult.StatusType.SucceededSync:
					// No need to wait; continue immediately.
					ContinueDialogue();
					return;

				case CommandDispatchResult.StatusType.SucceededAsync:
					// We got a coroutine to wait for. Wait for it, and call Continue.
					_ = WaitForTask(commandTask, () => ContinueDialogue(true));
					return;
			}

			var parts = SplitCommandText(command.Text);
			string commandName = parts.ElementAtOrDefault(0);

			switch (dispatchResult.Status)
			{
				case CommandDispatchResult.StatusType.NoTargetFound:
					GD.PushError($"Can't call command {commandName}: failed to find a game object named {parts.ElementAtOrDefault(1)}", this);
					break;

				case CommandDispatchResult.StatusType.TargetMissingComponent:
					GD.PushError($"Can't call command {commandName}, because {parts.ElementAtOrDefault(1)} doesn't have the correct component");
					break;

				case CommandDispatchResult.StatusType.InvalidParameterCount:
					GD.PushError($"Can't call command {commandName}: incorrect number of parameters");
					break;

				case CommandDispatchResult.StatusType.CommandUnknown:
					// Attempt a last-ditch dispatch by invoking our 'onCommand' Unity Event.
					EmitSignal(SignalName.OnCommand, command.Text);
					return;

				default:
					throw new ArgumentOutOfRangeException($"Internal error: Unknown command dispatch result status {dispatchResult}");
			}
			ContinueDialogue();
		}

		/// <summary>
		/// Forward the line to the dialogue UI.
		/// </summary>
		/// <param name="line">The line to send to the dialogue views.</param>
		private async void HandleLine(Line line)
		{
			var cancelLineSource = new CancellationTokenSource();

			// Get the localized line from our line provider
			CurrentLine = LineProvider.GetLocalizedLine(line);

			// Expand substitutions
			var text = Dialogue.ExpandSubstitutions(CurrentLine.RawText, CurrentLine.Substitutions);

			if (text == null)
			{
				GD.PushError($"Dialogue Runner couldn't expand substitutions in Yarn Project [{YarnProject.ResourceName}] node [{CurrentNodeName}] with line ID [{CurrentLine.TextID}]. "
					+ "This usually happens because it couldn't find text in the Localization. The line may not be tagged properly. "
					+ "Try re-importing this Yarn Program. "
					+ "For now, Dialogue Runner will swap in CurrentLine.RawText.");
				text = CurrentLine.RawText;
			}

			// Render the markup
			Dialogue.LanguageCode = TranslationServer.GetLocale();

			try
			{
				CurrentLine.Text = Dialogue.ParseMarkup(text);
			}
			catch (Markup.MarkupParseException e)
			{
				// Parsing the markup failed. We'll log a warning, and produce a markup result that
				// just contains the raw text.
				GD.PushWarning($"Failed to parse markup in \"{text}\": {e.Message}");
				CurrentLine.Text = new Markup.MarkupParseResult
				{
					Text = text,
					Attributes = new List<Markup.MarkupAttribute>()
				};
			}

			// Send line to all active dialogue views
			var views = new List<DialogueViewBase>(DialogueViews);
			var tasks = new List<Task>();
			foreach (var view in views)
			{
				if (view == null || view.CanProcess() == false)
				{
					continue;
				}

				var task = view.RunLine(CurrentLine, cancelLineSource);
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);

			tasks.Clear();
			foreach (var view in views)
			{
				if (view == null || view.CanProcess() == false)
				{
					continue;
				}

				var task = !cancelLineSource.IsCancellationRequested
					? view.DismissLine(CurrentLine)
					: view.CancelLine(CurrentLine);

				tasks.Add(task);
			}

			cancelLineSource.Dispose();

			ContinueDialogue();
		}

		private async void HandleOptions(OptionSet optionSet)
		{
			var options = optionSet.Options;
			if (options == null)
			{
				GD.PushError("options.Options == null");
				Dialogue.SetSelectedOption(0);
				ContinueDialogue();
				return;
			}

			int numOptions = options.Length;
			if (numOptions == 0)
			{
				GD.PushError("options.Options.Length == 0");
				Dialogue.SetSelectedOption(0);
				ContinueDialogue();
				return;
			}

			DialogueOption[] dialogueOptions = new DialogueOption[numOptions];
			for (int i = 0; i < numOptions; i++)
			{
				// Localize the line associated with the option
				var localisedLine = LineProvider.GetLocalizedLine(options[i].Line);
				var text = Dialogue.ExpandSubstitutions(localisedLine.RawText, options[i].Line.Substitutions);

				Dialogue.LanguageCode = TranslationServer.GetLocale();

				try
				{
					localisedLine.Text = Dialogue.ParseMarkup(text);
				}
				catch (Markup.MarkupParseException e)
				{
					// Parsing the markup failed. We'll log a warning, and produce a markup result
					// that just contains the raw text.
					GD.PushWarning($"Failed to parse markup in \"{text}\": {e.Message}");
					localisedLine.Text = new Markup.MarkupParseResult
					{
						Text = text,
						Attributes = new List<Markup.MarkupAttribute>()
					};
				}

				dialogueOptions[i] = new DialogueOption
				{
					TextID = optionSet.Options[i].Line.ID,
					DialogueOptionID = optionSet.Options[i].ID,
					Line = localisedLine,
					IsAvailable = optionSet.Options[i].IsAvailable,
				};
			}

			var tasks = new List<Task<DialogueOption>>();
			foreach (var dialogueView in DialogueViews)
			{
				if (dialogueView == null || dialogueView.CanProcess() == false)
					continue;

				var task = dialogueView.RunOptions(dialogueOptions);
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);

			DialogueOption selectedOption = null;
			foreach (var task in tasks)
			{
				var result = task.Result;
				if (result == null)
				{
					continue;
				}

				selectedOption = result;
				break;
			}

			if (selectedOption == null)
			{
				GD.PushError("options.Options.Length == 0");
				Dialogue.SetSelectedOption(0);
				ContinueDialogue();
				return;
			}

			Dialogue.SetSelectedOption(selectedOption.DialogueOptionID);

			if (RunSelectedOptionAsLine)
			{
				var option = optionSet.Options.FirstOrDefault(x => x.ID == selectedOption.DialogueOptionID);
				HandleLine(option.Line);
			}
			else
			{
				ContinueDialogue();
			}
		}

		private void HandleDialogueComplete()
		{
			foreach (var dialogueView in DialogueViews)
			{
				if (dialogueView == null || dialogueView.CanProcess() == false)
					continue;

				dialogueView.DialogueComplete();
			}

			EmitSignal(SignalName.OnDialogueComplete);
		}

		private void HandlePrepareForLines(IEnumerable<string> lineIDs)
		{
			if (LineProvider == null)
			{
				GD.PushError("lineProvider == null");
				return;
			}

			LineProvider.PrepareForLines(lineIDs);
		}

		private void ContinueDialogue(bool dontRestart = false)
		{
			if (dontRestart == true)
			{
				if (Dialogue.IsActive == false)
				{
					return;
				}
			}

			CurrentLine = null;
			Dialogue.Continue();
		}
		#endregion

		private static async Task WaitForTask(Task task, Action onTaskComplete)
		{
			if (task == null)
			{
				GD.PushError("task == null");
				return;
			}

			await task;
			onTaskComplete?.Invoke();
		}

		/// <summary>
		/// Splits input into a number of non-empty sub-strings, separated by whitespace, and
		/// grouping double-quoted strings into a single sub-string.
		/// </summary>
		/// <param name="input">The string to split.</param>
		/// <returns>A collection of sub-strings.</returns>
		/// <remarks>
		/// This method behaves similarly to the <see cref="string.Split(char[],
		/// StringSplitOptions)"/> method with the <see cref="StringSplitOptions"/> parameter set to
		/// <see cref="StringSplitOptions.RemoveEmptyEntries"/>, with the following differences: ///
		/// <list type="bullet">
		/// <item>Text that appears inside a pair of double-quote characters will not be split.</item>
		/// ///
		/// <item>
		/// Text that appears after a double-quote character and before the end of the input will
		/// not be split (that is, an unterminated double-quoted string will be treated as though it
		/// had been terminated at the end of the input.)
		/// </item>
		/// ///
		/// <item>
		/// When inside a pair of double-quote characters, the string <c>\\</c> will be converted to
		/// <c>\</c>, and the string <c>\"</c> will be converted to <c>"</c>.
		/// </item>
		/// </list>
		/// </remarks>
		private static IEnumerable<string> SplitCommandText(string input)
		{
			var reader = new System.IO.StringReader(input.Normalize());

			int c;

			var results = new List<string>();
			var currentComponent = new System.Text.StringBuilder();

			while ((c = reader.Read()) != -1)
			{
				if (char.IsWhiteSpace((char)c))
				{
					if (currentComponent.Length > 0)
					{
						// We've reached the end of a run of visible characters. Add this run to the
						// result list and prepare for the next one.
						results.Add(currentComponent.ToString());
						currentComponent.Clear();
					}
					else
					{
						// We encountered a whitespace character, but didn't have any characters
						// queued up. Skip this character.
					}

					continue;
				}
				else if (c == '\"')
				{
					// We've entered a quoted string!
					while (true)
					{
						c = reader.Read();
						if (c == -1)
						{
							// Oops, we ended the input while parsing a quoted string! Dump our
							// current word immediately and return.
							results.Add(currentComponent.ToString());
							return results;
						}
						else if (c == '\\')
						{
							// Possibly an escaped character!
							var next = reader.Peek();
							if (next == '\\' || next == '\"')
							{
								// It is! Skip the \ and use the character after it.
								reader.Read();
								currentComponent.Append((char)next);
							}
							else
							{
								// Oops, an invalid escape. Add the \ and whatever is after it.
								currentComponent.Append((char)c);
							}
						}
						else if (c == '\"')
						{
							// The end of a string!
							break;
						}
						else
						{
							// Any other character. Add it to the buffer.
							currentComponent.Append((char)c);
						}
					}

					results.Add(currentComponent.ToString());
					currentComponent.Clear();
				}
				else
				{
					currentComponent.Append((char)c);
				}
			}

			if (currentComponent.Length > 0)
			{
				results.Add(currentComponent.ToString());
			}

			return results;
		}

		private Dialogue CreateDialogueInstance()
		{
			if (!IsInsideTree())
			{
				GD.PushError("CreateDialogueInstance: !IsInsideTree");
				return null;
			}

			if (VariableStorage == null)
			{
				// If we don't have a variable storage, create an InMemoryVariableStorage and make
				// it use that.
				VariableStorage = new InMemoryVariableStorage();
				AddChild(VariableStorage);

				// Let the user know what we're doing.
				if (VerboseLogging)
				{
					GD.Print($"Dialogue Runner has no Variable Storage; creating a {nameof(InMemoryVariableStorage)}", this);
				}
			}

			// Create the main Dialogue runner, and pass our variableStorage to it
			var dialogue = new Dialogue(VariableStorage)
			{
				// Set up the logging system.
				LogDebugMessage = delegate (string message)
				{
					if (VerboseLogging)
					{
						GD.Print(message);
					}
				},
				LogErrorMessage = delegate (string message)
				{
					GD.PushError(message);
				},

				LineHandler = HandleLine,
				CommandHandler = HandleCommand,
				OptionsHandler = HandleOptions,
				NodeStartHandler = (node) =>
				{
					EmitSignal(SignalName.OnNodeStart, node);
				},
				NodeCompleteHandler = (node) =>
				{
					EmitSignal(SignalName.OnNodeComplete, node);
				},
				DialogueCompleteHandler = HandleDialogueComplete,
				PrepareForLinesHandler = HandlePrepareForLines
			};

			return dialogue;
		}
	}
}
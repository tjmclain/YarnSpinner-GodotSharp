using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Yarn.GodotEngine.LineProviders;

namespace Yarn.GodotEngine
{
	/// <summary>
	/// The DialogueRunner component acts as the interface between your game and Yarn Spinner.
	/// </summary>
	// https://yarnspinner.dev/docs/unity/components/dialogue-runner/
	public partial class DialogueRunner : Godot.Node, IActionRegistration
	{
		/// <summary>
		/// The underlying object that executes Yarn instructions and provides lines, options and commands.
		/// </summary>
		/// <remarks>Automatically created on first access.</remarks>
		private Dialogue _dialogue;

		private ICommandDispatcher _commandDispatcher;

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

		[Export]
		public YarnProject YarnProject { get; private set; }

		[Export]
		public LineProviderBehaviour LineProvider { get; private set; }

		[Export]
		public VariableStorageBehaviour VariableStorage { get; private set; }

		[Export]
		public Godot.Collections.Array<DialogueViewBase> DialogueViews { get; set; }

		[Export]
		public string StartNode { get; set; } = Dialogue.DefaultStartNodeName;

		[Export]
		public bool RunSelectedOptionAsLine { get; set; }

		[Export(PropertyHint.None, "If true, will print GD.Print messages every time it enters a node, and other frequent events")]
		public bool VerboseLogging { get; set; }

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
		public Dialogue Dialogue
			=> _dialogue ??= CreateDialogueInstance();

		public ICommandDispatcher CommandDispatcher
			=> _commandDispatcher ??= CreateCommandDispatcherInstance();

		/// <summary>
		/// The <see cref="LocalizedLine"/> currently being displayed on the dialogue views.
		/// </summary>
		public LocalizedLine CurrentLine { get; private set; }

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
		public static IEnumerable<string> SplitCommandText(string input)
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

		/// <summary>
		/// Replaces this DialogueRunner's yarn project with the provided project.
		/// </summary>
		public void SetProject(YarnProject newProject)
		{
			YarnProject = newProject;

			CommandDispatcher.SetupForProject(newProject);

			Dialogue.SetProgram(newProject.Program);

			if (LineProvider != null)
			{
				LineProvider.YarnProject = newProject;
			}
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

		/// <summary>
		/// Stops the <see cref="Dialogue"/>.
		/// </summary>
		public void Stop()
		{
			Dialogue.Stop();
		}

		/// <summary>
		/// Returns `true` when a node named `nodeName` has been loaded.
		/// </summary>
		/// <param name="nodeName">The name of the node.</param>
		/// <returns>`true` if the node is loaded, `false` otherwise/</returns>
		public bool NodeExists(string nodeName) => Dialogue.NodeExists(nodeName);

		/// <summary>
		/// Returns the collection of tags that the node associated with the node named `nodeName`.
		/// </summary>
		/// <param name="nodeName">The name of the node.</param>
		/// <returns>
		/// The collection of tags associated with the node, or `null` if no node with that name exists.
		/// </returns>
		public IEnumerable<string> GetTagsForNode(string nodeName) => Dialogue.GetTagsForNode(nodeName);

		/// <summary>
		/// Adds a command handler. Dialogue will pause execution after the command is called.
		/// </summary>
		/// <remarks>
		/// <para>
		/// When this command handler has been added, it can be called from your Yarn scripts like so:
		/// </para>
		/// ///
		/// <code lang="yarn">
		///&lt;&lt;commandName param1 param2&gt;&gt;
		/// </code>
		/// ///
		/// <para>
		/// If <paramref name="handler"/> is a method that returns a <see cref="Coroutine"/>, when
		/// the command is run, the <see cref="DialogueRunner"/> will wait for the returned
		/// coroutine to stop before delivering any more content.
		/// </para>
		/// <para>
		/// If <paramref name="handler"/> is a method that returns an <see cref="IEnumerator"/>,
		/// when the command is run, the <see cref="DialogueRunner"/> will start a coroutine using
		/// that method and wait for that coroutine to stop before delivering any more content.
		/// </para>
		/// </remarks>
		/// <param name="commandName">The name of the command.</param>
		/// <param name="handler">
		/// The <see cref="CommandHandler"/> that will be invoked when the command is called.
		/// </param>
		public void AddCommandHandler(string commandName, Delegate handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		/// <param name="method">The method that will be invoked when the command is called.</param>
		public void AddCommandHandler(string commandName, MethodInfo method) => CommandDispatcher.AddCommandHandler(commandName, method);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler(string commandName, Func<Task> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1>(string commandName, Func<T1, Task> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2>(string commandName, Func<T1, T2, Task> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3>(string commandName, Func<T1, T2, T3, Task> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3, T4>(string commandName, Func<T1, T2, T3, T4, Task> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, Func<T1, T2, T3, T4, T5, Task> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, Func<T1, T2, T3, T4, T5, T6, Task> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler(string commandName, Action handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1>(string commandName, Action<T1> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2>(string commandName, Action<T1, T2> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3>(string commandName, Action<T1, T2, T3> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3, T4>(string commandName, Action<T1, T2, T3, T4> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, Action<T1, T2, T3, T4, T5> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, Action<T1, T2, T3, T4, T5, T6> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <summary>
		/// Removes a command handler.
		/// </summary>
		/// <param name="commandName">The name of the command to remove.</param>
		public void RemoveCommandHandler(string commandName) => CommandDispatcher.RemoveCommandHandler(commandName);

		/// <summary>
		/// Add a new function that returns a value, so that it can be called from Yarn scripts.
		/// </summary>
		/// <remarks>
		/// <para>
		/// When this function has been registered, it can be called from your Yarn scripts like so:
		/// </para>
		/// ///
		/// <code lang="yarn">
		///&lt;&lt;if myFunction(1, 2) == true&gt;&gt;
		///myFunction returned true!
		///&lt;&lt;endif&gt;&gt;
		/// </code>
		/// ///
		/// <para>The <c>call</c> command can also be used to invoke the function:</para>
		/// ///
		/// <code lang="yarn">
		///&lt;&lt;call myFunction(1, 2)&gt;&gt;
		/// </code>
		/// </remarks>
		/// <param name="implementation">
		/// The <see cref="Delegate"/> that should be invoked when this function is called.
		/// </param>
		/// <seealso cref="Library"/>
		public void AddFunction(string name, Delegate implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <inheritdoc cref="AddFunction(string, Delegate)"/>
		/// <typeparam name="TResult">The type of the value that the function should return.</typeparam>
		public void AddFunction<TResult>(string name, Func<TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <inheritdoc cref="AddFunction{TResult}(string, Func{TResult})"/>
		/// <typeparam name="T1">The type of the first parameter to the function.</typeparam>
		public void AddFunction<T1, TResult>(string name, Func<T1, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <inheritdoc cref="AddFunction{T1,TResult}(string, Func{T1,TResult})"/>
		/// <typeparam name="T2">The type of the second parameter to the function.</typeparam>
		public void AddFunction<T1, T2, TResult>(string name, Func<T1, T2, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <inheritdoc cref="AddFunction{T1,T2,TResult}(string, Func{T1,T2,TResult})"/>
		/// <typeparam name="T3">The type of the third parameter to the function.</typeparam>
		public void AddFunction<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <inheritdoc cref="AddFunction{T1,T2,T3,TResult}(string, Func{T1,T2,T3,TResult})"/>
		/// <typeparam name="T4">The type of the fourth parameter to the function.</typeparam>
		public void AddFunction<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <inheritdoc cref="AddFunction{T1,T2,T3,T4,TResult}(string, Func{T1,T2,T3,T4,TResult})"/>
		/// <typeparam name="T5">The type of the fifth parameter to the function.</typeparam>
		public void AddFunction<T1, T2, T3, T4, T5, TResult>(string name, Func<T1, T2, T3, T4, T5, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <inheritdoc cref="AddFunction{T1,T2,T3,T4,T5,TResult}(string, Func{T1,T2,T3,T4,T5,TResult})"/>
		/// <typeparam name="T6">The type of the sixth parameter to the function.</typeparam>
		public void AddFunction<T1, T2, T3, T4, T5, T6, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <summary>
		/// Remove a registered function.
		/// </summary>
		/// <remarks>After a function has been removed, it cannot be called from Yarn scripts.</remarks>
		/// <param name="name">The name of the function to remove.</param>
		/// <seealso cref="AddFunction{TResult}(string, Func{TResult})"/>
		public void RemoveFunction(string name) => CommandDispatcher.RemoveFunction(name);

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

		public override void _Ready()
		{
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

		internal void HandleCommand(Command command)
		{
			var dispatchResult = CommandDispatcher.DispatchCommand(command.Text, out Task commandTask);

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
		internal async void HandleLine(Line line)
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

		private Dialogue CreateDialogueInstance()
		{
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
			_dialogue = new Dialogue(VariableStorage)
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
				PrepareForLinesHandler = PrepareForLines
			};

			CreateCommandDispatcherInstance();

			return _dialogue;
		}

		private ICommandDispatcher CreateCommandDispatcherInstance()
		{
			var actions = new Actions(this, Dialogue.Library);
			_commandDispatcher = actions;
			actions.RegisterActions();
			return _commandDispatcher;
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

		private void PrepareForLines(IEnumerable<string> lineIDs)
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
	}
}

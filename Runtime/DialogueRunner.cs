using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using static Yarn.Godot.DialogueRunner;
using GodotCollections = Godot.Collections;
using GodotNode = Godot.Node;

namespace Yarn.Godot
{
	/// <summary>
	/// The DialogueRunner component acts as the interface between your game and
	/// Yarn Spinner.
	/// </summary>
	// https://yarnspinner.dev/docs/unity/components/dialogue-runner/
	public partial class DialogueRunner : GodotNode, IActionRegistration
	{
		/// <summary>
		/// The <see cref="YarnProject"/> asset that should be loaded on
		/// scene start.
		/// </summary>
		[Export]
		public YarnProject yarnProject;

		/// <summary>
		/// The variable storage object.
		/// </summary>
		[Export]
		public VariableStorageBehaviour variableStorage;

		/// <inheritdoc cref="variableStorage"/>
		public VariableStorageBehaviour VariableStorage
		{
			get => variableStorage;
			set
			{
				variableStorage = value;
				if (_dialogue != null)
				{
					_dialogue.VariableStorage = value;
				}
			}
		}

		/// <summary>
		/// The View classes that will present the dialogue to the user.
		/// </summary>
		[Export]
		public GodotCollections.Array<DialogueViewBase> dialogueViews = new();

		/// <summary>The name of the node to start from.</summary>
		/// <remarks>
		/// This value is used to select a node to start from when <see
		/// cref="startAutomatically"/> is called.
		/// </remarks>
		[Export]
		public string startNode = Dialogue.DefaultStartNodeName;

		/// <summary>
		/// Whether the DialogueRunner should automatically start running
		/// dialogue after the scene loads.
		/// </summary>
		/// <remarks>
		/// The node specified by <see cref="startNode"/> will be used.
		/// </remarks>
		[Export]
		public bool startAutomatically = true;

		/// <summary>
		/// If true, when an option is selected, it's as though it were a
		/// line.
		/// </summary>
		[Export]
		public bool runSelectedOptionAsLine;

		[Export]
		public LineProviderBehaviour lineProvider;

		/// <summary>
		/// If true, will print GD.Print messages every time it enters a
		/// node, and other frequent events.
		/// </summary>
		[Export(PropertyHint.None, "If true, will print GD.Print messages every time it enters a node, and other frequent events")]
		public bool verboseLogging = true;

		/// <summary>
		/// Gets a value that indicates if the dialogue is actively
		/// running.
		/// </summary>
		public bool IsDialogueRunning { get; private set; }

		/// <summary>
		/// A Unity event that is called when a node starts running.
		/// </summary>
		/// <remarks>
		/// This event receives as a parameter the name of the node that is
		/// about to start running.
		/// </remarks>
		/// <seealso cref="Dialogue.NodeStartHandler"/>
		[Signal]
		public delegate void OnNodeStartEventHandler(string nodeName);

		/// <summary>
		/// A Unity event that is called when a node is complete.
		/// </summary>
		/// <remarks>
		/// This event receives as a parameter the name of the node that
		/// just finished running.
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
		/// A <see cref="StringUnityEvent"/> that is called when a <see
		/// cref="Command"/> is received.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Use this method to dispatch a command to other parts of your game.
		/// This method is only called if the <see cref="Command"/> has not been
		/// handled by a command handler that has been added to the <see
		/// cref="DialogueRunner"/>, or by a method on a <see
		/// cref="MonoBehaviour"/> in the scene with the attribute <see
		/// cref="YarnCommandAttribute"/>.
		/// </para>
		/// <para style="hint">
		/// When a command is delivered in this way, the <see
		/// cref="DialogueRunner"/> will not pause execution. If you want a
		/// command to make the DialogueRunner pause execution, see <see
		/// cref="AddCommandHandler(string, CommandHandler)"/>.
		/// </para>
		/// <para>
		/// This method receives the full text of the command, as it appears
		/// between the <c>&lt;&lt;</c> and <c>&gt;&gt;</c> markers.
		/// </para>
		/// </remarks>
		/// <seealso cref="AddCommandHandler(string, CommandHandler)"/>
		/// <seealso cref="AddCommandHandler(string, CommandHandler)"/>
		/// <seealso cref="YarnCommandAttribute"/>
		[Signal]
		public delegate void OnCommandEventHandler(string commandName);

		/// <summary>
		/// Gets the name of the current node that is being run.
		/// </summary>
		/// <seealso cref="Dialogue.currentNode"/>
		public string CurrentNodeName => Dialogue.CurrentNode;

		/// <summary>
		/// Gets the underlying <see cref="Dialogue"/> object that runs the
		/// Yarn code.
		/// </summary>
		public Dialogue Dialogue => _dialogue ?? (_dialogue = CreateDialogueInstance());

		/// <summary>
		/// A flag used to detect if an options handler attempts to set the
		/// selected option on the same frame that options were provided.
		/// </summary>
		/// <remarks>
		/// This field is set to false by <see
		/// cref="HandleOptions(OptionSet)"/> immediately before calling
		/// <see cref="DialogueViewBase.RunOptions(DialogueOption[],
		/// Action{int})"/> on all objects in <see cref="dialogueViews"/>,
		/// and set to true immediately after. If a call to <see
		/// cref="DialogueViewBase.RunOptions(DialogueOption[],
		/// Action{int})"/> calls its completion hander on the same frame,
		/// an error is generated.
		/// </remarks>
		private bool IsOptionSelectionAllowed = false;

		private ICommandDispatcher commandDispatcher;

		internal ICommandDispatcher CommandDispatcher
		{
			get
			{
				if (commandDispatcher == null)
				{
					var actions = new Actions(this, Dialogue.Library);
					commandDispatcher = actions;
					actions.RegisterActions();
				}
				return commandDispatcher;
			}
		}

		/// <summary>
		/// Replaces this DialogueRunner's yarn project with the provided
		/// project.
		/// </summary>
		public void SetProject(YarnProject newProject)
		{
			yarnProject = newProject;

			CommandDispatcher.SetupForProject(newProject);

			Dialogue.SetProgram(newProject.Program);

			if (lineProvider != null)
			{
				lineProvider.YarnProject = newProject;
			}
		}

		/// <summary>
		/// Loads any initial variables declared in the program and loads that variable with its default declaration value into the variable storage.
		/// Any variable that is already in the storage will be skipped, the assumption is that this means the value has been overridden at some point and shouldn't be otherwise touched.
		/// Can force an override of the existing values with the default if that is desired.
		/// </summary>
		public void SetInitialVariables(bool overrideExistingValues = false)
		{
			if (yarnProject == null)
			{
				GD.PushError("Unable to set default values, there is no project set");
				return;
			}

			// grabbing all the initial values from the program and inserting them into the storage
			// we first need to make sure that the value isn't already set in the storage
			var values = yarnProject.Program.InitialValues;
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
		/// <param name="startNode">The name of the node to start running
		/// from.</param>
		public void StartDialogue(string startNode)
		{
			// If the dialogue is currently executing instructions, then
			// calling ContinueDialogue() at the end of this method will
			// cause confusing results. Report an error and stop here.
			if (Dialogue.IsActive)
			{
				GD.PushError($"Can't start dialogue from node {startNode}: the dialogue is currently in the middle of running. Stop the dialogue first.");
				return;
			}

			if (yarnProject.NodeNames.Contains(startNode) == false)
			{
				GD.Print($"Can't start dialogue from node {startNode}: the Yarn Project {yarnProject.ResourceName} does not contain a node named \"{startNode}\"", yarnProject);
				return;
			}

			// Stop any processes that might be running already
			foreach (var dialogueView in dialogueViews)
			{
				if (dialogueView == null || dialogueView.CanProcess() == false)
				{
					continue;
				}
			}

			// Get it going

			// Mark that we're in conversation.
			IsDialogueRunning = true;

			EmitSignal(SignalName.OnDialogueStart);

			// Signal that we're starting up.
			foreach (var dialogueView in dialogueViews)
			{
				if (dialogueView == null || dialogueView.CanProcess() == false)
				{
					continue;
				}

				dialogueView.DialogueStarted();
			}

			// Request that the dialogue select the current node. This
			// will prepare the dialogue for running; as a side effect,
			// our prepareForLines delegate may be called.
			Dialogue.SetNode(startNode);

			// TODO: wait for line provider to prepare if necessary
			var waitForLines = lineProvider.WaitForLines();
			_ = WaitForTask(waitForLines, ContinueDialogue);
		}

		/// <summary>
		/// Unloads all nodes from the <see cref="Dialogue"/>.
		/// </summary>
		public void Clear()
		{
			Debug.Assert(!IsDialogueRunning, "You cannot clear the dialogue system while a dialogue is running.");
			Dialogue.UnloadAll();
		}

		/// <summary>
		/// Stops the <see cref="Dialogue"/>.
		/// </summary>
		public void Stop()
		{
			IsDialogueRunning = false;
			Dialogue.Stop();
		}

		/// <summary>
		/// Returns `true` when a node named `nodeName` has been loaded.
		/// </summary>
		/// <param name="nodeName">The name of the node.</param>
		/// <returns>`true` if the node is loaded, `false`
		/// otherwise/</returns>
		public bool NodeExists(string nodeName) => Dialogue.NodeExists(nodeName);

		/// <summary>
		/// Returns the collection of tags that the node associated with
		/// the node named `nodeName`.
		/// </summary>
		/// <param name="nodeName">The name of the node.</param>
		/// <returns>The collection of tags associated with the node, or
		/// `null` if no node with that name exists.</returns>
		public IEnumerable<string> GetTagsForNode(String nodeName) => Dialogue.GetTagsForNode(nodeName);

		/// <summary>
		/// Adds a command handler. Dialogue will pause execution after the
		/// command is called.
		/// </summary>
		/// <remarks>
		/// <para>When this command handler has been added, it can be called
		/// from your Yarn scripts like so:</para>
		///
		/// <code lang="yarn">
		/// &lt;&lt;commandName param1 param2&gt;&gt;
		/// </code>
		///
		/// <para>If <paramref name="handler"/> is a method that returns a <see
		/// cref="Coroutine"/>, when the command is run, the <see
		/// cref="DialogueRunner"/> will wait for the returned coroutine to stop
		/// before delivering any more content.</para>
		/// <para>If <paramref name="handler"/> is a method that returns an <see
		/// cref="IEnumerator"/>, when the command is run, the <see
		/// cref="DialogueRunner"/> will start a coroutine using that method and
		/// wait for that coroutine to stop before delivering any more content.
		/// </para>
		/// </remarks>
		/// <param name="commandName">The name of the command.</param>
		/// <param name="handler">The <see cref="CommandHandler"/> that will be
		/// invoked when the command is called.</param>
		public void AddCommandHandler(string commandName, Delegate handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		/// <param name="method">The method that will be invoked when the
		/// command is called.</param>
		public void AddCommandHandler(string commandName, MethodInfo method) => CommandDispatcher.AddCommandHandler(commandName, method);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler(string commandName, System.Func<Task> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1>(string commandName, System.Func<T1, Task> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2>(string commandName, System.Func<T1, T2, Task> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3>(string commandName, System.Func<T1, T2, T3, Task> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3, T4>(string commandName, System.Func<T1, T2, T3, T4, Task> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, System.Func<T1, T2, T3, T4, T5, Task> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, System.Func<T1, T2, T3, T4, T5, T6, Task> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler(string commandName, System.Action handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1>(string commandName, System.Action<T1> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2>(string commandName, System.Action<T1, T2> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3>(string commandName, System.Action<T1, T2, T3> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3, T4>(string commandName, System.Action<T1, T2, T3, T4> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, System.Action<T1, T2, T3, T4, T5> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
		public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, System.Action<T1, T2, T3, T4, T5, T6> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

		/// <summary>
		/// Removes a command handler.
		/// </summary>
		/// <param name="commandName">The name of the command to
		/// remove.</param>
		public void RemoveCommandHandler(string commandName) => CommandDispatcher.RemoveCommandHandler(commandName);

		/// <summary>
		/// Add a new function that returns a value, so that it can be
		/// called from Yarn scripts.
		/// </summary>
		/// <remarks>
		/// <para>When this function has been registered, it can be called from
		/// your Yarn scripts like so:</para>
		///
		/// <code lang="yarn">
		/// &lt;&lt;if myFunction(1, 2) == true&gt;&gt;
		///     myFunction returned true!
		/// &lt;&lt;endif&gt;&gt;
		/// </code>
		///
		/// <para>The <c>call</c> command can also be used to invoke the function:</para>
		///
		/// <code lang="yarn">
		/// &lt;&lt;call myFunction(1, 2)&gt;&gt;
		/// </code>
		/// </remarks>
		/// <param name="implementation">The <see cref="Delegate"/> that
		/// should be invoked when this function is called.</param>
		/// <seealso cref="Library"/>
		public void AddFunction(string name, Delegate implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <inheritdoc cref="AddFunction(string, Delegate)" />
		/// <typeparam name="TResult">The type of the value that the function should return.</typeparam>
		public void AddFunction<TResult>(string name, System.Func<TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <inheritdoc cref="AddFunction{TResult}(string, Func{TResult})" />
		/// <typeparam name="T1">The type of the first parameter to the function.</typeparam>
		public void AddFunction<T1, TResult>(string name, System.Func<T1, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <inheritdoc cref="AddFunction{T1,TResult}(string, Func{T1,TResult})" />
		/// <typeparam name="T2">The type of the second parameter to the function.</typeparam>
		public void AddFunction<T1, T2, TResult>(string name, System.Func<T1, T2, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <inheritdoc cref="AddFunction{T1,T2,TResult}(string, Func{T1,T2,TResult})" />
		/// <typeparam name="T3">The type of the third parameter to the function.</typeparam>
		public void AddFunction<T1, T2, T3, TResult>(string name, System.Func<T1, T2, T3, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <inheritdoc cref="AddFunction{T1,T2,T3,TResult}(string, Func{T1,T2,T3,TResult})" />
		/// <typeparam name="T4">The type of the fourth parameter to the function.</typeparam>
		public void AddFunction<T1, T2, T3, T4, TResult>(string name, System.Func<T1, T2, T3, T4, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <inheritdoc cref="AddFunction{T1,T2,T3,T4,TResult}(string, Func{T1,T2,T3,T4,TResult})" />
		/// <typeparam name="T5">The type of the fifth parameter to the function.</typeparam>
		public void AddFunction<T1, T2, T3, T4, T5, TResult>(string name, System.Func<T1, T2, T3, T4, T5, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <inheritdoc cref="AddFunction{T1,T2,T3,T4,T5,TResult}(string, Func{T1,T2,T3,T4,T5,TResult})" />
		/// <typeparam name="T6">The type of the sixth parameter to the function.</typeparam>
		public void AddFunction<T1, T2, T3, T4, T5, T6, TResult>(string name, System.Func<T1, T2, T3, T4, T5, T6, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);

		/// <summary>
		/// Remove a registered function.
		/// </summary>
		/// <remarks>
		/// After a function has been removed, it cannot be called from
		/// Yarn scripts.
		/// </remarks>
		/// <param name="name">The name of the function to remove.</param>
		/// <seealso cref="AddFunction{TResult}(string, Func{TResult})"/>
		public void RemoveFunction(string name) => CommandDispatcher.RemoveFunction(name);

		/// <summary>
		/// Sets the dialogue views and makes sure the callback <see cref="DialogueViewBase.MarkLineComplete"/>
		/// will respond correctly.
		/// </summary>
		/// <param name="views">The array of views to be assigned.</param>
		public void SetDialogueViews(DialogueViewBase[] views)
		{
			foreach (var view in views)
			{
				if (view == null)
				{
					continue;
				}
				view.requestInterrupt = OnViewRequestedInterrupt;
			}

			dialogueViews.Clear();
			dialogueViews.AddRange(views);
		}

		#region Private Properties/Variables/Procedures

		/// <summary>
		/// The <see cref="LocalizedLine"/> currently being displayed on
		/// the dialogue views.
		/// </summary>
		internal LocalizedLine CurrentLine { get; private set; }

		/// <summary>
		///  The collection of dialogue views that are currently either
		///  delivering a line, or dismissing a line from being on screen.
		/// </summary>
		private readonly HashSet<DialogueViewBase> ActiveDialogueViews = new HashSet<DialogueViewBase>();

		private Action<int> selectAction;

		/// <summary>
		/// The underlying object that executes Yarn instructions
		/// and provides lines, options and commands.
		/// </summary>
		/// <remarks>
		/// Automatically created on first access.
		/// </remarks>
		private Dialogue _dialogue;

		/// <summary>
		/// The current set of options that we're presenting.
		/// </summary>
		/// <remarks>
		/// This value is <see langword="null"/> when the <see
		/// cref="DialogueRunner"/> is not currently presenting options.
		/// </remarks>
		private OptionSet currentOptions;

		public override void _Ready()
		{
			if (dialogueViews.Count == 0 && startAutomatically)
			{
				GD.PushError($"Dialogue Runner doesn't have any dialogue views set up. No lines or options will be visible.");
			}

			foreach (var view in dialogueViews)
			{
				if (view == null)
				{
					continue;
				}
				view.requestInterrupt = OnViewRequestedInterrupt;
			}

			if (lineProvider == null)
			{
				lineProvider = new TextLineProvider();
				AddChild(lineProvider);

				// Let the user know what we're doing.
				if (verboseLogging)
				{
					GD.Print($"Dialogue Runner has no LineProvider; creating a {nameof(TextLineProvider)}.", this);
				}
			}

			if (yarnProject != null)
			{
				if (Dialogue.IsActive)
				{
					GD.PushError($"DialogueRunner wanted to load a Yarn Project in its Start method, but the Dialogue was already running one. The Dialogue Runner may not behave as you expect.");
				}

				// Load this new Yarn Project.
				SetProject(yarnProject);
			}

			if (yarnProject != null && startAutomatically)
			{
				StartDialogue(startNode);
			}
		}

		private Dialogue CreateDialogueInstance()
		{
			if (VariableStorage == null)
			{
				// If we don't have a variable storage, create an
				// InMemoryVariableStorage and make it use that.
				VariableStorage = new InMemoryVariableStorage();
				AddChild(VariableStorage);

				// Let the user know what we're doing.
				if (verboseLogging)
				{
					GD.Print($"Dialogue Runner has no Variable Storage; creating a {nameof(InMemoryVariableStorage)}", this);
				}
			}

			// Create the main Dialogue runner, and pass our
			// variableStorage to it
			var dialogue = new Dialogue(VariableStorage)
			{
				// Set up the logging system.
				LogDebugMessage = delegate (string message)
				{
					if (verboseLogging)
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

			selectAction = SelectedOption;
			return dialogue;
		}

		internal void HandleOptions(OptionSet options)
		{
			currentOptions = options;

			DialogueOption[] optionSet = new DialogueOption[options.Options.Length];
			for (int i = 0; i < options.Options.Length; i++)
			{
				// Localize the line associated with the option
				var localisedLine = lineProvider.GetLocalizedLine(options.Options[i].Line);
				var text = Dialogue.ExpandSubstitutions(localisedLine.RawText, options.Options[i].Line.Substitutions);

				Dialogue.LanguageCode = TranslationServer.GetLocale();

				try
				{
					localisedLine.Text = Dialogue.ParseMarkup(text);
				}
				catch (Yarn.Markup.MarkupParseException e)
				{
					// Parsing the markup failed. We'll log a warning, and
					// produce a markup result that just contains the raw text.
					GD.PushError($"Failed to parse markup in \"{text}\": {e.Message}");
					localisedLine.Text = new Yarn.Markup.MarkupParseResult
					{
						Text = text,
						Attributes = new List<Yarn.Markup.MarkupAttribute>()
					};
				}

				optionSet[i] = new DialogueOption
				{
					TextID = options.Options[i].Line.ID,
					DialogueOptionID = options.Options[i].ID,
					Line = localisedLine,
					IsAvailable = options.Options[i].IsAvailable,
				};
			}

			// Don't allow selecting options on the same frame that we
			// provide them
			IsOptionSelectionAllowed = false;

			foreach (var dialogueView in dialogueViews)
			{
				if (dialogueView == null || dialogueView.CanProcess() == false)
					continue;

				dialogueView.RunOptions(optionSet, selectAction);
			}

			IsOptionSelectionAllowed = true;
		}

		private void HandleDialogueComplete()
		{
			IsDialogueRunning = false;
			foreach (var dialogueView in dialogueViews)
			{
				if (dialogueView == null || dialogueView.CanProcess() == false)
					continue;

				dialogueView.DialogueComplete();
			}

			EmitSignal(SignalName.OnDialogueComplete);
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
					// We got a coroutine to wait for. Wait for it, and call
					// Continue.
					_ = WaitForTask(commandTask, ContinueDialogue);
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
					// Attempt a last-ditch dispatch by invoking our 'onCommand'
					// Unity Event.
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
		internal void HandleLine(Line line)
		{
			// Get the localized line from our line provider
			CurrentLine = lineProvider.GetLocalizedLine(line);

			// Expand substitutions
			var text = Dialogue.ExpandSubstitutions(CurrentLine.RawText, CurrentLine.Substitutions);

			if (text == null)
			{
				GD.PushError($"Dialogue Runner couldn't expand substitutions in Yarn Project [{yarnProject.ResourceName}] node [{CurrentNodeName}] with line ID [{CurrentLine.TextID}]. "
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
			catch (Yarn.Markup.MarkupParseException e)
			{
				// Parsing the markup failed. We'll log a warning, and
				// produce a markup result that just contains the raw text.
				GD.PushError($"Failed to parse markup in \"{text}\": {e.Message}");
				CurrentLine.Text = new Yarn.Markup.MarkupParseResult
				{
					Text = text,
					Attributes = new List<Yarn.Markup.MarkupAttribute>()
				};
			}

			// Clear the set of active dialogue views, just in case
			ActiveDialogueViews.Clear();

			// the following is broken up into two stages because otherwise if the
			// first view happens to finish first once it calls dialogue complete
			// it will empty the set of active views resulting in the line being considered
			// finished by the runner despite there being a bunch of views still waiting
			// so we do it over two loops.
			// the first finds every active view and flags it as such
			// the second then goes through them all and gives them the line

			// Mark this dialogue view as active
			foreach (var dialogueView in dialogueViews)
			{
				if (dialogueView == null || dialogueView.CanProcess() == false)
				{
					continue;
				}

				ActiveDialogueViews.Add(dialogueView);
			}
			// Send line to all active dialogue views
			foreach (var dialogueView in dialogueViews)
			{
				if (dialogueView == null || dialogueView.CanProcess() == false)
				{
					continue;
				}

				dialogueView.RunLine(CurrentLine,
					() => DialogueViewCompletedDelivery(dialogueView));
			}
		}

		// called by the runner when a view has signalled that it needs to interrupt the current line
		private void InterruptLine()
		{
			ActiveDialogueViews.Clear();

			foreach (var dialogueView in dialogueViews)
			{
				if (dialogueView == null || dialogueView.CanProcess() == false)
				{
					continue;
				}

				ActiveDialogueViews.Add(dialogueView);
			}

			foreach (var dialogueView in dialogueViews)
			{
				dialogueView.InterruptLine(CurrentLine, () => DialogueViewCompletedInterrupt(dialogueView));
			}
		}

		/// <summary>
		/// Indicates to the DialogueRunner that the user has selected an
		/// option
		/// </summary>
		/// <param name="optionIndex">The index of the option that was
		/// selected.</param>
		/// <exception cref="InvalidOperationException">Thrown when the
		/// <see cref="IsOptionSelectionAllowed"/> field is <see
		/// langword="true"/>, which is the case when <see
		/// cref="DialogueViewBase.RunOptions(DialogueOption[],
		/// Action{int})"/> is in the middle of being called.</exception>
		private void SelectedOption(int optionIndex)
		{
			if (IsOptionSelectionAllowed == false)
			{
				throw new InvalidOperationException("Selecting an option on the same frame that options are provided is not allowed. Wait at least one frame before selecting an option.");
			}

			// Mark that this is the currently selected option in the
			// Dialogue
			Dialogue.SetSelectedOption(optionIndex);

			if (runSelectedOptionAsLine)
			{
				foreach (var option in currentOptions.Options)
				{
					if (option.ID == optionIndex)
					{
						HandleLine(option.Line);
						return;
					}
				}

				GD.PushError($"Can't run selected option ({optionIndex}) as a line: couldn't find the option's associated {nameof(Line)} object");
				ContinueDialogue();
			}
			else
			{
				ContinueDialogue();
			}
		}

		private static async Task WaitForTask(Task task, Action onTaskComplete)
		{
			await task;
			onTaskComplete?.Invoke();
		}

		private void PrepareForLines(IEnumerable<string> lineIDs)
		{
			if (lineProvider == null)
			{
				GD.PushError("lineProvider == null");
			}
		}

		/// <summary>
		/// Called when a <see cref="DialogueViewBase"/> has finished
		/// delivering its line.
		/// </summary>
		/// <param name="dialogueView">The view that finished delivering
		/// the line.</param>
		private void DialogueViewCompletedDelivery(DialogueViewBase dialogueView)
		{
			// A dialogue view just completed its delivery. Remove it from
			// the set of active views.
			ActiveDialogueViews.Remove(dialogueView);

			// Have all of the views completed?
			if (ActiveDialogueViews.Count == 0)
			{
				DismissLineFromViews(dialogueViews);
			}
		}

		// this is similar to the above but for the interrupt
		// main difference is a line continues automatically every interrupt finishes
		private void DialogueViewCompletedInterrupt(DialogueViewBase dialogueView)
		{
			ActiveDialogueViews.Remove(dialogueView);

			if (ActiveDialogueViews.Count == 0)
			{
				DismissLineFromViews(dialogueViews);
			}
		}

		private void ContinueDialogue(bool dontRestart)
		{
			if (dontRestart == true)
			{
				if (Dialogue.IsActive == false)
				{
					return;
				}
			}

			ContinueDialogue();
		}

		private void ContinueDialogue()
		{
			CurrentLine = null;
			Dialogue.Continue();
		}

		/// <summary>
		/// Called by a <see cref="DialogueViewBase"/> derived class from
		/// <see cref="dialogueViews"/> to inform the <see
		/// cref="DialogueRunner"/> that the user intents to proceed to the
		/// next line.
		/// </summary>
		public void OnViewRequestedInterrupt()
		{
			if (CurrentLine == null)
			{
				GD.PushError("Dialogue runner was asked to advance but there is no current line");
				return;
			}

			// asked to advance when there are no active views
			// this means the views have already processed the lines as needed
			// so we can ignore this action
			if (ActiveDialogueViews.Count == 0)
			{
				GD.Print("user requested advance, all views finished, ignoring interrupt");
				return;
			}

			// now because lines are fully responsible for advancement the only advancement allowed is interruption
			InterruptLine();
		}

		private void DismissLineFromViews(IEnumerable<DialogueViewBase> dialogueViews)
		{
			ActiveDialogueViews.Clear();

			foreach (var dialogueView in dialogueViews)
			{
				// Skip any dialogueView that is null or not enabled
				if (dialogueView == null || dialogueView.CanProcess() == false)
				{
					continue;
				}

				// we do this in two passes - first by adding each
				// dialogueView into ActiveDialogueViews, then by asking
				// them to dismiss the line - because calling
				// view.DismissLine might immediately call its completion
				// handler (which means that we'd be repeatedly returning
				// to zero active dialogue views, which means
				// DialogueViewCompletedDismissal will mark the line as
				// entirely done)
				ActiveDialogueViews.Add(dialogueView);
			}

			foreach (var dialogueView in dialogueViews)
			{
				if (dialogueView == null || dialogueView.CanProcess() == false)
				{
					continue;
				}

				dialogueView.DismissLine(() => DialogueViewCompletedDismissal(dialogueView));
			}
		}

		private void DialogueViewCompletedDismissal(DialogueViewBase dialogueView)
		{
			// A dialogue view just completed dismissing its line. Remove
			// it from the set of active views.
			ActiveDialogueViews.Remove(dialogueView);

			// Have all of the views completed dismissal?
			if (ActiveDialogueViews.Count == 0)
			{
				// Then we're ready to continue to the next piece of
				// content.
				ContinueDialogue();
			}
		}
		#endregion Private Properties/Variables/Procedures

		/// <summary>
		/// Splits input into a number of non-empty sub-strings, separated
		/// by whitespace, and grouping double-quoted strings into a single
		/// sub-string.
		/// </summary>
		/// <param name="input">The string to split.</param>
		/// <returns>A collection of sub-strings.</returns>
		/// <remarks>
		/// This method behaves similarly to the <see
		/// cref="string.Split(char[], StringSplitOptions)"/> method with
		/// the <see cref="StringSplitOptions"/> parameter set to <see
		/// cref="StringSplitOptions.RemoveEmptyEntries"/>, with the
		/// following differences:
		///
		/// <list type="bullet">
		/// <item>Text that appears inside a pair of double-quote
		/// characters will not be split.</item>
		///
		/// <item>Text that appears after a double-quote character and
		/// before the end of the input will not be split (that is, an
		/// unterminated double-quoted string will be treated as though it
		/// had been terminated at the end of the input.)</item>
		///
		/// <item>When inside a pair of double-quote characters, the string
		/// <c>\\</c> will be converted to <c>\</c>, and the string
		/// <c>\"</c> will be converted to <c>"</c>.</item>
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
						// We've reached the end of a run of visible
						// characters. Add this run to the result list and
						// prepare for the next one.
						results.Add(currentComponent.ToString());
						currentComponent.Clear();
					}
					else
					{
						// We encountered a whitespace character, but
						// didn't have any characters queued up. Skip this
						// character.
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
							// Oops, we ended the input while parsing a
							// quoted string! Dump our current word
							// immediately and return.
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
								// Oops, an invalid escape. Add the \ and
								// whatever is after it.
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
		/// Loads all variables from the <see cref="PlayerPrefs"/> object into
		/// the Dialogue Runner's variable storage.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method loads a string containing JSON from the <see
		/// cref="PlayerPrefs"/> object under the key <see cref="SaveKey"/>,
		/// deserializes that JSON, and then uses the resulting object to set
		/// all variables in <see cref="VariableStorage"/>.
		/// </para>
		/// <para>
		/// The loaded information can be stored via the <see
		/// cref="SaveStateToPlayerPrefs(string)"/> method.
		/// </para>
		/// </remarks>
		/// <param name="saveFilePath">The key to use when storing the
		/// variables.</param>
		/// <returns><see langword="true"/> if the variables were successfully
		/// loaded from the player preferences; <see langword="false"/>
		/// otherwise.</returns>
		/// <seealso
		/// cref="VariableStorageBehaviour.SetAllVariables(Dictionary{string,
		/// float}, Dictionary{string, string}, Dictionary{string, bool},
		/// bool)"/>
		public bool LoadStateFromUserData(string saveFilePath = "yarn_variables.json")
		{
			string path = $"user://{saveFilePath}";
			if (!FileAccess.FileExists(saveFilePath))
			{
				GD.PushWarning($"!FileAccess.FileExists; saveFilePath = '{saveFilePath}'");
				return false;
			}

			using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
			string json = file.GetAsText();
			if (string.IsNullOrEmpty(json))
			{
				GD.PushWarning($"string.IsNullOrEmpty(json); saveFilePath = '{saveFilePath}'");
				return false;
			}

			var dictionaries = DeserializeAllVariablesFromJSON(json);
			variableStorage.SetAllVariables(dictionaries.Item1, dictionaries.Item2, dictionaries.Item3);
			return true;
		}

		/// <summary>
		/// Saves all variables in the Dialogue Runner's variable storage into
		/// the <see cref="PlayerPrefs"/> object.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method serializes all variables in <see
		/// cref="VariableStorage"/> into a string containing JSON, and then
		/// stores that string in the <see cref="PlayerPrefs"/> object under the
		/// key <paramref name="SaveKey"/>.
		/// </para>
		/// <para>
		/// The stored information can be restored via the <see
		/// cref="LoadStateFromPlayerPrefs(string)"/> method.
		/// </para>
		/// </remarks>
		/// <param name="saveFilePath">The key to use when storing the
		/// variables.</param>
		/// <seealso cref="VariableStorageBehaviour.GetAllVariables"/>
		public void SaveStateToUserData(string saveFilePath = "yarn_variables.json")
		{
			string path = $"user://{saveFilePath}";
			using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);

			var data = SerializeAllVariablesToJSON();
			file.StoreLine(data);
		}

		// takes in a JSON string and converts it into a tuple of dictionaries
		// intended to let you just dump these straight into the variable storage
		// throws exceptions if unable to convert or if the conversion half works
		public (Dictionary<string, float>, Dictionary<string, string>, Dictionary<string, bool>) DeserializeAllVariablesFromJSON(string jsonData)
		{
			if (string.IsNullOrEmpty(jsonData))
			{
				GD.PushError("string.IsNullOrEmpty(jsonData)");
				return default;
			}

			SaveData data = (SaveData)Json.ParseString(jsonData);
			if (data == null)
			{
				GD.PushError("SaveData data == null");
				return default;
			}

			if (data.floatKeys == null && data.floatValues == null)
			{
				throw new ArgumentException("Provided JSON string was not able to extract numeric variables");
			}
			if (data.stringKeys == null && data.stringValues == null)
			{
				throw new ArgumentException("Provided JSON string was not able to extract string variables");
			}
			if (data.boolKeys == null && data.boolValues == null)
			{
				throw new ArgumentException("Provided JSON string was not able to extract boolean variables");
			}

			if (data.floatKeys.Length != data.floatValues.Length)
			{
				throw new ArgumentException("Number of keys and values of numeric variables does not match");
			}
			if (data.stringKeys.Length != data.stringValues.Length)
			{
				throw new ArgumentException("Number of keys and values of string variables does not match");
			}
			if (data.boolKeys.Length != data.boolValues.Length)
			{
				throw new ArgumentException("Number of keys and values of boolean variables does not match");
			}

			var floats = new Dictionary<string, float>();
			for (int i = 0; i < data.floatValues.Length; i++)
			{
				floats.Add(data.floatKeys[i], data.floatValues[i]);
			}
			var strings = new Dictionary<string, string>();
			for (int i = 0; i < data.stringValues.Length; i++)
			{
				strings.Add(data.stringKeys[i], data.stringValues[i]);
			}
			var bools = new Dictionary<string, bool>();
			for (int i = 0; i < data.boolValues.Length; i++)
			{
				bools.Add(data.boolKeys[i], data.boolValues[i]);
			}

			return (floats, strings, bools);
		}

		public string SerializeAllVariablesToJSON()
		{
			(var floats, var strings, var bools) = variableStorage.GetAllVariables();

			SaveData data = new SaveData();
			data.floatKeys = floats.Keys.ToArray();
			data.floatValues = floats.Values.ToArray();
			data.stringKeys = strings.Keys.ToArray();
			data.stringValues = strings.Values.ToArray();
			data.boolKeys = bools.Keys.ToArray();
			data.boolValues = bools.Values.ToArray();

			return Json.Stringify(data);
		}

		[Serializable]
		public partial class SaveData : GodotObject
		{
			public string[] floatKeys;
			public float[] floatValues;
			public string[] stringKeys;
			public string[] stringValues;
			public string[] boolKeys;
			public bool[] boolValues;
		}
	}
}
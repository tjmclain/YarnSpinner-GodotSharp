using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Yarn.GodotSharp.Actions;
using Yarn.GodotSharp.Variables;
using Yarn.GodotSharp.Views;

namespace Yarn.GodotSharp;

/// <summary>
/// The DialogueRunner component acts as the interface between your game and Yarn Spinner.
/// </summary>
// https://yarnspinner.dev/docs/unity/components/dialogue-runner/
[GlobalClass]
public partial class DialogueRunner : Godot.Node
{
	/// <summary>
	/// The underlying object that executes Yarn instructions and provides lines, options and commands.
	/// </summary>
	/// <remarks>Automatically created on first access.</remarks>
	private Dialogue _dialogue;

	#region Signals

	/// <summary>
	/// A Unity event that is called when a node starts running.
	/// </summary>
	/// <remarks>
	/// This event receives as a parameter the name of the node that is about to start running.
	/// </remarks>
	/// <seealso cref="Dialogue.NodeStartHandler"/>
	[Signal]
	public delegate void NodeStartedEventHandler(string nodeName);

	/// <summary>
	/// A Unity event that is called when a node is complete.
	/// </summary>
	/// <remarks>This event receives as a parameter the name of the node that just finished running.</remarks>
	/// <seealso cref="Dialogue.NodeCompleteHandler"/>
	[Signal]
	public delegate void NodeCompletedEventHandler(string nodeName);

	/// <summary>
	/// A Unity event that is called when the dialogue starts running.
	/// </summary>
	[Signal]
	public delegate void DialogueStartedEventHandler();

	/// <summary>
	/// A Unity event that is called once the dialogue has completed.
	/// </summary>
	/// <seealso cref="Dialogue.DialogueCompleteHandler"/>
	[Signal]
	public delegate void DialogueCompletedEventHandler();

	[Signal]
	public delegate void CommandDispatchedEventHandler(string commandName);

	#endregion Signals

	public DialogueRunner()
	{
		DialogueStarting += () => GD.Print("DialogueRunner: DialogueStarting");
		NodeStarted += (node) => GD.Print($"DialogueRunner: NodeStarted = {node}");
		NodeCompleted += (node) => GD.Print($"DialogueRunner: NodeCompleted = {node}");
		DialogueCompleted += () => GD.Print("DialogueRunner: DialogueCompleted");
	}

	#region Exports

	[Export]
	public bool RunAutomatically { get; set; } = false;

	[Export]
	public string StartNode { get; set; } = Dialogue.DefaultStartNodeName;

	[Export]
	public bool RunSelectedOptionAsLine { get; set; } = false;

	[Export]
	public YarnProject YarnProject { get; private set; } = null;

	[Export]
	public LineProvider LineProvider { get; private set; } = null;

	[Export]
	public VariableStorage VariableStorage { get; private set; } = null;

	[Export]
	public ActionLibrary ActionLibrary { get; private set; } = null;

	[Export]
	public DialogueViewGroup MainDialogueViewGroup { get; set; } = null;

	[Export(PropertyHint.None, "If true, will print GD.Print messages every time it enters a node, and other frequent events")]
	public bool VerboseLogging { get; set; }

	#endregion Exports

	/// <summary>
	/// Gets the underlying <see cref="Dialogue"/> object that runs the Yarn code.
	/// </summary>
	public Dialogue Dialogue => _dialogue ??= CreateDialogueInstance();

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
	/// The <see cref="LocalizedLine"/> currently being displayed on the dialogue views.
	/// </summary>
	public LocalizedLine CurrentLine { get; private set; }

	#region Getter Methods

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

	#endregion Getter Methods

	#region Setter Methods

	/// <summary>
	/// Loads any initial variables declared in the program and loads that variable with its default
	/// declaration value into the variable storage. Any variable that is already in the storage
	/// will be skipped, the assumption is that this means the value has been overridden at some
	/// point and shouldn't be otherwise touched. Can force an override of the existing values with
	/// the default if that is desired.
	/// </summary>
	public void SetInitialVariables(bool overrideExistingValues = false)
	{
		if (YarnProject == null)
		{
			GD.PushError("Unable to set default values, there is no project set");
			return;
		}

		// grabbing all the initial values from the program and inserting them into the storage we
		// first need to make sure that the value isn't already set in the storage
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

	#endregion Setter Methods

	#region Godot.Node Methods

	public override void _Ready()
	{
		// Create a LineProvider if we're missing one
		if (LineProvider == null)
		{
			LineProvider = new LineProvider();

			GD.Print(
				$"{nameof(DialogueRunner)} has no {nameof(LineProvider)}; ",
				$"creating a {nameof(LineProvider)}"
			);
		}

		// Create an ActionLibrary if we're missing one, and refresh actions if necessary
		if (ActionLibrary == null)
		{
			ActionLibrary = new ActionLibrary();
			ActionLibrary.RefreshActions();

			GD.Print(
				$"{nameof(DialogueRunner)} has no {nameof(ActionLibrary)}; ",
				$" creating a {nameof(ActionLibrary)}"
			);
		}
		else if (Engine.IsEditorHint())
		{
			ActionLibrary.RefreshActions();
		}

		var library = Dialogue.Library;
		foreach (var function in ActionLibrary.Functions)
		{
			var implementation = function.CreateDelegate();
			library.RegisterFunction(function.Name, implementation);
		}

		if (RunAutomatically)
		{
			_ = Task.Run(() => StartDialogue(StartNode));
		}
	}

	#endregion Godot.Node Methods

	#region Dialogue Control Methods

	/// <summary>
	/// Start the dialogue from a specific node.
	/// </summary>
	/// <param name="startNode">The name of the node to start running from.</param>
	public async Task StartDialogue(string startNode)
	{
		// If the dialogue is currently executing instructions, then calling ContinueDialogue() at
		// the end of this method will cause confusing results. Report an error and stop here.
		if (Dialogue.IsActive)
		{
			GD.PushError(
				$"{nameof(DialogueRunner)}.{nameof(StartDialogue)}",
				$"Can't start dialogue from node '{startNode}': ",
				"the dialogue is currently in the middle of running. ",
				$"call '{nameof(Stop)}' first"
			);
			return;
		}

		if (YarnProject == null)
		{
			GD.PushError("YarnProject == null");
			return;
		}

		if (!IsNodeReady())
		{
			GD.Print("!IsNodeReady; await Ready signal");
			await ToSignal(this, Godot.Node.SignalName.Ready);
		}

		var program = YarnProject.CompileProgram();

		if (YarnProject.NodeNames.Contains(startNode) == false)
		{
			GD.Print($"Can't start dialogue from node {startNode}: the Yarn Project {YarnProject.ResourceName} does not contain a node named \"{startNode}\"", YarnProject);
			return;
		}

		Dialogue.SetProgram(program);

		SetInitialVariables();

		LineProvider.StringTable = YarnProject.StringTable;

		// if lines aren't ready yet, wait
		if (!LineProvider.LinesAvailable)
		{
			await ToSignal(LineProvider, LineProvider.SignalName.LinesBecameAvailable);
		}

		// Try CallDeferred instead
		//forum post: https://godotforums.org/d/35232-godot-41-is-here-smoother-more-reliable-and-with-plenty-of-new-features/12
		//docs: https://docs.godotengine.org/en/stable/classes/class_object.html#class-object-method-call-deferred
		CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.DialogueStarted);

		// Signal that we're starting up.
		MainDialogueViewGroup?.DialogueStarted();

		// Request that the dialogue select the current node. This will prepare the dialogue for
		// running; as a side effect, our prepareForLines delegate may be called.
		Dialogue.SetNode(startNode);

		ContinueDialogue();
	}

	/// <summary>
	/// Stops the <see cref="Dialogue"/>.
	/// </summary>
	public void Stop(bool clear)
	{
		Dialogue.Stop();

		if (clear)
		{
			Clear();
		}
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

	#endregion Dialogue Control Methods

	#region Dialogue Callback Handlers

	private void HandleCommand(Command command)
	{
		var result = ActionLibrary.DispatchCommand(command.Text, out Task commandTask);

		switch (result.Status)
		{
			case CommandDispatchResult.StatusType.SucceededAsync:
			case CommandDispatchResult.StatusType.SucceededSync:
				break;

			default:
				GD.PushError(result);
				break;
		}

		EmitSignal(SignalName.CommandDispatched, result.CommandName);

		if (commandTask == null)
		{
			// No need to wait; continue immediately.
			ContinueDialogue();
		}
		else
		{
			// We got a task to wait for. Wait for it, and call Continue.
			_ = WaitForTask(commandTask, () => ContinueDialogue(true));
		}
	}

	/// <summary>
	/// Forward the line to the dialogue UI.
	/// </summary>
	/// <param name="line">The line to send to the dialogue views.</param>
	private async Task HandleLine(Line line)
	{
		if (MainDialogueViewGroup == null)
		{
			GD.PushError("HandleLine: DialogueViewGroup == null");
			ContinueDialogue();
			return;
		}

		// Get the localized line from our line provider
		CurrentLine = LineProvider.GetLocalizedLine(line);

		GD.Print($"HandleLine: line = {line.ID}");

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
			// Parsing the markup failed. We'll log a warning, and produce a markup result that just
			// contains the raw text.
			GD.PushWarning($"Failed to parse markup in \"{text}\": {e.Message}");
			CurrentLine.Text = new Markup.MarkupParseResult
			{
				Text = text,
				Attributes = new List<Markup.MarkupAttribute>()
			};
		}

		GD.Print("Run line: " + CurrentLine.Text.Text);
		using (var cts = new CancellationTokenSource())
		{
			await MainDialogueViewGroup.RunLine(
				CurrentLine,
				() => cts.Cancel(),
				cts.Token
			);
		}

		GD.Print("Dismiss line");
		await MainDialogueViewGroup.DismissLine(CurrentLine);

		ContinueDialogue();
	}

	private async Task HandleOptions(OptionSet optionSet)
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

		if (MainDialogueViewGroup == null)
		{
			GD.PushError("HandleOptions: DialogueViewGroup == null");
			Dialogue.SetSelectedOption(options[0].ID);
			ContinueDialogue();
			return;
		}

		var dialogueOptions = new DialogueOption[numOptions];
		for (int i = 0; i < numOptions; i++)
		{
			var option = options[i];

			// Localize the line associated with the option
			var localisedLine = LineProvider.GetLocalizedLine(option.Line);
			var text = Dialogue.ExpandSubstitutions(localisedLine.RawText, option.Line.Substitutions);

			Dialogue.LanguageCode = TranslationServer.GetLocale();

			try
			{
				localisedLine.Text = Dialogue.ParseMarkup(text);
			}
			catch (Markup.MarkupParseException e)
			{
				GD.PushWarning($"Failed to parse markup in \"{text}\": {e.Message}");
				localisedLine.Text = new Markup.MarkupParseResult
				{
					Text = text,
					Attributes = new List<Markup.MarkupAttribute>()
				};
			}

			dialogueOptions[i] = new DialogueOption(option, localisedLine);
		}

		int selectedOptionIndex = -1;
		using (var cts = new CancellationTokenSource())
		{
			// RunOptions
			await Task.Run(
				() => MainDialogueViewGroup.RunOptions(dialogueOptions, (index) =>
				{
					selectedOptionIndex = index;
					cts.Cancel();
				}, cts.Token),
				cts.Token
			);
		}

		if (selectedOptionIndex < 0 || selectedOptionIndex >= numOptions)
		{
			GD.PushError($"selectedOptionIndex ({selectedOptionIndex}) is out of range; numOptions = {numOptions}");
			Dialogue.SetSelectedOption(options[0].ID);
			ContinueDialogue();
			return;
		}

		// DismissOptions
		await MainDialogueViewGroup.DismissOptions(dialogueOptions, selectedOptionIndex);

		var selectedOption = dialogueOptions[selectedOptionIndex];
		Dialogue.SetSelectedOption(selectedOption.DialogueOptionID);

		if (RunSelectedOptionAsLine)
		{
			var option = optionSet.Options.FirstOrDefault(x => x.ID == selectedOption.DialogueOptionID);
			await HandleLine(option.Line);
		}
		else
		{
			ContinueDialogue();
		}
	}

	private void HandleDialogueComplete()
	{
		EmitSignal(SignalName.DialogueCompleted);

		if (MainDialogueViewGroup == null)
		{
			GD.PushError("HandleOptions: DialogueViewGroup == null");
			return;
		}

		MainDialogueViewGroup.DialogueComplete();
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

	#endregion Dialogue Callback Handlers

	private static async Task WaitForTask(Task task, Action onTaskComplete)
	{
		if (task == null)
		{
			GD.PushError("task == null");
			onTaskComplete?.Invoke();
			return;
		}

		await task;
		onTaskComplete?.Invoke();
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
			// If we don't have a variable storage, create an InMemoryVariableStorage and make it
			// use that.
			VariableStorage = new VariableStorage();

			// Let the user know what we're doing.
			if (VerboseLogging)
			{
				GD.Print($"Dialogue Runner has no Variable Storage; creating a {nameof(VariableStorage)}", this);
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

			PrepareForLinesHandler = (lineIds) => LineProvider?.PrepareForLines(lineIds),
			LineHandler = (line) => _ = HandleLine(line),
			CommandHandler = HandleCommand,
			OptionsHandler = (options) => _ = HandleOptions(options),
			NodeStartHandler = (node) => CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.NodeStarted, node),
			NodeCompleteHandler = (node) => CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.NodeCompleted, node),
			DialogueCompleteHandler = HandleDialogueComplete,
		};

		return dialogue;
	}
}
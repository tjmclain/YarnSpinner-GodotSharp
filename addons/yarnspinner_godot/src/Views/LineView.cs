using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;
using Yarn.GodotSharp.Views.Effects;

namespace Yarn.GodotSharp.Views;

[GlobalClass]
public partial class LineView : Godot.Node, IRunLineHandler
{
	[Export]
	public RichTextLabel LineText { get; set; } = null;

	[Export]
	public RichTextLabel CharacterNameText { get; set; } = null;

	[Export]
	public Control CharacterNameContainer { get; set; } = null;

	[Export]
	public TextEffect TextAnimationEffect { get; set; } = null;

	[Export]
	public BaseButton ContinueButton { get; set; } = null;

	[Export]
	public StringName ContinueInputAction { get; set; } = string.Empty;

	private CancellationTokenSource _cancellationTokenSource;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (ContinueButton != null)
		{
			ContinueButton.Pressed -= ContinueDialogue;
			ContinueButton.Pressed += ContinueDialogue;

			ContinueButton.Hide();
		}
	}

	public override void _ExitTree()
	{
		if (ContinueButton != null)
		{
			ContinueButton.Pressed -= ContinueDialogue;
		}
	}

	public override void _Input(InputEvent evt)
	{
		if (string.IsNullOrEmpty(ContinueInputAction))
		{
			return;
		}

		if (!evt.IsActionPressed(ContinueInputAction))
		{
			return;
		}

		ContinueDialogue();
	}

	public virtual async Task RunLine(LocalizedLine line, Action interruptLine)
	{
		if (LineText == null)
		{
			GD.PushError("LineText == null");
			return;
		}

		CancelExecutingTasks();

		ContinueButton?.Show();

		string characterName = line.CharacterName;
		if (string.IsNullOrEmpty(characterName))
		{
			CharacterNameContainer?.Hide();
		}
		else
		{
			CharacterNameContainer?.Show();
		}

		if (CharacterNameText != null)
		{
			CharacterNameText.Text = characterName;
		}

		LineText.Text = line.TextWithoutCharacterName;

		if (TextAnimationEffect != null)
		{
			_cancellationTokenSource = new CancellationTokenSource();
			await Task.Run(
				() => TextAnimationEffect.Animate(LineText),
				_cancellationTokenSource.Token
			);

			CancelExecutingTasks();
		}

		_cancellationTokenSource = new CancellationTokenSource();

		var awaiter = new CancellationTokenAwaiter(_cancellationTokenSource.Token);
		await awaiter;

		_cancellationTokenSource = null;

		ContinueButton?.Hide();

		CancelExecutingTasks();

		interruptLine?.Invoke();
	}

	public virtual async Task DismissLine(LocalizedLine line)
	{
		CancelExecutingTasks();
		ContinueButton?.Hide();
		await Task.CompletedTask;
	}

	public virtual void ContinueDialogue()
	{
		CancelExecutingTasks();
	}

	protected virtual void CancelExecutingTasks()
	{
		if (_cancellationTokenSource == null)
		{
			return;
		}

		_cancellationTokenSource.Cancel();
		_cancellationTokenSource.Dispose();
		_cancellationTokenSource = null;
	}
}

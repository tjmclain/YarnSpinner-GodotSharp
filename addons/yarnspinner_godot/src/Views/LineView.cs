using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;
using Yarn.GodotSharp.Views.Effects;

namespace Yarn.GodotSharp.Views;

[GlobalClass]
public partial class LineView : DialogueViewControl, IRunLineHandler
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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (ContinueButton != null)
		{
			ContinueButton.Pressed -= ContinueDialogue;
			ContinueButton.Pressed += ContinueDialogue;

			ContinueButton.Hide();
		}

		CancelAndDisposeTokenSource();
		Hide();
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

		CancelAndDisposeTokenSource();

		Show();
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
			CancellationTokenSource = new CancellationTokenSource();
			await Task.Run(
				() => TextAnimationEffect.Animate(LineText),
				GetCancellationToken()
			);

			CancelAndDisposeTokenSource();
		}

		CancellationTokenSource = new CancellationTokenSource();

		var awaiter = new CancellationTokenAwaiter(GetCancellationToken());
		await awaiter;

		ContinueButton?.Hide();

		CancelAndDisposeTokenSource();

		interruptLine?.Invoke();
	}

	public virtual async Task DismissLine(LocalizedLine line)
	{
		CancelAndDisposeTokenSource();
		ContinueButton?.Hide();
		Hide();
		await Task.CompletedTask;
	}

	public virtual void ContinueDialogue()
	{
		CancelTokenSource();
	}
}

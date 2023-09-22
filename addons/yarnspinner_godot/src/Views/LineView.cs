using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yarn.GodotSharp.Views;

[GlobalClass]
public partial class LineView : Control, IRunLineHandler
{
	[Export]
	public RichTextLabel LineText { get; set; } = null;

	[Export]
	public RichTextLabel CharacterNameText { get; set; } = null;

	[Export]
	public Control CharacterNameContainer { get; set; } = null;

	[Export]
	public TextAnimation TextAnimation { get; set; } = null;

	[Export]
	public Button AdvanceDialogueButton { get; set; } = null;

	private CancellationTokenSource _cancellationTokenSource;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (AdvanceDialogueButton != null)
		{
			AdvanceDialogueButton.Pressed -= AdvanceDialogue;
			AdvanceDialogueButton.Pressed += AdvanceDialogue;
		}
	}

	public override void _ExitTree()
	{
		if (AdvanceDialogueButton != null)
		{
			AdvanceDialogueButton.Pressed -= AdvanceDialogue;
		}
	}

	public virtual async Task RunLine(LocalizedLine line, Action interruptLine)
	{
		if (LineText == null)
		{
			GD.PushError("LineText == null");
			return;
		}

		AdvanceDialogueButton?.Show();

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

		if (TextAnimation != null)
		{
			_cancellationTokenSource = new CancellationTokenSource();
			await Task.Run(
				() => TextAnimation.Animate(LineText),
				_cancellationTokenSource.Token
			);
			_cancellationTokenSource.Dispose();
		}

		_cancellationTokenSource = new CancellationTokenSource();

		await _cancellationTokenSource.Token;

		_cancellationTokenSource.Dispose();
		_cancellationTokenSource = null;
	}

	public virtual async Task DismissLine(LocalizedLine line)
	{
		await Task.CompletedTask;
	}

	public virtual void AdvanceDialogue()
	{
		if (_cancellationTokenSource == null)
		{
			return;
		}

		if (_cancellationTokenSource.IsCancellationRequested)
		{
			return;
		}

		_cancellationTokenSource.Cancel();
	}
}

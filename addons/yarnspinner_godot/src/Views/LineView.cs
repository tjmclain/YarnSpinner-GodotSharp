using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;

using Yarn.GodotSharp.Extensions;

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
	public TextEffect TextAnimation { get; set; } = null;

	[Export]
	public BaseButton ContinueButton { get; set; } = null;

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

	public virtual async Task RunLine(LocalizedLine line, Action interruptLine)
	{
		if (LineText == null)
		{
			GD.PushError("LineText == null");
			return;
		}

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

		ContinueButton?.Hide();
	}

	public virtual async Task DismissLine(LocalizedLine line)
	{
		ContinueButton?.Hide();
		await Task.CompletedTask;
	}

	public virtual void ContinueDialogue()
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

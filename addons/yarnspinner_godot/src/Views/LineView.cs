using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;
using Yarn.GodotSharp.Views.Effects;
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
	public TextEffect TextAnimationEffect { get; set; } = null;

	[Export]
	public BaseButton ContinueButton { get; set; } = null;

	private CancellationTokenSource _taskCancellationSource;

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

		if (TextAnimationEffect != null)
		{
			_taskCancellationSource = new CancellationTokenSource();
			await Task.Run(
				() => TextAnimationEffect.Animate(LineText),
				_taskCancellationSource.Token
			);
		}

		_taskCancellationSource = new CancellationTokenSource();

		await _taskCancellationSource.Token;

		_taskCancellationSource = null;

		ContinueButton?.Hide();

		interruptLine?.Invoke();
	}

	public virtual async Task DismissLine(LocalizedLine line)
	{
		CancelCurrentExecutingTask();
		ContinueButton?.Hide();
		await Task.CompletedTask;
	}

	public virtual void ContinueDialogue()
	{
		CancelCurrentExecutingTask();
	}

	protected virtual void CancelCurrentExecutingTask()
	{
		if (_taskCancellationSource == null)
		{
			return;
		}

		if (_taskCancellationSource.IsCancellationRequested)
		{
			return;
		}

		_taskCancellationSource.Cancel();
	}
}

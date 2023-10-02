using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Yarn.GodotSharp.Views;

[GlobalClass]
public partial class OptionsListView : Control, IRunOptionsHandler, IRunLineHandler
{
	private Control _previousLineContainer = null;
	private OptionView.OptionSelectedEventHandler _onOptionSelected = null;

	#region Exports

	[Export]
	public virtual PackedScene OptionViewPrototype { get; set; } = null;

	[Export]
	public virtual Control OptionViewsContainer { get; set; }

	[Export]
	public virtual RichTextLabel PreviousLineLabel { get; set; } = null;

	[Export]
	public virtual Control PreviousLineContainer
	{
		get => _previousLineContainer ?? PreviousLineLabel;
		set => _previousLineContainer = value;
	}

	#endregion Exports

	protected LocalizedLine PreviousLine { get; private set; } = null;
	protected List<OptionView> OptionViews { get; private set; } = new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Collect all of the already instantiated options
		var optionViews = OptionViewsContainer.GetChildren()
			.Where(x => typeof(OptionView).IsAssignableFrom(x.GetType()))
			.Select(x => x as OptionView);

		OptionViews.Clear();
		OptionViews.AddRange(optionViews);

		// Clear pool and recycling any existing views
		DismissOptionViews();
		Hide();
	}

	public async Task RunLine(LocalizedLine line, Action interruptLine, CancellationToken externalToken)
	{
		PreviousLine = line;
		await Task.CompletedTask;
	}

	public async Task DismissLine(LocalizedLine line)
	{
		await Task.CompletedTask;
	}

	#region IRunOptionsHandler

	public virtual async Task RunOptions(DialogueOption[] options, Action<int> selectOption, CancellationToken token)
	{
		GD.Print($"{nameof(RunOptions)}: {Name}");

		if (options == null)
		{
			GD.PushError("RunOptions: options == null");
			return;
		}

		if (options.Length == 0)
		{
			GD.PushError("RunOptions: options.Length == 0");
			return;
		}

		CallDeferred(MethodName.SetPreviousLineLabelText);

		if (OptionViews.Count < options.Length)
		{
			CallDeferred(MethodName.InstantiateOptionViewsUpTo, options.Length);

			await GodotUtility.WaitForProcessFrame();
		}

		var tcs = new TaskCompletionSource<int>();
		_onOptionSelected = new OptionView.OptionSelectedEventHandler((result) => tcs.SetResult(result));

		for (int i = 0; i < options.Length; i++)
		{
			if (OptionViews.Count <= i)
			{
				GD.PushError($"RunOptions: optionViews.Count ({OptionViews.Count}) <= {i}");
				continue;
			}

			var optionView = OptionViews[i];
			if (optionView == null)
			{
				GD.PushError($"RunOptions: optionView == null; i = {i}");
				continue;
			}

			optionView.CallDeferred(CanvasItem.MethodName.Show);
			optionView.CallDeferred(OptionView.MethodName.SetOption, options[i], i);
			optionView.OptionSelected += _onOptionSelected;
		}

		CallDeferred(CanvasItem.MethodName.Show);

		int selectedIndex = await tcs.Task;

		if (selectedIndex < 0 || selectedIndex >= options.Length)
		{
			GD.PushError($"selectIndex = {selectedIndex}, options.Length = {options.Length}");
		}

		GD.Print($"RunOptions: selectedIndex = {selectedIndex}");
		selectOption?.Invoke(selectedIndex);
	}

	public virtual async Task DismissOptions(DialogueOption[] options, int selectedOptionIndex)
	{
		CallDeferred(MethodName.DismissOptionViews);
		CallDeferred(CanvasItem.MethodName.Hide);

		await Task.CompletedTask;
	}

	#endregion IRunOptionsHandler

	protected virtual void SetPreviousLineLabelText()
	{
		if (PreviousLineLabel == null)
		{
			return;
		}

		if (PreviousLine == null)
		{
			PreviousLineContainer.Hide();
			return;
		}

		PreviousLineContainer.Show();
		PreviousLineLabel.Text = PreviousLine.Text.Text;
		PreviousLineLabel.VisibleCharacters = -1;
	}

	protected virtual void InstantiateOptionView()
	{
		if (OptionViewPrototype == null)
		{
			GD.PushError("InstantiateOptionView: OptionViewPrototype == null");
			return;
		}

		if (OptionViewsContainer == null)
		{
			GD.PushError("InstantiateOptionView: OptionViewsContainer == null");
			return;
		}

		var optionView = OptionViewPrototype.InstantiateOrNull<OptionView>();
		OptionViewsContainer.AddChild(optionView);
		OptionViews.Add(optionView);
	}

	protected virtual void InstantiateOptionViewsUpTo(int total)
	{
		int count = OptionViews.Count;
		int delta = total - count;
		if (delta <= 0)
		{
			return;
		}

		for (int i = 0; i < delta; i++)
		{
			InstantiateOptionView();
		}

		GD.Print($"{nameof(InstantiateOptionViewsUpTo)}: instantiated {delta} {nameof(OptionView)}s");
	}

	protected virtual void DismissOptionViews()
	{
		foreach (var optionView in OptionViews)
		{
			if (optionView == null)
			{
				GD.PushError("optionView == null");
				continue;
			}

			optionView.OptionSelected -= _onOptionSelected;
			optionView.Hide();
		}
	}
}
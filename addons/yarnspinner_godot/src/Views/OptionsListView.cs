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
	#region Fields

	protected readonly List<OptionView> _optionViewsPool = new();
	protected LocalizedLine _previousLine = null;
	protected Control _previousLineContainer = null;

	#endregion Fields

	#region Properties

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

	#endregion Properties

	#region Public Methods

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Clear pool and recycling any existing views
		_optionViewsPool.Clear();
		RecyleOptionViews();
		Hide();
	}

	public async Task RunLine(LocalizedLine line, Action interruptLine, CancellationToken externalToken)
	{
		_previousLine = line;
		await Task.CompletedTask;
	}

	public async Task DismissLine(LocalizedLine line)
	{
		await Task.CompletedTask;
	}

	public virtual async Task RunOptions(DialogueOption[] options, Action<int> selectOption, CancellationToken token)
	{
		if (options == null)
		{
			throw new InvalidDataException("options == null");
		}

		if (options.Length == 0)
		{
			throw new TaskCanceledException("options.Length == 0");
		}

		//SetPreviousLineLabelText(_previousLine);

		var tasks = new List<Task<int>>(options.Length);
		for (int i = 0; i < options.Length; i++)
		{
			var option = options[i];
			if (!TryGetOptionView(out var optionView))
			{
				continue;
			}

			optionView.SetOption(option, i);

			var task = Task.Run(async () =>
				{
					if (token.IsCancellationRequested)
					{
						//token.ThrowIfCancellationRequested();
						return -1;
					}

					var awaiter = ToSignal(optionView, OptionView.SignalName.OptionSelected);
					await awaiter;

					if (token.IsCancellationRequested)
					{
						//token.ThrowIfCancellationRequested();
						return -1;
					}

					var result = awaiter.GetResult();
					if (result == null || result.Length == 0)
					{
						throw new ArgumentException("result == null || result.Length == 0");
					}

					return result[0].AsInt32();
				}, token);

			tasks.Add(task);

			var selected = await Task.WhenAny(tasks);

			if (token.IsCancellationRequested)
			{
				//token.ThrowIfCancellationRequested();
				return;
			}

			int selectedIndex = selected.Result;

			if (selectedIndex < 0 || selectedIndex >= options.Length)
			{
				GD.PushError($"selectIndex = {selectedIndex}, options.Length = {options.Length}");
			}

			GD.Print($"RunOptions: selectedIndex = {selectedIndex}");
			selectOption?.Invoke(selectedIndex);
		}
	}

	public virtual async Task DismissOptions(DialogueOption[] options, int selectedOptionIndex)
	{
		RecyleOptionViews();
		await Task.CompletedTask;
	}

	#endregion Public Methods

	#region Protected Methods

	protected virtual void SetPreviousLineLabelText(LocalizedLine line)
	{
		if (PreviousLineLabel == null)
		{
			return;
		}

		if (line == null)
		{
			PreviousLineContainer.Hide();
			return;
		}

		PreviousLineContainer.Show();
		PreviousLineLabel.Text = line.Text.Text;
		PreviousLineLabel.VisibleCharacters = -1;
	}

	protected virtual bool TryGetOptionView(out OptionView optionView)
	{
		if (_optionViewsPool.Any())
		{
			optionView = _optionViewsPool.First();
			optionView.Show();
			return true;
		}

		if (OptionViewPrototype != null)
		{
			optionView = OptionViewPrototype.InstantiateOrNull<OptionView>();
			if (optionView != null)
			{
				return true;
			}
		}

		GD.PushError($"Failed to create OptionView; OptionViewPrototype = {OptionViewPrototype}");
		optionView = null;
		return false;
	}

	protected virtual void RecyleOptionViews()
	{
		var optionViews = OptionViewsContainer.GetChildren()
			.Where(x => typeof(OptionView).IsAssignableFrom(x.GetType()))
			.Select(x => x as OptionView);

		foreach (var optionView in optionViews)
		{
			if (optionView == null)
			{
				GD.PushError("optionView == null");
				continue;
			}

			optionView.Hide();
		}

		_optionViewsPool.AddRange(optionViews);
	}

	#endregion Protected Methods
}

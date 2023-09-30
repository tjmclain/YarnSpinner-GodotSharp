# Yarn Spinner for Godot C#

This plugin is an implementation of the [Yarn Spinner](https://yarnspinner.dev/) tool for writing game dialogue sequences. If you aren't familiar with Yarn Spinner, you can learn more about it by checking out their [documenation](https://docs.yarnspinner.dev/).

This plugin uses C# exclusively. If you are writing your game in GDScript, consider using the awesome [GDYarn plugin](https://godotengine.org/asset-library/asset/747) instead of this one.

The scripts included in this plugin are based on Yarn Spinner's own [Unity package](https://github.com/YarnSpinnerTool/YarnSpinner-Unity), but there are some important differences. Those are noted in the [wiki](https://github.com/tjmclain/YarnSpinner-GodotSharp/wiki/Getting-Started).

If you encounter an issue, please report it to the [Github page for this project](https://github.com/tjmclain/YarnSpinner-GodotSharp).

## Prerequisites

> **⚠️ Important**
>
> In order for the scripts in this plugin to work correctly, you will need to add the following Yarn Spinner NuGet packages to your C# project.
>
> - https://www.nuget.org/packages/YarnSpinner
> - https://www.nuget.org/packages/YarnSpinner.Compiler/

### Option 1: Add packages via command line

1. Open a terminal window in your Godot project's root folder (or whichever folder has your C# project in it)
2. In the terminal, execute the following commands:
   - `dotnet add package YarnSpinner`
   - `dotnet add package YarnSpinner.Compiler`

### Option 2: Add packages via Visual Studio package manager

If you're using Visual Studio as your IDE for your C# project, you can add these packages via the NuGet Package Manager.

1. Open the Package Manager via **Project > Manage Nuget Packages...**
2. On the **Browse** tab, search for YarnSpinner
3. Install the **YarnSpinner** and **YarnSpinner.Compiler** projects by selecting them in the list and then clicking the **Install** button.

![NuGet Package Manager](./addons/yarnspinner_godot/.screenshots/vs_nuget_package_manager_highlights.png)

If you encounter any issues with installing these packages, you can consult the official [NuGet documentation](https://learn.microsoft.com/en-gb/nuget/what-is-nuget). If you don't find any an answer to your problem there, please create an issue on this project's [Github page](https://github.com/tjmclain/YarnSpinner-GodotSharp/issues).

## Getting Started

TODO

## Features

### Runtime:

- [x] `DialogueRunner`: runtime Dialogue state control
- [x] `LineProvider`: serves localized dialogue lines to dialogue views
  - [ ] Serve localized audio files
- [x] `VariableStorage`: stores Yarn variables
  - [x] `Variable`: save Yarn variables as Godot `Variant`s during runtime
- [x] `ActionLibrary`: stores Yarn commands and functions
  - [x] Add `YarnCommand` and `YarnFunction` attributes to methods to find them automatically

### Dialogue Views:

- [x] Dialogue View interfaces: implement one or more of these in a custom `Control` class to present dialogue
  - [x] IDialogueStartedHandler
  - [x] IDialogueCompleteHandler
  - [x] IRunLineHandler
  - [x] IRunOptionsHandler
- [x] Use `async` / `await` operators to present dialogue via multithreaded C# `Task`s
- [x] `DialogueViewGroup`: container for other dialogue views or view groups
- [x] `DialogueLine`: displays a line of dialogue and (optionally) the speaking character's name
- [x] `OptionsListView`: displays dialogue options and handles option selection
- [x] `OptionView`: displays a single option

### Yarn Data:

- [x] `YarnProgram`: a '.yarn' file that contains a single yarn program
  - [x] "Add Line ID Tags": optionally generate and assign `lineid` tags to each line in a '.yarn' file
  - [x] "Export Strings For Translation": optionally export a csv file representing a `StringTable`
- [x] `YarnProject`: collects and compiles several yarn programs into one unified program
- [x] `StringTable`: a collection of `StringTableEntry`s that associates a translatable string with its translations and metadata
  - [x] "Lock" field is used to identify when translations are out of date with their source string
  - [x] "CustomFields" stores any custom values imported from the CSV (e.g. if you add a "Comments" field, it will be stored here)

### TODO

- [ ] Create example scenes to demo addon functionality
- [ ] Allow using Godot's internationalization system for translating strings
- [ ] Cache certain data offline to optimize `DialogueRunner` startup
  - [ ] Cache `YarnProject` program as a `PackedByteArray`
  - [ ] Cache `YarnProject`'s internal `StringTable`
  - [ ] Cache `ActionLibrary`'s lists of `ActionInfo`s

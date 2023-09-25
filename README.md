# Yarn Spinner for Godot C#

## About This Plugin

This plugin is an implementation of the [Yarn Spinner](https://yarnspinner.dev/) tool for writing game dialogue sequences. If you aren't familiar with Yarn Spinner, you can learn more about it by checking out their [documenation](https://docs.yarnspinner.dev/).

This plugin uses C# exclusively. If you are writing your game in GDScript, consider using the awesome [GDYarn plugin](https://godotengine.org/asset-library/asset/747) instead of this one.

The scripts included in this plugin are based on Yarn Spinner's own [Unity package](https://github.com/YarnSpinnerTool/YarnSpinner-Unity), but there are some important differences. Those are noted below.

If you encounter an issue, please report it to the [Github page for this project](https://github.com/tjmclain/YarnSpinner-GodotSharp).

## Prerequisites

> ### ⚠️ Important
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

![NuGet Package Manager](/addons/yarnspinner_godot/.screenshots/vs_nuget_package_manager_highlights.png)

If you encounter any issues with installing these packages, you can consult the official [NuGet documentation](https://learn.microsoft.com/en-gb/nuget/what-is-nuget). If you don't find any an answer to your problem there, please create an issue on this project's [Github page](https://github.com/tjmclain/YarnSpinner-GodotSharp/issues).

## Further Documentation

If you are looking for more information about this plugin and best practices for its use, refer to the [wiki on GitHub](https://github.com/tjmclain/YarnSpinner-GodotSharp/wiki/Getting-Started).

# Random Toolbox

Random Toolbox is a lightweight Windows desktop utility for everyday random tasks. It is built with WPF and .NET 8, uses the operating system's cryptographic random number generator, and stores settings locally as JSON.

## Features

- Random number generation with integer or decimal values
- Duplicate control, decimal precision, sorting, quick ranges, copy and clear actions
- Dice from D4 to D100, custom sides, multiple dice, total/min/max summaries
- Advanced dice expressions such as `2D6+3`, `1D20+5`, and `3D8-2`
- Keep-highest and keep-lowest dice modes
- Short dice-roll and coin-flip animations, configurable in Settings
- Random draw from multiline lists, with repeat and no-repeat modes
- Random ordering of people or other items
- Random grouping by group count or group size
- Searchable and filterable history with per-item delete, copy, and clear actions
- Presets for number, dice, draw, and sorting configurations
- Window size and last-used page restoration
- Light, dark, and system-following theme options
- Safe recovery from missing or corrupted JSON configuration files
- Self-contained single-file Windows EXE publishing

## Requirements

For development, install the .NET 8 SDK or a newer .NET SDK. End users do not need Python, Node.js, or the .NET runtime when using the self-contained EXE.

## Project Layout

```text
RandomToolbox.sln              Solution file
RandomToolbox.App/             WPF desktop application
  Models/                      Settings, history, presets, and result models
  Services/                    Secure random, storage, and history services
  MainWindow.xaml              Main window layout and controls
  MainWindow.xaml.cs           UI interaction and page logic
RandomToolbox.Tests/           Dependency-free core logic smoke tests
```

## Run From Source

```powershell
dotnet run --project RandomToolbox.App
```

## Run Core Tests

```powershell
dotnet run --project RandomToolbox.Tests
```

## Build the Windows EXE

Build the solution in Debug mode:

```powershell
dotnet build RandomToolbox.sln
```

Create a self-contained, single-file Windows x64 executable:

```powershell
dotnet publish RandomToolbox.App -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true
```

The published files are written to:

```text
RandomToolbox.App\bin\Release\net8.0-windows\win-x64\publish\
```

Run `随机工具箱.exe` from that directory. The EXE is self-contained and can be copied to another compatible Windows x64 computer.

## Download the Ready-to-Run EXE

The repository includes a ready-to-run build at:

```text
releases/RandomToolbox-win-x64.exe
```

SHA-256:

```text
C0C7D35C4C5DC8BD41B74592B0E51F8ECCE66F6EA7089BA6A5D1742AADEFF858
```

## Local Data

The application stores its JSON data at:

```text
%APPDATA%\RandomToolbox\data.json
```

The file contains application settings, window settings, recent configurations, history, and presets. If the file is missing, it is created automatically. If it is invalid, the broken file is backed up and the application starts with default settings instead of crashing.

## Error Handling

Invalid values are reported inside the application. Examples include an empty input, non-numeric values, an inverted range, zero dice, dice with fewer than two sides, too many generated values, impossible no-repeat requests, empty draw lists, and insufficient items for grouping.

## License

No license has been selected yet. Add a license file before distributing the project publicly under specific reuse terms.

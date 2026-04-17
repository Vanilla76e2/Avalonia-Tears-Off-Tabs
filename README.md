# TearOffTabs

A demo project for Avalonia UI showing how to implement **tear-off tabs** — tabs that can be dragged out of the main window to create independent floating windows, and snapped back into any existing window.

![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Avalonia](https://img.shields.io/badge/Avalonia-11.3-teal)

## Features

- **Drag to tear off** — drag a tab downward past a threshold to detach it into a new window
- **Snap back** — drag a floating tab over another window's tab strip to merge it back
- **Reorder tabs** — drag tabs horizontally within the same tab strip with a live insert indicator
- **Close tabs** — each tab has an ✕ button; empty windows close automatically
- **Add tabs dynamically** — the toolbar button creates new tabs at runtime
- **Ghost window** — a semi-transparent label follows the cursor during drag for visual feedback
- **MVVM architecture** — built with CommunityToolkit.Mvvm, no code-behind logic in ViewModels

## Tech Stack

| Library | Version |
|---|---|
| [Avalonia UI](https://avaloniaui.net/) | 11.3.11 |
| [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) | 8.2.1 |
| .NET | 8.0 |

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### Run

```bash
git clone Add link here
cd TearOffTabs
dotnet run --project TearOffTabs
```

## How It Works

### Core Components

**`TabShell`** (`Views/Controls/TabShell.axaml`) — the main user control that renders the tab strip and content area. All drag logic lives in its code-behind:

1. `PointerPressed` — records the starting tab and position, captures the pointer
2. `PointerMoved` — decides between reorder (horizontal drag) and tear-off (vertical drag past `TearOffThreshold = 20px`)
3. `PointerReleased` — commits the reorder or cleans up after a tear-off

**`TabShellViewModel`** — holds the `ObservableCollection<TabItemViewModel>` and exposes `AddTab`, `CloseTab`, `MoveTab`, and `TransferTab` methods.

**`WindowManager`** (singleton) — creates `TornWindow` instances, tracks all floating windows, and implements snap-target detection by hit-testing each window's tab strip bounds.

**`GhostWindow`** — a transparent, non-interactive overlay window that follows the cursor during drag to give visual feedback.

### Tear-off Flow

```
PointerMoved (Y delta > 20px)
  └─ WindowManager.CreateWindow(tab, sourceShell, screenPos)
       └─ sourceShell.TransferTab(tab, newShell)
            └─ new TornWindow shown at cursor position

PointerMoved (over another window's tab strip)
  └─ WindowManager.FindSnapTarget(screenPoint)
       └─ tornShell.TransferTab(tab, snapTarget)
            └─ empty TornWindow closed automatically
```

## Project Structure

```
TearOffTabs/
├── Services/
│   └── WindowManager.cs          # Singleton: window lifecycle & snap detection
├── ViewModels/
│   ├── TabItemViewModel.cs        # Single tab: title, content, close command
│   ├── TabShellViewModel.cs       # Tab collection: add/close/move/transfer
│   ├── MainWindowViewModel.cs     # Root VM with demo tabs
│   └── Pages/                     # Demo page view models
├── Views/
│   ├── Controls/
│   │   ├── TabShell.axaml(.cs)    # Tab strip + drag logic
│   │   └── GhostWindow.axaml(.cs) # Drag ghost overlay
│   ├── Pages/                     # Demo page views
│   ├── MainWindow.axaml(.cs)      # Application main window
│   └── TornWindow.axaml(.cs)      # Detached tab window
└── TearOffTabs.csproj
```

## License

[MIT](LICENSE)

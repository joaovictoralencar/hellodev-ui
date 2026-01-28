# HelloDev UI

A modular UI system for Unity games. Provides container-based UI management with animated transitions, navigation stacks, custom selectables with state machines, popup system, and styling components.

## Features

### Container System
- **UIContainer** - Individual UI screen/panel with animated show/hide
- **UIContainerGroup** - Manages multiple containers with navigation history
- **UIContainerGroupManager** - Top-level manager for multiple container groups
- Back navigation with automatic history stack
- Configurable animation easing and duration

### Navigation System
- **UINavigationInputHandler** - Routes Cancel input (Escape/B button) to containers
- **Auto-select** - Automatically selects UI elements when containers show
- **Selection Memory** - Remembers and restores last selected element per container
- **Parent Container Navigation** - Hierarchical back navigation between containers
- Works with both keyboard/mouse and gamepad

### Popup System
- **UIPopupService** - Manages popup queue and lifecycle
- **UIPopup** - Individual popup instance with buttons
- **Popup_SO** - ScriptableObject configuration with localization support
- **PopupRequestEvent** - Event-driven popup requests for decoupled access
- Queue system for multiple popups
- Focus save/restore when popups open/close

### Selectables
- **UISelectable** - Abstract state machine for interactive UI elements
- **UIButton** - Button with state management and click events
- **UIToggle** - Toggle with on/off state events
- **UIInputField** - TMP input field with state management
- **CustomSelectable** - Manual selection control without EventSystem

### Styling
- **TextStyle_SO** - ScriptableObject for text styling (font size, spacing)
- **Colour_SO** - ScriptableObject for reusable color definitions
- **BaseButtonSettings_SO** - Button configuration with scale animations
- **TextStyleUpdater** - Component for applying styles to TextMeshPro elements

### Animation
- Smooth show/hide transitions using PrimeTween
- Configurable easing curves per container
- Unscaled time support for pause menus

## Getting Started

### 1. Install the Package

**Via Package Manager (Local):**
1. Open Unity Package Manager (Window > Package Manager)
2. Click "+" > "Add package from disk"
3. Navigate to this folder and select `package.json`

**Dependencies:**
- `com.hellodev.utils`
- `com.hellodev.events` (for popup system)
- `com.unity.textmeshpro`
- `com.unity.inputsystem` (for navigation input)
- `com.unity.localization` (optional, for localized popups)
- PrimeTween (optional, for smooth animations)

### 2. Create Your First UI Container

1. Create a Canvas with a child Panel
2. Add these components to the Panel:
   - `Canvas` (for sorting)
   - `CanvasGroup` (for alpha fading)
   - `GraphicRaycaster` (for input)
   - `UIContainer` (from HelloDev.UI.Default)

3. Configure in the inspector:
   - Set a unique `ID` (e.g., "MainMenu")
   - Choose `On Start Action` (Show, Hide, etc.)
   - Set `Auto Selectable` to the first button
   - Configure animation easing

### 3. Set Up Navigation Input

1. Create an empty GameObject named `UINavigationHandler`
2. Add `UINavigationInputHandler` component
3. Configure input bindings (defaults work for most cases):
   - Keyboard: Escape
   - Gamepad: B button (East)

### 4. Set Up Popup Service (Optional)

1. Create an empty GameObject named `PopupService`
2. Add `UIPopupService` component
3. Create a popup prefab with `UIPopup` component
4. Assign the prefab to the service
5. Optionally create a `PopupRequestEvent` asset for decoupled access

## UI Hierarchy

The system supports a hierarchical structure:

```
UIContainerGroupManager
├── UIContainerGroup (Main Menu)
│   ├── UIContainer (Title Screen)
│   ├── UIContainer (Settings)
│   └── UIContainer (Credits)
├── UIContainerGroup (Gameplay HUD)
│   ├── UIContainer (HUD)
│   └── UIContainer (Pause Menu)
└── UIContainerGroup (Inventory)
    ├── UIContainer (Items)
    └── UIContainer (Equipment)
```

## Navigation System

The navigation system handles Cancel/Back input and routes it to the appropriate container.

### How It Works

1. **UINavigationInputHandler** listens for Cancel input (Escape/B button)
2. When triggered, it finds the container that owns the currently selected UI element
3. The container's `HandleBack()` is called, which either:
   - Navigates to the parent container (if set)
   - Calls `Group.Back()` (if in a container group)
   - Hides the container (if no parent)

### Setting Up Navigation

```csharp
// In UIContainer inspector:
// - Back Button: Optional button that triggers HandleBack()
// - Parent Container: Container to show when going back
// - Auto Selectable: First element to select when shown
// - Remember Selection: Restore last selection on re-show
```

### Container Navigation

```csharp
using HelloDev.UI.Default;

public class MenuController : MonoBehaviour
{
    [SerializeField] private UIContainer settingsMenu;
    [SerializeField] private UIContainer mainMenu;

    void Start()
    {
        // Set up parent relationship
        // When Back is pressed in settings, it goes to main menu
        // Configure this in inspector via Parent Container field
    }

    // HandleBack() is called automatically by UINavigationInputHandler
    // Or you can call it manually:
    public void OnBackPressed()
    {
        var container = UIContainer.GetContainerForSelection();
        container?.HandleBack();
    }
}
```

## Popup System

The popup system manages modal dialogs with queuing support.

### Quick Popup (Runtime Strings)

```csharp
using HelloDev.UI.Popups;

public class GameManager : MonoBehaviour
{
    [SerializeField] private UIPopupService popupService;

    public void ShowConfirmation()
    {
        popupService.ShowPopup(
            title: "Confirm Action",
            message: "Are you sure you want to quit?",
            buttonLabels: new[] { "Yes", "No" },
            onResult: buttonIndex =>
            {
                if (buttonIndex == 0) // Yes
                    Application.Quit();
            }
        );
    }
}
```

### Configured Popup (ScriptableObject)

1. Create: **Create > HelloDev > UI > Popup**
2. Configure title, message, icon, and buttons
3. Use in code:

```csharp
[SerializeField] private Popup_SO confirmQuitPopup;
[SerializeField] private UIPopupService popupService;

public void ShowQuitConfirmation()
{
    popupService.ShowPopup(confirmQuitPopup, OnQuitResult);
}

void OnQuitResult(int buttonIndex)
{
    if (buttonIndex == 0) // First button
        Application.Quit();
}
```

### Event-Driven Popups (Decoupled)

For systems that shouldn't have direct popup service references:

```csharp
// Create asset: Create > HelloDev > UI > Events > Popup Request Event
[SerializeField] private PopupRequestEvent popupRequestEvent;

public void RequestPopup()
{
    var request = PopupRequest.Quick(
        "Alert",
        "Something happened!",
        new[] { "OK" }
    );
    popupRequestEvent.Raise(request);
}
```

### Popup Configuration (Popup_SO)

| Field | Description |
|-------|-------------|
| `customPrefab` | Optional custom popup prefab |
| `title` | Localized title text |
| `message` | Localized message text |
| `icon` | Optional popup icon |
| `buttons` | Array of button configurations |
| `defaultButtonIndex` | Which button to focus initially |
| `cancelButtonIndex` | Which button Cancel input triggers (-1 = last) |

## Usage Examples

### Container Management

```csharp
using HelloDev.UI.Default;

public class MenuController : MonoBehaviour
{
    [SerializeField] private UIContainer mainMenu;
    [SerializeField] private UIContainer settingsMenu;

    public void ShowSettings()
    {
        mainMenu.Hide();
        settingsMenu.Show();
    }

    // With callbacks
    public void ShowSettingsWithCallback()
    {
        settingsMenu.Show(onShowCallback: () => Debug.Log("Settings shown!"));
    }

    // Instant (no animation)
    public void InstantHide()
    {
        mainMenu.InstaHide();
    }

    // Check visibility
    public bool IsSettingsVisible()
    {
        return settingsMenu.IsVisible();
    }
}
```

### Container Group Navigation

```csharp
using HelloDev.UI.Default;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private UIContainerGroup menuGroup;

    public void ShowScreen(string screenId)
    {
        menuGroup.ShowContainer(screenId);
    }

    public void GoBack()
    {
        menuGroup.Back();  // Uses navigation stack
    }

    public void HideAllScreens()
    {
        menuGroup.HideAll();
    }
}
```

### Custom Buttons with State Events

```csharp
using HelloDev.UI.Default;
using UnityEngine;
using UnityEngine.UI;

public class ButtonFeedback : MonoBehaviour
{
    [SerializeField] private UIButton button;
    [SerializeField] private Image backgroundImage;

    void Start()
    {
        // Subscribe to state changes
        button.NormalStateEvent.AddListener(() => backgroundImage.color = Color.white);
        button.SelectedStateEvent.AddListener(() => backgroundImage.color = Color.yellow);
        button.HighlightedStateEvent.AddListener(() => backgroundImage.color = Color.cyan);
        button.PressedStateEvent.AddListener(() => backgroundImage.color = Color.green);
        button.DisabledStateEvent.AddListener(() => backgroundImage.color = Color.gray);

        // Or use the generic state change event
        button.ChangedStateEvent.AddListener(OnStateChanged);
    }

    void OnStateChanged(UISelectable.SelectableState newState)
    {
        Debug.Log($"Button state: {newState}");
    }
}
```

### Toggle Usage

```csharp
using HelloDev.UI.Default;
using UnityEngine;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private UIToggle musicToggle;
    [SerializeField] private UIToggle sfxToggle;

    void Start()
    {
        musicToggle.OnToggleOn.AddListener(() => AudioManager.EnableMusic(true));
        musicToggle.OnToggleOff.AddListener(() => AudioManager.EnableMusic(false));

        // Or use OnValueChanged
        sfxToggle.OnValueChanged.AddListener(OnSFXChanged);
    }

    void OnSFXChanged(bool isOn)
    {
        AudioManager.EnableSFX(isOn);
    }
}
```

## API Reference

### UIContainer
| Member | Description |
|--------|-------------|
| `ID` | Unique identifier for the container |
| `Show()` | Animated fade in |
| `Hide()` | Animated fade out |
| `InstaShow()` | Instant show (no animation) |
| `InstaHide()` | Instant hide (no animation) |
| `HandleBack()` | Handles Cancel/Back navigation |
| `Focus()` | Focuses the container and selects appropriate element |
| `IsVisible()` | Check if currently visible |
| `IsAnimating()` | Check if animation in progress |
| `autoSelectable` | Selectable to focus when shown |
| `ParentContainer` | Container to navigate to on back |

### UIContainer (Static)
| Member | Description |
|--------|-------------|
| `ActiveContainers` | All currently visible containers |
| `GetContainerForSelection()` | Gets container owning current selection |

### UINavigationInputHandler
| Member | Description |
|--------|-------------|
| `cancelActionName` | Input action name |
| `keyboardCancel` | Keyboard binding (default: Escape) |
| `gamepadCancel` | Gamepad binding (default: B button) |
| `TriggerCancel()` | Manually trigger cancel |
| `SetPopupService()` | Set popup service reference |

### UIPopupService
| Member | Description |
|--------|-------------|
| `ShowPopup(Popup_SO, callback)` | Show configured popup |
| `ShowPopup(title, message, buttons, callback)` | Show quick popup |
| `HandleCancelInput()` | Handle cancel for active popup |
| `ForceCloseCurrentPopup()` | Force close without callback |
| `HasActivePopup` | True if popup is showing |
| `CurrentPopup` | The active popup instance |

### UIPopup
| Member | Description |
|--------|-------------|
| `Setup(Popup_SO, callback)` | Configure from ScriptableObject |
| `Setup(title, message, buttons, callback)` | Configure from strings |
| `Show()` | Display the popup |
| `Close(buttonIndex)` | Close with result |
| `HandleCancel()` | Trigger cancel button |
| `Container` | The UIContainer component |

### PopupRequest
| Member | Description |
|--------|-------------|
| `FromConfig(Popup_SO, callback)` | Create from ScriptableObject |
| `Quick(title, message, buttons, callback)` | Create quick popup |

## Dependencies

### Required
- com.hellodev.utils
- com.hellodev.events
- com.unity.textmeshpro
- com.unity.inputsystem

### Optional
- com.unity.localization (for localized popups)
- PrimeTween (for smooth container animations via ITweenProvider)
- Odin Inspector (for enhanced inspector)

## Changelog

### v1.1.0 (2026-01-27)
**Navigation System:**
- Added UINavigationInputHandler for Cancel/Back routing
- Added container-level HandleBack() with parent navigation
- Added auto-select and selection memory per container
- Added active container registry

**Popup System:**
- Added UIPopupService for popup queue management
- Added UIPopup component for popup instances
- Added Popup_SO for configured popups with localization
- Added PopupRequestEvent for decoupled popup requests
- Added focus save/restore when popups open/close

### v1.0.1 (2026-01-03)
**Logging:**
- Replaced Debug.LogWarning with HelloDev.Logging.Logger for consistent logging across HelloDev packages

### v1.0.0
- Initial release
- UIContainer, UIContainerGroup, UIContainerGroupManager
- UISelectable state machine with UIButton, UIToggle, UIInputField
- CustomSelectable for manual selection control
- TextStyle_SO, Colour_SO, BaseButtonSettings_SO styling

## License

MIT License

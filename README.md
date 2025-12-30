# HelloDev UI

A modular UI system for Unity games. Provides container-based UI management with animated transitions, navigation stacks, custom selectables with state machines, and styling components.

## Features

### Container System
- **UIContainer** - Individual UI screen/panel with animated show/hide
- **UIContainerGroup** - Manages multiple containers with navigation history
- **UIContainerGroupManager** - Top-level manager for multiple container groups
- Back navigation with automatic history stack
- Configurable animation easing and duration

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
- `com.unity.textmeshpro`
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
   - Configure animation easing

### 3. Show/Hide Containers in Code

```csharp
using HelloDev.UI.Default;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    [SerializeField] private UIContainer mainMenu;
    [SerializeField] private UIContainer settingsMenu;

    public void ShowSettings()
    {
        mainMenu.Hide();
        settingsMenu.Show();
    }

    public void HideSettings()
    {
        settingsMenu.Hide();
        mainMenu.Show();
    }
}
```

### 4. Use Container Groups for Navigation

For multi-screen menus with back navigation:

```csharp
using HelloDev.UI.Default;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private UIContainerGroup menuGroup;

    public void ShowSettings()
    {
        menuGroup.ShowContainer("Settings");  // Pushes to back stack
    }

    public void ShowCredits()
    {
        menuGroup.ShowContainer("Credits");
    }

    public void GoBack()
    {
        menuGroup.Back();  // Returns to previous screen
    }
}
```

## Installation

### Via Package Manager (Local)
1. Open Unity Package Manager
2. Click "+" > "Add package from disk"
3. Navigate to this folder and select `package.json`

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

## Usage

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

### Container Group Manager

For managing multiple groups (e.g., switching between menu and gameplay UI):

```csharp
using HelloDev.UI.Default;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIContainerGroupManager uiManager;

    public void ShowGameplayUI()
    {
        uiManager.ShowContainer("GameplayHUD", "HUD");
    }

    public void ShowMainMenu()
    {
        uiManager.ShowContainer("MainMenu", "Title");
    }

    public void GoBackGroup()
    {
        uiManager.Back();
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

    // Programmatic control
    public void SetMusicEnabled(bool enabled)
    {
        musicToggle.SetIsOn(enabled);
    }
}
```

### Input Field Usage

```csharp
using HelloDev.UI.Default;
using UnityEngine;

public class LoginPanel : MonoBehaviour
{
    [SerializeField] private UIInputField usernameField;
    [SerializeField] private UIInputField passwordField;

    void Start()
    {
        usernameField.OnEndEdit.AddListener(OnUsernameEntered);
        passwordField.OnTextChanged.AddListener(OnPasswordTyping);
    }

    void OnUsernameEntered(string username)
    {
        Debug.Log($"Username: {username}");
    }

    void OnPasswordTyping(string password)
    {
        // Validate password strength in real-time
    }

    public void ClearFields()
    {
        usernameField.SetText("");
        passwordField.SetText("");
    }
}
```

### Custom Selectable for Lists

```csharp
using HelloDev.UI.Default;
using UnityEngine;

public class ListItem : MonoBehaviour
{
    [SerializeField] private CustomSelectable selectable;

    public void Select()
    {
        selectable.Select();  // Manual selection
    }

    public void Deselect()
    {
        selectable.Deselect();
    }

    void Start()
    {
        selectable.ManualSelectedEvent.AddListener(OnSelected);
        selectable.ManualDeselectedEvent.AddListener(OnDeselected);
    }

    void OnSelected() => Debug.Log("Item selected");
    void OnDeselected() => Debug.Log("Item deselected");
}
```

### Text Styling

1. Create a TextStyle_SO: **Create > HelloDev > UI > Text Style**
2. Configure font size, character spacing, etc.
3. Add **TextStyleUpdater** component to your TextMeshPro object
4. Assign the TextStyle_SO reference

```csharp
using HelloDev.UI.Default;
using UnityEngine;

public class DynamicText : MonoBehaviour
{
    [SerializeField] private TextStyleUpdater textUpdater;
    [SerializeField] private TextStyle_SO normalStyle;
    [SerializeField] private TextStyle_SO highlightedStyle;

    public void Highlight()
    {
        textUpdater.TextStyleSO = highlightedStyle;  // Auto-applies
    }

    public void Normal()
    {
        textUpdater.TextStyleSO = normalStyle;
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
| `SetVisibility(visible, instant)` | Flexible visibility control |
| `IsVisible()` | Check if currently visible |
| `IsAnimating()` | Check if animation in progress |
| `onShow` | Event fired when show completes |
| `onHide` | Event fired when hide completes |
| `onStartShow` | Event fired when show starts |
| `onStartHide` | Event fired when hide starts |

### UIContainerGroup
| Member | Description |
|--------|-------------|
| `GroupID` | Unique identifier for the group |
| `Containers` | All child containers |
| `CurrentContainer` | Currently visible container |
| `ShowContainer(id)` | Show container by ID |
| `ShowContainer(container)` | Show container by reference |
| `Back()` | Navigate to previous container |
| `HideAll()` | Hide all containers |

### UIContainerGroupManager
| Member | Description |
|--------|-------------|
| `ShowContainer(groupId, containerId)` | Show specific container in group |
| `ShowContainerGroup(group)` | Show entire group |
| `Back()` | Navigate to previous group |
| `HideAll()` | Hide all groups |

### UISelectable
| Member | Description |
|--------|-------------|
| `IsInteractable` | Get/set interactability |
| `SetInteractable(bool)` | Set interactability |
| `NormalStateEvent` | Event for normal state |
| `SelectedStateEvent` | Event for selected state |
| `HighlightedStateEvent` | Event for highlighted state |
| `PressedStateEvent` | Event for pressed state |
| `DisabledStateEvent` | Event for disabled state |
| `ChangedStateEvent` | Event for any state change |

### UIButton
| Member | Description |
|--------|-------------|
| `OnClick` | Click event (UnityButton.onClick) |
| `IsInteractable` | Button interactability |
| `DeselectOnClick` | Auto-deselect after click |

### UIToggle
| Member | Description |
|--------|-------------|
| `IsOn` | Toggle state |
| `SetIsOn(bool)` | Set toggle state |
| `OnToggleOn` | Event when toggled on |
| `OnToggleOff` | Event when toggled off |
| `OnValueChanged` | Event for any value change |

### UIInputField
| Member | Description |
|--------|-------------|
| `Text` | Input text content |
| `SetText(string)` | Set text programmatically |
| `IsFocused` | Whether field is focused |
| `ActivateInputField()` | Activate keyboard input |
| `OnTextChanged` | Event for text changes |
| `OnEndEdit` | Event when editing ends |

### TextStyle_SO
| Member | Description |
|--------|-------------|
| `ApplyTo(TextMeshProUGUI)` | Apply style to text component |
| `fontSize` | Font size |
| `characterSpacing` | Character spacing |
| `wordSpacing` | Word spacing |
| `lineSpacing` | Line spacing |

## Dependencies

### Required
- com.hellodev.utils
- com.unity.textmeshpro

### Optional
- PrimeTween (for smooth container animations via ITweenProvider)
- Odin Inspector (for enhanced inspector)

## Changelog

### v1.0.0
- Initial release
- UIContainer, UIContainerGroup, UIContainerGroupManager
- UISelectable state machine with UIButton, UIToggle, UIInputField
- CustomSelectable for manual selection control
- TextStyle_SO, Colour_SO, BaseButtonSettings_SO styling

## License

MIT License

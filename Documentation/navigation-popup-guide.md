# Navigation and Popup System Guide

*Last Updated: 2026-01-27*

---

## What You'll Build

By the end of this guide, you'll have a fully functional UI system that:

- Navigates between menu screens with Escape/B button
- Remembers which button was selected when returning to a screen
- Shows popups that queue and handle Cancel input properly
- Works with both keyboard/mouse and gamepad

**Final Result Preview:**

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           MAIN MENU                                     │
│                                                                         │
│                    [ New Game ]  ← Auto-selected                        │
│                    [ Continue ]                                         │
│                    [ Settings ]  ← Press Enter to open                  │
│                    [ Quit ]                                             │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Press Settings
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          SETTINGS                                       │
│                                                                         │
│    [ Audio ]        [ Video ]        [ Controls ]                       │
│                                                                         │
│                                              [ Back ]  ← Escape returns │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Press Quit (from main menu)
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│                    ┌───────────────────────────┐                        │
│                    │     Confirm Quit?         │                        │
│                    │                           │                        │
│                    │  Are you sure you want    │  ← Popup               │
│                    │  to quit the game?        │                        │
│                    │                           │                        │
│                    │   [ Yes ]    [ No ]       │  ← Escape triggers No  │
│                    └───────────────────────────┘                        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Prerequisites

### Required Packages

| Package | Purpose | How to Verify |
|---------|---------|---------------|
| **HelloDev UI** | The container and navigation system | Check `Assets/HelloDev/com.hellodev.ui` exists |
| **HelloDev Events** | Event system for popups | Check `Assets/HelloDev/com.hellodev.events` exists |
| **TextMeshPro** | UI text rendering | Window > TextMeshPro > Import TMP Essential Resources |
| **Input System** | For navigation input | Window > Package Manager > Input System |

### Optional Packages

| Package | Purpose | Needed For |
|---------|---------|------------|
| **Localization** | Multi-language popup text | Localized popup titles/messages |
| **PrimeTween** | Smooth animations | Container show/hide animations |
| **Odin Inspector** | Enhanced editor | Better inspector experience |

---

## Glossary

| Term | Meaning |
|------|---------|
| `UIContainer` | A UI panel/screen with show/hide animations |
| `UIContainerGroup` | Manages multiple containers with back stack |
| `Parent Container` | The container to return to when pressing Back |
| `Auto Selectable` | The UI element to focus when a container opens |
| `UIPopupService` | Manages popup queue and display |
| `Popup_SO` | ScriptableObject defining popup content |
| `PopupRequest` | Data structure for requesting a popup |

---

## Part 1: Setting Up Navigation

### Step 1.1: Create the Navigation Input Handler

1. In **Hierarchy**, right-click > **Create Empty**
2. Rename to: `UINavigationHandler`
3. **Add Component** > search `UINavigationInputHandler` > add it

### Step 1.2: Configure Input Bindings

| Field | Default Value | Description |
|-------|---------------|-------------|
| **Cancel Action Name** | `UI_Cancel` | Internal action name |
| **Keyboard Cancel** | `<Keyboard>/escape` | Escape key |
| **Gamepad Cancel** | `<Gamepad>/buttonEast` | B button (Xbox) / Circle (PlayStation) |
| **Debug** | unchecked | Enable for console logging |

**You can customize bindings** using Input System paths:
- `<Keyboard>/backspace` - Use Backspace instead
- `<Gamepad>/buttonSouth` - Use A button instead

### Checkpoint: Verify Setup

1. Enter **Play Mode**
2. Press **Escape**
3. Check Console for: `Cancel input performed` (if debug is enabled)
4. Exit Play Mode

---

## Part 2: Creating UI Containers with Navigation

### Step 2.1: Create a Main Menu Container

1. In **Hierarchy**, right-click > **UI > Canvas**
2. Right-click on **Canvas** > **UI > Panel**
3. Rename Panel to: `MainMenuContainer`
4. Add required components:
   - **Add Component** > `Canvas`
   - **Add Component** > `Canvas Group`
   - **Add Component** > `Graphic Raycaster`
   - **Add Component** > `UIContainer`

5. Configure **UIContainer**:

| Field | Value | Why |
|-------|-------|-----|
| **ID** | `MainMenu` | Unique identifier |
| **On Start Action** | `InstaShow` | Shows immediately on start |
| **Auto Selectable** | (assign later) | First button to select |
| **Remember Selection** | checked | Restores last selection |

### Step 2.2: Add Buttons to Main Menu

1. Right-click on `MainMenuContainer` > **UI > Button - TextMeshPro**
2. Rename to: `NewGameButton`
3. Set button text to: `New Game`
4. Repeat for: `SettingsButton`, `QuitButton`

5. Select `MainMenuContainer`
6. Drag `NewGameButton` to **Auto Selectable** field

### Step 2.3: Create a Settings Container

1. Right-click on **Canvas** > **UI > Panel**
2. Rename to: `SettingsContainer`
3. Add same components as Main Menu
4. Configure **UIContainer**:

| Field | Value | Why |
|-------|-------|-----|
| **ID** | `Settings` | Unique identifier |
| **On Start Action** | `InstaHide` | Hidden on start |
| **Parent Container** | `MainMenuContainer` | Where Back navigates to |
| **Auto Selectable** | (first settings button) | First button to select |

5. Add buttons: `AudioButton`, `VideoButton`, `BackButton`

### Step 2.4: Wire Up Navigation

In the **SettingsContainer** inspector:
1. Find **Back Button** field under Navigation
2. Drag `BackButton` to this field

**What this does:** When `BackButton` is clicked, it calls `HandleBack()` which navigates to the parent container.

### Step 2.5: Connect Main Menu to Settings

Create a script or use UnityEvents:

**Option A: Button OnClick (No Code)**
1. Select `SettingsButton` in Main Menu
2. In **Button** component, click **+** under On Click()
3. Drag `SettingsContainer` to the object field
4. Select function: `UIContainer > Show`

**Option B: Script**
```csharp
using HelloDev.UI.Default;
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIContainer mainMenu;
    [SerializeField] private UIContainer settings;

    public void OpenSettings()
    {
        mainMenu.Hide();
        settings.Show();
    }
}
```

### Checkpoint: Test Navigation

1. Enter **Play Mode**
2. Main Menu should appear with `New Game` button selected
3. Use arrow keys/gamepad to navigate to Settings
4. Press **Enter/A** to open Settings
5. Settings should appear with first button selected
6. Press **Escape/B** to go back
7. Main Menu should appear with Settings button selected (remembered!)

---

## Part 3: Understanding Navigation Flow

### How Cancel/Back Works

```
                    ┌─────────────────────────────────────┐
                    │     UINavigationInputHandler        │
                    │                                     │
                    │  Listens for Escape/B button        │
                    └──────────────┬──────────────────────┘
                                   │
                                   │ Cancel pressed
                                   ▼
                    ┌─────────────────────────────────────┐
                    │  UIContainer.GetContainerForSelection()  │
                    │                                     │
                    │  Finds which container owns the     │
                    │  currently selected UI element      │
                    └──────────────┬──────────────────────┘
                                   │
                                   │ Container found
                                   ▼
                    ┌─────────────────────────────────────┐
                    │      container.HandleBack()         │
                    └──────────────┬──────────────────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                    │
              ▼                    ▼                    ▼
     ┌────────────────┐   ┌────────────────┐   ┌────────────────┐
     │ Has Group?     │   │ Has Parent?    │   │ No Parent      │
     │                │   │                │   │                │
     │ Group.Back()   │   │ Hide self,     │   │ Just Hide      │
     │                │   │ Show parent    │   │                │
     └────────────────┘   └────────────────┘   └────────────────┘
```

### Container States

| State | Description |
|-------|-------------|
| **Active** | Visible and receiving input |
| **Animating** | Show/hide animation in progress |
| **Hidden** | Not visible, no input |

### Selection Memory

When a container hides:
1. It saves the currently selected element (if within this container)
2. When the container re-shows, it restores that selection
3. If the saved element is invalid, falls back to `autoSelectable`

---

## Part 4: Setting Up the Popup System

### Step 4.1: Create the Popup Service

1. In **Hierarchy**, right-click > **Create Empty**
2. Rename to: `PopupService`
3. **Add Component** > `UIPopupService`

### Step 4.2: Create a Popup Prefab

#### Create the Popup Container

1. Right-click in **Project** > **Create > Prefab** (or drag from scene later)
2. Create a new Panel in your Canvas temporarily
3. Rename to: `PopupPrefab`
4. Add components:
   - `Canvas` (sorting order: 100)
   - `Canvas Group`
   - `Graphic Raycaster`
   - `UIContainer`
   - `UIPopup`

#### Configure UIContainer

| Field | Value |
|-------|-------|
| **ID** | `Popup` |
| **On Start Action** | `InstaHide` |
| **Open Duration** | `0.2` |
| **Hide Duration** | `0.15` |

#### Add Popup UI Elements

Create this hierarchy:
```
PopupPrefab (UIContainer, UIPopup)
├── Background (Image - dark semi-transparent)
├── PopupWindow (Image - popup background)
│   ├── TitleText (TextMeshPro with LocalizeStringEvent)
│   ├── MessageText (TextMeshPro with LocalizeStringEvent)
│   ├── IconImage (Image - optional)
│   └── ButtonContainer (Horizontal Layout Group)
│       └── (buttons will be spawned here)
```

#### Configure UIPopup

| Field | What to Assign |
|-------|----------------|
| **Title Text** | The TitleText LocalizeStringEvent |
| **Message Text** | The MessageText LocalizeStringEvent |
| **Icon Image** | The IconImage (optional) |
| **Button Container** | The ButtonContainer transform |
| **Button Prefab** | A UIButton prefab for popup buttons |

#### Create the Button Prefab

1. Create a button with `UIButton` component
2. Add `TextMeshPro` child for label
3. Save as prefab: `PopupButton`

#### Save the Popup Prefab

1. Drag `PopupPrefab` to Project folder
2. Delete the original from scene

### Step 4.3: Configure UIPopupService

| Field | What to Assign |
|-------|----------------|
| **Default Prefab** | `PopupPrefab` |
| **Popup Container** | Parent transform for popups (or leave empty for service transform) |
| **Request Event** | (optional) A `PopupRequestEvent` asset |

### Step 4.4: Connect to Navigation Handler

1. Select `UINavigationHandler`
2. Drag `PopupService` to **Popup Service** field

**Why?** When Cancel is pressed while a popup is active, it should close the popup instead of navigating back.

---

## Part 5: Creating Popup Content

### Option A: Quick Popup (Code Only)

No assets needed, just code:

```csharp
using HelloDev.UI.Popups;
using UnityEngine;

public class QuitConfirmation : MonoBehaviour
{
    [SerializeField] private UIPopupService popupService;

    public void ShowQuitPopup()
    {
        popupService.ShowPopup(
            title: "Quit Game?",
            message: "Are you sure you want to quit?",
            buttonLabels: new[] { "Yes", "No" },
            onResult: OnQuitResult
        );
    }

    void OnQuitResult(int buttonIndex)
    {
        if (buttonIndex == 0) // Yes
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        // buttonIndex 1 (No) - popup just closes
    }
}
```

### Option B: Configured Popup (ScriptableObject)

Better for localization and reusability:

#### Step 5.1: Create Popup Asset

1. Right-click in Project > **Create > HelloDev > UI > Popup**
2. Rename to: `SO_Popup_QuitConfirm`

#### Step 5.2: Configure Popup Asset

| Field | Value |
|-------|-------|
| **Title** | Create localized string "Quit Game?" |
| **Message** | Create localized string "Are you sure?" |
| **Icon** | (optional) Warning icon sprite |
| **Default Button Index** | `1` (No - safer default) |
| **Cancel Button Index** | `1` (No - Escape = No) |

#### Step 5.3: Add Buttons

1. Expand **Buttons** array
2. Add Element 0:
   - **Label**: "Yes"
   - **Type**: `Yes`
3. Add Element 1:
   - **Label**: "No"
   - **Type**: `No`

#### Step 5.4: Use in Code

```csharp
[SerializeField] private Popup_SO quitPopup;
[SerializeField] private UIPopupService popupService;

public void ShowQuitPopup()
{
    popupService.ShowPopup(quitPopup, OnQuitResult);
}
```

### Option C: Event-Driven Popup (Decoupled)

For systems that shouldn't reference the popup service directly:

#### Step 5.1: Create Event Asset

1. Right-click in Project > **Create > HelloDev > UI > Events > Popup Request Event**
2. Rename to: `SO_Event_PopupRequest`

#### Step 5.2: Assign to Service

1. Select `PopupService` in Hierarchy
2. Drag `SO_Event_PopupRequest` to **Request Event** field

#### Step 5.3: Use Anywhere

```csharp
[SerializeField] private PopupRequestEvent popupEvent;

public void RequestPopup()
{
    var request = PopupRequest.Quick(
        "Information",
        "Something happened!",
        new[] { "OK" }
    );
    popupEvent.Raise(request);
}
```

---

## Part 6: Advanced Navigation Patterns

### Pattern: Container Groups

For complex menus with multiple screens:

```
UIContainerGroup (Main Menu Group)
├── UIContainer (Title Screen)
├── UIContainer (New Game)
├── UIContainer (Continue)
└── UIContainer (Settings)
```

**How Group Navigation Works:**
1. Group maintains a back stack
2. `ShowContainer("Settings")` pushes to stack
3. `Back()` pops from stack and shows previous

### Pattern: Nested Containers

For sub-menus within screens:

```
UIContainer (Settings)
├── Buttons Panel
└── UIContainer (Audio Settings)  ← Nested
    └── Parent Container = Settings
```

When Back is pressed in Audio Settings:
1. `HandleBack()` is called
2. Audio Settings hides
3. Settings shows (parent container)

### Pattern: Modal Popup Priority

Popups take precedence over normal navigation:

```
Cancel pressed
    │
    ├── Popup active? → Close popup
    │
    └── No popup → Normal container HandleBack()
```

---

## Part 7: Designer Checklist

### For Each UI Container

- [ ] Add `Canvas`, `CanvasGroup`, `GraphicRaycaster` components
- [ ] Add `UIContainer` component
- [ ] Set unique **ID**
- [ ] Set **On Start Action** (usually `InstaHide` except for initial screen)
- [ ] Assign **Auto Selectable** (first interactable element)
- [ ] Enable **Remember Selection** (usually yes)
- [ ] Set **Parent Container** if this is a sub-menu
- [ ] Assign **Back Button** if there's a visual back button
- [ ] Configure animation **Duration** and **Ease**

### For Navigation Handler

- [ ] One `UINavigationInputHandler` in scene
- [ ] Bindings match your project's input scheme
- [ ] Popup service assigned (if using popups)

### For Popup System

- [ ] One `UIPopupService` in scene
- [ ] Default popup prefab created and assigned
- [ ] Button prefab created and assigned to popup
- [ ] Request event created (if using decoupled popups)

### For Each Popup Asset

- [ ] Title and message configured (localized if needed)
- [ ] Buttons defined with labels
- [ ] Default button index set (safest option)
- [ ] Cancel button index set (usually last button)

---

## Troubleshooting

### Navigation Doesn't Work

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| Nothing happens on Escape | No handler in scene | Add `UINavigationInputHandler` |
| Console: "No container found" | No visible containers | Check containers are shown |
| Wrong container receives Back | Selection in wrong container | Verify element is child of correct container |

### Selection Issues

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| Nothing selected on show | No auto selectable set | Assign **Auto Selectable** |
| Wrong element selected | Auto selectable not interactable | Check element is active and interactable |
| Selection not remembered | Remember Selection unchecked | Enable **Remember Selection** |

### Popup Issues

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| Popup doesn't appear | No prefab assigned | Assign **Default Prefab** on service |
| Buttons don't work | Button prefab missing UIButton | Add `UIButton` component |
| Cancel doesn't close popup | Service not linked to handler | Assign popup service to navigation handler |
| Multiple popups overlap | Using Show instead of queue | Always use `ShowPopup()` method |

### Debugging Tips

1. **Enable debug logging**: Check **Debug** on `UINavigationInputHandler`
2. **Check active containers**: Use `UIContainer.ActiveContainers` in code
3. **Verify hierarchy**: Ensure UI elements are children of their container
4. **Test in isolation**: Test one container at a time

---

## API Quick Reference

### UIContainer

```csharp
// Show/Hide
container.Show();
container.Hide();
container.InstaShow();
container.InstaHide();

// Navigation
container.HandleBack();  // Called by navigation handler
container.Focus();       // Selects appropriate element

// State
container.IsVisible();
container.IsAnimating();

// Static
UIContainer.GetContainerForSelection();  // Gets container for current selection
UIContainer.ActiveContainers;            // All visible containers
```

### UIPopupService

```csharp
// Quick popup
popupService.ShowPopup("Title", "Message", new[] { "OK" }, OnResult);

// Configured popup
popupService.ShowPopup(popupSO, OnResult);

// Custom prefab popup
popupService.ShowPopup(customPrefab, "Title", "Message", buttons, OnResult);

// Cancel handling
popupService.HandleCancelInput();

// State
popupService.HasActivePopup;
popupService.CurrentPopup;
```

### PopupRequest

```csharp
// From config
var request = PopupRequest.FromConfig(popupSO, OnResult);

// Quick
var request = PopupRequest.Quick("Title", "Message", buttons, OnResult);

// Raise via event
popupRequestEvent.Raise(request);
```

---

## Related Documentation

- [README.md](../README.md) - Package overview and API reference
- [HelloDev Events](../../com.hellodev.events/README.md) - Event system used by popups

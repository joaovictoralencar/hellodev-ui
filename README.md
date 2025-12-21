# HelloDev UI

A modular UI system for Unity games. Provides container-based UI management, custom selectables, and styling components.

## Features

### Container System
- **UIContainer** - Base container with show/hide animations
- **UIContainerGroup** - Manages multiple containers with navigation
- **UIContainerGroupManager** - Manages multiple container groups

### Selectables
- **UISelectable** - Abstract base for selectable UI elements
- **UIButton** - Button with state management and events
- **UIToggle** - Toggle with state management
- **UIInputField** - Input field with state management
- **CustomSelectable** - Manual selection management without Unity's EventSystem

### Styling
- **TextStyle_SO** - ScriptableObject for text styling (font, size, color)
- **Colour_SO** - ScriptableObject for color management
- **BaseButtonSettings_SO** - Button configuration settings
- **TextStyleUpdater** - Component for applying text styles to TextMeshPro elements

## Installation

### Via Package Manager (Local)
1. Open Unity Package Manager
2. Click "+" > "Add package from disk"
3. Navigate to this folder and select `package.json`

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
}
```

### Custom Buttons

```csharp
using HelloDev.UI.Default;

public class MyButton : MonoBehaviour
{
    [SerializeField] private UIButton button;

    void Start()
    {
        button.OnClick.AddListener(HandleClick);
    }

    void HandleClick()
    {
        Debug.Log("Button clicked!");
    }
}
```

### Text Styling

1. Create a TextStyle_SO asset: `Create > HelloDev > UI > Text Style`
2. Configure font, size, color settings
3. Add TextStyleUpdater component to your TextMeshPro object
4. Assign the TextStyle_SO to the updater

## Dependencies

### Required
- com.hellodev.utils
- com.unity.textmeshpro

### Optional
- DOTween (for container animations)
- Odin Inspector (for enhanced inspector)

## License

MIT License

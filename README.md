# HelloDev UI

A modular UI system for Unity games. Provides container-based UI management, custom selectables, and styling components.

## Installation

Add the package to Unity Package Manager -> Add package from Git URL:

$installUrl

(For a specific release, add #v1.0.0 to the URL after tagging a release.)

## Usage

Import into your project and follow the HelloDev docs.

Key changes in v1.0.2
- Moved all prefabs from Assets/Prefabs to Samples~/Prefabs so prefabs are optional and won't be compiled into the package.
- Input-specific prefabs (InputButtons) moved to Samples~/Prefabs/BaseComponents/Buttons/InputButtons and are optional; they depend on the external com.hellodev.input repository and are not a hard dependency of this package.
- Added com.unity.localization v1.5.11 as a hard dependency to provide built-in localization support.

Installation
Add the package to Unity Package Manager -> Add package from Git URL:

$installUrl#v1.0.2

See CHANGELOG.md for full release notes.

# Input Prefabs (Samples~/InputButtons)

These are optional input-specific UI prefabs. They require the Unity Input System and (optionally) the HelloDev Input package to compile and work correctly.

Requirements
- Unity Input System (com.unity.inputsystem)
- Optional: HelloDev Input package (com.hellodev.input)
  GitHub: https://github.com/joaovictoralencar/com.hellodev.input

Quick install
1. In Unity: Window → Package Manager → search for "Input System" (Unity Registry) and Install.
2. (If prompted) Enable the Input System backend in Player Settings → Active Input Handling and restart the Editor.
3. Optionally add HelloDev Input: Package Manager → + → Add package from Git URL → `https://github.com/joaovictoralencar/com.hellodev.input.git` (or use a tag/branch).
4. Package Manager → HelloDev UI → Samples → import "Input Prefabs (Requires Input System & com.hellodev.input)".

Troubleshooting
- If you see compile errors referencing `UnityEngine.InputSystem`, ensure step 1 is completed and the package is installed.
- If prefabs reference scripts from `com.hellodev.input`, add that package or copy the prefabs into your project and adapt the components.

Notes
- These samples are optional; they are intentionally separated so the main package does not hard-depend on the Input package.
- If you prefer, clone the com.hellodev.input repo and add it as a local package for development.

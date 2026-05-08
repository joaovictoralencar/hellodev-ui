# Changelog

All notable changes to this project are documented here.

## [1.0.5] - 2026-05-08
- Add Samples~/InputButtons/README with install instructions.

## [1.0.4] - 2026-05-08
- Split samples into two entries: "Base Prefabs" (Samples~/Prefabs) and "Input Prefabs" (Samples~/InputButtons). This makes input prefabs optional and clearly separated.
- Updated package.json samples and bumped version to 1.0.4.

## [1.0.3] - 2026-05-08
- Made Input System usage optional at compile-time (ENABLE_INPUT_SYSTEM) and exposed samples in package.json; bumped to 1.0.3.

## [1.0.2] - 2026-05-08
- Moved prefabs from `Assets/Prefabs` to `Samples~/Prefabs` so they are optional samples and not compiled into the package.
- Moved InputButtons to `Samples~/Prefabs/BaseComponents/Buttons/InputButtons` (optional; depends on `com.hellodev.input`).
- Added `com.unity.localization` v1.5.11 as a hard dependency to enable localization features.

## [1.0.1] - 2026-05-08
- Added Unity Localization as dependency; moved InputButtons to `Samples~/InputButtons`.

## [1.0.0]
- Initial package release.

# Color Database & Theming - Implementation Plan

## Validation & critique of last prompt
- The request is well-scoped: you want a single runtime-accessible color database, inspector-friendly authoring, generated constants, and Addressables support.
- Covered items: data model, binders, constants generation, Addressables, locator key generation (constant), editor buttons and UX.
- Missing / ambiguous items to be explicit about:
  - Migration strategy for existing Colour_SO references (keep compatibility by allowing Colour_SO as leaf values).
  - Package dependencies: TextMeshPro is referenced in this repo — plan will include TMP binders.
  - Multiple databases vs single global database: user previously chose a single database registered by a generated key constant — plan assumes single primary database but can support multiple keyed databases.
  - Runtime mutation: you marked per-slot runtime mutation out-of-scope; plan treats runtime color changes as theme switches only, but supports optional per-slot updates later.
  - Addressables config: plan will include fallback to Resources for projects not using Addressables during development.
  - Editor UX: detailed inspector and migration tools are needed to make adoption easy.

## High-level approach
- Keep ScriptableObjects as the canonical authoring format (reuse existing `Colour_SO`).
- Add `ColorDatabase_SO` (addressable asset) that contains:
  - List of `ColorSlot` entries (id GUID, name, role, tag, description, optional default Colour_SO reference).
  - List of `ColorTheme` entries (id, name, list of ColorValue {slotId, Color}).
  - activeThemeId and API GetColor(slotId, themeId).
  - Editor buttons: GenerateConstants, SyncHex, AddThemeFromActive.
- Add `ColorDatabaseRuntime` MonoBehaviour that loads the SO via Addressables at startup (with Resources fallback), registers with a keyed locator under the generated DatabaseKey constant, and exposes events OnThemeChanged(string themeId) and OnSlotColorChanged(string slotId).
- Create UI binders for uGUI + TMP: `BaseColorBinder` (slotId or direct Colour_SO), `ImageColorBinder`, `TMPColorBinder`, `ButtonColorBinder` (applies ColorBlock states). Binders subscribe on OnEnable and apply current color immediately.
- Editor generator `ColorIdGenerator` that writes `ColorIds.cs`, `ColorThemeIds.cs`, and `DatabaseKeys.cs` (includes the chosen DB key constant).

## Files to add/modify
- Runtime/Scripts/
  - ColorDatabase_SO.cs (ScriptableObject)
  - ColorDatabaseRuntime.cs (MonoBehaviour host + loader)
  - Models/ColorSlot.cs, ColorTheme.cs, ColorValue.cs (serializables)
  - Bindings/BaseColorBinder.cs, ImageColorBinder.cs, TMPColorBinder.cs, ButtonColorBinder.cs
- Editor/
  - ColorIdGenerator.cs (editor code to generate constants)
  - ColorDatabase_SO.Editor.cs (custom inspector helpers: swatches, table view)
- Samples/
  - Scenes/ColorDatabaseSample.unity
  - Prefabs/UI sample using binders
- Docs/
  - README update: usage, Addressables setup, migration guide

## Implementation todos (order + dependencies)
1. color-db-so: Implement ColorDatabase_SO (depends: none)
   - Model types, inspector-friendly serialization, GenerateConstants button
2. editor-generator: Implement ColorIdGenerator (depends: 1)
   - Emit ColorIds, ThemeIds, DatabaseKeys (include DB key constant)
3. color-db-runtime: Implement ColorDatabaseRuntime (depends: 1, Addressables API)
   - Addressables load + Resources fallback, locator registration, events
4. binders: Implement binders for Image/TMP/Button (depends: 3)
   - Subscribe/unsubscribe, immediate apply, fallbacks, logger warnings
5. addressables-integration: Mark sample ColorDatabase_SO as addressable and add loader example (depends: 3)
6. samples-tests: Add sample scene and manual acceptance tests (depends: 4,5)
7. docs: Update README and add migration guide (depends: all)

## Acceptance criteria
- ColorDatabase_SO asset created and can author slots/themes in inspector.
- ColorIdGenerator generates constants successfully and database registers under generated key.
- ColorDatabaseRuntime loads asset via Addressables and registers with locator at runtime.
- Bindings (Image/TMP/Button) apply colors immediately and react to theme switches.
- Sample scene demonstrates theme switching and binder usage.

## Risks & mitigations
- Addressables complexity: provide Resources fallback and dev-mode shortcuts.
- Existing Colour_SO references: keep backwards compatibility by allowing binders to accept direct Colour_SO references and database slot IDs.
- TextMeshPro dependency: repo already references TMP; if absent, fallback to UnityEngine.UI.Text (but repo uses TMP so include TMP binder).

## Next steps
- Implement the files above following the todo order.
- Generate tasks in the session tracker (done).

-- End of plan --

# Art Style System - Implementation Plan

This plan outlines the steps for implementing and testing the Art Style system, building upon the initial script creation (Implementation Run 1). Each target focuses on a specific set of features, followed by testing in a dedicated scene (`ArtStyleTestScene`).

**Test Scene Setup (`ArtStyleTestScene`):**

*   Create a new scene.
*   Add a Canvas.
*   Inside the Canvas, create a root `GameObject` (e.g., "StyledPanel").
*   Add UI elements under "StyledPanel":
    *   An `Image` component (`UnityEngine.UI.Image`).
    *   A legacy `Text` component (`UnityEngine.UI.Text`).
    *   A `TextMeshProUGUI` component (`TMPro.TextMeshProUGUI`).
*   Create a simple UI `Button` elsewhere in the scene (for triggering runtime actions like style switching or animations).
*   Create necessary ScriptableObject assets in the project for testing (initial `ArtStyle`, `ArtSetType`, `ArtSet`, `Items`, `ArtSetting`).

---

**Target 1: Core Data Structure Population & Validation**

*   **Goal:** Create initial instances of all required ScriptableObject assets (`Items`, `Sets`, `Types`, `ArtStyle`, `ArtSetting`) representing at least one basic, usable theme. Ensure basic data can be entered via the default Unity inspector.
*   **Implementation:**
    *   Using the Unity Editor: Create -> Art Style -> ...
    *   Create one `ArtSetting` asset (e.g., `GlobalArtSetting`).
    *   Create one `ArtStyle` asset (e.g., `DefaultStyle`).
    *   Create one `ArtSetType` asset (e.g., `PanelType`).
    *   Create one `ArtSet` asset (e.g., `DefaultPanelSet`), assign `PanelType` to it.
    *   Create one of each `Set` asset (`PicSet`, `ColourSet`, `FontSet`, `AnimationSet`) and assign them to `DefaultPanelSet`.
    *   Create 1-2 instances of each `Item` type (`PicItem`, `ColourItem`, `FontItem`, `AnimationItem`) with sample data (sprites, colors, fonts, basic animation settings) and add them to their respective `Set` assets.
    *   Assign `DefaultPanelSet` to `DefaultStyle`.
    *   Assign `PanelType` to `DefaultStyle.availableSetTypes`.
    *   Assign `DefaultStyle` to `GlobalArtSetting.activeArtStyle`.
*   **Files Edited:** None (Asset creation and population in Unity Editor only).
*   **Testing:**
    *   Select each created asset in the Project window.
    *   Verify all `[SerializeField]` fields are visible in the Inspector.
    *   Confirm that you can assign references (Sprites, Fonts, Colors, other SOs) and edit values (floats, bools, curves).
    *   Ensure the basic hierarchy links correctly (ArtSetting -> ArtStyle -> ArtSet -> Sets -> Items).

---

**Target 2: GUID Generation & Read-Only Display**

*   **Goal:** Implement automatic GUID generation for all ScriptableObjects requiring an ID (`Item` types, `ArtSetType`, `ArtSet`, `ArtStyle`). Make the ID field visible but read-only in the inspector.
*   **Implementation:**
    *   Modify the `OnEnable` method (uncomment/add) in each relevant ScriptableObject script to check if `_id` is null or empty and generate a new GUID using `System.Guid.NewGuid().ToString()`.
    *   Create basic custom editor scripts (`Editor` folder required: `Assets/Modules/ArtStyle/Scripts/Editor/`) for each SO type.
    *   In each custom editor's `OnInspectorGUI`, draw the default inspector (`DrawDefaultInspector()`) and then explicitly draw the `_id` property using `EditorGUILayout.PropertyField` with the `GUI.enabled = false` scope or `EditorGUILayout.LabelField` to make it read-only.
*   **Files Edited:**
    *   `Scripts/Data/Items/PicItem.cs`
    *   `Scripts/Data/Items/ColourItem.cs`
    *   `Scripts/Data/Items/FontItem.cs`
    *   `Scripts/Data/Items/AnimationItem.cs`
    *   `Scripts/Data/ArtSetType.cs`
    *   `Scripts/Data/ArtSet.cs`
    *   `Scripts/Data/ArtStyle.cs`
    *   *(New)* `Scripts/Editor/Items/PicItemEditor.cs`
    *   *(New)* `Scripts/Editor/Items/ColourItemEditor.cs`
    *   *(New)* `Scripts/Editor/Items/FontItemEditor.cs`
    *   *(New)* `Scripts/Editor/Items/AnimationItemEditor.cs`
    *   *(New)* `Scripts/Editor/ArtSetTypeEditor.cs`
    *   *(New)* `Scripts/Editor/ArtSetEditor.cs`
    *   *(New)* `Scripts/Editor/ArtStyleEditor.cs`
*   **Testing:**
    *   Select the assets created in Target 1. Verify their `_id` field is now populated and read-only.
    *   Create *new* instances of each SO type via the Assets -> Create menu. Verify they automatically receive a unique GUID upon creation/first inspection.
    *   Duplicate an existing asset (Ctrl+D). Verify the duplicated asset gets a *new*, unique GUID.

---

**Target 3: Basic `ArtSetApplicator` & Element Connection (`FromSet` Mode)**

*   **Goal:** Ensure `ArtSetApplicator` correctly finds and builds lookup dictionaries from a *directly assigned* `ArtSet`. Ensure `Element` components find the applicator and apply styles correctly using only the `FromSet` mode.
*   **Implementation:**
    *   Refine `ArtSetApplicator.Awake/OnEnable` and `BuildLookupDictionaries` to handle the `_selectedArtSet` when `_useArtSetting` is false. Ensure dictionaries are built correctly.
    *   Refine `Element.Awake` to reliably cache the `ArtSetApplicator`.
    *   Refine `Element.ApplyStyle` methods to correctly use `_applicator.TryGet...Item(itemId, out item)` and apply the found item's properties (Sprite, Color, Font) when the corresponding mode is set to `FromSet`. Implement fallback logic using `_applicator.Default...Item` and log warnings if an ID is specified but not found.
*   **Files Edited:**
    *   `Scripts/Components/ArtSetApplicator.cs`
    *   `Scripts/Components/ImageArtElement.cs`
    *   `Scripts/Components/TextArtElement.cs`
    *   `Scripts/Components/AnimationElement.cs` (Verify `Awake` and `ApplyStyle` call `ResolveAnimationData`)
*   **Testing:**
    *   In `ArtStyleTestScene`:
        *   Add `ArtSetApplicator` to "StyledPanel". Set `_useArtSetting = false`. Assign the `DefaultPanelSet` directly to `_selectedArtSet`. Assign valid default items (created in Target 1) to the applicator's default fields.
        *   Add `ImageArtElement` to the Image GameObject. Set `spriteMode = FromSet`, `colourMode = FromSet`. Assign a valid `_picItemId` and `_colourItemId` from `DefaultPanelSet`.
        *   Add `TextArtElement` to the Text GameObject. Set `fontMode = FromSet`, `colourMode = FromSet`. Assign valid `_fontItemId` and `_colourItemId`.
        *   Add `TextArtElement` to the TMP_Text GameObject. Set `fontMode = FromSet`, `colourMode = FromSet`. Assign valid `_fontItemId` and `_colourItemId`.
        *   Ensure `ArtSetApplicator.applyOnStartOrEnable` is true.
    *   Enter Play mode.
        *   Verify the Image shows the correct sprite and color tint.
        *   Verify the Text shows the correct legacy font and color.
        *   Verify the TMP_Text shows the correct TMP font and color.
    *   Stop Play mode. Change one of the Item IDs in an element to an invalid GUID. Enter Play mode. Verify the element uses the default item assigned on the applicator and that a warning is logged to the console.

---

**Target 4: Element Overrides & Default Color Modes**

*   **Goal:** Implement and test all override modes (`Override`, `None`) and default color modes (`DefaultPicColour`, `DefaultFontColour`) in `ImageArtElement` and `TextArtElement`.
*   **Implementation:**
    *   Complete the `switch` statement logic in `ImageArtElement.ApplyStyle` for `SpriteSourceMode.Override`, `SpriteSourceMode.None`, `ColourSourceMode.Override`, `ColourSourceMode.DefaultPicColour`, `ColourSourceMode.None`.
    *   Complete the `switch` statement logic in `TextArtElement.ApplyStyle` for `FontSourceMode.Override`, `ColourSourceMode.Override`, `ColourSourceMode.DefaultFontColour`, `ColourSourceMode.None`.
*   **Files Edited:**
    *   `Scripts/Components/ImageArtElement.cs`
    *   `Scripts/Components/TextArtElement.cs`
*   **Testing:**
    *   In `ArtStyleTestScene` (Play mode or Editor mode if `ApplyStyle` is called `OnValidate` or via editor button):
        *   Select the Image GameObject.
            *   Set `spriteMode = Override`, assign a different sprite. Verify it applies.
            *   Set `colourMode = Override`, assign a different color. Verify it applies.
            *   Set `colourMode = DefaultPicColour`. Verify the color from the *resolved* `PicItem`'s `defaultColour` field is applied (even if `spriteMode` is `Override` or `None`, it should still try to resolve the PicItem by ID for this).
            *   Set `spriteMode = None`, `colourMode = None`. Verify the Image retains its original inspector values.
        *   Select the Text/TMP_Text GameObjects.
            *   Set `fontMode = Override`, assign different fonts. Verify.
            *   Set `colourMode = Override`, assign a different color. Verify.
            *   Set `colourMode = DefaultFontColour`. Verify the color from the *resolved* `FontItem`'s `defaultColour` field is applied.
            *   Set `colourMode = None`. Verify the text retains its original color.

---

**Target 5: `ArtSetting` Integration & Runtime Switching**

*   **Goal:** Enable `ArtSetApplicator` to use the `ArtStyle` from the `ArtSetting` singleton and react to runtime changes of `ArtSetting.ActiveArtStyle`.
*   **Implementation:**
    *   Ensure `ArtSetting.Instance` property is robust (e.g., handles case where asset doesn't exist).
    *   Implement the `ActiveArtStyle` property setter in `ArtSetting` to invoke `OnActiveStyleChanged?.Invoke(value)`.
    *   In `ArtSetApplicator`, implement `OnEnable` to subscribe `HandleGlobalStyleChange` to `ArtSetting.Instance.OnActiveStyleChanged` *if* `_useArtSetting` is true.
    *   Implement `OnDisable` to unsubscribe.
    *   Implement `HandleGlobalStyleChange` to call `DetermineAndApplyStyle()`.
    *   Refine `DetermineActiveArtSet` to handle the `_useArtSetting = true` case: get `ArtSetting.Instance.ActiveArtStyle`, check `_artSetTypeFilter`, call `activeStyle.FindArtSetByType(_artSetTypeFilter)`, and return the result.
    *   Implement `ArtStyle.FindArtSetByType(ArtSetType type)`.
*   **Files Edited:**
    *   `Scripts/Singletons/ArtSetting.cs`
    *   `Scripts/Components/ArtSetApplicator.cs`
    *   `Scripts/Data/ArtStyle.cs`
*   **Testing:**
    *   Create a second `ArtStyle` (`StyleB`) with a corresponding `ArtSet` (`PanelSetB`) of the same `ArtSetType` (`PanelType`), but using different `Items` (different sprites/colors/fonts).
    *   In `ArtStyleTestScene`, configure the `ArtSetApplicator` on "StyledPanel": set `_useArtSetting = true`, assign `PanelType` to `_artSetTypeFilter`.
    *   Ensure `GlobalArtSetting` initially points to `DefaultStyle`.
    *   Enter Play mode. Verify `DefaultStyle` is applied.
    *   Use the test Button and a simple script (`ButtonClicked() => ArtSetting.Instance.ActiveArtStyle = styleBAsset;`) to change `ArtSetting.Instance.ActiveArtStyle` to `StyleB` at runtime.
    *   Verify the UI elements immediately update to reflect `StyleB`.
    *   Add another button/logic to switch back to `DefaultStyle`. Verify switching works both ways.

---

**Target 6: Basic Animation Implementation (Fade)**

*   **Goal:** Implement and test the basic fade-in animation using `AnimationElement` and `AnimationApplicator`.
*   **Implementation:**
    *   Review `AnimationApplicator.FadeCoroutine` for correctness (using `AnimationItem` properties: duration, curve, start opacity). Ensure `Time.unscaledDeltaTime` is used. Handle duration <= 0.
    *   Ensure `AnimationElement.ResolveAnimationData` correctly finds the `AnimationItem` (or default) and resolves the duration (considering override). Add the check for `CanvasGroup` presence if `UseFade` is true.
    *   Ensure `AnimationElement.PlayAnimation` correctly calls `_animationApplicator.Play(this)`.
    *   Ensure `AnimationApplicator.Play` calls `Stop()` first and starts the coroutine.
    *   Ensure `Stop()` stops the coroutine.
*   **Files Edited:**
    *   `Scripts/Components/AnimationApplicator.cs`
    *   `Scripts/Components/AnimationElement.cs`
*   **Testing:**
    *   In `ArtStyleTestScene`:
        *   Add `CanvasGroup`, `AnimationElement`, and `AnimationApplicator` to the Image GameObject.
        *   In `DefaultPanelSet`'s `AnimationSet`, ensure there's an `AnimationItem` with `useFade = true` and sensible duration/curve/startOpacity (e.g., 0.5s, EaseInOut, 0.0). Assign its ID to the `AnimationElement`.
        *   Use the test Button and a script to call `theImageAnimationElement.PlayAnimation()` when clicked.
    *   Enter Play mode. Click the button.
        *   Verify the Image fades in over the specified duration using the defined curve.
        *   Test calling `PlayAnimation` again while it's running (it should restart).
        *   Add another button to call `StopAnimation`. Verify it stops the fade mid-way.
        *   Test the `overrideDuration` feature on `AnimationElement`.

---

**Target 7 - 10: Editor Enhancements**

*   **Goal:** Implement the custom editors as described in `Info.md` to improve usability.
*   **Targets:**
    *   **7: `PicSet` / `ColourSet` Editors:** Grid layout, selection, inline editing, add button, GUID generation.
    *   **8: `FontSet` / `AnimationSet` Editors:** `ReorderableList`, inline editing, add/remove, GUID generation, font preview toggle.
    *   **9: Element Editors (`Image`/`Text`/`Animation`):** Find applicator, populate Item dropdowns by name (store ID), conditional override fields, warnings.
    *   **10: Applicator / Style / Set / Setting Editors:** Filtered `ArtSet` dropdown, validation, utility buttons ("Apply Style Now", "Setup Element Components").
*   **Implementation:** Create/modify editor scripts in the `Editor` folder, using `SerializedObject`, `SerializedProperty`, `EditorGUILayout`, `EditorGUI`, `ReorderableList`, etc. Follow best practices (Undo, `SetDirty`, change checks). Implement GUID generation on add buttons. Implement filtering logic for dropdowns.
*   **Files Edited:**
    *   *(New/Modify)* All scripts within `Scripts/Editor/` folder.
    *   `Scripts/Components/ArtSetApplicator.cs` (Refine `SetupElementComponentsEditor` logic).
*   **Testing (Iterative per Target 7-10):**
    *   Open relevant assets (`Sets`, `Styles`, `Setting`) or select relevant GameObjects (`Applicator`, `Elements`) in the editor.
    *   Verify the custom inspector layouts match the design.
    *   Test all UI controls: dropdowns, buttons, toggles, lists (add, remove, reorder).
    *   Verify data binding (changes in editor save to the asset/component).
    *   Verify GUID generation when adding items via custom editors.
    *   Verify dropdown filtering (`ArtSetApplicatorEditor`, Element Editors).
    *   Test utility buttons ("Apply Style Now", "Setup Element Components"). Check console for logs/warnings.
    *   Test editor responsiveness and look for errors in the console.

---

**Target 11: Refinement, Documentation & Final Testing**

*   **Goal:** Perform final code review, add missing comments/documentation, optimize if needed, and write user documentation.
*   **Implementation:**
    *   Review all runtime and editor scripts for clarity, consistency, and potential bugs/optimizations.
    *   Add/improve XML documentation comments (`<summary>`, `<param>`, etc.).
    *   Write a `UserGuide.md` explaining how to use the system (creating styles, applying them, component usage).
*   **Files Edited:**
    *   Potentially all `.cs` files.
    *   *(New)* `Docs/UserGuide.md` (or similar location).
*   **Testing:**
    *   Perform thorough integration testing in `ArtStyleTestScene`, covering all features (style application, overrides, runtime switching, animation, defaults).
    *   Create a slightly more complex test scene (e.g., with nested applicators if supported, or multiple distinct styled areas) and verify behaviour.
    *   Review `UserGuide.md` for accuracy and clarity.

---
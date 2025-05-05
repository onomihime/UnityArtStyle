# Art Style System - Implementation Plan (Revision 3)

This plan outlines the steps for implementing and testing the Art Style system, based on the revised design document (`Info.md`) and incorporating the initial script setup (`PreImplemenationRun.md`). Each target focuses on a specific set of features, followed by testing in a dedicated scene (`ArtStyleTestScene`).

**Prerequisites:**

*   Initial C# scripts created as per `PreImplemenationRun.md`.
*   A test scene (`ArtStyleTestScene`) set up with a Canvas, a root GameObject ("StyledPanel"), basic UI elements (`Image`, `Text`, `TMP_Text`), and a UI Button for testing runtime actions.

---

**Target 1: Asset-Only Scriptable Objects & Basic Editors**

*   **Goal:** Establish the core data structure hierarchy using only standalone ScriptableObject assets. Implement basic custom editors for creating, managing, and viewing these assets, including automatic GUID generation. No inline instance creation or complex editor layouts yet.
*   **Implementation:**
    *   **GUID Generation:** Ensure `OnEnable` in all relevant SO scripts (`Item` types, `ArtSetType`, `ArtSet`, `ArtStyle`) generates a GUID if `_id` is empty.
    *   **Basic Custom Editors:** Create editor scripts (`Editor` folder: `Assets/Modules/ArtStyle/Scripts/Editor/`) for *all* ScriptableObject types (`ArtSetting`, `ArtStyle`, `ArtSetType`, `ArtSet`, `PicSet`, `ColourSet`, `FontSet`, `AnimationSet`, `PicItem`, `ColourItem`, `FontItem`, `AnimationItem`).
        *   Initially, use `DrawDefaultInspector()`.
        *   Add logic to display the `_id` field as read-only (e.g., `EditorGUILayout.LabelField("ID", property.stringValue)`).
    *   **`ArtSetType` Editor:** Enhance to manage `ItemType` lists (`picItemTypes`, `colourItemTypes`, etc.) using `ReorderableList`. Ensure new `ItemType`s get unique IDs upon creation within the list.
    *   **`Set` Editors (`PicSetEditor`, etc.):** Enhance to manage their respective `Item` lists (`List<PicItem>`, etc.) using `ReorderableList`. For now, these lists will only accept references to existing `Item` `.asset` files dragged into the list or assigned via object pickers. Ensure the editor adds object fields for assigning *asset* references.
    *   **`ArtSet` Editor:** Enhance to show object fields for assigning `setType` (`ArtSetType` asset) and the four `Set` types (`PicSet`, `ColourSet`, etc. - *asset references only*).
    *   **`ArtStyle` Editor:** Enhance to manage the `artSets` mapping (e.g., using a list or dictionary structure suitable for the editor). For now, it will only allow assigning existing `ArtSet` `.asset` files.
    *   **`ArtSetting` Editor:** Enhance to manage `artSetTypeSlots` (`ReorderableList` of `ArtSetType` assets) and `artStyles` (`ReorderableList` of `ArtStyle` assets). Add a selector for `activeArtStyleIndex`.
*   **Files Edited/Created:**
    *   All SO scripts in `Scripts/Data/` (for `OnEnable` GUID generation).
    *   *(New/Modify)* All editor scripts within `Scripts/Editor/`.
*   **Testing:**
    *   Use the Unity Editor `Assets -> Create -> Art Style -> ...` menus to create one instance of each SO type as a `.asset` file.
    *   Verify GUIDs are generated and read-only in the inspector.
    *   Verify `ArtSetType` editor allows adding/removing/editing `ItemType`s (name, ID generated).
    *   Verify `Set` editors allow adding/removing existing `Item` assets to their lists.
    *   Verify `ArtSet` editor allows assigning `ArtSetType` and `Set` assets.
    *   Verify `ArtStyle` editor allows assigning `ArtSet` assets.
    *   Verify `ArtSetting` editor allows assigning `ArtSetType` and `ArtStyle` assets and selecting the active style.
    *   Populate a basic hierarchy: `ArtSetting` -> `ArtStyle` -> `ArtSet` (with `ArtSetType`) -> `Set`s -> `Item`s (all as separate `.asset` files).

---

**Target 2: Element Data Classes & Default Items**

*   **Goal:** Define the `Element` data classes (`ImageArtElement`, `TextArtElement`, `AnimationElement`) used by the `ArtSetApplicator`. Integrate these lists into the `ArtSetApplicator` and add fields for default/fallback items.
*   **Implementation:**
    *   **Element Classes:** Create the `[System.Serializable]` classes `ImageArtElement`, `TextArtElement`, `AnimationElement` in `Scripts/Components/` or `Scripts/Data/Elements/`. Define their fields as per `Info.md` (target component, ItemType IDs, override enums/values).
    *   **`ArtSetApplicator` Script:**
        *   Remove `ItemComponentMap` class and `_itemComponentMappings` list.
        *   Add `[SerializeField]` lists: `List<ImageArtElement> _imageElements`, `List<TextArtElement> _textElements`, `List<AnimationElement> _animationElements`.
        *   Add the `[SerializeField]` fields for default items: `_defaultPicItem` (`PicItem`), `_defaultColourItem` (`ColourItem`), `_defaultFontItem` (`FontItem`), `_defaultAnimationItem` (`AnimationItem`).
    *   **`ArtSetApplicatorEditor` Script:**
        *   Create/Modify the editor script for `ArtSetApplicator`.
        *   Remove UI related to `_itemComponentMappings`.
        *   Ensure the default item fields are displayed.
        *   Implement basic `ReorderableList` UI for the new element lists (`_imageElements`, etc.). For this target, just ensure the lists appear and allow adding/removing default instances. Detailed element editing UI comes in Target 4's editor work.
*   **Files Edited/Created:**
    *   *(New)* `Scripts/Components/ImageArtElement.cs` (or `Data/Elements/`)
    *   *(New)* `Scripts/Components/TextArtElement.cs` (or `Data/Elements/`)
    *   *(New)* `Scripts/Components/AnimationElement.cs` (or `Data/Elements/`)
    *   `Scripts/Components/ArtSetApplicator.cs` (Remove old mapping, add element lists, add default item fields).
    *   *(Modify)* `Scripts/Editor/ArtSetApplicatorEditor.cs` (Remove old mapping UI, display default fields, add basic element list UI).
*   **Testing:**
    *   Add the `ArtSetApplicator` component to the "StyledPanel" GameObject in the test scene.
    *   Select the "StyledPanel" GameObject. Verify the "Default Pic Item", "Default Colour Item", etc., fields are visible.
    *   Create default `Item` assets (e.g., `DefaultErrorPic`, `DefaultBlackColour`) and assign them to the corresponding fields on the `ArtSetApplicator`.
    *   Verify the "Image Elements", "Text Elements", and "Animation Elements" lists are visible in the Inspector.
    *   Verify you can add and remove elements from these lists (they will be empty/default for now).

---

**Target 3: Inline Scriptable Object Creation & Extraction**

*   **Goal:** Implement the ability to create `Set` and `Item` instances directly ("inline") within their parent SO editors (`ArtSet`, `Set`s respectively). Provide visual distinction and an "Extract to Asset" feature.
*   **Implementation:**
    *   **Serialization:** Ensure `ArtSet`'s fields for `Set`s (`picSet`, etc.) and `Set`'s fields for `Item`s (`List<PicItem>`, etc.) can store direct instances (potentially using `[SerializeReference]` if storing derived types in base class lists, although direct `List<PicItem>` etc. might suffice if structured carefully).
    *   **`Set` Editors (`PicSetEditor`, etc.):**
        *   Modify the `ReorderableList` or management UI.
        *   Add a "+" button variant or dropdown option to "Create New Inline Item".
        *   When creating inline, instantiate the `Item` scriptable object *without* creating an asset file and add it to the list.
        *   Draw the inline item's editor fields directly within the list element using `Editor.CreateEditor()` and `OnInspectorGUI()`.
        *   Visually distinguish inline items (e.g., different background, label).
        *   Add an "Extract to Asset" button next to inline items. This button logic should:
            *   Prompt the user for a save location/name.
            *   Create a new `.asset` file using `AssetDatabase.CreateAsset()`.
            *   Copy the data from the inline instance to the new asset.
            *   Replace the inline instance in the list with a reference to the newly created asset.
            *   Mark the parent `Set` asset dirty.
    *   **`ArtSet` Editor:**
        *   Modify the sections for `picSet`, `colourSet`, etc.
        *   Allow assigning an existing `.asset` OR clicking a "Create New Inline Set" button.
        *   If an inline `Set` is created, instantiate it, store it directly in the `ArtSet`'s field, and draw its corresponding `Set` editor inline using `Editor.CreateEditor()`.
        *   Visually distinguish inline sets.
        *   Add an "Extract to Asset" button for inline `Set`s, similar to the item extraction logic.
*   **Files Edited:**
    *   `Scripts/Data/ArtSet.cs` (Ensure fields can hold inline SOs).
    *   `Scripts/Data/Items/` (All Item scripts - ensure constructors/setup work for inline).
    *   `Scripts/Data/Sets/` (All Set scripts - ensure fields/lists can hold inline Items).
    *   `Scripts/Editor/Items/` (All Item editors - may need adjustments for inline drawing).
    *   `Scripts/Editor/Sets/` (All Set editors - implement inline item creation, drawing, extraction).
    *   `Scripts/Editor/ArtSetEditor.cs` (Implement inline set creation, drawing, extraction).
*   **Testing:**
    *   Select an `ArtSet` asset.
        *   Verify you can create an inline `PicSet`.
        *   Verify the `PicSetEditor` draws inline within the `ArtSetEditor`.
        *   Within the inline `PicSetEditor`, verify you can create an inline `PicItem`.
        *   Verify the inline `PicItem` editor draws within the inline `PicSetEditor`.
        *   Verify you can assign an existing `PicItem` asset to the inline `PicSet`.
        *   Test the "Extract to Asset" button for the inline `PicItem`. Verify it creates an asset and replaces the inline entry.
        *   Test the "Extract to Asset" button for the inline `PicSet`. Verify it creates an asset and replaces the inline `Set`.
    *   Repeat testing for `ColourSet`, `FontSet`, `AnimationSet` and their respective items.
    *   Test mixing asset-based and inline Sets/Items within the same parent.

---

**Target 4: `ArtSetApplicator` Component & Application Logic**

*   **Goal:** Implement the core functionality of the `ArtSetApplicator` component to apply styles (Pictures, Colours, Fonts) to UI elements based on the `Element` lists, handling overrides. Integrate runtime style switching via `ArtSetting`.
*   **Implementation:**
    *   **`ArtSetApplicator` Script:**
        *   Implement `Awake`, `OnEnable`, `Start`, `OnDisable`.
        *   Implement logic to determine the active `ArtSet`: Check `artSetOverride`, then `useArtSetting` (check `artStyleOverride` or get from `ArtSetting.Instance.ActiveArtStyle` using `artSetTypeFilter`). Resolve the `ArtSetType`.
        *   Implement `BuildLookupDictionaries()`: Create `Dictionary<string, TItem>` (keyed by `ItemType` ID) for items in the active `PicSet`, `ColourSet`, `FontSet`, `AnimationSet`. Handle null Sets. Cache these dictionaries.
        *   Implement `ApplyStyle()`:
            *   Iterate through `_imageElements`, `_textElements`, `_animationElements`.
            *   For each element:
                *   Get the required `ItemType` IDs.
                *   Look up the corresponding `Item`s in the cached dictionaries using the IDs. Use defaults (`_defaultPicItem`, etc.) if lookup fails.
                *   Check the element's override settings (`spriteMode`, `colourMode`, `fontMode`, `durationMode`, etc.).
                *   Determine the final property values based on the resolved item and override settings.
                *   Apply the final values to the element's target `Component` (`targetImage`, `targetText`, `targetTransform`), checking component types where necessary (e.g., `Text` vs `TMP_Text`).
                *   For `AnimationElement`, ensure `AnimationApplicator` and `CanvasGroup` exist on the target GameObject (this setup might be better handled by the editor button).
        *   Implement subscription/unsubscription to `ArtSetting.OnActiveStyleChanged` in `OnEnable`/`OnDisable` if `useArtSetting` is true. The handler should re-determine the `ArtSet`, rebuild dictionaries, and call `ApplyStyle()`.
        *   Implement `PlayAnimation(RectTransform target)`: Find the `AnimationElement` for the target, resolve the `AnimationItem` and duration (considering overrides), find the `AnimationApplicator`, call `Play`.
    *   **`ArtSetting` Script:** Ensure `ActiveArtStyle` property setter invokes `OnActiveStyleChanged`. Ensure `Instance` property is robust.
    *   **`ArtStyle` Script:** Implement `FindArtSetByType(ArtSetType type)` method (or similar logic to get the correct `ArtSet` based on the type filter).
    *   **`ArtSetApplicatorEditor` Script:**
        *   Enhance the `ReorderableList` UI for `_imageElements`, `_textElements`, `_animationElements`.
        *   Each list element editor needs:
            *   An object field for the specific target `Component`.
            *   Dropdowns to select the relevant `ItemType` IDs (populated dynamically based on the resolved `ArtSetType`).
            *   Fields for the override enums and values.
            *   Validation (warn if component type is wrong, ItemType ID is invalid, etc.).
        *   Add "Apply Style Now" button (`[ContextMenu]` or editor button) that calls `ApplyStyle()` on the target component.
        *   Add "Setup Animation Components" button: Iterates `_animationElements`, finds targets, adds `AnimationApplicator`/`CanvasGroup` if missing.
*   **Files Edited:**
    *   `Scripts/Components/ArtSetApplicator.cs`
    *   `Scripts/Editor/ArtSetApplicatorEditor.cs`
    *   `Scripts/Singletons/ArtSetting.cs`
    *   `Scripts/Data/ArtStyle.cs`
    *   `Scripts/Data/ArtSetType.cs` (Need access to ItemType definitions for editor dropdowns).
*   **Testing:**
    *   In `ArtStyleTestScene`, configure `ArtSetApplicator` on "StyledPanel".
        *   Add elements to the `_imageElements`, `_textElements` lists. Assign target components (`Image`, `Text`, `TMP_Text`). Select appropriate `ItemType` IDs.
        *   Test different override modes (`PicSetSprite`, `OverrideSprite`, `ColourSetColour`, `OverrideColour`, etc.) for each element type.
        *   Test with `useArtSetting = false` and `artSetOverride` assigned directly. Set `applyOnStart = true`. Enter Play mode. Verify correct sprites, tints, fonts, and text colors are applied according to element settings and overrides.
        *   Test default item fallback: In the `ArtSet`, remove an item referenced by an element (where override is not used). Ensure the `ArtSetApplicator`'s default item is used instead and a warning is logged.
        *   Test with `useArtSetting = true`. Configure `ArtSetting` with two `ArtStyle`s (`StyleA`, `StyleB`) using different `ArtSet`s (of the same `ArtSetType`) with visually distinct items. Set `artSetTypeFilter` on the applicator.
        *   Enter Play mode. Verify `StyleA` applies. Use the test button to change `ArtSetting.Instance.ActiveArtStyle` at runtime. Verify the UI updates immediately to `StyleB`. Test switching back.
        *   Test the "Apply Style Now" button in the editor.

---

**Target 5: `AnimationApplicator` Component**

*   **Goal:** Implement the `AnimationApplicator` component for basic fade animations and integrate it with the `ArtSetApplicator` via `AnimationElement`.
*   **Implementation:**
    *   **`AnimationApplicator` Script:**
        *   Create/Verify the `AnimationApplicator.cs` MonoBehaviour. Require `CanvasGroup`.
        *   Implement `Play(AnimationItem item, float duration)` method. Start a coroutine (`FadeCoroutine`).
        *   Implement `Stop()` method to stop the coroutine.
        *   Implement `FadeCoroutine`: Use `item.curve`, resolved `duration`, `item.fadeStartOpacity`. Animate `CanvasGroup.alpha`. Use `Time.unscaledDeltaTime`. Handle duration <= 0.
        *   Add `playOnEnable` field and `defaultAnimationItem` field. Implement logic in `OnEnable` to play default/last item if `playOnEnable` is true.
    *   **`ArtSetApplicator` Script:**
        *   Ensure `ApplyStyle` correctly resolves `AnimationItem` for `AnimationElement` (needed for `PlayAnimation` and potentially `AnimationApplicator`'s `playOnEnable`).
        *   Ensure `PlayAnimation(RectTransform target)` correctly finds the element, resolves the item/duration (with overrides), finds the applicator, and calls `Play`.
    *   **`ArtSetApplicatorEditor` Script:**
        *   Ensure the "Setup Animation Applicators" button correctly iterates `_animationElements` and adds missing components (`AnimationApplicator`, `CanvasGroup`) to the `targetTransform.gameObject`.
*   **Files Edited/Created:**
    *   `Scripts/Components/AnimationApplicator.cs`
    *   `Scripts/Components/ArtSetApplicator.cs` (Refine `ApplyStyle` for animation item resolution, refine `PlayAnimation`).
    *   `Scripts/Editor/ArtSetApplicatorEditor.cs` (Verify "Setup Animation Applicators" button logic).
*   **Testing:**
    *   In the test scene, add an `AnimationElement` to the `ArtSetApplicator`. Assign the `Image`'s `RectTransform` as the target. Select an `AnimationItemType` ID.
    *   Use the "Setup Animation Applicators" button. Verify `AnimationApplicator` and `CanvasGroup` are added to the Image GameObject.
    *   Configure the `AnimationApplicator`'s `playOnEnable` or use the test UI Button and a script to call `artSetApplicator.PlayAnimation(imageRectTransform)` on the `ArtSetApplicator`.
    *   Enter Play mode. Verify the Image fades as defined by the `AnimationItem` and the element's duration override (if used).
    *   Test calling `Stop()` via another button/script (may need a way to get the `AnimationApplicator` reference).

---

**Target 6: Editor Visual Enhancements**

*   **Goal:** Improve the usability and visual appeal of the custom editors created in previous targets.
*   **Implementation:**
    *   **`PicSetEditor` / `ColourSetEditor`:** Implement the grid layout for items. Display previews (sprite/color swatch) in the grid cells. Implement selection highlighting and display the full editor for the selected item below/beside the grid.
    *   **`FontSetEditor`:** Add the optional font preview toggle and implementation.
    *   **`ArtSetApplicatorEditor`:** Improve layout of element list editors, add validation warnings (e.g., incompatible component types, invalid ItemType IDs), potentially use headers/foldouts for better organization. Make the `ItemType` dropdowns more user-friendly (grouping, clearer names).
    *   **General:** Review all custom editors for clarity, consistent layout, spacing, and use of `EditorGUIUtility.labelWidth`, `EditorGUILayout.HelpBox`, etc. Ensure Undo/Redo works correctly (`serializedObject.ApplyModifiedProperties`).
*   **Files Edited:**
    *   `Scripts/Editor/Sets/PicSetEditor.cs`
    *   `Scripts/Editor/Sets/ColourSetEditor.cs`
    *   `Scripts/Editor/Sets/FontSetEditor.cs`
    *   `Scripts/Editor/ArtSetApplicatorEditor.cs`
    *   *(Potentially)* Other editor scripts for minor layout tweaks.
*   **Testing:**
    *   Open assets of each type (`PicSet`, `ColourSet`, `FontSet`) and select the `ArtSetApplicator` GameObject.
    *   Verify the improved layouts and previews.
    *   Test grid selection and inline editing display.
    *   Test font previews (if implemented).
    *   Verify validation warnings appear correctly in the `ArtSetApplicatorEditor`.
    *   Check editor responsiveness and look for console errors.

---

**Target 7: Refinement, Documentation & Final Testing**

*   **Goal:** Perform final code review, add documentation, optimize if necessary, and conduct thorough testing.
*   **Implementation:**
    *   **Code Review:** Review all runtime and editor scripts for clarity, consistency, performance, and potential bugs. Refactor where needed.
    *   **Documentation:**
        *   Add/improve XML documentation comments (`<summary>`, `<param>`, `<returns>`, etc.) to all public classes, methods, and properties in runtime scripts.
        *   Add tooltips (`[Tooltip]`) to `[SerializeField]` fields.
        *   Write a `UserGuide.md` explaining the system: concepts (Types, Sets, Items, Styles, Setting), asset vs. inline creation, using the `ArtSetApplicator` (element lists, overrides), triggering animations, runtime switching.
    *   **Optimization:** Profile editor and runtime performance if any bottlenecks were suspected during development. Optimize lookups or editor drawing if required.
    *   **Final Testing:**
        *   Perform thorough integration testing in `ArtStyleTestScene`, covering all features: asset/inline creation, style application (all types), defaults, runtime switching, animation playback, editor tools/buttons.
        *   Create a more complex test scene (e.g., multiple applicators with different type filters, nested UI elements) and verify behavior.
        *   Test edge cases (e.g., missing assets, empty sets, invalid mappings).
*   **Files Edited/Created:**
    *   Potentially all `.cs` files (for comments, minor fixes, optimizations).
    *   *(New)* `Docs/UserGuide.md` (or similar location).
*   **Testing:** Execute the final testing plan. Review the `UserGuide.md` for accuracy and clarity. Fix any remaining bugs.

---
# Art Style System Design (Revision 3)

## Core Concepts

*   **Goal:** A highly modular and flexible UI art style system for Unity, enabling artists to define and swap entire visual styles easily, driven by a structured type hierarchy and minimizing scene component clutter.
*   **Hierarchy & Types:** The system uses a nested hierarchy of ScriptableObjects and defined types:
    *   `ArtSetting` (Singleton SO): Defines the overall structure for styles within a project scope.
        *   Holds a list of managed `ArtStyle`s (one active).
        *   Defines the available **`ArtSetType` Slots** that all managed `ArtStyle`s must conform to (e.g., "MainMenu", "GameplayHUD", "Tooltip"). Acts as the single "ArtStyleType" definition.
    *   `ArtStyle` (SO): Represents a complete visual theme (e.g., "SciFi", "Fantasy").
        *   Contains `ArtSet`s mapped to the `ArtSetType` slots defined in `ArtSetting`. Handles extra `ArtSet`s if the `ArtSetting` definition changes.
    *   `ArtSetType` (SO): Defines the *kind* of UI element or section a style applies to (e.g., "Button", "Panel", "Slider").
        *   Contains lists of **`ItemType` definitions** (simple classes like `PicItemType`, `ColourItemType`, etc.) for each category (Pictures, Colours, Fonts, Animations). These define the *slots* within the corresponding `Set`s. (e.g., A "Button" `ArtSetType` might define `PicItemType`s: "Background", "Icon"; `ColourItemType`s: "NormalText", "HoverText").
    *   `ArtSet` (SO): Groups related asset collections (`PicSet`, `ColourSet`, `FontSet`, `AnimationSet`) for a specific `ArtSetType` within an `ArtStyle`.
        *   References an `ArtSetType`.
        *   Contains references to `PicSet`, `ColourSet`, `FontSet`, `AnimationSet`. These can be references to asset SOs or **inline instances** stored directly within the `ArtSet`.
    *   `Set` Types (`PicSet`, `ColourSet`, `FontSet`, `AnimationSet` - SOs): Contain the actual visual data.
        *   Store `Item`s (`PicItem`, `ColourItem`, etc.) mapped to the `ItemType` slots defined by the parent `ArtSet`'s `ArtSetType`.
        *   Handle items that don't match current slots ("extra" items).
        *   Items themselves can be references to asset SOs or **inline instances**.
    *   `Item` Types (`PicItem`, `ColourItem`, `FontItem`, `AnimationItem` - SOs): Define individual visual elements (sprite, color, font, animation data).
*   **Application:**
    *   An `ArtSetApplicator` (`MonoBehaviour`) is attached to a root UI GameObject.
    *   It references an `ArtSet` (determined via `ArtSetting`/`ArtStyle` or override).
    *   The `ArtSetApplicator` contains lists of **`Element` data classes** (`ImageArtElement`, `TextArtElement`, `AnimationElement`).
    *   Each `Element` instance holds:
        *   A reference to a specific target UI `Component` (`Image`, `Text`, `TMP_Text`, `RectTransform`).
        *   References to the relevant `ItemType` IDs (from the `ArtSet`'s `ArtSetType`) it should use.
        *   Override settings (enums and values) allowing per-element deviations from the `ArtSet`.
    *   When the style needs to be applied (on start, or when the active style changes), the `ArtSetApplicator` iterates through its `Element` lists:
        1.  For each `Element`, get the required `ItemType` IDs.
        2.  Look up the corresponding `Item`s in the active `ArtSet`'s relevant `Set`s based on the `ItemType` IDs.
        3.  Consider the `Element`'s override settings (e.g., "Use Override Sprite", "Use Set Colour").
        4.  Apply the final resolved/overridden properties to the `Element`'s target `Component`.
*   **Referencing & Performance:**
    *   All SOs (`ArtSetting`, `ArtStyle`, `ArtSetType`, `ArtSet`, `Set`s, `Item`s) have a unique, persistent `id` (GUID string). `ItemType`s within `ArtSetType` also have IDs.
    *   Lookups primarily use IDs. Names are for editor display.
    *   `ArtSetApplicator` caches lookups (e.g., `Dictionary<ItemTypeID, Item>`) from its active `Set`s for efficient application.
*   **Inline vs. Asset Flexibility:**
    *   `Item`s (`PicItem`, etc.) and `Set`s (`PicSet`, etc.) can be created either as standalone `.asset` files (reusable, easier to manage globally) or as inline instances stored directly within their parent SO (quicker for one-offs, less project clutter).
    *   Editors must provide clear ways to create/assign both types.
    *   `ArtSet` itself *must* be a standalone `.asset` file.
*   **Type System Robustness (Slots, Extras, Defaults):**
    *   When type definitions change (e.g., `ArtSetting` changes `ArtSetType` slots, or `ArtSetType` changes `ItemType` slots):
        *   Matching items/sets are retained in their slots.
        *   Items/sets in the instance that no longer have a corresponding slot in the type definition are kept but marked as "extra".
        *   Empty slots remain empty but should use a designated "Default" item/set during application if available.
    *   A default `ItemType` should exist for each category (Pic, Colour, Font, Anim). A default `ArtSetType` might be needed in `ArtSetting`.
*   **Runtime Style Switching:** `ArtSetting` provides `OnActiveStyleChanged`. Subscribed `ArtSetApplicator`s re-evaluate their active `ArtSet` based on the new `ArtStyle` and re-apply the style to their mapped components.

## Data Structures

### Item Types (`ScriptableObject`)

*   **`PicItem`**: `id`, `name`, `sprite`, `defaultColour`.
*   **`ColourItem`**: `id`, `name`, `colour`.
*   **`FontItem`**: `id`, `name`, `font`, `tmpFont`, `defaultColour`.
*   **`AnimationItem`**: `id`, `name`, `duration`, `useFade`, `fadeStartOpacity`, `curve`.
    *(These can be `.asset` files or inline instances within a `Set`)*

### Set Types (`ScriptableObject`)

*   Contains data structures (e.g., `List` or `Dictionary`) to store `Item`s mapped to `ItemType` IDs.
*   Needs to store both asset references (`Item` SO) and inline `Item` instances.
*   Needs to track "extra" items not matching the current `ArtSetType`.
*   **`PicSet`**: Manages `PicItem`s.
*   **`ColourSet`**: Manages `ColourItem`s.
*   **`FontSet`**: Manages `FontItem`s.
*   **`AnimationSet`**: Manages `AnimationItem`s.
    *(These can be `.asset` files or inline instances within an `ArtSet`)*

### Hierarchy Types (`ScriptableObject` unless noted)

*   **`ItemType`** (Base class, **Not** SO):
    *   `id` (string - GUID): Unique persistent identifier within its `ArtSetType`.
    *   `name` (string): User-friendly name (e.g., "Button Background", "Primary Text").
*   **`PicItemType`** : `ItemType` { }
*   **`ColourItemType`** : `ItemType` { }
*   **`FontItemType`** : `ItemType` { }
*   **`AnimationItemType`** : `ItemType` { }
    *(These are defined and managed within the `ArtSetType` editor)*

*   **`ArtSetType`** (`ScriptableObject`):
    *   `id` (string - GUID): Unique persistent identifier.
    *   `name` (string): User-friendly name (e.g., "Button", "Panel").
    *   `picItemTypes` (`List<PicItemType>`): Defines picture slots.
    *   `colourItemTypes` (`List<ColourItemType>`): Defines colour slots.
    *   `fontItemTypes` (`List<FontItemType>`): Defines font slots.
    *   `animationItemTypes` (`List<AnimationItemType>`): Defines animation slots.
    *   *(Should include a default/fallback ItemType for each list)*
*   **`ArtSet`** (`ScriptableObject` - **Must be Asset**):
    *   `id` (string - GUID): Unique persistent identifier.
    *   `name` (string): User-friendly name (e.g., "MainMenu_SciFi_ButtonSet").
    *   `setType` (`ArtSetType`): Reference to the type defining the structure.
    *   `picSet` (`PicSet`): Reference or inline instance.
    *   `colourSet` (`ColourSet`): Reference or inline instance.
    *   `fontSet` (`FontSet`): Reference or inline instance.
    *   `animationSet` (`AnimationSet`): Reference or inline instance.
*   **`ArtStyle`** (`ScriptableObject`):
    *   `id` (string - GUID): Unique persistent identifier.
    *   `name` (string): User-friendly name (e.g., "SciFiTheme", "FantasyTheme").
    *   `artSets` (Structure mapping `ArtSetType` ID to `ArtSet`): Contains `ArtSet` references, conforming to slots defined in `ArtSetting`. Handles extras.
*   **`ArtSetting`** (Singleton `ScriptableObject`):
    *   `artSetTypeSlots` (`List<ArtSetType>`): Defines the required `ArtSetType`s for all managed styles. Includes defaults/fallbacks.
    *   `artStyles` (`List<ArtStyle>`): List of styles managed by this setting.
    *   `activeArtStyleIndex` (int): Index of the active style in the `artStyles` list.
    *   `ActiveArtStyle` (Property): Gets/sets the active style, triggers event.
    *   `OnActiveStyleChanged` (event): Event triggered when `ActiveArtStyle` changes.

### Element Data Classes (`[System.Serializable]`, **Not** SO or MonoBehaviour)

*   These classes are stored in lists within the `ArtSetApplicator` and define how specific UI components are styled.

*   **`ImageArtElement`**:
    *   `targetImage` (`Image`): Reference to the target Image component.
    *   `picItemId` (string): ID of the `PicItemType` to use for the sprite.
    *   `colourItemId` (string): ID of the `ColourItemType` to use for the tint.
    *   `spriteMode` (enum: `PicSetSprite`, `NoSprite`, `OverrideSprite`): How to determine the sprite.
    *   `colourMode` (enum: `DefaultColour`, `ColourSetColour`, `OverrideColour`): How to determine the tint color.
    *   `overrideSprite` (`Sprite`): Sprite to use if `spriteMode` is `OverrideSprite`.
    *   `overrideColour` (`Color`): Color to use if `colourMode` is `OverrideColour`.

*   **`TextArtElement`**:
    *   `targetText` (`Component`): Reference to the target Text or TMP_Text component. *(Store as Component, check type at runtime/editor)*.
    *   `fontItemId` (string): ID of the `FontItemType` to use.
    *   `colourItemId` (string): ID of the `ColourItemType` to use.
    *   `fontMode` (enum: `FontSetFont`, `OverrideFont`): How to determine the font.
    *   `colourMode` (enum: `DefaultColour`, `ColourSetColour`, `OverrideColour`): How to determine the text color.
    *   `overrideFont` (`Font`): Legacy Font to use if `fontMode` is `OverrideFont`.
    *   `overrideTmpFont` (`TMP_FontAsset`): TMP Font to use if `fontMode` is `OverrideFont`.
    *   `overrideColour` (`Color`): Color to use if `colourMode` is `OverrideColour`.

*   **`AnimationElement`**:
    *   `targetTransform` (`RectTransform`): Reference to the target UI element's RectTransform.
    *   `animationItemId` (string): ID of the `AnimationItemType` to use.
    *   `durationMode` (enum: `AnimationSetDuration`, `OverrideDuration`): How to determine the animation duration.
    *   `overrideDuration` (float): Duration to use if `durationMode` is `OverrideDuration`.

## Component Behaviours (MonoBehaviours)

*   **`ArtSetApplicator`**:
    *   Attached to a root UI GameObject.
    *   `useArtSetting` (bool): If true, uses the active `ArtStyle` from `ArtSetting`.
    *   `artStyleOverride` (`ArtStyle`): Used if `useArtSetting` is false.
    *   `artSetTypeFilter` (`ArtSetType`): Specifies the type of `ArtSet` this applicator should find within the style.
    *   `artSetOverride` (`ArtSet`): Direct override, ignores style/type filter.
    *   `imageElements` (`List<ImageArtElement>`): List of image styling definitions.
    *   `textElements` (`List<TextArtElement>`): List of text styling definitions.
    *   `animationElements` (`List<AnimationElement>`): List of animation styling definitions.
    *   `defaultPicItem` (`PicItem`): Fallback if a `PicItem` slot is empty or lookup fails.
    *   `defaultColourItem` (`ColourItem`): Fallback.
    *   `defaultFontItem` (`FontItem`): Fallback.
    *   `defaultAnimationItem` (`AnimationItem`): Fallback.
    *   `applyOnStart` (bool): Apply style automatically.
    *   *Responsibility:*
        *   Determines the active `ArtSet` based on overrides or `ArtSetting`/`ArtStyle`/`artSetTypeFilter`.
        *   Builds/caches lookup dictionaries (`Dictionary<string, TItem>`) for the items within the active `ArtSet`'s `Set`s, keyed by `ItemType` ID.
        *   On `ApplyStyle()`: Iterates through `imageElements`, `textElements`, `animationElements`. For each element:
            *   Gets the required `ItemType` IDs.
            *   Looks up the corresponding `Item`s in the cached dictionaries (using the `ItemType` IDs). Uses defaults if lookup fails.
            *   Checks the element's override settings (`spriteMode`, `colourMode`, etc.).
            *   Determines the final property values (sprite, color, font, etc.).
            *   Applies the final values to the element's target `Component`.
            *   For `AnimationElement`, ensures `AnimationApplicator` and `CanvasGroup` exist on the target.
        *   Handles subscribing/unsubscribing to `ArtSetting.OnActiveStyleChanged`.
        *   Provides default `Item` values.
    *   `ApplyStyle()` Method: Performs the lookup, override checks, and application logic. Rebuilds caches if the `ArtSet` changes.
    *   `PlayAnimation(RectTransform target)` Method (Example): Finds the `AnimationElement` targeting the given transform, resolves the `AnimationItem` and duration, finds the `AnimationApplicator`, and calls `Play`.

*   **`AnimationApplicator`**:
    *   Attached (potentially automatically by `ArtSetApplicator`'s editor) to GameObjects targeted by an `AnimationElement`. Requires `CanvasGroup`.
    *   *Responsibility:* Executes the animation defined by an `AnimationItem` provided by the `ArtSetApplicator`. Designed to be replaceable.
    *   `playOnEnable` (bool): Play animation automatically? Needs configuration.
    *   `defaultAnimationItem` (`AnimationItem`): Used for `playOnEnable` if no other item is specified.
    *   *Interface:* `Play(AnimationItem item, float duration)`, `Stop()`.

## Editor Implementations

*   **`Item` Editors (`PicItemEditor`, etc.):**
    *   Standard or simple custom editor showing ID (read-only), name, and data fields.
    *   Used for `.asset` files and for drawing *inline instances* within `Set` editors.
*   **`Set` Editors (`PicSetEditor`, etc. - Integrated into `ArtSetEditor`):**
    *   Displays slots based on the `ArtSetType` referenced by the parent `ArtSet`.
    *   Each slot shows the `ItemType` name.
    *   Each slot allows:
        *   Assigning an existing `Item` `.asset` via Object Field.
        *   Creating a **new inline `Item` instance** via a "+" button (draws the Item's editor inline).
        *   Clearing the slot.
    *   Displays a separate list for "extra" items (items present but not matching a current slot). Allows removing extras.
    *   Uses appropriate layouts (grid for Pic/Colour, list for Font/Anim).
*   **`ArtSetType` Editor:**
    *   Displays ID (read-only), name.
    *   Uses `ReorderableList` or similar for managing the lists of `PicItemType`, `ColourItemType`, `FontItemType`, `AnimationItemType`. Allows adding (generates ID), removing, reordering, and editing names.
*   **`ArtSet` Editor:**
    *   Displays ID (read-only), name.
    *   Object field for `setType` (`ArtSetType`).
    *   Sections for each `Set` (`PicSet`, `ColourSet`, etc.):
        *   Allows assigning an existing `Set` `.asset` via Object Field OR creating a **new inline `Set` instance** via a "+" button.
        *   If a `Set` (inline or asset) is present, **draws its corresponding `Set` editor directly inline** (showing the slots based on `setType`).
*   **`ArtStyle` Editor:**
    *   Displays ID (read-only), name.
    *   Displays slots based on the `ArtSetType`s defined in the relevant `ArtSetting`.
    *   Each slot shows the `ArtSetType` name and allows assigning an `ArtSet` `.asset` via Object Field.
    *   Displays a list of "extra" `ArtSet`s not matching current slots.
*   **`ArtSetting` Editor:**
    *   Displays fields for managing `artSetTypeSlots` (`ReorderableList` of `ArtSetType` assets).
    *   Displays fields for managing `artStyles` (`ReorderableList` of `ArtStyle` assets).
    *   Dropdown/Selector for `activeArtStyleIndex`.
    *   Button to trigger `OnActiveStyleChanged` manually (for testing).
*   **`ArtSetApplicator` Editor:**
    *   Displays `useArtSetting` toggle, `artStyleOverride`, `artSetTypeFilter`, `artSetOverride`.
    *   Displays default item fields.
    *   Displays `applyOnStart` toggle.
    *   **Element Lists:** Uses `ReorderableList` for `imageElements`, `textElements`, `animationElements`.
        *   Each list element editor allows:
            *   Assigning the specific target `Component` (`Image`, `Text`/`TMP_Text`, `RectTransform`).
            *   Selecting relevant `ItemType` IDs (dropdowns populated based on resolved `ArtSetType`).
            *   Setting override enums and values.
        *   Needs validation (e.g., correct component type, valid ItemType IDs).
    *   Button: "Apply Style Now".
    *   Button: "Setup Animation Components" (scans `animationElements` and adds/checks for `AnimationApplicator` + `CanvasGroup` on targets).

## Extra Notes

*   **Naming Conventions:** Even more critical now with nested types and inline instances. Clear naming for `ArtSetType`s and `ItemType`s is essential for the mapping process.
*   **Editor Performance:** The nested editors (`Set` within `ArtSet`) and dynamic dropdowns (`ItemType` mapping) require careful optimization to keep the inspector responsive.
*   **User Documentation:** Needs significant updates explaining the type hierarchy, slots, inline vs. asset creation, and the crucial `ArtSetApplicator` mapping process.
*   **Inline Data Management:** Users need to understand that inline instances are not easily reusable. An "Extract to Asset" feature would be highly beneficial for promoting an inline instance to a reusable asset.
*   **Asset Management:** Maintain a clear project folder structure. Decide on conventions for where inline vs. asset SOs are preferred.

## Implementation Notes

*   **GUID Generation:** Ensure reliable GUID generation for all SOs and `ItemType`s upon creation (e.g., in `OnEnable` or via editor creation logic). IDs should be serialized and read-only in editors.
*   **Serialization:** Inline instances rely heavily on Unity's serialization. Use `[SerializeReference]` for lists/fields holding base class/interface types if storing derived inline instances directly (though storing them within dedicated `Set`/`ArtSet` SOs might avoid this complexity if structured carefully). Test serialization thoroughly.
*   **Dictionary Caching:** `ArtSetApplicator` still caches `Item` lookups, but now keyed by `ItemType` ID. Rebuild caches when the active `ArtSet` changes.
*   **Element Implementation:** The `Element` classes are simple `[System.Serializable]` classes. The editor needs to provide the `ItemType` selection logic based on the resolved `ArtSetType`.
*   **Error Handling:** Robust checks needed for null references (Types, Sets, Items, target Components), missing slots, failed lookups, and incorrect component types in mappings. Log clear warnings. Use default items extensively.
*   **Editor Scripting:** Heavy reliance on custom editors. Use `SerializedObject`/`SerializedProperty`. Manage Undo/Redo and `SetDirty`. Use `EditorGUI.BeginChangeCheck`.

## Final Considerations

*   **Complexity vs. Benefit:** Re-confirm that the added structural rigidity and reduced component count justify the significant increase in system complexity and editor development effort.
*   **Usability Focus:** Prioritize editor clarity above all else. If the mapping or inline/asset system is confusing, adoption will fail. Get feedback early.
*   **"Extract to Asset":** Plan for this feature. It significantly mitigates the downsides of inline instances.
*   **Testing Plan:** Requires extensive testing, especially around type changes, inline/asset combinations, serialization, editor functionality, and the `ArtSetApplicator` mapping logic.
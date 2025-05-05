# Art Style System Design

## Core Concepts

*   **Goal:** A highly modular and flexible UI art style system for Unity, enabling artists to define and swap entire visual styles easily while allowing fine-grained overrides.
*   **Hierarchy:** The system uses a hierarchy of ScriptableObjects:
    *   `ArtSetting` (Singleton): Defines the globally active `ArtStyle`.
    *   `ArtStyle`: Contains a collection of named `ArtSet`s, categorized by `ArtSetType`.
    *   `ArtSet`: Groups related asset collections (`PicSet`, `ColourSet`, `FontSet`, `AnimationSet`) representing a specific UI theme (e.g., "MainMenu", "GameplayHUD").
*   **Asset Types:** Individual visual elements are defined in `Item` ScriptableObjects (`PicItem`, `ColourItem`, `FontItem`, `AnimationItem`) and grouped into `Set` ScriptableObjects (`PicSet`, `ColourSet`, etc.).
*   **Application:** `MonoBehaviour` components (`ImageArtElement`, `TextArtElement`, `AnimationElement`) are attached to UI GameObjects. An `ArtSetApplicator` component manages these elements, linking them to the appropriate `ArtSet` based on the active `ArtStyle` or a direct override.
*   **Referencing & Performance:**
    *   Each `Item` (`PicItem`, `ColourItem`, etc.) has both a user-friendly `name` (string) and a unique, persistent `id` (GUID stored as a string). GUIDs should be automatically generated upon item creation in editors.
    *   `Element` components store the `id` of the `Item` they should display.
    *   When swapping `ArtSet`s or applying styles, the system primarily uses the `id` to find the corresponding `Item`. The `name` is used *only* for display in editors.
    *   To ensure efficient lookups, especially in large `Set`s, internal `Dictionary<string, TItem>` lookups should be used where possible (e.g., cached within `ArtSetApplicator` or potentially within the `Set` ScriptableObjects themselves). If an `id` is not found, the system will use the default item specified in the `ArtSetApplicator`.
*   **Overrides:** Individual `Element` components can override specific properties (sprite, color, font, animation duration) instead of using the values from the selected `ArtSet`.
*   **Type System (for Robust Swapping):**
    *   `ArtStyle` will contain an editable list of `ArtSetType`s (`ScriptableObject` holding a name/ID).
    *   Each `ArtSet` within an `ArtStyle` must be assigned an `ArtSetType`.
    *   `ArtSetApplicator` will have a field to specify which `ArtSetType` it expects. When using an `ArtStyle`, its `ArtSet` dropdown will be filtered to only show `ArtSet`s matching the applicator's specified type. This ensures more structured and predictable swapping.
*   **Component Management:** `Element` components (`ImageArtElement`, etc.) can be added manually or via an editor tool on the `ArtSetApplicator` ("Setup Element Components") to scan child objects and add the appropriate elements.
*   **Runtime Style Switching:** The `ArtSetting` singleton should provide an event (e.g., `OnActiveStyleChanged`) that `ArtSetApplicator` instances can subscribe to. When the global `activeArtStyle` changes, subscribed applicators should automatically refresh and apply the new style.

## Data Structures (ScriptableObjects)

### Item Types

*   **`PicItem`**:
    *   `id` (string - GUID): Unique persistent identifier.
    *   `name` (string): User-friendly name.
    *   `sprite` (Sprite): The image sprite.
    *   `defaultColour` (Color): A tint color.
*   **`ColourItem`**:
    *   `id` (string - GUID): Unique persistent identifier.
    *   `name` (string): User-friendly name.
    *   `colour` (Color): A flat color.
*   **`FontItem`**:
    *   `id` (string - GUID): Unique persistent identifier.
    *   `name` (string): User-friendly name.
    *   `font` (Font): Legacy Unity Font asset.
    *   `tmpFont` (TMP_FontAsset): TextMeshPro Font Asset.
    *   `defaultColour` (Color): Default text color.
*   **`AnimationItem`**:
    *   `id` (string - GUID): Unique persistent identifier.
    *   `name` (string): User-friendly name.
    *   `duration` (float): Animation duration in seconds.
    *   `useFade` (bool): Enable opacity animation (requires CanvasGroup on the target GameObject).
    *   `fadeStartOpacity` (float): Initial opacity (0 to 1).
    *   `curve` (AnimationCurve): Curve controlling the animation timing (primarily for fade).

### Set Types

*   **`PicSet`**: Contains a `List<PicItem>`. (Consider internal Dictionary for lookup).
*   **`ColourSet`**: Contains a `List<ColourItem>`. (Consider internal Dictionary for lookup).
*   **`FontSet`**: Contains a `List<FontItem>`. (Consider internal Dictionary for lookup).
*   **`AnimationSet`**: Contains a `List<AnimationItem>`. (Consider internal Dictionary for lookup).

### Hierarchy Types

*   **`ArtSetType`** (`ScriptableObject`):
    *   `id` (string - GUID): Unique persistent identifier.
    *   `name` (string): User-friendly name for the type (e.g., "Button", "Panel", "Tooltip").
*   **`ArtSet`**:
    *   `id` (string - GUID): Unique persistent identifier.
    *   `name` (string): User-friendly name (e.g., "MainMenuStyle", "GameplayHUDStyle").
    *   `setType` (`ArtSetType`): The type assigned to this ArtSet.
    *   `picSet` (`PicSet`): Reference to a Picture Set.
    *   `colourSet` (`ColourSet`): Reference to a Colour Set.
    *   `fontSet` (`FontSet`): Reference to a Font Set.
    *   `animationSet` (`AnimationSet`): Reference to an Animation Set.
*   **`ArtStyle`**:
    *   `id` (string - GUID): Unique persistent identifier.
    *   `name` (string): User-friendly name (e.g., "SciFiTheme", "FantasyTheme").
    *   `availableSetTypes` (`List<ArtSetType>`): Defines the types of ArtSets this style uses.
    *   `artSets` (`List<ArtSet>`): The collection of ArtSets belonging to this style.
*   **`ArtSetting`** (Singleton ScriptableObject):
    *   `activeArtStyle` (`ArtStyle`): Reference to the globally active Art Style.
    *   `OnActiveStyleChanged` (event): Event triggered when `activeArtStyle` is changed.

## Component Behaviours (MonoBehaviours)

*   **`ImageArtElement`**:
    *   Attached to a GameObject with an `Image` component.
    *   `picItemId` (string - GUID): ID of the `PicItem` to use.
    *   `colourItemId` (string - GUID): ID of the `ColourItem` to use for tinting.
    *   `spriteMode` (enum: `FromSet`, `Override`, `None`): Determines sprite source.
    *   `colourMode` (enum: `FromSet`, `Override`, `DefaultPicColour`): Determines color source.
    *   `overrideSprite` (Sprite): Sprite used when `spriteMode` is `Override`.
    *   `overrideColour` (Color): Color used when `colourMode` is `Override`.
    *   *Responsibility:* Finds the referenced `ArtSetApplicator` (e.g., in parent), gets the relevant `ArtSet`, finds the `PicItem` and `ColourItem` by ID (using efficient lookups provided by the applicator), and applies the sprite/color to the `Image` component based on `spriteMode` and `colourMode`. Handles fallback to `PicItem.defaultColour` or the applicator's default `ColourItem` if needed.
*   **`TextArtElement`**:
    *   Attached to a GameObject with a `Text` or `TextMeshProUGUI` component.
    *   `fontItemId` (string - GUID): ID of the `FontItem` to use.
    *   `colourItemId` (string - GUID): ID of the `ColourItem` to use.
    *   `fontMode` (enum: `FromSet`, `Override`): Determines font source.
    *   `colourMode` (enum: `FromSet`, `Override`, `DefaultFontColour`): Determines color source.
    *   `overrideFont` (Font): Legacy font override.
    *   `overrideTmpFont` (TMP_FontAsset): TMP font override.
    *   `overrideColour` (Color): Color override.
    *   *Responsibility:* Finds the `ArtSetApplicator`, gets the `ArtSet`, finds `FontItem` and `ColourItem` by ID. Applies the correct font type (`Font` or `TMP_FontAsset`) and color to the attached text component based on modes. Handles fallback to `FontItem.defaultColour` or the applicator's default `ColourItem`.
*   **`AnimationElement`**:
    *   Attached to a UI GameObject (RectTransform). Requires a `CanvasGroup` component on the same GameObject if fading is used.
    *   `animationItemId` (string - GUID): ID of the `AnimationItem` to use.
    *   `overrideDuration` (bool): Whether to override the duration.
    *   `durationOverride` (float): The override duration value.
    *   *Responsibility:* Finds the `ArtSetApplicator`, gets the `ArtSet`, finds the `AnimationItem` by ID. Provides the animation data (potentially with overridden duration) to an `AnimationApplicator` component (which might be added/managed by this component or the `ArtSetApplicator`). Ensures a `CanvasGroup` exists if the selected `AnimationItem.useFade` is true.
*   **`AnimationApplicator`**:
    *   Attached to the same GameObject as `AnimationElement` (potentially added automatically or via the "Setup Element Components" tool). Requires a `CanvasGroup` component.
    *   *Responsibility:* Executes the animation defined by an `AnimationItem`. Designed to be **replaceable** by a more advanced implementation later.
    *   **Initial Implementation (Fade Only):**
        *   Requires a `CanvasGroup` component on the GameObject.
        *   Sets the initial state (`CanvasGroup.alpha = AnimationItem.fadeStartOpacity`) if `AnimationItem.useFade` is true.
        *   Animates the `CanvasGroup.alpha` back to 1 using the `AnimationItem.curve` and `duration`.
        *   Pros: Simple, self-contained logic for basic fade-in.
        *   Cons: Limited functionality (only fade-in), potentially less performant than dedicated tweening libraries for complex scenarios.
    *   *Interface:* Should expose simple `Play()` and potentially `Stop()` methods.
*   **`ArtSetApplicator`**:
    *   Attached to a root UI GameObject for a specific section (e.g., Main Menu Panel).
    *   `useArtSetting` (bool): If true, uses the `ArtStyle` from the `ArtSetting` singleton and subscribes to its `OnActiveStyleChanged` event.
    *   `artStyleOverride` (`ArtStyle`): Used if `useArtSetting` is false.
    *   `artSetTypeFilter` (`ArtSetType`): Specifies the type of `ArtSet` this applicator manages.
    *   `selectedArtSet` (`ArtSet`): The specific `ArtSet` to apply. This can be set directly (if no `ArtStyle` is used) or selected from a dropdown filtered by `artSetTypeFilter` if an `ArtStyle` is active.
    *   `defaultPicItem` (`PicItem`): Fallback if a `PicItem` ID is not found in the `selectedArtSet`.
    *   `defaultColourItem` (`ColourItem`): Fallback if a `ColourItem` ID is not found.
    *   `defaultFontItem` (`FontItem`): Fallback if a `FontItem` ID is not found.
    *   `defaultAnimationItem` (`AnimationItem`): Fallback if an `AnimationItem` ID is not found.
    *   `applyOnStart` (bool): Automatically apply the style when the component starts.
    *   *Responsibility:*
        *   Determines the active `ArtSet` based on `ArtSetting`, `artStyleOverride`, and `selectedArtSet`.
        *   Builds/caches lookup dictionaries (`Dictionary<string, TItem>`) for the items within the active `ArtSet`'s referenced `PicSet`, `ColourSet`, `FontSet`, and `AnimationSet` for efficient querying by child `Element` components.
        *   Provides the active `ArtSet` reference and efficient item lookup methods (e.g., `TryGetPicItem(string id, out PicItem item)`) to child `Element` components.
        *   Triggers the `ApplyStyle` method on start if `applyOnStart` is true, or when the `ArtSetting.OnActiveStyleChanged` event is received (if `useArtSetting` is true).
        *   Provides default `Item` values for `Element` components when an ID lookup fails.
        *   Handles subscribing/unsubscribing to `ArtSetting.OnActiveStyleChanged`.
    *   `ApplyStyle()` Method: Iterates through child `Element` components and triggers their update logic (or they pull data when enabled/updated). Rebuilds lookup dictionaries if the `ArtSet` has changed.

## Editor Implementations

*   **`PicSet` Editor:**
    *   Uses a custom editor (`UnityEditor.Editor`).
    *   Displays the `List<PicItem>` using `EditorGUI` and `EditorGUILayout` controls, potentially leveraging `UnityEditorInternal.ReorderableList`.
    *   Aims for a grid layout using `EditorGUILayout.BeginHorizontal` and `EditorGUILayout.EndHorizontal`, calculating columns based on inspector width.
    *   Each grid cell is selectable.
    *   Inside the cell: Displays `PicItem.name`, `PicItem.sprite` preview (`EditorGUI.DrawPreviewTexture` or `GUILayout.Box`), and `PicItem.defaultColour` (`EditorGUI.ColorField` as a small rect). Use a placeholder texture if `sprite` is null.
    *   Maintains a selection index. When a cell is clicked, update the index.
    *   If an item is selected, display its full editor fields (name, sprite object field, color field) above or below the grid.
    *   Provides a "+" button to add a new `PicItem` (assigning a new `GUID` automatically).
*   **`ColourSet` Editor:**
    *   Similar custom editor structure to `PicSet` Editor, using a responsive grid for `ColourItem`s.
    *   Each grid cell shows `ColourItem.name` and a preview of the `colour` (`EditorGUI.DrawRect` or similar).
    *   Clicking selects the `ColourItem` and shows its full editor fields (`name`, `colour` field) above or below the grid.
    *   "+" button adds a new `ColourItem` (assigning a new `GUID` automatically).
*   **`FontSet` Editor:**
    *   Custom editor using `ReorderableList`.
    *   Each element represents a `FontItem`.
    *   Displays fields for `name`, `font` (ObjectField), `tmpFont` (ObjectField), and `defaultColour` (ColorField).
    *   Includes a toggle (`Draw Font Previews`, default off) at the top. If enabled, attempts to show a small sample text preview using the selected font (may impact performance).
    *   `ReorderableList` provides add/remove; ensure new items get a unique `GUID` automatically.
*   **`AnimationSet` Editor:**
    *   Custom editor using `ReorderableList`.
    *   Each element represents an `AnimationItem`.
    *   Displays fields for `name`, `duration`, `useFade` toggle, `fadeStartOpacity` float field, and the `curve` field.
    *   `ReorderableList` handles adding/removing; ensure new items get a unique `GUID` automatically.
*   **`ImageArtElement` Editor:**
    *   Custom editor for `ImageArtElement`.
    *   Finds the associated `ArtSetApplicator`.
    *   If an applicator and a valid `ArtSet` with `PicSet`/`ColourSet` are found:
        *   Displays dropdowns (`EditorGUILayout.Popup`) for selecting `PicItem` and `ColourItem` by name, storing the corresponding ID.
    *   Displays enum popups for `spriteMode` and `colourMode`.
    *   Conditionally displays `overrideSprite` and `overrideColour` fields.
    *   Shows read-only previews of the currently applied sprite and color tint.
*   **`TextArtElement` Editor:**
    *   Custom editor for `TextArtElement`.
    *   Finds the associated `ArtSetApplicator`.
    *   If an applicator and a valid `ArtSet` with `FontSet`/`ColourSet` are found:
        *   Displays dropdowns for selecting `FontItem` and `ColourItem` by name, storing the ID.
    *   Displays enum popups for `fontMode` and `colourMode`.
    *   Conditionally displays `overrideFont`, `overrideTmpFont`, and `overrideColour` fields.
    *   Indicates attached text component type (`Text` or `TextMeshProUGUI`).
*   **`AnimationElement` Editor:**
    *   Custom editor for `AnimationElement`.
    *   Finds the associated `ArtSetApplicator`.
    *   If an applicator and a valid `ArtSet` with `AnimationSet` are found:
        *   Displays a dropdown for selecting `AnimationItem` by name, storing the ID.
    *   Displays the `overrideDuration` toggle.
    *   Conditionally displays the `durationOverride` float field.
    *   Checks for and warns if a required `CanvasGroup` component is missing when `useFade` is relevant.
*   **`ArtSet` Editor:**
    *   Standard or simple custom editor.
    *   Displays `name` field.
    *   Displays an object field for `setType` (`ArtSetType`).
    *   Displays object fields for `picSet`, `colourSet`, `fontSet`, and `animationSet`. Assigning a new `ArtSet` should generate a new `GUID` automatically.
*   **`ArtSetApplicator` Editor:**
    *   Custom editor for `ArtSetApplicator`.
    *   Displays `useArtSetting` toggle.
    *   Conditionally displays `artStyleOverride` object field.
    *   Displays `artSetTypeFilter` object field.
    *   Determines the active `ArtStyle`.
    *   If an `ArtStyle` is active:
        *   Filters `ArtSet`s based on `artSetTypeFilter`.
        *   Displays a dropdown showing filtered `ArtSet` names, assigning the reference to `selectedArtSet`.
    *   If no `ArtStyle` is active:
        *   Displays a direct object field for `selectedArtSet`.
    *   Displays object fields for `defaultPicItem`, `defaultColourItem`, `defaultFontItem`, and `defaultAnimationItem`.
    *   Displays `applyOnStart` toggle.
    *   Includes buttons: "Apply Style Now" (triggers `ApplyStyle` in editor) and "Setup Element Components" (scans children with `Image`/`Text`/etc., adds corresponding `Element` components, and potentially `AnimationApplicator` if `AnimationElement` is added).
*   **`ArtStyle` Editor:**
    *   Custom editor, potentially using `ReorderableList` for `artSets`.
    *   Displays `name` field.
    *   Displays `ReorderableList` for `availableSetTypes` (`ArtSetType` references).
    *   Displays `ReorderableList` for `artSets` (`ArtSet` references). Validate that added `ArtSet`s have a `setType` matching one in `availableSetTypes`. Assigning a new `ArtStyle` should generate a new `GUID` automatically.
*   **`ArtSetting` Editor:**
    *   Custom editor for the singleton `ArtSetting` ScriptableObject.
    *   Displays an object field for `activeArtStyle`.
    *   *(Optional Info Display):* Could add read-only sections showing the hierarchy within the `activeArtStyle`.


## Extra Notes

*   **Naming Conventions:** While IDs ensure robust referencing, encourage artists and designers to use clear and consistent naming conventions for `Item`s, `Set`s, `Style`s, and `Type`s. This significantly improves organization and usability within the editors.
*   **Editor Performance:** Be mindful of potential performance bottlenecks in custom editors, especially when dealing with large `Set`s (hundreds/thousands of items) or complex previews (like font rendering). Optimize editor code where necessary (e.g., avoid heavy calculations in `OnGUI`, use efficient drawing methods).
*   **User Documentation:** Plan for creating documentation or tutorials for the artists and designers who will primarily interact with this system. Clear guidance on creating styles, sets, items, and applying them via components will be crucial for adoption.
*   **Future Extensions:** While the current scope focuses on core visual elements (images, colors, fonts, basic fade animation), the system is designed for modularity. Future extensions could include sound effects (`SoundItem`, `SoundSet`), material properties, or integration with more advanced animation/tweening systems. Focus on delivering the current scope robustly first.
*   **Asset Management:** Consider how these numerous ScriptableObject assets will be organized within the Unity project structure. A clear folder hierarchy (e.g., `Assets/ArtStyles/[StyleName]/Sets/`, `Assets/ArtStyles/[StyleName]/Items/`) is recommended.

## Implementation Notes

*   **GUID Generation:** Use `System.Guid.NewGuid().ToString()` to generate IDs. Ensure this happens automatically and reliably when new `Item`, `Set`, `Style`, or `Type` ScriptableObjects are created via editor scripts (e.g., using `ScriptableObject.CreateInstance` and setting the ID immediately, or within `OnEnable` if the ID is null/empty). The `id` field should likely be `[SerializeField]` but potentially `[HideInInspector]` or drawn read-only in custom editors to prevent accidental manual changes.
*   **Dictionary Caching:** The `ArtSetApplicator` is responsible for building and caching the lookup dictionaries (`Dictionary<string, TItem>`) for the active `ArtSet`.
    *   Build/rebuild these dictionaries in `Awake` or `OnEnable`, and whenever the `selectedArtSet` (or the `ArtStyle` providing it) changes.
    *   Ensure the dictionaries handle cases where referenced `Set`s (`picSet`, `colourSet`, etc.) might be null.
    *   Provide clear, efficient public methods like `TryGetPicItem(string id, out PicItem item)` for `Element` components to use.
*   **Finding the Applicator:** `Element` components should find their parent `ArtSetApplicator`. `GetComponentInParent<ArtSetApplicator>(true)` is suitable. Cache this reference in the `Element`'s `Awake` or `OnEnable` to avoid repeated lookups.
*   **Event Handling (`OnActiveStyleChanged`):**
    *   Implement `ArtSetting.OnActiveStyleChanged` using a standard C# event (`public event System.Action<ArtStyle> OnActiveStyleChanged;`) or a `UnityEvent<ArtStyle>`.
    *   `ArtSetApplicator` components (where `useArtSetting` is true) must subscribe to this event in `OnEnable` and unsubscribe in `OnDisable` to prevent errors and memory leaks.
    *   The event handler should trigger the applicator to re-evaluate its `selectedArtSet` based on the new global `ArtStyle` and call `ApplyStyle()`.
*   **Editor Scripting Best Practices:**
    *   Place all editor scripts (`CustomEditor`, `EditorWindow`, etc.) within an "Editor" folder.
    *   Use `SerializedObject` and `SerializedProperty` (`serializedObject.FindProperty`, `EditorGUILayout.PropertyField`) for modifying ScriptableObject/MonoBehaviour fields in custom editors. This ensures proper handling of undo/redo, prefab overrides, and multi-object editing.
    *   Use `EditorGUI.BeginChangeCheck()` and `EndChangeCheck()` around sections that modify properties to correctly mark the object dirty (`EditorUtility.SetDirty`) and save changes.
*   **`AnimationApplicator` (Initial):** Keep the initial fade implementation simple, likely using a coroutine started by `Play()` within the `AnimationApplicator`. Ensure it correctly uses the `AnimationItem` data (duration, curve, start opacity) and accesses the required `CanvasGroup`. The `Stop()` method should halt the coroutine. Design its interface (`Play`/`Stop`) clearly so it can be easily replaced later.
*   **Error Handling & Defaults:** Implement robust null checks throughout the system (e.g., checking if `selectedArtSet`, `picSet`, `colourSet`, etc., are assigned). When an `Item` ID lookup fails within an `Element`, log a clear `Debug.LogWarning` indicating the missing ID and the GameObject context, then attempt to use the corresponding `default...Item` provided by the `ArtSetApplicator`.
*   **TextMeshPro vs. Legacy Text:** The `TextArtElement` must detect whether the attached component is `UnityEngine.UI.Text` or `TMPro.TextMeshProUGUI` (check for both) and apply font/color changes using the appropriate API (`.font`/`.color` vs. `.font`/`.color`).
*   **`ArtSetting` Singleton:** Implement a reliable way to access the single `ArtSetting` instance. Common patterns include:
    *   A static property that uses `Resources.Load<ArtSetting>("Path/To/ArtSettingAsset")`. Requires the asset to be in a "Resources" folder.
    *   A static property that finds the asset using `AssetDatabase.FindAssets` (Editor-only, suitable for accessing in editor scripts) or loads it from a predefined path.
    *   Using a manager scene/prefab that holds a reference. (Less ideal for a settings asset).
    Choose a method appropriate for accessing it both at runtime and in editor scripts.
*   **Code Structure:** Use the namespace `Modules.ArtStyle` and `Modules.ArtStyle.Editor` to prevent naming conflicts. Keep data structures (ScriptableObjects), runtime logic (MonoBehaviours), and editor logic (Editor scripts) in separate, well-organized folders.

## Final Considerations

*   **GUID Generation Strategy:** Decide definitively whether GUIDs are generated in `OnEnable` for each SO (simpler, runs often) or only during asset creation via custom editor logic (cleaner, requires careful implementation). **Recommendation:** `OnEnable` is often easier, but ensure the `id` field is serialized and consider making it read-only in editors.
*   **Dictionary Location:** Confirm the strategy for lookup dictionaries. Sticking with the `ArtSetApplicator` building dictionaries for its active `ArtSet` is recommended initially for simplicity. Re-evaluate only if profiling shows it's a bottleneck.
*   **Editor Usability Focus:** Prioritize the user experience for artists/designers. Get early feedback on editor workflows. Ensure clarity in dropdowns, previews, and error messages. Monitor and optimize editor performance, especially for large sets.
*   **Keep `AnimationApplicator` Simple:** Strictly adhere to the minimal fade-only implementation for the initial `AnimationApplicator`. Ensure `Play`/`Stop` work correctly with coroutines and `CanvasGroup.alpha`. Resist adding more features here initially.
*   **Testing Plan:** Define a testing strategy. Consider basic unit tests if feasible, but prioritize integration testing in dedicated test scenes to verify core functionalities like style application, overrides, runtime switching, defaults, and editor tools ("Setup Element Components").
*   **Avoid Scope Creep:** Focus on implementing the features exactly as defined in this document. Defer any new ideas or extensions until the core system is complete and stable.
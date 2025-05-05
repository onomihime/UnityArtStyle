# Art Style System - Usage Guide

This guide explains how to set up and use the Art Style system in your Unity project.

## 1. Initial Setup: The `ArtSetting` Singleton

The `ArtSetting` ScriptableObject acts as the central configuration for the entire system within your project.

1.  **Create the `ArtSetting` Asset:**
    *   Go to `Assets -> Create -> Art Style -> Art Setting`.
    *   Name it appropriately (e.g., "ProjectArtSetting").
    *   **Important:** You should only have **one** `ArtSetting` asset loaded in your project at any time. The system will log an error if duplicates are detected. Place it in a location that's always included (e.g., directly under `Assets` or in a `Resources` folder if needed for runtime loading, though direct referencing is preferred).
2.  **Define `ArtSetType` Slots:**
    *   Select the `ArtSetting` asset.
    *   In the inspector, find the "Art Set Type Slots" list.
    *   This list defines the categories of UI sections your styles will need to cover (e.g., "MainMenu", "GameplayHUD", "Buttons", "Panels").
    *   Create `ArtSetType` assets (`Assets -> Create -> Art Style -> Art Set Type`) for each category you need.
    *   Drag these `ArtSetType` assets into the "Art Set Type Slots" list on your `ArtSetting`. The order might be relevant for editor display in `ArtStyle`.

## 2. Defining Structure: `ArtSetType` and `ItemType`s

Each `ArtSetType` defines the specific "slots" available for different kinds of visual elements.

1.  **Select an `ArtSetType` Asset:**
    *   Find an `ArtSetType` asset you created (e.g., "ButtonArtSetType").
2.  **Define `ItemType` Slots:**
    *   In the inspector, you'll see lists for `Pic Item Types`, `Colour Item Types`, `Font Item Types`, and `Animation Item Types`.
    *   For each list, add the specific named slots you need for this type. Examples for a "Button":
        *   **Pic Item Types:** "Background", "Icon", "Border"
        *   **Colour Item Types:** "Normal Text", "Hover Text", "Disabled Text", "Icon Tint"
        *   **Font Item Types:** "Button Label Font"
        *   **Animation Item Types:** "Hover Animation", "Click Animation"
    *   Give each `ItemType` a clear, descriptive `Name`. The `Id` (GUID) is generated automatically and should not be changed.

## 3. Creating Styles: `ArtStyle` and `ArtSet`s

An `ArtStyle` groups together different `ArtSet`s to form a complete visual theme.

1.  **Create an `ArtStyle` Asset:**
    *   Go to `Assets -> Create -> Art Style -> Art Style`. Name it (e.g., "SciFiStyle", "FantasyStyle").
2.  **Assign `ArtSet`s:**
    *   Select the `ArtStyle` asset.
    *   The inspector shows slots corresponding to the `ArtSetType`s defined in your `ArtSetting`.
    *   For each slot (e.g., "MainMenu", "Buttons"), you need to assign an `ArtSet` asset.
3.  **Create `ArtSet` Assets:**
    *   Go to `Assets -> Create -> Art Style -> Art Set`. Name it descriptively (e.g., "SciFiStyle_Buttons", "FantasyStyle_MainMenu").
    *   **Assign `ArtSetType`:** Select the `ArtSet` asset. Drag the corresponding `ArtSetType` (e.g., "ButtonArtSetType") onto the "Set Type" field. This is crucial for defining the structure of the contained `Set`s.
    *   Assign these newly created `ArtSet` assets to the appropriate slots in your `ArtStyle` asset.
4.  **Add `ArtStyle` to `ArtSetting`:**
    *   Select your main `ArtSetting` asset.
    *   Drag your new `ArtStyle` asset(s) into the "Managed Art Styles" list.
    *   Set the "Active Art Style" dropdown to the style you want to be active by default.

## 4. Populating Sets: `PicSet`, `ColourSet`, etc. (Inline vs. Asset)

Now, fill the `ArtSet`s with actual visual data using `Set` objects (`PicSet`, `ColourSet`, `FontSet`, `AnimationSet`).

1.  **Select an `ArtSet` Asset:** (e.g., "SciFiStyle_Buttons")
2.  **Configure Contained Sets:**
    *   In the "Contained Sets" section, you have fields for `Pic Set`, `Colour Set`, `Font Set`, and `Animation Set`.
    *   For each, you have two main options:
        *   **Assign Asset:** Create a `Set` asset (`Assets -> Create -> Art Style -> Sets -> [Type]Set`) and drag it onto the field. This is good for reusable sets of items.
        *   **Create Inline:** Click the "Create Inline" button. This embeds the `Set` data directly within the `ArtSet` asset. Good for sets specific to this `ArtSet` only.
3.  **Edit Set Items:**
    *   Once a `Set` is assigned or created inline, foldout sections appear below (e.g., "Picture Set Items").
    *   These sections show slots based on the `ItemType`s defined in the `ArtSet`'s assigned `ArtSetType`.
    *   **Default/Fallback Item (Index 0):** This slot **must** be filled. It's used when a specific `ItemType` lookup fails later.
    *   **Item Slots:** For each named slot (e.g., "Background", "Icon"), you can:
        *   **Assign Asset:** Create an `Item` asset (`Assets -> Create -> Art Style -> Items -> [Type]Item`) and drag it onto the object field. Good for reusable items (like a common font or color).
        *   **Create Inline:** Click "Create Inline" to embed the `Item` data directly within the containing `Set` (which might itself be inline or an asset). Good for unique items.
        *   **Clear:** Remove the assigned item.
    *   **Extra Items:** Items that exist in the `Set`'s list but don't correspond to a current `ItemType` slot are shown here.
    *   **Extract Asset:** If an item or set is an inline instance, an "Extract Asset" button appears, allowing you to save it as a separate, reusable `.asset` file.

## 5. Applying Styles: The `ArtSetApplicator` Component

This component links the style system to your actual UI elements in the scene.

1.  **Add `ArtSetApplicator`:** Add the `ArtSetApplicator` component to a root GameObject of the UI section you want to style (e.g., the main Canvas, a specific Panel).
2.  **Configure Source:**
    *   **`Use Art Setting` (Recommended):** Leave checked to use the globally active style from `ArtSetting`.
        *   **`Art Set Type Filter`:** Select the `ArtSetType` that corresponds to this UI section (e.g., "MainMenu", "Buttons"). The applicator will find the `ArtSet` matching this type within the active `ArtStyle`.
    *   **Overrides (Use with caution):**
        *   Uncheck `Use Art Setting` to manually control the style.
        *   `Art Style Override`: Assign a specific `ArtStyle`.
        *   `Art Set Override`: Assign a specific `ArtSet` directly, ignoring Style and Type Filter. This is the most direct override.
3.  **Set Default Fallbacks:** Assign default `Item` assets (`_defaultPicItem`, etc.). These are used if an item lookup fails completely (e.g., the required slot is empty in the `Set`, and the `Set`'s own default item is also missing).
4.  **Map Elements:** This is the core mapping step.
    *   Expand the "Image Elements", "Legacy Text Elements", "TMP Text Elements", and "Animation Elements" foldouts.
    *   For each UI element you want to style:
        *   Add an entry to the appropriate list.
        *   **Target:** Drag the specific UI component (`Image`, `Text`, `TMP_Text`, `RectTransform` for animations) onto the "Target" field.
        *   **Item Selection:** Use the dropdowns (e.g., "Pic Item", "Tint Colour", "Font Item") to select the *`ItemType` Name* (defined in your `ArtSetType`) that should provide the data for this component property. The dropdowns are populated based on the `ArtSetType` resolved from the Source Configuration.
        *   **Overrides:** If you need this specific element to deviate from the style:
            *   Check the relevant override flag (e.g., `overrideSpriteFlag`).
            *   Assign the override value directly (e.g., drag a specific `Sprite` to `overrideSprite`).
            *   Special flags like `usePicDefaultColourFlag` or `useFontDefaultColourFlag` allow using the default color defined within the selected `PicItem` or `FontItem` instead of looking up a `ColourItem`.
5.  **Apply:**
    *   Click "Apply Style Now" in the inspector to see changes immediately in the editor.
    *   The style will also apply automatically at runtime based on the configuration.

## 6. Runtime Style Switching

1.  Get a reference to the `ArtSetting.Instance`.
2.  Set the `ArtSetting.Instance.ActiveArtStyle` property to a different `ArtStyle` asset (which must be present in the `ArtSetting`'s "Managed Art Styles" list).
3.  The `OnActiveStyleChanged` event will fire.
4.  Active `ArtSetApplicator` components subscribed to the event will automatically flag themselves to update their resolved `ArtSet` and rebuild internal lookups the next time they need to apply the style (e.g., if `ApplyStyle()` is called manually or if they are designed to re-apply automatically - current implementation requires manual re-application after style change). *Consider adding an option or method to force re-application on all applicators after a style change if needed.*

## 7. Animation Setup

1.  Map animation targets in the `ArtSetApplicator`'s "Animation Elements" list, selecting the target `RectTransform` and the desired `AnimationItemType`.
2.  Ensure the target GameObject has:
    *   An `AnimationApplicator` component.
    *   A `CanvasGroup` component (required for fade animations).
3.  Use the "Setup Animation Components" button in the `ArtSetApplicator` inspector to automatically add missing `AnimationApplicator` and `CanvasGroup` components to all targeted transforms.
4.  To trigger an animation at runtime, get a reference to the root `ArtSetApplicator` and call `artSetApplicator.PlayAnimation(targetRectTransform)`.
5.  Use the "Play Animation (Runtime Only)" button in the `AnimationApplicator` inspector (on the target GameObject) to test the default animation during Play mode.

## 8. Best Practices & Tips

*   **Clear Naming:** Use descriptive names for `ArtSetType`s, `ItemType`s, `ArtStyle`s, `ArtSet`s, and `Item`s. This makes mapping in the `ArtSetApplicator` much easier.
*   **Defaults are Crucial:** Always define the Default/Fallback item (index 0) in every `Set` (`PicSet`, `ColourSet`, etc.). Also, assign the Default Fallback items in the `ArtSetApplicator`. This prevents errors when styles are incomplete.
*   **Inline vs. Asset:** Use `.asset` files for `Item`s and `Set`s that are reused across multiple `ArtSet`s or `ArtStyle`s. Use inline instances for elements unique to a specific context to reduce project clutter. Use "Extract Asset" if an inline instance becomes reusable.
*   **`ArtSetType` Design:** Plan your `ArtSetType`s and their `ItemType` slots carefully. They define the fundamental structure your styles must adhere to.
*   **Folder Structure:** Keep your Art Style assets organized (e.g., separate folders for Types, Styles, Sets, Items).
*   **Testing:** Test style switching and ensure all elements update correctly. Verify fallback behavior when items are missing.

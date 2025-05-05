# Art Style System - Technical Documentation

## 1. Introduction & Goal

This document outlines the technical design of the modular Art Style system for Unity.

**Goal:** To provide a highly flexible and structured system for defining and managing UI visual styles. It aims to enable artists to easily create, swap, and override visual themes while minimizing scene component clutter and relying on ScriptableObjects for data management.

## 2. Core Concepts

*   **Hierarchy & Types:** The system uses a nested hierarchy of ScriptableObjects and defined types to structure visual data.
*   **Application:** `ArtSetApplicator` components on UI GameObjects map specific UI elements (`Image`, `Text`, etc.) to items defined within an `ArtSet`.
*   **Referencing & Performance:** ScriptableObjects are primarily referenced via unique GUID strings (`Id`). Lookups are cached where appropriate.
*   **Inline vs. Asset Flexibility:** Key data containers (`Set`s and `Item`s) can exist as reusable `.asset` files or as inline instances embedded within their parent ScriptableObject.
*   **Type System Robustness:** The system handles changes in type definitions (e.g., adding/removing slots) by retaining matching items and marking non-matching ones as "extra". Default/fallback items are used for missing slots.
*   **Runtime Style Switching:** The system supports changing the active style at runtime via the `ArtSetting` singleton.

## 3. Data Structures

*(Based on Revision 3 Design)*

### 3.1. Item Types (`ScriptableObject`)

*   Base class for individual visual elements. Can be `.asset` files or inline instances within a `Set`. All inherit from `IArtItem` (defined in `ArtSetApplicator`) requiring a `string Id` property.
*   **`PicItem`**: `id`, `name`, `_sprite`, `_defaultColour`.
*   **`ColourItem`**: `id`, `name`, `_colour`.
*   **`FontItem`**: `id`, `name`, `_font`, `_tmpFont`, `_defaultColour`.
*   **`AnimationItem`**: `id`, `name`, `_duration`, `_useFade`, `_fadeStartOpacity`, `_curve`.

### 3.2. Set Types (`ScriptableObject`)

*   Base class `BaseSet<T>` (defined in `ArtSetApplicator`) requires `List<T> Items`. Contain collections of `Item`s mapped to `ItemType` slots. Can be `.asset` files or inline instances within an `ArtSet`.
*   **`PicSet`**: Manages `PicItem`s in `_items` list.
*   **`ColourSet`**: Manages `ColourItem`s in `_items` list.
*   **`FontSet`**: Manages `FontItem`s in `_items` list.
*   **`AnimationSet`**: Manages `AnimationItem`s in `_items` list.
    *   Index 0 of the `_items` list is always considered the **Default/Fallback** item for that set.

### 3.3. Hierarchy Types (`ScriptableObject` unless noted)

*   **`ItemType`** (Base class, **Not** SO, `[System.Serializable]`):
    *   `Id` (string - GUID): Unique persistent identifier within its `ArtSetType`.
    *   `Name` (string): User-friendly name (e.g., "Button Background", "Primary Text").
*   **`PicItemType`** : `ItemType` { }
*   **`ColourItemType`** : `ItemType` { }
*   **`FontItemType`** : `ItemType` { }
*   **`AnimationItemType`** : `ItemType` { }
    *(These are defined and managed within the `ArtSetType` editor)*

*   **`ArtSetType`** (`ScriptableObject`):
    *   `_id` (string - GUID): Unique persistent identifier.
    *   `_name` (string): User-friendly name (e.g., "Button", "Panel").
    *   `_picItemTypes` (`List<PicItemType>`): Defines picture slots.
    *   `_colourItemTypes` (`List<ColourItemType>`): Defines colour slots.
    *   `_fontItemTypes` (`List<FontItemType>`): Defines font slots.
    *   `_animationItemTypes` (`List<AnimationItemType>`): Defines animation slots.
*   **`ArtSet`** (`ScriptableObject` - **Must be Asset**):
    *   `_id` (string - GUID): Unique persistent identifier.
    *   `_name` (string): User-friendly name (e.g., "MainMenu_SciFi_ButtonSet").
    *   `_setType` (`ArtSetType`): Reference to the type defining the structure.
    *   `_picSet` (`PicSet`): Reference or inline instance.
    *   `_colourSet` (`ColourSet`): Reference or inline instance.
    *   `_fontSet` (`FontSet`): Reference or inline instance.
    *   `_animationSet` (`AnimationSet`): Reference or inline instance.
*   **`ArtStyle`** (`ScriptableObject`):
    *   `_id` (string - GUID): Unique persistent identifier.
    *   `_name` (string): User-friendly name (e.g., "SciFiTheme", "FantasyTheme").
    *   `_artSets` (Structure mapping `ArtSetType` ID to `ArtSet`): Contains `ArtSet` references, conforming to slots defined in `ArtSetting`. Handles extras. *(Implementation details may vary - currently uses a List)*.
*   **`ArtSetting`** (Singleton `ScriptableObject`):
    *   `_artSetTypeSlots` (`List<ArtSetType>`): Defines the required `ArtSetType`s for all managed styles.
    *   `_artStyles` (`List<ArtStyle>`): List of styles managed by this setting.
    *   `_activeArtStyleIndex` (int): Index of the active style in the `_artStyles` list.
    *   `ActiveArtStyle` (Property): Gets/sets the active style, triggers `OnActiveStyleChanged`.
    *   `OnActiveStyleChanged` (UnityEvent): Event triggered when `ActiveArtStyle` changes.
    *   **Singleton Logic:** Uses a static `Instance` property. `OnEnable` assigns `this` to `_instance` if null, logs error if a different instance exists. `OnDisable` clears `_instance` if it matches `this`.

### 3.4. Element Data Classes (`[System.Serializable]`, **Not** SO or MonoBehaviour)

*   Stored in lists within `ArtSetApplicator`. Define mappings between UI components and `ItemType`s.

*   **`ImageArtElement`**:
    *   `targetImage` (`Image`): Target component.
    *   `picItemId` (string): ID of `PicItemType` for sprite.
    *   `colourItemId` (string): ID of `ColourItemType` for tint.
    *   `overrideSpriteFlag` (bool): Use `overrideSprite`?
    *   `overrideColourFlag` (bool): Use `overrideColour`?
    *   `usePicDefaultColourFlag` (bool): Use the `PicItem`'s `_defaultColour` instead of looking up `colourItemId`?
    *   `overrideSprite` (`Sprite`): Override value.
    *   `overrideColour` (`Color`): Override value.

*   **`LegacyTextArtElement`**:
    *   `targetText` (`Text`): Target component.
    *   `fontItemId` (string): ID of `FontItemType`.
    *   `colourItemId` (string): ID of `ColourItemType`.
    *   `overrideFontFlag` (bool): Use `overrideFont`?
    *   `overrideColourFlag` (bool): Use `overrideColour`?
    *   `useFontDefaultColourFlag` (bool): Use the `FontItem`'s `_defaultColour` instead of looking up `colourItemId`?
    *   `overrideFont` (`Font`): Override value.
    *   `overrideColour` (`Color`): Override value.

*   **`TMPTextArtElement`**:
    *   `targetText` (`TMP_Text`): Target component.
    *   `fontItemId` (string): ID of `FontItemType`.
    *   `colourItemId` (string): ID of `ColourItemType`.
    *   `overrideFontFlag` (bool): Use `overrideTmpFont`?
    *   `overrideColourFlag` (bool): Use `overrideColour`?
    *   `useFontDefaultColourFlag` (bool): Use the `FontItem`'s `_defaultColour` instead of looking up `colourItemId`?
    *   `overrideTmpFont` (`TMP_FontAsset`): Override value.
    *   `overrideColour` (`Color`): Override value.

*   **`AnimationElement`**:
    *   `targetTransform` (`RectTransform`): Target component's transform.
    *   `animationItemId` (string): ID of `AnimationItemType`.
    *   `overrideAnimationFlag` (bool): Use `overrideAnimationItem`?
    *   `overrideAnimationItem` (`AnimationItem`): Override value.

## 4. Component Behaviours (MonoBehaviours)

*   **`ArtSetApplicator`**:
    *   **Purpose:** Applies a resolved `ArtSet` to mapped UI elements.
    *   **Configuration:** `_useArtSetting`, `_artStyleOverride`, `_artSetTypeFilter`, `_artSetOverride`, Default Fallback Items (`_defaultPicItem`, etc.), Element Mappings (`_imageElements`, etc.).
    *   **Core Logic:**
        *   `ResolveActiveArtSet()`: Determines the `ArtSet` to use based on configuration.
        *   `BuildLookupDictionaries()`: Caches `Item`s from the resolved `ArtSet` by their `ItemType` ID. Triggered when the resolved set changes or `ApplyStyle` is forced.
        *   `ApplyStyle()` / `ApplyStyleInternal()`: Iterates through element lists, resolves final values using lookups and overrides (`ResolveSprite`, `ResolveImageColour`, etc.), and applies them to target components.
        *   `PlayAnimation()`: Finds the relevant `AnimationElement`, resolves the `AnimationItem`, finds the `AnimationApplicator` on the target, and calls its `Play` method.
        *   Handles subscription to `ArtSetting.OnActiveStyleChanged` to flag dictionaries for rebuild.
    *   **Editor Support:** `ApplyStyleEditor()`, `SetupElementComponentsEditor()`.

*   **`AnimationApplicator`**:
    *   **Purpose:** Executes a specific animation defined by an `AnimationItem`. Designed to be simple and potentially replaceable.
    *   **Requirements:** Requires a `CanvasGroup` on the same GameObject if fade animations are used.
    *   **Configuration:** `_playOnEnable`, `_defaultAnimationItem`.
    *   **Interface:** `Play(AnimationItem item, float duration)`, `Stop()`. Manages the animation coroutine.

## 5. Editor Implementations

*   **`Item` Editors (`PicItemEditor`, etc.):** Simple editors for `.asset` files and used for drawing inline instances within `Set` editors.
*   **`Set` Editors (`PicSetEditor`, etc. - Integrated into `ArtSetEditor`):** Display slots based on the parent `ArtSet`'s `ArtSetType`. Allow assigning `.asset` Items or creating/editing inline Items. Show "extra" items.
*   **`ArtSetType` Editor:** Manages lists of `ItemType` definitions (Pic, Colour, Font, Anim). Allows adding (generates ID), removing, reordering, editing names.
*   **`ArtSet` Editor:** Manages `_setType` reference. Allows assigning `.asset` Sets or creating/editing inline Sets. Draws the corresponding `Set` editor inline.
*   **`ArtStyle` Editor:** Manages `ArtSet` references mapped to `ArtSetType` slots defined in `ArtSetting`. Shows "extra" sets.
*   **`ArtSetting` Editor:** Manages `_artSetTypeSlots` and `_artStyles`. Provides selector for `_activeArtStyleIndex`. Shows duplicate instance warning.
*   **`ArtSetApplicator` Editor:** Configures source (`ArtSetting` vs overrides), default fallbacks, and element mappings (`ReorderableList`s). Element editors allow target component assignment, `ItemType` ID selection (dropdowns based on resolved `ArtSetType`), and override configuration. Includes "Apply Style Now" and "Setup Animation Components" buttons.
*   **`AnimationApplicatorEditor`:** Provides a runtime "Play" button using the default animation item.
*   **Utility Classes:** `ArtSetEditorUtils`, `ArtSetItemEditorUtils` provide helper functions for drawing complex fields (like inline/asset object fields with previews).

## 6. Key Features & Notes

*   **GUID Generation:** IDs are crucial for stable referencing. Ensure they are generated reliably on creation.
*   **Serialization:** Inline instances rely on Unity's serialization (`[SerializeReference]` is generally *not* needed due to the structure using concrete `Set` types holding lists of concrete `Item` types or references).
*   **Error Handling:** Editors and runtime components include checks for nulls and missing definitions, often falling back to default items.
*   **Editor Usability:** Significant effort is placed on custom editors to manage the complexity, including inline creation, previews, and validation messages.
*   **"Extract to Asset":** A feature in `ArtSetEditorUtils` allows promoting inline instances (`Set`s or `Item`s) to reusable `.asset` files.

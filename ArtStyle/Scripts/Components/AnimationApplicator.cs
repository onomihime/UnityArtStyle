// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using System.Collections; // Required for Coroutines

namespace Modules.ArtStyle
{
    /// <summary>
    /// Applies a specific animation (currently fade) based on an AnimationItem.
    /// Requires a CanvasGroup component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class AnimationApplicator : MonoBehaviour
    {
        [Tooltip("Should the animation play automatically when this component is enabled?")]
        [SerializeField] private bool _playOnEnable = false;
        // Note: If playOnEnable is true, we need a way to know WHICH AnimationItem to play.
        // This implies the ArtSetApplicator needs to configure this component, or this
        // component needs a reference back or a default AnimationItem field.
        // For now, let's assume Play is called externally or playOnEnable uses a default/cached item.
        [Tooltip("Default Animation Item to use if Play is called without specifying one, or for Play On Enable.")]
        [SerializeField] private AnimationItem _defaultAnimationItem;


        private CanvasGroup _canvasGroup;
        private Coroutine _activeFadeCoroutine;
        private AnimationItem _lastPlayedItem; // Cache the item for potential use with playOnEnable

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (_playOnEnable)
            {
                // Decide which item to play. Use last played or default?
                AnimationItem itemToPlay = _lastPlayedItem ?? _defaultAnimationItem;
                if (itemToPlay != null)
                {
                    Play(itemToPlay, itemToPlay.Duration);
                }
                else
                {
                     Debug.LogWarning($"[AnimationApplicator] Play On Enable is true, but no AnimationItem is available (last played or default).", this);
                }
            }
        }

        /// <summary>
        /// Starts the animation defined by the AnimationItem.
        /// </summary>
        /// <param name="item">The AnimationItem defining the animation properties.</param>
        /// <param name="duration">The duration for this specific playback (can override item's default).</param>
        public void Play(AnimationItem item, float duration)
        {
            if (item == null)
            {
                Debug.LogError("[AnimationApplicator] Cannot play animation, AnimationItem is null.", this);
                return;
            }
            if (_canvasGroup == null)
            {
                 Debug.LogError("[AnimationApplicator] Cannot play animation, CanvasGroup component not found.", this);
                 return;
            }

            _lastPlayedItem = item; // Cache for potential playOnEnable

            // Stop any existing animation first
            Stop();

            if (item.UseFade)
            {
                _activeFadeCoroutine = StartCoroutine(FadeCoroutine(item, duration));
            }
            else
            {
                // Handle other animation types later if needed
                Debug.LogWarning($"[AnimationApplicator] AnimationItem '{item.name}' does not use fade. No animation played.", this);
            }
        }

        /// <summary>
        /// Stops any currently playing animation controlled by this component.
        /// </summary>
        public void Stop()
        {
            if (_activeFadeCoroutine != null)
            {
                StopCoroutine(_activeFadeCoroutine);
                _activeFadeCoroutine = null;
                // Optionally reset alpha or leave it as is? Resetting might be safer.
                // if (_canvasGroup != null) _canvasGroup.alpha = 1f; // Or target alpha?
            }
        }

        private IEnumerator FadeCoroutine(AnimationItem item, float duration)
        {
            if (duration <= 0) duration = item.Duration; // Use item's duration if override is invalid
            if (duration <= 0) // Still invalid? Set to a tiny value to avoid division by zero
            {
                 Debug.LogWarning($"[AnimationApplicator] Invalid duration (0 or less) for fade. Using 0.1s.", this);
                 duration = 0.1f;
            }


            float elapsedTime = 0f;
            float startAlpha = item.FadeStartOpacity;
            // Target alpha is assumed to be 1, based on the curve evaluating to 1 at time 1.
            float targetAlpha = item.Curve.Evaluate(1f);

            _canvasGroup.alpha = startAlpha;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime; // Use unscaled time for UI animations
                float timeNormalized = Mathf.Clamp01(elapsedTime / duration);
                float curveValue = item.Curve.Evaluate(timeNormalized);

                // Lerp between startAlpha and the final alpha defined by the curve's end value
                _canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, curveValue);

                yield return null; // Wait for the next frame
            }

            // Ensure final alpha is set precisely
            _canvasGroup.alpha = targetAlpha;
            _activeFadeCoroutine = null; // Mark as finished
        }
    }
}

// Copyright (c) 2025 onomihime (github.com/onomihime)
// originally from: github.com/onomihime/UnityArtStyle
// Licensed under the MIT License. See the LICENSE file in the repository root for full license text.
// This file may be used in commercial projects provided the above copyright notice and this permission notice appear in all copies.

using UnityEngine;
using System; // Required for Guid

namespace Modules.ArtStyle
{
    /// <summary>
    /// Represents an animation definition within an Art Style.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAnimationItem", menuName = "Art Style/Items/Animation Item")]
    public class AnimationItem : ScriptableObject // REMOVED: , ArtSetApplicator.IArtItem - Interface removed from Applicator
    {
        [SerializeField]
        [Tooltip("Unique persistent identifier (GUID). Auto-generated.")]
        private string _id;

        [SerializeField]
        [Tooltip("User-friendly name for identification in editors.")]
        private string _name = "New Animation Item";

        [SerializeField]
        [Tooltip("Duration of the animation in seconds.")]
        private float _duration = 0.2f;

        [SerializeField]
        [Tooltip("Should the animation include a fade in/out? Requires a CanvasGroup on the target.")]
        private bool _useFade = true;

        [SerializeField]
        [Tooltip("The starting opacity for the fade-in effect (0 = fully transparent). Only used if UseFade is true.")]
        [Range(0f, 1f)]
        private float _fadeStartOpacity = 0f; // ADDED

        [SerializeField]
        [Tooltip("Animation curve for controlling the animation's timing and easing.")]
        private AnimationCurve _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // ADDED (Default EaseInOut)


        // TODO: Add fields for specific animation properties (e.g., scale, position offset, rotation)

        /// <summary>
        /// Unique persistent identifier (GUID).
        /// </summary>
        public string Id => _id; // Implement interface property

        /// <summary>
        /// User-friendly name for identification in editors.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Duration of the animation in seconds.
        /// </summary>
        public float Duration => _duration;

        /// <summary>
        /// Should the animation include a fade in/out?
        /// </summary>
        public bool UseFade => _useFade;

        /// <summary>
        /// The starting opacity for the fade-in effect.
        /// </summary>
        public float FadeStartOpacity => _fadeStartOpacity; // ADDED

        /// <summary>
        /// Animation curve for controlling timing and easing.
        /// </summary>
        public AnimationCurve Curve => _curve; // ADDED

        private void OnEnable()
        {
            // Ensure ID is generated if missing
            if (string.IsNullOrEmpty(_id))
            {
                _id = Guid.NewGuid().ToString();
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
    }
}

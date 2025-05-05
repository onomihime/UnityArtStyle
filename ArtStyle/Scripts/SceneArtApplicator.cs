using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.ArtStyle
{
    
    [ExecuteAlways]
    public class SceneArtApplicator : MonoBehaviour
    {
        private void OnEnable()
        {
            // Apply art style in edit mode
            if (!Application.isPlaying)
            {
                ApplyArtStyle();
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            // Apply art style in play mode
            ApplyArtStyle();
        }

        public void ApplyArtStyle()
        {
            //Find all objects with the ArtSetApplicator component in the scene
            ArtSetApplicator[] artSetApplicators = FindObjectsOfType<ArtSetApplicator>();
            // Loop through each ArtSetApplicator and apply the active art style
            foreach (ArtSetApplicator applicator in artSetApplicators)
            {
                applicator.ApplyStyle();
            }
        }
    }
}

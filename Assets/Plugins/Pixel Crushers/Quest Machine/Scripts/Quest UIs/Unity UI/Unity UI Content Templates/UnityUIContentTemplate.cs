// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Abstract base class for Unity UI content templates.
    /// </summary>
    public abstract class UnityUIContentTemplate : MonoBehaviour
    {

        public UnityUIContentTemplate originalPrefab { get; set; }

        /// <summary>
        /// Content that this UI object represents so UIs can reuse the same content
        /// template when refreshing UIs.
        /// </summary>
        public object contentReference { get; set; }

        /// <summary>
        /// Additional info about content to differentiate UI objects that represent
        /// different aspects of the same content (e.g., track button vs abandon button).
        /// </summary>
        public string contentTag { get; set; }

        /// <summary>
        /// Marks this UI object as no longer needing to be used. However, during UI 
        /// refresh it may be unretired. The UI object stays active while retired to
        /// retain its EventSystem selection state.
        /// </summary>
        public virtual void Retire()
        {
        }

        /// <summary>
        /// Deactivates this UI object. The inactive object is still kept in a pool
        /// for possible later reactivation and reuse.
        /// </summary>
        public virtual void Despawn()
        {
            if (gameObject == null) return;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Reactivates this UI object.
        /// </summary>
        public virtual void Respawn()
        {
            if (gameObject == null) return;
            gameObject.SetActive(true);
        }

    }
}

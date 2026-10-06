// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System.Collections.Generic;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Manages Unity UI content that has been instantiated from templates.
    /// </summary>
    public class UnityUIInstancedContentManager
    {

        protected List<UnityUIContentTemplate> instances = new List<UnityUIContentTemplate>();

        public List<UnityUIContentTemplate> instancedContent { get { return instances; } }

        protected List<UnityUIContentTemplate> retiredInstances = new List<UnityUIContentTemplate>();

        protected List<UnityUIContentTemplate> despawnedInstances = new List<UnityUIContentTemplate>();

        public void AddToContainer(UnityUIContentTemplate instance, RectTransform container)
        {
            if (container == null)
            {
                Debug.LogError("Quest Machine: Container isn't assigned to hold instance of UI template.", instance);
                return;
            }
            instance.transform.SetParent(container, false);
        }

        public void Remove(UnityUIContentTemplate instance)
        {
            instances.Remove(instance);
            instance.Despawn();
            despawnedInstances.Add(instance);
        }

        public UnityUIContentTemplate GetLastAdded()
        {
            return (instances.Count > 0) ? instances[instances.Count - 1] : null;
        }

        /// <summary>
        /// Moves all instanced content to the retiredInstances list.
        /// </summary>
        public void RetireInstances()
        {
            instances.ForEach(instance => instance.Retire());
            retiredInstances.Clear();
            retiredInstances.AddRange(instances);
            instances.Clear();
        }

        /// <summary>
        /// Moves all retired instances to despawned list, adding to existing despawned list.
        /// </summary>
        public void DespawnRetiredInstances()
        {
            retiredInstances.ForEach(instance => { if (instance != null) instance.Despawn(); });
            despawnedInstances.AddRange(retiredInstances);
            retiredInstances.Clear();
        }

        public T AcquireContentInstance<T>(T template, object contentReference, string contentTag) where T : UnityUIContentTemplate
        {
            // Look for a retired instance that originally held the specified content:
            var retiredInstance = retiredInstances.Find(x =>
                x is T &&
                x.originalPrefab == template &&
                x.contentReference == contentReference &&
                x.contentTag == contentTag);
            if (retiredInstance != null)
            { // Retired instances are still active, so no need to set active.
                retiredInstances.Remove(retiredInstance);
                instances.Add(retiredInstance);
                retiredInstance.transform.SetAsLastSibling();
                return retiredInstance as T;
            }

            // Failing that, look for a despawned instance of the right type:
            var despawnedInstance = despawnedInstances.Find(x =>
                x is T &&
                x.originalPrefab == template);
            if (despawnedInstance != null)
            {
                despawnedInstances.Remove(despawnedInstance);
                instances.Add(despawnedInstance);
                despawnedInstance.Respawn();
                despawnedInstance.transform.SetAsLastSibling();
                despawnedInstance.contentReference = contentReference;
                despawnedInstance.contentTag = contentTag;
                return despawnedInstance as T;
            }

            // Otherwise create a new instance:
            var newInstance = GameObject.Instantiate<T>(template);
            instances.Add(newInstance);
            newInstance.gameObject.SetActive(true);
            newInstance.originalPrefab = template;
            newInstance.contentReference = contentReference;
            newInstance.contentTag = contentTag;
            return newInstance;
        }

    }
}

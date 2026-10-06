// Copyright (c) Pixel Crushers. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers.QuestMachine
{

    [Serializable]
    public class QuestMachineSceneEvent
    {
        public string guid = string.Empty;
        public UnityEvent onExecute = new UnityEvent();
    }

    /// <summary>
    /// Holds scene-specific UnityEvents referenced by a dialogue database's dialogue entries.
    /// </summary>
    [AddComponentMenu("")]
    public class QuestMachineSceneEvents : MonoBehaviour
    {
        [HelpBox("Do not remove this GameObject. It contains UnityEvents referenced by Quest Machine quest actions. This GameObject should not be a child of the Dialogue Manager or marked as Don't Destroy On Load.", HelpBoxMessageType.Info)]
        public List<QuestMachineSceneEvent> sceneEvents = new List<QuestMachineSceneEvent>();

        /// <summary>
        /// Runtime list of QuestMachineSceneEvents in all loaded scenes.
        /// </summary>
        private static List<QuestMachineSceneEvents> m_sceneInstances = new List<QuestMachineSceneEvents>();

#if UNITY_2019_3_OR_NEWER && UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStaticVariables()
        {
            m_sceneInstances = new List<QuestMachineSceneEvents>();
        }
#endif

        private void Awake()
        {
            RegisterInstance();
        }

        private void Start()
        {
            RegisterInstance();
        }

        private void OnDestroy()
        {
            m_sceneInstances.Remove(this);
        }

        private void RegisterInstance()
        {
            if (!m_sceneInstances.Contains(this))
            {
                m_sceneInstances.Add(this);
            }
        }

        public static int AddNewSceneEvent(out string guid, QuestMachineSceneEvents sceneInstanceToUse = null)
        {
            guid = string.Empty;
            if (Application.isPlaying) return -1;
            if (sceneInstanceToUse == null) sceneInstanceToUse = PixelCrushers.GameObjectUtility.FindFirstObjectByType<QuestMachineSceneEvents>();
            if (sceneInstanceToUse == null) return -1;
            guid = Guid.NewGuid().ToString();
            var x = new QuestMachineSceneEvent();
            x.guid = guid;
            sceneInstanceToUse.sceneEvents.Add(x);
            return sceneInstanceToUse.sceneEvents.Count - 1;
        }

        public static void RemoveSceneEvent(string guid, QuestMachineSceneEvents sceneInstanceToUse = null)
        {
            if (Application.isPlaying) return;
            if (sceneInstanceToUse != null)
            {
                sceneInstanceToUse.sceneEvents.RemoveAll(x => x.guid == guid);
            }
            else
            {
                foreach (var instance in PixelCrushers.GameObjectUtility.FindObjectsByType<QuestMachineSceneEvents>())
                {
                    instance.sceneEvents.RemoveAll(x => x.guid == guid);
                }
            }
        }

        public static QuestMachineSceneEvent GetSceneEvent(string guid)
        {
            if (!Application.isPlaying) return null;
            foreach (var sceneInstance in m_sceneInstances)
            {
                if (sceneInstance == null || sceneInstance.sceneEvents == null) continue;
                var result = sceneInstance.sceneEvents.Find(x => x.guid == guid);
                if (result != null) return result;
            }
            return null;
        }

        public static int GetSceneEventIndex(string guid)
        {
            if (!Application.isPlaying) return -1;
            foreach (var sceneInstance in m_sceneInstances)
            {
                if (sceneInstance == null || sceneInstance.sceneEvents == null) continue;
                var result = sceneInstance.sceneEvents.FindIndex(x => x.guid == guid);
                if (result != -1) return result;
            }
            return -1;
        }

        public static int GetSceneEventIndex(string guid, QuestMachineSceneEvents instance)
        {
            if (instance == null) return -1;
            return instance.sceneEvents.FindIndex(x => x.guid == guid);
        }

    }
}

// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Instantiates a prefab.
    /// </summary>
    public class InstantiatePrefabQuestAction : QuestAction
    {

        [Tooltip("Prefab to instantiate.")]
        [SerializeField]
        private GameObject m_prefab;

        [Tooltip("Name of GameObject (usually an empty GameObject) where prefab should be instantiated.")]
        [SerializeField]
        private StringField m_locationTransform;

        [Tooltip("Remove '(Clone)' from end of instantiated object's name.")]
        [SerializeField]
        private bool m_useOriginalName;

        /// <summary>
        /// Prefab to instantiate.
        /// </summary>
        public GameObject prefab
        {
            get { return m_prefab; }
            set { m_prefab = value; }
        }

        /// <summary>
        /// Name of GameObject (usually an empty GameObject) where prefab should be instantiated.
        /// </summary>
        public StringField locationTransform
        {
            get { return m_locationTransform; }
            set { m_locationTransform = value; }
        }

        /// <summary>
        /// Remove '(Clone)' from end of instantiated object's name.
        /// </summary>
        public bool useOriginalName
        {
            get { return m_useOriginalName; }
            set { m_useOriginalName = value; }
        }

        public override string GetEditorName()
        {
            if (prefab == null) return "Instantiate";
            if (locationTransform == null) return "Instantiate: " + prefab.name;
            return "Instantiate: " + prefab.name + " at " + locationTransform;
        }

        public override void Execute()
        {
            if (prefab == null) return;
            GameObject instance = null;
            var location = StringField.IsNullOrEmpty(locationTransform) ? null : GameObjectUtility.GameObjectHardFind(StringField.GetStringValue(locationTransform));
            if (location == null)
            {
                if (QuestMachine.debug) Debug.Log("Quest Machine: Instantiating prefab '" + prefab + "'.", prefab);
                instance = Instantiate(prefab);
            }
            else
            {
                if (QuestMachine.debug) Debug.Log("Quest Machine: Instantiating prefab '" + prefab + "' at " + locationTransform + ".", prefab);
                instance = Instantiate(prefab, location.transform.position, location.transform.rotation);
            }
            if (instance != null && useOriginalName)
            {
                var index = instance.name.LastIndexOf("(Clone)");
                if (index != -1)
                {
                    instance.name = instance.name.Substring(0, index).TrimEnd();
                }
            }
        }

    }

}

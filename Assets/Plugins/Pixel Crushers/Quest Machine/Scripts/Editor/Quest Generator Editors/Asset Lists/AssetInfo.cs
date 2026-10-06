using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace PixelCrushers.QuestMachine
{

    [Serializable]
    public class AssetInfo
    {
        public string path;
        public string name;
        public UnityEngine.Object asset;

//        // Unity 6.4 deprecates instance IDs in favor of entity IDs. We need to convert them.
//        // We'll need to remove this in a later Quest Machine version when it's actually removed.
//        public int instanceID;

//        [SerializeField] private EntityIdWrapper m_entityId = EntityIdWrapper.None;
//        public EntityIdWrapper entityId
//        {
//            get
//            {
//#if UNITY_6000_3_OR_NEWER
//                if (!m_entityId.isValid) UpdateEntityId();
//                return m_entityId; 
//#else
//                return new EntityIdWrapper(instanceID);
//#endif
//            }
//        }

        public const string AssetsPath = "Assets/";

        public string assetPath { get { return AssetsPath + path + ((string.IsNullOrEmpty(path)) ? string.Empty : "/") + name + ".asset"; } }

        public string pathAndName { get { return path + ((string.IsNullOrEmpty(path)) ? string.Empty : "/") + name; } }

        public AssetInfo() { }

        public AssetInfo(ScriptableObject asset)
        {
            this.asset = asset;
            var assetPath = AssetDatabase.GetAssetPath(asset);
            path = Path.GetDirectoryName(assetPath);
            path = (path.Length > AssetsPath.Length) ? path.Substring(AssetsPath.Length) : string.Empty;
            name = Path.GetFileNameWithoutExtension(assetPath);
            //--- We now use entity ID: instanceID = asset.GetInstanceID();
            //m_entityId = EntityUtility.GetEntityId(asset);
        }

//        /// <summary>
//        /// Utility method to update entity ID, used by menu item to handle conversion.
//        /// Only valid in Unity 6.3+. In older versions, keep using instance ID.
//        /// </summary>
//        public void UpdateEntityId()
//        {
//#if UNITY_6000_3_OR_NEWER
//            if (instanceID == 0)
//            {
//                m_entityId = EntityIdWrapper.None;
//            }
//            else
//            {
//                var obj = EditorUtility.InstanceIDToObject(instanceID);
//                if (obj == null)
//                {
//                    m_entityId = EntityIdWrapper.None;
//                }
//                else
//                {
//                    m_entityId = EntityUtility.GetEntityId(obj);
//                }
//            }
//#endif
//        }

    }

}
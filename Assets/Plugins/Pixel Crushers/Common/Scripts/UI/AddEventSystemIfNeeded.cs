// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers
{

    public class AddEventSystemIfNeeded : MonoBehaviour
    {

        [SerializeField] private string m_messageIfAddingEventSystem = "";

        private void Start()
        {
            UIUtility.RequireEventSystem(m_messageIfAddingEventSystem);
        }

    }

}

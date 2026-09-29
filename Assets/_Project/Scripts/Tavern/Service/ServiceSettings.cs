using System;
using UnityEngine;

namespace Hearthdelve.Tavern.Service
{
    [Serializable]
    public struct ServiceSettings
    {
        [Min(10), Tooltip("Length of evening service in seconds (default 6 minutes).")]
        public float lengthSeconds;
        [Min(0), Tooltip("No new customers arrive in the last this-many seconds.")]
        public float lastOrdersSeconds;
        [Min(0.1f)] public float minArrivalGap;
        [Min(0.1f)] public float maxArrivalGap;
        [Min(1), Tooltip("Most customers inside at once (seated + queueing).")]
        public int maxCustomers;
        [Min(1), Tooltip("Dishes the menu can hold.")]
        public int maxMenuSize;

        public static ServiceSettings Default => new()
        {
            lengthSeconds = 360f,
            lastOrdersSeconds = 45f,
            minArrivalGap = 8f,
            maxArrivalGap = 16f,
            maxCustomers = 8,
            maxMenuSize = 3,
        };
    }
}

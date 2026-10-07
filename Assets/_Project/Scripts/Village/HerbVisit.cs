using System.Collections;
using Hearthdelve.Shared.Characters;
using UnityEngine;

namespace Hearthdelve.Village
{
    /// <summary>
    /// Kaloren's herbs changing hands (4h Checkpoint C, locked canon): when he walks up to Grim and Ogrin's cottage on a herb day,
    /// the herbs show over his head, then over Grim's (or at Ogrin's window), with a warm face after. Wordless and small; it
    /// happens whether or not the keeper is there to see it, and only when he walks up (a load finds him already there, done).
    /// The talk is in their conversations (Grim and Ogrin know it's a herb day).
    /// </summary>
    public sealed class HerbVisit : MonoBehaviour
    {
        public const string Activity = "herbs";

        [SerializeField] Sprite m_Herbs;
        [SerializeField] Sprite m_Thanks;
        [SerializeField, Min(0.1f)] float m_Beat = 1.6f;

        /// <summary>Herbs handed over today (tests); the visit itself is the schedule's.</summary>
        public int Handovers { get; private set; }

        public void Configure(Sprite herbs, Sprite thanks)
        {
            m_Herbs = herbs;
            m_Thanks = thanks;
        }

        void OnEnable() => Villager.Arrived += OnArrived;
        void OnDisable() => Villager.Arrived -= OnArrived;

        void OnArrived(Villager v)
        {
            if (v.CharacterId == CharacterIds.Kaloren && v.Activity == Activity) StartCoroutine(Handover(v));
        }

        IEnumerator Handover(Villager kaloren)
        {
            Handovers++;
            kaloren.ShowOverhead(m_Herbs, m_Beat);
            yield return new WaitForSeconds(m_Beat);
            // To Grim, if he's home; otherwise left at Ogrin's window.
            Villager grim = Villager.Find(CharacterIds.Grim);
            Villager ogrin = Villager.Find(CharacterIds.Ogrin);
            Villager to = grim != null && grim.Shown && !grim.Walking && Vector2.Distance(grim.transform.position, kaloren.transform.position) < 4f ? grim
                : ogrin != null && ogrin.Indoors ? ogrin : null;
            if (to == null) yield break;
            to.ShowOverhead(m_Herbs, m_Beat);
            yield return new WaitForSeconds(m_Beat);
            to.ShowOverhead(m_Thanks, m_Beat);
            if (ogrin != null && ogrin != to && ogrin.Shown) ogrin.ShowOverhead(m_Thanks, m_Beat);
        }
    }
}

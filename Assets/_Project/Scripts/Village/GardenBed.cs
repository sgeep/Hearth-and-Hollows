using System.Collections;
using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Garden;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Tavern.Scene;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Hearthdelve.Village
{
    /// <summary>
    /// One bed of the garden behind Tally Ho! (4h Checkpoint B), by its stable id. The rules and the state are the game's
    /// (<see cref="GardenRules"/>, <see cref="GameState.Garden"/>); this shows them and offers what can be done: an empty bed
    /// asks the UI which crop to plant, a growing one can be tended once a day, a ready one is harvested. Its plants, one per
    /// tile, follow the crop's stage; tended today, its soil is darker; each action plays Farm's animated icon over the bed
    /// while the keeper faces it.
    /// </summary>
    public sealed class GardenBed : MonoBehaviour
    {
        static readonly List<GardenBed> s_All = new();

        [SerializeField] string m_Id;
        [SerializeField] TavernInteractable m_Interactable;
        [SerializeField] SpriteRenderer[] m_Plants;
        [SerializeField, Tooltip("The beds' soil and this bed's cells on it (darkened when tended today).")] Tilemap m_Soil;
        [SerializeField] RectInt m_Cells;
        [SerializeField] Color m_TendedSoil = new(0.7f, 0.6f, 0.55f, 1f);
        [SerializeField] SpriteRenderer m_Action;
        [SerializeField] Sprite[] m_PlantFrames;
        [SerializeField] Sprite[] m_TendFrames;
        [SerializeField] Sprite[] m_HarvestFrames;
        [SerializeField, Min(0.02f)] float m_FrameSeconds = 0.12f;

        string m_Shown;
        Coroutine m_Playing;

        public static IReadOnlyList<GardenBed> All => s_All;
        public string Id => m_Id;
        public TavernInteractable Interactable => m_Interactable;
        /// <summary>The stage on show (tests).</summary>
        public BedStage ShownStage { get; private set; }
        public bool ShowsTended { get; private set; }
        /// <summary>An action icon is playing (tests).</summary>
        public bool IsActing => m_Action != null && m_Action.enabled;

        public static GardenBed Find(string id) => s_All.Find(b => b.m_Id == id);

        public void Configure(string id, TavernInteractable interactable, SpriteRenderer[] plants, Tilemap soil, RectInt cells, SpriteRenderer action,
            Sprite[] plant, Sprite[] tend, Sprite[] harvest)
        {
            m_Id = id;
            m_Interactable = interactable;
            m_Plants = plants;
            m_Soil = soil;
            m_Cells = cells;
            m_Action = action;
            m_PlantFrames = plant;
            m_TendFrames = tend;
            m_HarvestFrames = harvest;
        }

        static GameFlow Flow => GameFlow.Instance;
        static bool InPlay => Flow != null && Flow.InGame && Flow.State != null;
        BedState Bed => InPlay ? Flow.State.Garden.Bed(m_Id) : null;
        CropDefinition CropOf(BedState bed) => bed != null && !bed.IsEmpty && Flow.Database != null ? Flow.Database.Crop(bed.Crop) : null;

        void OnEnable()
        {
            s_All.Add(this);
            if (m_Interactable != null)
            {
                m_Interactable.Used += OnUsed;
                m_Interactable.Describe = Describe;
            }
            EventBus<CropPlanted>.Subscribe(OnPlanted);
            EventBus<CropTended>.Subscribe(OnTended);
            EventBus<CropHarvested>.Subscribe(OnHarvested);
            if (m_Action != null) m_Action.enabled = false;
            m_Shown = null;
        }

        void OnDisable()
        {
            s_All.Remove(this);
            if (m_Interactable != null)
            {
                m_Interactable.Used -= OnUsed;
                m_Interactable.Describe = null;
            }
            EventBus<CropPlanted>.Unsubscribe(OnPlanted);
            EventBus<CropTended>.Unsubscribe(OnTended);
            EventBus<CropHarvested>.Unsubscribe(OnHarvested);
        }

        void Update()
        {
            bool daytime = TavernDirector.Instance != null && TavernDirector.Instance.Phase == TavernPhase.Daytime;
            bool talking = StoryServices.Conversations != null && StoryServices.Conversations.IsTalking;
            bool usable = InPlay && daytime && !talking;
            if (m_Interactable != null && usable != m_Interactable.IsAvailable) m_Interactable.SetAvailable(usable);
            Refresh();
        }

        // ---------- what can be done ----------

        TavernHint Describe()
        {
            BedState bed = Bed;
            if (bed == null) return TavernHint.Use(GardenText.Plant);
            CropDefinition crop = CropOf(bed);
            VigorSettings costs = Flow.VigorSettings;
            Vigor vigor = Flow.State.Vigor;
            if (bed.IsEmpty)
                return vigor.CanAfford(costs.Cost(VigorActivity.PlantBed))
                    ? TavernHint.Use(GardenText.Plant)
                    : new TavernHint(TavernHintKind.Note, GardenText.TooTiredToPlant);
            if (GardenRules.IsReady(bed, crop)) return TavernHint.Use(GardenText.Harvest);
            if (GardenRules.CanTend(Flow.State.Garden, vigor, m_Id, crop, Flow.State.Day, costs) == GardenResult.Done) return TavernHint.Use(GardenText.Tend);
            return new TavernHint(TavernHintKind.Growing, crop != null ? crop.nameKey : null, count: GardenRules.DaysToReady(bed, crop));
        }

        void OnUsed(TavernInteractable _)
        {
            BedState bed = Bed;
            if (bed == null) return;
            CropDefinition crop = CropOf(bed);
            if (bed.IsEmpty)
            {
                if (Flow.State.Vigor.CanAfford(Flow.VigorSettings.Cost(VigorActivity.PlantBed))) EventBus<GardenPlantAsked>.Publish(new GardenPlantAsked(m_Id));
            }
            else if (GardenRules.IsReady(bed, crop)) Flow.HarvestBed(m_Id);
            else Flow.TendBed(m_Id);
        }

        // ---------- how it looks ----------

        void Refresh()
        {
            BedState bed = Bed;
            CropDefinition crop = CropOf(bed);
            BedStage stage = GardenRules.Stage(bed, crop);
            bool tended = bed != null && GardenRules.TendedToday(bed, Flow.State.Day) && !GardenRules.IsReady(bed, crop);
            string shown = $"{crop?.id}|{stage}|{tended}";
            if (shown == m_Shown) return;
            m_Shown = shown;
            ShownStage = stage;
            ShowsTended = tended;
            Sprite sprite = crop == null ? null : stage switch
            {
                BedStage.Seeds => crop.seeds,
                BedStage.Sprouting => crop.sprout,
                BedStage.Growing => crop.growing,
                BedStage.Ready => crop.ready,
                _ => null,
            };
            if (m_Plants != null)
                foreach (SpriteRenderer plant in m_Plants)
                    if (plant != null)
                    {
                        plant.sprite = sprite;
                        plant.enabled = sprite != null;
                    }
            if (m_Soil == null) return;
            Color soil = tended ? m_TendedSoil : Color.white;
            for (int x = m_Cells.xMin; x < m_Cells.xMax; x++)
            for (int y = m_Cells.yMin; y < m_Cells.yMax; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (!m_Soil.HasTile(cell)) continue;
                m_Soil.SetTileFlags(cell, TileFlags.None);
                m_Soil.SetColor(cell, soil);
            }
        }

        void OnPlanted(CropPlanted e)
        {
            if (e.BedId == m_Id) Act(m_PlantFrames);
        }

        void OnTended(CropTended e)
        {
            if (e.BedId == m_Id) Act(m_TendFrames);
        }

        void OnHarvested(CropHarvested e)
        {
            if (e.BedId == m_Id) Act(m_HarvestFrames);
        }

        /// <summary>The keeper turns to the bed while Farm's action icon plays over it (two loops of its four frames).</summary>
        void Act(Sprite[] frames)
        {
            Refresh();
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.TryGetComponent(out PlayerLook look))
                look.Hold((Vector2)transform.position - (Vector2)player.transform.position, frames != null ? frames.Length * 2 * m_FrameSeconds : 0.5f);
            if (m_Action == null || frames == null || frames.Length == 0 || !isActiveAndEnabled) return;
            if (m_Playing != null) StopCoroutine(m_Playing);
            m_Playing = StartCoroutine(Play(frames));
        }

        IEnumerator Play(Sprite[] frames)
        {
            m_Action.enabled = true;
            for (int loop = 0; loop < 2; loop++)
                foreach (Sprite frame in frames)
                {
                    m_Action.sprite = frame;
                    yield return new WaitForSecondsRealtime(m_FrameSeconds);
                }
            m_Action.enabled = false;
            m_Playing = null;
        }
    }
}

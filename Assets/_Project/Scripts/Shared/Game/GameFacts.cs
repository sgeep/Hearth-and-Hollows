using Hearthdelve.Core.Events;

namespace Hearthdelve.Shared.Game
{
    // Gameplay facts published for later reactions (4f Checkpoint C). 4g's Dialogue System and Quest Machine adapters will
    // listen to these at the EventBus boundary; nothing in gameplay waits for an answer. Stable ids only, never display text.

    /// <summary>A furnishing found in the Hollows came home and is owned now.</summary>
    public readonly struct CurioBroughtHome : IEvent
    {
        public readonly string FurnitureId;
        public CurioBroughtHome(string furnitureId) => FurnitureId = furnitureId;
    }

    /// <summary>A piece was placed in an area of the property (Decorate Mode, on putting it down).</summary>
    public readonly struct FurniturePlaced : IEvent
    {
        public readonly string FurnitureId;
        public readonly string AreaId;

        public FurniturePlaced(string furnitureId, string areaId)
        {
            FurnitureId = furnitureId;
            AreaId = areaId;
        }
    }

    /// <summary>A boss trophy went up on the property's walls for the first time (its homecoming).</summary>
    public readonly struct TrophyDisplayed : IEvent
    {
        public readonly string FurnitureId;
        public readonly string AreaId;

        public TrophyDisplayed(string furnitureId, string areaId)
        {
            FurnitureId = furnitureId;
            AreaId = areaId;
        }
    }

    /// <summary>Something was bought at the Brackenford market (by ingredient id).</summary>
    public readonly struct MarketPurchase : IEvent
    {
        public readonly string IngredientId;
        public readonly int Count;
        public readonly int Gold;

        public MarketPurchase(string ingredientId, int count, int gold)
        {
            IngredientId = ingredientId;
            Count = count;
            Gold = gold;
        }
    }

    /// <summary>A part was broken down at the Butcher Block (mise en place), by whom ("keeper" or a staff id), and how well.</summary>
    public readonly struct PartButchered : IEvent
    {
        public readonly string PartId;
        public readonly string CutId;
        public readonly int Cuts;
        public readonly float Score;
        public readonly string By;

        public PartButchered(string partId, string cutId, int cuts, float score, string by)
        {
            PartId = partId;
            CutId = cutId;
            Cuts = cuts;
            Score = score;
            By = by;
        }
    }

    /// <summary>A member of staff (stable id: "pip", "gunta") finished a dish or a pot, and how good it was (0–1).</summary>
    public readonly struct StaffWorkDone : IEvent
    {
        public readonly string StaffId;
        public readonly string RecipeId;
        public readonly float Quality;

        public StaffWorkDone(string staffId, string recipeId, float quality)
        {
            StaffId = staffId;
            RecipeId = recipeId;
            Quality = quality;
        }
    }
}

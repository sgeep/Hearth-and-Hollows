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

    /// <summary>Vigor was spent on strenuous surface work (4h Checkpoint B): what, how much, and how much is left today.</summary>
    public readonly struct VigorSpent : IEvent
    {
        public readonly Surface.VigorActivity Activity;
        public readonly int Amount;
        public readonly int Remaining;
        public VigorSpent(Surface.VigorActivity activity, int amount, int remaining)
        {
            Activity = activity;
            Amount = amount;
            Remaining = remaining;
        }
    }

    /// <summary>A garden bed was prepared and planted (4h Checkpoint B).</summary>
    public readonly struct CropPlanted : IEvent
    {
        public readonly string BedId;
        public readonly string CropId;
        public CropPlanted(string bedId, string cropId)
        {
            BedId = bedId;
            CropId = cropId;
        }
    }

    /// <summary>A growing bed was tended (once a day), and how many days it has been tended so far.</summary>
    public readonly struct CropTended : IEvent
    {
        public readonly string BedId;
        public readonly string CropId;
        public readonly int TendedDays;
        public CropTended(string bedId, string cropId, int tendedDays)
        {
            BedId = bedId;
            CropId = cropId;
            TendedDays = tendedDays;
        }
    }

    /// <summary>A bed was harvested: what came out (ingredient id), how many and how good, into the storeroom.</summary>
    public readonly struct CropHarvested : IEvent
    {
        public readonly string BedId;
        public readonly string CropId;
        public readonly string IngredientId;
        public readonly int Count;
        public readonly Ingredients.Quality Quality;
        public CropHarvested(string bedId, string cropId, string ingredientId, int count, Ingredients.Quality quality)
        {
            BedId = bedId;
            CropId = cropId;
            IngredientId = ingredientId;
            Count = count;
            Quality = quality;
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

    // ---------- 4f Checkpoint D: service, requests, bosses ----------

    /// <summary>
    /// A patron's order became a special request: they particularly want this dish tonight. <c>VisitId</c> tells this
    /// evening's patrons apart (it isn't saved); <c>PatronId</c> is their profile's stable id (a villager or a kind of Visitor).
    /// </summary>
    public readonly struct CustomerRequestIssued : IEvent
    {
        public readonly string PatronId;
        public readonly int VisitId;
        public readonly string RecipeId;

        public CustomerRequestIssued(string patronId, int visitId, string recipeId)
        {
            PatronId = patronId;
            VisitId = visitId;
            RecipeId = recipeId;
        }
    }

    /// <summary>A special request was met: the dish's quality (1 is a perfect Fine dish) and the thanks given.</summary>
    public readonly struct CustomerRequestCompleted : IEvent
    {
        public readonly string PatronId;
        public readonly int VisitId;
        public readonly string RecipeId;
        public readonly float Quality;
        public readonly int BonusGold;
        public readonly int BonusRenown;

        public CustomerRequestCompleted(string patronId, int visitId, string recipeId, float quality, int bonusGold, int bonusRenown)
        {
            PatronId = patronId;
            VisitId = visitId;
            RecipeId = recipeId;
            Quality = quality;
            BonusGold = bonusGold;
            BonusRenown = bonusRenown;
        }
    }

    /// <summary>A special request was missed. <c>Reason</c>: "walked_out", "sold_out" or "closing_time".</summary>
    public readonly struct CustomerRequestFailed : IEvent
    {
        public readonly string PatronId;
        public readonly int VisitId;
        public readonly string RecipeId;
        public readonly string Reason;

        public CustomerRequestFailed(string patronId, int visitId, string recipeId, string reason)
        {
            PatronId = patronId;
            VisitId = visitId;
            RecipeId = recipeId;
            Reason = reason;
        }
    }

    /// <summary>A patron paid for a dish during service.</summary>
    public readonly struct DishServed : IEvent
    {
        public readonly string RecipeId;
        public readonly string PatronId;
        public readonly float Quality;
        public readonly int Gold;
        public readonly int Tip;
        public readonly bool WasRequest;

        public DishServed(string recipeId, string patronId, float quality, int gold, int tip, bool wasRequest)
        {
            RecipeId = recipeId;
            PatronId = patronId;
            Quality = quality;
            Gold = gold;
            Tip = tip;
            WasRequest = wasRequest;
        }
    }

    /// <summary>An evening's service ended (the doors opened that night): what it came to.</summary>
    public readonly struct ServiceCompleted : IEvent
    {
        public readonly int Day;
        public readonly int DishesServed;
        public readonly int Gold;
        public readonly int Tips;
        public readonly int Renown;
        public readonly int Walkouts;
        public readonly int RequestsCompleted;
        public readonly int RequestsFailed;

        public ServiceCompleted(int day, int dishesServed, int gold, int tips, int renown, int walkouts, int requestsCompleted, int requestsFailed)
        {
            Day = day;
            DishesServed = dishesServed;
            Gold = gold;
            Tips = tips;
            Renown = renown;
            Walkouts = walkouts;
            RequestsCompleted = requestsCompleted;
            RequestsFailed = requestsFailed;
        }
    }

    /// <summary>
    /// A boss fell on a delve that's now over (every defeat, not only the first: <see cref="BossFirstCleared"/> is the
    /// first). <c>TimesDefeated</c> counts this one; <c>CameHome</c> is false if the delve then ended in death.
    /// </summary>
    public readonly struct BossDefeated : IEvent
    {
        public readonly string BossId;
        public readonly int TimesDefeated;
        public readonly bool CameHome;

        public BossDefeated(string bossId, int timesDefeated, bool cameHome)
        {
            BossId = bossId;
            TimesDefeated = timesDefeated;
            CameHome = cameHome;
        }
    }

    /// <summary>A part of the day began in the tavern (4g Checkpoint B): "Daytime", "Arrival", "Prep", "Service", "Results", "Night".</summary>
    public readonly struct TavernPhaseStarted : IEvent
    {
        public readonly string Phase;
        public TavernPhaseStarted(string phase) => Phase = phase;
    }

    /// <summary>The player chose to begin the evening (4h): the day, and the minute on the surface clock when they did.</summary>
    public readonly struct EveningPrepChosen : IEvent
    {
        public readonly int Day;
        public readonly int Minute;

        public EveningPrepChosen(int day, int minute)
        {
            Day = day;
            Minute = minute;
        }
    }

    /// <summary>A quest object was picked up in the Hollows (4g Checkpoint B).</summary>
    public readonly struct QuestObjectFound : IEvent
    {
        public readonly string ObjectId;
        public QuestObjectFound(string objectId) => ObjectId = objectId;
    }

    /// <summary>A quest object came home with an extraction: persistent quest progress.</summary>
    public readonly struct QuestObjectBroughtHome : IEvent
    {
        public readonly string ObjectId;
        public QuestObjectBroughtHome(string objectId) => ObjectId = objectId;
    }

    /// <summary>A quest object was lost with a death before extraction (it turns up again while the quest wants it).</summary>
    public readonly struct QuestObjectLost : IEvent
    {
        public readonly string ObjectId;
        public QuestObjectLost(string objectId) => ObjectId = objectId;
    }

    /// <summary>A quest object was handed over (its quest's part done).</summary>
    public readonly struct QuestObjectDelivered : IEvent
    {
        public readonly string ObjectId;
        public QuestObjectDelivered(string objectId) => ObjectId = objectId;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Data
{
    // --- Character Data Transfer Object (DTO) ---
    [Serializable]
    public class CharacterDTO
    {
        // --- Core Identity & MMO Basics ---
        public string CharacterId;
        public string CharacterName;
        public string Name;
        public string Race;
        public string Subrace;
        public string Class;
        public string Subclass;
        public int Level;
        public int Experience;
        public string Faction;
        public string Gender;
        public string Description;
        public string Biography;
        public string Category;
        public List<string> Tags;

        // --- Appearance & Cosmetics ---
        public Appearance Appearance;
        public Sprite Portrait;
        public List<CosmeticItem> Cosmetics;

        // --- Devion Games Specific ---
        public string PrefabName;
        public GameObject PrefabReference;
        public bool IsActive;
        public int Slot;

        // --- Progression & Advancement ---
        public Attributes Attributes;
        public Stats Stats;
        public List<Skill> Skills;
        public List<Ability> Abilities;
        public Mastery Mastery;                         // Mastery progression (spells, weapons, trades, etc)
        public Advancement Advancement;                 // AA/Alternate Advancement, Perks, etc.
        public List<Trait> RacialTraits;
        public List<Trait> ClassTraits;
        public List<Trait> InnateTraits;

        // --- Inventory, Equipment, Bags ---
        public List<Item> Inventory;
        public List<Equipment> Equipment;
        public List<Bag> Bags;

        // --- Questing & Tracking ---
        public List<QuestProgress> Quests;
        public List<Achievement> Achievements;

        // --- Tradeskills & Gathering ---
        public Tradeskills Tradeskills;
        public Gathering Gathering;

        // --- Social & World Info ---
        public List<string> Friends;
        public List<string> Guilds;
        public List<string> Titles;
        public string LastLocation;
        public DateTime CreatedAt;
        public DateTime LastLogin;
        public int Currency;
        public int Platinum;
        public int Gold;
        public int Silver;
        public int Copper;

        // --- Custom, Extensible ---
        public Dictionary<string, string> CustomProperties;
    }

    // --- Appearance & Cosmetics ---
    [Serializable]
    public class Appearance
    {
        public string HairStyle;
        public string HairColor;
        public string EyeColor;
        public string SkinTone;
        public string FacePreset;
        public string BodyType;
        public string Markings;
        public string Outfit;
        public string Voice;
        public List<string> Tattoos;
        public List<string> Accessories; // e.g., earrings, rings, etc.
    }

    [Serializable]
    public class CosmeticItem
    {
        public string CosmeticId;
        public string Name;
        public Sprite Icon;
        public string Slot; // "Mount", "Pet", "Title", etc.
    }

    // --- Attributes, Stats, Skills, Abilities ---
    [Serializable]
    public class Attributes
    {
        public int Strength;
        public int Dexterity;
        public int Constitution;
        public int Intelligence;
        public int Wisdom;
        public int Charisma;
        public int Luck;
        public int Agility;
        public int Spirit;
    }

    [Serializable]
    public class Stats
    {
        public int Health;
        public int Mana;
        public int Stamina;
        public int Endurance;
        public int Armor;
        public int AttackPower;
        public int SpellPower;
        public int Defense;
        public int CritChance;
        public int CritDamage;
        public int Haste;
        public int Dodge;
        public int Parry;
        public int Block;
        public int MovementSpeed;
        public int SwimSpeed;
        public int FlightSpeed;
    }

    [Serializable]
    public class Skill
    {
        public string SkillId;
        public string Name;
        public int Rank;
        public int Proficiency;
        public string Description;
        public SkillType Type; // e.g., Combat, Tradeskill, Language, etc.
    }

    public enum SkillType { Combat, Tradeskill, Language, Lore }

    [Serializable]
    public class Ability
    {
        public string AbilityId;
        public string Name;
        public string Description;
        public int Power;
        public float Cooldown;
        public string ElementType;
        public AbilityType Type;
        public Mastery Mastery;
        public List<string> RequiredWeapons; // e.g., "Sword", "Bow", etc.
    }

    public enum AbilityType { Spell, CombatArt, Passive, Racial, Class, Pet }

    [Serializable]
    public class Trait
    {
        public string TraitId;
        public string Name;
        public string Description;
        public TraitType Type;
        public int Rank;
    }

    public enum TraitType { Racial, Class, Innate, Legendary }

    // --- Mastery & Advancement ---
    [Serializable]
    public class Mastery
    {
        public List<MasteryTrack> Tracks; // Each spell, weapon, etc.
    }

    [Serializable]
    public class MasteryTrack
    {
        public string Name;          // e.g., "Sword Mastery"
        public int CurrentLevel;
        public int MaxLevel;
        public int Experience;
        public List<MasteryReward> Rewards;
    }

    [Serializable]
    public class MasteryReward
    {
        public string Name;
        public string Description;
        public Sprite Icon;
        public int UnlockLevel;
    }

    [Serializable]
    public class Advancement
    {
        public int PointsSpent;
        public int PointsAvailable;
        public List<AdvancementNode> Nodes;
    }

    [Serializable]
    public class AdvancementNode
    {
        public string NodeId;
        public string Name;
        public string Description;
        public int Rank;
        public int MaxRank;
        public Sprite Icon;
    }

    // --- Inventory, Equipment, Bags ---
    [Serializable]
    public class Item
    {
        public string ItemId;
        public string ItemName;
        public int Quantity;
        public string Rarity;
        public string Description;
        public string SlotType;
        public Sprite Icon;
        public bool IsEquipped;
        public int Durability;
        public int MaxDurability;
        public int ItemLevel;
        public List<Enchantment> Enchantments;
        public List<Socket> Sockets;
        public bool IsTradable;
        public bool IsQuestItem;
        public ItemType Type;
    }

    public enum ItemType { Weapon, Armor, Consumable, CraftingMaterial, Quest, Misc }

    [Serializable]
    public class Enchantment
    {
        public string Name;
        public string Effect;
        public int Power;
        public Sprite Icon;
    }

    [Serializable]
    public class Socket
    {
        public string SocketType; // "Gem", "Rune", etc.
        public string InsertedItemId;
        public Sprite Icon;
    }

    [Serializable]
    public class Equipment
    {
        public string Slot;
        public string ItemId;
        public string ItemName;
        public int ItemLevel;
        public string Rarity;
        public Sprite Icon;
        public List<string> Enchantments;
        public int Durability;
        public int MaxDurability;
        public bool IsBroken;
    }

    [Serializable]
    public class Bag
    {
        public string BagId;
        public string Name;
        public int Size;
        public List<Item> Contents;
        public int WeightReduction;
    }

    // --- Questing & Achievements ---
    [Serializable]
    public class QuestProgress
    {
        public string QuestId;
        public string Title;
        public string Description;
        public QuestState State;
        public List<QuestObjective> Objectives;
        public DateTime StartedAt;
        public DateTime? CompletedAt;
    }

    public enum QuestState { NotStarted, InProgress, Completed, Failed }

    [Serializable]
    public class QuestObjective
    {
        public string ObjectiveId;
        public string Description;
        public int Progress;
        public int ProgressMax;
        public bool IsOptional;
    }

    [Serializable]
    public class Achievement
    {
        public string AchievementId;
        public string Name;
        public string Description;
        public DateTime DateUnlocked;
        public Sprite Icon;
        public int Points;
        public List<string> Criteria;
    }

    // --- Tradeskills & Gathering ---
    [Serializable]
    public class Tradeskills
    {
        public List<Tradeskill> Skills;
        public List<Recipe> KnownRecipes;
    }

    [Serializable]
    public class Tradeskill
    {
        public string Name;            // E.g. Blacksmithing, Alchemy
        public int Level;
        public int Experience;
        public int MaxLevel;
        public List<MasteryTrack> Mastery;
        public List<Achievement> Achievements;
    }

    [Serializable]
    public class Recipe
    {
        public string RecipeId;
        public string Name;
        public List<Ingredient> Ingredients;
        public Item Result;
        public int SkillRequired;
        public bool IsDiscovered;
        public bool IsRepeatable;
    }

    [Serializable]
    public class Ingredient
    {
        public string ItemId;
        public int Quantity;
        public bool IsOptional;
    }

    [Serializable]
    public class Gathering
    {
        public GatheringSkill Mining;
        public GatheringSkill Foraging;
        public GatheringSkill Fishing;
        public GatheringSkill Lumberjacking;
        public GatheringSkill Skinning;
        // Add more as needed
    }

    [Serializable]
    public class GatheringSkill
    {
        public string Name;
        public int Level;
        public int Experience;
        public int MaxLevel;
        public List<MasteryTrack> Mastery;
    }
}
// ---- Data Contracts ----

[Serializable]
public class AuthResponse
{
    public string id;
    public string username;
    public string access_token;
    public string refresh_token;
    public int expires_in;
    public string token_type;
    public string message;
    public string error;
}



[Serializable]
public class InventoryDTO
{
    public string ui_data;    // JSON string (e.g., inventory slots, etc.)
    public string scene_data; // JSON string (e.g., dropped items, etc.)
    public int size;
    public List<InventoryItemDTO> items;
}

[Serializable]
public class InventoryItemDTO
{
    public string itemName;
    public int amount;
    public int slotIndex;
}

[Serializable]
public class QuestsDTO
{
    public string active_quests;    // JSON array/string
    public string completed_quests; // JSON array/string
    public string failed_quests;    // JSON array/string
}

[Serializable]
public class StatsDTO
{
    public string stats_json; // JSON string of stats
}


using UnityEngine;
using System;
using System.Collections.Generic;

namespace FreeFire.Inventory
{
    public enum ItemCategory
    {
        AssaultRifle,
        SMG,
        Shotgun,
        Sniper,
        Pistol,
        Ammo,
        HelmetLevel1,
        HelmetLevel2,
        HelmetLevel3,
        VestLevel1,
        VestLevel2,
        VestLevel3,
        Medkit,
        MiniMedkit,
        ShieldPotion,
        GlooWall,
        Grenade,
        Scope2x,
        Scope4x,
        ScopeAWM
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [Serializable]
    public struct LootItemDefinition
    {
        public string ItemId;
        public string DisplayName;
        public ItemCategory Category;
        public ItemRarity Rarity;
        public float SpawnWeight;
        public int MaxStackSize;
        public bool IsStackable;
    }

    public class LootTable : MonoBehaviour
    {
        public static LootTable Instance { get; private set; }

        private readonly List<LootItemDefinition> itemDefinitions = new List<LootItemDefinition>();
        private float totalWeight;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultLootTable();
        }

        private void InitializeDefaultLootTable()
        {
            AddItem("ar_m4a1", "M4A1", ItemCategory.AssaultRifle, ItemRarity.Common, 15f);
            AddItem("ar_ak47", "AK-47", ItemCategory.AssaultRifle, ItemRarity.Common, 14f);
            AddItem("ar_scar", "SCAR", ItemCategory.AssaultRifle, ItemRarity.Uncommon, 10f);
            AddItem("ar_famas", "FAMAS", ItemCategory.AssaultRifle, ItemRarity.Uncommon, 10f);
            AddItem("ar_groza", "Groza", ItemCategory.AssaultRifle, ItemRarity.Rare, 4f);
            AddItem("ar_parafal", "Parafal", ItemCategory.AssaultRifle, ItemRarity.Rare, 5f);

            AddItem("smg_mp40", "MP40", ItemCategory.SMG, ItemRarity.Common, 18f);
            AddItem("smg_ump", "UMP", ItemCategory.SMG, ItemRarity.Common, 16f);
            AddItem("smg_p90", "P90", ItemCategory.SMG, ItemRarity.Uncommon, 8f);
            AddItem("smg_vector", "Vector", ItemCategory.SMG, ItemRarity.Rare, 5f);

            AddItem("sg_m1014", "M1014", ItemCategory.Shotgun, ItemRarity.Common, 12f);
            AddItem("sg_spas12", "SPAS-12", ItemCategory.Shotgun, ItemRarity.Uncommon, 8f);
            AddItem("sg_m1887", "M1887", ItemCategory.Shotgun, ItemRarity.Rare, 4f);

            AddItem("sniper_kar98k", "Kar98k", ItemCategory.Sniper, ItemRarity.Uncommon, 6f);
            AddItem("sniper_awm", "AWM", ItemCategory.Sniper, ItemRarity.Legendary, 1.5f);
            AddItem("sniper_m82b", "M82B", ItemCategory.Sniper, ItemRarity.Epic, 2f);
            AddItem("sniper_svd", "SVD", ItemCategory.Sniper, ItemRarity.Uncommon, 5f);

            AddItem("pistol_usp", "USP", ItemCategory.Pistol, ItemRarity.Common, 20f);
            AddItem("pistol_deagle", "Desert Eagle", ItemCategory.Pistol, ItemRarity.Uncommon, 8f);

            AddItem("ammo_ar", "AR Ammo (x30)", ItemCategory.Ammo, ItemRarity.Common, 35f, 180, true);
            AddItem("ammo_smg", "SMG Ammo (x30)", ItemCategory.Ammo, ItemRarity.Common, 30f, 200, true);
            AddItem("ammo_sg", "SG Ammo (x8)", ItemCategory.Ammo, ItemRarity.Common, 25f, 40, true);
            AddItem("ammo_sniper", "Sniper Ammo (x5)", ItemCategory.Ammo, ItemRarity.Uncommon, 15f, 30, true);

            AddItem("helm_1", "Helmet Lvl 1", ItemCategory.HelmetLevel1, ItemRarity.Common, 18f);
            AddItem("helm_2", "Helmet Lvl 2", ItemCategory.HelmetLevel2, ItemRarity.Uncommon, 8f);
            AddItem("helm_3", "Helmet Lvl 3", ItemCategory.HelmetLevel3, ItemRarity.Rare, 3f);
            AddItem("vest_1", "Vest Lvl 1", ItemCategory.VestLevel1, ItemRarity.Common, 18f);
            AddItem("vest_2", "Vest Lvl 2", ItemCategory.VestLevel2, ItemRarity.Uncommon, 8f);
            AddItem("vest_3", "Vest Lvl 3", ItemCategory.VestLevel3, ItemRarity.Rare, 3f);

            AddItem("med_medkit", "Medkit", ItemCategory.Medkit, ItemRarity.Uncommon, 10f, 5, true);
            AddItem("med_mini", "Mini Medkit", ItemCategory.MiniMedkit, ItemRarity.Common, 20f, 10, true);
            AddItem("med_shield", "Shield Potion", ItemCategory.ShieldPotion, ItemRarity.Uncommon, 8f, 5, true);

            AddItem("util_gloo", "Gloo Wall", ItemCategory.GlooWall, ItemRarity.Common, 15f, 5, true);
            AddItem("util_grenade", "Grenade", ItemCategory.Grenade, ItemRarity.Common, 12f, 4, true);

            AddItem("scope_2x", "2x Scope", ItemCategory.Scope2x, ItemRarity.Common, 12f);
            AddItem("scope_4x", "4x Scope", ItemCategory.Scope4x, ItemRarity.Uncommon, 6f);
            AddItem("scope_awm", "AWM Scope", ItemCategory.ScopeAWM, ItemRarity.Rare, 2f);

            RecalculateTotalWeight();
        }

        private void AddItem(string id, string name, ItemCategory category,
            ItemRarity rarity, float weight, int maxStack = 1, bool stackable = false)
        {
            itemDefinitions.Add(new LootItemDefinition
            {
                ItemId = id,
                DisplayName = name,
                Category = category,
                Rarity = rarity,
                SpawnWeight = weight,
                MaxStackSize = maxStack,
                IsStackable = stackable
            });
        }

        private void RecalculateTotalWeight()
        {
            totalWeight = 0f;
            foreach (var item in itemDefinitions)
            {
                totalWeight += item.SpawnWeight;
            }
        }

        public LootItemDefinition GetRandomItem()
        {
            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var item in itemDefinitions)
            {
                cumulative += item.SpawnWeight;
                if (roll <= cumulative)
                {
                    return item;
                }
            }

            return itemDefinitions[itemDefinitions.Count - 1];
        }

        public LootItemDefinition GetRandomItemByCategory(ItemCategory category)
        {
            var filtered = itemDefinitions.FindAll(i => i.Category == category);
            if (filtered.Count == 0) return GetRandomItem();

            float filteredWeight = 0f;
            foreach (var item in filtered) filteredWeight += item.SpawnWeight;

            float roll = UnityEngine.Random.Range(0f, filteredWeight);
            float cumulative = 0f;

            foreach (var item in filtered)
            {
                cumulative += item.SpawnWeight;
                if (roll <= cumulative) return item;
            }

            return filtered[filtered.Count - 1];
        }

        public LootItemDefinition? GetItemById(string itemId)
        {
            foreach (var item in itemDefinitions)
            {
                if (item.ItemId == itemId) return item;
            }
            return null;
        }
    }
}

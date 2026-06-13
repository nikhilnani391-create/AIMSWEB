using UnityEngine;
using System;
using System.Collections.Generic;

namespace FreeFire.Inventory
{
    [Serializable]
    public struct InventorySlot
    {
        public string ItemId;
        public ItemCategory Category;
        public int Quantity;
        public int MaxStack;
        public bool IsEmpty;
    }

    public class PlayerInventory : MonoBehaviour
    {
        [Header("Inventory Configuration")]
        [SerializeField] private int maxWeaponSlots = 2;
        [SerializeField] private int maxConsumableSlots = 6;
        [SerializeField] private int maxAmmoTypes = 4;

        private InventorySlot[] weaponSlots;
        private InventorySlot[] consumableSlots;
        private readonly Dictionary<string, int> ammoStash = new Dictionary<string, int>();

        private InventorySlot equippedHelmet;
        private InventorySlot equippedVest;
        private int activeWeaponIndex;

        public int ActiveWeaponIndex => activeWeaponIndex;

        public event Action<int, InventorySlot> OnWeaponSlotChanged;
        public event Action<int, InventorySlot> OnConsumableSlotChanged;
        public event Action<string, int> OnAmmoChanged;
        public event Action<InventorySlot> OnHelmetChanged;
        public event Action<InventorySlot> OnVestChanged;

        private void Awake()
        {
            weaponSlots = new InventorySlot[maxWeaponSlots];
            consumableSlots = new InventorySlot[maxConsumableSlots];

            for (int i = 0; i < maxWeaponSlots; i++)
                weaponSlots[i] = CreateEmptySlot();
            for (int i = 0; i < maxConsumableSlots; i++)
                consumableSlots[i] = CreateEmptySlot();

            equippedHelmet = CreateEmptySlot();
            equippedVest = CreateEmptySlot();
        }

        public bool TryAddItem(LootItemDefinition item)
        {
            switch (item.Category)
            {
                case ItemCategory.AssaultRifle:
                case ItemCategory.SMG:
                case ItemCategory.Shotgun:
                case ItemCategory.Sniper:
                case ItemCategory.Pistol:
                    return TryAddWeapon(item);

                case ItemCategory.HelmetLevel1:
                case ItemCategory.HelmetLevel2:
                case ItemCategory.HelmetLevel3:
                    return TryEquipHelmet(item);

                case ItemCategory.VestLevel1:
                case ItemCategory.VestLevel2:
                case ItemCategory.VestLevel3:
                    return TryEquipVest(item);

                case ItemCategory.Ammo:
                    return TryAddAmmo(item);

                default:
                    return TryAddConsumable(item);
            }
        }

        private bool TryAddWeapon(LootItemDefinition item)
        {
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i].IsEmpty)
                {
                    weaponSlots[i] = new InventorySlot
                    {
                        ItemId = item.ItemId,
                        Category = item.Category,
                        Quantity = 1,
                        MaxStack = 1,
                        IsEmpty = false
                    };
                    OnWeaponSlotChanged?.Invoke(i, weaponSlots[i]);
                    return true;
                }
            }
            return false;
        }

        private bool TryAddConsumable(LootItemDefinition item)
        {
            for (int i = 0; i < consumableSlots.Length; i++)
            {
                if (!consumableSlots[i].IsEmpty &&
                    consumableSlots[i].ItemId == item.ItemId &&
                    consumableSlots[i].Quantity < item.MaxStackSize)
                {
                    var slot = consumableSlots[i];
                    slot.Quantity = Mathf.Min(slot.Quantity + 1, item.MaxStackSize);
                    consumableSlots[i] = slot;
                    OnConsumableSlotChanged?.Invoke(i, consumableSlots[i]);
                    return true;
                }
            }

            for (int i = 0; i < consumableSlots.Length; i++)
            {
                if (consumableSlots[i].IsEmpty)
                {
                    consumableSlots[i] = new InventorySlot
                    {
                        ItemId = item.ItemId,
                        Category = item.Category,
                        Quantity = 1,
                        MaxStack = item.MaxStackSize,
                        IsEmpty = false
                    };
                    OnConsumableSlotChanged?.Invoke(i, consumableSlots[i]);
                    return true;
                }
            }

            return false;
        }

        private bool TryAddAmmo(LootItemDefinition item)
        {
            string ammoKey = item.ItemId;
            if (!ammoStash.ContainsKey(ammoKey))
            {
                ammoStash[ammoKey] = 0;
            }

            if (ammoStash[ammoKey] >= item.MaxStackSize)
                return false;

            int addAmount = GetAmmoAddAmount(item);
            ammoStash[ammoKey] = Mathf.Min(ammoStash[ammoKey] + addAmount, item.MaxStackSize);
            OnAmmoChanged?.Invoke(ammoKey, ammoStash[ammoKey]);
            return true;
        }

        private int GetAmmoAddAmount(LootItemDefinition item)
        {
            switch (item.Category)
            {
                case ItemCategory.Ammo:
                    if (item.ItemId.Contains("sniper")) return 5;
                    if (item.ItemId.Contains("sg")) return 8;
                    return 30;
                default:
                    return 1;
            }
        }

        private bool TryEquipHelmet(LootItemDefinition item)
        {
            int newLevel = GetArmorLevel(item.Category);
            int currentLevel = equippedHelmet.IsEmpty ? 0 : GetArmorLevel(equippedHelmet.Category);

            if (newLevel <= currentLevel) return false;

            equippedHelmet = new InventorySlot
            {
                ItemId = item.ItemId,
                Category = item.Category,
                Quantity = 1,
                MaxStack = 1,
                IsEmpty = false
            };
            OnHelmetChanged?.Invoke(equippedHelmet);
            return true;
        }

        private bool TryEquipVest(LootItemDefinition item)
        {
            int newLevel = GetArmorLevel(item.Category);
            int currentLevel = equippedVest.IsEmpty ? 0 : GetArmorLevel(equippedVest.Category);

            if (newLevel <= currentLevel) return false;

            equippedVest = new InventorySlot
            {
                ItemId = item.ItemId,
                Category = item.Category,
                Quantity = 1,
                MaxStack = 1,
                IsEmpty = false
            };
            OnVestChanged?.Invoke(equippedVest);
            return true;
        }

        private static int GetArmorLevel(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.HelmetLevel1:
                case ItemCategory.VestLevel1:
                    return 1;
                case ItemCategory.HelmetLevel2:
                case ItemCategory.VestLevel2:
                    return 2;
                case ItemCategory.HelmetLevel3:
                case ItemCategory.VestLevel3:
                    return 3;
                default:
                    return 0;
            }
        }

        public void SwitchWeapon(int index)
        {
            if (index < 0 || index >= weaponSlots.Length) return;
            if (weaponSlots[index].IsEmpty) return;
            activeWeaponIndex = index;
        }

        public InventorySlot GetWeaponSlot(int index)
        {
            if (index < 0 || index >= weaponSlots.Length) return CreateEmptySlot();
            return weaponSlots[index];
        }

        public InventorySlot GetConsumableSlot(int index)
        {
            if (index < 0 || index >= consumableSlots.Length) return CreateEmptySlot();
            return consumableSlots[index];
        }

        public int GetAmmoCount(string ammoId)
        {
            return ammoStash.TryGetValue(ammoId, out int count) ? count : 0;
        }

        public bool ConsumeAmmo(string ammoId, int amount)
        {
            if (!ammoStash.ContainsKey(ammoId) || ammoStash[ammoId] < amount)
                return false;

            ammoStash[ammoId] -= amount;
            OnAmmoChanged?.Invoke(ammoId, ammoStash[ammoId]);
            return true;
        }

        public bool UseConsumable(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= consumableSlots.Length) return false;
            if (consumableSlots[slotIndex].IsEmpty) return false;

            var slot = consumableSlots[slotIndex];
            slot.Quantity--;
            if (slot.Quantity <= 0)
            {
                slot = CreateEmptySlot();
            }
            consumableSlots[slotIndex] = slot;
            OnConsumableSlotChanged?.Invoke(slotIndex, consumableSlots[slotIndex]);
            return true;
        }

        public InventorySlot DropWeapon(int index)
        {
            if (index < 0 || index >= weaponSlots.Length || weaponSlots[index].IsEmpty)
                return CreateEmptySlot();

            InventorySlot dropped = weaponSlots[index];
            weaponSlots[index] = CreateEmptySlot();
            OnWeaponSlotChanged?.Invoke(index, weaponSlots[index]);
            return dropped;
        }

        private static InventorySlot CreateEmptySlot()
        {
            return new InventorySlot
            {
                ItemId = string.Empty,
                Category = ItemCategory.Ammo,
                Quantity = 0,
                MaxStack = 0,
                IsEmpty = true
            };
        }

        public bool HasGlooWall()
        {
            for (int i = 0; i < consumableSlots.Length; i++)
            {
                if (!consumableSlots[i].IsEmpty && consumableSlots[i].Category == ItemCategory.GlooWall)
                    return true;
            }
            return false;
        }

        public bool ConsumeGlooWall()
        {
            for (int i = 0; i < consumableSlots.Length; i++)
            {
                if (!consumableSlots[i].IsEmpty && consumableSlots[i].Category == ItemCategory.GlooWall)
                {
                    return UseConsumable(i);
                }
            }
            return false;
        }
    }
}

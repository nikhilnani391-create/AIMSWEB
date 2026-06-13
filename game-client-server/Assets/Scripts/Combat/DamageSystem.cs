using UnityEngine;
using System;
using System.Collections.Generic;

namespace FreeFire.Combat
{
    public enum ArmorSlot
    {
        Helmet,
        Vest
    }

    [Serializable]
    public struct ArmorData
    {
        public ArmorSlot Slot;
        public int Level;
        public float DamageReduction;
        public float CurrentDurability;
        public float MaxDurability;
    }

    public class DamageSystem : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 200f;
        [SerializeField] private float maxShield = 100f;

        private float currentHealth;
        private float currentShield;

        private readonly Dictionary<ArmorSlot, ArmorData> equippedArmor =
            new Dictionary<ArmorSlot, ArmorData>();

        public float CurrentHealth => currentHealth;
        public float CurrentShield => currentShield;
        public float MaxHealth => maxHealth;
        public float MaxShield => maxShield;
        public bool IsDead => currentHealth <= 0;

        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnShieldChanged;
        public event Action<uint> OnDeath;
        public event Action<ArmorSlot> OnArmorBroken;

        private uint ownerId;

        public void Initialize(uint playerId)
        {
            ownerId = playerId;
            currentHealth = maxHealth;
            currentShield = 0f;
        }

        public float ApplyDamage(float rawDamage, bool isHeadshot, uint attackerId)
        {
            if (IsDead) return 0f;

            float damage = rawDamage;

            if (isHeadshot)
            {
                damage = ApplyArmorReduction(damage, ArmorSlot.Helmet);
            }

            damage = ApplyArmorReduction(damage, ArmorSlot.Vest);

            float shieldDamage = Mathf.Min(currentShield, damage);
            currentShield -= shieldDamage;
            damage -= shieldDamage;

            float healthDamage = Mathf.Min(currentHealth, damage);
            currentHealth -= healthDamage;

            float totalDamageApplied = shieldDamage + healthDamage;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            if (shieldDamage > 0)
            {
                OnShieldChanged?.Invoke(currentShield, maxShield);
            }

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                OnDeath?.Invoke(attackerId);
            }

            return totalDamageApplied;
        }

        private float ApplyArmorReduction(float damage, ArmorSlot slot)
        {
            if (!equippedArmor.ContainsKey(slot)) return damage;

            var armor = equippedArmor[slot];
            if (armor.CurrentDurability <= 0) return damage;

            float reducedDamage = damage * (1f - armor.DamageReduction);
            float absorbed = damage - reducedDamage;

            armor.CurrentDurability -= absorbed;
            if (armor.CurrentDurability <= 0)
            {
                armor.CurrentDurability = 0;
                OnArmorBroken?.Invoke(slot);
            }

            equippedArmor[slot] = armor;
            return reducedDamage;
        }

        public void EquipArmor(ArmorData armor)
        {
            equippedArmor[armor.Slot] = armor;
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void AddShield(float amount)
        {
            currentShield = Mathf.Min(currentShield + amount, maxShield);
            OnShieldChanged?.Invoke(currentShield, maxShield);
        }

        public void ApplyZoneDamage(float damagePerSecond, float deltaTime)
        {
            if (IsDead) return;

            float damage = damagePerSecond * deltaTime;
            currentHealth -= damage;

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                OnDeath?.Invoke(0);
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public static ArmorData CreateArmor(ArmorSlot slot, int level)
        {
            float reduction;
            float durability;

            switch (level)
            {
                case 1:
                    reduction = slot == ArmorSlot.Helmet ? 0.30f : 0.25f;
                    durability = slot == ArmorSlot.Helmet ? 80f : 100f;
                    break;
                case 2:
                    reduction = slot == ArmorSlot.Helmet ? 0.45f : 0.40f;
                    durability = slot == ArmorSlot.Helmet ? 150f : 170f;
                    break;
                case 3:
                    reduction = slot == ArmorSlot.Helmet ? 0.58f : 0.55f;
                    durability = slot == ArmorSlot.Helmet ? 230f : 250f;
                    break;
                default:
                    reduction = 0f;
                    durability = 0f;
                    break;
            }

            return new ArmorData
            {
                Slot = slot,
                Level = level,
                DamageReduction = reduction,
                CurrentDurability = durability,
                MaxDurability = durability
            };
        }
    }
}

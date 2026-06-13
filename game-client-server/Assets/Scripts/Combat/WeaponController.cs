using UnityEngine;
using System;

namespace FreeFire.Combat
{
    [Serializable]
    public struct WeaponStats
    {
        public string WeaponId;
        public string DisplayName;
        public WeaponType Type;
        public float BaseDamage;
        public float HeadshotMultiplier;
        public float FireRate;
        public int MagazineSize;
        public float ReloadTime;
        public float MaxRange;
        public float BaseSpreadAngle;
        public float MaxSpreadAngle;
        public float SpreadIncreasePerShot;
        public float SpreadRecoveryRate;
        public float RecoilVertical;
        public float RecoilHorizontal;
        public float RecoilRecoverySpeed;
        public float AdsZoomMultiplier;
    }

    public enum WeaponType
    {
        AssaultRifle,
        SMG,
        Shotgun,
        Sniper,
        Pistol,
        LMG
    }

    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponStats stats;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private Transform aimOrigin;

        private int currentAmmo;
        private float lastFireTime;
        private float currentSpread;
        private Vector2 currentRecoilOffset;
        private bool isReloading;
        private float reloadTimer;
        private bool isFiring;

        public int CurrentAmmo => currentAmmo;
        public bool IsReloading => isReloading;
        public float CurrentSpread => currentSpread;
        public WeaponStats Stats => stats;

        public event Action OnFire;
        public event Action OnReloadComplete;
        public event Action<float> OnSpreadChanged;

        private void Awake()
        {
            currentAmmo = stats.MagazineSize;
            currentSpread = stats.BaseSpreadAngle;
        }

        private void Update()
        {
            UpdateSpreadRecovery();
            UpdateRecoilRecovery();
            UpdateReload();
        }

        public FireCommand TryFire(bool isADS)
        {
            if (isReloading || currentAmmo <= 0) return default;

            float fireInterval = 1f / stats.FireRate;
            if (Time.time - lastFireTime < fireInterval) return default;

            lastFireTime = Time.time;
            currentAmmo--;
            isFiring = true;

            ApplyRecoil();
            ApplySpreadIncrease();

            Vector3 spreadOffset = CalculateSpreadOffset(isADS);
            Vector3 fireDirection = (aimOrigin.forward + spreadOffset).normalized;

            var cmd = new FireCommand
            {
                Timestamp = Time.time,
                Origin = muzzlePoint.position,
                Direction = fireDirection,
                WeaponId = stats.WeaponId,
                IsADS = isADS
            };

            OnFire?.Invoke();

            if (currentAmmo <= 0)
            {
                StartReload();
            }

            return cmd;
        }

        public void StartReload()
        {
            if (isReloading || currentAmmo >= stats.MagazineSize) return;

            isReloading = true;
            reloadTimer = stats.ReloadTime;
        }

        private void UpdateReload()
        {
            if (!isReloading) return;

            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0)
            {
                currentAmmo = stats.MagazineSize;
                isReloading = false;
                OnReloadComplete?.Invoke();
            }
        }

        private void ApplyRecoil()
        {
            currentRecoilOffset.y += stats.RecoilVertical;
            currentRecoilOffset.x += UnityEngine.Random.Range(
                -stats.RecoilHorizontal, stats.RecoilHorizontal);
        }

        private void UpdateRecoilRecovery()
        {
            if (currentRecoilOffset.sqrMagnitude > 0.001f)
            {
                currentRecoilOffset = Vector2.Lerp(
                    currentRecoilOffset, Vector2.zero,
                    stats.RecoilRecoverySpeed * Time.deltaTime);
            }
        }

        private void ApplySpreadIncrease()
        {
            currentSpread = Mathf.Min(
                currentSpread + stats.SpreadIncreasePerShot,
                stats.MaxSpreadAngle);
            OnSpreadChanged?.Invoke(currentSpread);
        }

        private void UpdateSpreadRecovery()
        {
            if (!isFiring && currentSpread > stats.BaseSpreadAngle)
            {
                currentSpread = Mathf.Max(
                    stats.BaseSpreadAngle,
                    currentSpread - stats.SpreadRecoveryRate * Time.deltaTime);
                OnSpreadChanged?.Invoke(currentSpread);
            }
            isFiring = false;
        }

        private Vector3 CalculateSpreadOffset(bool isADS)
        {
            float effectiveSpread = isADS ? currentSpread * 0.5f : currentSpread;
            float spreadRad = effectiveSpread * Mathf.Deg2Rad;
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spreadRad;
            return aimOrigin.right * randomCircle.x + aimOrigin.up * randomCircle.y;
        }

        public Vector2 GetRecoilOffset()
        {
            return currentRecoilOffset;
        }
    }

    public struct FireCommand
    {
        public float Timestamp;
        public Vector3 Origin;
        public Vector3 Direction;
        public string WeaponId;
        public bool IsADS;
    }
}

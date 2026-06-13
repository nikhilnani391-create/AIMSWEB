using UnityEngine;
using System;
using System.Collections.Generic;

namespace FreeFire.Systems
{
    [Serializable]
    public struct ZoneStage
    {
        public int StageNumber;
        public float DelaySec;
        public float ShrinkTimeSec;
        public float RadiusModifier;
        public float DamagePerSec;
    }

    public class SafeZoneController : MonoBehaviour
    {
        [Header("Zone Configuration")]
        [SerializeField] private float initialRadius = 2000f;
        [SerializeField] private Vector2 mapCenter = Vector2.zero;

        private readonly ZoneStage[] zoneStages = new ZoneStage[]
        {
            new ZoneStage { StageNumber = 1, DelaySec = 180f, ShrinkTimeSec = 60f, RadiusModifier = 0.50f, DamagePerSec = 1f },
            new ZoneStage { StageNumber = 2, DelaySec = 120f, ShrinkTimeSec = 45f, RadiusModifier = 0.25f, DamagePerSec = 3f },
            new ZoneStage { StageNumber = 3, DelaySec = 90f,  ShrinkTimeSec = 30f, RadiusModifier = 0.12f, DamagePerSec = 7f },
            new ZoneStage { StageNumber = 4, DelaySec = 60f,  ShrinkTimeSec = 30f, RadiusModifier = 0.00f, DamagePerSec = 15f },
        };

        private int currentStageIndex;
        private float stageTimer;
        private bool isShrinking;
        private float shrinkProgress;

        private Vector2 currentCenter;
        private float currentRadius;
        private Vector2 targetCenter;
        private float targetRadius;

        private Vector2 previousCenter;
        private float previousRadius;

        private bool isActive;

        public float CurrentRadius => currentRadius;
        public Vector2 CurrentCenter => currentCenter;
        public float CurrentDamagePerSec => isActive && currentStageIndex < zoneStages.Length
            ? zoneStages[currentStageIndex].DamagePerSec : 0f;
        public int CurrentStage => currentStageIndex + 1;
        public bool IsShrinking => isShrinking;

        public event Action<int, Vector2, float> OnZoneStageBegin;
        public event Action<int> OnZoneShrinkComplete;

        public void StartZoneSequence()
        {
            currentCenter = mapCenter;
            currentRadius = initialRadius;
            previousCenter = currentCenter;
            previousRadius = currentRadius;
            currentStageIndex = 0;
            stageTimer = 0f;
            isShrinking = false;
            isActive = true;

            CalculateNextTarget();
            OnZoneStageBegin?.Invoke(1, targetCenter, targetRadius);

            Debug.Log("[SafeZone] Zone sequence started.");
        }

        public void UpdateZone(float deltaTime)
        {
            if (!isActive || currentStageIndex >= zoneStages.Length) return;

            stageTimer += deltaTime;
            ZoneStage stage = zoneStages[currentStageIndex];

            if (!isShrinking)
            {
                if (stageTimer >= stage.DelaySec)
                {
                    isShrinking = true;
                    shrinkProgress = 0f;
                    stageTimer = 0f;
                    previousCenter = currentCenter;
                    previousRadius = currentRadius;

                    Debug.Log($"[SafeZone] Stage {stage.StageNumber} shrinking begins.");
                }
            }
            else
            {
                shrinkProgress += deltaTime / stage.ShrinkTimeSec;
                shrinkProgress = Mathf.Clamp01(shrinkProgress);

                float t = SmoothStep(shrinkProgress);
                currentRadius = Mathf.Lerp(previousRadius, targetRadius, t);
                currentCenter = Vector2.Lerp(previousCenter, targetCenter, t);

                if (shrinkProgress >= 1f)
                {
                    isShrinking = false;
                    stageTimer = 0f;
                    OnZoneShrinkComplete?.Invoke(stage.StageNumber);

                    currentStageIndex++;
                    if (currentStageIndex < zoneStages.Length)
                    {
                        CalculateNextTarget();
                        OnZoneStageBegin?.Invoke(
                            zoneStages[currentStageIndex].StageNumber,
                            targetCenter, targetRadius);
                    }
                    else
                    {
                        Debug.Log("[SafeZone] All zone stages complete.");
                    }
                }
            }
        }

        public bool IsPlayerInZone(Vector3 playerPosition)
        {
            Vector2 playerPos2D = new Vector2(playerPosition.x, playerPosition.z);
            float distance = Vector2.Distance(playerPos2D, currentCenter);
            return distance <= currentRadius;
        }

        public float GetDamageForPlayer(Vector3 playerPosition, float deltaTime)
        {
            if (!isActive || IsPlayerInZone(playerPosition)) return 0f;

            float damage = CurrentDamagePerSec * deltaTime;
            return damage;
        }

        private void CalculateNextTarget()
        {
            if (currentStageIndex >= zoneStages.Length) return;

            ZoneStage stage = zoneStages[currentStageIndex];
            targetRadius = initialRadius * stage.RadiusModifier;

            float maxOffset = currentRadius - targetRadius;
            if (maxOffset < 0) maxOffset = 0;

            float offsetX = UnityEngine.Random.Range(-maxOffset * 0.5f, maxOffset * 0.5f);
            float offsetZ = UnityEngine.Random.Range(-maxOffset * 0.5f, maxOffset * 0.5f);

            targetCenter = currentCenter + new Vector2(offsetX, offsetZ);

            Debug.Log($"[SafeZone] Next target: center=({targetCenter.x:F0}, {targetCenter.y:F0}), " +
                      $"radius={targetRadius:F0}m");
        }

        private static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}

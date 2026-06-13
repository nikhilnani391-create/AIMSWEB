using UnityEngine;
using System.Collections.Generic;

namespace FreeFire.Combat
{
    public class AimAssist : MonoBehaviour
    {
        [Header("Aim Assist Configuration")]
        [SerializeField] private float outerDetectionRadius = 2.0f;
        [SerializeField] private float innerStickyRadius = 0.8f;
        [SerializeField] private float assistStrength = 0.15f;
        [SerializeField] private float maxAssistAngle = 15f;
        [SerializeField] private float maxAssistDistance = 50f;

        [Header("Slowdown Settings")]
        [SerializeField] private float aimSlowdownFactor = 0.6f;
        [SerializeField] private bool enableSlowdown = true;

        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        private readonly List<Transform> potentialTargets = new List<Transform>();

        public Vector3 ApplyAimAssist(Vector3 aimDirection, bool isADS)
        {
            if (!isADS) return aimDirection;

            Transform bestTarget = null;
            float bestScore = float.MaxValue;
            Vector3 bestTargetCenter = Vector3.zero;

            foreach (var target in potentialTargets)
            {
                if (target == null) continue;

                Vector3 targetCenter = target.position + Vector3.up * 1.0f;
                Vector3 toTarget = targetCenter - cameraTransform.position;
                float distance = toTarget.magnitude;

                if (distance > maxAssistDistance) continue;

                float angle = Vector3.Angle(aimDirection, toTarget.normalized);
                if (angle > maxAssistAngle) continue;

                float screenSpaceDistance = Mathf.Tan(angle * Mathf.Deg2Rad) * distance;
                float normalizedScreenDist = screenSpaceDistance / outerDetectionRadius;

                if (normalizedScreenDist > 1f) continue;

                float score = normalizedScreenDist * 0.7f + (distance / maxAssistDistance) * 0.3f;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                    bestTargetCenter = targetCenter;
                }
            }

            if (bestTarget == null) return aimDirection;

            Vector3 toTargetDir = (bestTargetCenter - cameraTransform.position).normalized;
            float currentAngle = Vector3.Angle(aimDirection, toTargetDir);
            float normalizedAngle = currentAngle / maxAssistAngle;

            float effectiveStrength = assistStrength * (1f - normalizedAngle * 0.5f);
            Vector3 assistedDirection = Vector3.Slerp(aimDirection, toTargetDir, effectiveStrength);

            return assistedDirection.normalized;
        }

        public float GetAimSlowdownMultiplier(Vector3 aimDirection, bool isADS)
        {
            if (!isADS || !enableSlowdown) return 1f;

            foreach (var target in potentialTargets)
            {
                if (target == null) continue;

                Vector3 targetCenter = target.position + Vector3.up * 1.0f;
                Vector3 toTarget = (targetCenter - cameraTransform.position).normalized;
                float angle = Vector3.Angle(aimDirection, toTarget);

                if (angle < maxAssistAngle * 0.5f)
                {
                    float proximity = 1f - (angle / (maxAssistAngle * 0.5f));
                    return Mathf.Lerp(1f, aimSlowdownFactor, proximity);
                }
            }

            return 1f;
        }

        public void RegisterTarget(Transform target)
        {
            if (!potentialTargets.Contains(target))
            {
                potentialTargets.Add(target);
            }
        }

        public void UnregisterTarget(Transform target)
        {
            potentialTargets.Remove(target);
        }

        public void ClearTargets()
        {
            potentialTargets.Clear();
        }
    }
}

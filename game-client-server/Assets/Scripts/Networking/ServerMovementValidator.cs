using UnityEngine;
using System.Collections.Generic;

namespace FreeFire.Networking
{
    public class ServerMovementValidator : MonoBehaviour
    {
        private struct PlayerPositionHistory
        {
            public Vector3 Position;
            public float Timestamp;
        }

        private readonly Dictionary<uint, PlayerPositionHistory> lastVerifiedState =
            new Dictionary<uint, PlayerPositionHistory>();

        private const float MAX_WALKING_SPEED = 5.0f;
        private const float MAX_SPRINT_SPEED = 7.5f;
        private const float MAX_VERTICAL_SPEED = 20f;
        private const float BUFFER_TOLERANCE = 1.15f;
        private const float TELEPORT_THRESHOLD = 50f;

        private readonly Dictionary<uint, int> violationCounts = new Dictionary<uint, int>();
        private const int MAX_VIOLATIONS_BEFORE_KICK = 5;

        public enum ValidationResult
        {
            Valid,
            SpeedViolation,
            TeleportDetected,
            TimestampManipulation,
            FirstRegistration
        }

        public ValidationResult ValidateClientMovement(
            uint playerId, Vector3 requestedPosition, float clientTimestamp, bool isSprinting)
        {
            float currentTime = Time.time;

            if (!lastVerifiedState.ContainsKey(playerId))
            {
                lastVerifiedState[playerId] = new PlayerPositionHistory
                {
                    Position = requestedPosition,
                    Timestamp = currentTime
                };
                return ValidationResult.FirstRegistration;
            }

            PlayerPositionHistory history = lastVerifiedState[playerId];
            float deltaTime = currentTime - history.Timestamp;

            if (deltaTime <= 0.001f)
            {
                return ValidationResult.TimestampManipulation;
            }

            float distanceTravelled = Vector3.Distance(requestedPosition, history.Position);

            if (distanceTravelled > TELEPORT_THRESHOLD)
            {
                RecordViolation(playerId);
                Debug.LogWarning(
                    $"[AntiCheat] Teleport detected: Player {playerId} " +
                    $"moved {distanceTravelled:F1}m in {deltaTime:F3}s");
                return ValidationResult.TeleportDetected;
            }

            float maxSpeed = isSprinting ? MAX_SPRINT_SPEED : MAX_WALKING_SPEED;
            float maxExpectedDistance = maxSpeed * deltaTime * BUFFER_TOLERANCE;

            float verticalDelta = Mathf.Abs(requestedPosition.y - history.Position.y);
            float maxVerticalDistance = MAX_VERTICAL_SPEED * deltaTime * BUFFER_TOLERANCE;

            if (distanceTravelled > maxExpectedDistance || verticalDelta > maxVerticalDistance)
            {
                RecordViolation(playerId);
                Debug.LogWarning(
                    $"[AntiCheat] Speed violation: Player {playerId} " +
                    $"moved {distanceTravelled:F1}m in {deltaTime:F3}s " +
                    $"(max expected: {maxExpectedDistance:F1}m)");
                return ValidationResult.SpeedViolation;
            }

            ClearViolation(playerId);
            lastVerifiedState[playerId] = new PlayerPositionHistory
            {
                Position = requestedPosition,
                Timestamp = currentTime
            };

            return ValidationResult.Valid;
        }

        private void RecordViolation(uint playerId)
        {
            if (!violationCounts.ContainsKey(playerId))
                violationCounts[playerId] = 0;

            violationCounts[playerId]++;

            if (violationCounts[playerId] >= MAX_VIOLATIONS_BEFORE_KICK)
            {
                Debug.LogError(
                    $"[AntiCheat] Player {playerId} exceeded maximum violations. Flagged for kick.");
            }
        }

        private void ClearViolation(uint playerId)
        {
            if (violationCounts.ContainsKey(playerId) && violationCounts[playerId] > 0)
            {
                violationCounts[playerId]--;
            }
        }

        public bool ShouldKickPlayer(uint playerId)
        {
            return violationCounts.ContainsKey(playerId) &&
                   violationCounts[playerId] >= MAX_VIOLATIONS_BEFORE_KICK;
        }

        public void RemovePlayer(uint playerId)
        {
            lastVerifiedState.Remove(playerId);
            violationCounts.Remove(playerId);
        }
    }
}

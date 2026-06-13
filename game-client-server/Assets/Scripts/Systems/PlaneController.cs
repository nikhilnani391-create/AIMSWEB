using UnityEngine;
using System;
using System.Collections.Generic;

namespace FreeFire.Systems
{
    public class PlaneController : MonoBehaviour
    {
        [SerializeField] private float planeAltitude = 500f;
        [SerializeField] private float planeSpeed = 160f;

        private Vector3 startPosition;
        private Vector3 endPosition;
        private Vector3 flightDirection;
        private float flightDuration;
        private float flightProgress;
        private bool isFlying;

        private readonly HashSet<uint> ejectedPlayers = new HashSet<uint>();
        private readonly List<uint> passengersOnBoard = new List<uint>();

        public bool IsFlying => isFlying;
        public Vector3 CurrentPosition => transform.position;
        public float Progress => flightProgress;

        public event Action<uint, Vector3> OnPlayerEjected;

        public void Initialize(Vector3 start, Vector3 end, float duration)
        {
            startPosition = new Vector3(start.x, planeAltitude, start.z);
            endPosition = new Vector3(end.x, planeAltitude, end.z);
            flightDirection = (endPosition - startPosition).normalized;
            flightDuration = duration;
            flightProgress = 0f;
            isFlying = true;

            transform.position = startPosition;
            transform.LookAt(endPosition);

            Debug.Log($"[Plane] Flight path: {startPosition} -> {endPosition}");
        }

        public void AddPassenger(uint playerId)
        {
            if (!passengersOnBoard.Contains(playerId))
            {
                passengersOnBoard.Add(playerId);
            }
        }

        private void Update()
        {
            if (!isFlying) return;

            flightProgress += Time.deltaTime / flightDuration;
            flightProgress = Mathf.Clamp01(flightProgress);

            transform.position = Vector3.Lerp(startPosition, endPosition, flightProgress);

            if (flightProgress >= 1f)
            {
                ForceEjectAll();
                isFlying = false;
            }
        }

        public bool TryEjectPlayer(uint playerId)
        {
            if (!isFlying) return false;
            if (ejectedPlayers.Contains(playerId)) return false;

            ejectedPlayers.Add(playerId);
            passengersOnBoard.Remove(playerId);

            Vector3 ejectPosition = transform.position;
            OnPlayerEjected?.Invoke(playerId, ejectPosition);

            Debug.Log($"[Plane] Player {playerId} ejected at {ejectPosition}");
            return true;
        }

        public void ForceEjectAll()
        {
            var remaining = new List<uint>(passengersOnBoard);
            foreach (uint playerId in remaining)
            {
                TryEjectPlayer(playerId);
            }
        }
    }
}

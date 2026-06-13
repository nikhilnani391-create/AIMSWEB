using UnityEngine;
using System;
using System.Collections.Generic;

namespace FreeFire.Systems
{
    public enum MatchState
    {
        WaitingInLobby,
        PlaneFlyover,
        ActiveGameplay,
        MatchEnd
    }

    public class BattleRoyaleGameLoop : MonoBehaviour
    {
        public static BattleRoyaleGameLoop Instance { get; private set; }

        [Header("Match Configuration")]
        [SerializeField] private int requiredPlayers = 50;
        [SerializeField] private float lobbyCountdownSec = 30f;
        [SerializeField] private float planeFlightDurationSec = 25f;
        [SerializeField] private float matchEndDelaySec = 10f;

        [Header("Map Bounds")]
        [SerializeField] private Vector2 mapSize = new Vector2(4000f, 4000f);
        [SerializeField] private Vector2 mapCenter = Vector2.zero;

        public MatchState CurrentState { get; private set; }
        public float StateTimer { get; private set; }
        public int AlivePlayerCount => alivePlayers.Count;

        private readonly HashSet<uint> alivePlayers = new HashSet<uint>();
        private readonly List<uint> eliminationOrder = new List<uint>();
        private float lobbyTimer;
        private uint winnerId;

        public event Action<MatchState> OnStateChanged;
        public event Action<uint, int> OnPlayerEliminated;
        public event Action<uint> OnMatchWinner;

        private PlaneController planeController;
        private SafeZoneController safeZone;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            planeController = GetComponentInChildren<PlaneController>();
            safeZone = GetComponentInChildren<SafeZoneController>();
            TransitionToState(MatchState.WaitingInLobby);
        }

        private void Update()
        {
            StateTimer += Time.deltaTime;

            switch (CurrentState)
            {
                case MatchState.WaitingInLobby:
                    UpdateLobby();
                    break;
                case MatchState.PlaneFlyover:
                    UpdatePlaneFlyover();
                    break;
                case MatchState.ActiveGameplay:
                    UpdateActiveGameplay();
                    break;
                case MatchState.MatchEnd:
                    UpdateMatchEnd();
                    break;
            }
        }

        public void RegisterPlayer(uint playerId)
        {
            alivePlayers.Add(playerId);
        }

        public void EliminatePlayer(uint playerId, uint killerId)
        {
            if (!alivePlayers.Remove(playerId)) return;

            eliminationOrder.Add(playerId);
            int placement = alivePlayers.Count + 1;
            OnPlayerEliminated?.Invoke(playerId, placement);

            if (alivePlayers.Count <= 1 && CurrentState == MatchState.ActiveGameplay)
            {
                foreach (uint id in alivePlayers)
                {
                    winnerId = id;
                }
                TransitionToState(MatchState.MatchEnd);
            }
        }

        private void UpdateLobby()
        {
            if (alivePlayers.Count >= requiredPlayers)
            {
                lobbyTimer += Time.deltaTime;
                if (lobbyTimer >= lobbyCountdownSec)
                {
                    TransitionToState(MatchState.PlaneFlyover);
                }
            }
            else
            {
                lobbyTimer = 0f;
            }
        }

        private void UpdatePlaneFlyover()
        {
            if (StateTimer >= planeFlightDurationSec)
            {
                if (planeController != null)
                {
                    planeController.ForceEjectAll();
                }
                TransitionToState(MatchState.ActiveGameplay);
            }
        }

        private void UpdateActiveGameplay()
        {
            if (safeZone != null)
            {
                safeZone.UpdateZone(Time.deltaTime);
            }
        }

        private void UpdateMatchEnd()
        {
            if (StateTimer >= matchEndDelaySec)
            {
                Debug.Log("[GameLoop] Match complete. Cleaning up...");
            }
        }

        private void TransitionToState(MatchState newState)
        {
            Debug.Log($"[GameLoop] State transition: {CurrentState} -> {newState}");
            CurrentState = newState;
            StateTimer = 0f;

            switch (newState)
            {
                case MatchState.PlaneFlyover:
                    InitializePlaneFlyover();
                    break;
                case MatchState.ActiveGameplay:
                    safeZone?.StartZoneSequence();
                    break;
                case MatchState.MatchEnd:
                    OnMatchWinner?.Invoke(winnerId);
                    break;
            }

            OnStateChanged?.Invoke(newState);
        }

        private void InitializePlaneFlyover()
        {
            if (planeController == null) return;

            Vector3 startPoint = GetRandomMapEdgePoint();
            Vector3 endPoint = GetOppositeMapEdgePoint(startPoint);

            planeController.Initialize(startPoint, endPoint, planeFlightDurationSec);
        }

        private Vector3 GetRandomMapEdgePoint()
        {
            float halfX = mapSize.x / 2f;
            float halfZ = mapSize.y / 2f;
            int edge = UnityEngine.Random.Range(0, 4);

            switch (edge)
            {
                case 0: return new Vector3(
                    UnityEngine.Random.Range(-halfX, halfX), 500f, halfZ);
                case 1: return new Vector3(
                    UnityEngine.Random.Range(-halfX, halfX), 500f, -halfZ);
                case 2: return new Vector3(
                    -halfX, 500f, UnityEngine.Random.Range(-halfZ, halfZ));
                default: return new Vector3(
                    halfX, 500f, UnityEngine.Random.Range(-halfZ, halfZ));
            }
        }

        private Vector3 GetOppositeMapEdgePoint(Vector3 start)
        {
            return new Vector3(-start.x, start.y, -start.z);
        }

        public bool IsPlayerAlive(uint playerId)
        {
            return alivePlayers.Contains(playerId);
        }
    }
}

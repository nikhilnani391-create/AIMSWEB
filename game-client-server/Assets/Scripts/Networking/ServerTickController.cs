using UnityEngine;
using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace FreeFire.Networking
{
    public class ServerTickController : MonoBehaviour
    {
        public static ServerTickController Instance { get; private set; }

        [SerializeField] private int targetTickRate = 30;

        public int CurrentTick { get; private set; }
        public float TickDeltaTime => 1f / targetTickRate;
        public int TargetTickRate => targetTickRate;

        public event Action<int> OnTick;

        private float tickAccumulator;
        private readonly Stopwatch performanceTimer = new Stopwatch();

        private float avgTickDurationMs;
        private int ticksThisSecond;
        private float secondTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!NetworkManager.Instance || !NetworkManager.Instance.IsServer) return;

            tickAccumulator += Time.deltaTime;
            secondTimer += Time.deltaTime;

            int maxTicksPerFrame = 5;
            int ticksThisFrame = 0;

            while (tickAccumulator >= TickDeltaTime && ticksThisFrame < maxTicksPerFrame)
            {
                performanceTimer.Restart();

                CurrentTick++;
                OnTick?.Invoke(CurrentTick);

                performanceTimer.Stop();
                float tickMs = (float)performanceTimer.Elapsed.TotalMilliseconds;
                avgTickDurationMs = Mathf.Lerp(avgTickDurationMs, tickMs, 0.1f);

                tickAccumulator -= TickDeltaTime;
                ticksThisFrame++;
                ticksThisSecond++;
            }

            if (secondTimer >= 1f)
            {
                if (ticksThisSecond < targetTickRate - 2)
                {
                    Debug.LogWarning(
                        $"[ServerTick] Tick rate deficit: {ticksThisSecond}/{targetTickRate} " +
                        $"(avg tick: {avgTickDurationMs:F2}ms)");
                }
                ticksThisSecond = 0;
                secondTimer = 0f;
            }
        }

        public float GetServerTime()
        {
            return CurrentTick * TickDeltaTime;
        }

        public float GetAverageTickDurationMs()
        {
            return avgTickDurationMs;
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

namespace FreeFire.Networking
{
    public struct InterpolationSnapshot
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Timestamp;
        public byte AnimState;
    }

    public class InterpolationBuffer : MonoBehaviour
    {
        [SerializeField] private float interpolationDelay = 0.1f;
        [SerializeField] private int maxBufferSize = 20;
        [SerializeField] private float extrapolationLimit = 0.2f;

        private readonly LinkedList<InterpolationSnapshot> buffer =
            new LinkedList<InterpolationSnapshot>();

        private float renderTimestamp;

        public void AddSnapshot(InterpolationSnapshot snapshot)
        {
            if (buffer.Count > 0 && snapshot.Timestamp <= buffer.Last.Value.Timestamp)
                return;

            buffer.AddLast(snapshot);

            while (buffer.Count > maxBufferSize)
            {
                buffer.RemoveFirst();
            }
        }

        private void Update()
        {
            renderTimestamp = Time.time - interpolationDelay;

            if (buffer.Count < 2) return;

            var (before, after) = FindSurroundingSnapshots(renderTimestamp);
            if (!before.HasValue || !after.HasValue) return;

            float duration = after.Value.Timestamp - before.Value.Timestamp;
            if (duration <= 0) return;

            float t = (renderTimestamp - before.Value.Timestamp) / duration;
            t = Mathf.Clamp01(t);

            transform.position = Vector3.Lerp(before.Value.Position, after.Value.Position, t);
            transform.rotation = Quaternion.Slerp(before.Value.Rotation, after.Value.Rotation, t);

            PruneOldSnapshots();
        }

        private (InterpolationSnapshot? before, InterpolationSnapshot? after)
            FindSurroundingSnapshots(float time)
        {
            var node = buffer.First;

            while (node != null && node.Next != null)
            {
                if (node.Value.Timestamp <= time && node.Next.Value.Timestamp >= time)
                {
                    return (node.Value, node.Next.Value);
                }
                node = node.Next;
            }

            if (buffer.Count >= 2)
            {
                var last = buffer.Last;
                var secondLast = last.Previous;

                if (time > last.Value.Timestamp &&
                    time - last.Value.Timestamp < extrapolationLimit)
                {
                    float dt = time - last.Value.Timestamp;
                    float sampleDt = last.Value.Timestamp - secondLast.Value.Timestamp;

                    if (sampleDt > 0)
                    {
                        Vector3 velocity = (last.Value.Position - secondLast.Value.Position) / sampleDt;
                        var extrapolated = new InterpolationSnapshot
                        {
                            Position = last.Value.Position + velocity * dt,
                            Rotation = last.Value.Rotation,
                            Timestamp = time,
                            AnimState = last.Value.AnimState
                        };
                        return (last.Value, extrapolated);
                    }
                }
            }

            return (null, null);
        }

        private void PruneOldSnapshots()
        {
            float cutoff = renderTimestamp - 1f;
            while (buffer.Count > 2 && buffer.First.Value.Timestamp < cutoff)
            {
                buffer.RemoveFirst();
            }
        }

        public void Clear()
        {
            buffer.Clear();
        }
    }
}

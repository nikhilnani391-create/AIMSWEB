using UnityEngine;
using System.Collections.Generic;

namespace FreeFire.Combat
{
    public struct HitboxSnapshot
    {
        public uint PlayerId;
        public Vector3 Position;
        public Quaternion Rotation;
        public Bounds HeadBounds;
        public Bounds BodyBounds;
        public float Timestamp;
    }

    public struct HitResult
    {
        public bool DidHit;
        public uint HitPlayerId;
        public float Damage;
        public bool IsHeadshot;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
    }

    public class LagCompensation : MonoBehaviour
    {
        private const float MAX_HISTORY_DURATION = 0.4f;
        private const int MAX_SNAPSHOTS_PER_PLAYER = 40;
        private const float HEAD_HITBOX_RADIUS = 0.15f;
        private const float BODY_HITBOX_RADIUS = 0.3f;

        private readonly Dictionary<uint, LinkedList<HitboxSnapshot>> hitboxHistory =
            new Dictionary<uint, LinkedList<HitboxSnapshot>>();

        public void RecordSnapshot(uint playerId, Vector3 position, Quaternion rotation,
            Bounds headBounds, Bounds bodyBounds)
        {
            if (!hitboxHistory.ContainsKey(playerId))
            {
                hitboxHistory[playerId] = new LinkedList<HitboxSnapshot>();
            }

            var snapshot = new HitboxSnapshot
            {
                PlayerId = playerId,
                Position = position,
                Rotation = rotation,
                HeadBounds = headBounds,
                BodyBounds = bodyBounds,
                Timestamp = Time.time
            };

            var history = hitboxHistory[playerId];
            history.AddLast(snapshot);

            while (history.Count > MAX_SNAPSHOTS_PER_PLAYER)
            {
                history.RemoveFirst();
            }

            float cutoff = Time.time - MAX_HISTORY_DURATION;
            while (history.Count > 0 && history.First.Value.Timestamp < cutoff)
            {
                history.RemoveFirst();
            }
        }

        public HitResult ProcessFireCommand(
            uint shooterPlayerId,
            FireCommand fireCmd,
            float shooterBaseDamage,
            float headshotMultiplier)
        {
            float targetTimestamp = fireCmd.Timestamp;
            var result = new HitResult { DidHit = false };

            float closestHitDistance = float.MaxValue;

            foreach (var kvp in hitboxHistory)
            {
                uint targetPlayerId = kvp.Key;
                if (targetPlayerId == shooterPlayerId) continue;

                var interpolated = InterpolateSnapshot(kvp.Value, targetTimestamp);
                if (!interpolated.HasValue) continue;

                var snap = interpolated.Value;

                if (RaycastAgainstBounds(fireCmd.Origin, fireCmd.Direction,
                        snap.HeadBounds, out float headDist))
                {
                    if (headDist < closestHitDistance)
                    {
                        closestHitDistance = headDist;
                        result = new HitResult
                        {
                            DidHit = true,
                            HitPlayerId = targetPlayerId,
                            Damage = shooterBaseDamage * headshotMultiplier,
                            IsHeadshot = true,
                            HitPoint = fireCmd.Origin + fireCmd.Direction * headDist,
                            HitNormal = -fireCmd.Direction
                        };
                    }
                    continue;
                }

                if (RaycastAgainstBounds(fireCmd.Origin, fireCmd.Direction,
                        snap.BodyBounds, out float bodyDist))
                {
                    if (bodyDist < closestHitDistance)
                    {
                        closestHitDistance = bodyDist;
                        result = new HitResult
                        {
                            DidHit = true,
                            HitPlayerId = targetPlayerId,
                            Damage = shooterBaseDamage,
                            IsHeadshot = false,
                            HitPoint = fireCmd.Origin + fireCmd.Direction * bodyDist,
                            HitNormal = -fireCmd.Direction
                        };
                    }
                }
            }

            return result;
        }

        private HitboxSnapshot? InterpolateSnapshot(
            LinkedList<HitboxSnapshot> history, float targetTime)
        {
            if (history.Count == 0) return null;

            if (targetTime <= history.First.Value.Timestamp)
                return history.First.Value;
            if (targetTime >= history.Last.Value.Timestamp)
                return history.Last.Value;

            var node = history.First;
            while (node.Next != null)
            {
                if (node.Value.Timestamp <= targetTime && node.Next.Value.Timestamp >= targetTime)
                {
                    var a = node.Value;
                    var b = node.Next.Value;
                    float t = (targetTime - a.Timestamp) / (b.Timestamp - a.Timestamp);

                    return new HitboxSnapshot
                    {
                        PlayerId = a.PlayerId,
                        Position = Vector3.Lerp(a.Position, b.Position, t),
                        Rotation = Quaternion.Slerp(a.Rotation, b.Rotation, t),
                        HeadBounds = LerpBounds(a.HeadBounds, b.HeadBounds, t),
                        BodyBounds = LerpBounds(a.BodyBounds, b.BodyBounds, t),
                        Timestamp = targetTime
                    };
                }
                node = node.Next;
            }

            return history.Last.Value;
        }

        private static Bounds LerpBounds(Bounds a, Bounds b, float t)
        {
            return new Bounds(
                Vector3.Lerp(a.center, b.center, t),
                Vector3.Lerp(a.size, b.size, t));
        }

        private static bool RaycastAgainstBounds(
            Vector3 origin, Vector3 direction, Bounds bounds, out float distance)
        {
            var ray = new Ray(origin, direction);
            bool hit = bounds.IntersectRay(ray, out distance);
            return hit && distance >= 0;
        }

        public void RemovePlayer(uint playerId)
        {
            hitboxHistory.Remove(playerId);
        }

        public void ClearAll()
        {
            hitboxHistory.Clear();
        }
    }
}

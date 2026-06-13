using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace FreeFire.Networking
{
    public struct PlayerNetworkState
    {
        public uint PlayerId;
        public Vector3 Position;
        public Quaternion Rotation;
        public byte InputFlags;
        public float Health;
        public byte WeaponId;
        public byte AnimationState;
    }

    public static class StateSerializer
    {
        private const byte SNAPSHOT_HEADER = 0x10;
        private const int BYTES_PER_PLAYER = 34;

        public static byte[] SerializeWorldSnapshot(
            int tick, List<PlayerNetworkState> playerStates)
        {
            using (var stream = new MemoryStream(8 + playerStates.Count * BYTES_PER_PLAYER))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(SNAPSHOT_HEADER);
                writer.Write(tick);
                writer.Write((byte)playerStates.Count);

                foreach (var state in playerStates)
                {
                    writer.Write(state.PlayerId);

                    WriteCompressedVector3(writer, state.Position);
                    WriteCompressedQuaternion(writer, state.Rotation);

                    writer.Write(state.InputFlags);
                    writer.Write((byte)Mathf.Clamp(state.Health, 0, 255));
                    writer.Write(state.WeaponId);
                    writer.Write(state.AnimationState);
                }

                return stream.ToArray();
            }
        }

        public static (int tick, List<PlayerNetworkState> states) DeserializeWorldSnapshot(
            byte[] data)
        {
            var states = new List<PlayerNetworkState>();

            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                byte header = reader.ReadByte();
                if (header != SNAPSHOT_HEADER)
                    throw new InvalidDataException("Invalid snapshot header");

                int tick = reader.ReadInt32();
                int playerCount = reader.ReadByte();

                for (int i = 0; i < playerCount; i++)
                {
                    var state = new PlayerNetworkState
                    {
                        PlayerId = reader.ReadUInt32(),
                        Position = ReadCompressedVector3(reader),
                        Rotation = ReadCompressedQuaternion(reader),
                        InputFlags = reader.ReadByte(),
                        Health = reader.ReadByte(),
                        WeaponId = reader.ReadByte(),
                        AnimationState = reader.ReadByte()
                    };
                    states.Add(state);
                }

                return (tick, states);
            }
        }

        private static void WriteCompressedVector3(BinaryWriter writer, Vector3 v)
        {
            writer.Write(HalfPrecision.FloatToHalf(v.x));
            writer.Write(HalfPrecision.FloatToHalf(v.y));
            writer.Write(HalfPrecision.FloatToHalf(v.z));
        }

        private static Vector3 ReadCompressedVector3(BinaryReader reader)
        {
            return new Vector3(
                HalfPrecision.HalfToFloat(reader.ReadUInt16()),
                HalfPrecision.HalfToFloat(reader.ReadUInt16()),
                HalfPrecision.HalfToFloat(reader.ReadUInt16())
            );
        }

        private static void WriteCompressedQuaternion(BinaryWriter writer, Quaternion q)
        {
            int largest = 0;
            float largestValue = Mathf.Abs(q.x);
            float[] components = { q.x, q.y, q.z, q.w };

            for (int i = 1; i < 4; i++)
            {
                float abs = Mathf.Abs(components[i]);
                if (abs > largestValue)
                {
                    largest = i;
                    largestValue = abs;
                }
            }

            float sign = components[largest] >= 0 ? 1f : -1f;

            byte header = (byte)(largest & 0x03);
            writer.Write(header);

            int written = 0;
            for (int i = 0; i < 4; i++)
            {
                if (i == largest) continue;
                short compressed = (short)(components[i] * sign * 16384f);
                writer.Write(compressed);
                written++;
            }
        }

        private static Quaternion ReadCompressedQuaternion(BinaryReader reader)
        {
            byte header = reader.ReadByte();
            int largest = header & 0x03;

            float[] components = new float[4];
            float sumSquares = 0f;

            int readIndex = 0;
            for (int i = 0; i < 4; i++)
            {
                if (i == largest) continue;
                components[i] = reader.ReadInt16() / 16384f;
                sumSquares += components[i] * components[i];
                readIndex++;
            }

            components[largest] = Mathf.Sqrt(Mathf.Max(0f, 1f - sumSquares));

            return new Quaternion(components[0], components[1], components[2], components[3]);
        }
    }

    public static class HalfPrecision
    {
        public static ushort FloatToHalf(float value)
        {
            int fbits = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
            int sign = (fbits >> 16) & 0x8000;
            int val = ((fbits & 0x7FFFFFFF) >> 13) - (0x38000000 >> 13);

            if (val <= 0) return (ushort)sign;
            if (val >= 0x7C00) return (ushort)(sign | 0x7C00);

            return (ushort)(sign | val);
        }

        public static float HalfToFloat(ushort half)
        {
            int sign = (half >> 15) & 1;
            int exponent = (half >> 10) & 0x1F;
            int mantissa = half & 0x3FF;

            if (exponent == 0)
            {
                if (mantissa == 0) return sign == 0 ? 0f : -0f;
                float result = mantissa / 1024f * Mathf.Pow(2, -14);
                return sign == 0 ? result : -result;
            }

            if (exponent == 31)
            {
                return mantissa == 0
                    ? (sign == 0 ? float.PositiveInfinity : float.NegativeInfinity)
                    : float.NaN;
            }

            int fbits = (sign << 31) | ((exponent + 112) << 23) | (mantissa << 13);
            return BitConverter.ToSingle(BitConverter.GetBytes(fbits), 0);
        }
    }
}

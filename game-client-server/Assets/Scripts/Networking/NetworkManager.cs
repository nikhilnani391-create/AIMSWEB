using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace FreeFire.Networking
{
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting
    }

    public struct NetworkMessage
    {
        public byte ChannelId;
        public byte[] Data;
        public int Length;
        public IPEndPoint Sender;
        public float Timestamp;
    }

    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Server Configuration")]
        [SerializeField] private int listenPort = 7777;
        [SerializeField] private int maxConnections = 50;
        [SerializeField] private float connectionTimeoutSec = 30f;

        [Header("Network Settings")]
        [SerializeField] private int sendBufferSize = 65536;
        [SerializeField] private int receiveBufferSize = 65536;

        public bool IsServer { get; private set; }
        public bool IsClient { get; private set; }
        public ConnectionState State { get; private set; }

        private UdpClient udpSocket;
        private Thread receiveThread;
        private volatile bool isRunning;

        private readonly Queue<NetworkMessage> incomingMessages = new Queue<NetworkMessage>();
        private readonly object messageLock = new object();

        private readonly Dictionary<IPEndPoint, ClientConnection> connectedClients =
            new Dictionary<IPEndPoint, ClientConnection>();

        public event Action<IPEndPoint> OnClientConnected;
        public event Action<IPEndPoint> OnClientDisconnected;
        public event Action<NetworkMessage> OnMessageReceived;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartServer(int port = -1)
        {
            if (port > 0) listenPort = port;

            try
            {
                udpSocket = new UdpClient(listenPort);
                udpSocket.Client.SendBufferSize = sendBufferSize;
                udpSocket.Client.ReceiveBufferSize = receiveBufferSize;
                udpSocket.Client.SetSocketOption(
                    SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                IsServer = true;
                IsClient = false;
                State = ConnectionState.Connected;
                isRunning = true;

                receiveThread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Name = "NetworkReceiveThread"
                };
                receiveThread.Start();

                Debug.Log($"[NetworkManager] Server started on UDP port {listenPort}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Failed to start server: {ex.Message}");
                State = ConnectionState.Disconnected;
            }
        }

        public void StartClient(string serverAddress, int serverPort)
        {
            try
            {
                udpSocket = new UdpClient(0);
                udpSocket.Client.SendBufferSize = sendBufferSize;
                udpSocket.Client.ReceiveBufferSize = receiveBufferSize;

                var endpoint = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);

                IsServer = false;
                IsClient = true;
                State = ConnectionState.Connecting;
                isRunning = true;

                receiveThread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Name = "NetworkReceiveThread"
                };
                receiveThread.Start();

                byte[] connectPacket = PacketBuilder.BuildConnectionRequest();
                udpSocket.Send(connectPacket, connectPacket.Length, endpoint);

                Debug.Log($"[NetworkManager] Client connecting to {serverAddress}:{serverPort}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Failed to start client: {ex.Message}");
                State = ConnectionState.Disconnected;
            }
        }

        public void SendToClient(IPEndPoint target, byte[] data, int length)
        {
            if (!isRunning || udpSocket == null) return;

            try
            {
                udpSocket.Send(data, length, target);
            }
            catch (SocketException ex)
            {
                Debug.LogWarning($"[NetworkManager] Send failed to {target}: {ex.Message}");
            }
        }

        public void SendToServer(byte[] data, int length, IPEndPoint serverEndpoint)
        {
            if (!isRunning || udpSocket == null) return;

            try
            {
                udpSocket.Send(data, length, serverEndpoint);
            }
            catch (SocketException ex)
            {
                Debug.LogWarning($"[NetworkManager] Send to server failed: {ex.Message}");
            }
        }

        public void BroadcastToAll(byte[] data, int length)
        {
            if (!IsServer) return;

            lock (connectedClients)
            {
                foreach (var kvp in connectedClients)
                {
                    SendToClient(kvp.Key, data, length);
                }
            }
        }

        private void ReceiveLoop()
        {
            while (isRunning)
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpSocket.Receive(ref remoteEP);

                    if (data.Length == 0) continue;

                    var msg = new NetworkMessage
                    {
                        Data = data,
                        Length = data.Length,
                        Sender = remoteEP,
                        Timestamp = Time.time
                    };

                    if (IsServer)
                    {
                        HandleServerReceive(msg, remoteEP);
                    }

                    lock (messageLock)
                    {
                        incomingMessages.Enqueue(msg);
                    }
                }
                catch (SocketException)
                {
                    if (!isRunning) break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private void HandleServerReceive(NetworkMessage msg, IPEndPoint sender)
        {
            lock (connectedClients)
            {
                if (!connectedClients.ContainsKey(sender))
                {
                    if (connectedClients.Count >= maxConnections) return;

                    if (PacketBuilder.IsConnectionRequest(msg.Data))
                    {
                        var conn = new ClientConnection
                        {
                            Endpoint = sender,
                            ConnectedAt = Time.time,
                            LastPacketTime = Time.time
                        };
                        connectedClients[sender] = conn;

                        byte[] ack = PacketBuilder.BuildConnectionAccepted();
                        SendToClient(sender, ack, ack.Length);
                    }
                }
                else
                {
                    connectedClients[sender].LastPacketTime = Time.time;
                }
            }
        }

        private void Update()
        {
            ProcessIncomingMessages();

            if (IsServer)
            {
                CheckTimeouts();
            }
        }

        private void ProcessIncomingMessages()
        {
            lock (messageLock)
            {
                while (incomingMessages.Count > 0)
                {
                    var msg = incomingMessages.Dequeue();
                    OnMessageReceived?.Invoke(msg);
                }
            }
        }

        private void CheckTimeouts()
        {
            float currentTime = Time.time;
            var timedOut = new List<IPEndPoint>();

            lock (connectedClients)
            {
                foreach (var kvp in connectedClients)
                {
                    if (currentTime - kvp.Value.LastPacketTime > connectionTimeoutSec)
                    {
                        timedOut.Add(kvp.Key);
                    }
                }

                foreach (var ep in timedOut)
                {
                    connectedClients.Remove(ep);
                    OnClientDisconnected?.Invoke(ep);
                    Debug.Log($"[NetworkManager] Client timed out: {ep}");
                }
            }
        }

        public void Shutdown()
        {
            isRunning = false;
            State = ConnectionState.Disconnected;

            udpSocket?.Close();
            receiveThread?.Join(1000);

            lock (connectedClients)
            {
                connectedClients.Clear();
            }

            IsServer = false;
            IsClient = false;

            Debug.Log("[NetworkManager] Shutdown complete.");
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        public int GetConnectedClientCount()
        {
            lock (connectedClients)
            {
                return connectedClients.Count;
            }
        }
    }

    public class ClientConnection
    {
        public IPEndPoint Endpoint;
        public float ConnectedAt;
        public float LastPacketTime;
    }

    public static class PacketBuilder
    {
        private const byte CONNECT_REQUEST = 0x01;
        private const byte CONNECT_ACCEPTED = 0x02;

        public static byte[] BuildConnectionRequest()
        {
            return new byte[] { CONNECT_REQUEST, 0xFF, 0xFF };
        }

        public static byte[] BuildConnectionAccepted()
        {
            return new byte[] { CONNECT_ACCEPTED, 0xFF, 0xFF };
        }

        public static bool IsConnectionRequest(byte[] data)
        {
            return data.Length >= 1 && data[0] == CONNECT_REQUEST;
        }
    }
}

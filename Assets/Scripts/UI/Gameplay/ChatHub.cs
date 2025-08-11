using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct ChatMessage : INetworkSerializable
{
    public ulong SenderId;
    public FixedString64Bytes SenderName;
    public FixedString128Bytes Text;
    public double ServerTime;

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref SenderId);
        s.SerializeValue(ref SenderName);
        s.SerializeValue(ref Text);
        s.SerializeValue(ref ServerTime);
    }
}

public class ChatHub : NetworkBehaviour
{
    public static ChatHub Instance { get; private set; }

    public NetworkList<ChatMessage> Messages;

    private readonly Dictionary<ulong, double> _lastSent = new();
    private const int MaxLen = 240;

    private void Awake()
    {
        Messages = new NetworkList<ChatMessage>();
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && Messages.Count == 0)
            AddSystem("Welcome! Press T to open chat.");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendChatServerRpc(string text, ServerRpcParams rpc = default)
    {
        var senderId = rpc.Receive.SenderClientId;
        if (string.IsNullOrWhiteSpace(text)) return;

        text = text.Trim();
        if (text.Length > MaxLen) text = text.Substring(0, MaxLen);

        var now = NetworkManager.ServerTime.TimeAsFloat;
        if (_lastSent.TryGetValue(senderId, out var last) && now - last < 0.4f) return; // rate limit
        _lastSent[senderId] = now;

        var senderName = GetPlayerName(senderId);

        Messages.Add(new ChatMessage {
            SenderId = senderId,
            SenderName = senderName,
            Text = text,
            ServerTime = NetworkManager.ServerTime.Time
        });
    }

    public void AddSystem(string text)
    {
        if (!IsServer) return;
        Messages.Add(new ChatMessage {
            SenderId = ulong.MaxValue,
            SenderName = "System",
            Text = text,
            ServerTime = NetworkManager.ServerTime.Time
        });
    }

    private FixedString64Bytes GetPlayerName(ulong clientId)
    {
        foreach (var p in FindObjectsOfType<Player>())
            if (p.IsSpawned && p.OwnerClientId == clientId)
                return p.DisplayName.Value;
        return $"Player{clientId}";
    }
}

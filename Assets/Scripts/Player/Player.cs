using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;

public class Player : NetworkBehaviour
{
    // Quick access to the local player's Player component
    public static Player Local { get; private set; }

    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Name (Networked)")]
    public NetworkVariable<FixedString64Bytes> DisplayName = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    // Optional: notify listeners (e.g., UI) when the name changes
    public event Action<string> OnDisplayNameChanged;

    [SerializeField] private Nameplate nameplate;
    private const string PrefsKey = "player_name";

    public string NameString => DisplayName.Value.ToString();

    public override void OnNetworkSpawn()
    {
        if (IsOwner) Local = this;

        // Owner sets their own name once on spawn (from PlayerPrefs or fallback)
        if (IsOwner && string.IsNullOrEmpty(DisplayName.Value.ToString()))
        {
            var name = LoadNameOrFallback();
            SetMyName(name);
        }

        // Everyone keeps their UI in sync
        DisplayName.OnValueChanged += OnNameChanged;

        // Initialize immediately for late joiners / first frame
        if (nameplate != null && !string.IsNullOrEmpty(DisplayName.Value.ToString()))
            nameplate.SetText(DisplayName.Value.ToString());
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && Local == this) Local = null;
        DisplayName.OnValueChanged -= OnNameChanged;
    }

    private void OnNameChanged(FixedString64Bytes oldVal, FixedString64Bytes newVal)
    {
        var s = newVal.ToString();
        if (nameplate != null) nameplate.SetText(s);
        OnDisplayNameChanged?.Invoke(s);
    }

    private string LoadNameOrFallback()
    {
        if (PlayerPrefs.HasKey(PrefsKey))
        {
            var stored = PlayerPrefs.GetString(PrefsKey)?.Trim();
            if (!string.IsNullOrEmpty(stored))
                return stored;
        }

        // deterministic-ish fallback to reduce collisions
        if (NetworkManager.Singleton && NetworkManager.Singleton.LocalClientId != 0)
            return $"Player{NetworkManager.Singleton.LocalClientId:0000}";

        return $"Player{UnityEngine.Random.Range(1000, 9999)}";
    }

    private void SetMyName(string newName)
    {
        if (!IsOwner) return;

        if (string.IsNullOrWhiteSpace(newName))
            newName = $"Player{UnityEngine.Random.Range(1000, 9999)}";

        if (newName.Length > 60) newName = newName.Substring(0, 60);

        DisplayName.Value = newName;

        // Persist so next session starts with the same name
        PlayerPrefs.SetString(PrefsKey, newName);
        PlayerPrefs.Save();
    }

    // -------- Chat helpers (for UI) --------
    public void SendChat(string text)
    {
        if (!IsOwner) return;
        if (string.IsNullOrWhiteSpace(text)) return;
        if (ChatHub.Instance == null) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;

        ChatHub.Instance.SendChatServerRpc(text.Trim());
    }

    private void Update()
    {
        if (!IsOwner) return;

        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        Vector3 move = input * moveSpeed * Time.deltaTime;
        MoveServerRpc(move);

        // Example: let player press Enter to send chat from anywhere (optional)
        // if (Input.GetKeyDown(KeyCode.Return)) SendChat("Hello world!");
    }

    [ServerRpc]
    private void MoveServerRpc(Vector3 move, ServerRpcParams rpcParams = default)
    {
        transform.position += move;
    }
}

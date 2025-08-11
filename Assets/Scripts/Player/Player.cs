using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class Player : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Name (Networked)")]
    public NetworkVariable<FixedString64Bytes> DisplayName = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    [SerializeField] private Nameplate nameplate;
    private const string PrefsKey = "player_name";

    public override void OnNetworkSpawn()
    {
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
        DisplayName.OnValueChanged -= OnNameChanged;
    }

    private void OnNameChanged(FixedString64Bytes oldVal, FixedString64Bytes newVal)
    {
        if (nameplate != null)
            nameplate.SetText(newVal.ToString());
    }

    private string LoadNameOrFallback()
    {
        if (PlayerPrefs.HasKey(PrefsKey))
        {
            var stored = PlayerPrefs.GetString(PrefsKey)?.Trim();
            if (!string.IsNullOrEmpty(stored))
                return stored;
        }

        // No stored name: make a deterministic fallback so multiple clients don’t collide
        // (uses LocalClientId if available, else random)
        if (NetworkManager.Singleton && NetworkManager.Singleton.LocalClientId != 0)
            return $"Player{NetworkManager.Singleton.LocalClientId:0000}";

        return $"Player{Random.Range(1000, 9999)}";
    }

    private void SetMyName(string newName)
    {
        if (!IsOwner) return;

        if (string.IsNullOrWhiteSpace(newName))
            newName = $"Player{Random.Range(1000, 9999)}";

        if (newName.Length > 60) newName = newName.Substring(0, 60);

        DisplayName.Value = newName;

        // Persist so next session starts with the same name
        PlayerPrefs.SetString(PrefsKey, newName);
        PlayerPrefs.Save();
    }

    private void Update()
    {
        if (!IsOwner) return;

        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        Vector3 move = input * moveSpeed * Time.deltaTime;
        MoveServerRpc(move);
    }

    [ServerRpc]
    private void MoveServerRpc(Vector3 move, ServerRpcParams rpcParams = default)
    {
        transform.position += move;
    }
}

using System;
using System.Collections;
using System.IO;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct NoticeItem : INetworkSerializable, IEquatable<NoticeItem>
{
    public FixedString128Bytes Title;
    public FixedString512Bytes Body;
    public double ServerTime;

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref Title);
        s.SerializeValue(ref Body);
        s.SerializeValue(ref ServerTime);
    }

    public bool Equals(NoticeItem other) =>
        ServerTime.Equals(other.ServerTime) &&
        Title.Equals(other.Title) &&
        Body.Equals(other.Body);

    public override bool Equals(object obj) => obj is NoticeItem other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int h = 17;
            h = h * 23 + ServerTime.GetHashCode();
            h = h * 23 + Title.GetHashCode();
            h = h * 23 + Body.GetHashCode();
            return h;
        }
    }
}

public class NoticeBoard : NetworkBehaviour
{
    public static NoticeBoard Instance { get; private set; }
    public NetworkList<NoticeItem> Items;

    private string SavePath => Path.Combine(Application.persistentDataPath, "noticeboard.json");
    private Coroutine _saveDebounce;

    // Use the exact delegate type expected by NetworkList<T>
    private NetworkList<NoticeItem>.OnListChangedDelegate _onListChangedHandler;

    [Serializable]
    private class NoticeDTO { public string title; public string body; public double time; }
    [Serializable]
    private class NoticeListDTO { public NoticeDTO[] items; }

    private void Awake()
    {
        Items = new NetworkList<NoticeItem>();
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            LoadFromDisk();
            _onListChangedHandler = (NetworkListEvent<NoticeItem> _) => DebouncedSave();
            Items.OnListChanged += _onListChangedHandler;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            if (_onListChangedHandler != null)
                Items.OnListChanged -= _onListChangedHandler;
            SaveToDisk();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestAddNoticeServerRpc(string title, string body)
    {
        AddNoticeInternal(title, body);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestClearAllServerRpc()
    {
        if (!IsServer) return;
        Items.Clear();
    }

    private void AddNoticeInternal(string title, string body)
    {
        if (!IsServer) return;
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body)) return;

        title = (title ?? "").Trim();
        body  = (body ?? "").Trim();

        if (title.Length > 120) title = title.Substring(0, 120);
        if (body.Length  > 480) body  = body.Substring(0, 480);

        Items.Add(new NoticeItem {
            Title = title,
            Body  = body,
            ServerTime = NetworkManager.ServerTime.Time
        });
    }

    private void DebouncedSave()
    {
        if (_saveDebounce != null) StopCoroutine(_saveDebounce);
        _saveDebounce = StartCoroutine(SaveSoon());
    }

    private IEnumerator SaveSoon()
    {
        yield return new WaitForSeconds(0.5f);
        SaveToDisk();
        _saveDebounce = null;
    }

    private void SaveToDisk()
    {
        try
        {
            var dto = new NoticeListDTO { items = new NoticeDTO[Items.Count] };
            for (int i = 0; i < Items.Count; i++)
            {
                dto.items[i] = new NoticeDTO {
                    title = Items[i].Title.ToString(),
                    body  = Items[i].Body.ToString(),
                    time  = Items[i].ServerTime
                };
            }
            File.WriteAllText(SavePath, JsonUtility.ToJson(dto, true));
        }
        catch (Exception e) { Debug.LogWarning($"NoticeBoard save failed: {e.Message}"); }
    }

    private void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(SavePath)) return;
            var json = File.ReadAllText(SavePath);
            var dto = JsonUtility.FromJson<NoticeListDTO>(json);
            Items.Clear();
            if (dto?.items != null)
            {
                foreach (var it in dto.items)
                    Items.Add(new NoticeItem {
                        Title = it.title,
                        Body  = it.body,
                        ServerTime = it.time
                    });
            }
        }
        catch (Exception e) { Debug.LogWarning($"NoticeBoard load failed: {e.Message}"); }
    }
}

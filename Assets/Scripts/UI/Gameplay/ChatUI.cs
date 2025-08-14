using System.Text;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ChatUI : MonoBehaviour
{
    [Header("Panel & Widgets")]
    [SerializeField] private GameObject panel;         // left-side parent
    [SerializeField] private TMP_InputField input;
    [SerializeField] private TMP_Text outputText;          
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private KeyCode toggleKey = KeyCode.T;

    private void Start()
    {
        if (panel) panel.SetActive(false);
        if (input) input.onSubmit.AddListener(_ => OnSend());

        if (ChatHub.Instance != null)
            ChatHub.Instance.Messages.OnListChanged += _ => Redraw();

        // draw any existing history
        Redraw();
    }

    private void OnDestroy()
    {
        if (ChatHub.Instance != null)
            ChatHub.Instance.Messages.OnListChanged -= _ => Redraw();
    }

void Update()
{
    // If typing, ignore the toggle hotkey
    if (input && input.isFocused)
    {
        // Allow ESC to close even while typing
        if (panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            Toggle(false);
            EventSystem.current.SetSelectedGameObject(null); // unfocus input
        }
        return;
    }

    // Toggle open/close with T
    if (Input.GetKeyDown(toggleKey))
    {
        Toggle(!panel.activeSelf);
    }

    // Also close with ESC as fallback
    if (panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
    {
        Toggle(false);
    }
}

public void Toggle(bool? forceState = null)
{
    if (!panel) return;

    bool show = forceState ?? !panel.activeSelf;
    panel.SetActive(show);

    if (show && input)
    {
        input.Select();
        input.ActivateInputField();
    }
}

public void OnSend()
{
    if (!input) return;
    var text = (input.text ?? "").Trim();
    if (string.IsNullOrEmpty(text)) return;

    if (ChatHub.Instance && NetworkManager.Singleton && NetworkManager.Singleton.IsConnectedClient)
    {
        ChatHub.Instance.SendChatServerRpc(text);  // <-- direct, no Player.Local timing issues
        input.text = "";
        input.ActivateInputField();
    }
    else
    {
        Debug.LogWarning("Chat send failed: no ChatHub/connection.");
    }
}


    private void Redraw()
    {
        if (!outputText) return;
        if (ChatHub.Instance == null) { outputText.text = ""; return; }

        var sb = new StringBuilder();
        foreach (var m in ChatHub.Instance.Messages)
        {
            var name = m.SenderId == ulong.MaxValue ? "System" : m.SenderName.ToString();
            sb.AppendLine($"[{name}] {m.Text}");
        }
        outputText.text = sb.ToString();

        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
    }
}

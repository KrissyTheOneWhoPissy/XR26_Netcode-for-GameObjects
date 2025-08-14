using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NoticeBoardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text listText;              // or TMP_Text if you prefer
    [SerializeField] private TMP_InputField titleInput;
    [SerializeField] private TMP_InputField bodyInput;
    [SerializeField] private Button addButton;
    [SerializeField] private Button clearButton;

    private NetworkList<NoticeItem>.OnListChangedDelegate _onListChangedHandler;

    private void Start()
    {
        if (NoticeBoard.Instance != null)
        {
            _onListChangedHandler = (NetworkListEvent<NoticeItem> _) => Redraw();
            NoticeBoard.Instance.Items.OnListChanged += _onListChangedHandler;
        }

        if (addButton)  addButton.onClick.AddListener(AddNotice);
        if (clearButton) clearButton.onClick.AddListener(ClearAll);

        Redraw();
    }

    private void OnDestroy()
    {
        if (NoticeBoard.Instance != null && _onListChangedHandler != null)
            NoticeBoard.Instance.Items.OnListChanged -= _onListChangedHandler;
    }

    private void Redraw()
    {
        if (listText == null)
            return;

        if (NoticeBoard.Instance == null)
        {
            listText.text = "";
            return;
        }

        var sb = new StringBuilder();
        foreach (var it in NoticeBoard.Instance.Items)
        {
            var title = it.Title.ToString();
            var body  = it.Body.ToString();

            if (!string.IsNullOrEmpty(title)) sb.AppendLine($"• {title}");
            if (!string.IsNullOrEmpty(body))  sb.AppendLine($"  {body}");
            sb.AppendLine();
        }
        listText.text = sb.ToString();
    }

    private void AddNotice()
    {
        if (NoticeBoard.Instance == null) return;
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsConnectedClient) return;

        var title = titleInput ? titleInput.text : "";
        var body  = bodyInput  ? bodyInput.text  : "";

        NoticeBoard.Instance.RequestAddNoticeServerRpc(title, body);

        if (titleInput) titleInput.text = "";
        if (bodyInput)  bodyInput.text  = "";
    }

    private void ClearAll()
    {
        if (NoticeBoard.Instance == null) return;
        NoticeBoard.Instance.RequestClearAllServerRpc();
    }
}

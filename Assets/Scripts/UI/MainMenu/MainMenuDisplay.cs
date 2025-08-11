using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuDisplay : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Header("Optional: Name Input")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private string prefsKey = "player_name";

    void Start()
    {
        // Pre-fill if previously set
        if (nameInput && PlayerPrefs.HasKey(prefsKey))
            nameInput.text = PlayerPrefs.GetString(prefsKey);
    }

    private void SaveNameIfProvided()
    {
        if (!nameInput) return;
        var trimmed = (nameInput.text ?? "").Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
            PlayerPrefs.SetString(prefsKey, trimmed);
            PlayerPrefs.Save();
        }
    }

    public void StartHost()
    {
        SaveNameIfProvided();
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public void StartServer()
    {
        SaveNameIfProvided();
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public void StartClient()
    {
        SaveNameIfProvided();
        NetworkManager.Singleton.StartClient();
        // Client will be moved by the server's SceneManager load
    }
}
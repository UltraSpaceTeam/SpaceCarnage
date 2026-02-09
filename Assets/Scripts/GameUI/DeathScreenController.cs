using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreenController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI deathSourceText;
    [SerializeField] private Button respawnButton;
    [SerializeField] private Button exitButton;

    private void Start()
    {
        respawnButton.onClick.AddListener(OnRespawnClicked);
        exitButton.onClick.AddListener(OnExitClicked);
    }

    public void Show(DamageContext source)
    {
        if (PauseMenuController.IsPaused)
        {
            PauseMenuController.Instance.TogglePauseMenu();
        }

        panel.SetActive(true);
        deathSourceText.text = $"Killed by: {source.AttackerName} with {source.WeaponID}";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        panel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnRespawnClicked()
    {
        var localPlayer = NetworkClient.localPlayer;
        if (localPlayer != null)
        {
            if (localPlayer.TryGetComponent<Player>(out var playerScript))
            {
                playerScript.CmdRequestRespawn();
            }
        }
    }

    private void OnExitClicked()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else
        {
            NetworkManager.singleton.StopClient();
        }
    }
}
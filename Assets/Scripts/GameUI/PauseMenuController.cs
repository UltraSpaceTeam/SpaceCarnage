using UnityEngine;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PauseMenuController : MonoBehaviour
{
    private static PauseMenuController _instance;
    public static PauseMenuController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PauseMenuController>(true);
                if (_instance != null)
                {
                    Debug.Log("[PAUSE] [INFO] Instance found by lazy method: " + _instance.gameObject.name);
                    _instance.gameObject.SetActive(true);
                }
            }
            return _instance;
        }
    }

    public static bool IsPaused { get; private set; } = false;

    [Header("Panels")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    private void Awake()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        IsPaused = false;

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumePauseMenu);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitToMenu);
    }

    public void TogglePauseMenu()
    {
        if (IsPaused)
        {
            ResumePauseMenu();
        }
        else
        {
            OpenPauseMenu();
        }
    }

    private void OpenPauseMenu()
    {
        Debug.Log("[PAUSE] [INFO] Opening ESC-menu");

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (resumeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
            resumeButton.OnSelect(null);
        }

        IsPaused = true;
    }

    public void ResumePauseMenu()
    {
        Debug.Log("[PAUSE] [INFO] Button 'Back to the game' is working - resuming");

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        

        EventSystem.current.SetSelectedGameObject(null);

        IsPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        Debug.Log("[PAUSE] [INFO] Button 'Settings' is working - settings panel is opening");

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void ReturnToPauseMenu()
    {
        Debug.Log("[PAUSE] [INFO] Button 'Close' in settings is working - returning to the ESC-menu");

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);

        if (resumeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }
    }

    public void ExitToMenu()
    {
        Debug.Log("[PAUSE] [INFO] Button 'Exit to main menu' is working - exiting to main menu");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }

        SceneManager.LoadScene("ShipEditor");
    }
}
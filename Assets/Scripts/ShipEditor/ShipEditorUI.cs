using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using kcp2k;
using Mirror;

public enum ShipComponentType { Hull, Weapon, Engine }

[System.Serializable]
public class ShipComponent
{
    public int componentId;
    public string componentName;
    public ShipComponentType componentType;
    public GameObject modelPrefab;
    public Sprite componentIcon;
    public int damage;
    public int health;
    public int speed;
    public string description;
    public bool isDefault;
	public ScriptableObject componentData;
}

[System.Serializable]
public class JoinGameResponse
{
    public string ip;
    public int port;
    public string key;
}

// Компонент для хранения данных в слоте
public class SlotData : MonoBehaviour
{
    public ShipComponent component;
}

public class ShipEditorUI : MonoBehaviour
{
    [Header("Ship Components")]
    [SerializeField] private ShipAssembler shipAssembler;
    [SerializeField] public ShipComponent[] hullComponents;
    [SerializeField] public ShipComponent[] weaponComponents;
    [SerializeField] public ShipComponent[] engineComponents;

    [Header("New UI - Three Sections")]
    [SerializeField] private Button[] hullSlots = new Button[4];    // 4 кнопки для корпусов
    [SerializeField] private Button[] weaponSlots = new Button[4];  // 4 кнопки для оружия
    [SerializeField] private Button[] engineSlots = new Button[4];  // 4 кнопки для двигателей

    [Header("UI Elements - Right Panel")]
    [SerializeField] private Transform shipPreviewAnchor;
    [SerializeField] private Button battleButton;
    [SerializeField] private TextMeshProUGUI shipStatsText;

    [Header("UI Elements - Header")]
    [SerializeField] private Button settingsMenuButton;
    [SerializeField] private GameObject dropdownPanel;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button logoutButton;

    [Header("Settings Window")]
    [SerializeField] private GameObject settingsWindow;
    [SerializeField] private TMP_Dropdown graphicsDropdown;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button applySettingsButton;
    [SerializeField] private Button closeSettingsButton;

    [Header("Colors")]
    [SerializeField] private Color selectedComponentColor = new Color(0.1f, 0.7f, 0.2f, 1f);
    [SerializeField] private Color normalComponentColor = new Color(0.2f, 0.2f, 0.2f, 1f);

	[Header("SFX controllers")]
	[SerializeField] public AudioMixerGroup sfxGroup;
    [SerializeField] public AudioMixerGroup musicGroup;
	[SerializeField] private float minVolumeDb = -40.0f;
	[SerializeField] private float maxVolumeDb = 0.0f;
	
    [Header("Leaderboard")]
    [SerializeField] private GlobalLeaderboardUI leaderboardUI;
	
	[Header("Context Menu")]
	[SerializeField] private Camera camera;
	[SerializeField] private GameObject canvas;
	[SerializeField] private GameObject contextMenuPanel;
	[SerializeField] private RectTransform rectMenuPanel; 
	

    private Dictionary<ShipComponentType, ShipComponent> selectedComponents = new Dictionary<ShipComponentType, ShipComponent>();
    private bool isInitialized = false;
    
    // Для вращения корабля
    private GameObject currentRotatingShip;
    private float rotationSpeed = 20f;

    void Start()
    {
        SetupUIEvents();
        PopulateSlots();
        LoadSavedConfiguration();
        UpdateShipPreview();
        UpdateStats();
        LoadSettings();
        isInitialized = true;
    }

    void Update()
    {
        // Вращаем корабль если он есть
        if (currentRotatingShip != null)
        {
            currentRotatingShip.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			canvas.transform as RectTransform,
			Input.mousePosition,
			camera,
			out Vector2 localPoint
		);
		rectMenuPanel.anchoredPosition = localPoint;
    }

    void SetupUIEvents()
    {
        // Настройка слотов корпусов
        for (int i = 0; i < hullSlots.Length; i++)
        {
            int index = i;
            if (hullSlots[i] != null)
            {
                hullSlots[i].onClick.AddListener(() => OnHullSlotClicked(index));
            }
        }
        
        // Настройка слотов оружия
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            int index = i;
            if (weaponSlots[i] != null)
            {
                weaponSlots[i].onClick.AddListener(() => OnWeaponSlotClicked(index));
            }
        }
        
        // Настройка слотов двигателей
        for (int i = 0; i < engineSlots.Length; i++)
        {
            int index = i;
            if (engineSlots[i] != null)
            {
                engineSlots[i].onClick.AddListener(() => OnEngineSlotClicked(index));
            }
        }

        // Battle button
        battleButton.onClick.AddListener(StartBattle);

        // Header menu
        settingsMenuButton.onClick.AddListener(() => dropdownPanel.SetActive(!dropdownPanel.activeSelf));

        if (leaderboardButton != null)
        {
            leaderboardButton.onClick.RemoveAllListeners();
            leaderboardButton.onClick.AddListener(OpenLeaderboard);
        }

        settingsButton.onClick.AddListener(OpenSettingsWindow);
        logoutButton.onClick.AddListener(Logout);

        // Settings window
        applySettingsButton.onClick.AddListener(ApplySettings);
        closeSettingsButton.onClick.AddListener(CloseSettingsWindow);
    }

    private void OpenLeaderboard()
    {
        if (leaderboardUI != null)
        {
            leaderboardUI.Show();
        }
        else
        {
            Debug.LogError("GlobalLeaderboardUI не назначен в ShipEditorUI! Назначьте панель лидерборда в инспекторе.");
        }
    }

    void PopulateSlots()
    {
        // Заполняем слоты корпусов
        for (int i = 0; i < Mathf.Min(hullSlots.Length, hullComponents.Length); i++)
        {
            if (hullSlots[i] != null && hullComponents[i] != null)
            {
                // Устанавливаем иконку
                Image icon = hullSlots[i].transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null && hullComponents[i].componentIcon != null)
                {
                    icon.sprite = hullComponents[i].componentIcon;
                    icon.gameObject.SetActive(true);
                }
                
                // Сохраняем данные компонента в слоте
                SlotData slotData = hullSlots[i].gameObject.GetComponent<SlotData>();
                if (slotData == null) slotData = hullSlots[i].gameObject.AddComponent<SlotData>();
                slotData.component = hullComponents[i];
                
                // Добавляем текст с названием (опционально)
                TextMeshProUGUI nameText = hullSlots[i].transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = hullComponents[i].componentName;
                }
            }
        }
        
        // Заполняем слоты оружия
        for (int i = 0; i < Mathf.Min(weaponSlots.Length, weaponComponents.Length); i++)
        {
            if (weaponSlots[i] != null && weaponComponents[i] != null)
            {
                Image icon = weaponSlots[i].transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null && weaponComponents[i].componentIcon != null)
                {
                    icon.sprite = weaponComponents[i].componentIcon;
                    icon.gameObject.SetActive(true);
                }
                
                SlotData slotData = weaponSlots[i].gameObject.GetComponent<SlotData>();
                if (slotData == null) slotData = weaponSlots[i].gameObject.AddComponent<SlotData>();
                slotData.component = weaponComponents[i];
                
                TextMeshProUGUI nameText = weaponSlots[i].transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = weaponComponents[i].componentName;
                }
            }
        }
        
        // Заполняем слоты двигателей
        for (int i = 0; i < Mathf.Min(engineSlots.Length, engineComponents.Length); i++)
        {
            if (engineSlots[i] != null && engineComponents[i] != null)
            {
                Image icon = engineSlots[i].transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null && engineComponents[i].componentIcon != null)
                {
                    icon.sprite = engineComponents[i].componentIcon;
                    icon.gameObject.SetActive(true);
                }
                
                SlotData slotData = engineSlots[i].gameObject.GetComponent<SlotData>();
                if (slotData == null) slotData = engineSlots[i].gameObject.AddComponent<SlotData>();
                slotData.component = engineComponents[i];
                
                TextMeshProUGUI nameText = engineSlots[i].transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = engineComponents[i].componentName;
                }
            }
        }
    }

    void OnHullSlotClicked(int slotIndex)
    {
        if (slotIndex < hullComponents.Length && hullComponents[slotIndex] != null)
        {
            SelectComponent(hullComponents[slotIndex], ShipComponentType.Hull);
            HighlightSelectedSlot(hullSlots, slotIndex);
        }
    }

    void OnWeaponSlotClicked(int slotIndex)
    {
        if (slotIndex < weaponComponents.Length && weaponComponents[slotIndex] != null)
        {
            SelectComponent(weaponComponents[slotIndex], ShipComponentType.Weapon);
            HighlightSelectedSlot(weaponSlots, slotIndex);
        }
    }

    void OnEngineSlotClicked(int slotIndex)
    {
        if (slotIndex < engineComponents.Length && engineComponents[slotIndex] != null)
        {
            SelectComponent(engineComponents[slotIndex], ShipComponentType.Engine);
            HighlightSelectedSlot(engineSlots, slotIndex);
        }
    }

    void HighlightSelectedSlot(Button[] slots, int selectedIndex)
    {
        // Сбрасываем цвет всех слотов этой категории
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].GetComponent<Image>().color = normalComponentColor;
            }
        }
        
        // Подсвечиваем выбранный слот
        if (selectedIndex >= 0 && selectedIndex < slots.Length && slots[selectedIndex] != null)
        {
            slots[selectedIndex].GetComponent<Image>().color = selectedComponentColor;
        }
    }

    void SelectComponent(ShipComponent component, ShipComponentType type)
    {
        // Сохраняем выбранный компонент
        selectedComponents[type] = component;
        
        Debug.Log($"Выбран {type}: {component.componentName}");
        
        // Обновляем визуализацию и статистику
        UpdateShipPreview();
        UpdateStats();
        
        // Сохраняем конфигурацию
        SaveConfiguration();
    }

    void UpdateShipPreview()
    {
        // Проверяем что все компоненты выбраны
        if (!selectedComponents.ContainsKey(ShipComponentType.Hull) ||
            !selectedComponents.ContainsKey(ShipComponentType.Weapon) ||
            !selectedComponents.ContainsKey(ShipComponentType.Engine))
        {
            return;
        }
        
        // Получаем выбранные компоненты
        var hull = selectedComponents[ShipComponentType.Hull];
        var weapon = selectedComponents[ShipComponentType.Weapon];
        var engine = selectedComponents[ShipComponentType.Engine];
        
        // Проверяем ShipAssembler
        if (shipAssembler == null)
        {
            Debug.LogError("ShipAssembler не привязан!");
            return;
        }

        // Проверяем префабы
        Debug.Log($"Корпус prefab: {hull.modelPrefab != null} ({hull.componentName})");
        Debug.Log($"Оружие prefab: {weapon.modelPrefab != null} ({weapon.componentName})");
        Debug.Log($"Двигатель prefab: {engine.modelPrefab != null} ({engine.componentName})");
        
        // Создаём временные ScriptableObjects для ShipAssembler
        var hullData = ScriptableObject.CreateInstance<HullData>();
        hullData.prefab = hull.modelPrefab;
        
        var weaponData = ScriptableObject.CreateInstance<WeaponData>();
        weaponData.prefab = weapon.modelPrefab;
        
        var engineData = ScriptableObject.CreateInstance<EngineData>();
        engineData.prefab = engine.modelPrefab;
        
        // Собираем корабль через ShipAssembler
        shipAssembler.EquipHull(hullData);
        shipAssembler.EquipWeapon(weaponData);
        shipAssembler.EquipEngine(engineData);
        
        // Сохраняем ссылку на корабль для вращения
        if (shipAssembler.CurrentHullObject != null)
        {
            currentRotatingShip = shipAssembler.CurrentHullObject;
            
            // Увеличиваем размер корабля
            //currentRotatingShip.transform.localScale = Vector3.one * 3f;
            
            // Сдвигаем вправо
            //currentRotatingShip.transform.localPosition = new Vector3(2f, 0f, 0f);
        }
        
        Debug.Log("Корабль собран и будет вращаться!");
    }

    void UpdateStats()
    {
        if (!selectedComponents.ContainsKey(ShipComponentType.Hull) || 
            !selectedComponents.ContainsKey(ShipComponentType.Weapon) || 
            !selectedComponents.ContainsKey(ShipComponentType.Engine))
            return;
        
        var hull = selectedComponents[ShipComponentType.Hull];
        var weapon = selectedComponents[ShipComponentType.Weapon];
        var engine = selectedComponents[ShipComponentType.Engine];
        
		HullData hullData = (HullData) hull.componentData;
		WeaponData weaponData = (WeaponData) weapon.componentData;
		EngineData engineData = (EngineData) engine.componentData;
		
        float totalDamage = weaponData.damage;
        float totalHealth = hullData.maxHealth;
        float totalMass = hullData.mass + engineData.mass + weaponData.mass;
        float power = engineData.power;
		
        shipStatsText.text = 
            $"<size=24><b>SHIP STATISTICS</b></size>\n\n" +
            $"<b>Hull:</b> {hull.componentName}\n" +
            $"<b>Weapon:</b> {weapon.componentName}\n" +
            $"<b>Engine:</b> {engine.componentName}\n\n" +
            $"<b>Damage:</b> {totalDamage}\n" +
            $"<b>Health:</b> {totalHealth}\n" +
            $"<b>Mass:</b> {totalMass}\n" +
			$"<b>Power:</b> {power}";
    }

    async void StartBattle()
    {
        SaveConfiguration();

        battleButton.interactable = false;

        var config = ConfigManager.LoadConfig();
        if (config == null || config.player_id <= 0)
        {
            Debug.LogError("Player ID not found. Please relogin.");
            battleButton.interactable = true;
            return;
        }
        if (!string.IsNullOrEmpty(config.jwt_token))
        {
            APINetworkManager.SetToken(config.jwt_token);
        }

        Debug.Log($"Looking for a match for Player {config.player_id}...");

        try
        {
            string query = $"player_id={config.player_id}";
            JoinGameResponse matchData = await APINetworkManager.Instance.GetRequestAsync<JoinGameResponse>("/games/join", query);

            if (matchData != null)
            {
                Debug.Log($"Match found! Connecting to {matchData.ip}:{matchData.port} with key {matchData.key}");
                ConnectToMatch(matchData);
            }
            else
            {
                Debug.LogError("Failed to join game: Empty response.");
                battleButton.interactable = true;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Matchmaking Error: {ex.Message}");
            battleButton.interactable = true;
        }
    }

    private void ConnectToMatch(JoinGameResponse matchData)
    {
        if (GameData.Instance != null)
        {
            GameData.Instance.SetSessionData(0, matchData.key);
        }
        else
        {
            Debug.LogError("GameData Instance not found! Auth key won't be set.");
        }

        NetworkManager.singleton.networkAddress = matchData.ip;

        if (Transport.active is KcpTransport kcp)
        {
            kcp.Port = (ushort)matchData.port;
        }

        NetworkManager.singleton.StartClient();
    }

    void OpenSettingsWindow()
    {
        dropdownPanel.SetActive(false);
        settingsWindow.SetActive(true);
    }

    void CloseSettingsWindow()
    {
        settingsWindow.SetActive(false);
    }

	float VolumeMapping(float sliderValue)
	{
		// 0-1 → -40-0 с логарифмической кривой
		// Более естественно для восприятия
		if (sliderValue <= 0.0001f) return -200f; // Полная тишина
		return Mathf.Log10(sliderValue) * 20f; // Преобразует 0.001 → -60, 0.01 → -40, 1 → 0
	}

    void ApplySettings()
    {
        // Save graphics settings
        PlayerPrefs.SetInt("GraphicsQuality", graphicsDropdown.value);
        
        // Save audio settings
        PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);
        
        PlayerPrefs.Save();
        
		sfxGroup.audioMixer.SetFloat("SFXVolume", VolumeMapping(sfxSlider.value));
		musicGroup.audioMixer.SetFloat("MusicVolume", VolumeMapping(musicSlider.value));
		
        // Apply settings
        ApplyGraphicsQuality(graphicsDropdown.value);
        
        Debug.Log("Settings applied");
        CloseSettingsWindow();
    }

    void LoadSettings()
    {
        // Load graphics settings
        int graphicsQuality = PlayerPrefs.GetInt("GraphicsQuality", 1);
        graphicsDropdown.value = graphicsQuality;
        ApplyGraphicsQuality(graphicsQuality);
        
        // Load audio settings
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
		
		
		sfxGroup.audioMixer.SetFloat("SFXVolume", VolumeMapping(sfxSlider.value));
		musicGroup.audioMixer.SetFloat("MusicVolume", VolumeMapping(musicSlider.value));
    }

    void ApplyGraphicsQuality(int qualityLevel)
    {
        QualitySettings.SetQualityLevel(qualityLevel, true);
    }

    void Logout()
    {
        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.Save();
        SceneManager.LoadScene("LoginScene");
    }

    void LoadSavedConfiguration()
    {
        bool hasSavedConfig = false;
        
        foreach (ShipComponentType type in System.Enum.GetValues(typeof(ShipComponentType)))
        {
            int savedComponentId = PlayerPrefs.GetInt($"ShipComponent_{type}", 0);
            
            hasSavedConfig = true;
            ShipComponent savedComponent = FindComponentById(type, savedComponentId);
            
            if (savedComponent != null)
            {
                selectedComponents[type] = savedComponent;
                
                // Подсвечиваем выбранный слот
                int slotIndex = FindSlotIndexByComponent(type, savedComponent);
                if (slotIndex >= 0)
                {
                    switch (type)
                    {
                        case ShipComponentType.Hull:
                            HighlightSelectedSlot(hullSlots, slotIndex);
                            break;
                        case ShipComponentType.Weapon:
                            HighlightSelectedSlot(weaponSlots, slotIndex);
                            break;
                        case ShipComponentType.Engine:
                            HighlightSelectedSlot(engineSlots, slotIndex);
                            break;
                    }
                }
            }
        }
        
        // If no saved config, select default components
        if (!hasSavedConfig)
        {
            SelectDefaultComponents();
        }

        Debug.Log("После загрузки выбрано компонентов: " + selectedComponents.Count);
        foreach (var kvp in selectedComponents)
        {
            Debug.Log(kvp.Key + ": " + kvp.Value.componentName);
        }
    }

    int FindSlotIndexByComponent(ShipComponentType type, ShipComponent component)
    {
        ShipComponent[] components = GetComponentsByCategory(type);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].componentId == component.componentId)
            {
                return i;
            }
        }
        return -1;
    }

    void SelectDefaultComponents()
    {
        SelectDefaultForType(ShipComponentType.Hull, hullComponents);
        SelectDefaultForType(ShipComponentType.Weapon, weaponComponents);
        SelectDefaultForType(ShipComponentType.Engine, engineComponents);
    }

    void SelectDefaultForType(ShipComponentType type, ShipComponent[] components)
    {
        ShipComponent defaultComponent = components.FirstOrDefault(p => p.isDefault);
        
        if (defaultComponent == null && components.Length > 0)
            defaultComponent = components[0];
        
        if (defaultComponent != null)
        {
            selectedComponents[type] = defaultComponent;
            
            // Подсвечиваем слот по умолчанию
            int slotIndex = System.Array.IndexOf(components, defaultComponent);
            if (slotIndex >= 0)
            {
                switch (type)
                {
                    case ShipComponentType.Hull:
                        HighlightSelectedSlot(hullSlots, slotIndex);
                        break;
                    case ShipComponentType.Weapon:
                        HighlightSelectedSlot(weaponSlots, slotIndex);
                        break;
                    case ShipComponentType.Engine:
                        HighlightSelectedSlot(engineSlots, slotIndex);
                        break;
                }
            }
        }
    }

    ShipComponent FindComponentById(ShipComponentType type, int componentId)
    {
        foreach (var component in GetComponentsByCategory(type))
        {
            if (component.componentId == componentId)
                return component;
        }
        return null;
    }

    ShipComponent[] GetComponentsByCategory(ShipComponentType category)
    {
        return category switch
        {
            ShipComponentType.Hull => hullComponents,
            ShipComponentType.Weapon => weaponComponents,
            ShipComponentType.Engine => engineComponents,
            _ => new ShipComponent[0]
        };
    }

    void SaveConfiguration()
    {
        foreach (var component in selectedComponents)
        {
            PlayerPrefs.SetInt($"ShipComponent_{component.Key}", component.Value.componentId);
        }
        PlayerPrefs.Save();
    }
	
		
	void OnPointerEnter() {
		Debug.Log("PointerEnter");
	}
	
}
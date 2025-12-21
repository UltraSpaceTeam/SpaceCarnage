using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Controllers")]
    [SerializeField] private HUDController hudController;
    [SerializeField] private DeathScreenController deathScreenController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowDeathScreen(string source)
    {
        deathScreenController.Show(source);
    }

    public void HideDeathScreen()
    {
        deathScreenController.Hide();
    }

    public void UpdateHealth(float current, float max)
    {
        hudController.UpdateHealth(current, max);
    }

    public void AddKillFeedEntry(string killer, string victim)
    {
        hudController.AddKillFeed(killer, victim);
    }
}
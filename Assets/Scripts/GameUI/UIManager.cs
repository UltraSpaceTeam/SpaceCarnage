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

    public void ShowDeathScreen(DamageContext source)
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

    public void AddKillFeedEntry(DamageContext ctx, string victim)
    {
        hudController.AddKillFeed(ctx, victim);
    }
}
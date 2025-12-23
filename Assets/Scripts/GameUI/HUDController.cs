using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Health Bar Components")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Health Bar Settings")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Gradient colorGradient;

    [Header("Killfeed Settings")]
    [SerializeField] private Transform killFeedContainer;
    [SerializeField] private GameObject killFeedItemPrefab;
    [SerializeField] private int maxKillFeedItems = 5;
    [SerializeField] private float killFeedDuration = 7f;


    private float _targetFillAmount = 1f;

    private void Start()
    {
        if (healthBarFill != null)
        {
            _targetFillAmount = healthBarFill.fillAmount;
            healthBarFill.color = colorGradient.Evaluate(_targetFillAmount);
        }
    }

    private void Update()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, _targetFillAmount, Time.deltaTime * smoothSpeed);

            healthBarFill.color = colorGradient.Evaluate(healthBarFill.fillAmount);
        }
    }

    public void UpdateHealth(float current, float max)
    {
        _targetFillAmount = Mathf.Clamp01(current / max);

        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(current)} / {max}";
        }

    }

    public void AddKillFeed(DamageContext ctx, string victim)
    {
        GameObject item = Instantiate(killFeedItemPrefab, killFeedContainer);

        var textComp = item.GetComponentInChildren<TextMeshProUGUI>();

        if (textComp != null)
        {
            string message = "";

            switch (ctx.Type)
            {
                case DamageType.Suicide:
                    message = $"<b>{victim}</b> <color=yellow>[SUICIDE]</color>";
                    break;

                case DamageType.Collision:
                    message = $"<b>{victim}</b> <color=#AAAAAA>[CRASH]</color> {ctx.WeaponID}";
                    break;

                case DamageType.Weapon:
                    if (ctx.AttackerName == victim)
                    {
                        message = $"<b>{victim}</b> <color=red>[OWN GOAL]</color> {ctx.WeaponID}";
                    }
                    else
                    {
                        message = $"<b>{ctx.AttackerName}</b> <color=red>[{ctx.WeaponID}]</color> <b>{victim}</b>";
                    }
                    break;

                default:
                    message = $"<b>{victim}</b> died";
                    break;
            }

            textComp.text = message;
        }

        if (killFeedContainer.childCount > maxKillFeedItems)
        {
            Destroy(killFeedContainer.GetChild(0).gameObject);
        }

        Destroy(item, killFeedDuration);
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem.XR;
using Mirror;

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

    [Header("Ability Indicator")]
    [SerializeField] private GameObject abilityPanel;
    [SerializeField] private Image abilityIcon;
    [SerializeField] private Image abilityCooldownFill;
    [SerializeField] private Image abilityProgressFill;
    [SerializeField] private TextMeshProUGUI abilityText;
    [SerializeField] private Sprite dashIcon;
    [SerializeField] private Sprite shieldIcon;
    [SerializeField] private Sprite invisIcon;
    [SerializeField] private Sprite defaultAbilityIcon;

    [Header("Ammo Indicator")]
    [SerializeField] private TextMeshProUGUI ammoText;   // Текст типа "10/20" или "Reloading..."

    [Header("Match Timer")]
    [SerializeField] private TextMeshProUGUI matchTimerText;

    private float _targetFillAmount = 1f;

    private void Start()
    {
        if (healthBarFill != null)
        {
            _targetFillAmount = healthBarFill.fillAmount;
            healthBarFill.color = colorGradient.Evaluate(_targetFillAmount);
        }

        if (abilityPanel != null) abilityPanel.SetActive(false);
        if (ammoText != null) ammoText.text = "";
    }

    private void Update()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, _targetFillAmount, Time.deltaTime * smoothSpeed);

            healthBarFill.color = colorGradient.Evaluate(healthBarFill.fillAmount);
        }

        UpdateAbilityIndicator();
        UpdateAmmoIndicator();
        if (matchTimerText != null)
        {
            matchTimerText.text = GetMatchTimerText();
        }
    }

    private void UpdateAmmoIndicator()
    {
        var localPlayer = FindLocalPlayer();
        if (localPlayer == null || ammoText == null) return;

        var shooting = localPlayer.GetComponent<ShipShooting>();
        var weapon = shooting?.CurrentWeaponData;

        if (weapon == null)
        {
            ammoText.text = "No Weapon";
            return;
        }

        if (shooting.IsReloading)
            ammoText.text = "Reloading...";
        else
            ammoText.text = $"{shooting.CurrentAmmo} / {weapon.ammo}";
    }

    private void UpdateAbilityIndicator()
    {
        var localPlayer = FindLocalPlayer();
        if (localPlayer == null) return;

        var assembler = localPlayer.GetComponent<ShipAssembler>();
        var controller = localPlayer.GetComponent<PlayerController>();

        if (assembler?.CurrentEngine?.ability == null)
        {
            if (abilityPanel != null) abilityPanel.SetActive(false);
            return;
        }

        var ability = assembler.CurrentEngine.ability;
        float cooldownTimer = controller.AbilityCooldownRemaining;

        if (abilityPanel != null) abilityPanel.SetActive(true);

        if (abilityIcon != null)
        {
            if (ability is ShieldAbility) abilityIcon.sprite = shieldIcon;
            else if (ability is InvisAbility) abilityIcon.sprite = invisIcon;
            else if (ability is DashAbility) abilityIcon.sprite = dashIcon;
            else abilityIcon.sprite = defaultAbilityIcon;

            bool onCooldown = cooldownTimer > 0.01f;
            abilityIcon.color = onCooldown ? new Color(0.55f, 0.55f, 0.55f, 1f) : Color.white;
        }

        float statusValue = controller.AbilityStatusValue;

        if (abilityProgressFill != null)
        {
            float fillAmount = statusValue;
            Color fillColor = Color.clear;

            if (ability is ShieldAbility)
            {
                if (fillAmount >= 1f)
                    fillColor = new Color(0f, 1f, 0f, 0.55f);
                else if (fillAmount > 0.6f)
                    fillColor = Color.Lerp(new Color(1f, 1f, 0f, 0.55f), new Color(0f, 1f, 0f, 0.55f), (fillAmount - 0.6f) / 0.4f);
                else if (fillAmount > 0.01f)
                    fillColor = Color.Lerp(new Color(1f, 0.4f, 0.4f, 0.55f), new Color(1f, 1f, 0f, 0.55f), fillAmount / 0.6f);
                else
                    fillColor = new Color(0.6f, 0.2f, 0.2f, 0.55f);
            }
            else if (ability is InvisAbility)
            {
                if (statusValue >= 0.99f)
                {
                    fillAmount = 1f;
                    fillColor = new Color(0.6f, 0.6f, 1f, 0.6f);
                }
                else if (statusValue > 0.01f)
                {
                    fillColor = new Color(0.7f, 0.7f, 1f, 0.7f);
                }
                else
                {
                    fillAmount = 0f;
                    fillColor = Color.clear;
                }
            }

            abilityProgressFill.fillAmount = fillAmount;
            abilityProgressFill.color = fillColor;
        }

        if (abilityCooldownFill != null)
        {
            float cooldownProgress = ability.cooldown > 0 ? Mathf.Clamp01(cooldownTimer / ability.cooldown) : 0f;
            abilityCooldownFill.fillAmount = cooldownProgress;
            float alpha = cooldownProgress * 0.7f;
            abilityCooldownFill.color = new Color(0.1f, 0.1f, 0.1f, alpha);
        }

        if (abilityText != null)
        {
            if (ability is ShieldAbility)
            {
                if (statusValue >= 1f) abilityText.text = "READY";
                else if (statusValue > 0f) abilityText.text = $"ON: {Mathf.RoundToInt(statusValue * 100)}%";
                else abilityText.text = "BROKEN";
            }
            else if (ability is InvisAbility)
            {
                if (statusValue >= 0.99f) abilityText.text = "Invisible";
                else if (statusValue > 0.01f) abilityText.text = "Activating...";
                else abilityText.text = "";
            }
            else
            {
                abilityText.text = "";
            }
        }
    }

    private Player FindLocalPlayer()
    {
        foreach (var player in Player.ActivePlayers.Values)
            if (player.isLocalPlayer) return player;
        return null;
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
                    if (ctx.AttackerId != 0)
                        message = $"<b>{ctx.AttackerName}</b> <color=#AAAAAA>[RAM]</color> <b>{victim}</b>";
                    else
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

    private string GetMatchTimerText()
    {
        const float MatchDuration = 600f;
        const float EndingDuration = 30f;
        if (Player.ClientTimerState == 1 && Player.ClientMatchStartTime > 0)
        {
            var elapsed = NetworkTime.time - Player.ClientMatchStartTime;
            var rem = Mathf.Max(0, MatchDuration - (float)elapsed);
            return $"{(int)(rem / 60):D2}:{(int)(rem % 60):D2}";
        }

        if (Player.ClientTimerState == 2 && Player.ClientEndingStartTime > 0)
        {
            var elapsed = NetworkTime.time - Player.ClientEndingStartTime;
            var rem = Mathf.Max(0, EndingDuration - (float)elapsed);
            return $"{(int)(rem / 60):D2}:{(int)(rem % 60):D2}";
        }

        return "--:--";
    }

}
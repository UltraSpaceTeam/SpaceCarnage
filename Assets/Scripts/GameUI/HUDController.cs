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

    public void AddKillFeed(string killer, string victim)
    {
        
    }
}
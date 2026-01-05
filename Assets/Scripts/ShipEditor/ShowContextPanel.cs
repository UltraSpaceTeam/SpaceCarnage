using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShowContextPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[Header("Ship Component ID")]
	[SerializeField] private ScriptableObject component;
	
	[Header("Show Contorl Panel - Settings")]
	[SerializeField] private GameObject contextMenuPanel;
    [SerializeField] private TextMeshProUGUI shipStatsText;
		
	public void OnPointerEnter(PointerEventData data) {
		string information = $"";
		contextMenuPanel.SetActive(true);
		if (component is HullData) {
			HullData comp = (HullData)component;
			information = $"Health: {comp.maxHealth}\n";
		} else if (component is EngineData) {			
			EngineData comp = (EngineData)component;
			information = $"Ability cooldown: {comp.ability.cooldown}\n";
		} else if (component is WeaponData) {			
			WeaponData comp = (WeaponData)component;
			information = $"Damage: {comp.damage}\nRate of fire: {comp.fireRate}\nRange: {comp.range}\n";
		}
		shipStatsText.text = information;
	}
	
	public void OnPointerExit(PointerEventData data) {
		contextMenuPanel.SetActive(false);
	}
}

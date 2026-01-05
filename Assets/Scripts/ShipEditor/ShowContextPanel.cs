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
			information = $"Health: {comp.maxHealth}\nMass: {comp.mass}";
		} else if (component is EngineData) {			
			EngineData comp = (EngineData)component;
			if (comp.ability != null) {
				information = $"Ability cooldown: {comp.ability.cooldown}\n";
			}
			information += $"Mass: {comp.mass}\nPower: {comp.power}";
		} else if (component is WeaponData) {			
			WeaponData comp = (WeaponData)component;
			information = $"Damage: {comp.damage}\nRate of fire: {comp.fireRate}\nRange: {comp.range}\nMass: {comp.mass}";
		}
		shipStatsText.text = information;
	}
	
	public void OnPointerExit(PointerEventData data) {
		contextMenuPanel.SetActive(false);
	}
}

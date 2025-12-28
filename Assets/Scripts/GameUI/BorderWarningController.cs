using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BorderWarningController : MonoBehaviour
{
    [Header("Border Warning Settings")]
    [SerializeField] private Transform warningContainer;
    [SerializeField] private GameObject warningItemPrefab;
	
	private GameObject item;
	
	public void Show()
    {
        item = Instantiate(warningItemPrefab, warningContainer);
        var textComp = item.GetComponentInChildren<TextMeshProUGUI>();
        string message = $"Warning: Space Radiation";
	
        textComp.text = message;
    }
	
	public void Hide()
    {
        Destroy(item, 0);
    }
}

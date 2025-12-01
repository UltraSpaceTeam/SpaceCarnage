using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class TabToNextController : MonoBehaviour
{
    [SerializeField] private TMP_InputField next;
    private TMP_InputField self;

    private void Start()
    {
        self = GetComponent<TMP_InputField>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && self.isFocused)
        {
            if (next != null)
            {
                next.Select();
            }
        }
    }
}

using UnityEngine;

public class ToggleButton : MonoBehaviour
{
    [SerializeField]
    private GameObject onToggle;

    [SerializeField]
    private GameObject offToggle;

    public void Switch(bool on)
    {
        onToggle.SetActive(on);
        offToggle.SetActive(!on);
    }
}

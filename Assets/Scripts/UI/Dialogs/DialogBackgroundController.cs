using System.Collections.Generic;
using UnityEngine;

public class DialogBackgroundController : MonoBehaviour
{
    private static readonly Stack<DialogBackgroundController> dialogs;

    [SerializeField]
    private GameObject background;

    [SerializeField]
    private int siblingIndex;

    public int SiblingIndex => siblingIndex;

    static DialogBackgroundController()
    {
        dialogs = new Stack<DialogBackgroundController>();
    }

    private void OnEnable()
    {
        dialogs.Push(this);

        background.SetActive(true);
        background.transform.SetSiblingIndex(SiblingIndex);      
    }

    private void OnDisable()
    {
        if (dialogs.Peek() == this)
        {
            dialogs.Pop();

            while (dialogs.Count > 0)
            {
                DialogBackgroundController dialog = dialogs.Peek();

                if (dialog.gameObject.activeSelf)
                {
                    background.transform.SetSiblingIndex(dialog.SiblingIndex);
                    return;
                }
                else
                {
                    dialogs.Pop();
                }
            }

            background.transform.SetSiblingIndex(0);
            background.transform.gameObject.SetActive(false);
        }
    }
}

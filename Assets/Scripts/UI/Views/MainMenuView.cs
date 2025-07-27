using UnityEngine;
using Zillneuron.UILayout;

public class MainMenuView : ACompleteView
{
    protected override void OnLaunch()
    {
        base.OnLaunch();

        gameObject.SetActive(true);
    }

    protected override void OnFinish()
    {
        base.OnFinish();

        gameObject.SetActive(false);
    }

    protected void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            ChangeActivityToNext<GameplayView>();
        }
    }
}

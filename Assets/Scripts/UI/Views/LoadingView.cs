using UnityEngine;
using Zillneuron.UILayout;

public class LoadingView : StartView
{
    protected override void OnLaunch()
    {
        gameObject.SetActive(true);

        base.OnLaunch();
    }


    protected override void OnFinish()
    {
        base.OnFinish();

        gameObject.SetActive(false);
    }

    protected override void Update()
    {
        base.Update();

        if(Input.GetKeyDown(KeyCode.Z))
        {
            ChangeActivityToNext<MainMenuView>();
        }
    }
}

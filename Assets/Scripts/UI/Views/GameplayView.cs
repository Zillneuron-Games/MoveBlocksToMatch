using Zillneuron.UILayout;
using UnityEngine;

public class GameplayView : ACompleteView
{
    [SerializeField]
    private GameplayManager gameplayManager;

    protected override void OnLaunch()
    {
        base.OnLaunch();

        gameObject.SetActive(true);

        gameplayManager.StartGame();
    }

    protected override void OnFinish()
    {
        base.OnFinish();

        gameObject.SetActive(false);
    }
}

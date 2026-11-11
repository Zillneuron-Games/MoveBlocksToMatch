using TMPro;
using UnityEngine;
using Zillneuron.UILayout;
using Zillneuron.Blockmates.Services.SignIn;

public class LevelsView : ACompleteView, ILevelSelector
{
    private int nextGameId;

    private Canvas canvas;

    [SerializeField]
    private LevelPanel[] levelPanels;

    protected override void OnInitialize()
    {
        base.OnInitialize();

        canvas = GetComponentInParent<Canvas>(true);
    }

    protected override void ConstructFragments()
    {
        base.ConstructFragments();

        for (int i = 0; i < levelPanels.Length; i++)
        {
            levelPanels[i].Construct(this);
        }
    }

    protected override void LaunchFragments()
    {
        base.LaunchFragments();

        nextGameId = AccountService.Instance.NextLevel;

        Debug.LogError($"Next Game Id : {nextGameId}");

        for (int i = 0; i < levelPanels.Length; i++)
        {
            levelPanels[i].Launch();
        }
    }

    protected override void FinishFragments()
    {
        base.FinishFragments();

        for (int i = 0; i < levelPanels.Length; i++)
        {
            levelPanels[i].Finish();
        }
    }

    protected override void DestructFragments()
    {
        base.DestructFragments();

        for (int i = 0; i < levelPanels.Length; i++)
        {
            levelPanels[i].Destruct();
        }
    }

    protected override void OnLaunch()
    {
        base.OnLaunch();

        canvas.enabled = true;
    }

    protected override void OnFinish()
    {
        base.OnFinish();

        canvas.enabled = false;
    }

    public void Select(int level)
    {
        Debug.LogError($"Level : {level}");
        ChangeActivityToNextAndReplace<GameplayView>();
    }

    public bool Unlocked(int level)
    {
        return level <= nextGameId;
    }

    public void Back_ButtonHandler()
    {
        ChangeActivityToPrevious();
    }
}

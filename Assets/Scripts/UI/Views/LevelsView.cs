using TMPro;
using UnityEngine;
using Zillneuron.UILayout;

public class LevelsView : ACompleteView, ILevelSelector
{
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

    public void Back_ButtonHandler()
    {
        ChangeActivityToPrevious();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zillneuron.UILayout;
using Zillneuron.Blockmates.Services.SignIn;

public class LoadingView : StartView
{
    private bool needsWelcome;
    private float loadingBarTargetProgress = 0.16f;
    private float logoMoveDuration = 0.5f;
    private Canvas canvas;


    [SerializeField]
    private RectTransform logo;

    [SerializeField]
    public Vector2 logoTargetPosition;

    [SerializeField]
    private LoadingBar loadingBar;

    protected override void OnInitialize()
    {
        base.OnInitialize();

        canvas = GetComponentInParent<Canvas>(true);
    }

    protected override void OnLaunch()
    {
        canvas.enabled = true;

        base.OnLaunch();
    }

    protected override void OnFinish()
    {
        base.OnFinish();

        canvas.enabled = false;
    }

    protected override void Start()
    {
        base.Start();

        StartLoadingBarProgress();
        InitializeServices();
    }

    protected async void InitializeServices()
    {
        loadingBarTargetProgress = 0.2f;

        await AccountService.Instance.InitializeAsync();

        needsWelcome = AccountService.Instance.NeedsWelcome;

        loadingBarTargetProgress = 0.4f;

        if (needsWelcome)
        {
            await Task.Delay(100);

            loadingBarTargetProgress = 0.6f;

            await Task.Delay(100);

            loadingBarTargetProgress = 0.8f;

            await Task.Delay(100);

            loadingBarTargetProgress = 1.0f;
        }
        else
        {
            loadingBarTargetProgress = 0.6f;

            await Task.Delay(100);
            
            loadingBarTargetProgress = 0.8f;

            await LocalizationManager.Instance.Initialize(AccountService.Instance.LanguageCode);

            loadingBarTargetProgress = 1.0f;
        }

        //if (AccountService.Instance.IsCloudAvailable)
        //{
        //    if (AccountService.Instance.IsFirstLoad)
        //    {
        //        var allKeys = await DataManagementService.Instance.GetAllKeys();

        //        loadingBarTargetProgress = 0.6f;

        //        if (allKeys.Count == 0)
        //        {
        //            var saveData = new Dictionary<string, object>()
        //            {
        //                { DataManagementService.Key_NextGameId, "1" }
        //            };
        //            var resultData = await DataManagementService.Instance.SaveData(saveData);

        //            //DataReader.SetNextGameId(1);
        //            //DataReader.SetFirstLoadOnDevice();
        //        }
        //        else
        //        {
        //            var keys = new HashSet<string>() { DataManagementService.Key_NextGameId };
        //            var loadData = await DataManagementService.Instance.LoadData(keys);
        //            var number = loadData[DataManagementService.Key_NextGameId];
        //            DataReader.SetNextGameId(Convert.ToInt32(loadData[DataManagementService.Key_NextGameId]));
        //        }
        //    }

        //    loadingBarTargetProgress = 0.8f;

        //    await LocalizationManager.Instance.Initialize(AccountService.Instance.LanguageCode);

        //    loadingBarTargetProgress = 1.0f;
        //}
    }

    private void StartLoadingBarProgress()
    {
        StartCoroutine(UpdateLoadingBar());
    }

    private IEnumerator UpdateLoadingBar()
    {
        float currentProgress = loadingBarTargetProgress;

        while (currentProgress < 1)
        {
            yield return null;

            currentProgress = Mathf.MoveTowards(currentProgress, loadingBarTargetProgress, Time.deltaTime);

            loadingBar.SetFillPercentage(currentProgress);
        }

        loadingBar.Hide();
        StartMoveLogo();
    }

    public void StartMoveLogo()
    {
        StartCoroutine(MoveLogoSmoothly());
    }

    private IEnumerator MoveLogoSmoothly()
    {
        Vector2 startPos = logo.anchoredPosition;
        float elapsedTime = 0;

        while (elapsedTime < logoMoveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / logoMoveDuration);
            logo.anchoredPosition = Vector2.Lerp(startPos, logoTargetPosition, t);
            yield return null;
        }

        logo.anchoredPosition = logoTargetPosition;

        if (needsWelcome)
        {
            ChangeActivityToNext<WelcomeView>();
        }
        else
        {
            ChangeActivityToNext<MainMenuView>();
        }
    }
}

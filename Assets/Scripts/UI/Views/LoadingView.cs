using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    protected override void Start()
    {
        base.Start();

        InitializeServices();
    }

    protected async void InitializeServices()
    {
        var initialized = await SignInService.Instance.Initialize();

        if (initialized)
        {
            if (DataReader.GetFirstLoadOnDevice())
            {
                var allKeys = await DataManagementService.Instance.GetAllKeys();
                if (allKeys.Count == 0)
                {
                    var saveData = new Dictionary<string, object>()
                    {
                        { DataManagementService.Key_NextGameId, "1" }
                    };
                    var resultData = await DataManagementService.Instance.SaveData(saveData);

                    DataReader.SetNextGameId(1);
                    DataReader.SetFirstLoadOnDevice();
                }
                else
                {
                    var keys = new HashSet<string>() { DataManagementService.Key_NextGameId };
                    var loadData = await DataManagementService.Instance.LoadData(keys);
                    var number = loadData[DataManagementService.Key_NextGameId];
                    DataReader.SetNextGameId(Convert.ToInt32(loadData[DataManagementService.Key_NextGameId]));
                }
            }

            await LocalizationManager.Instance.Initialize(DataReader.GetLanguage());

            ChangeActivityToNext<MainMenuView>();
        }       
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using Zillneuron.Utilities;

namespace Zillneuron.UILayout
{
    public class ViewContext : MonoSingleton<ViewContext>
    {
        private bool isInitialized;

        private ACompleteView currentView;
        private ADialog currentDialog;
        private APopUp currentPopUp;
        private Dictionary<string, ACompleteView> views;
        private Dictionary<string, ADialog> dialogs;
        private Dictionary<string, APopUp> popUps;

        [SerializeField]
        private StartView startView;

        public bool IsInitialized { get { return isInitialized; } }

        // (Optional) Prevent non-singleton constructor use.
        protected ViewContext() { }

        public void Construct()
        {
            #region Views

            views = new Dictionary<string, ACompleteView>();
            ACompleteView[] viewComponents = FindObjectsOfType<ACompleteView>(true);
           
            foreach (ACompleteView component in viewComponents)
            {
                component.Construct();
                views.Add(component.GetType().Name, component);
            }

            currentView = views[startView.GetType().Name];
            currentView.Launch();

            #endregion Views

            #region Dialogs

            dialogs = new Dictionary<string, ADialog>();
            ADialog[] dialogComponents = FindObjectsOfType<ADialog>(true);

            foreach (ADialog component in dialogComponents)
            {
                component.Construct();
                dialogs.Add(component.GetType().Name, component);
            }

            #endregion Dialogs

            #region PopUps

            popUps = new Dictionary<string, APopUp>();
            APopUp[] popUpComponents = FindObjectsOfType<APopUp>(true);

            foreach (APopUp component in popUpComponents)
            {
                component.Construct();
                popUps.Add(component.GetType().Name, component);
            }

            #endregion PopUps

            isInitialized = true;
        }

        public T GetView<T>() where T : ACompleteView
        {
            return (T)views[typeof(T).Name];
        }

        public T GetDialog<T>() where T : ADialog
        {
            return (T)dialogs[typeof(T).Name];
        }

        public T GetPopUp<T>() where T : APopUp
        {
            return (T)popUps[typeof(T).Name];
        }

        public void ChangeView(string activityName)
        {
            currentView.Finish();
            currentView = views[activityName];
            currentView.Launch();
        }

        public void ChangeView<T>() where T : ACompleteView
        {
            currentView.Finish();
            currentView = views[typeof(T).Name];
            currentView.Launch();
        }

        public void EscapeView()
        {
            currentView.Escape();
        }

        public void PauseView()
        {
            currentView.Pause();
        }

        public void ResumeView()
        {
            currentView.Resume();
        }

        public void OpenDialog(string activityName, ADialogParameters parameters)
        {
            currentView.Pause();

            currentDialog = dialogs[activityName];
            currentDialog.SetUp(parameters);
            currentDialog.Launch();
        }

        public void OpenDialog<T>(ADialogParameters parameters) where T : ADialog
        {
            currentView.Pause();

            currentDialog = dialogs[typeof(T).Name];
            currentDialog.SetUp(parameters);
            currentDialog.Launch();
        }

        public void HideDialog(string activityName)
        {
            currentDialog = dialogs[activityName];
            currentDialog.Finish();

            currentView.Resume();
        }

        public void HideDialog<T>() where T : ADialog
        {
            currentDialog = dialogs[typeof(T).Name];
            currentDialog.Finish();

            currentView.Resume();
        }

        public void OpenPopUp(string activityName, APopUpParameters parameters)
        {
            currentPopUp = popUps[activityName];
            currentPopUp.SetUp(parameters);
            currentPopUp.Launch();
        }

        public void OpenPopUp<T>(APopUpParameters parameters) where T : APopUp
        {
            currentPopUp = popUps[typeof(T).Name];
            currentPopUp.SetUp(parameters);
            currentPopUp.Launch();
        }

        public void HidePopUp(string activityName)
        {
            currentPopUp = popUps[activityName];
            currentPopUp.Finish();
        }

        public void HidePopUp<T>() where T : APopUp
        {
            currentPopUp = popUps[typeof(T).Name];
            currentPopUp.Finish();
        }
    }
}
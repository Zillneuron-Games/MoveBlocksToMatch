using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Zillneuron.UILayout
{
    public abstract class ACompleteView : AView
    {
        protected static readonly Stack<string> previousActivityNames;

        protected static string PreviousActivity()
        {
            previousActivityNames.Pop();
            return previousActivityNames.Peek();
        }

        protected static void NextActivity(string activityName)
        {
            previousActivityNames.Push(activityName);
        }

        protected bool paused;

        static ACompleteView()
        {
            previousActivityNames = new Stack<string>();
        }

        public void Pause()
        {
            paused = true;
            OnPause();
        }

        public void Resume()
        {
            paused = false;
            OnResume();
        }

        public void Escape()
        {
            if (paused)
            {
                return;
            }

            OnEscape();
        }

        protected virtual void OnPause()
        {

        }

        protected virtual void OnResume()
        {
            UpdateViewElements();
        }

        protected virtual void OnEscape()
        {
            ChangeActivityToPrevious();
        }

        protected void ChangeActivityToRoot<T>() where T : ACompleteView
        {
            previousActivityNames.Clear();

            ChangeActivityToNext<T>();
        }

        protected void ChangeActivityToNextAndReplace<T>() where T : ACompleteView
        {
            previousActivityNames.Pop();

            ChangeActivityToNext<T>();
        }

        protected void ChangeActivityToNext<T>() where T : ACompleteView
        {
            string activityName = typeof(T).Name;

            ViewContext.Instance.ChangeView<T>();

            NextActivity(activityName);
        }

        protected void ChangeActivityToPrevious()
        {
            string activityName = PreviousActivity();

            ViewContext.Instance.ChangeView(activityName);
        }

        protected void OpenLoadingDialog(string message)
        {
            ADialogParameters parameters = LoadingDialog.CreateDialogParameters(message);
            ViewContext.Instance.OpenDialog<LoadingDialog>(parameters);
        }

        protected void CloseLoadingDialog()
        {
            ViewContext.Instance.HideDialog<LoadingDialog>();
        }

        protected void OpenAlertDialog(string message, string okButtonName, Action onClickOk)
        {
            ADialogParameters parameters = AlertDialog.CreateDialogParameters(message, okButtonName, onClickOk);
            ViewContext.Instance.OpenDialog<AlertDialog>(parameters);
        }

        protected void CloseAlertDialog()
        {
            ViewContext.Instance.HideDialog<AlertDialog>();
        }

        protected void OpenConfirmDialog(string message, string okButtonName, string cancelButtonName, Action onClickOk, Action cancelClickOk)
        {
            ADialogParameters parameters = ConfirmDialog.CreateDialogParameters(message, okButtonName, cancelButtonName, onClickOk, cancelClickOk);
            ViewContext.Instance.OpenDialog<ConfirmDialog>(parameters);
        }

        protected void CloseConfirmDialog()
        {
            ViewContext.Instance.HideDialog<ConfirmDialog>();
        }

        protected static void LogStack()
        {
            var result = "";
            var index = 1;
            var array = previousActivityNames.Reverse().ToArray();

            foreach (var x in array)
            {
                result += index.ToString();
                result += ". ";
                result += XTab(index);
                result += x;
                result += "\n";

                index++;
            }

            Debug.LogError(result);
        }

        private static string XTab(int count)
        {
            var result = "";

            for (int i = 0; i < count; i++)
            {
                result += "  ";
            }

            return result;
        }
    }
}


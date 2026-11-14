using System;
using UnityEngine;
using TMPro;

namespace Zillneuron.UILayout
{
    public class LoadingDialog : ADialog
    {
        protected string message;

        [SerializeField]
        protected TMP_Text messageText;

        public static ADialogParameters CreateDialogParameters(string message)
        {
            return new LoadingDialogParameters(message);
        }

        public override void SetUp(ADialogParameters parameters)
        {
            LoadingDialogParameters currentParameters = parameters as LoadingDialogParameters;
        }

        private class LoadingDialogParameters : ADialogParameters
        {
            private string message;

            public string Message => message;

            public LoadingDialogParameters(string message)
            {
                this.message = message;
            }
        }

    }
}

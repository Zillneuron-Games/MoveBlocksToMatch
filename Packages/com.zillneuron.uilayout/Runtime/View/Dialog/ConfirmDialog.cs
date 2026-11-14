using System;
using UnityEngine;
using TMPro;

namespace Zillneuron.UILayout
{
    public class ConfirmDialog : AlertDialog
    {
        protected Action onClickCancel;

        [SerializeField]
        protected TMP_Text cancelButtonText;

        public static ADialogParameters CreateDialogParameters(string message, string okButtonName, string cancelButtonName, Action onClickOk, Action onClickCancel)
        {
            return new ConfirmDialogParameters(message, okButtonName, cancelButtonName, onClickOk, onClickCancel);
        }

        public override void SetUp(ADialogParameters parameters)
        {
            ConfirmDialogParameters currentParameters = parameters as ConfirmDialogParameters;

            message = currentParameters.Message;
            onClickOk = currentParameters.OnClickOk;
            onClickCancel = currentParameters.OnClickCancel;

            messageText.text = message;
            okButtonText.text = currentParameters.OkButtonName;
            cancelButtonText.text = currentParameters.CancelButtonName;

        }

        public void OnClick_Cancel()
        {
            onClickCancel();
        }

        private class ConfirmDialogParameters : ADialogParameters
        {
            private string message;
            private string okButtonName;
            private string cancelButtonName;
            private Action onClickOk;
            private Action onClickCancel;

            public string Message => message;
            public string OkButtonName => okButtonName;
            public string CancelButtonName => cancelButtonName;
            public Action OnClickOk => onClickOk;
            public Action OnClickCancel => onClickCancel;

            public ConfirmDialogParameters(string message, string okButtonName, string cancelButtonName, Action onClickOk, Action onClickCancel)
            {
                this.message = message;
                this.okButtonName = okButtonName;
                this.cancelButtonName = cancelButtonName;
                this.onClickOk = onClickOk;
                this.onClickCancel = onClickCancel;
            }
        }
    }
}

using System;
using UnityEngine;
using TMPro;

namespace Zillneuron.UILayout
{
    public class AlertDialog : ADialog
    {
        protected string message;
        protected Action onClickOk;

        [SerializeField]
        protected TMP_Text messageText;

        [SerializeField]
        protected TMP_Text okButtonText;

        public string Message => message;

        public static ADialogParameters CreateDialogParameters(string message, string okButtonName, Action onClickOk)
        {
            return new AlertDialogParameters(message, okButtonName, onClickOk);
        }

        public override void SetUp(ADialogParameters parameters)
        {
            AlertDialogParameters currentParameters  = parameters as AlertDialogParameters;

            message = currentParameters.Message;
            onClickOk = currentParameters.OnClickOk;

            messageText.text = message;
            okButtonText.text = currentParameters.OkButtonName;
        }

        public void OnClick_Ok()
        {
            onClickOk();
        }

        private class AlertDialogParameters : ADialogParameters
        {
            private string message;
            private string okButtonName;
            private Action onClickOk;

            public string Message => message;
            public string OkButtonName => okButtonName;
            public Action OnClickOk => onClickOk;

            public AlertDialogParameters(string message, string okButtonName, Action onClickOk)
            {
                this.message = message;
                this.okButtonName = okButtonName;
                this.onClickOk = onClickOk;
            }
        }
    }
}

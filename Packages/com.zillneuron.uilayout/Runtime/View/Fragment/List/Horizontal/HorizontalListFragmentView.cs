using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Zillneuron.UILayout
{
    public class HorizontalListFragmentView : AListFragmentView
    {
        protected float scrollbarValue;

        [SerializeField]
        protected Scrollbar scrollbar;

        protected override bool ListEndReached { get { return viewPosition.x > 1.0f; } }

        public void CorrectScrollbarPoistion()
        {
            var selectedEelement = GetSelectedElements().OrderBy(x => x.Number).FirstOrDefault();
            if (selectedEelement != null)
            {
                float count = elementComponents.Count;
                float index = selectedEelement.Number;

                scrollbarValue = index / count;
            }
        }

        private void OnEnable()
        {
            StartCoroutine(SetScrollbarPosition());
        }

        private IEnumerator SetScrollbarPosition()
        {
            yield return new WaitForFixedUpdate();
            scrollbar.value = scrollbarValue;
        }
    }
}


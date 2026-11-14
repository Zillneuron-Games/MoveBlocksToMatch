using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Zillneuron.UILayout
{
    public abstract class AListFragmentView : AFragmentView
    {
        #region Members

        protected event Action<string> onElementOpened;
        protected event Action<string> onElementSelected;
        protected event Action<string> onElementDeselected;
        protected event Action onListEndReached;

        protected bool isMultiSelectable;
        protected Vector2 viewPosition;
        public bool IsMultiSelectable { get { return isMultiSelectable; } }
        public int ElementsCount { get { return elementComponents.Count; } }
        public List<AListElementFragmentView> Elements { get { return elementComponents.Values.ToList(); } }

        protected GameObject prefab;
        protected List<string> ids;
        protected Dictionary<string, AListElementFragmentView> elementComponents = new Dictionary<string, AListElementFragmentView>();

        [SerializeField]
        protected GameObject holderPanel;

        #endregion

        #region Override 

        protected override void LaunchFragments()
        {
            if (elementComponents != null)
            {
                foreach (var element in elementComponents)
                {
                    element.Value.Launch();
                }
            }

            base.OnLaunch();
        }

        protected override void FinishFragments()
        {
            base.OnFinish();

            if (elementComponents != null)
            {
                foreach (var element in elementComponents)
                {
                    element.Value.Finish();
                }
            }
        }

        #endregion

        public virtual void Initialize(bool isMultiSelectable, GameObject prefab, List<string> elementIds)
        {
            this.isMultiSelectable = isMultiSelectable;
            this.prefab = prefab;
            this.ids = elementIds;
        }

        public virtual void Create()
        {
            Clear();
            CreateElements();
        }

        public virtual void Create(List<string> elementIds)
        {
            ids.AddRange(elementIds);
            CreateElements();
        }

        protected void CreateElements()
        {
            for (int i = 0; i < ids.Count; i++)
            {
                if (!elementComponents.ContainsKey(ids[i]))
                {
                    var element = Instantiate(prefab);

                    element.transform.SetParent(holderPanel.transform, false);
                    element.transform.SetAsLastSibling();

                    var component = element.GetComponent<AListElementFragmentView>();

                    component.Construct();
                    component.Initialize(i, ids[i]);
                    component.AddOnClickSelectHandler(OnElementClickSelectHandler);
                    component.AddOnClickOpenHandler(OnElementClickOpenHandler);
                    component.Launch();

                    elementComponents.Add(ids[i], component);
                }
            }
        }

        public void SelectElement(string elementId)
        {
            var element = GetElement(elementId);
            element.OnClick_Select();
        }

        public void SelectAll()
        {
            foreach (var element in elementComponents.Values)
            {
                element.OnClick_Select();
            }
        }

        public AListElementFragmentView GetElement(string id)
        {
            return elementComponents[id];
        }

        public List<AListElementFragmentView> GetElements()
        {
            var keys = elementComponents.Select(x => x.Key).ToList(); ;
            return GetElements(keys);
        }

        public List<AListElementFragmentView> GetSelectedElements()
        {
            var keys = elementComponents.Where(x => x.Value.Selected).Select(x => x.Key).ToList(); ;
            return GetElements(keys);
        }

        public List<AListElementFragmentView> GetDeselectedElements()
        {
            var keys = elementComponents.Where(x => !x.Value.Selected).Select(x => x.Key).ToList(); ;
            return GetElements(keys);
        }

        private List<AListElementFragmentView> GetElements(List<string> ids)
        {
            var elements = new List<AListElementFragmentView>();

            foreach (var id in ids)
            {
                elements.Add(elementComponents[id]);
            }

            return elements;
        }

        public virtual void AddOnElementOpenedHandler(Action<string> handler)
        {
            onElementOpened -= handler;
            onElementOpened += handler;
        }

        public virtual void RemoveOnElementOpenedHandler(Action<string> handler)
        {
            onElementOpened -= handler;
        }

        public virtual void AddOnElementSelectedHandler(Action<string> handler)
        {
            onElementSelected -= handler;
            onElementSelected += handler;
        }

        public virtual void RemoveOnElementSelectedHandler(Action<string> handler)
        {
            onElementSelected -= handler;
        }

        public virtual void AddOnElementDeselectedHandler(Action<string> handler)
        {
            onElementDeselected -= handler;
            onElementDeselected += handler;
        }

        public virtual void RemoveOnElementDeselectedHandler(Action<string> handler)
        {
            onElementDeselected -= handler;
        }

        public virtual void AddOnListEndReachedHandler(Action handler)
        {
            onListEndReached -= handler;
            onListEndReached += handler;
        }

        public virtual void RemoveOnListEndReachedHandler(Action handler)
        {
            onListEndReached -= handler;
        }

        protected virtual void Clear()
        {
            foreach (var ec in elementComponents)
            {
                ec.Value.ClearOnClickOpenHandlers();
                Destroy(ec.Value.gameObject);
            }

            elementComponents.Clear();
        }

        protected virtual void OnElementClickSelectHandler(object sender, string id)
        {
            if (!IsMultiSelectable)
            {
                var selectedElementIds = elementComponents.Where(x => x.Value.Selected).Select(x => x.Key).ToList();

                foreach (var selectedElementId in selectedElementIds)
                {
                    elementComponents[selectedElementId].Deselect();
                }
            }

            if (elementComponents[id].Selected)
            {
                elementComponents[id].Deselect();
                if (onElementDeselected != null)
                {
                    onElementDeselected(id);
                }
            }
            else
            {
                elementComponents[id].Select();
                if (onElementSelected != null)
                {
                    onElementSelected(id);
                }
            }
        }

        protected virtual void OnElementClickOpenHandler(object sender, string id)
        {
            if (onElementOpened != null)
            {
                onElementOpened(id);
            }
        }

        protected abstract bool ListEndReached { get; }

        public void OnValueChanged(Vector2 viewPosition)
        {
            this.viewPosition = viewPosition;
        }

        public void OnEndDrag()
        {
            if (ListEndReached)
            {
                if (onListEndReached != null)
                {
                    onListEndReached();
                }
            }
        }
    }
}

using System;

namespace Zillneuron.UILayout
{
    public abstract class AListElementFragmentView : AFragmentView
    {
        protected event EventHandler<string> onClickSelectHandler;
        protected event EventHandler<string> onClickOpenHandler;

        public int Number { get; protected set; }

        public bool Selected { get; protected set; }

        public string Id { get; protected set; }

        public abstract void Initialize(int number, string id);

        public virtual void Select()
        {
            Selected = true;
        }

        public virtual void Deselect()
        {
            Selected = false;
        }

        public void AddOnClickOpenHandler(EventHandler<string> handler)
        {
            onClickOpenHandler -= handler;
            onClickOpenHandler += handler;
        }

        public void ClearOnClickOpenHandlers()
        {
            onClickOpenHandler = null;
        }

        public void OnClick_Open()
        {
            if (onClickOpenHandler != null)
            {
                onClickOpenHandler(this, Id);
            }
        }

        public void AddOnClickSelectHandler(EventHandler<string> handler)
        {
            onClickSelectHandler -= handler;
            onClickSelectHandler += handler;
        }

        public void ClearOnClickSelectHandlers()
        {
            onClickSelectHandler = null;
        }

        public void OnClick_Select()
        {
            if (onClickSelectHandler != null)
            {
                onClickSelectHandler(this, Id);
            }
        }
    }
}


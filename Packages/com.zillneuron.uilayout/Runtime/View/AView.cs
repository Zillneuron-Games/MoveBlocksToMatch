using System;
using UnityEngine;

namespace Zillneuron.UILayout
{
    public abstract class AView : MonoBehaviour
    {
        private bool initialized;

        public bool Initialized { get { return initialized; } }

        #region Set Up 

        private void Initialize()
        {
            initialized = true;

            OnInitialize();
        }

        public void Construct()
        {
            initialized = false;

            ConstructFragments();
            OnConstruct();
        }

        public void Launch()
        {
            if (!initialized)
            {
                Initialize();
            }

            LaunchFragments();

            UpdateViewElements();

            OnLaunch();
        }

        public void Finish()
        {
            FinishFragments();

            OnFinish();
        }

        public void Destruct()
        {
            DestructFragments();
            OnDestruct();
        }

        #endregion

        #region Set Up Fragments

        protected virtual void ConstructFragments()
        {

        }

        protected virtual void LaunchFragments()
        {

        }

        protected virtual void FinishFragments()
        {

        }

        protected virtual void DestructFragments()
        {

        }

        protected virtual void SetTexts()
        {

        }

        protected virtual void SetColors()
        {

        }

        #endregion

        #region Events

        protected virtual void OnConstruct()
        {

        }

        protected virtual void OnInitialize()
        {

        }

        protected virtual void OnLaunch()
        {

        }

        protected virtual void OnFinish()
        {

        }

        protected virtual void OnDestruct()
        {

        }

        #endregion

        protected void UpdateViewElements()
        {
            SetTexts();
            SetColors();
        }
    }
}

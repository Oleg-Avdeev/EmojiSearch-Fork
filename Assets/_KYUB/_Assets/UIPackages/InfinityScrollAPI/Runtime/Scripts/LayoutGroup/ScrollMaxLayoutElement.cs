﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Kyub.UI
{
    [ExecuteInEditMode]
    public class ScrollMaxLayoutElement : MaxLayoutElement
    {
        #region Private Variables

        bool _isVisible = false;
        protected int _cachedIndex = -1;
        protected ScrollLayoutGroup _cachedScrollLayout = null;

        #endregion

        #region Callbacks

        public UnityEvent OnBecameVisible = new UnityEvent();
        public UnityEvent OnBecameInvisible = new UnityEvent();

        #endregion

        #region Public Properties

        public int LayoutElementIndex
        {
            get
            {
                if (!Application.isPlaying && ScrollLayoutGroup != null && _cachedIndex < 0)
                    return ScrollLayoutGroup.FindElementIndex(this.gameObject);

                return _cachedIndex;
            }
            protected internal set
            {
                if (_cachedIndex == value)
                    return;
                _cachedIndex = value;
            }
        }

        public ScrollLayoutGroup ScrollLayoutGroup
        {
            get
            {
                if (_cachedScrollLayout == null)
                    _cachedScrollLayout = ScrollLayoutGroup.GetComponentInParent<ScrollLayoutGroup>(this, true);
                return _cachedScrollLayout;
            }
            protected set
            {
                if (_cachedScrollLayout == value)
                    return;
                UnregisterEvents();
                _cachedScrollLayout = value;
                if (enabled && gameObject.activeInHierarchy)
                    RegisterEvents();
            }
        }

        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
        }


        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            if (ScrollLayoutGroup != null)
                RegisterEvents();
            base.OnEnable();
            if (_started && gameObject.activeInHierarchy && enabled)
            {
                //ApplyElementSize();
                //Force recalculate element
                if (!_previousActiveSelf && ScrollLayoutGroup != null)
                    ScrollLayoutGroup.SetCachedElementsLayoutDirty();
            }

            _previousActiveSelf = gameObject.activeSelf;
        }

        bool _started = false;
        protected override void Start()
        {
            base.Start();
            _started = true;
            //SetElementSizeDirty();
        }

        bool _previousActiveSelf = false;
        protected override void OnDisable()
        {
            //_applyElementSizeRoutine = null;
            CancelInvoke();
            UnregisterEvents();
            base.OnDisable();

            //Recalculate element size ignoring this element position (object disabled)
            if (!gameObject.activeSelf && ScrollLayoutGroup != null)
                ScrollLayoutGroup.SetCachedElementsLayoutDirty();

            _previousActiveSelf = gameObject.activeSelf;
        }

        /*protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (enabled)
                ApplyElementSize();
        }*/

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            ScrollLayoutGroup = ScrollLayoutGroup.GetComponentInParent<ScrollLayoutGroup>(this, true);
            SetDirty();
        }

        #endregion

        #region Helper Functions

        public virtual void SetElementSizeDirty()
        {
            if (!IsInvoking("ApplyElementSize") && LayoutElementIndex >= 0 && _isVisible)
                Invoke("ApplyElementSize", 0);
        }

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
            if (_cachedScrollLayout != null)
                _cachedScrollLayout.OnElementBecameInvisible.AddListener(HandleOnBecameInvisible);
            if (_cachedScrollLayout != null)
                _cachedScrollLayout.OnElementBecameVisible.AddListener(HandleOnBecameVisible);
        }

        protected virtual void UnregisterEvents()
        {
            if (_cachedScrollLayout != null)
                _cachedScrollLayout.OnElementBecameInvisible.RemoveListener(HandleOnBecameInvisible);
            if (_cachedScrollLayout != null)
                _cachedScrollLayout.OnElementBecameVisible.RemoveListener(HandleOnBecameVisible);
        }

        protected void ApplyElementSize()
        {
            //_applyElementSizeRoutine = null;
            if (ScrollLayoutGroup != null && LayoutElementIndex >= 0 && _isVisible)
            {
                ScrollLayoutGroup.SetCachedElementSize(LayoutElementIndex, ScrollLayoutGroup.CalculateElementSize(transform, ScrollLayoutGroup.IsVertical()));
            }
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnBecameInvisible(GameObject p_object, int p_index)
        {
            if (p_object == this.gameObject)
            {
                _isVisible = false;
                LayoutElementIndex = p_index;
                if (OnBecameInvisible != null)
                    OnBecameInvisible.Invoke();
            }
            else if (LayoutElementIndex >= 0 && p_index == LayoutElementIndex)
            {
                _isVisible = false;
                LayoutElementIndex = -1;
            }
        }

        protected virtual void HandleOnBecameVisible(GameObject p_object, int p_index)
        {
            if (p_object == this.gameObject)
            {
                _isVisible = true;
                LayoutElementIndex = p_index;
                if (OnBecameVisible != null)
                    OnBecameVisible.Invoke();
            }
            else if (LayoutElementIndex >= 0 && p_index == LayoutElementIndex)
            {
                _isVisible = false;
                LayoutElementIndex = -1;
            }
        }

        #endregion

        #region Override

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            if (ScrollLayoutGroup != null && !ScrollLayoutGroup.IsVertical())
                ApplyElementSize();
        }

        public override void CalculateLayoutInputVertical()
        {
            base.CalculateLayoutInputVertical();
            if (ScrollLayoutGroup != null && ScrollLayoutGroup.IsVertical())
                ApplyElementSize();
        }

        #endregion
    }
}

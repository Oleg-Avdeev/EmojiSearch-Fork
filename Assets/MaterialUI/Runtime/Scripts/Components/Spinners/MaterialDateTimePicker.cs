﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaterialUI
{
    public class MaterialDateTimePicker : BaseSpinner<EmptyStyleProperty>
    {
        #region Helper Classes

        public enum DateTimeMode { Date, Time, DateTime, Month }

        [System.Serializable]
        public class DateUnityEvent : UnityEvent<System.DateTime> { }
        [System.Serializable]
        public class StrUnityEvent : UnityEvent<string> { }

        #endregion

        #region Static Vars

        static DialogDatePicker s_MonthPicker = null;
        static DialogDatePicker s_DatePicker = null;
        static DialogTimePicker s_TimePicker = null;

        #endregion

        #region Private Variables

        [Header("DateTime Fields")]
        [SerializeField]
        protected DateTimeMode m_PickerMode = DateTimeMode.Date;
        [Space]
        [SerializeField, Tooltip("Use this to return current string date format\n-'d' will display dd/MM/aaaa ,\n-'t' will display HH:mm ,\n-'g' will display full date and time")]
        protected string m_DateFormat = "d";
        [SerializeField]
        protected string m_CultureInfo = "current";

        [Space]
        [SerializeField]
        protected string m_CurrentFormattedDate = "";
        [Space]
        [SerializeField, SerializeStyleProperty]
        protected Color m_DialogColor = MaterialColor.teal500;

        [Space]
        [SerializeField, Tooltip("Will always apply hint text or will only show hint when date is invalid?")]
        protected bool m_AlwaysDisplayHintText = false;
        [SerializeField]
        protected string m_HintText = "";

        [Space]
        [SerializeField, SerializeStyleProperty]
        protected Graphic m_HintTextComponent = null;
        [SerializeField, SerializeStyleProperty, UnityEngine.Serialization.FormerlySerializedAs("m_ButtonTextContent")]
        protected Graphic m_TextComponent = null;
        [SerializeField, SerializeStyleProperty, UnityEngine.Serialization.FormerlySerializedAs("m_ButtonImageContent")]

        #endregion

        #region Callback

        [Header("DateTime Callbacks")]
        public DateUnityEvent OnDateTimeChangedCallback;
        public StrUnityEvent OnFormattedDateTimeChangedCallback;

        #endregion

        #region Public Properties

        public string hintText
        {
            get
            {
                if (m_HintText == null)
                    m_HintText = "";
                return m_HintText;
            }
            set
            {
                if (m_HintText == value)
                    return;
                m_HintText = value;
                UpdateLabelState();
            }
        }

        public virtual Graphic hintTextComponent
        {
            get
            {
                return m_HintTextComponent;
            }
        }

        public virtual Graphic textComponent
        {
            get
            {
                return m_TextComponent;
            }
        }

        public virtual Color textComponentColor
        {
            get
            {
                return textComponent != null ? textComponent.color : Color.black;
            }

            set
            {
                if (textComponent != null && textComponent.color != value)
                    textComponent.color = value;
            }
        }

        public Color dialogColor
        {
            get
            {
                return m_DialogColor;
            }

            set
            {
                m_DialogColor = value;
            }
        }

        public string CurrentFormattedDate
        {
            get
            {
                return m_CurrentFormattedDate;
            }

            set
            {
                if (m_CurrentFormattedDate == value)
                    return;

                //We only call full callbacks if value is valid
                System.DateTime date = System.DateTime.MinValue;
                if (TryParseDate(value, m_DateFormat, GetCultureInfo(), out date))
                    HandleOnChangeDateTime(date);
                else
                {
                    //Only call FormattedDate callback
                    m_CurrentFormattedDate = value;
                    UpdateLabelState();
                    if (OnFormattedDateTimeChangedCallback != null)
                        OnFormattedDateTimeChangedCallback.Invoke(m_CurrentFormattedDate);

                }
            }
        }

        public string DateFormat
        {
            get
            {
                return m_DateFormat;
            }

            set
            {
                var v_value = string.IsNullOrEmpty(value) ? "" : value;
                if (m_DateFormat == v_value)
                    return;

                m_CurrentFormattedDate = HasValidDate()? GetCurrentDate().ToString(v_value, GetCultureInfo()) : "";
                m_DateFormat = v_value;
                HandleOnChangeDateTime(m_CurrentFormattedDate);
            }
        }

        public string CultureInfo
        {
            get
            {
                return m_CultureInfo;
            }

            set
            {
                var v_value = string.IsNullOrEmpty(value) ? "" : value;
                if (m_CultureInfo == v_value)
                    return;
                m_CurrentFormattedDate = HasValidDate() ? GetCurrentDate().ToString(v_value, GetCultureInfo()) : "";
                m_CultureInfo = v_value;
                HandleOnChangeDateTime(m_CurrentFormattedDate);
            }
        }

        public DateTimeMode PickerMode
        {
            get
            {
                return m_PickerMode;
            }

            set
            {
                m_PickerMode = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void Start()
        {
            base.Start();
            Init();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateLabelState();
            if (Application.isPlaying)
                HandleOnChangeDateTime(m_CurrentFormattedDate);
        }

#endif

        #endregion

        #region Overriden Functions

        public override bool IsExpanded()
        {
            return (s_DatePicker != null && s_DatePicker.gameObject.activeSelf) ||
                (s_MonthPicker != null && s_MonthPicker.gameObject.activeSelf) || 
                (s_TimePicker != null && s_TimePicker.gameObject.activeSelf);
        }

        public override void Show()
        {
            System.DateTime date = GetCurrentDate();

            var isTimePicker = m_PickerMode == DateTimeMode.Time || m_PickerMode == DateTimeMode.DateTime;
            var isDatePicker = m_PickerMode == DateTimeMode.Date || m_PickerMode == DateTimeMode.DateTime;
            var isMonthPicker = m_PickerMode == DateTimeMode.Month;

            if (isDatePicker || isMonthPicker)
            {
                //We must call changeTime after change date (if supported)
                System.Action<System.DateTime> onChangeDate = (pickedDate) =>
                {
                    HandleOnChangeDateKeepingLastTime(pickedDate);

                    pickedDate = GetCurrentDate();
                    if (isTimePicker)
                    {
                        ShowFrameActivity<DialogTimePicker>(s_TimePicker, PrefabManager.ResourcePrefabs.dialogTimePicker, 
                            (dialog, isDialog) => 
                            {
                                s_TimePicker = dialog;
                                dialog.Initialize(pickedDate, HandleOnChangeTimeKeepingLastDate, m_DialogColor);
                            });
                    }
                };

                //Show DatePicker
                if (isMonthPicker)
                {
                    ShowFrameActivity<DialogDatePicker>(s_MonthPicker, PrefabManager.ResourcePrefabs.dialogMonthPicker,
                                (dialog, isDialog) =>
                                {
                                    s_MonthPicker = dialog;
                                    dialog.Initialize(date.Year, date.Month, date.Day, onChangeDate, HandleOnHide, m_DialogColor);
                                    dialog.SetCultureInfo(GetCultureInfo());
                                });
                }
                else
                {
                    ShowFrameActivity<DialogDatePicker>(s_DatePicker, PrefabManager.ResourcePrefabs.dialogDatePicker,
                                (dialog, isDialog) =>
                                {
                                    s_DatePicker = dialog;
                                    dialog.Initialize(date.Year, date.Month, date.Day, onChangeDate, HandleOnHide, m_DialogColor);
                                    dialog.SetCultureInfo(GetCultureInfo());
                                });
                } 
            }

            //Show Time Picker Only
            if (isTimePicker && m_PickerMode != DateTimeMode.DateTime)
            {
                ShowFrameActivity<DialogTimePicker>(s_TimePicker, PrefabManager.ResourcePrefabs.dialogTimePicker,
                            (dialog, isDialog) =>
                            {
                                s_TimePicker = dialog;
                                dialog.Initialize(date, HandleOnChangeDateTime, m_DialogColor);
                            });
            }
        }

        public override void Hide()
        {
            if (s_MonthPicker != null && s_MonthPicker.gameObject.activeSelf)
                s_MonthPicker.Hide();
            if (s_DatePicker != null && s_DatePicker.gameObject.activeSelf)
                s_DatePicker.Hide();
            if (s_TimePicker != null && s_TimePicker.gameObject.activeSelf)
                s_TimePicker.Hide();

            HandleOnHide();
        }

        #endregion

        #region Public Helper Functions

        public void SetPickerMode(int pickerMode)
        {
            var mode = (DateTimeMode)pickerMode;
            if (mode != DateTimeMode.Date &&
                mode != DateTimeMode.Time &&
                mode != DateTimeMode.DateTime &&
                mode != DateTimeMode.Month)
            {
                mode = DateTimeMode.Date;
            }

            PickerMode = mode;
        }

        public bool HasValidDate()
        {
            return IsValidDate(m_CurrentFormattedDate, m_DateFormat, GetCultureInfo());
        }

        public void SetCurrentDate(System.DateTime p_value)
        {
            HandleOnChangeDateTime(p_value);
        }

        public System.DateTime GetCurrentDate()
        {
            return ParseDate(m_CurrentFormattedDate, m_DateFormat, GetCultureInfo());
        }

        public override void RefreshVisualStyles(bool p_canAnimate = true)
        {
            SetStylePropertyColorsActive_Internal(p_canAnimate, 0);
        }

        #endregion

        #region Internal Helper Functions

        protected override void Init()
        {
            //Call ChangeDateTime Initial Callback
            m_CurrentFormattedDate = m_CurrentFormattedDate.Trim();
            if (m_CurrentFormattedDate == "*" || string.Equals(m_CurrentFormattedDate, "now", System.StringComparison.CurrentCultureIgnoreCase))
            {
                HandleOnChangeDateTime(System.DateTime.Now);
            }
            else if (string.Equals(m_CurrentFormattedDate, "utcnow", System.StringComparison.CurrentCultureIgnoreCase))
            {
                HandleOnChangeDateTime(System.DateTime.UtcNow);
            }
            else if (string.Equals(m_CurrentFormattedDate, "min", System.StringComparison.CurrentCultureIgnoreCase))
            {
                HandleOnChangeDateTime(System.DateTime.MinValue);
            }
            else
            {
                HandleOnChangeDateTime(m_CurrentFormattedDate);
            }
        }

        protected virtual void UpdateLabelState()
        {
            bool isValidData = IsValidDate(m_CurrentFormattedDate, m_DateFormat, GetCultureInfo());
            if (textComponent != null)
                textComponent.SetGraphicText(isValidData ? m_CurrentFormattedDate : "\u200B");

            //Apply Hint Text
            var hintTextStr = !isValidData || m_AlwaysDisplayHintText ? this.hintText : null;
            if (hintTextComponent != null && (textComponent != hintTextComponent || (!isValidData && hintTextStr != null)))
                hintTextComponent.SetGraphicText(!string.IsNullOrEmpty(hintTextStr) ? hintTextStr : "\u200B");

#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public System.Globalization.CultureInfo GetCultureInfo()
        {
            if (m_CultureInfo == null)
                m_CultureInfo = "";

            if (string.IsNullOrEmpty(m_CultureInfo) ||
                string.Equals(m_CultureInfo, "invariant", System.StringComparison.CurrentCultureIgnoreCase) ||
                string.Equals(m_CultureInfo, "invariantculture", System.StringComparison.CurrentCultureIgnoreCase))
            {
                return System.Globalization.CultureInfo.InvariantCulture;
            }
            else if (string.Equals(m_CultureInfo, "*", System.StringComparison.CurrentCultureIgnoreCase) ||
                string.Equals(m_CultureInfo, "current", System.StringComparison.CurrentCultureIgnoreCase) ||
                string.Equals(m_CultureInfo, "currentculture", System.StringComparison.CurrentCultureIgnoreCase))
            {
                return System.Globalization.CultureInfo.CurrentCulture;
            }
            else
            {
                try
                {
                    return new System.Globalization.CultureInfo(m_CultureInfo, false);
                }
                catch
                {
                    //Culture not found, revert to empty
                    Debug.Log("Culture '" + m_CultureInfo + "' not found (sender: " + name + ")");
                    m_CultureInfo = "";
                }
            }
            return System.Globalization.CultureInfo.InvariantCulture;
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnChangeTimeKeepingLastDate(System.DateTime date)
        {
            var currentDate = GetCurrentDate();

            date = new System.DateTime(currentDate.Year, currentDate.Month, currentDate.Day, date.Hour, date.Minute, date.Second, date.Millisecond);
            HandleOnChangeDateTime(date);
        }

        protected virtual void HandleOnChangeDateKeepingLastTime(System.DateTime date)
        {
            var currentDate = GetCurrentDate();

            date = new System.DateTime(date.Year, date.Month, date.Day, currentDate.Hour, currentDate.Minute, currentDate.Second, currentDate.Millisecond);
            HandleOnChangeDateTime(date);
        }

        protected virtual void HandleOnChangeDateTime(System.DateTime date)
        {
            HandleOnChangeDateTime(date.ToString(m_DateFormat, GetCultureInfo()));
        }

        protected virtual void HandleOnChangeDateTime(string dateFormatted)
        {
            System.DateTime date = System.DateTime.Now;
            TryParseDate(dateFormatted, m_DateFormat, GetCultureInfo(), out date);
            m_CurrentFormattedDate = dateFormatted;

            UpdateLabelState();
            if (OnDateTimeChangedCallback != null)
                OnDateTimeChangedCallback.Invoke(date);
            if (OnFormattedDateTimeChangedCallback != null)
                OnFormattedDateTimeChangedCallback.Invoke(m_CurrentFormattedDate);
        }

        #endregion

        #region Static Helper Functions

        public static bool IsValidDate(string dateFormatted, string format, System.Globalization.CultureInfo cultureInfo)
        {
            System.DateTime date = System.DateTime.MinValue;
            return TryParseDate(dateFormatted, format, cultureInfo, out date);
        }

        public static System.DateTime ParseDate(string dateStr, string format, System.Globalization.CultureInfo cultureInfo)
        {
            System.DateTime date = System.DateTime.Now;
            TryParseDate(dateStr, format, cultureInfo, out date);
            return date;
        }

        public static bool TryParseDate(string dateStr, string format, System.Globalization.CultureInfo cultureInfo, out System.DateTime date)
        {
            date = System.DateTime.Now;
            if (!string.IsNullOrEmpty(dateStr))
            {
                if (!System.DateTime.TryParseExact(dateStr, format, cultureInfo, System.Globalization.DateTimeStyles.None, out date))
                    return System.DateTime.TryParse(dateStr, cultureInfo, System.Globalization.DateTimeStyles.None, out date);
                else
                    return true;
            }
            return false;
        }

        #endregion

    }
}

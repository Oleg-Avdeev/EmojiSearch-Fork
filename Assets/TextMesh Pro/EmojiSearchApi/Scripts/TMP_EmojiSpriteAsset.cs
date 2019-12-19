﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TMPro
{
    public class TMP_EmojiSpriteAsset : TMP_SpriteAsset, ISerializationCallbackReceiver
    {
        #region Fields

#if UNITY_ANDROID || UNITY_EDITOR
        [Header("Platform Specific Fields")]
        public bool overrideAndroidDefinition = false;
        public Texture androidSpriteSheet;
#endif

#if UNITY_IOS || UNITY_EDITOR
        public bool overrideIOSDefinition = false;
        public Texture iOSSpriteSheet;
#endif

        #endregion

        #region Serialization Callback

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (overrideAndroidDefinition)
                spriteSheet = androidSpriteSheet;
            else
                androidSpriteSheet = null;
#endif
#if UNITY_IOS && !UNITY_EDITOR
            if (overrideIOSDefinition)
                spriteSheet = iOSSpriteSheet;
            else
                iOSSpriteSheet = null;
#endif

        }

        #endregion
    }
}

// Copyright @2021 COMCREATE. All rights reserved.

using System.Reflection;
using UnityEngine;

namespace UniSaga.Core
{
    internal static class UnityEnumeratorExtensions
    {
        private static FieldInfo _waitForSecondsSecondsFieldInfo;

        private static FieldInfo WaitForSecondsSecondsFieldInfo
        {
            get
            {
                if (_waitForSecondsSecondsFieldInfo != null) return _waitForSecondsSecondsFieldInfo;
                _waitForSecondsSecondsFieldInfo = typeof(WaitForSeconds).GetField("m_Seconds",
                    BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic);
                return _waitForSecondsSecondsFieldInfo;
            }
        }

        public static float GetSeconds(this WaitForSeconds self) =>
            (float)WaitForSecondsSecondsFieldInfo.GetValue(self);
    }
}
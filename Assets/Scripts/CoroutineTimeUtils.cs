using System.Collections.Generic;
using UnityEngine;

namespace Shmackle.Utils.CoroutinesTimer
{
    public static class CoroutineTimeUtils
    {
        private const int InitialCacheCapacity = 16;

        public static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();
        public static readonly WaitForFixedUpdate FixedUpdate = new WaitForFixedUpdate();
        public static readonly YieldInstruction WaitForZeroSeconds = null;

        private static readonly Dictionary<float, WaitForSeconds> _waitForSecondsCache =
            new Dictionary<float, WaitForSeconds>(InitialCacheCapacity, new FloatComparer());

        public static WaitForSeconds GetWaitForSeconds(float seconds)
        {
            if (seconds <= 0f)
            {
                return GetWaitForSecondsInternal(0f);
            }
            return GetWaitForSecondsInternal(seconds);
        }

        private static WaitForSeconds GetWaitForSecondsInternal(float seconds)
        {
            if (_waitForSecondsCache.TryGetValue(seconds, out WaitForSeconds waitInstruction))
            {
                return waitInstruction;
            }

            waitInstruction = new WaitForSeconds(seconds);
            _waitForSecondsCache.Add(seconds, waitInstruction);
            return waitInstruction;
        }

        public static void ClearWaitForSecondsCache()
        {
            _waitForSecondsCache.Clear();
            GetWaitForSecondsInternal(0f);
        }

        private class FloatComparer : IEqualityComparer<float>
        {
            private const float Epsilon = 0.00001f;

            public bool Equals(float x, float y)
            {
                return Mathf.Abs(x - y) < Epsilon;
            }

            public int GetHashCode(float obj)
            {
                return obj.GetHashCode();
            }
        }
    }
    /*
       yield return CoroutineTimeUtils.GetWaitForSeconds(1.5f);
       
       yield return CoroutineTimeUtils.EndOfFrame;
       
       yield return CoroutineTimeUtils.FixedUpdate;
       
       yield return CoroutineTimeUtils.GetWaitForSeconds(0f);
       
       yield return CoroutineTimeUtils.WaitForZeroSeconds;
       
     */
}
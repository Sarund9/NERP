using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NerpRuntime
{
    public static class NerpMath
    {
        
        public static Matrix4x4 TranslateTransform(
            Matrix4x4 targetLtW, Matrix4x4 fromWtL, Matrix4x4 toLtW)
        {
            return toLtW * fromWtL * targetLtW;
        }
    }
}

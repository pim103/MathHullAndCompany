using System;
using UnityEngine;

namespace Utils
{
    public class MathUtils
    {
        public static float AngleClockwise(Vector3 a, Vector3 b)
        {
            float det = a.x * b.y - a.y * b.x;
            float alpha = (float)Math.Atan2(det, Vector3.Dot(a, b));
            return alpha;
        }
    }
}
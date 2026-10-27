using System.Runtime.CompilerServices;
using UnityEngine;

namespace _LumenLib.Calculate.VectorCalculate
{
    public static class Vector2Calc
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 StartToTarget(Vector2 start, Vector2 target)
        {
            Vector2 direction = target - start;
            direction.Normalize();
            return direction;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TargetInDistance(Vector2 start, Vector2 target, float radius)
        {
            return radius >= 0f && StartToTarget(start, target).sqrMagnitude <= radius * radius; 
        }


        private const float Tolerance = 0.001f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Approximately(Vector2 a, Vector2 b)
        {
            return StartToTarget(a, b).sqrMagnitude <= Tolerance * Tolerance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TargetIsFront(Vector2 myPos, Vector2 facingDir, Vector2 target)
        {
            Vector2 toTarget = StartToTarget(myPos, target);
            float dot = Vector2.Dot(facingDir, toTarget);

            return dot > 0f;
        }
    }
}
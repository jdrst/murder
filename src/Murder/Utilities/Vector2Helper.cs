﻿using System.Numerics;

namespace Murder.Utilities
{
    public static class Vector2Helper
    {
        private static readonly Vector2 _center = new(0.5f, 0.5f);
        private static readonly Vector2 _down = new(0, 1f);
        private static readonly Vector2 _up = new(0, -1f);

        public static Vector2 Center => _center;
        public static Vector2 Down => _down;
        public static Vector2 Up => _up;

        public static Vector2 LerpSnap(Vector2 origin, Vector2 target, float factor, float threshold = 0.01f) =>
            new(Calculator.LerpSnap(origin.X, target.X, factor, threshold),
                Calculator.LerpSnap(origin.Y, target.Y, factor, threshold));

        public static Vector2 LerpSnap(Vector2 origin, Vector2 target, double factor, float threshold = 0.01f) =>
            new((float)Calculator.LerpSnap(origin.X, target.X, factor, threshold),
                (float)Calculator.LerpSnap(origin.Y, target.Y, factor, threshold));

        ///<summary>
        /// Calculates the internal angle of a triangle.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static float CalculateAngle(Vector2 a, Vector2 b, Vector2 c)
        {
            // Calculate the vectors AB and AC.
            Vector2 v1 = b - a;
            Vector2 v2 = c - a;

            // Calculate the dot product of the vectors.
            float dot = Vector2.Dot(v1, v2);

            // Calculate the cross product of the vectors.
            float cross = v1.X * v2.Y - v1.Y * v2.X;

            // Return the angle in radians.
            return (float)Math.Atan2(cross, dot);
        }

        /// <summary>
        /// Creates a vector from an angle in radians.
        /// </summary>
        /// <param name="angle">Angle in radians</param>
        /// <returns></returns>
        public static Vector2 FromAngle(float angle)
        {
            return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }

        public static float Deviation(Vector2 vec1, Vector2 vec2)
        {
            // Calculate the dot product
            float dotProduct = Vector2.Dot(vec1.Normalized(), vec2.Normalized());

            // Cosine values range from -1 to 1, mapping it to 0-1
            float deviation = (dotProduct + 1) / 2;

            return 1 - deviation;
        }
    }
}
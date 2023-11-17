using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;

namespace HolePunching.HolePunch
{
    internal class Edge (Vector2 v1, Vector2 v2)
    {
        public Vector2 v1 = v1;
        public Vector2 v2 = v2;
        protected Vector2 segment = v2 - v1;

        public Edge Intersect(Edge other)
        {
            if (!InBoundingBox(other))
                return null;
            if (Parallel(other))
                return ParallelIntersect(other);
            //TODO: check for intersection in non parallel case
        }
        private bool InBoundingBox(Edge other)
        {
            return Intersect1D(v1.X, v2.X, other.v1.X, other.v2.X) && Intersect1D(v1.Y, v2.Y, other.v1.Y, other.v2.Y);
        }
        private static bool Intersect1D(float a1, float a2, float b1, float b2)
        {
            if (a1 > a2)
                (a1, a2) = (a2, a1);
            if (b1 > b2)
                (b1, b2) = (b2, b1);
            return Math.Max(a1, b1) <= Math.Min(a2, b2);
        }
        private Edge ParallelIntersect(Edge other)
        {
            Vector2 q1 = other.v1;
            Vector2 q2 = other.v2;
            if(Vector2.Dot(other.v1, segment) > Vector2.Dot(other.v2, segment))
                (q2, q1) = (q1, q2);
            Vector2 w1 = v1;
            if (Vector2.Dot(q1, segment) > Vector2.Dot(v1, segment))
                w1 = q1;
            Vector2 w2 = v2;
            if(Vector2.Dot(q2, segment) < Vector2.Dot(v2, segment))
                w2 = q2;
            return new Edge(w1, w2);
        }
        protected bool Parallel(Edge other)
        {
            return Math.Abs(segment.X * other.segment.Y) == Math.Abs(segment.Y * other.segment.X);
        }
        public bool IsPoint()
        {
            return v1 == v2;
        }
    }
}

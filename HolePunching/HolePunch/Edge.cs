using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;

namespace HolePunching.HolePunch
{
    struct Line
    {
        //line of form ax + by + c = 0
        public float a,b,c;
        public Line(Vector2 p1, Vector2 p2)
        {
            a = p1.Y - p2.Y;
            b = p1.X - p2.X;
            c = -a * p1.X - b * p1.Y;
        }
        //Cramer's rule. Note this breaks for parallel lines.
        public readonly Vector2 Intersect(Line other)
        {
            float denominator = Det(a, b, other.a, other.b);
            float x = -Det(c, b, other.c, other.b) / denominator;
            float y = -Det(a, c, other.a, other.c) / denominator;
            return new Vector2(x, y);
        }
        private static float Det(float a, float b, float c, float d)
        {
            return a * d - c * b;
        }
    }
    internal class Edge 
    {
        public Vector2 v1;
        public Vector2 v2;
        //perpendicular is normal to edge, but may not have magnitude 1
        public Vector2 Perpendicular;
        protected Vector2 segment;
        protected Line line;
        static readonly float ERR = 1e-9f;
        public Edge(Vector2 v1, Vector2 v2)
        {
            this.v1 = v1;
            this.v2 = v2;
            segment = v2 - v1;
            Perpendicular = new Vector2(segment.Y, -segment.X);
            line = new(v1, v2);
        }

        public Edge Intersect(Edge other)
        {
            if (!InBoundingBox(other))
                return null;
            if (Parallel(other))
                return ParallelIntersect(other);

            Vector2 intersection = line.Intersect(other.line);
            if (Vector2.Dot(intersection, segment) < Vector2.Dot(v1, segment) - ERR || Vector2.Dot(intersection, segment) > Vector2.Dot(v2, segment) + ERR)
                return null;
            return new Edge(intersection, intersection);
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
            return Math.Max(a1, b1) <= Math.Min(a2, b2) + ERR;
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
            return segment.LengthSquared() <= ERR*ERR;
        }
    }
}

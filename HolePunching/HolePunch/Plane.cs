using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using Stride.Core.Mathematics;

namespace HolePunching.HolePunch
{
    public struct Plane
    {
        public Vector3 origin;
        public Vector3 normal, unitX, unitY;
        //bootleg zero
        public const float O = 1e-9f;
        public Plane(Vector3 origin, Vector3 normal)
        {
            this.origin = origin;
            this.normal = normal;
            normal.Normalize();
            Vector3 nonparallel = normal.X == 0 && normal.Y == 0 ? normal + Vector3.UnitX : normal + Vector3.UnitZ;
            unitX = Vector3.Cross(normal, nonparallel);
            unitY = Vector3.Cross(normal, unitX);
            unitX.Normalize();
            unitY.Normalize();
        }
        public Plane(Vector3 origin, Vector3 unitX, Vector3 unitY)
        {
            this.origin = origin;
            this.unitX = unitX;
            this.unitY = unitY;
            normal = Vector3.Cross(this.unitX, this.unitY);
        }

        public readonly Vector2 Project(Vector3 point)
        {
            Vector3 diff = point - origin;
            float dist = Vector3.Dot(diff, normal);
            //planePoint is nearest point on plane to point
            Vector3 planePoint = point - dist * normal;
            //Now project onto unit plane XY vectors to get local coords. These unit vectors are arbitrary but consistent for each plane.
            return ToPlaneSpace(planePoint);
        }
        public readonly Vector2 ToPlaneSpace(Vector3 point)
        {
            float px = ProjDist(unitX, point - origin);
            float py = ProjDist(unitY, point - origin);
            return new Vector2(px, py);
        }
        public readonly Vector3 ToWorldSpace(Vector2 planePoint)
        {
            return origin + planePoint.X * unitX + planePoint.Y * unitY;
        }
        public readonly Vector3 ToWorldSpace(Coordinate planePoint)
        {
            return ToWorldSpace(GeometryHelper.CoordToVec(planePoint));
        }
        private static float ProjDist(Vector3 u, Vector3 v)
        {
            return Vector3.Dot(v, u) / Vector3.Dot(u, u);
        }
        //Find point at which line intersects plane. Throws exception if line is parallel to plane.
        public readonly Vector2 LineIntersect(Vector3 lineStart, Vector3 lineDir)
        {
            var dot = Vector3.Dot(normal, lineDir);
            if (Math.Abs(dot) <= O)
                throw new Exception("No point intersection, line is parallel to plane.");
            var w = lineStart - origin;
            var dist = -(Vector3.Dot(normal, w)) / dot;
            Vector3 planePoint = lineStart + (lineDir * dist);
            return ToPlaneSpace(planePoint);
        }
    }
}

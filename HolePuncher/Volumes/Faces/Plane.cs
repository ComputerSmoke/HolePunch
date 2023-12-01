using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.Utilities;
using Silk.NET.OpenXR;
using Stride.Core.Mathematics;

namespace HolePuncher.Volumes.Faces
{
    public struct Line(Vector2 point, Vector2 dir)
    {
        public Vector2 point = point;
        public Vector2 dir = dir;
    }
    public class Plane
    {
        public Vector3 origin;
        public Vector3 normal, unitX, unitY;
        //bootleg zero
        public const float O = 1e-6f;
        public Plane(Vector3 origin, Vector3 normal)
        {
            this.origin = origin;
            this.normal = normal;
            this.normal.Normalize();
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
            normal.Normalize();
        }
        //Rotate plane by rot around source
        public void RotateAround(Vector3 target, Vector3 axis, float angle)
        {
            Vector3.RotateAround(origin, target, axis, angle);
            Vector3.RotateAround(normal, Vector3.Zero, axis, angle);
            Vector3.RotateAround(unitX, Vector3.Zero, axis, angle);
            Vector3.RotateAround(unitY, Vector3.Zero, axis, angle);
        }
        public Vector2 Project(Vector3 point)
        {
            Vector3 diff = point - origin;
            float dist = Vector3.Dot(diff, normal);
            //planePoint is nearest point on plane to point
            Vector3 planePoint = point - dist * normal;
            //Now project onto unit plane XY vectors to get local coords. These unit vectors are arbitrary but consistent for each plane.
            return ToPlaneSpace(planePoint);
        }
        public Vector2 ToPlaneSpace(Vector3 point)
        {
            float px = ProjDist(unitX, point - origin);
            float py = ProjDist(unitY, point - origin);
            return new Vector2(px, py);
        }
        public Vector3 ToWorldSpace(Vector2 planePoint)
        {
            return origin + planePoint.X * unitX + planePoint.Y * unitY;
        }
        public Vector3 ToWorldSpace(Coordinate planePoint)
        {
            return ToWorldSpace(GeometryHelper.CoordToVec(planePoint));
        }
        private static float ProjDist(Vector3 u, Vector3 v)
        {
            return Vector3.Dot(v, u) / Vector3.Dot(u, u);
        }
        //Find point at which line intersects plane. Throws exception if line is parallel to plane.
        public Vector2 LineIntersect(Vector3 lineStart, Vector3 lineDir)
        {
            if (Parallel(lineDir))
                throw new NoIntersectException("Line parallel to plane");
            var dot = Vector3.Dot(normal, lineDir);
            var w = lineStart - origin;
            var dist = -Vector3.Dot(normal, w) / dot;
            Vector3 planePoint = lineStart + lineDir * dist;
            return ToPlaneSpace(planePoint);
        }
        public bool Parallel(Plane other)
        {
            return Vector3.Cross(normal, other.normal).LengthSquared() <= O*O;
        }
        public bool Parallel(Vector3 lineDir)
        {
            return Math.Abs(Vector3.Dot(normal, lineDir)) <= O;
        }
        //Line representing intersection of another plane. 
        public Line Intersection(Plane plane)
        {
            if (Parallel(plane))
                throw new NoIntersectException("Planes are parallel");
            Vector3 dir = Vector3.Cross(normal, plane.normal);
            Vector3 perpDir = Vector3.Cross(dir, plane.normal);
            Vector3 intersection = ToWorldSpace(LineIntersect(plane.origin, perpDir));
            return new Line(ToPlaneSpace(intersection), ToPlaneSpace(origin+dir));
        }
    }
}

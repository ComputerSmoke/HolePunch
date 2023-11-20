using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using Stride.Core.Mathematics;

namespace HolePunching.HolePunch
{
    public struct Prism (Vector3 start, Vector3 normal, float radius, int numSides, float height)
    {
        public Plane facePlane = new(start, normal);
        public float radius = radius;
        public float height = height;
        public Polygon face = Geometry.InscribeCircle(radius, numSides);
    }
    public struct Plane
    {
        public Vector3 origin;
        public Vector3 normal,unitX,unitY;
        public Plane(Vector3 origin, Vector3 normal)
        {
            this.origin = origin;
            this.normal = normal;
            Vector3 nonparallel = normal.X == 0 && normal.Y == 0 ? normal + Vector3.UnitX : normal + Vector3.UnitZ;
            unitX = Vector3.Cross(normal, nonparallel);
            unitY = Vector3.Cross(normal, unitX);
            unitX.Normalize();
            unitY.Normalize();
        }
        
        public readonly Vector2 Project(Vector3 point)
        {
            Vector3 diff = point - origin;
            float dist = Vector3.Dot(diff, normal);
            //planePoint is nearest point on plane to point
            Vector3 planePoint = point - dist * normal;
            //Now project onto unit plane XY vectors to get local coords. These unit vectors are arbitrary but consistent for each plane.
            float px = ProjDist(unitX, planePoint);
            float py = ProjDist(unitY, planePoint);
            return new Vector2(px, py);
        }
        public readonly Vector3 ToWorldSpace(Vector2 planePoint)
        {
            return origin + planePoint.X * unitX + planePoint.Y * unitY;
        }
        private static float ProjDist(Vector3 u, Vector3 v)
        {
            return Vector3.Dot(v, u) / Vector3.Dot(u, u);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HolePuncher.Volumes.Faces;
using NetTopologySuite.Geometries;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Plane = HolePuncher.Volumes.Faces.Plane;

namespace HolePuncher.Volumes
{
    public class Prism (Vector3 start, Vector3 normal, float radius, int numSides) : IVolume
    {
        public Plane facePlane = new(start, normal);
        public float radius = radius;
        public int numSides = numSides;
        public Polygon face = GeometryHelper.InscribeCircle(radius, numSides);
        public BoundingBox3D BoundingBox { get; set; } = new BoundingBox3D([new Vector3(-1e12f, -1e12f, -1e12f), new Vector3(1e12f, 1e12f, 1e12f)]);
        public Geometry Slice(Plane plane)
        {
            throw new Exception("Interest area must be provided to take slice of prism with unlimited height.");
        }
        public Prism ToMeshSpace(Mesh mesh, Skeleton skeleton)
        {
            Vector3 newStart = mesh.PointToMeshSpace(skeleton, facePlane.origin);
            Vector3 newNormal = mesh.DirToMeshSpace(skeleton, normal);
            float newRadius = mesh.ScaleToMeshSpace(skeleton, radius);
            return new Prism(newStart, newNormal, newRadius, numSides);
        }
        //Get the slice of the prism with respect to provided plane, may be limited to area around provided triangle.
        public Geometry Slice(Plane plane, Geometry interestArea)
        {
            if (IsParallel(plane))
                return ParallelSlice(plane, interestArea);
            Coordinate[] sliceCoords = new Coordinate[face.Coordinates.Length];
            for (int i = 0; i < face.Coordinates.Length - 1; i++)
            {
                Vector3 pos = facePlane.ToWorldSpace(GeometryHelper.CoordToVec(face.Coordinates[i]));
                sliceCoords[i] = GeometryHelper.VecToCoord(plane.LineIntersect(pos, facePlane.normal));
            }
            sliceCoords[^1] = sliceCoords[0];
            return GeometryHelper.CreatePolygon(sliceCoords);
        }
        public bool IsParallel(Plane plane)
        {
            return Math.Abs(Vector3.Dot(facePlane.normal, plane.normal)) <= 1e-4;
        }
        //Slice of prism for plane parallel to prism. null if no intersect.
        //Only area around triangle
        private Polygon ParallelSlice(Plane plane, Geometry interestArea)
        {
            //rectangle of width infinity, height is height of line intersecting facePolygon at the adjusted coords of the plane origin
            LineString line = FacePlaneIntersectionPerp(plane);
            Geometry intersect = face.Intersection(line);
            if (intersect.IsEmpty || intersect.Coordinates.Length < 2)
                return null;
            Vector2 SwitchPlanes(Plane sourcePlane, Coordinate coord) =>
                plane.ToPlaneSpace(sourcePlane.ToWorldSpace(GeometryHelper.CoordToVec(coord)));
            Vector2 p0 = SwitchPlanes(facePlane, intersect.Coordinates[0]);
            Vector2 p1 = SwitchPlanes(facePlane, intersect.Coordinates[1]);
            Vector2 heightLine = p0 - p1;
            Vector2 widthLineDir = new(-heightLine.Y, heightLine.X);
            widthLineDir.Normalize();
            //Get min length of prism slice that would yield area around triangle
            float maxDist = .001f, minDist = -.001f;
            for (int i = 0; i < interestArea.Envelope.Coordinates.Length - 1; i++)
            {
                Vector2 v = GeometryHelper.CoordToVec(interestArea.Envelope.Coordinates[i]);
                float d1 = Vector2.Dot(v - p0, widthLineDir);
                float d2 = Vector2.Dot(v - p1, widthLineDir);
                if (d1 > maxDist)
                    maxDist = d1;
                if (d2 > maxDist)
                    maxDist = d2;
                if (d1 < minDist)
                    minDist = d1;
                if (d2 < minDist)
                    minDist = d2;
            }
            minDist -= .1f;
            maxDist += .1f;
            //Vertices of resulting rectangle
            Coordinate v1 = GeometryHelper.VecToCoord(p0 + widthLineDir * minDist);
            Coordinate v2 = GeometryHelper.VecToCoord(p0 + widthLineDir * maxDist);
            Coordinate v3 = GeometryHelper.VecToCoord(p1 + widthLineDir * maxDist);
            Coordinate v4 = GeometryHelper.VecToCoord(p1 + widthLineDir * minDist);
            Coordinate[] coords = [v1, v2, v3, v4, v1];
            Polygon res = GeometryHelper.CreatePolygon(coords);
            return res;
        }
        //line on face plane that is intersection of another, perpendicular plane.
        private LineString FacePlaneIntersectionPerp(Plane plane)
        {
            Vector3 intersection = facePlane.ToWorldSpace(facePlane.LineIntersect(plane.origin, facePlane.normal));
            Vector3 up = Vector3.Cross(facePlane.normal, plane.normal);
            Vector3 c0 = Vector3.Dot(facePlane.origin - intersection, up) / Vector3.Dot(up, up) * up + intersection;
            Vector2 c1 = facePlane.ToPlaneSpace(c0 + up * (radius + 10f));
            Vector2 c2 = facePlane.ToPlaneSpace(c0 - up * (radius + 10f));
            return GeometryHelper.CreateLineSegment(c1, c2);
        }
    }

}

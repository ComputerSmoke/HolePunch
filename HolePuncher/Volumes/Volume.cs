using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Polygonize;
using HolePuncher.Volumes.Faces;
using Plane = HolePuncher.Volumes.Faces.Plane;
using Point = NetTopologySuite.Geometries.Point;
using System.ComponentModel;
using Valve.VR;
using Stride.Engine;
using Stride.Rendering;
using Stride.Graphics;
using Triangle = HolePuncher.Volumes.Faces.Triangle;
using System.Security.Cryptography.X509Certificates;

namespace HolePuncher.Volumes
{
    public struct BoundingBox3D
    {
        public Vector3 min;
        public Vector3 max;
        public BoundingBox3D(Vector3[] points)
        {
            min = points[0];
            max = points[0];
            for(int i = 1; i < points.Length; i++)
            {
                min = Vector3.Min(min, points[i]);
                max = Vector3.Max(max, points[i]);
            }
        }
        public readonly float Volume()
        {
            Vector3 diff = max - min;
            return diff.X * diff.Y * diff.Z;
        }
        public readonly bool Intersects(BoundingBox3D other)
        {
            return min.X <= other.max.X && min.Y <= other.max.Y && min.Z <= other.max.Z
                && max.X >= other.min.X && max.Y >= other.min.Y && max.Z >= other.min.Z;
        }
        public readonly BoundingBox3D RotateAround(Vector3 target, Vector3 axis, float angle)
        {
            Vector3[] vertices = [
                min,
                new Vector3(min.X, min.Y, max.Z),
                new Vector3(min.X, max.Z, min.Z),
                new Vector3(max.X, min.Y, min.Z),
                new Vector3(min.X, max.Y, max.Z),
                new Vector3(max.X, min.Y, max.Z),
                new Vector3(max.X, max.Y, min.Z),
                max
            ];
            for(int i = 0; i < vertices.Length; i++)
                Vector3.RotateAround(in vertices[i], target, axis, angle);
            return new BoundingBox3D(vertices);
        }
    }
    public readonly struct FaceSlice(Coordinate c0, Coordinate c1, Vector2 normal)
    {
        public readonly Coordinate c0 = c0;
        public readonly Coordinate c1 = c1;
        //Normal points outside of box that will be created, opposite of direction of extension.
        public readonly Vector2 normal = normal;
        public readonly Polygon ExtendToBox(float dist)
        {
            Coordinate c3 = GeometryHelper.VecToCoord(
                GeometryHelper.CoordToVec(c0) - normal * dist
            );
            Coordinate c4 = GeometryHelper.VecToCoord(
                GeometryHelper.CoordToVec(c1) - normal * dist
            );
            return GeometryHelper.CreatePolygon([c0, c1, c4, c3, c0]);
        }
    }
    //Represents a convex 3d shape
    public class Volume(Face[] faces, BoundingBox3D boundingBox) : IVolume
    {
        public Face[] Faces { get; } = faces;
        public BoundingBox3D BoundingBox { get; set; } = boundingBox;
        //Take slice of this volume
        public Geometry Slice(Plane slicer)
        {
            Geometry res = null;
            List<FaceSlice> slices = [];
            for(int i = 0; i < Faces.Length; i++)
            {
                Face face = Faces[i];
                Geometry faceSliceGeometry = face.Slice(slicer);
                if (faceSliceGeometry.Coordinates.Length < 2)
                    continue;
                Vector2 normal = slicer.Project(face.plane.normal + slicer.origin);
                normal.Normalize();
                slices.Add(new FaceSlice(faceSliceGeometry.Coordinates[0], faceSliceGeometry.Coordinates[^1], normal));
            }
            //Special case: remove matching slices if plane intersected an edge of the volume
            List<FaceSlice> unmatchedSlices = [];
            for(int i = 0; i < slices.Count; i++)
            {
                bool matches = false;
                for(int j = 0; j < slices.Count; j++)
                {
                    if (i == j)
                        continue;
                    Coordinate ci0 = slices[i].c0;
                    Coordinate ci1 = slices[i].c1;
                    Coordinate cj0 = slices[j].c0;
                    Coordinate cj1 = slices[j].c1;
                    if (ci0.Distance(cj0) <= Plane.O && ci1.Distance(cj1) <= Plane.O
                        || ci1.Distance(cj0) <= Plane.O && ci0.Distance(cj1) <= Plane.O)
                        matches = true;
                }
                if (!matches)
                    unmatchedSlices.Add(slices[i]);
            }
            foreach(FaceSlice faceSlice in unmatchedSlices)
            {
                Geometry shadow = faceSlice.ExtendToBox((BoundingBox.max - BoundingBox.min).Length());
                res = res == null ? shadow : res.Intersection(shadow);
            }
            return res ?? GeometryHelper.CreateEmpty();
        }
        //Adjust line segments to connect at endpoints when possible within maxDist
        private static List<Geometry> ConnectLines(List<Geometry> slices, float maxDist)
        {
            List<Geometry> res = [];
            //Get the valid line strings
            foreach(Geometry geometry in slices)
            {
                if (geometry.IsEmpty || geometry.Coordinates.Length < 2)
                    continue;
                Geometry line = GeometryHelper.CreateLineSegment(geometry.Coordinates[0], geometry.Coordinates[^1]);
                res.Add(line);
            }
            if (res.Count == 0)
                return res;
            Coordinate ConnectCoord(Coordinate coord, int lineIdx)
            {
                Coordinate nearest = lineIdx == 0 ? res[1].Coordinate : res[0].Coordinate;
                Coordinate FindNearest(Coordinate other) => 
                    coord.Distance(nearest) > coord.Distance(other) ? other : nearest;
                for(int i = 0; i < res.Count; i++)
                {
                    if (i == lineIdx)
                        continue;
                    nearest = FindNearest(res[i].Coordinates[0]);
                    nearest = FindNearest(res[i].Coordinates[1]);
                }
                return coord.Distance(nearest) < maxDist ? nearest : coord;
            }
            //Adjust them to connect at edges where possible.
            for(int i = 0; i < res.Count; i++)
            {
                Coordinate c1 = ConnectCoord(res[i].Coordinates[0], i);
                Coordinate c2 = ConnectCoord(res[i].Coordinates[1], i);
                res[i] = GeometryHelper.CreateLineSegment(c1, c2);
            }
            return res;
        }
        public Geometry Slice(Plane slicer, Geometry interestArea)
        {
            return Slice(slicer);
        }
        //Rotate volume around a point
        public void RotateAround(Vector3 target, Vector3 axis, float angle)
        {
            foreach (Face face in Faces)
                face.plane.RotateAround(target, axis, angle);
            BoundingBox = BoundingBox.RotateAround(target, axis, angle);
        }
        public VertexPositionNormalTexture[] GetVertexPositionNormalTexture()
        {
            List<Triangle> triangles = [];
            foreach(Face face in Faces)
            {
                Triangle[] triangulation = Triangle.Triangulate(face.plane, face.geometry, false);
                triangles.AddRange(triangulation);
            }
            return Triangle.TrianglesToVertices(triangles);
        }
        public bool IntersectsPrism(Prism prism)
        {
            foreach(Face face in Faces)
            {
                Geometry slice = prism.Slice(face.plane, face.geometry);
                if (slice != null && face.geometry.Intersects(slice))
                    return true;
            }
            return false;
        }
        public bool InsidePrism(Prism prism)
        {
            foreach (Face face in Faces)
            {
                Geometry slice = prism.Slice(face.plane, face.geometry);
                if (slice == null || !face.geometry.Difference(slice).IsEmpty)
                    return false;
            }
            return true;
        }
        public List<Triangle> Punch(List<Triangle> triangles)
        {
            List<Triangle> res = [];
            foreach (Triangle triangle in triangles)
                res.AddRange(Punch(triangle));
            return res;
        }
        public List<Triangle> Crop(List<Triangle> triangles)
        {
            List<Triangle> res = [];
            foreach (Triangle triangle in triangles)
                res.AddRange(Crop(triangle));
            return res;
        }
        //Get area of triangle outside of this volume, then triangularize it
        private Triangle[] Punch(Triangle triangle)
        {
            if (!BoundingBox.Intersects(triangle.BoundingBox))
                return [triangle];
            Geometry holeSlice = Slice(triangle.plane);
            if (holeSlice != null && !holeSlice.IsEmpty)
                holeSlice = holeSlice.GetGeometryN(0);
            return HolePunch.Punch(triangle, holeSlice);
        }
        //Get area inside this volume, then triangularize it
        private Triangle[] Crop(Triangle triangle)
        {
            if (!BoundingBox.Intersects(triangle.BoundingBox))
                return [];
            Geometry holeSlice = Slice(triangle.plane);
            if (holeSlice != null && !holeSlice.IsEmpty)
                holeSlice = holeSlice.GetGeometryN(0);
            return HolePunch.Crop(triangle, holeSlice);
        }
    }
}

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
    //Represents a convex 3d shape
    public class Volume(Face[] faces, BoundingBox3D boundingBox) : IVolume
    {
        public Face[] Faces { get; } = faces;
        public BoundingBox3D BoundingBox { get; set; } = boundingBox;
        //Take slice of this volume
        public Geometry Slice(Plane slicer)
        {
            Polygonizer polygonizer = new();
            for(int i = 0; i < Faces.Length; i++)
            {
                Face face = Faces[i];
                Geometry faceSlice = face.Slice(slicer);
                polygonizer.Add(RoundCoords(faceSlice, 3));
            }
            return polygonizer.GetGeometry();
        }
        private static Geometry RoundCoords(Geometry input, int decimals)
        {
            if (input.IsEmpty)
                return input;
            Coordinate[] translated = new Coordinate[input.Coordinates.Length];
            double div = Math.Pow(10, decimals);
            for (int i = 0; i < input.Coordinates.Length; i++)
            {
                Coordinate coord = new Coordinate(
                    Math.Round(input.Coordinates[i].X * div)/div, 
                    Math.Round(input.Coordinates[i].Y*div)/div
                );
                translated[i] = coord;
            }
            if (input is LineString)
                return GeometryHelper.GeometryFactory.CreateLineString(translated);
            if(input is Point)
                return GeometryHelper.GeometryFactory.CreatePoint(translated[0]);
            throw new Exception("Unrecognized geometry");
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Polygonize;

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
    }
    //Represents a convex 3d shape
    internal class Volume(Face[] faces, BoundingBox3D BoundingBox) : IVolume
    {
        readonly Face[] faces = faces;
        public BoundingBox3D BoundingBox { get; set; } = BoundingBox;
        //Take slice of this volume
        public Geometry Slice(Plane slicer)
        {
            Polygonizer polygonizer = new(true);
            for(int i = 0; i < faces.Length; i++)
            {
                Face face = faces[i];
                polygonizer.Add(face.Slice(slicer));
            }
            return polygonizer.GetGeometry();
        }
        public Geometry Slice(Plane slicer, Geometry interestArea)
        {
            return Slice(slicer);
        }
    }
}

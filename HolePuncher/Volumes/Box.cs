using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HolePuncher.Volumes.Faces;
using NetTopologySuite.Geometries;
using Silk.NET.OpenXR;
using Stride.Core.Mathematics;
using Plane = HolePuncher.Volumes.Faces.Plane;

namespace HolePuncher.Volumes
{
    public class Box(Vector3 p0, Vector3 p1) : Volume(GetFaces(p0, p1), new BoundingBox3D([p0, p1]))
    {
        static Face[] GetFaces(Vector3 p0, Vector3 p1)
        {
            Face[] faces =
            [
                GetFace(
                    p0, 
                    new Vector3(p0.X, p0.Y, p1.Z), 
                    new Vector3(p0.X, p1.Y, p1.Z), 
                    new Vector3(p0.X, p1.Y, p0.Z),
                    new Vector3(p0.X-p1.X, 0, 0)
                ),
                GetFace(
                    p1,
                    new Vector3(p1.X, p0.Y, p1.Z),
                    new Vector3(p1.X, p0.Y, p0.Z),
                    new Vector3(p1.X, p1.Y, p0.Z),
                    new Vector3(p1.X - p0.X, 0, 0)
                ),
                GetFace(
                    p0,
                    new Vector3(p0.X, p0.Y, p1.Z),
                    new Vector3(p1.X, p0.Y, p1.Z),
                    new Vector3(p1.X, p0.Y, p0.Z),
                    new Vector3(0, p0.Y - p1.Y, 0)
                ),
                GetFace(
                    p1,
                    new Vector3(p0.X, p1.Y, p1.Z),
                    new Vector3(p0.X, p1.Y, p0.Z),
                    new Vector3(p1.X, p1.Y, p0.Z),
                    new Vector3(0, p1.Y - p0.Y, 0)
                ),
                GetFace(
                    p0,
                    new Vector3(p1.X, p0.Y, p0.Z),
                    new Vector3(p1.X, p1.Y, p0.Z),
                    new Vector3(p0.X, p1.Y, p0.Z),
                    new Vector3(0, 0, p0.Z - p1.Z)
                ),
                GetFace(
                    p1,
                    new Vector3(p1.X, p0.Y, p1.Z),
                    new Vector3(p0.X, p0.Y, p1.Z),
                    new Vector3(p0.X, p1.Y, p1.Z),
                    new Vector3(0, 0, p1.Z - p0.Z)
                ),
            ];
            return faces;
        }
        static Face GetFace(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal)
        {
            Plane plane = new (v0, normal);
            Coordinate c0 = new (0, 0);
            Coordinate c1 = GeometryHelper.VecToCoord(plane.ToPlaneSpace(v1));
            Coordinate c2 = GeometryHelper.VecToCoord(plane.ToPlaneSpace(v2));
            Coordinate c3 = GeometryHelper.VecToCoord(plane.ToPlaneSpace(v3));
            Geometry geom = GeometryHelper.CreatePolygon([c0, c1, c2, c3, c0]);
            return new(geom, plane);
        }
        public float Volume()
        {
            return BoundingBox.Volume();
        }
    }
}

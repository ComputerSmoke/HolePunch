using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using Stride.Core.Mathematics;

namespace HolePuncher.Volumes
{
    internal class Cube(Vector3 p0, float edgeLength) : Volume(GetFaces(p0, edgeLength), GetBoundingBox(p0, edgeLength))
    {
        static Face[] GetFaces(Vector3 p0, float edgeLength)
        {
            Face[] faces = new Face[6];

            //Note geometry is not deep copied, so don't mess with it later in the faces lol.
            Geometry geometry = GetFace(p0, edgeLength);
            (Face,Face) FacesInDir(Vector3 normal)
            {
                return (
                    new Face(geometry, new Plane(p0, -normal)),
                    new Face(geometry, new Plane(p0+ edgeLength*normal, normal))
                );
            }
            (faces[0], faces[1]) = FacesInDir(Vector3.UnitX);
            (faces[2], faces[3]) = FacesInDir(Vector3.UnitY);
            (faces[4], faces[5]) = FacesInDir(Vector3.UnitZ);
            return faces;
        }
        static Polygon GetFace(Vector3 p0, float edgeLength)
        {
            Coordinate c0 = new (0,0);
            Coordinate c1 = new (edgeLength, 0);
            Coordinate c2 = new (edgeLength, edgeLength);
            Coordinate c3 = new (0, edgeLength);
            return GeometryHelper.CreatePolygon([c0, c1, c2, c3, c0]);
        }
        static BoundingBox3D GetBoundingBox(Vector3 p0, float edgeLength)
        {
            Vector3 p1 = p0 + new Vector3(edgeLength, edgeLength, edgeLength);
            return new BoundingBox3D([p0, p1]);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate.Polygon;
using NetTopologySuite.Triangulate.Tri;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace HolePuncher.Volumes.Faces
{
    public class Triangle(Vector3 p1, Vector3 p2, Vector3 p3) : Face([p1, p2, p3])
    {
        public Vector3 V1 { get { return vertices[0]; } }
        public Vector3 V2 { get { return vertices[1]; } }
        public Vector3 V3 { get { return vertices[2]; } }
        //Turn geometry on plane into triangles, then put back in 3-space from plane
        public static Triangle[] Triangulate(Plane plane, Geometry geom)
        {
            if (geom.IsEmpty || geom.Coordinates.Length < 3)
                return [];
            try
            {
                var tris = new ConstrainedDelaunayTriangulator(geom).GetTriangles();
                Triangle[] res = new Triangle[tris.Count];

                Triangle TriToTriangle(Tri t) =>
                new(
                        plane.ToWorldSpace(GeometryHelper.CoordToVec(t.GetCoordinate(0))),
                        plane.ToWorldSpace(GeometryHelper.CoordToVec(t.GetCoordinate(1))),
                        plane.ToWorldSpace(GeometryHelper.CoordToVec(t.GetCoordinate(2)))
                    );
                int i = 0;
                foreach (Tri t in tris)
                {
                    res[i] = TriToTriangle(t);
                    i++;
                }
                return res;
            }
            catch
            {
                return [];
            }
        }
        //Turn a list of triangles into a VertexPositionNormalTexture array
        public static VertexPositionNormalTexture[] TrianglesToVertices(List<Triangle> triangles)
        {
            VertexPositionNormalTexture[] newVertices = new VertexPositionNormalTexture[triangles.Count * 3];
            for (int i = 0; i < triangles.Count; i++)
            {
                newVertices[i * 3].Position = triangles[i].V1;
                newVertices[i * 3 + 1].Position = triangles[i].V2;
                newVertices[i * 3 + 2].Position = triangles[i].V3;
                newVertices[i * 3].Normal = triangles[i].plane.normal;
                newVertices[i * 3 + 1].Normal = triangles[i].plane.normal;
                newVertices[i * 3 + 2].Normal = triangles[i].plane.normal;
            }
            return newVertices;
        }
    }
}

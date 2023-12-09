using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate.Polygon;
using NetTopologySuite.Triangulate.Tri;
using Silk.NET.OpenXR;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace HolePuncher.Volumes.Faces
{
    public class Triangle : Face
    {
        //Vertex coordinates
        public Vector3 V1 { get { return vertices[0]; } }
        public Vector3 V2 { get { return vertices[1]; } }
        public Vector3 V3 { get { return vertices[2]; } }
        //True if triangle is on original outer face of object. Determines which material is used.
        public bool IsOuterFace { get; set; }
        //Texture coordinates of vertices
        public Vector2 T1 { get; set; }
        public Vector2 T2 { get; set; }
        public Vector2 T3 { get; set; }
        public Triangle(Vector3 p1, Vector3 p2, Vector3 p3) : base([p1, p2, p3]) { }
        public Triangle(Plane plane, Coordinate c0, Coordinate c1, Coordinate c2) : base(GeometryHelper.CreatePolygon([c0, c1, c2, c0]), plane) { }
        //Turn geometry into triangles, using plane of originalTriangle, and preserving IsOuterFace and texture coordinates
        public static Triangle[] Triangulate(Triangle originalTriangle, Geometry geom)
        {
            Triangle[] res = Triangulate(originalTriangle.plane, geom, false);
            for(int i = 0; i < res.Length; i++)
            {
                res[i].IsOuterFace = originalTriangle.IsOuterFace;
                Vector2 p = GeometryHelper.CoordToVec(originalTriangle.geometry.Coordinates[0]);
                Vector2 u = GeometryHelper.CoordToVec(originalTriangle.geometry.Coordinates[1]) - p;
                Vector2 v = GeometryHelper.CoordToVec(originalTriangle.geometry.Coordinates[2]) - p;
                Vector2 pt = originalTriangle.T1;
                Vector2 dut = originalTriangle.T2 - pt;
                Vector2 dvt = originalTriangle.T3 - pt;
                if (dut.LengthSquared() <= Plane.O || dvt.LengthSquared() <= Plane.O)
                    continue;
                Vector2 ProjectTexturePos(Vector3 pos)
                {
                    Vector2 b = originalTriangle.plane.ToPlaneSpace(pos) - p;
                    float denom = Det(u.X, v.X, u.Y, v.Y);
                    float A = Det(b.X, v.X, b.Y, v.Y) / denom;
                    float B = Det(u.X, b.X, u.Y, b.Y) / denom;
                    Vector2 res = pt + A * dut + B * dvt;
                    return res;
                }
                res[i].T1 = ProjectTexturePos(res[i].V1);
                res[i].T2 = ProjectTexturePos(res[i].V2);
                res[i].T3 = ProjectTexturePos(res[i].V3);
            }
            return res;
        }
        private static float Det(float a, float b, float c, float d)
        {
            return a * d - b * c;
        }
        //Turn geometry on plane into triangles, then put back in 3-space from plane
        public static Triangle[] Triangulate(Plane plane, Geometry geom, bool invertNormal)
        {
            if (geom.IsEmpty || geom.Coordinates.Length < 3)
                return [];
            try
            {
                var tris = new ConstrainedDelaunayTriangulator(geom).GetTriangles();
                Triangle[] res = new Triangle[tris.Count];

                Triangle TriToTriangle(Tri t)
                {
                    if(!invertNormal)
                        return new(
                            plane.ToWorldSpace(GeometryHelper.CoordToVec(t.GetCoordinate(0))),
                            plane.ToWorldSpace(GeometryHelper.CoordToVec(t.GetCoordinate(1))),
                            plane.ToWorldSpace(GeometryHelper.CoordToVec(t.GetCoordinate(2)))
                        );
                    return new(
                        plane.ToWorldSpace(GeometryHelper.CoordToVec(t.GetCoordinate(0))),
                        plane.ToWorldSpace(GeometryHelper.CoordToVec(t.GetCoordinate(2))),
                        plane.ToWorldSpace(GeometryHelper.CoordToVec(t.GetCoordinate(1)))
                    );
                }
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
                newVertices[i * 3].TextureCoordinate = triangles[i].T1;
                newVertices[i * 3 + 1].TextureCoordinate = triangles[i].T2;
                newVertices[i * 3 + 2].TextureCoordinate = triangles[i].T3;
            }
            return newVertices;
        }
        public static (VertexPositionNormalTexture[], VertexPositionNormalTexture[]) TrianglesToInnerOuterVertices(List<Triangle> triangles) {
            List<Triangle> inner = triangles.FindAll((Triangle tri) => !tri.IsOuterFace);
            List<Triangle> outer = triangles.FindAll((Triangle tri) => tri.IsOuterFace);
            return (TrianglesToVertices(inner), TrianglesToVertices(outer));
        }
    }
}

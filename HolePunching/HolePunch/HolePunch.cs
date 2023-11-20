using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Engine;
using Stride.Rendering;
using Stride.Graphics;
using Stride.Extensions;
using Stride.Core.Mathematics;
using System.Collections;
using Stride.Particles.Updaters.FieldShapes;
using Valve.VR;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate.Polygon;
using NetTopologySuite.Triangulate.Tri;

namespace HolePunching.HolePunch
{
    struct Triangle (Vector3 v1, Vector3 v2, Vector3 v3)
    {
        public Vector3 v1 = v1;
        public Vector3 v2 = v2;
        public Vector3 v3 = v3;
        public Plane plane = new(Vector3.Zero, Vector3.Cross(v3 - v1, v2 - v1));
    }
    public class HolePunch : StartupScript
    {
        private Model model;
        private VertexPositionTexture[] vertices;
        public override void Start()
        {
            base.Start();
            Geometry.Init();
            model = Entity.Get<ModelComponent>().Model;
            vertices = ExtractVertices(model);
            SetModel(vertices);
            AddHole(new Prism(new Vector3(1, 0, 0), Vector3.UnitX, .1f, 6));
        }
        private void SetModel(VertexPositionTexture[] vertices)
        {
            model.Meshes.Clear();
            var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(GraphicsDevice, vertices,
                                                                 GraphicsResourceUsage.Dynamic);
            int[] indices = new int[vertices.Length];
            for (int i = 0; i < indices.Length; i++)
                indices[i] = i;
            var indexBuffer = Stride.Graphics.Buffer.Index.New(GraphicsDevice, indices);

            var customMesh = new Mesh
            {
                Draw = new MeshDraw
                {
                    /* Vertex buffer and index buffer setup */
                    PrimitiveType = PrimitiveType.TriangleList,
                    DrawCount = indices.Length,
                    IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Length),
                    VertexBuffers = [ new VertexBufferBinding(vertexBuffer,
                                  VertexPositionTexture.Layout, vertexBuffer.ElementCount) ],
                }
            };
            // add the mesh to the model
            model.Meshes.Add(customMesh);
        }
        //TODO: get the vertices back from the mesh
        private VertexPositionTexture[] ExtractVertices(Model model)
        {
            (List<Vector3> verts, List<int> _) = MeshExtensions.GetMeshVerticesAndIndices(model, Game);
            var res = new VertexPositionTexture[verts.Count];
            int i = 0;
            foreach(Vector3 v in verts)
            {
                res[i].Position = v;
                i++;
            }
            return Cube();
        }
        private static VertexPositionTexture[] Cube()
        {
            List<VertexPositionTexture> res = [];
            var top = Face(new Vector3(-.5f, .5f, -.5f), Quaternion.Identity);
            var bottom = Face(new Vector3(-.5f, .5f, -.5f), Quaternion.RotationAxis(new Vector3(1, 0, 0), (float)Math.PI));
            res.AddRange(top);
            res.AddRange(bottom);
            var left = Face(new Vector3(-.5f, .5f, -.5f), Quaternion.RotationAxis(new Vector3(1, 0, 0), (float)Math.PI/2));
            var right = Face(new Vector3(-.5f, .5f, -.5f), Quaternion.RotationAxis(new Vector3(1, 0, 0), -(float)Math.PI / 2));
            var front = Face(new Vector3(-.5f, .5f, -.5f), Quaternion.RotationAxis(new Vector3(0, 0, 1), (float)Math.PI / 2));
            var rear = Face(new Vector3(-.5f, .5f, -.5f), Quaternion.RotationAxis(new Vector3(0, 0, 1), -(float)Math.PI / 2));
            res.AddRange(left);
            res.AddRange(right);
            res.AddRange(front);
            res.AddRange(rear);
            return [.. res];
        }
        private static VertexPositionTexture[] Face(Vector3 pos, Quaternion rot)
        {
            VertexPositionTexture[] res = new VertexPositionTexture[6];

            res[0].Position = pos;
            res[1].Position = pos + new Vector3(1, 0, 0);
            res[2].Position = pos + new Vector3(1, 0, 1);
            res[5].Position = pos;
            res[4].Position = pos + new Vector3(0, 0, 1);
            res[3].Position = pos + new Vector3(1, 0, 1);
            for(int i = 0; i < 6; i++)
                rot.Rotate(ref res[i].Position);
            return res;
        }
        public void AddHole(Prism hole)
        {
            SetModel(PunchHole(vertices, hole));
        }
        private static VertexPositionTexture[] PunchHole(VertexPositionTexture[] vertices, Prism hole) {
            List<Triangle> punched = [];
            for(int i = 0; i+2 < vertices.Length; i += 3)
            {
                Triangle triangle = new(vertices[i].Position, vertices[i + 1].Position, vertices[i + 2].Position);
                
                Polygon triangleProj = Geometry.CreateTriangle(
                    hole.facePlane.Project(triangle.v1), 
                    hole.facePlane.Project(triangle.v2), 
                    hole.facePlane.Project(triangle.v3)
                );
                //Shortcut if outside bounding box
                if (DisjointBoundingBox(triangleProj, hole.radius))
                    punched.Add(triangle);
                else
                    punched.AddRange(Punch(triangle.plane, triangleProj, hole));
            }
            VertexPositionTexture[] newVertices = new VertexPositionTexture[punched.Count * 3];
            for(int i = 0; i < punched.Count; i++)
            {
                newVertices[i * 3].Position = punched[i].v1;
                newVertices[i * 3 + 1].Position = punched[i].v2;
                newVertices[i * 3 + 2].Position = punched[i].v3;
            }
            return newVertices;
        }
        private static Triangle[] Punch(Plane trianglePlane, Polygon triangleProj, Prism hole)
        {
            //TODO special case: planes trianglePlane and hole face plane are orthogonal
            Polygon cutProj = Geometry.Difference(triangleProj, hole.face);
            var tris = new ConstrainedDelaunayTriangulator(cutProj).GetTriangles();
            Triangle[] res = new Triangle[tris.Count];
            int i = 0;
            //TODO: projection needs to go back along face plane's normal, not triangle plane normal.
            Vector3 unproject(Tri t, int idx) =>
                trianglePlane.ToWorldSpace(
                    trianglePlane.Project(
                        hole.facePlane.ToWorldSpace(
                            Geometry.CoordToVec(t.GetCoordinate(idx))
                        )
                    )
                );
            foreach (Tri t in tris)
            {
                res[i] = new Triangle(
                    unproject(t, 0),
                    unproject(t, 1),
                    unproject(t, 2)
                );
                i++;
            }
            return res;
        }
        private static bool DisjointBoundingBox(Polygon polygon, float holeRadius)
        {
            var coords = polygon.Coordinates;
            double minX=0, maxX=0, minY=0, maxY=0;
            foreach(var coord in coords)
            {
                minX = Math.Min(minX, coord.X);
                maxX = Math.Max(maxX, coord.X);
                minY = Math.Min(minY, coord.Y);
                maxY = Math.Max(maxY, coord.Y);
            }
            return maxX < -holeRadius || minX > holeRadius || maxY < -holeRadius || minY > holeRadius;
        }
    }
}

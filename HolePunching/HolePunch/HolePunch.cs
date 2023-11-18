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

namespace HolePunching.HolePunch
{
    struct Triangle
    {
        public Vector3 v1;
        public Vector3 v2;
        public Vector3 v3;
        public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }
    }
    struct Triangle2D
    {
        public Vector2 v1;
        public Vector2 v2;
        public Vector2 v3;
        public Triangle2D(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }
    }
    public class HolePunch : StartupScript
    {
        private Model model;
        private VertexPositionTexture[] vertices;
        public override void Start()
        {
            base.Start();
            model = Entity.Get<ModelComponent>().Model;
            vertices = ExtractVertices(model);
            SetModel(vertices);
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
                    VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer,
                                  VertexPositionTexture.Layout, vertexBuffer.ElementCount) },
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
            List<VertexPositionTexture> res = new();
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
            return res.ToArray();
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
        public static void PunchHole(VertexPositionTexture[] vertices, Vector3 holeStart, Vector3 holeNormal, float holeRadius, int numSides) {
            List<Triangle> intersected = new();
            for(int i = 0; i+2 < vertices.Length; i += 3)
            {
                Triangle triangle = new Triangle(vertices[i].Position, vertices[i + 1].Position, vertices[i + 2].Position);
                if(CylinderIntersect(triangle, holeStart, holeNormal, holeRadius))
                    intersected.Add(triangle);
            }
        }
        private static bool CylinderIntersect(Triangle triangle, Vector3 holeStart, Vector3 holeNormal, float holeRadius)
        {
            //Project triangle onto plane normal to holeDirection
            Triangle2D projectedTriangle = new(
                ProjectToPlane(triangle.v1, holeStart, holeNormal), 
                ProjectToPlane(triangle.v2, holeStart, holeNormal), 
                ProjectToPlane(triangle.v3, holeStart, holeNormal)
            );

        }
        private static Vector2 ProjectToPlane(Vector3 point, Vector3 planeOrigin, Vector3 planeNormal)
        {
            Vector3 diff = point - planeOrigin;
            float dist = Vector3.Dot(diff, planeNormal);
            //planePoint is nearest point on plane to point
            Vector3 planePoint = point - dist * planeNormal;
            //Now project onto unit plane XY vectors to get local coords. These unit vectors are arbitrary but consistent for each plane.
            Vector3 nonparallel = planeNormal.X == 0 && planeNormal.Y == 0 ? planeNormal + Vector3.UnitX : planeNormal = Vector3.UnitZ;
            Vector3 unitX = Vector3.Cross(planeNormal, nonparallel);
            Vector3 unitY = Vector3.Cross(planeNormal, unitX);
            float px = ProjDist(unitX, planePoint);
            float py = ProjDist(unitY, planePoint);
            return new Vector2(px, py);
        }
        private static float ProjDist(Vector3 u, Vector3 v)
        {
            return Vector3.Dot(v, u) / Vector3.Dot(u, u);
        }
    }
}

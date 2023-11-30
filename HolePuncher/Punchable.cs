using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.Materials;
using Valve.VR;
using HolePuncher.Volumes;

namespace HolePuncher
{
    public class Punchable : StartupScript
    {

        private ModelComponent modelComponent;
        private VertexPositionNormalTexture[] vertices;
        private Material material;
        public override void Start()
        {
            base.Start();
            modelComponent = Entity.Get<ModelComponent>();
            vertices = ExtractVertices(modelComponent.Model);
            modelComponent.Model = new();
            material = Content.Load<Material>("Sphere Material");
            SetModel(vertices);
            /*AddHole(new Prism(new Vector3(2, 0, 0), Vector3.UnitX, .1f, 8));
            AddHole(new Prism(new Vector3(1, -.5f, 0), Vector3.UnitX, .1f, 6));
            AddHole(new Prism(new Vector3(1, 0, 1), new Vector3(-1, 0, -1), .1f, 3));
            AddHole(new Prism(new Vector3(0, 2, 0), Vector3.UnitY, .1f, 8));
            AddHole(new Prism(new Vector3(2, .5f, .5f), Vector3.UnitX, .1f, 8));*/
        }
        private void SetModel(VertexPositionNormalTexture[] vertices)
        {
            modelComponent.Model = new();
            modelComponent.Model.Meshes.Clear();
            modelComponent.Model.Materials.Clear();
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
                    VertexBuffers = [new VertexBufferBinding(vertexBuffer,
                                  VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount)],
                }
            };
            customMesh.MaterialIndex = 0;
            // add the mesh to the model
            modelComponent.Model.Meshes.Add(customMesh);
            //this.vertices = vertices;
            modelComponent.Model.Materials.Add(material);
        }
        //TODO: get the vertices back from the mesh
        private VertexPositionNormalTexture[] ExtractVertices(Model model)
        {
            return Cube();
        }
        private static VertexPositionNormalTexture[] Cube()
        {
            List<VertexPositionNormalTexture> res = [];
            var top = Face(new Vector3(-.5f, .5f, -.5f), Quaternion.Identity);
            var bottom = Face(new Vector3(-.5f, .5f, -.5f), Quaternion.RotationAxis(new Vector3(1, 0, 0), (float)Math.PI));
            res.AddRange(top);
            res.AddRange(bottom);
            var left = Face(new Vector3(-.5f, .5f, -.5f), Quaternion.RotationAxis(new Vector3(1, 0, 0), (float)Math.PI / 2));
            var right = Face(new Vector3(-.5f, .5f, -.5f), Quaternion.RotationAxis(new Vector3(1, 0, 0), -(float)Math.PI / 2));
            var front = Face(new Vector3(-.5f, .5f, -.5f), Quaternion.RotationAxis(new Vector3(0, 0, 1), (float)Math.PI / 2));
            var rear = Face(new Vector3(-.5f, .5f, -.5f), Quaternion.RotationAxis(new Vector3(0, 0, 1), -(float)Math.PI / 2));
            res.AddRange(left);
            res.AddRange(right);
            res.AddRange(front);
            res.AddRange(rear);
            return [.. res];
        }
        private static VertexPositionNormalTexture[] Face(Vector3 pos, Quaternion rot)
        {
            VertexPositionNormalTexture[] res = new VertexPositionNormalTexture[6];

            res[0].Position = pos;
            res[1].Position = pos + new Vector3(1, 0, 0);
            res[2].Position = pos + new Vector3(1, 0, 1);
            res[5].Position = pos;
            res[4].Position = pos + new Vector3(0, 0, 1);
            res[3].Position = pos + new Vector3(1, 0, 1);
            for (int i = 0; i < 6; i++)
            {
                rot.Rotate(ref res[i].Position);
                Vector3 normal = Vector3.UnitY;
                rot.Rotate(ref normal);
                res[i].Normal = normal;
            }

            return res;
        }
        public void AddHole(Prism hole)
        {
            VertexPositionNormalTexture[] res = HolePunch.PunchHole(vertices, hole);
            vertices = res;
            SetModel(res);
        }
        public void AddHole(Volume hole)
        {
            VertexPositionNormalTexture[] res = HolePunch.PunchHole(vertices, hole);
            vertices = res;
            SetModel(res);
        }
        public void AddHoleFromWorld(Vector3 pos, Vector3 dir, float radius)
        {
           // int x = Fib(10);
            dir.Normalize();
            Entity.Transform.GetWorldTransformation(out Vector3 worldPos, out Quaternion rot, out _);
            rot.Rotate(ref dir);
            pos -= worldPos;
            pos -= dir * .1f;
            Prism hole = new(pos, -dir, radius, 6);
            AddHole(hole);
        }
        public void AddChipFromWorld(Vector3 pos, Vector3 dir, float width, float depth)
        {
            //Pyramid hole = new(pos, width, depth);
            Cube hole = new(pos, width);
            //TODO: rotate hole towards dir
            AddHole(hole);
        }
        private int Fib(int n)
        {
            if (n == 0)
                return 1;
            if (n == 1)
                return 1;
            return Fib(n - 1) + Fib(n - 2);
        }
    }
}

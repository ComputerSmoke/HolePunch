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

namespace HolePunching.HolePunch
{
    public class Punchable : StartupScript
    {

        private Model model;
        private VertexPositionNormalTexture[] vertices;
        private Material material;
        public override void Start()
        {
            base.Start();
            GeometryHelper.Init();
            model = Entity.Get<ModelComponent>().Model;
            vertices = ExtractVertices(model);
            model = new();
            Entity.Get<ModelComponent>().Model = model;
            material = Content.Load<Material>("Sphere Material");
            SetModel(vertices);
            AddHole(new Prism(new Vector3(2, 0, 0), Vector3.UnitX, .1f, 8));
            AddHole(new Prism(new Vector3(1, -.5f, 0), Vector3.UnitX, .1f, 6));
            AddHole(new Prism(new Vector3(1, 0, 1), new Vector3(-1, 0, -1), .1f, 3));
            AddHole(new Prism(new Vector3(0, 2, 0), Vector3.UnitY, .1f, 8));
            AddHole(new Prism(new Vector3(2, .5f, .5f), Vector3.UnitX, .1f, 8));
        }
        private void SetModel(VertexPositionNormalTexture[] vertices)
        {
            model.Meshes.Clear();
            model.Materials.Clear();
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
            model.Meshes.Add(customMesh);
            this.vertices = vertices;
            model.Materials.Add(material);
        }
        //TODO: get the vertices back from the mesh
        private VertexPositionNormalTexture[] ExtractVertices(Model model)
        {
            (List<Vector3> verts, List<int> _) = MeshExtensions.GetMeshVerticesAndIndices(model, Game);
            var res = new VertexPositionNormalTexture[verts.Count];
            int idx = 0;
            foreach (Vector3 v in verts)
            {
                res[idx].Position = v;
                idx++;
            }
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
            for(int i = 0; i < res.Length; i++)
            {
                res[i].TextureCoordinate = new Vector2(.5f, .5f);
            }
            SetModel(res);
        }
    }
}

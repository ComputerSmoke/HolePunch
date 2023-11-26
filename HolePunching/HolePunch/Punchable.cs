using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;

namespace HolePunching.HolePunch
{
    public class Punchable : StartupScript
    {

        private Model model;
        private VertexPositionTexture[] vertices;
        private MaterialInstance material;
        public override void Start()
        {
            base.Start();
            GeometryHelper.Init();
            model = Entity.Get<ModelComponent>().Model;
            material = model.Materials[0];
            vertices = ExtractVertices(model);
            SetModel(vertices);
            AddHole(new Prism(new Vector3(2, 0, 0), Vector3.UnitX, .1f, 8));
            //AddHole(new Prism(new Vector3(1, -.5f, 0), Vector3.UnitX, .1f, 6));
            //AddHole(new Prism(new Vector3(1, 0, 1), new Vector3(-1, 0, -1), .1f, 3));
            //AddHole(new Prism(new Vector3(0, 2, 0), Vector3.UnitY, .1f, 8));
            //AddHole(new Prism(new Vector3(2, .5f, .5f), Vector3.UnitX, .1f, 8));
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
                    VertexBuffers = [new VertexBufferBinding(vertexBuffer,
                                  VertexPositionTexture.Layout, vertexBuffer.ElementCount)],
                }
            };
            // add the mesh to the model
            model.Meshes.Add(customMesh);
            this.vertices = vertices;
            model.Materials.Add(material);
        }
        //TODO: get the vertices back from the mesh
        private VertexPositionTexture[] ExtractVertices(Model model)
        {
            (List<Vector3> verts, List<int> _) = MeshExtensions.GetMeshVerticesAndIndices(model, Game);
            var res = new VertexPositionTexture[verts.Count];
            int idx = 0;
            foreach (Vector3 v in verts)
            {
                res[idx].Position = v;
                idx++;
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
        private static VertexPositionTexture[] Face(Vector3 pos, Quaternion rot)
        {
            VertexPositionTexture[] res = new VertexPositionTexture[6];

            res[0].Position = pos;
            res[1].Position = pos + new Vector3(1, 0, 0);
            res[2].Position = pos + new Vector3(1, 0, 1);
            res[5].Position = pos;
            res[4].Position = pos + new Vector3(0, 0, 1);
            res[3].Position = pos + new Vector3(1, 0, 1);
            for (int i = 0; i < 6; i++)
                rot.Rotate(ref res[i].Position);
            return res;
        }
        public void AddHole(Prism hole)
        {
            SetModel(HolePunch.PunchHole(vertices, hole));
        }
    }
}

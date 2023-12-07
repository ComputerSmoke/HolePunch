using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HolePuncher.Volumes.Faces;

namespace HolePuncher.Volumes
{
    public class VolumeRenderer : StartupScript
    {

        private ModelComponent modelComponent;
        public override void Start()
        {
            base.Start();
            modelComponent = Entity.Get<ModelComponent>();
        }
        public void Render(Volume volume)
        {
            SetModel(Triangle.TrianglesToVertices(volume.GetTriangles()));
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
            //modelComponent.Model.Materials.Add(material);
        }
    }
}

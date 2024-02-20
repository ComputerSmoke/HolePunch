using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using HolePuncher.Volumes.Faces;
using HolePuncher.Volumes;
using NetTopologySuite.Index.IntervalRTree;
using Stride.Rendering;
using Stride.Engine;
using Stride.Graphics;

namespace HolePuncher
{
    internal class FaceTree(Mesh originalMesh, int innerMaterialIndex, Vector3 p0, Vector3 p1, GraphicsDevice graphicsDevice, int maxVerts, float atomicVolume)
    {
        private FaceTree left;
        private FaceTree right;
        private List<Triangle> verts = [];
        private readonly Box box = new (p0, p1);
        private readonly GraphicsDevice graphicsDevice = graphicsDevice;
        public readonly int maxVerts = maxVerts;
        public readonly float atomicVolume = atomicVolume;
        private Mesh outerMesh;
        private Mesh innerMesh;
        public readonly Mesh originalMesh = originalMesh;
        private readonly int innerMaterialIndex = innerMaterialIndex;
        //Set vertices of this tree
        public void SetVertices(List<Triangle> verts)
        {
            if (Atomic())
            {
                verts = [];
                return;
            }
            List<Triangle> cropped = box.Crop(verts);
            if (!Leaf())
            {
                left.SetVertices(verts);
                right.SetVertices(verts);
            } else
                SetVerticesLeaf(cropped);
        }
        private void SetVerticesLeaf(List<Triangle> verts)
        {
            this.verts = verts;
            (innerMesh, outerMesh) = BuildMeshes();
            Split();
        }
        //Build outer and inner meshes from triangles
        private (Mesh, Mesh) BuildMeshes()
        {
            var (innerVerts, outerVerts) = Triangle.TrianglesToInnerOuterVertices(verts);
            Mesh innerMesh = BuildMesh(innerVerts, graphicsDevice, innerMaterialIndex);
            Mesh outerMesh = BuildMesh(outerVerts, graphicsDevice, originalMesh.MaterialIndex);
            return (innerMesh, outerMesh);
        }
        private Mesh BuildMesh(VertexPositionNormalTexture[] vertices, GraphicsDevice graphicsDevice, int materialIndex)
        {
            if(vertices.Length == 0)
                return null;
            var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(graphicsDevice, vertices,
                                                                 GraphicsResourceUsage.Dynamic);
            int[] indices = new int[vertices.Length];
            for (int i = 0; i < indices.Length; i++)
                indices[i] = i;
            var indexBuffer = Stride.Graphics.Buffer.Index.New(graphicsDevice, indices);

            return new Mesh
            {
                Draw = new MeshDraw
                {
                    /* Vertex buffer and index buffer setup */
                    PrimitiveType = PrimitiveType.TriangleList,
                    DrawCount = indices.Length,
                    IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Length),
                    VertexBuffers = [new VertexBufferBinding(vertexBuffer,
                                  VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount)],
                },
                MaterialIndex = materialIndex,
                NodeIndex = originalMesh.NodeIndex
            };
        }
        //Split into two children along axis of greatest length
        private void Split()
        {
            if (verts.Count <= maxVerts)
                return;
            Vector3 diff = box.BoundingBox.max - box.BoundingBox.min;
            if(Math.Abs(diff.X) > Math.Abs(diff.Y) && Math.Abs(diff.X) > Math.Abs(diff.Z))
            {
                Vector3 p1 = box.BoundingBox.min;
                Vector3 p2 = new(box.BoundingBox.min.X + diff.X/2, box.BoundingBox.max.Y, box.BoundingBox.max.Z);
                left = new FaceTree(originalMesh, innerMaterialIndex, p1, p2, graphicsDevice, maxVerts, atomicVolume);
                left.SetVertices(verts);
                Vector3 p3 = new (box.BoundingBox.min.X + diff.X / 2, box.BoundingBox.min.Y, box.BoundingBox.min.Z);
                Vector3 p4 = new(box.BoundingBox.max.X, box.BoundingBox.max.Y, box.BoundingBox.max.Z);
                right = new FaceTree(originalMesh, innerMaterialIndex, p3, p4, graphicsDevice, maxVerts, atomicVolume);
                right.SetVertices(verts);
            } else if(Math.Abs(diff.Y) > Math.Abs(diff.Z))
            {
                Vector3 p1 = box.BoundingBox.min;
                Vector3 p2 = new(box.BoundingBox.max.X, box.BoundingBox.min.Y + diff.Y/2, box.BoundingBox.max.Z);
                left = new FaceTree(originalMesh, innerMaterialIndex, p1, p2, graphicsDevice, maxVerts, atomicVolume);
                left.SetVertices(verts);
                Vector3 p3 = new(box.BoundingBox.min.X, box.BoundingBox.min.Y + diff.Y / 2, box.BoundingBox.min.Z);
                Vector3 p4 = new(box.BoundingBox.max.X, box.BoundingBox.max.Y, box.BoundingBox.max.Z);
                right = new FaceTree(originalMesh, innerMaterialIndex, p3, p4, graphicsDevice, maxVerts, atomicVolume);
                right.SetVertices(verts);
            } else
            {
                Vector3 p1 = box.BoundingBox.min;
                Vector3 p2 = new(box.BoundingBox.max.X, box.BoundingBox.max.Y, box.BoundingBox.min.Z + diff.Z / 2);
                left = new FaceTree(originalMesh, innerMaterialIndex, p1, p2, graphicsDevice, maxVerts, atomicVolume);
                left.SetVertices(verts);
                Vector3 p3 = new(box.BoundingBox.min.X, box.BoundingBox.min.Y, box.BoundingBox.min.Z + diff.Z / 2);
                Vector3 p4 = new(box.BoundingBox.max.X, box.BoundingBox.max.Y, box.BoundingBox.max.Z);
                right = new FaceTree(originalMesh, innerMaterialIndex, p3, p4, graphicsDevice, maxVerts, atomicVolume);
                right.SetVertices(verts);
            }
            verts = [];
        }
        private bool Leaf()
        {
            return left == null;
        }
        private bool Atomic()
        {
            return box.Volume() <= atomicVolume;
        }
        public List<Mesh> GetMeshes()
        {
            if (Leaf())
            {
                if (innerMesh != null && outerMesh != null)
                    return [innerMesh, outerMesh];
                if (innerMesh != null)
                    return [innerMesh];
                if(outerMesh != null)
                    return [outerMesh];
                return [];
            }
            List<Mesh> res = left.GetMeshes();
            res.AddRange(right.GetMeshes());
            return res;
        }
        public List<Triangle> GetVertices()
        {
            if (Leaf())
                return verts;
            var res = left.GetVertices();
            res.AddRange(right.GetVertices());
            return res;
        }
        public void PunchHole(Prism hole)
        {
            if(!box.IntersectsPrism(hole))
                return;
            if(box.InsidePrism(hole))
            {
                left = null;
                right = null;
                verts = [];
            }
            if (!Leaf())
            {
                left.PunchHole(hole);
                right.PunchHole(hole);
            }
            else if (verts.Count > 0)
                SetVerticesLeaf(HolePunch.PunchHole(verts, hole));
        }
    }
}

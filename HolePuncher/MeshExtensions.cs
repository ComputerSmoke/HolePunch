using Stride.Games;
using Stride.Rendering;
using System;
using Stride.Core.Serialization;
using Stride.Core;
using Stride.Physics;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using HolePuncher.Volumes.Faces;
using Stride.Graphics;
using System.ComponentModel;

namespace HolePuncher
{
    public static class MeshExtensions
    {
        struct VertexTexture(Vector3 pos, Vector2 textPos)
        {
            public Vector3 pos = pos;
            public Vector2 textPos = textPos;
        }
        //Get triangles that make up mesh, in local mesh space
        public static List<Triangle> GetTriangles(this Mesh mesh, IGame game) {
            List<Triangle> res = [];
            var (verts, indices) = mesh.GetMeshVerticesAndIndices(game);

            for(int i = 0; i+2 < indices.Count; i += 3)
            {
                Vector3 v1 = verts[indices[i]].pos;
                Vector3 v2 = verts[indices[i+1]].pos;
                Vector3 v3 = verts[indices[i+2]].pos;
                Triangle triangle = new(v1, v2, v3)
                {
                    T1 = verts[indices[i]].textPos,
                    T2 = verts[indices[i + 1]].textPos,
                    T3 = verts[indices[i + 2]].textPos,
                    IsOuterFace = true
                };
                res.Add(triangle);
            }
            return res;
        }
        //Convert point from model space to mesh space
        public static Vector3 PointToMeshSpace(this Mesh mesh, Skeleton skeleton, Vector3 input)
        {
            List<TransformTRS> transforms = ParentTransforms(skeleton.Nodes, mesh.NodeIndex);
            foreach (TransformTRS transform in transforms)
            {
                input -= transform.Position;
                input /= transform.Scale;
                Quaternion rev = transform.Rotation;
                rev.Invert();
                rev.Rotate(ref input);
            }
            return input;
        }
        //Convert direction from model to mesh space
        public static Vector3 DirToMeshSpace(this Mesh mesh, Skeleton skeleton, Vector3 input)
        {
            List<TransformTRS> transforms = ParentTransforms(skeleton.Nodes, mesh.NodeIndex);
            foreach (TransformTRS transform in transforms)
            {
                Quaternion rev = transform.Rotation;
                rev.Invert();
                rev.Rotate(ref input);
            }
            return input;
        }
        //Scale value from model space to mesh space
        public static float ScaleToMeshSpace(this Mesh mesh, Skeleton skeleton, float input)
        {
            List<TransformTRS> transforms = ParentTransforms(skeleton.Nodes, mesh.NodeIndex);
            foreach (TransformTRS transform in transforms)
            {
                if (Math.Abs(transform.Scale.X - transform.Scale.Y) > 1e-6f && Math.Abs(transform.Scale.X - transform.Scale.Z) > 1e-6f)
                    throw new UnpunchableMeshException("X Y Z scale of mesh must be the same. Recommend exporting the model with 'use space transform' disabled.");
                input /= transform.Scale.X;
            }
            return input;
        }
        //Get the list of transforms, starting with base, that are applied to bring a point from local mesh space to model space
        private static List<TransformTRS> ParentTransforms(ModelNodeDefinition[] nodes, int idx)
        {
            if (idx < 0)
                return [];
            if (nodes[idx].ParentIndex == idx)
                return [nodes[idx].Transform];
            List<TransformTRS> res = ParentTransforms(nodes, nodes[idx].ParentIndex);
            res.Add(nodes[idx].Transform);
            return res;
        }
        private static (List<VertexTexture> verts, List<int> indices) GetMeshVerticesAndIndices(this Mesh mesh, IGame game)
        {
            return GetMeshData(mesh, game.Services, game);
        }

        static unsafe (List<VertexTexture> verts, List<int> indices) GetMeshData(Mesh meshData, IServiceRegistry services, IGame game)
        {
            //NOTE: FBX should be exported from blender with Use space transform disabled to avoid floating point precision errors and stuff
            var combinedVerts = new List<VertexTexture>(meshData.Draw.VertexBuffers[0].Count);
            var combinedIndices = new List<int>(meshData.Draw.IndexBuffer.Count);

            var vBuffer = meshData.Draw.VertexBuffers[0].Buffer;
            var iBuffer = meshData.Draw.IndexBuffer.Buffer;
            byte[] verticesBytes = vBuffer.GetData<byte>(game.GraphicsContext.CommandList); ;
            byte[] indicesBytes = iBuffer.GetData<byte>(game.GraphicsContext.CommandList); ;

            if ((verticesBytes?.Length ?? 0) == 0 || (indicesBytes?.Length ?? 0) == 0)
            {
                throw new InvalidOperationException(
                    $"Failed to find mesh buffers while attempting to build a {nameof(StaticMeshColliderShape)}. " +
                    $"Make sure that the model is either an asset on disk, or has its buffer data attached to the buffer through '{nameof(AttachedReference)}'\n" +
                    $"You can also explicitly build a {nameof(StaticMeshColliderShape)} using the second constructor instead of this one.");
            }

            int vertMappingStart = combinedVerts.Count;

            fixed (byte* bytePtr = verticesBytes)
            {
                var vBindings = meshData.Draw.VertexBuffers[0];
                int count = vBindings.Count;
                int stride = vBindings.Declaration.VertexStride;
                int posOffset = 0;
                int textureOffset = 0;
                int byteCounter = 0;

                foreach(var elt in vBindings.Declaration.VertexElements)
                {
                    if (elt.SemanticAsText == "POSITION")
                        posOffset = byteCounter;
                    else if (elt.SemanticAsText == "TEXCOORD")
                        textureOffset = byteCounter;
                    byteCounter += elt.Format.SizeInBytes();
                }
                for (int i = 0, vHead = vBindings.Offset; i < count; i++, vHead += stride)
                {
                    var pos = *(Vector3*)(bytePtr + vHead + posOffset);
                    var texture = *(Vector2*)(bytePtr + vHead + textureOffset);

                    combinedVerts.Add(new(pos, texture));
                }
            }

            fixed (byte* bytePtr = indicesBytes)
            {
                if (meshData.Draw.IndexBuffer.Is32Bit)
                {
                    foreach (int i in new Span<int>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                    {
                        combinedIndices.Add(vertMappingStart + i);
                    }
                }
                else
                {
                    foreach (ushort i in new Span<ushort>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                    {
                        combinedIndices.Add(vertMappingStart + i);
                    }
                }
            }
            return (combinedVerts, combinedIndices);
        }
    }
}
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

namespace HolePuncher
{
    public static class MeshExtensions
    {
        struct VertexTexture(Vector3 pos, Vector2 textPos)
        {
            public Vector3 pos = pos;
            public Vector2 textPos = textPos;
        }
        public static List<Triangle> GetTriangles(this Model model, IGame game) {
            List<Triangle> res = [];
            var (verts, indices) = model.GetMeshVerticesAndIndices(game);
            for(int i = 0; i+2 < indices.Count; i += 3)
            {
                Vector3 v1 = verts[indices[i]].pos;
                Vector3 v2 = verts[indices[i+1]].pos;
                Vector3 v3 = verts[indices[i+2]].pos;
                Triangle triangle = new(v1, v2, v3)
                {
                    IsOuterFace = true,
                    T1 = verts[indices[i]].textPos,
                    T2 = verts[indices[i + 1]].textPos,
                    T3 = verts[indices[i + 2]].textPos
                };
                res.Add(triangle);
            }
            return res;
        }
        private static (List<VertexTexture> verts, List<int> indices) GetMeshVerticesAndIndices(this Model model, IGame game)
        {
            return GetMeshData(model, game.Services, game);
        }

        static unsafe (List<VertexTexture> verts, List<int> indices) GetMeshData(Model model, IServiceRegistry services, IGame game)
        {

            int totalVerts = 0, totalIndices = 0;
            foreach (var meshData in model.Meshes)
            {
                totalVerts += meshData.Draw.VertexBuffers[0].Count;
                totalIndices += meshData.Draw.IndexBuffer.Count;
            }

            var combinedVerts = new List<VertexTexture>(totalVerts);
            var combinedIndices = new List<int>(totalIndices);

            foreach (var meshData in model.Meshes)
            {
                var vBuffer = meshData.Draw.VertexBuffers[0].Buffer;
                var iBuffer = meshData.Draw.IndexBuffer.Buffer;
                byte[] verticesBytes = vBuffer.GetData<byte>(game.GraphicsContext.CommandList); ;
                byte[] indicesBytes = iBuffer.GetData<byte>(game.GraphicsContext.CommandList); ;

                if ((verticesBytes?.Length ?? 0) == 0 || (indicesBytes?.Length ?? 0) == 0)
                {
                    throw new InvalidOperationException(
                        $"Failed to find mesh buffers while attempting to build a {nameof(StaticMeshColliderShape)}. " +
                        $"Make sure that the {nameof(model)} is either an asset on disk, or has its buffer data attached to the buffer through '{nameof(AttachedReference)}'\n" +
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
                    //TODO: support multiple textures?
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
            }
            return (combinedVerts, combinedIndices);
        }
    }
}
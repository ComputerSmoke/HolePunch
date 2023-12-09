using HolePuncher.Volumes;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using HolePuncher.Volumes.Faces;
using NetTopologySuite.Noding;
using Stride.Core.Collections;
using Stride.Games;

namespace HolePuncher
{
    public class Puncher
    {
        private readonly ModelComponent modelComponent;
        private readonly Skeleton skeleton;
        private readonly Queue<Prism> holeQueue;
        private readonly List<FaceTree> faceTrees;
        private bool queueLock;
        //Intialize from existing model 
        public Puncher(IGame game, ModelComponent modelComponent, Material innerMaterial, int leafCapacity, float atomicVolume)
        {
            this.modelComponent = modelComponent;
            skeleton = modelComponent.Model.Skeleton;
            modelComponent.Materials[modelComponent.Materials.Count] = innerMaterial;
            faceTrees = [];
            foreach (Mesh mesh in modelComponent.Model.Meshes)
            {
                List<Triangle> triangles = mesh.GetTriangles(game);
                var (p0, p1) = VertBounds(triangles);
                FaceTree tree = new(mesh, modelComponent.Materials.Count-1, p0, p1, game.GraphicsDevice, leafCapacity, atomicVolume);
                tree.SetVertices(triangles);
                faceTrees.Add(tree);
            }
            UpdateModel();
            holeQueue = [];
        }
        private static (Vector3, Vector3) VertBounds(List<Triangle> triangles)
        {
            Vector3 min = new(1e6f, 1e6f, 1e6f);
            Vector3 max = new(-1e6f, -1e6f, -1e6f);
            static Vector3 MinVert(Triangle triangle)
            {
                float minX = Math.Min(triangle.V1.X, Math.Min(triangle.V2.X, triangle.V3.X));
                float minY = Math.Min(triangle.V1.Y, Math.Min(triangle.V2.Y, triangle.V3.Y));
                float minZ = Math.Min(triangle.V1.Z, Math.Min(triangle.V2.Z, triangle.V3.Z));
                return new Vector3(minX, minY, minZ);
            }
            static Vector3 MaxVert(Triangle triangle)
            {
                float x = Math.Max(triangle.V1.X, Math.Max(triangle.V2.X, triangle.V3.X));
                float y = Math.Max(triangle.V1.Y, Math.Max(triangle.V2.Y, triangle.V3.Y));
                float z = Math.Max(triangle.V1.Z, Math.Max(triangle.V2.Z, triangle.V3.Z));
                return new Vector3(x, y, z);
            }
            foreach (Triangle triangle in triangles)
            {
                Vector3 tmin = MinVert(triangle);
                Vector3 tmax = MaxVert(triangle);
                min.X = Math.Min(min.X, tmin.X);
                min.Y = Math.Min(min.Y, tmin.Y);
                min.Z = Math.Min(min.Z, tmin.Z);
                max.X = Math.Max(max.X, tmax.X);
                max.Y = Math.Max(max.Y, tmax.Y);
                max.Z = Math.Max(max.Z, tmax.Z);
            }
            return (min, max);
        }
        private void UpdateModel()
        {
            List<Mesh> meshes = [];
            foreach(FaceTree faceTree in faceTrees)
                meshes.AddRange(faceTree.GetMeshes());
            Model model = [.. meshes];
            model.Skeleton = skeleton;
            modelComponent.Model = model;
        }
        public void AddHole(Prism hole)
        {
            holeQueue.Enqueue(hole);
            EvalHoleQueue();
        }
        private async void EvalHoleQueue()
        {
            if (queueLock)
                return;
            queueLock = true;
            await Task.Run(() =>
            {
                while (holeQueue.Count > 0)
                {
                    Prism hole = holeQueue.Dequeue();
                    foreach(FaceTree faceTree in faceTrees)
                    {
                        Prism localHole = hole.ToMeshSpace(faceTree.originalMesh, skeleton);
                        faceTree.PunchHole(localHole);
                    }
                    UpdateModel();
                }
            });
            queueLock = false;
        }
    }
}

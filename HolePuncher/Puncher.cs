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

namespace HolePuncher
{
    public class Puncher
    {
        private readonly ModelComponent modelComponent;
        private readonly Material innerMaterial;
        private readonly Material outerMaterial;
        private readonly FaceTree faceTree;
        private readonly Queue<Prism> holeQueue;
        private bool queueLock;
        //Initialize with new volume shape
        public Puncher(Volume initialShape, ModelComponent modelComponent,
        Material innerMaterial, Material outerMaterial, GraphicsDevice graphicsDevice, int leafCapacity, float atomicVolume)
        {
            this.modelComponent = modelComponent;
            this.innerMaterial = innerMaterial;
            this.outerMaterial = outerMaterial;
            Vector3 v0 = initialShape.BoundingBox.min;
            Vector3 v1 = initialShape.BoundingBox.max;
            faceTree = new(v0, v1, graphicsDevice, leafCapacity, atomicVolume);
            faceTree.SetVertices(initialShape.GetTriangles());
            UpdateModel();
            holeQueue = [];
        }
        //Intialize from existing model 
        public Puncher(Stride.Games.IGame game, ModelComponent modelComponent, Material innerMaterial, int leafCapacity, float atomicVolume)
        {
            this.modelComponent = modelComponent;
            this.innerMaterial = innerMaterial;
            this.outerMaterial = modelComponent.Model.Materials[0].Material;
            List<Triangle> triangles = modelComponent.Model.GetTriangles(game);
            var (v0, v1) = VertBounds(triangles);
            faceTree = new(v0, v1, game.GraphicsDevice, leafCapacity, atomicVolume);
            faceTree.SetVertices(triangles);
            UpdateModel();
            holeQueue = [];
        }
        private static (Vector3, Vector3) VertBounds(List<Triangle> triangles)
        {
            Vector3 min = new (1e6f, 1e6f, 1e6f);
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
            modelComponent.Model = faceTree.GetModel();
            modelComponent.Materials[0] = innerMaterial;
            modelComponent.Materials[1] = outerMaterial;
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
                    faceTree.PunchHole(hole);
                    UpdateModel();
                }
            });
            queueLock = false;
        }
    }
}

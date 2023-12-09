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

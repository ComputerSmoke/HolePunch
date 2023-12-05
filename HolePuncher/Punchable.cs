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
using HolePuncher.Volumes.Faces;

namespace HolePuncher
{
    public class Punchable : StartupScript
    {

        private ModelComponent modelComponent;
        private Material material;
        public VolumeRenderer DebugHoleRenderer { get; set; }
        private FaceTree faceTree;
        private Queue<Prism> holeQueue;
        private bool queueLock;
        public int VertsPerNode { get; set; }
        public float MinNodeVolume { get; set; }
        public override void Start()
        {
            base.Start();
            modelComponent = Entity.Get<ModelComponent>();
            modelComponent.Model = new();
            material = Content.Load<Material>("Sphere Material");
            faceTree = new(new Vector3(-.5f, -.5f, -.5f), new Vector3(.5f, .5f, .5f), GraphicsDevice, VertsPerNode, MinNodeVolume);
            faceTree.SetVertices(Cube());
            UpdateModel();
            holeQueue = new Queue<Prism>();
            AddHole(new Prism(new Vector3(2, 0, 0), Vector3.UnitX, .1f, 8));
            /*AddHole(new Prism(new Vector3(1, -.5f, 0), Vector3.UnitX, .1f, 6));
            AddHole(new Prism(new Vector3(1, 0, 1), new Vector3(-1, 0, -1), .1f, 3));
            AddHole(new Prism(new Vector3(0, 2, 0), Vector3.UnitY, .1f, 8));
            AddHole(new Prism(new Vector3(2, .5f, .5f), Vector3.UnitX, .1f, 8));*/
            //AddChipFromWorld(Vector3.Zero, Vector3.UnitY, 1, 1);
        }
        private void UpdateModel()
        {
            modelComponent.Model = faceTree.GetModel();
            modelComponent.Materials[0] = material;
        }
        
        //TODO: get the vertices back from the mesh
        /*private VertexPositionNormalTexture[] ExtractVertices(Model model)
        {
            return Cube();
        }*/
        private static List<Triangle> Cube()
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

            List<Triangle> verts = [];
            for(int i = 0; i < res.Count - 2; i += 3)
            {
                verts.Add(new Triangle(res[i].Position, res[i + 1].Position, res[i + 2].Position));
            }

            return verts;
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
    }
}

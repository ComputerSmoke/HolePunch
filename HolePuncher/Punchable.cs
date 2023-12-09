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

        private Puncher puncher;
        public float sx;
        public float sy;
        public float sz;
        public int LeafCapacity { get; set; }
        public float AtomicSize { get; set; }
        public Material InnerMaterial { get; set; }
        public Material OuterMaterial { get; set; }
        public override void Start()
        {
            base.Start();
            ModelComponent modelComponent = Entity.Get<ModelComponent>();
            Vector3 p0 = new(-sx / 2, -sy / 2, -sz / 2);
            Vector3 p1 = p0 * -1;
            Box initialVolume = new(p0, p1);
            puncher = new(initialVolume, modelComponent, InnerMaterial, OuterMaterial, GraphicsDevice, LeafCapacity, AtomicSize);
        }
        public void AddHoleFromWorld(Vector3 pos, Vector3 dir, float radius)
        {
            dir.Normalize();
            Entity.Transform.GetWorldTransformation(out Vector3 worldPos, out Quaternion rot, out _);
            rot.Invert();
            rot.Rotate(ref dir);
            pos -= worldPos;
            rot.Rotate(ref pos);
            pos -= dir * .1f;
            Prism hole = new(pos, -dir, radius, 6);
            puncher.AddHole(hole);
        }
    }
}

﻿using Stride.Engine;
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
        public int LeafCapacity { get; set; }
        public float AtomicSize { get; set; }
        public Material InnerMaterial { get; set; }
        public override void Start()
        {
            base.Start();
            ModelComponent modelComponent = Entity.Get<ModelComponent>();
            puncher = new(Game, modelComponent, InnerMaterial, LeafCapacity, AtomicSize);
        }
        public void AddHoleFromWorld(Vector3 pos, Vector3 dir, float radius)
        {
            dir.Normalize();
            Entity.Transform.GetWorldTransformation(out Vector3 worldPos, out Quaternion rot, out Vector3 scale);
            if (Math.Abs(scale.X - scale.Y) > 1e-6 || Math.Abs(scale.X - scale.Z) > 1e-6)
                throw new UnpunchableMeshException("X Y and Z scales of model must be equal");
            rot.Invert();
            rot.Rotate(ref dir);
            pos -= worldPos;
            rot.Rotate(ref pos);
            pos -= dir * .1f;
            pos /= scale.X;
            Prism hole = new(pos, -dir, radius, 6);
            puncher.AddHole(hole);
        }
    }
}

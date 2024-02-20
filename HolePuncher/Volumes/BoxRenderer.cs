using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;

namespace HolePuncher.Volumes
{
    public class BoxRenderer : VolumeRenderer
    {
        public Vector3 v0;
        public Vector3 v1;
        private Box box;
        public override void Start()
        {
            base.Start();
            box = new(v0, v1);
            Render(box);
        }
    }
}

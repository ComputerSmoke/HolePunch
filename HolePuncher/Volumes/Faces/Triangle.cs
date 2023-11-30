using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;

namespace HolePuncher.Volumes.Faces
{
    public class Triangle(Vector3 p1, Vector3 p2, Vector3 p3) : Face([p1, p2, p3])
    {
        public Vector3 V1 { get { return vertices[0]; } }
        public Vector3 V2 { get { return vertices[1]; } }
        public Vector3 V3 { get { return vertices[2]; } }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HolePuncher.Volumes.Faces;
using Stride.Core.Mathematics;

namespace HolePuncher.Volumes
{
    public class Pyramid(Vector3 pos, float width, float height) : Volume(GetFaces(pos, width, height), GetBoundingBox(pos, width, height))
    {
        private static (Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) Vertices(Vector3 pos, float width, float height)
        {
            Vector3 p0 = pos;
            Vector3 p1 = pos + (Vector3.UnitX * width);
            Vector3 p2 = pos + (new Vector3(.5f, 0, .8660254038f) * width);
            Vector3 p3 = pos + (new Vector3(.5f, 0, .2886751345f) * width) + (Vector3.UnitY * height);
            return (p0, p1, p2, p3);
        }
        private static Face[] GetFaces(Vector3 pos, float width, float height)
        {
            var (p0, p1, p2, p3) = Vertices(pos, width, height);
            return [
                new Face([p0, p2, p1]),
                new Face([p0, p3, p2]),
                new Face([p0, p1, p3]),
                new Face([p1, p2, p3])
            ];
        }
        private static BoundingBox3D GetBoundingBox(Vector3 pos, float width, float height)
        {
            var (p0, p1, p2, p3) = Vertices(pos, width, height);
            return new BoundingBox3D([p0, p1, p2, p3]);
        }
    }
}

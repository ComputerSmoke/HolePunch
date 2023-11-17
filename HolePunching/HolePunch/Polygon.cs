using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.MediaFoundation;
using Stride.Core.Mathematics;

namespace HolePunching.HolePunch
{
    internal class Polygon(Vector2[] vertices)
    {
        //vertices in clockwise order
        public readonly Vector2[] vertices = vertices;
        //perps are normal to edges, but may not have magnitude 1
        private readonly Vector2[] perps = Perpendiculars(vertices);

        //True if point is strictly inside polygon
        public bool Contains(Vector2 point)
        {
            for(int i = 0; i < vertices.Length; i++)
            {
                Vector2 v1 = vertices[i];
                Vector2 normal = perps[i];
                float dist = Vector2.Dot(normal, point - v1);
                if(dist <= 0)
                    return false;
            }
            return true;
        }
        private static Vector2[] Perpendiculars(Vector2[] vertices)
        {
            Vector2[] normals = new Vector2[vertices.Length-1];
            for(int i = 0; i < vertices.Length; i++)
            {
                Vector2 v1 = vertices[i];
                Vector2 v2 = vertices[(i+1)%vertices.Length];
                Vector2 edge = v2 - v1;
                //normal is edge rotated 90 degrees clockwise, length normalized
                normals[i] = new Vector2(edge.Y, -edge.X);
            }
            return normals;
        }
    }
}

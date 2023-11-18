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
        private readonly Edge[] edges = Edges(vertices);

        //True if point is strictly inside polygon
        public bool Contains(Vector2 point)
        {
            for(int i = 0; i < edges.Length; i++)
            {
                Edge edge = edges[i];
                Vector2 v1 = edge.v1;
                float dist = Vector2.Dot(edge.Perpendicular, point - v1);
                if(dist <= 0)
                    return false;
            }
            return true;
        }
        //Return triangles whose union makes this polygon
        public Polygon[] Partition()
        {

        }
        private static Edge[] Edges(Vector2[] vertices)
        {
            Edge[] edges = new Edge[vertices.Length];
            for(int i = 0; i < vertices.Length; i++)
            {
                Vector2 v1 = vertices[i];
                Vector2 v2 = vertices[(i + 1)%vertices.Length];
                edges[i] = new Edge(v1, v2);
            }
            return edges;
        }
    }
}

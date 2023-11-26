using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Engine;
using Stride.Rendering;
using Stride.Graphics;
using Stride.Extensions;
using Stride.Core.Mathematics;
using System.Collections;
using Stride.Particles.Updaters.FieldShapes;
using Valve.VR;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate.Polygon;
using NetTopologySuite.Triangulate.Tri;
using NetTopologySuite.Noding;
using BulletSharp;

namespace HolePunching.HolePunch
{
    //Line segment intersections with faces, used to construct walls of hole
    struct WallIntersect(Prism hole, Plane trianglePlane, Coordinate c1, Coordinate c2)
    {
        public Vector3 p1 = trianglePlane.ToWorldSpace(c1); 
        public Vector3 p2 = trianglePlane.ToWorldSpace(c2); 
        public bool forward = Vector3.Dot(trianglePlane.normal, hole.facePlane.normal) > 0;
    }
    struct WallIntersect2D(Plane plane, WallIntersect intersect)
    {
        public LineString line = GeometryHelper.CreateLineSegment(plane.ToPlaneSpace(intersect.p1), plane.ToPlaneSpace(intersect.p2));
        public bool forward = intersect.forward;
    }
    //Represents a triangle in 3 space
    struct Triangle (Vector3 v1, Vector3 v2, Vector3 v3)
    {
        public Vector3 v1 = v1;
        public Vector3 v2 = v2;
        public Vector3 v3 = v3;
        public Plane plane = new(v1, Vector3.Cross(v3 - v1, v2 - v1));
    }
    public static class HolePunch
    {
        public static VertexPositionTexture[] PunchHole(VertexPositionTexture[] vertices, Prism hole) {
            List<Triangle> punched = [];
            List<WallIntersect>[] wallIntersects = new List<WallIntersect>[hole.face.Coordinates.Length - 1];
            Plane[] wallPlanes = new Plane[wallIntersects.Length];
            for(int i = 0; i < hole.face.Coordinates.Length-1; i++)
            {
                wallIntersects[i] = [];
                wallPlanes[i] = EdgePlane(hole.face.Coordinates[i], hole.face.Coordinates[i+1], hole.facePlane);
            }
            for (int i = 0; i < vertices.Length-2; i += 3)
            {
                Triangle triangle = new(vertices[i].Position, vertices[i + 1].Position, vertices[i + 2].Position);
                Polygon trianglePoly = GeometryHelper.CreateTriangle(
                    triangle.plane.ToPlaneSpace(triangle.v1),
                    triangle.plane.ToPlaneSpace(triangle.v2),
                    triangle.plane.ToPlaneSpace(triangle.v3)
                );
                Polygon holeSlice = hole.Slice(triangle.plane, trianglePoly);
                Polygon smallHoleSlice = hole.SmallSlice(triangle.plane, trianglePoly);
                punched.AddRange(Punch(triangle, hole, trianglePoly, smallHoleSlice));
                AddWallIntersects(wallIntersects, triangle, hole, trianglePoly, holeSlice);
            }
            List<Triangle> walls = BuildAllWalls(wallIntersects, wallPlanes);
            punched.AddRange(walls);
            VertexPositionTexture[] newVertices = new VertexPositionTexture[punched.Count * 3];
            for(int i = 0; i < punched.Count; i++)
            {
                newVertices[i * 3].Position = punched[i].v1;
                newVertices[i * 3 + 1].Position = punched[i].v2;
                newVertices[i * 3 + 2].Position = punched[i].v3;
            }
            return newVertices;
        }
        private static Plane EdgePlane(Coordinate c1, Coordinate c2, Plane facePlane)
        {
            Vector3 p1 = facePlane.ToWorldSpace(c1);
            Vector3 p2 = facePlane.ToWorldSpace(c2);
            Vector3 unitX = (p2 - p1);
            unitX.Normalize();
            Vector3 unitY = -facePlane.normal;
            unitY.Normalize();
            return new Plane(p1, unitX, unitY);
        }
        //Returns list of triangles that make all walls of hole
        private static List<Triangle> BuildAllWalls(List<WallIntersect>[] wallIntersects, Plane[] wallPlanes)
        {
            List<Triangle> res = [];
            for (int i = 0; i < wallIntersects.Length; i++)
                res.AddRange(BuildWalls(wallIntersects[i], wallPlanes[i]));
            return res;
        }
        private static Triangle[] BuildWalls(List<WallIntersect> wallIntersects, Plane plane)
        {
            if(wallIntersects.Count < 2)
                return [];
            WallIntersect2D[] intersects = new WallIntersect2D[wallIntersects.Count];
            for (int i = 0; i < intersects.Length; i++)
                intersects[i] = new(plane, wallIntersects[i]);
            Array.Sort(intersects, delegate (WallIntersect2D intersect1, WallIntersect2D intersect2)
            {
                //Sort by which is further forward at an midpoint of one of the lines. For non intersecting, this puts line in front first.
                LineString line1 = intersect1.line;
                LineString line2 = intersect2.line;
                Coordinate p1 = line1.Coordinates[0];
                Coordinate p2 = line1.Coordinates[^1];
                Coordinate q1 = line2.Coordinates[0];
                Coordinate q2 = line2.Coordinates[^1];
                double x = (q1.X + q2.X) / 2;
                double y = (q1.Y + q2.Y) / 2;
                double y1 = ((p2.Y - p1.Y) / (p2.X - p1.X)) * (x - p1.X) + p1.Y;
                if (y1 < y)
                    return -1;
                if (y1 > y)
                    return 1;
                return 0;
            });
            double maxY = 0;
            foreach(WallIntersect2D intersect in intersects)
            {
                if (intersect.line.Coordinates[0].Y > maxY)
                    maxY = intersect.line.Coordinates[0].Y;
                if (intersect.line.Coordinates[^1].Y > maxY)
                    maxY = intersect.line.Coordinates[^1].Y;
            }
            maxY++;
            //Now build polygon for each, take union if front edge, difference if back. Doesn't handle line intersections, if those 
            //turn out to exist then the segments will have to be cut into more lines in the above code (but this makes it n^2 time).
            Geometry wall = GeometryHelper.CreateEmpty();
            foreach(WallIntersect2D intersect in intersects)
            {
                Coordinate b0 = new (intersect.line.Coordinates[0].X, maxY);
                Coordinate b1 = new(intersect.line.Coordinates[^1].X, maxY);
                Polygon shadow = GeometryHelper.CreatePolygon([
                    intersect.line.Coordinates[0], 
                    intersect.line.Coordinates[1], 
                    b1, b0, intersect.line.Coordinates[0]
                ]);
                if (intersect.forward)
                    wall = wall.Union(shadow);
                else
                    wall = wall.Difference(shadow);
            }
            return Triangulate(plane, wall);
        }
        private static void AddWallIntersects(
            List<WallIntersect>[] wallIntersects, 
            Triangle triangle, Prism hole, Polygon trianglePoly, Polygon holeSlice)
        {
            //No wall intersects if triangle plane parallel to hole, or outside of bounding box
            if (hole.IsParallel(triangle.plane) || holeSlice == null || !holeSlice.Envelope.Intersects(trianglePoly.Envelope))
                return;
            //Otherwise, add intersects for each edge
            for(int i = 0; i < holeSlice.Coordinates.Length-1; i++)
            {
                List<WallIntersect> intersectList = wallIntersects[i];
                LineString edge = GeometryHelper.CreateLineSegment(holeSlice.Coordinates[i], holeSlice.Coordinates[i + 1]);
                Geometry intersects = edge.Intersection(trianglePoly);
                if (intersects.Coordinates.Length < 2)
                    continue;
                WallIntersect intersect = new(hole, triangle.plane, intersects.Coordinates[0], intersects.Coordinates[^1]);
                intersectList.Add(intersect);
            }
        }
        //Put hole in triangle, then triangularize result and return triangles
        private static Triangle[] Punch(Triangle triangle, Prism hole, Polygon trianglePoly, Polygon holeSlice)
        {
            if (holeSlice == null || !holeSlice.Envelope.Intersects(trianglePoly.Envelope))
                return [triangle];

            Geometry cutProj = trianglePoly.Difference(holeSlice.Difference(holeSlice.Boundary));
            return Triangulate(triangle.plane, cutProj);
        }
        //Turn geometry on plane into triangles, then put back in 3-space from plane
        private static Triangle[] Triangulate(Plane plane, Geometry geom)
        {
            var tris = new ConstrainedDelaunayTriangulator(geom).GetTriangles();
            Triangle[] res = new Triangle[tris.Count];

            Triangle TriToTriangle(Tri t) =>
            new(
                    plane.ToWorldSpace(GeometryHelper.CoordToVec(t.GetCoordinate(0))),
                    plane.ToWorldSpace(GeometryHelper.CoordToVec(t.GetCoordinate(1))),
                    plane.ToWorldSpace(GeometryHelper.CoordToVec(t.GetCoordinate(2)))
                );
            int i = 0;
            foreach (Tri t in tris)
            {
                res[i] = TriToTriangle(t);
                i++;
            }
            return res;
        }
    }
}

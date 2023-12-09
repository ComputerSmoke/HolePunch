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
using Valve.VR;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate.Polygon;
using NetTopologySuite.Triangulate.Tri;
using NetTopologySuite.Noding;
using HolePuncher.Volumes;
using System.Runtime.Serialization;
using HolePuncher.Volumes.Faces;
using Plane = HolePuncher.Volumes.Faces.Plane;
using Triangle = HolePuncher.Volumes.Faces.Triangle;

namespace HolePuncher
{
    //Line segment intersections with faces, used to construct walls of hole
    struct WallIntersect
    {
        public Vector3 p1;
        public Vector3 p2;
        public bool forward;
        public WallIntersect(Prism hole, Plane trianglePlane, Coordinate c1, Coordinate c2)
        {
            p1 = trianglePlane.ToWorldSpace(c1);
            p2 = trianglePlane.ToWorldSpace(c2);
            forward = Vector3.Dot(trianglePlane.normal, hole.facePlane.normal) > 0;
        }
        public WallIntersect(Face face, Plane trianglePlane, Coordinate c1, Coordinate c2)
        {
            p1 = trianglePlane.ToWorldSpace(c1);
            p2 = trianglePlane.ToWorldSpace(c2);
            forward = Vector3.Dot(face.plane.unitY, trianglePlane.normal) < 0;
        }
    }
    struct WallIntersect2D
    {
        public LineString line;
        public bool forward;
        public WallIntersect2D(Coordinate c0, Coordinate c1, bool forward)
        {
            line = GeometryHelper.CreateLineSegment(c0, c1);
            this.forward = forward;
        }
        public WallIntersect2D(Plane plane, WallIntersect intersect) {
            line = GeometryHelper.CreateLineSegment(plane.ToPlaneSpace(intersect.p1), plane.ToPlaneSpace(intersect.p2));
            forward = intersect.forward;
        }
        
    }
    //Represents a triangle in 3 space
    public static class HolePunch
    {
        //put a hole in the mesh by punching hole in faces, then adding walls of hole inside mesh
        public static VertexPositionNormalTexture[] PunchHole(VertexPositionNormalTexture[] vertices, Volume hole)
        {
            Triangle[] triangles = new Triangle[vertices.Length / 3];
            for(int i = 0; i < triangles.Length; i++)
                triangles[i] = new Triangle(vertices[i*3].Position, vertices[i*3+1].Position, vertices[i*3+2].Position);
            (List<Triangle> involved, List<Triangle> uninvolved) = FindAffected(triangles, hole);
            List<Triangle> punched = hole.Punch(involved);
            punched.AddRange(InternalWalls(involved, hole));
            punched.AddRange(uninvolved);
            return Triangle.TrianglesToVertices(punched);
        }
        //Sort triangles into a list that might be intersected, and one that is not intersected by hole
        private static (List<Triangle>, List<Triangle>) FindAffected(Triangle[] triangles, Volume hole)
        {
            List<Triangle> affected = [];
            List<Triangle> unaffected = [];
            foreach(Triangle triangle in triangles)
            {
                if (!hole.BoundingBox.Intersects(triangle.BoundingBox))
                    unaffected.Add(triangle);
                else
                {
                    Geometry slice = hole.Slice(triangle.plane);
                    bool intersects = slice.Intersects(triangle.geometry);
                    if (intersects)
                        affected.Add(triangle);
                    else
                        unaffected.Add(triangle);
                }
            }
            return (affected, unaffected);
        }
        //For each face, build its internal walls. TODO: draw back face of hole if internal
        private static List<Triangle> InternalWalls(List<Triangle> triangles, Volume hole)
        {
            List<Triangle> res = [];
            foreach (Face face in hole.Faces)
                res.AddRange(InternalWalls(triangles, face));
            return res;
        }
        private static Triangle[] InternalWalls(List<Triangle> triangles, Face face)
        {
            float extendDist = (float)face.geometry.Envelope.Length / 2;
            List<WallIntersect2D> intersects = [];
            foreach(Triangle triangle in triangles)
            {
                Geometry sliceGeometry = face.SliceLocal(triangle.plane);
                if (sliceGeometry.Coordinates.Length < 2)
                    continue;
                Vector2 normal = face.plane.Project(triangle.plane.normal + face.plane.origin);
                bool forward = Vector2.Dot(normal, Vector2.UnitY) < 0;
                intersects.Add(new(sliceGeometry.Coordinates[0], sliceGeometry.Coordinates[^1], forward));
            }
            //add bounds of face (all as negative)
            for(int i = 0; i < face.geometry.Coordinates.Length-1; i++)
            {
                Coordinate c0 = face.geometry.Coordinates[i];
                Coordinate c1 = face.geometry.Coordinates[i + 1];
                //TODO: bound must start forward if inside shape, so rebase to guarantee origin outside and search stuff from outside or whatever
                intersects.Add(new(c0, c1, false));
            }
            return BuildWalls(intersects.ToArray(), face.plane);
        }
        private static List<WallIntersect> FaceIntersects(List<Triangle> triangles, Face face)
        {
            List<WallIntersect> res = [];
            foreach (Triangle triangle in triangles)
            {
                Geometry intersects = triangle.Slice(face.plane).Intersection(face.geometry);
                if (intersects.Coordinates.Length < 2)
                    continue;
                WallIntersect intersect = new(face, triangle.plane, intersects.Coordinates[0], intersects.Coordinates[^1]);
                res.Add(intersect);
            }
            return res;
        }
        public static VertexPositionNormalTexture[] PunchHole(VertexPositionNormalTexture[] vertices, Prism hole)
        {
            List<Triangle> verts = [];
            for(int i = 0; i < vertices.Length - 2; i += 3)
                verts.Add(new(vertices[i].Position, vertices[i + 1].Position, vertices[i + 2].Position));
            return Triangle.TrianglesToVertices(PunchHole(verts, hole));
        }
        //TODO: fix hole starting inside a volume, hitting negative edge first
        public static List<Triangle> PunchHole(List<Triangle> verts, Prism hole)
        {
            Prism smallHole = new(hole.facePlane.origin, hole.facePlane.normal, hole.radius - 1e-4f, hole.numSides);
            List<Triangle> punched = [];
            List<WallIntersect>[] wallIntersects = new List<WallIntersect>[hole.face.Coordinates.Length - 1];
            Plane[] wallPlanes = new Plane[wallIntersects.Length];
            for (int i = 0; i < hole.face.Coordinates.Length - 1; i++)
            {
                wallIntersects[i] = [];
                wallPlanes[i] = EdgePlane(hole.face.Coordinates[i], hole.face.Coordinates[i + 1], hole.facePlane);
            }
            foreach (Triangle triangle in verts)
            {
                Geometry holeSlice = hole.Slice(triangle.plane, triangle.geometry);
                Geometry smallHoleSlice = smallHole.Slice(triangle.plane, triangle.geometry);
                punched.AddRange(Punch(triangle, smallHoleSlice));
                AddWallIntersects(wallIntersects, triangle, hole, holeSlice);
            }
            List<Triangle> walls = BuildAllWalls(wallIntersects, wallPlanes);
            punched.AddRange(walls);
            return punched;
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
            if (wallIntersects.Count < 2)
                return [];
            WallIntersect2D[] intersects = new WallIntersect2D[wallIntersects.Count];
            for (int i = 0; i < intersects.Length; i++)
                intersects[i] = new(plane, wallIntersects[i]);
            return BuildWalls(intersects, plane);
        }
        private static Triangle[] BuildWalls(WallIntersect2D[] intersects, Plane plane)
        {
            Array.Sort(intersects, delegate (WallIntersect2D intersect1, WallIntersect2D intersect2)
            {
                return CompareLines(intersect1.line, intersect2.line);
            });
            double maxY = 0;
            foreach (WallIntersect2D intersect in intersects)
            {
                if (intersect.line.Coordinates[0].Y > maxY)
                    maxY = intersect.line.Coordinates[0].Y;
                if (intersect.line.Coordinates[^1].Y > maxY)
                    maxY = intersect.line.Coordinates[^1].Y;
            }
            //Now build polygon for each, take union if front edge, difference if back. Doesn't handle line intersections, if those 
            //turn out to exist then the segments will have to be cut into more lines in the above code (but this makes it n^2 time).
            Geometry wall = GeometryHelper.CreateEmpty();
            foreach (WallIntersect2D intersect in intersects)
            {
                if (Math.Abs(intersect.line.Coordinates[0].X - intersect.line.Coordinates[^1].X) <= Plane.O)
                    continue;
                Coordinate b0 = new(intersect.line.Coordinates[0].X, maxY);
                Coordinate b1 = new(intersect.line.Coordinates[^1].X, maxY);
                Polygon shadow = GeometryHelper.CreatePolygon([
                    intersect.line.Coordinates[0],
                    intersect.line.Coordinates[1],
                    b1,
                    b0,
                    intersect.line.Coordinates[0]
                ]);

                try
                {
                    if (intersect.forward)
                        wall = wall.Union(shadow);
                    else
                        wall = wall.Difference(shadow);
                }
                catch (Exception) { }
            }
            return Triangle.Triangulate(plane, wall, false);
        }
        private static int CompareLines(LineString line1, LineString line2)
        {
            //Sort by which is further forward at an midpoint of one of the lines. For non intersecting, this puts line in front first.
            Coordinate p1 = line1.Coordinates[0];
            Coordinate p2 = line1.Coordinates[^1];
            Coordinate q1 = line2.Coordinates[0];
            Coordinate q2 = line2.Coordinates[^1];
            static int CompareMidpoint(Coordinate p1, Coordinate p2, Coordinate q1, Coordinate q2)
            {
                double x = (q1.X + q2.X) / 2;
                double y = (q1.Y + q2.Y) / 2;
                double y1 = ((p2.Y - p1.Y) / (p2.X - p1.X)) * (x - p1.X) + p1.Y;
                if (y1 < y)
                    return -1;
                else if (y < y1)
                    return 1;
                return 0;
            }
            int m1 = CompareMidpoint(p1, p2, q1, q2);
            int m2 = CompareMidpoint(q1, q2, p1, p2);
            if (m1 == -1 && m2 == 1)
                return -1;
            else if (m1 == 1 && m2 == -1)
                return 1;
            return 0;
        }
        private static void AddWallIntersects(
            List<WallIntersect>[] wallIntersects,
            Triangle triangle, Prism hole, Geometry holeSlice)
        {
            //No wall intersects if triangle plane parallel to hole, or outside of bounding box
            if (hole.IsParallel(triangle.plane) || holeSlice == null || !holeSlice.Envelope.Intersects(triangle.geometry.Envelope))
                return;
            //Otherwise, add intersects for each edge
            for (int i = 0; i < holeSlice.Coordinates.Length - 1; i++)
            {
                List<WallIntersect> intersectList = wallIntersects[i];
                LineString edge = GeometryHelper.CreateLineSegment(holeSlice.Coordinates[i], holeSlice.Coordinates[i + 1]);
                Geometry intersects = edge.Intersection(triangle.geometry);
                if (intersects.Coordinates.Length < 2)
                    continue;
                WallIntersect intersect = new(hole, triangle.plane, intersects.Coordinates[0], intersects.Coordinates[^1]);
                intersectList.Add(intersect);
            }
        }
        //Put hole in triangle, then triangularize result and return triangles
        public static Triangle[] Punch(Triangle triangle, Geometry holeSlice)
        {
            if (holeSlice == null || !holeSlice.Envelope.Intersects(triangle.geometry.Envelope))
                return [triangle];
            try
            {
                Geometry cutProj = triangle.geometry.Difference(holeSlice);
                return Triangle.Triangulate(triangle, cutProj);
            } catch (Exception) {
                return [];
            }
        }
        //Take only intersection of triangle with hole, then triangularize and return triangles
        public static Triangle[] Crop(Triangle triangle, Geometry holeSlice)
        {
            if (holeSlice == null || !holeSlice.Envelope.Intersects(triangle.geometry.Envelope))
                return [];

            try
            {
                Geometry cutProj = triangle.geometry.Intersection(holeSlice);
                return Triangle.Triangulate(triangle, cutProj);
            }
            catch (Exception) {
                return [];
            }
        }
    }
}

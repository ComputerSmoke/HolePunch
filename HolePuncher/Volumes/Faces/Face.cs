using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Point = NetTopologySuite.Geometries.Point;
using System.ComponentModel;

namespace HolePuncher.Volumes.Faces
{
    public class Face
    {
        public Geometry geometry;
        public Plane plane;
        public Vector3[] vertices;
        public BoundingBox3D BoundingBox { get; }
        public Face(Geometry geometry, Plane plane)
        {
            this.geometry = geometry;
            this.plane = plane;
            vertices = new Vector3[geometry.Coordinates.Length - 1];
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = plane.ToWorldSpace(GeometryHelper.CoordToVec(geometry.Coordinates[i]));
            BoundingBox = new BoundingBox3D(vertices);
        }
        //Specify face with at least 3 points, clockwise when viewing from normal direction
        public Face(Vector3[] points)
        {
            vertices = points;
            Vector3 normal = Vector3.Cross(points[2] - points[0], points[1] - points[0]);
            plane = new(points[0], normal);
            Coordinate[] coords = new Coordinate[points.Length + 1];
            for (int i = 0; i < points.Length; i++)
                coords[i] = GeometryHelper.VecToCoord(plane.ToPlaneSpace(points[i]));
            coords[^1] = coords[0];
            geometry = GeometryHelper.CreatePolygon(coords);
            BoundingBox = new BoundingBox3D(vertices);
        }
        //Take slice of this face
        public Geometry Slice(Plane slicer)
        {
            if (plane.Parallel(slicer))
                return GeometryHelper.CreateEmpty();
            Line intersection = plane.Intersection(slicer);
            LineString segment = CropLine(intersection);
            Geometry slice = geometry.Intersection(segment);
            if (slice.IsEmpty || !slice.IsValid)
                return GeometryHelper.CreateEmpty();
            Coordinate TranslateCoord(Coordinate coord)
            {
                return GeometryHelper.VecToCoord(slicer.ToPlaneSpace(plane.ToWorldSpace(GeometryHelper.CoordToVec(coord))));
            }
            Point TranslatePoint(Coordinate coord)
            {
                return GeometryHelper.GeometryFactory.CreatePoint(TranslateCoord(coord));
            }
            LineString TranslateLineString(Coordinate[] coords)
            {
                Coordinate[] translated = new Coordinate[coords.Length];
                for (int i = 0; i < coords.Length; i++)
                    translated[i] = TranslateCoord(coords[i]);
                return GeometryHelper.GeometryFactory.CreateLineString(translated);
            }
            if (slice is Point point)
                return TranslatePoint(point.Coordinate);
            if (slice is LineString line)
                return TranslateLineString(line.Coordinates);
            if (slice is MultiLineString multiLine)
            {
                return TranslateLineString(multiLine.Coordinates);
            }
            throw new Exception("Unrecognized geometry in face slice");
        }
        //Cut a line down to a segment that could intersect geometry
        private LineString CropLine(Line line)
        {
            float minDist = 0;
            float maxDist = 0;
            foreach (Coordinate coord in geometry.Coordinates)
            {
                float dist = Vector2.Dot(line.dir, GeometryHelper.CoordToVec(coord) - line.point);
                if (dist < minDist)
                    minDist = dist;
                if (dist > maxDist)
                    maxDist = dist;
            }
            Coordinate c1 = GeometryHelper.VecToCoord(line.point + line.dir * (minDist - 1e-6f));
            Coordinate c2 = GeometryHelper.VecToCoord(line.point + line.dir * (maxDist + 1e-6f));
            return GeometryHelper.CreateLineSegment(c1, c2);
        }
    }
}

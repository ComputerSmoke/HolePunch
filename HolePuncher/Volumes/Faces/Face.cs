using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Point = NetTopologySuite.Geometries.Point;
using System.ComponentModel;
using Stride.Core.Mathematics;
using Silk.NET.OpenXR;

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
        //Take slice of this face. Returns tuple with slice on local face plane and slice on slicer plane.
        public Geometry Slice(Plane slicer)
        {
            return TranslateGeometry(SliceLocal(slicer), slicer);
        }
        //Get slice of face in local coordinates, not in coordinates of other plane.
        public Geometry SliceLocal(Plane slicer)
        {
            if (plane.Parallel(slicer))
                return GeometryHelper.CreateEmpty();
            Line intersection = plane.Intersection(slicer);
            LineString segment = CropLine(intersection);
            Geometry slice = geometry.Intersection(segment);
            if (slice.IsEmpty || !slice.IsValid)
                return GeometryHelper.CreateEmpty();
            return slice;
        }
        //Convert geometry from local space of this plane to local of another plane, must be along intersection of planes.
        private Geometry TranslateGeometry(Geometry geometry, Plane dest)
        {
            if (geometry.IsEmpty)
                return geometry;
            Coordinate TranslateCoord(Coordinate coord)
            {
                return GeometryHelper.VecToCoord(dest.ToPlaneSpace(plane.ToWorldSpace(GeometryHelper.CoordToVec(coord))));
            }
            Point TranslatePoint(Coordinate coord)
            {
                return GeometryHelper.GeometryFactory.CreatePoint(TranslateCoord(coord));
            }
            Coordinate[] TranslateCoords(Coordinate[] coords)
            {
                Coordinate[] translated = new Coordinate[coords.Length];
                for (int i = 0; i < coords.Length; i++)
                    translated[i] = TranslateCoord(coords[i]);
                return translated;
            }
            LineString TranslateLineString(Coordinate[] coords)
            {
                return GeometryHelper.GeometryFactory.CreateLineString(TranslateCoords(coords));
            }
            Polygon TranslatePoly(Coordinate[] coords)
            {
                return GeometryHelper.GeometryFactory.CreatePolygon(TranslateCoords(coords));
            }
            if (geometry is Point point)
                return TranslatePoint(point.Coordinate);
            if (geometry is LineString line)
                return TranslateLineString(line.Coordinates);
            if (geometry is MultiLineString multiLine)
                return TranslateLineString(multiLine.Coordinates);
            if(geometry is Polygon poly)
                return TranslatePoly(poly.Coordinates);
            throw new Exception("Unrecognized geometry geometry plane conversion.");
        }
        //Cut a line down to a segment that could intersect geometry
        private LineString CropLine(Line line)
        {
            float minDist = 0;
            float maxDist = 0;
            Vector2 dir = line.p2 - line.p1;
            foreach (Coordinate coord in geometry.Coordinates)
            {
                float dist = Vector2.Dot(dir, GeometryHelper.CoordToVec(coord) - line.p1);
                if (dist < minDist)
                    minDist = dist;
                if (dist > maxDist)
                    maxDist = dist;
            }
            Coordinate c1 = GeometryHelper.VecToCoord(line.p1 + dir * (minDist * 2));
            Coordinate c2 = GeometryHelper.VecToCoord(line.p1 + dir * (maxDist * 2));
            return GeometryHelper.CreateLineSegment(c1, c2);
        }
    }
}

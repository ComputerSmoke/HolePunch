﻿using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Point = NetTopologySuite.Geometries.Point;
using System.ComponentModel;

namespace HolePuncher.Volumes
{
    internal class Face(Geometry geometry, Plane plane)
    {
        public Geometry geometry = geometry;
        public Plane plane = plane;
        //Take slice of this face
        public Geometry Slice(Plane slicer)
        {
            Line intersection = plane.Intersection(slicer);
            LineString segment = CropLine(intersection);
            Geometry slice = geometry.Intersection(segment);
            if (slice.IsEmpty || !slice.IsValid)
                return slice;
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
                for(int i = 0; i < coords.Length; i++)
                    translated[i] = coords[i];
                return GeometryHelper.GeometryFactory.CreateLineString(translated);
            }
            if (slice is Point point)
                return TranslatePoint(point.Coordinate);
            if(slice is MultiPoint multiPoint)
            {
                Point[] points = new Point[multiPoint.Count];
                for (int i = 0; i < points.Length; i++)
                    points[i] = TranslatePoint(multiPoint[i].Coordinate);
                return GeometryHelper.GeometryFactory.CreateMultiPoint(points);
            }
            if(slice is LineString line)
            {
                return TranslateLineString(line.Coordinates);
            }
            if(slice is MultiLineString multiLine)
            {
                LineString[] lineStrings = new LineString[multiLine.Count];
                for(int i = 0; i < lineStrings.Length; i++)
                    lineStrings[i] = TranslateLineString(multiLine[i].Coordinates);
                return GeometryHelper.GeometryFactory.CreateMultiLineString(lineStrings);
            }
            throw new Exception("Unrecognized geometry in face slice");
        }
        //Cut a line down to a segment that could intersect geometry
        private LineString CropLine(Line line)
        {
            float minDist = 0;
            float maxDist = 0;
            foreach(Coordinate coord in geometry.Coordinates)
            {
                float dist = Vector2.Dot(line.dir, GeometryHelper.CoordToVec(coord) - line.point);
                if(dist < minDist)
                    minDist = dist;
                if(dist > maxDist) 
                    maxDist = dist;
            }
            Coordinate c1 = GeometryHelper.VecToCoord(line.point + line.dir * (minDist - 1e6f));
            Coordinate c2 = GeometryHelper.VecToCoord(line.point + line.dir * (maxDist + 1e6f));
            return GeometryHelper.CreateLineSegment(c1, c2);
        }
    }
}

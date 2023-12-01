using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Noding;
using System.Security.Cryptography;

namespace HolePuncher
{
    public static class GeometryHelper
    {
        public static GeometryFactory GeometryFactory { get; }
        static GeometryHelper()
        {
            NtsGeometryServices.Instance = new NtsGeometryServices(
                // default CoordinateSequenceFactory
                NetTopologySuite.Geometries.Implementation.CoordinateArraySequenceFactory.Instance,
                // default precision model
                new NetTopologySuite.Geometries.PrecisionModel(1000d),
                // default SRID
                4326,
                // Geometry overlay operation function set to use (Legacy or NG)
                GeometryOverlay.NG,
                // Coordinate equality comparer to use (CoordinateEqualityComparer or PerOrdinateEqualityComparer)
                new CoordinateEqualityComparer()
            );
            GeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
        }
        public static Polygon CreateTriangle(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            var c1 = VecToCoord(p1);
            var coords = new Coordinate[4]
            {
                c1, VecToCoord(p2), VecToCoord(p3), c1
            };
            return GeometryFactory.CreatePolygon(coords);
        }
        public static Polygon CreatePolygon(Coordinate[] coords)
        {
            return GeometryFactory.CreatePolygon(coords);
        }
        public static LineString CreateLineSegment(Vector2 p1, Vector2 p2)
        {
            var c1 = VecToCoord(p1);
            var c2 = VecToCoord(p2);
            return CreateLineSegment(c1, c2);
        }
        public static LineString CreateLineSegment(Coordinate c1, Coordinate c2)
        {
            Coordinate[] coords = [c1, c2];
            return GeometryFactory.CreateLineString(coords);
        }
        public static Polygon InscribeCircle(float radius, int numSides)
        {
            var coords = new Coordinate[numSides+1];
            for(int i = 0; i < numSides; i++)
            {
                coords[i] = new Coordinate(
                    radius * Math.Cos(i * 2 * Math.PI / numSides), 
                    radius * Math.Sin(i * 2 * Math.PI / numSides)
                );
            }
            coords[numSides] = coords[0];
            return GeometryFactory.CreatePolygon(coords);
        }
        public static Coordinate VecToCoord(Vector2 v)
        {
            return new Coordinate(v.X, v.Y);
        }
        public static Vector2 CoordToVec(Coordinate coord)
        {
            return new Vector2((float)coord.X, (float)coord.Y);
        }
        public static Geometry CreateEmpty()
        {
            return GeometryFactory.CreateEmpty(Dimension.Unknown);
        }
        //Difference of two geometries which may be collections
        public static Geometry Difference(Geometry g1, Geometry g2)
        {
            Geometry res = CreateEmpty();
            for(int i = 0; i < g1.NumGeometries; i++)
                res.Union(g1.GetGeometryN(i));
            for(int i = 0; i < g2.NumGeometries; i++) 
                res.Difference(g2.GetGeometryN(i));
            return res;
        }
    }
}

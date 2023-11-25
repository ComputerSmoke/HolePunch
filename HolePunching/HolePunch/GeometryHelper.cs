using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Noding;

namespace HolePunching.HolePunch
{
    public static class GeometryHelper
    {
        static GeometryFactory geometryFactory;
        static bool initialized;
        public static void Init()
        {
            if (initialized)
                return;
            initialized = true;
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
            geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
        }
        public static Polygon CreateTriangle(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            var c1 = VecToCoord(p1);
            var coords = new Coordinate[4]
            {
                c1, VecToCoord(p2), VecToCoord(p3), c1
            };
            return geometryFactory.CreatePolygon(coords);
        }
        public static Polygon CreatePolygon(Coordinate[] coords)
        {
            return geometryFactory.CreatePolygon(coords);
        }
        public static Geometry Difference(Polygon p1, Polygon p2)
        {
            return p1.Difference(p2);
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
            return geometryFactory.CreateLineString(coords);
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
            return geometryFactory.CreatePolygon(coords);
        }
        public static Coordinate VecToCoord(Vector2 v)
        {
            return new Coordinate(v.X, v.Y);
        }
        public static Vector2 CoordToVec(Coordinate coord)
        {
            return new Vector2((float)coord.X, (float)coord.Y);
        }
    }
}

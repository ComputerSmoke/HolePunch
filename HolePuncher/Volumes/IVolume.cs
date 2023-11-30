using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HolePuncher.Volumes.Faces;
using NetTopologySuite.Geometries;

namespace HolePuncher.Volumes
{
    internal interface IVolume
    {
        public BoundingBox3D BoundingBox { get; }
        public Geometry Slice(Plane plane, Geometry interestArea);
        public Geometry Slice(Plane plane);
    }
}

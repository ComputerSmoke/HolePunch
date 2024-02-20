using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HolePuncher.Volumes.Faces
{
    internal class NoIntersectException : Exception
    {
        public NoIntersectException() { }
        public NoIntersectException(string message) : base(message) { }
    }
}

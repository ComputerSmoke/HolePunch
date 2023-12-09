using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HolePuncher
{
    public class UnpunchableMeshException : Exception
    {
        public UnpunchableMeshException(string message) : base(message) { }
    }
}

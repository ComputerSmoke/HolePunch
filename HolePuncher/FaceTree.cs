using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HolePuncher
{
    internal class FaceTree(bool leaf)
    {
        private FaceTree[,,] subtrees = new FaceTree[2, 2, 2];
        private bool leaf = leaf;

    }
}

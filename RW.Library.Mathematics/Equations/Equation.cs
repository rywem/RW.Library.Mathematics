using RW.Library.Mathematics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RW.Library.Mathematics.Equations
{

    //
    // ======================
    //  EQUATION
    // ======================
    //

    /// <summary>
    /// Represents Left = Right.
    /// </summary>
    public class Equation
    {
        public NodeExpression Left { get; }
        public NodeExpression Right { get; }

        public Equation(NodeExpression left, NodeExpression right)
        {
            Left = left;
            Right = right;
        }

        public override string ToString() => $"{Left} = {Right}";
    }
}

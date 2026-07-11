using RW.Library.Mathematics.Equations;
using RW.Library.Mathematics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RW.Library.Mathematics.Solvers
{
    /// <summary>
    /// Utility for equation preprocessing.
    /// </summary>
    public static class EquationTools
    {
        /// <summary>
        /// Returns a single expression equivalent to Left - Right,
        /// simplified. Equation is satisfied when result == 0.
        /// </summary>
        public static NodeExpression ToZeroOnRight(Equation eq)
        {
            var left = eq.Left.Simplify();
            var right = eq.Right.Simplify();
            var diff = new SubtractExpression(left, right);
            return diff.Simplify();
        }
    }
}

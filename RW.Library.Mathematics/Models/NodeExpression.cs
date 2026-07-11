using RW.Library.Mathematics.Models;
using System;

/// <summary>
/// Base type for all symbolic algebra expressions.
/// Every node in the expression tree derives from this.
/// </summary>
namespace RW.Library.Mathematics.Models
{

    /// <summary>
    /// Base type for all symbolic algebra expressions.
    /// </summary>
    public abstract class NodeExpression
    {
        /// <summary>
        /// Returns a simplified form of the expression.
        /// </summary>
        public abstract NodeExpression Simplify();

        /// <summary>
        /// Pretty printed algebraic representation.
        /// </summary>
        public abstract override string ToString();
    }
}
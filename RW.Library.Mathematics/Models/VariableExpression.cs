/// <summary>
/// Base type for all symbolic algebra expressions.
/// Every node in the expression tree derives from this.
/// </summary>
namespace RW.Library.Mathematics.Models
{
    //
    // ======================
    //  VARIABLE SYMBOL
    // ======================
    //

    /// <summary>
    /// Represents a symbolic variable such as x, y.
    /// </summary>
    internal class VariableExpression : NodeExpression
    {
        public string Name { get; }

        public VariableExpression(string name)
        {
            Name = name;
        }

        public override NodeExpression Simplify() => this;

        public override string ToString() => Name;
    }
}
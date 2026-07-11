/// <summary>
/// Base type for all symbolic algebra expressions.
/// Every node in the expression tree derives from this.
/// </summary>
namespace RW.Library.Mathematics.Models
{
    //
    // ======================
    //  NUMBER LITERAL
    // ======================
    //

    /// <summary>
    /// Represents a numeric literal (integer, real, etc.).
    /// </summary>
    public class NumberExpression : NodeExpression
    {
        public double Value { get; }

        public NumberExpression(double value)
        {
            Value = value;
        }

        public override NodeExpression Simplify() => this;

        public override string ToString() =>
            (Value % 1 == 0) ? ((int)Value).ToString() : Value.ToString();
    }
}
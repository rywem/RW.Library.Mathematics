/// <summary>
/// Base type for all symbolic algebra expressions.
/// Every node in the expression tree derives from this.
/// </summary>
namespace RW.Library.Mathematics.Models
{
    //
    // ===============================
    //  UNARY: NEGATION
    // ===============================
    //

    /// <summary>
    /// Represents unary negation: -expr.
    /// </summary>
    public class NegateExpression : NodeExpression
    {
        public NodeExpression Inner { get; }

        public NegateExpression(NodeExpression inner)
        {
            Inner = inner;
        }

        public override NodeExpression Simplify()
        {
            var inner = Inner.Simplify();

            // --a  →  a
            if (inner is NegateExpression neg)
                return neg.Inner;

            // -(number) → number * -1
            if (inner is NumberExpression n)
                return new NumberExpression(-n.Value);

            return new NegateExpression(inner);
        }

        public override string ToString() => $"-({Inner})";
    }
}
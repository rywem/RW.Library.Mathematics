/// <summary>
/// Base type for all symbolic algebra expressions.
/// Every node in the expression tree derives from this.
/// </summary>
namespace RW.Library.Mathematics.Models
{
    //
    // SUBTRACTION
    //
    public class SubtractExpression : NodeExpression
    {
        public NodeExpression Left { get; }
        public NodeExpression Right { get; }

        public SubtractExpression(NodeExpression left, NodeExpression right)
        {
            Left = left;
            Right = right;
        }

        public override NodeExpression Simplify()
        {
            var L = Left.Simplify();
            var R = Right.Simplify();

            // Constant folding
            if (L is NumberExpression a && R is NumberExpression b)
                return new NumberExpression(a.Value - b.Value);

            // x - 0 = x
            if (R is NumberExpression rb && rb.Value == 0)
                return L;

            return new SubtractExpression(L, R);
        }

        public override string ToString() => $"{Left} - {Right}";
    }
}
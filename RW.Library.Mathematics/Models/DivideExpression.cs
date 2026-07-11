/// <summary>
/// Base type for all symbolic algebra expressions.
/// Every node in the expression tree derives from this.
/// </summary>
namespace RW.Library.Mathematics.Models
{
    //
    // DIVISION
    //
    public class DivideExpression : NodeExpression
    {
        public NodeExpression Left { get; }
        public NodeExpression Right { get; }

        public DivideExpression(NodeExpression left, NodeExpression right)
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
                return new NumberExpression(a.Value / b.Value);

            // x / 1 = x
            if (R is NumberExpression rb && rb.Value == 1)
                return L;

            return new DivideExpression(L, R);
        }

        public override string ToString()
        {
            return $"{Left} / {Right}";
        }
    }
}
/// <summary>
/// Base type for all symbolic algebra expressions.
/// Every node in the expression tree derives from this.
/// </summary>
namespace RW.Library.Mathematics.Models
{
    //
    // ===============================
    //  BINARY OPERATORS
    // ===============================
    //

    //
    // ADDITION
    //
    internal class AddExpression : NodeExpression
    {
        public NodeExpression Left { get; }
        public NodeExpression Right { get; }

        public AddExpression(NodeExpression left, NodeExpression right)
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
                return new NumberExpression(a.Value + b.Value);

            // 0 + x = x
            if (L is NumberExpression la && la.Value == 0)
                return R;

            // x + 0 = x
            if (R is NumberExpression rb && rb.Value == 0)
                return L;

            return new AddExpression(L, R);
        }

        public override string ToString() => $"{Left} + {Right}";
    }
}
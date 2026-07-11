/// <summary>
/// Base type for all symbolic algebra expressions.
/// Every node in the expression tree derives from this.
/// </summary>
namespace RW.Library.Mathematics.Models
{
    //
    // MULTIPLICATION
    //
    public class MultiplyExpression : NodeExpression
    {
        public NodeExpression Left { get; }
        public NodeExpression Right { get; }

        public MultiplyExpression(NodeExpression left, NodeExpression right)
        {
            Left = left;
            Right = right;
        }

        public override NodeExpression Simplify()
        {
            var L = Left.Simplify();
            var R = Right.Simplify();

            // Constant folding: numbers multiply
            if (L is NumberExpression a && R is NumberExpression b)
                return new NumberExpression(a.Value * b.Value);

            // 0 * x = 0
            if (L is NumberExpression la && la.Value == 0) return L;
            if (R is NumberExpression rb && rb.Value == 0) return R;

            // 1 * x = x
            if (L is NumberExpression la2 && la2.Value == 1) return R;
            if (R is NumberExpression rb2 && rb2.Value == 1) return L;

            return new MultiplyExpression(L, R);
        }

        public override string ToString()
        {
            bool ParenL = Left is AddExpression || Left is SubtractExpression;
            bool ParenR = Right is AddExpression || Right is SubtractExpression;

            string l = ParenL ? $"({Left})" : Left.ToString();
            string r = ParenR ? $"({Right})" : Right.ToString();

            return $"{l}{r}";
        }
    }
}
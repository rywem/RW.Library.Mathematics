/// <summary>
/// Base type for all symbolic algebra expressions.
/// Every node in the expression tree derives from this.
/// </summary>
namespace RW.Library.Mathematics.Models
{
    //
    // POWER
    //
    internal class PowerExpression : NodeExpression
    {
        public NodeExpression BaseExpression { get; }
        public NodeExpression Exponent { get; }

        public PowerExpression(NodeExpression baseExpression, NodeExpression exponent)
        {
            BaseExpression = baseExpression;
            Exponent = exponent;
        }

        public override NodeExpression Simplify()
        {
            var B = BaseExpression.Simplify();
            var E = Exponent.Simplify();

            // Constant fold numeric^numeric
            if (B is NumberExpression a && E is NumberExpression b)
                return new NumberExpression(Math.Pow(a.Value, b.Value));

            // x^1 = x
            if (E is NumberExpression one && one.Value == 1)
                return B;

            // x^0 = 1
            if (E is NumberExpression zero && zero.Value == 0)
                return new NumberExpression(1);

            return new PowerExpression(B, E);
        }

        public override string ToString()
        {
            bool parenBase = BaseExpression is AddExpression
                             || BaseExpression is SubtractExpression
                             || BaseExpression is MultiplyExpression;

            string b = parenBase ? $"({BaseExpression})" : BaseExpression.ToString();
            return $"{b}^{Exponent}";
        }
    }
}
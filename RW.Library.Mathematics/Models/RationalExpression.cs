/// <summary>
/// Base type for all symbolic algebra expressions.
/// Every node in the expression tree derives from this.
/// </summary>
namespace RW.Library.Mathematics.Models
{
    /// <summary>
    /// Represents an exact rational number: Numerator / Denominator.
    /// </summary>
    public sealed class RationalExpression : NodeExpression
    {
        public long Numerator { get; }
        public long Denominator { get; }

        public RationalExpression(long numerator, long denominator)
        {
            if (denominator == 0)
                throw new DivideByZeroException("Denominator cannot be zero.");

            // Normalize sign so denominator is positive.
            if (denominator < 0)
            {
                numerator = -numerator;
                denominator = -denominator;
            }

            long g = Gcd(Math.Abs(numerator), denominator);
            Numerator = numerator / g;
            Denominator = denominator / g;
        }

        public override NodeExpression Simplify() => this;

        public override string ToString() =>
            Denominator == 1 ? Numerator.ToString() : $"{Numerator}/{Denominator}";

        private static long Gcd(long a, long b)
        {
            while (b != 0)
            {
                long t = b;
                b = a % b;
                a = t;
            }
            return a == 0 ? 1 : a;
        }
    }
}
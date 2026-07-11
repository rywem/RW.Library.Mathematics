using RW.Library.Mathematics.Equations;
using RW.Library.Mathematics.Models;
using RW.Library.Mathematics.Solvers;

namespace RW.Library.Mathematics.Tests.Models
{
    public class ExpressionTreeTests
    {
        private static NumberExpression N(double v) => new(v);
        private static VariableExpression X => new("x");

        // --- constant folding ------------------------------------------------

        [Fact]
        public void Add_FoldsConstants()
        {
            var result = new AddExpression(N(2), N(3)).Simplify();
            Assert.Equal("5", result.ToString());
        }

        [Fact]
        public void Add_Identity_ZeroIsRemoved()
        {
            Assert.Equal("x", new AddExpression(N(0), X).Simplify().ToString());
            Assert.Equal("x", new AddExpression(X, N(0)).Simplify().ToString());
        }

        [Fact]
        public void Subtract_FoldsAndDropsZero()
        {
            Assert.Equal("4", new SubtractExpression(N(9), N(5)).Simplify().ToString());
            Assert.Equal("x", new SubtractExpression(X, N(0)).Simplify().ToString());
        }

        [Fact]
        public void Multiply_Identities()
        {
            Assert.Equal("6", new MultiplyExpression(N(2), N(3)).Simplify().ToString());
            Assert.Equal("0", new MultiplyExpression(N(0), X).Simplify().ToString());
            Assert.Equal("x", new MultiplyExpression(N(1), X).Simplify().ToString());
            Assert.Equal("x", new MultiplyExpression(X, N(1)).Simplify().ToString());
        }

        [Fact]
        public void Divide_FoldsAndDropsUnitDenominator()
        {
            Assert.Equal("4", new DivideExpression(N(8), N(2)).Simplify().ToString());
            Assert.Equal("x", new DivideExpression(X, N(1)).Simplify().ToString());
        }

        [Fact]
        public void Power_Identities()
        {
            Assert.Equal("8", new PowerExpression(N(2), N(3)).Simplify().ToString());
            Assert.Equal("x", new PowerExpression(X, N(1)).Simplify().ToString());
            Assert.Equal("1", new PowerExpression(X, N(0)).Simplify().ToString());
        }

        [Fact]
        public void Negate_DoubleNegationCancels()
        {
            var doubleNeg = new NegateExpression(new NegateExpression(X)).Simplify();
            Assert.Equal("x", doubleNeg.ToString());
        }

        [Fact]
        public void Negate_Number_FoldsToNegative()
        {
            Assert.Equal("-5", new NegateExpression(N(5)).Simplify().ToString());
        }

        [Fact]
        public void Nested_Expression_SimplifiesRecursively()
        {
            // (2 + 3) * (x + 0) -> 5x, printed as "5x"
            var expr = new MultiplyExpression(
                new AddExpression(N(2), N(3)),
                new AddExpression(X, N(0)));

            Assert.Equal("5x", expr.Simplify().ToString());
        }

        // --- rational --------------------------------------------------------

        [Theory]
        [InlineData(2, 4, "1/2")]
        [InlineData(-1, -2, "1/2")]
        [InlineData(3, -9, "-1/3")]
        [InlineData(6, 3, "2")]
        public void Rational_ReducesAndNormalizesSign(long num, long den, string expected)
        {
            var r = new RationalExpression(num, den);
            Assert.Equal(expected, r.ToString());
        }

        [Fact]
        public void Rational_ZeroDenominator_Throws()
        {
            Assert.Throws<System.DivideByZeroException>(() => new RationalExpression(1, 0));
        }

        // --- equations -------------------------------------------------------

        [Fact]
        public void Equation_ToString_ShowsBothSides()
        {
            var eq = new Equation(new AddExpression(X, N(1)), N(3));
            Assert.Equal("x + 1 = 3", eq.ToString());
        }

        [Fact]
        public void EquationTools_ToZeroOnRight_FoldsConstantEquation()
        {
            // 7 = 3  ->  7 - 3  ->  4
            var eq = new Equation(N(7), N(3));
            var diff = EquationTools.ToZeroOnRight(eq);
            Assert.Equal("4", diff.ToString());
        }
    }
}

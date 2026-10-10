using System.Numerics;
using RW.Library.Mathematics.Polynomials;
using RW.Library.Mathematics.RationalFunctions;

namespace RW.Library.Mathematics.Tests.RationalFunctions
{
    /// <summary>The field Q(x1..xn): parsing, canonical form, exact zero test, gcd, rejection.</summary>
    public class RationalFunctionTests
    {
        private static RationalFunction P(string s) => RationalFunction.Parse(s);

        // ---------------------------------------------------------------- canonical form

        [Theory]
        [InlineData("(x+1)^2 - x^2 - 2x - 1", "0")]
        [InlineData("(x^2-1)/(x-1)", "x+1")]
        [InlineData("1/(x+1) + 1/(x-1)", "2x/(x^2-1)")]
        [InlineData("x/x", "1")]
        [InlineData("(a*b)/(b*a)", "1")]
        [InlineData("2/4", "0.5")]
        [InlineData("x^-2", "1/x^2")]
        [InlineData("x^(-2)", "1/(x*x)")]
        [InlineData("(x^3-y^3)/(x-y)", "x^2+x*y+y^2")]
        [InlineData("(x^2-y^2)/(x^2+2x*y+y^2)", "(x-y)/(x+y)")]
        [InlineData("-x^2", "0-x*x")]
        [InlineData("(-x)^2", "x^2")]
        [InlineData("2^3^2", "512")]
        [InlineData("(6x^2+6x)/(4x)", "(3x+3)/2")]
        [InlineData("1/(1/x)", "x")]
        [InlineData("(a/b)/(c/d)", "a*d/(b*c)")]
        [InlineData("(x+1)(x-1)", "x^2-1")]
        [InlineData("2(x+1)", "2x+2")]
        [InlineData("(x+1)y", "x*y+y")]
        [InlineData("1/2x", "x/2")]
        [InlineData("x^2y", "x*x*y")]
        [InlineData("x - -x", "2x")]
        [InlineData("+x", "x")]
        [InlineData("1.5x", "3x/2")]
        [InlineData(".5", "1/2")]
        [InlineData("x^0", "1")]
        [InlineData("(x+y)^3", "x^3+3x^2*y+3x*y^2+y^3")]
        [InlineData("(a+b)/(c+d) - (a+b)/(c+d)", "0")]
        [InlineData("a/b + c/d", "(a*d+b*c)/(b*d)")]
        [InlineData("1/(x-1) - 1/(1-x)", "2/(x-1)")]
        [InlineData("(x^4-1)/(x^2+1)", "x^2-1")]
        [InlineData("x − 1", "x-1")]
        [InlineData("(a^2-b^2)*(c)/((a-b)*c^2)", "(a+b)/c")]
        [InlineData("X + x", "x + X")]
        [InlineData("1/(a+1/(b+1/c))", "(b*c+1)/(a*b*c+a+c)")]
        public void EqualExpressions_HaveTheSameCanonicalForm(string left, string right)
        {
            Assert.Equal(P(right), P(left));
            Assert.Equal(P(right).ToString(), P(left).ToString());
            Assert.Equal(P(right).GetHashCode(), P(left).GetHashCode());
            Assert.True((P(left) - P(right)).IsZero);
        }

        [Theory]
        [InlineData("x", "y")]
        [InlineData("x", "X")]
        [InlineData("x+1", "x")]
        [InlineData("1/x", "x")]
        [InlineData("(x+1)^2", "x^2+1")]
        [InlineData("x/y", "y/x")]
        [InlineData("1/3", "0.333333")]
        public void DifferentExpressions_AreNotEqual(string left, string right)
        {
            Assert.NotEqual(P(right), P(left));
            Assert.False((P(left) - P(right)).IsZero);
        }

        [Theory]
        [InlineData("(x+1)^2 - x^2 - 2x - 1")]
        [InlineData("(a+b)(a-b) - a^2 + b^2")]
        [InlineData("1/(x+1) - 1/(x+1)")]
        [InlineData("x/(x^2-1) - 1/(2(x-1)) - 1/(2(x+1))")]
        [InlineData("(a+b+c)^3 - (a+b+c)*(a+b+c)^2")]
        [InlineData("(x^2+y^2)^2 - (x^2-y^2)^2 - (2x*y)^2")]
        [InlineData("0*x")]
        [InlineData("0/(x+1)")]
        [InlineData("a*d - b*c - (d*a - c*b)")]
        public void HiddenZeros_AreZero(string expression)
        {
            Assert.True(P(expression).IsZero);
            Assert.Equal("0", P(expression).ToString());
            Assert.True(P(expression).Denominator.IsOne);
        }

        [Theory]
        [InlineData("x - 1")]
        [InlineData("(x+1)^2 - x^2 - 2x")]
        [InlineData("1/(x+1) - 1/(x+2)")]
        [InlineData("x^100 - x^99")]
        public void NonZeroPolynomials_AreNotZero_EvenIfTheyHaveRoots(string expression)
            => Assert.False(P(expression).IsZero);

        [Theory]
        [InlineData("(x^2-1)/(x-1)", "x + 1")]
        [InlineData("2/4", "1/2")]
        [InlineData("1/(1-x)", "(-1)/(x - 1)")]
        [InlineData("x^2+2x+1", "x^2 + 2*x + 1")]
        [InlineData("y*x^2*3", "3*x^2*y")]
        [InlineData("x/2", "x/2")]
        [InlineData("1/x^2", "1/x^2")]
        [InlineData("1/(2x)", "1/(2*x)")]
        [InlineData("-x", "-x")]
        [InlineData("b - a", "-a + b")]
        [InlineData("(a+b)/(a-b)", "(a + b)/(a - b)")]
        [InlineData("7", "7")]
        [InlineData("-3/6", "(-1)/2")]
        public void Printing_IsCanonical(string expression, string expected)
            => Assert.Equal(expected, P(expression).ToString());

        [Theory]
        [InlineData("x")]
        [InlineData("-x")]
        [InlineData("x^2 - 2x*y + 3")]
        [InlineData("1/(x+1)")]
        [InlineData("(a*d-b*c)/(a^2+1)")]
        [InlineData("-3/7")]
        [InlineData("(x-y)^5/(x+y)^3")]
        [InlineData("1/(2x)")]
        [InlineData("-1/(a*b*c)")]
        [InlineData("(2a - 3b^2)/(5c)")]
        public void Printing_RoundTripsThroughTheParser(string expression)
            => Assert.Equal(P(expression), P(P(expression).ToString()));

        [Fact]
        public void CanonicalForm_HasNoCommonFactor_AndAPositiveLeadingDenominator()
        {
            var f = P("(4x^2 - 4)/(-6x - 6)");   // = -2(x - 1)/3
            Assert.Equal("-2*x + 2", f.Numerator.ToString());
            Assert.Equal("3", f.Denominator.ToString());
            Assert.True(MultiPolynomial.Gcd(f.Numerator, f.Denominator).IsOne);
        }

        // ---------------------------------------------------------------- field laws

        private static readonly string[] Samples =
        {
            "0", "1", "-2/3", "x", "x+1", "1/(x+1)", "(a+b)/(c-1)", "x^2-y^2", "(x-y)/(x+y)", "a*b/(x^2+1)", "3x/(2y)"
        };

        [Fact]
        public void FieldLaws_HoldExactly()
        {
            foreach (var sa in Samples)
            foreach (var sb in Samples)
            {
                var a = P(sa); var b = P(sb);
                Assert.Equal(a + b, b + a);
                Assert.Equal(a * b, b * a);
                Assert.True((a - a).IsZero);
                Assert.Equal(a, a + b - b);
                if (!b.IsZero) Assert.Equal(a, a * b / b);
                if (!a.IsZero) Assert.Equal(RationalFunction.One, a * a.Reciprocal());

                foreach (var sc in Samples)
                {
                    var c = P(sc);
                    Assert.Equal((a + b) + c, a + (b + c));
                    Assert.Equal((a * b) * c, a * (b * c));
                    Assert.Equal(a * (b + c), a * b + a * c);
                }
            }
        }

        [Fact]
        public void Arithmetic_AgreesWithExactEvaluationAtARationalPoint()
        {
            var point = "abcxy".ToDictionary(v => v, v => new Fraction(v % 11 + 3, v % 3 + 2));
            foreach (var sa in Samples)
            foreach (var sb in Samples)
            {
                var a = P(sa); var b = P(sb);
                Same(a.Evaluate(point) + b.Evaluate(point), (a + b).Evaluate(point));
                Same(a.Evaluate(point) * b.Evaluate(point), (a * b).Evaluate(point));
                Same(a.Evaluate(point) - b.Evaluate(point), (a - b).Evaluate(point));
                if (!b.IsZero) Same(a.Evaluate(point) / b.Evaluate(point), (a / b).Evaluate(point));
            }
        }

        private static void Same(Fraction expected, Fraction actual)
        {
            Assert.Equal(expected.Numerator, actual.Numerator);
            Assert.Equal(expected.Denominator, actual.Denominator);
        }

        [Fact]
        public void Evaluate_IsExact()
        {
            var value = P("(x^2+1)/(y-1/3)").Evaluate(new Dictionary<char, Fraction>
            {
                ['x'] = new Fraction(1, 2),
                ['y'] = new Fraction(2, 1)
            });
            Same(new Fraction(3, 4), value);   // (5/4) / (5/3)
        }

        [Fact]
        public void Evaluate_AtAPole_Throws()
            => Assert.Throws<DivideByZeroException>(() =>
                P("1/(x-1)").Evaluate(new Dictionary<char, Fraction> { ['x'] = Fraction.One }));

        [Fact]
        public void Reciprocal_OfZero_Throws()
            => Assert.Throws<DivideByZeroException>(() => RationalFunction.Zero.Reciprocal());

        [Fact]
        public void BigCoefficients_DoNotOverflow()
        {
            var f = P("(x+1)^80");
            Assert.Equal(81, f.Numerator.TermCount);
            Assert.True((f / P("(x+1)^79") - P("x+1")).IsZero);
            Assert.Equal(BigInteger.Pow(2, 80), f.Evaluate(new Dictionary<char, Fraction> { ['x'] = Fraction.One }).Numerator);
        }

        // ---------------------------------------------------------------- gcd and exact division

        [Theory]
        [InlineData("x^2-1", "x^2+2x+1", "x + 1")]
        [InlineData("x^2-y^2", "x^2+2x*y+y^2", "x + y")]
        [InlineData("6x", "4x^2", "2*x")]
        [InlineData("x+1", "y+1", "1")]
        [InlineData("x^2+1", "x^2-1", "1")]
        [InlineData("12", "18", "6")]
        [InlineData("0", "3x-3", "3*x - 3")]
        [InlineData("-x", "0", "x")]
        [InlineData("a*b*c", "a*c*d", "a*c")]
        [InlineData("(x+y+z)(a-b)", "(x+y+z)(a+b)", "x + y + z")]
        [InlineData("(x*y-1)(x+y)(a+1)", "(x*y-1)(x-y)(a+1)", "a*x*y + x*y - a - 1")]
        [InlineData("(x^2+x*y+y^2)(x-y)^2", "(x-y)(x+y)", "x - y")]
        [InlineData("x^5-1", "x^3-1", "x - 1")]
        [InlineData("2(a+b)^3", "6(a+b)^2(a-b)", "2*a^2 + 4*a*b + 2*b^2")]
        [InlineData("a*d-b*c", "a*d+b*c", "1")]
        public void Gcd_IsExact(string left, string right, string expected)
        {
            var g = MultiPolynomial.Gcd(P(left).Numerator, P(right).Numerator);
            Assert.Equal(expected, g.ToString());
            Assert.Equal(g, MultiPolynomial.Gcd(P(right).Numerator, P(left).Numerator));
            if (!g.IsZero)
            {
                Assert.True(P(left).Numerator.TryDivideExact(g, out _));
                Assert.True(P(right).Numerator.TryDivideExact(g, out _));
            }
        }

        [Fact]
        public void Gcd_RecoversAPlantedMultivariateFactor()
        {
            var planted = P("x^2*y - 3z + a").Numerator;
            var p = P("x + y*z - 2").Numerator;
            var q = P("x*y + z^2 + 1").Numerator;
            Assert.Equal(planted, MultiPolynomial.Gcd(planted * p, planted * q));
        }

        [Theory]
        [InlineData("x^2-1", "x-1", "x + 1")]
        [InlineData("x^3+y^3", "x+y", "x^2 - x*y + y^2")]
        [InlineData("6x*y", "3y", "2*x")]
        [InlineData("0", "x", "0")]
        public void ExactDivision_Succeeds(string dividend, string divisor, string quotient)
        {
            Assert.True(P(dividend).Numerator.TryDivideExact(P(divisor).Numerator, out var q));
            Assert.Equal(quotient, q.ToString());
        }

        [Theory]
        [InlineData("x^2+1", "x-1")]
        [InlineData("x", "y")]
        [InlineData("x", "2")]
        [InlineData("x+1", "x^2")]
        [InlineData("x^2+x*y", "x+2y")]
        public void ExactDivision_ReportsFailure_RatherThanDroppingARemainder(string dividend, string divisor)
            => Assert.False(P(dividend).Numerator.TryDivideExact(P(divisor).Numerator, out _));

        // ---------------------------------------------------------------- rejection

        [Theory]
        [InlineData("sin(x)", "sin")]
        [InlineData("sqrt(x)", "sqrt")]
        [InlineData("ln(x)", "ln")]
        [InlineData("abs(x)", "abs")]
        [InlineData("x + cos(y)^2", "cos")]
        [InlineData("pi", "pi")]
        [InlineData("xy", "x*y")]
        [InlineData("x^(1/2)", "not an integer")]
        [InlineData("x^0.5", "not an integer")]
        [InlineData("x^y", "not an integer")]
        [InlineData("2^x", "not an integer")]
        [InlineData("e^x", "not an integer")]
        [InlineData("x^(2/3)", "not an integer")]
        [InlineData("i", "imaginary")]
        [InlineData("2i + 1", "imaginary")]
        [InlineData("f(x)", "function call")]
        [InlineData("x(y+1)", "function call")]
        [InlineData("x2", "number follows")]
        [InlineData("1e5", "number follows")]
        [InlineData("x_1", "unexpected '_'")]
        [InlineData("x!", "unexpected '!'")]
        [InlineData("a = b", "unexpected '='")]
        [InlineData("x**2", "unexpected '*'")]
        [InlineData("x,y", "unexpected ','")]
        [InlineData("|x|", "unexpected '|'")]
        [InlineData("√x", "unexpected '√'")]
        [InlineData("x +", "ends early")]
        [InlineData("x^", "ends early")]
        [InlineData("(x+1", "never closed")]
        [InlineData("x+1)", "unbalanced")]
        [InlineData("()", "unexpected ')'")]
        [InlineData("0^0", "0^0")]
        [InlineData("(x-x)^0", "0^0")]
        [InlineData("x^100000", "maximum")]
        [InlineData("x^-100000", "maximum")]
        [InlineData("1/0", "divides by zero")]
        [InlineData("1/(x-x)", "divides by zero")]
        [InlineData("0^-1", "divides by zero")]
        [InlineData("x/((x+1)^2-x^2-2x-1)", "divides by zero")]
        [InlineData("", "empty")]
        [InlineData("   ", "empty")]
        public void OutsideTheDomain_IsRejected_WithAReason(string expression, string reason)
        {
            var ex = Assert.Throws<RationalFunctionDomainException>(() => P(expression));
            Assert.Contains(reason, ex.Message);
        }

        [Fact]
        public void Rejection_NamesTheInput_AndWhatIsSupported()
        {
            var ex = Assert.Throws<RationalFunctionDomainException>(() => P("sin(x) + 1"));
            Assert.Contains("'sin(x) + 1' is not a rational function", ex.Message);
            Assert.Contains("integer powers", ex.Message);
            Assert.IsAssignableFrom<FormatException>(ex);
        }

        [Fact]
        public void RunawayExpansion_IsStopped_NotLeftToExhaustMemory()
            => Assert.Throws<RationalFunctionTooLargeException>(() => P("(1+a)(1+b)(1+c)(1+d)(1+f)(1+g)(1+h)(1+j)(1+k)(1+l)(1+m)(1+n)(1+o)(1+p)(1+q)(1+r)(1+s)"));
    }
}

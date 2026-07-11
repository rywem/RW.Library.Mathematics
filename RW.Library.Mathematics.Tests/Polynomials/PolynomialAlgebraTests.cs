using System.Collections.Generic;
using System.Linq;
using RW.Library.Mathematics.Polynomials;

namespace RW.Library.Mathematics.Tests.Polynomials
{
    public class PolynomialAlgebraTests
    {
        private static Dictionary<string, int> Vars(params (string v, int e)[] pairs)
            => pairs.ToDictionary(p => p.v, p => p.e);

        // --- Term algebra ----------------------------------------------------

        [Fact]
        public void Term_Multiply_AddsExponents_AndMultipliesCoefficients()
        {
            var a = new Term(3, Vars(("x", 2)));
            var b = new Term(-2, Vars(("x", 1), ("y", 1)));

            var product = Term.Multiply(a, b);

            Assert.Equal(-6, product.Coefficient);
            Assert.Equal(3, product.Variables["x"]);
            Assert.Equal(1, product.Variables["y"]);
        }

        [Fact]
        public void Monomial_Signature_IgnoresCoefficient()
        {
            var a = new Term(3, Vars(("x", 2)));
            var b = new Term(9, Vars(("x", 2)));
            Assert.Equal(Monomial.Signature(a), Monomial.Signature(b));
        }

        // --- Polynomial algebra ---------------------------------------------

        [Fact]
        public void Polynomial_CombinesLikeTerms_OnConstruction()
        {
            var p = new Polynomial(
                new Term(1, Vars(("x", 1))),
                new Term(2, Vars(("x", 1))),
                new Term(4));

            Assert.Equal(2, p.Terms.Count);
            Assert.Contains(p.Terms, t => t.Coefficient == 3 && t.Variables.ContainsKey("x"));
            Assert.Contains(p.Terms, t => t.Coefficient == 4 && t.Variables.Count == 0);
        }

        [Fact]
        public void Polynomial_ZeroCoefficientTerms_AreDropped()
        {
            var p = new Polynomial(
                new Term(1, Vars(("x", 1))),
                new Term(-1, Vars(("x", 1))));

            Assert.Empty(p.Terms); // x - x collapses to nothing
            Assert.Equal("0", p.ToString());
        }

        [Fact]
        public void Polynomial_Multiply_ExpandsProduct()
        {
            // (x + 1)(x - 1) = x^2 - 1
            var left = Polynomial.Parse("x + 1");
            var right = Polynomial.Parse("x - 1");

            var product = left * right;

            // x^2 term is 1, constant is -1, no linear term.
            Assert.Contains(product.Terms, t => t.Variables.TryGetValue("x", out var e) && e == 2 && t.Coefficient == 1);
            Assert.Contains(product.Terms, t => t.Variables.Count == 0 && t.Coefficient == -1);
            Assert.DoesNotContain(product.Terms, t => t.Variables.TryGetValue("x", out var e) && e == 1);
        }

        [Fact]
        public void Polynomial_GetVariables_ReturnsAllSymbols()
        {
            var p = Polynomial.Parse("2xy^2 - 3z + 4");
            var vars = p.GetVariables();
            Assert.Equal(new[] { "x", "y", "z" }, vars.OrderBy(v => v).ToArray());
        }

        // --- Expansion of factor trees --------------------------------------

        [Fact]
        public void Expand_BinomialSquare()
        {
            // (x + 1)^2 -> x^2 + 2x + 1
            var factor = Factor.ProductFromString("(x+1)^2");
            var expanded = PolynomialExpansion.Expand(factor);

            Assert.Contains(expanded.Terms, t => t.Variables.TryGetValue("x", out var e) && e == 2 && t.Coefficient == 1);
            Assert.Contains(expanded.Terms, t => t.Variables.TryGetValue("x", out var e) && e == 1 && t.Coefficient == 2);
            Assert.Contains(expanded.Terms, t => t.Variables.Count == 0 && t.Coefficient == 1);
        }

        [Fact]
        public void Expand_CubeOfBinomial_MultiVariable()
        {
            // (a + h)^3 -> a^3 + 3a^2h + 3ah^2 + h^3
            var factor = Factor.ProductFromString("(a+h)^3");
            var expanded = PolynomialExpansion.Expand(factor);

            Assert.Contains(expanded.Terms, t => t.Coefficient == 1 && Deg(t, "a") == 3 && Deg(t, "h") == 0);
            Assert.Contains(expanded.Terms, t => t.Coefficient == 3 && Deg(t, "a") == 2 && Deg(t, "h") == 1);
            Assert.Contains(expanded.Terms, t => t.Coefficient == 3 && Deg(t, "a") == 1 && Deg(t, "h") == 2);
            Assert.Contains(expanded.Terms, t => t.Coefficient == 1 && Deg(t, "a") == 0 && Deg(t, "h") == 3);
        }

        [Fact]
        public void ParseMany_PowerZero_YieldsConstantOne()
        {
            var factors = Factor.ParseMany("(x+1)^0").ToArray();
            var expanded = PolynomialExpansion.Expand(Factor.Product(factors));
            Assert.Equal("1", expanded.ToString());
        }

        [Fact]
        public void ParseMany_IntegerPower_RepeatsFactor()
        {
            var factors = Factor.ParseMany("(x+1)^3").ToArray();
            Assert.Equal(3, factors.Length);
        }

        [Fact]
        public void ParseMany_FractionalExponent_Throws()
        {
            Assert.Throws<System.NotSupportedException>(() => Factor.ParseMany("(x+1)^1/2").ToArray());
        }

        private static int Deg(Term t, string v) => t.Variables.TryGetValue(v, out var e) ? e : 0;
    }
}

using System.Collections.Generic;
using System.Linq;
using RW.Library.Mathematics.Polynomials;

namespace RW.Library.Mathematics.Tests.Factorization
{
    public class PolynomialFactorizationTests
    {
        /// <summary>
        /// The fundamental invariant: expanding the factor tree must reproduce the
        /// original polynomial exactly. This is format-agnostic and covers coefficient,
        /// content and sign handling in one shot.
        /// </summary>
        [Theory]
        [InlineData("x^2 - 5x + 6")]         // (x-2)(x-3)
        [InlineData("x^2 - 1")]              // (x-1)(x+1)
        [InlineData("x^2 + 2x + 1")]         // (x+1)^2
        [InlineData("2x^2 - 7x - 4")]        // (2x+1)(x-4)
        [InlineData("x^3 - 6x^2 + 11x - 6")] // (x-1)(x-2)(x-3)
        [InlineData("4x^2 - 2x^3 - 2")]      // leading negative coefficient
        [InlineData("6x^2 + 11x - 10")]      // (3x-2)(2x+5)
        [InlineData("x^2 + 1")]              // irreducible over the rationals
        [InlineData("x^2 - 2")]              // irreducible over the rationals
        [InlineData("5")]                    // pure constant
        [InlineData("x^3")]                  // repeated root at 0
        public void Factorize_Then_Expand_Reproduces_Original(string expression)
        {
            var original = Polynomial.Parse(expression);

            var factorTree = PolynomialFactorization.Factorize(original);
            var expanded = PolynomialExpansion.Expand(factorTree);

            Assert.Equal(original.ToString(), expanded.ToString());
        }

        [Fact]
        public void Factorize_Quadratic_ProducesTwoLinearFactors()
        {
            var poly = Polynomial.Parse("x^2 - 5x + 6");
            var tree = PolynomialFactorization.Factorize(poly);

            var leaves = Flatten(tree).ToList();
            // Two non-constant linear leaves: (x - 2) and (x - 3).
            var linear = leaves.Where(p => p.Terms.Any(t => t.Variables.Count > 0)).ToList();
            Assert.Equal(2, linear.Count);
            Assert.All(linear, p => Assert.Equal(1, MaxDegree(p)));
        }

        [Fact]
        public void Factorize_Multivariate_Throws()
        {
            var poly = Polynomial.Parse("x^2 + xy + y^2");
            Assert.Throws<System.InvalidOperationException>(() => PolynomialFactorization.Factorize(poly));
        }

        [Fact]
        public void Factorize_Irreducible_KeepsSingleLeaf()
        {
            var poly = Polynomial.Parse("x^2 + 1");
            var tree = PolynomialFactorization.Factorize(poly);
            var expanded = PolynomialExpansion.Expand(tree);
            Assert.Equal(poly.ToString(), expanded.ToString());
        }

        // --- helpers ---------------------------------------------------------

        private static IEnumerable<Polynomial> Flatten(Factor factor)
        {
            var acc = new List<Polynomial>();
            void Collect(Factor n)
            {
                if (n.IsLeaf && n.Polynomial != null) { acc.Add(n.Polynomial); return; }
                foreach (var c in n.Children) Collect(c);
            }
            Collect(factor);
            return acc;
        }

        private static int MaxDegree(Polynomial p) =>
            p.Terms.SelectMany(t => t.Variables.Values).DefaultIfEmpty(0).Max();
    }
}

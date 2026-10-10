using System.Diagnostics;
using RW.Library.Mathematics.Polynomials;
using RW.Library.Mathematics.RationalFunctions;

namespace RW.Library.Mathematics.Tests.RationalFunctions
{
    /// <summary>
    /// Exact linear algebra over rational functions. Matrices are written "row; row" with
    /// cells separated by spaces, so a cell itself contains no spaces.
    /// </summary>
    public class RationalMatrixTests
    {
        // ---------------------------------------------------------------- helpers

        private static RationalFunction[,] Mat(string text)
            => RationalMatrix.Parse(text.Split(';')
                .Select(row => row.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToArray());

        private static string Show(RationalFunction[,] m)
            => string.Join("; ", Enumerable.Range(0, m.GetLength(0)).Select(r =>
                string.Join(" ", Enumerable.Range(0, m.GetLength(1)).Select(c => m[r, c].ToString().Replace(" ", "")))));

        private static void AssertSame(RationalFunction[,] expected, RationalFunction[,] actual)
        {
            Assert.Equal(expected.GetLength(0), actual.GetLength(0));
            Assert.Equal(expected.GetLength(1), actual.GetLength(1));
            for (int r = 0; r < expected.GetLength(0); r++)
                for (int c = 0; c < expected.GetLength(1); c++)
                    Assert.True(expected[r, c].Equals(actual[r, c]),
                        $"[{r},{c}]: expected {expected[r, c]}, got {actual[r, c]}\n  expected {Show(expected)}\n  actual   {Show(actual)}");
        }

        private static RationalFunction[,] Identity(int n)
        {
            var m = new RationalFunction[n, n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    m[r, c] = r == c ? RationalFunction.One : RationalFunction.Zero;
            return m;
        }

        private static RationalFunction[,] Transpose(RationalFunction[,] m)
        {
            var t = new RationalFunction[m.GetLength(1), m.GetLength(0)];
            for (int r = 0; r < m.GetLength(0); r++)
                for (int c = 0; c < m.GetLength(1); c++)
                    t[c, r] = m[r, c];
            return t;
        }

        private static RationalFunction[,] Column(RationalFunction[] v)
        {
            var m = new RationalFunction[v.Length, 1];
            for (int r = 0; r < v.Length; r++) m[r, 0] = v[r];
            return m;
        }

        private static bool IsZero(RationalFunction[,] m) => m.Cast<RationalFunction>().All(e => e.IsZero);

        /// <summary>A fixed rational point at which none of the test matrices has a pole.</summary>
        private static readonly Dictionary<char, Fraction> Point =
            "abcdfghjkmnpqrstxyz".Select((v, k) => (v, k)).ToDictionary(p => p.v, p => new Fraction(3 * p.k * p.k + 7, p.k + 2));

        private static RationalFunction Const(Fraction f)
            => RationalFunction.Create(MultiPolynomial.Constant(f.Numerator), MultiPolynomial.Constant(f.Denominator));

        /// <summary>The matrix with every variable replaced by its value at <see cref="Point"/>.</summary>
        private static RationalFunction[,] AtPoint(RationalFunction[,] m)
        {
            var result = new RationalFunction[m.GetLength(0), m.GetLength(1)];
            for (int r = 0; r < m.GetLength(0); r++)
                for (int c = 0; c < m.GetLength(1); c++)
                    result[r, c] = Const(m[r, c].Evaluate(Point));
            return result;
        }

        /// <summary>
        /// Textbook Gauss-Jordan written directly with field operations. It shares nothing with
        /// the fraction-free elimination under test except the field itself, so agreement
        /// between the two is evidence for both.
        /// </summary>
        private static RationalFunction[,] NaiveRref(RationalFunction[,] source)
        {
            var m = (RationalFunction[,])source.Clone();
            int rows = m.GetLength(0), cols = m.GetLength(1);
            for (int col = 0, row = 0; col < cols && row < rows; col++)
            {
                int p = row;
                while (p < rows && m[p, col].IsZero) p++;
                if (p == rows) continue;
                for (int c = 0; c < cols; c++) (m[row, c], m[p, c]) = (m[p, c], m[row, c]);
                var pivot = m[row, col];
                for (int c = 0; c < cols; c++) m[row, c] /= pivot;
                for (int r = 0; r < rows; r++)
                {
                    if (r == row) continue;
                    var factor = m[r, col];
                    for (int c = 0; c < cols; c++) m[r, c] -= factor * m[row, c];
                }
                row++;
            }
            return m;
        }

        // ---------------------------------------------------------------- the corpus

        /// <summary>Every matrix with its rank, worked out by hand.</summary>
        private static readonly (string Matrix, int Rank)[] Corpus =
        {
            ("1 2; 3 4", 2),
            ("1 2 3; 4 5 6; 7 8 9", 2),
            ("x a; x a", 1),
            ("a b; c d", 2),
            ("x 1; 1 x", 2),
            ("x+1 x^2-1; 1 x-1", 1),
            ("1/(x+1) 1; 1 x+1", 1),
            ("1/(x+1) 1/(x-1); x y", 2),
            ("a b c; d f g; h k m", 3),
            ("1 a a^2; 1 b b^2; 1 c c^2", 3),
            ("x 1 0; 1 x 1; 0 1 x", 3),
            ("a b c d; b c d a", 2),
            ("a b; c d; a+c b+d; x*a x*b", 2),
            ("0 0; 0 0", 0),
            ("x", 1),
            ("0", 0),
            ("0 x; y 1", 2),
            ("a b c; a*x b*x c*x; a/y b/y c/y", 1),
            ("1 a a^2 a^3; 1 b b^2 b^3; 1 c c^2 c^3; 1 d d^2 d^3", 4),
            ("x 1 2 3; 1 x 1 2; 2 1 x 1; 3 2 1 x", 4),
            ("1/x 1/y 1/z; x y z; 1 1 1", 3),
            ("(x+1)^2-x^2-2x-1 1 x; 0 0 y; 0 0 0", 2),
            ("a b c d; f g h j; k m n p; q r s t", 4),
            ("1/2 1/3; 0.25 1/5", 2),
            ("x^2-y^2 x-y; x+y 1", 1),
            ("a 0 0; 0 b 0; 0 0 c", 3),
            ("1 2 3 4; 2 4 6 8; 1 0 1 0", 2),
            ("0 0 1; 0 0 x; 0 0 0; 0 y 0", 2),
            ("x/(y+1) y/(x+1); (x^2+x)/(y+1) y", 1),
            ("1 x x^2 x^3 x^4", 1),
            ("1; x; x^2", 1),
            ("a+b a-b; a-b a+b", 2),
            ("1/(a-b) 1/(a+b); 1/(a+b) 1/(a-b)", 2),
            ("x y 0 0; 0 0 x y; x y x y", 2),
        };

        public static IEnumerable<object[]> All => Corpus.Select(m => new object[] { m.Matrix });
        public static IEnumerable<object[]> AllWithRank => Corpus.Select(m => new object[] { m.Matrix, m.Rank });

        private static bool IsSquare(string m) => m.Split(';').Length == m.Split(';')[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        public static IEnumerable<object[]> Square => Corpus.Where(m => IsSquare(m.Matrix)).Select(m => new object[] { m.Matrix });

        // Singular exactly when the hand-computed rank is below the size.
        public static IEnumerable<object[]> Invertible =>
            Corpus.Where(m => IsSquare(m.Matrix) && m.Rank == m.Matrix.Split(';').Length).Select(m => new object[] { m.Matrix });

        public static IEnumerable<object[]> Singular =>
            Corpus.Where(m => IsSquare(m.Matrix) && m.Rank < m.Matrix.Split(';').Length).Select(m => new object[] { m.Matrix });

        /// <summary>Unordered pairs of square matrices of the same size, 3x3 at most.</summary>
        public static IEnumerable<object[]> SquarePairs
        {
            get
            {
                var squares = Corpus.Select(m => m.Matrix).Where(IsSquare).Where(m => m.Split(';').Length <= 3).ToList();
                for (int i = 0; i < squares.Count; i++)
                    for (int j = i; j < squares.Count; j++)
                        if (squares[i].Split(';').Length == squares[j].Split(';').Length)
                            yield return new object[] { squares[i], squares[j] };
            }
        }

        // ---------------------------------------------------------------- properties over the corpus

        [Theory, MemberData(nameof(AllWithRank))]
        public void Rank_MatchesTheHandComputedValue(string matrix, int rank)
            => Assert.Equal(rank, RationalMatrix.Rank(Mat(matrix)));

        [Theory, MemberData(nameof(AllWithRank))]
        public void Rank_OfTheTranspose_IsTheSame(string matrix, int rank)
            => Assert.Equal(rank, RationalMatrix.Rank(Transpose(Mat(matrix))));

        [Theory, MemberData(nameof(All))]
        public void Rref_IsIdempotent(string matrix)
        {
            var once = RationalMatrix.Rref(Mat(matrix));
            AssertSame(once, RationalMatrix.Rref(once));
        }

        [Theory, MemberData(nameof(All))]
        public void Rref_AgreesWithTextbookGaussJordanInTheField(string matrix)
            => AssertSame(NaiveRref(Mat(matrix)), RationalMatrix.Rref(Mat(matrix)));

        [Theory, MemberData(nameof(All))]
        public void Rref_HasTheRrefShape(string matrix)
        {
            var rref = RationalMatrix.Rref(Mat(matrix), out var pivots);
            Assert.Equal(pivots.OrderBy(p => p), pivots);
            Assert.Equal(pivots.Distinct().Count(), pivots.Length);
            for (int r = 0; r < rref.GetLength(0); r++)
            {
                for (int c = 0; c < rref.GetLength(1); c++)
                {
                    bool beforePivot = r >= pivots.Length || c < pivots[r];
                    if (beforePivot) Assert.True(rref[r, c].IsZero, $"[{r},{c}] should be 0 in {Show(rref)}");
                }
                if (r < pivots.Length)
                    for (int other = 0; other < rref.GetLength(0); other++)
                        Assert.Equal(other == r ? RationalFunction.One : RationalFunction.Zero, rref[other, pivots[r]]);
            }
        }

        /// <summary>
        /// "Generic" made concrete: at a point chosen with no special relation to the entries,
        /// substituting first and reducing second gives the same matrix as reducing first.
        /// </summary>
        [Theory, MemberData(nameof(All))]
        public void Rref_CommutesWithSubstitutingAGenericPoint(string matrix)
            => AssertSame(RationalMatrix.Rref(AtPoint(Mat(matrix))), AtPoint(RationalMatrix.Rref(Mat(matrix))));

        [Theory, MemberData(nameof(AllWithRank))]
        public void NullSpace_HasDimensionColumnsMinusRank_AndEveryVectorIsKilledExactly(string matrix, int rank)
        {
            var a = Mat(matrix);
            var basis = RationalMatrix.NullSpace(a);
            Assert.Equal(a.GetLength(1) - rank, basis.Length);
            foreach (var v in basis)
            {
                Assert.Equal(a.GetLength(1), v.Length);
                Assert.True(IsZero(RationalMatrix.Multiply(a, Column(v))), $"A·v is not 0 for {matrix}");
            }
        }

        [Theory, MemberData(nameof(All))]
        public void NullSpace_VectorsAreIndependent(string matrix)
        {
            var basis = RationalMatrix.NullSpace(Mat(matrix));
            if (basis.Length == 0) return;
            var asRows = new RationalFunction[basis.Length, basis[0].Length];
            for (int r = 0; r < basis.Length; r++)
                for (int c = 0; c < basis[0].Length; c++)
                    asRows[r, c] = basis[r][c];
            Assert.Equal(basis.Length, RationalMatrix.Rank(asRows));
        }

        [Theory, MemberData(nameof(Invertible))]
        public void Inverse_TimesTheMatrix_IsExactlyTheIdentity_OnBothSides(string matrix)
        {
            var a = Mat(matrix);
            var inverse = RationalMatrix.Inverse(a);
            AssertSame(Identity(a.GetLength(0)), RationalMatrix.Multiply(a, inverse));
            AssertSame(Identity(a.GetLength(0)), RationalMatrix.Multiply(inverse, a));
        }

        // The 16-symbol 4x4 is left out here only for time: inverting its inverse (every entry a
        // cubic over the same 24-term determinant) is correct but takes over ten seconds.
        public static IEnumerable<object[]> InvertibleAndQuickToInvertTwice =>
            Invertible.Where(m => !((string)m[0]).StartsWith("a b c d; f g h j"));

        [Theory, MemberData(nameof(InvertibleAndQuickToInvertTwice))]
        public void Inverse_OfTheInverse_IsTheMatrix_AndItsDeterminantIsTheReciprocal(string matrix)
        {
            var a = Mat(matrix);
            var inverse = RationalMatrix.Inverse(a);
            AssertSame(a, RationalMatrix.Inverse(inverse));
            Assert.Equal(RationalMatrix.Determinant(a).Reciprocal(), RationalMatrix.Determinant(inverse));
        }

        [Theory, MemberData(nameof(Invertible))]
        public void InvertibleMatrix_ReducesToTheIdentity_WithANonZeroDeterminant(string matrix)
        {
            var a = Mat(matrix);
            AssertSame(Identity(a.GetLength(0)), RationalMatrix.Rref(a));
            Assert.False(RationalMatrix.Determinant(a).IsZero);
            Assert.Empty(RationalMatrix.NullSpace(a));
        }

        [Theory, MemberData(nameof(Singular))]
        public void SingularMatrix_HasDeterminantZero_AndNoInverse(string matrix)
        {
            var a = Mat(matrix);
            Assert.True(RationalMatrix.Determinant(a).IsZero);
            var ex = Assert.Throws<InvalidOperationException>(() => RationalMatrix.Inverse(a));
            Assert.Contains("no inverse", ex.Message);
        }

        [Theory, MemberData(nameof(Square))]
        public void Determinant_OfTheTranspose_IsTheSame(string matrix)
            => Assert.Equal(RationalMatrix.Determinant(Mat(matrix)), RationalMatrix.Determinant(Transpose(Mat(matrix))));

        [Theory, MemberData(nameof(Square))]
        public void Determinant_CommutesWithSubstitutingAPoint(string matrix)
            => Assert.Equal(Const(RationalMatrix.Determinant(Mat(matrix)).Evaluate(Point)),
                            RationalMatrix.Determinant(AtPoint(Mat(matrix))));

        [Theory, MemberData(nameof(Square))]
        public void Determinant_AgreesWithCofactorExpansion(string matrix)
            => Assert.Equal(Cofactor(Mat(matrix)), RationalMatrix.Determinant(Mat(matrix)));

        /// <summary>Laplace expansion along the first row: slow, obviously right, no elimination.</summary>
        private static RationalFunction Cofactor(RationalFunction[,] m)
        {
            int n = m.GetLength(0);
            if (n == 1) return m[0, 0];
            var sum = RationalFunction.Zero;
            for (int col = 0; col < n; col++)
            {
                var minor = new RationalFunction[n - 1, n - 1];
                for (int r = 1; r < n; r++)
                    for (int c = 0, k = 0; c < n; c++)
                        if (c != col) minor[r - 1, k++] = m[r, c];
                var term = m[0, col] * Cofactor(minor);
                sum = col % 2 == 0 ? sum + term : sum - term;
            }
            return sum;
        }

        [Theory, MemberData(nameof(SquarePairs))]
        public void Determinant_IsMultiplicative(string left, string right)
        {
            var a = Mat(left); var b = Mat(right);
            Assert.Equal(RationalMatrix.Determinant(a) * RationalMatrix.Determinant(b),
                         RationalMatrix.Determinant(RationalMatrix.Multiply(a, b)));
        }

        // ---------------------------------------------------------------- hand-computed answers

        [Theory]
        [InlineData("1 2; 3 4", "1 0; 0 1")]
        [InlineData("1 2 3; 4 5 6; 7 8 9", "1 0 -1; 0 1 2; 0 0 0")]
        [InlineData("2 4; 1 2", "1 2; 0 0")]
        [InlineData("1 2 3 4; 2 4 6 8; 1 0 1 0", "1 0 1 0; 0 1 1 2; 0 0 0 0")]
        [InlineData("0 0 5; 0 0 0", "0 0 1; 0 0 0")]
        [InlineData("1/2 1/3 1; 1/4 1/5 0", "1 0 12; 0 1 -15")]
        [InlineData("x a; x a", "1 a/x; 0 0")]
        [InlineData("a b; c d", "1 0; 0 1")]
        [InlineData("x 1; 1 1", "1 0; 0 1")]
        [InlineData("x+1 x^2-1; 1 x-1", "1 x-1; 0 0")]
        [InlineData("1/(x+1) 1; 1 x+1", "1 x+1; 0 0")]
        [InlineData("a b c; a*x b*x c*x", "1 b/a c/a; 0 0 0")]
        [InlineData("a b c d; b c d a", "1 0 (c^2-b*d)/(a*c-b^2) (c*d-a*b)/(a*c-b^2); 0 1 (a*d-b*c)/(a*c-b^2) (a^2-b*d)/(a*c-b^2)")]
        [InlineData("(x+1)^2-x^2-2x-1 1 x; 0 0 y; 0 0 0", "0 1 0; 0 0 1; 0 0 0")]
        [InlineData("0 x; y 1", "1 0; 0 1")]
        [InlineData("x", "1")]
        [InlineData("0", "0")]
        [InlineData("1; x; x^2", "1; 0; 0")]
        [InlineData("x y z", "1 y/x z/x")]
        public void Rref_HandComputed(string matrix, string expected)
            => AssertSame(Mat(expected), RationalMatrix.Rref(Mat(matrix)));

        [Theory]
        [InlineData("2 1; 5 3", "1")]
        [InlineData("1 2 3; 0 1 4; 5 6 0", "1")]
        [InlineData("1 2 3; 4 5 6; 7 8 9", "0")]
        [InlineData("1/2 1/3; 1/4 1/5", "1/60")]
        [InlineData("0 1; 1 0", "-1")]
        [InlineData("0 0 1; 0 1 0; 1 0 0", "-1")]
        [InlineData("0 1 0; 0 0 1; 1 0 0", "1")]
        [InlineData("a b; c d", "a*d-b*c")]
        [InlineData("0 a; b 0", "-a*b")]
        [InlineData("x a; x a", "0")]
        [InlineData("x 1; 1 x", "x^2-1")]
        [InlineData("1/(x+1) 1/(x-1); 1 1", "-2/(x^2-1)")]
        [InlineData("1 a a^2; 1 b b^2; 1 c c^2", "(b-a)(c-a)(c-b)")]
        [InlineData("1 a a^2 a^3; 1 b b^2 b^3; 1 c c^2 c^3; 1 d d^2 d^3", "(b-a)(c-a)(d-a)(c-b)(d-b)(d-c)")]
        [InlineData("x 1 0; 1 x 1; 0 1 x", "x^3-2x")]
        [InlineData("a 0 0; 0 b 0; 0 0 c", "a*b*c")]
        [InlineData("a b c; 0 d f; 0 0 g", "a*d*g")]
        [InlineData("a b c; d f g; h k m", "a*(f*m-g*k)-b*(d*m-g*h)+c*(d*k-f*h)")]
        [InlineData("x", "x")]
        [InlineData("1/x", "1/x")]
        [InlineData("0", "0")]
        [InlineData("x 0; 0 1/x", "1")]
        [InlineData("1/a 1/b; 1/c 1/d", "(b*c-a*d)/(a*b*c*d)")]
        public void Determinant_HandComputed(string matrix, string expected)
            => Assert.Equal(RationalFunction.Parse(expected), RationalMatrix.Determinant(Mat(matrix)));

        [Theory]
        [InlineData("2 1; 5 3", "3 -1; -5 2")]
        [InlineData("1 2 3; 0 1 4; 5 6 0", "-24 18 5; 20 -15 -4; -5 4 1")]
        [InlineData("1 2; 3 4", "-2 1; 3/2 -1/2")]
        [InlineData("a b; c d", "d/(a*d-b*c) -b/(a*d-b*c); -c/(a*d-b*c) a/(a*d-b*c)")]
        [InlineData("x", "1/x")]
        [InlineData("1/(x+1)", "x+1")]
        [InlineData("0 x; y 0", "0 1/y; 1/x 0")]
        [InlineData("x 1; 1 x", "x/(x^2-1) -1/(x^2-1); -1/(x^2-1) x/(x^2-1)")]
        [InlineData("a 0 0; 0 b 0; 0 0 c", "1/a 0 0; 0 1/b 0; 0 0 1/c")]
        [InlineData("1 x; 0 1", "1 -x; 0 1")]
        public void Inverse_HandComputed(string matrix, string expected)
            => AssertSame(Mat(expected), RationalMatrix.Inverse(Mat(matrix)));

        // ---------------------------------------------------------------- the two AngouriMath failures

        [Fact]
        public void TwoEqualSymbolicRows_HaveRankOne()
            => Assert.Equal(1, RationalMatrix.Rank(Mat("x a; x a")));

        [Fact]
        public void GenericTwoByTwo_ReducesToTheIdentity_WithNoNaN()
        {
            var rref = RationalMatrix.Rref(Mat("a b; c d"), out var pivots);
            Assert.Equal("1 0; 0 1", Show(rref));
            Assert.Equal(new[] { 0, 1 }, pivots);
        }

        // ---------------------------------------------------------------- the documented limit

        [Fact]
        public void Results_AreGeneric_NotCaseSplitOnParameterValues()
        {
            // x - 1 is not the zero polynomial, so [x 1; 1 1] has rank 2 as a matrix over Q(x)...
            Assert.Equal(2, RationalMatrix.Rank(Mat("x 1; 1 1")));
            Assert.Equal("x - 1", RationalMatrix.Determinant(Mat("x 1; 1 1")).ToString());
            // ...even though the particular matrix at x = 1 has rank 1. That case is not reported.
            Assert.Equal(1, RationalMatrix.Rank(Mat("1 1; 1 1")));
        }

        [Fact]
        public void GenericInverse_Exists_EvenThoughItHasPoles()
        {
            var inverse = RationalMatrix.Inverse(Mat("x 1; 1 1"));
            Assert.Equal("1/(x-1) (-1)/(x-1); (-1)/(x-1) x/(x-1)", Show(inverse));
        }

        // ---------------------------------------------------------------- shapes

        [Theory]
        [InlineData("0 0; 0 0", 2)]
        [InlineData("0 0 0", 3)]
        [InlineData("0; 0; 0", 1)]
        [InlineData("x-x (x+1)^2-x^2-2x-1; 0 a-a", 2)]
        public void ZeroMatrix_HasRankZero_AndItsNullSpaceIsEverything(string matrix, int columns)
        {
            var a = Mat(matrix);
            Assert.Equal(0, RationalMatrix.Rank(a));
            Assert.True(IsZero(RationalMatrix.Rref(a, out var pivots)));
            Assert.Empty(pivots);
            var basis = RationalMatrix.NullSpace(a);
            Assert.Equal(columns, basis.Length);
            for (int k = 0; k < columns; k++)
                for (int c = 0; c < columns; c++)
                    Assert.Equal(k == c ? RationalFunction.One : RationalFunction.Zero, basis[k][c]);
        }

        [Fact]
        public void WideMatrix_HasFreeColumns()
        {
            RationalMatrix.Rref(Mat("1 x 0 y; 0 0 1 z"), out var pivots);
            Assert.Equal(new[] { 0, 2 }, pivots);
            var basis = RationalMatrix.NullSpace(Mat("1 x 0 y; 0 0 1 z"));
            Assert.Equal(2, basis.Length);
            Assert.Equal(new[] { "-x", "1", "0", "0" }, basis[0].Select(e => e.ToString()));
            Assert.Equal(new[] { "-y", "0", "-z", "1" }, basis[1].Select(e => e.ToString()));
        }

        [Fact]
        public void TallMatrix_OfFullColumnRank_HasATrivialNullSpace()
        {
            var a = Mat("a b; c d; a+c b+d; x*a x*b");
            Assert.Equal("1 0; 0 1; 0 0; 0 0", Show(RationalMatrix.Rref(a)));
            Assert.Empty(RationalMatrix.NullSpace(a));
        }

        [Fact]
        public void NullSpace_HandComputed()
        {
            var basis = RationalMatrix.NullSpace(Mat("1 2 3; 4 5 6; 7 8 9"));
            Assert.Equal(new[] { "1", "-2", "1" }, Assert.Single(basis).Select(e => e.ToString()));

            basis = RationalMatrix.NullSpace(Mat("x a; x a"));
            Assert.Equal(new[] { "(-a)/x", "1" }, Assert.Single(basis).Select(e => e.ToString()));
        }

        [Theory]
        [InlineData("a b c")]
        [InlineData("a b; c d; f g")]
        public void Determinant_AndInverse_NeedASquareMatrix(string matrix)
        {
            Assert.Contains("square", Assert.Throws<ArgumentException>(() => RationalMatrix.Determinant(Mat(matrix))).Message);
            Assert.Contains("square", Assert.Throws<ArgumentException>(() => RationalMatrix.Inverse(Mat(matrix))).Message);
        }

        // ---------------------------------------------------------------- solve

        private static LinearSolution Solve(string augmented) => RationalMatrix.Solve(Mat(augmented));

        private static string[] Text(RationalFunction[] v) => v.Select(e => e.ToString().Replace(" ", "")).ToArray();

        [Theory]
        [InlineData("1 1 3; 1 -1 1", "2 1")]
        [InlineData("2 0 0 2; 0 3 0 3; 0 0 4 2", "1 1 1/2")]
        [InlineData("1 1 1 6; 0 2 5 -4; 2 5 -1 27", "5 3 -2")]
        [InlineData("a b f; c d g", "(d*f-b*g)/(a*d-b*c) (a*g-c*f)/(a*d-b*c)")]
        [InlineData("x 1", "1/x")]
        [InlineData("x 1 1; 1 x 1", "1/(x+1) 1/(x+1)")]
        [InlineData("1 0 a; 0 1 b; 1 1 a+b", "a b")]
        [InlineData("1/(x+1) 0 1; 0 x-1 x^2-1", "x+1 x+1")]
        public void Solve_Unique(string augmented, string expected)
        {
            var solution = Solve(augmented);
            Assert.Equal(LinearSolutionKind.Unique, solution.Kind);
            Assert.Equal(expected.Split(' ').Select(e => RationalFunction.Parse(e)), solution.Particular);
            Assert.Empty(solution.Homogeneous);
            Assert.Empty(solution.FreeColumns);
        }

        [Fact]
        public void Solve_Parametric_Numeric()
        {
            var solution = Solve("1 1 2; 2 2 4");
            Assert.Equal(LinearSolutionKind.Parametric, solution.Kind);
            Assert.Equal(new[] { "2", "0" }, Text(solution.Particular!));
            Assert.Equal(new[] { "-1", "1" }, Text(Assert.Single(solution.Homogeneous)));
            Assert.Equal(new[] { 1 }, solution.FreeColumns);
        }

        [Fact]
        public void Solve_Parametric_Symbolic()
        {
            var solution = Solve("x a b; x a b");
            Assert.Equal(LinearSolutionKind.Parametric, solution.Kind);
            Assert.Equal(new[] { "b/x", "0" }, Text(solution.Particular!));
            Assert.Equal(new[] { "(-a)/x", "1" }, Text(Assert.Single(solution.Homogeneous)));
        }

        [Fact]
        public void Solve_Parametric_WithAFreeVariableInTheMiddle()
        {
            var solution = Solve("1 2 0 3; 0 0 1 x");
            Assert.Equal(LinearSolutionKind.Parametric, solution.Kind);
            Assert.Equal(new[] { 1 }, solution.FreeColumns);
            Assert.Equal(new[] { "3", "0", "x" }, Text(solution.Particular!));
            Assert.Equal(new[] { "-2", "1", "0" }, Text(solution.Homogeneous[0]));
        }

        [Theory]
        [InlineData("1 1 2; 1 1 3")]
        [InlineData("x a 1; x a 2")]
        [InlineData("0 0 1")]
        [InlineData("0 x")]
        [InlineData("1 0 a; 0 1 b; 1 1 a+b+1")]
        [InlineData("x+1 x^2-1 1; 1 x-1 1")]
        public void Solve_Inconsistent(string augmented)
        {
            var solution = Solve(augmented);
            Assert.Equal(LinearSolutionKind.Inconsistent, solution.Kind);
            Assert.Null(solution.Particular);
        }

        [Fact]
        public void Solve_ConsistencyIsGeneric_Too()
        {
            // 0 = x is inconsistent over Q(x): x is an indeterminate, not possibly-zero.
            Assert.Equal(LinearSolutionKind.Inconsistent, Solve("1 1 1; 1 1 1+x").Kind);
            // With a hidden zero on the right the same system is consistent.
            Assert.Equal(LinearSolutionKind.Parametric, Solve("1 1 1; 1 1 1+(x+1)^2-x^2-2x-1").Kind);
        }

        [Theory]
        [InlineData("1 1 3; 1 -1 1")]
        [InlineData("a b f; c d g")]
        [InlineData("x a b; x a b")]
        [InlineData("1 2 0 3; 0 0 1 x")]
        [InlineData("a b c d; f g h j")]
        [InlineData("1/(x+1) 1 x; 1 x+1 x^2+x")]
        [InlineData("a b c f; d g h k; m n p q")]
        [InlineData("0 0 0; 0 0 0")]
        [InlineData("x y z 1")]
        [InlineData("1 a a^2 x; 1 b b^2 y; 1 c c^2 z")]
        public void Solve_EverySolutionItDescribes_SatisfiesTheSystemExactly(string augmented)
        {
            var m = Mat(augmented);
            int rows = m.GetLength(0), unknowns = m.GetLength(1) - 1;
            var a = new RationalFunction[rows, unknowns];
            var b = new RationalFunction[rows, 1];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < unknowns; c++) a[r, c] = m[r, c];
                b[r, 0] = m[r, unknowns];
            }

            var solution = RationalMatrix.Solve(m);
            Assert.NotEqual(LinearSolutionKind.Inconsistent, solution.Kind);
            AssertSame(b, RationalMatrix.Multiply(a, Column(solution.Particular!)));
            Assert.Equal(unknowns - RationalMatrix.Rank(a), solution.Homogeneous.Length);
            foreach (var v in solution.Homogeneous)
                Assert.True(IsZero(RationalMatrix.Multiply(a, Column(v))));

            // Particular + (symbolic multiples of the basis) is still a solution.
            var combined = (RationalFunction[])solution.Particular!.Clone();
            for (int k = 0; k < solution.Homogeneous.Length; k++)
                for (int c = 0; c < unknowns; c++)
                    combined[c] += RationalFunction.Parse($"t^{k + 1}/(u+{k})") * solution.Homogeneous[k][c];
            AssertSame(b, RationalMatrix.Multiply(a, Column(combined)));
        }

        [Fact]
        public void Solve_NeedsARightHandSideColumn()
            => Assert.Throws<ArgumentException>(() => Solve("1; 2"));

        [Fact]
        public void Solve_AgreesWithTheInverse()
        {
            var a = Mat("a b c; d f g; h k m");
            var solution = Solve("a b c x; d f g y; h k m z");
            AssertSame(RationalMatrix.Multiply(RationalMatrix.Inverse(a), Mat("x; y; z")), Column(solution.Particular!));
        }

        // ---------------------------------------------------------------- parsing a grid

        [Fact]
        public void Parse_RejectsACellOutsideTheDomain_AndSaysWhichCell()
        {
            var ex = Assert.Throws<RationalFunctionDomainException>(() =>
                RationalMatrix.Parse(new[] { new[] { "x", "1" }, new[] { "sin(x)", "2" } }));
            Assert.Contains("Row 2, column 1", ex.Message);
            Assert.Contains("sin", ex.Message);
        }

        [Theory]
        [InlineData("sqrt(x)")]
        [InlineData("x^(1/2)")]
        [InlineData("2^x")]
        [InlineData("")]
        [InlineData("1/0")]
        public void Parse_NeverLetsABadCellThrough(string cell)
            => Assert.Throws<RationalFunctionDomainException>(() =>
                RationalMatrix.Parse(new[] { new[] { "1", cell }, new[] { "0", "1" } }));

        [Fact]
        public void Parse_RejectsEmptyAndRaggedGrids()
        {
            Assert.Contains("empty", Assert.Throws<ArgumentException>(() => RationalMatrix.Parse(Array.Empty<string[]>())).Message);
            Assert.Contains("empty", Assert.Throws<ArgumentException>(() => RationalMatrix.Parse(new[] { Array.Empty<string>() })).Message);
            Assert.Contains("same length", Assert.Throws<ArgumentException>(() =>
                RationalMatrix.Parse(new[] { new[] { "a", "b" }, new[] { "c" } })).Message);
        }

        // ---------------------------------------------------------------- larger cases

        [Fact]
        public void GenericFourByFour_DeterminantHas24Terms_AndTheInverseIsExact()
        {
            var a = Mat("a b c d; f g h j; k m n p; q r s t");
            var det = RationalMatrix.Determinant(a);
            Assert.True(det.IsPolynomial);
            Assert.Equal(24, det.Numerator.TermCount);

            var inverse = RationalMatrix.Inverse(a);
            AssertSame(Identity(4), RationalMatrix.Multiply(a, inverse));
            // Every entry of the inverse is a cofactor over the determinant, already in lowest terms.
            foreach (var entry in inverse)
            {
                Assert.Equal(6, entry.Numerator.TermCount);
                Assert.True(entry.Denominator.Equals(det.Numerator) || entry.Denominator.Equals(-det.Numerator));
            }
        }

        [Fact]
        public void GenericFiveByFive_DeterminantHas120Terms()
        {
            var det = RationalMatrix.Determinant(Mat("a b c d f; g h j k m; n p q r s; t u v w x; y z A B C"));
            Assert.Equal(120, det.Numerator.TermCount);
        }

        [Fact]
        public void HilbertLikeMatrix_OfRationalFunctions_InvertsExactly()
        {
            var a = Mat("1/(x+1) 1/(x+2) 1/(x+3); 1/(x+2) 1/(x+3) 1/(x+4); 1/(x+3) 1/(x+4) 1/(x+5)");
            AssertSame(Identity(3), RationalMatrix.Multiply(a, RationalMatrix.Inverse(a)));
            // Cauchy determinant: 4 / ((x+1)(x+2)^2(x+3)^3(x+4)^2(x+5)).
            Assert.Equal(RationalFunction.Parse("4/((x+1)(x+2)^2(x+3)^3(x+4)^2(x+5))"), RationalMatrix.Determinant(a));
        }

        [Fact]
        public void SixBySix_Numeric_MatchesKnownDeterminant()
        {
            // Tridiagonal 2/-1: the determinant of the n x n one is n + 1.
            var a = Mat("2 -1 0 0 0 0; -1 2 -1 0 0 0; 0 -1 2 -1 0 0; 0 0 -1 2 -1 0; 0 0 0 -1 2 -1; 0 0 0 0 -1 2");
            Assert.Equal("7", RationalMatrix.Determinant(a).ToString());
            AssertSame(Identity(6), RationalMatrix.Multiply(a, RationalMatrix.Inverse(a)));
        }

        [Fact]
        public void SymbolicFourByFour_FinishesQuickly()
        {
            var a = Mat("a b c d; f g h j; k m n p; q r s t");
            var clock = Stopwatch.StartNew();
            RationalMatrix.Determinant(a);
            RationalMatrix.Rref(a);
            RationalMatrix.Inverse(a);
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(10), $"took {clock.Elapsed}");
        }
    }
}

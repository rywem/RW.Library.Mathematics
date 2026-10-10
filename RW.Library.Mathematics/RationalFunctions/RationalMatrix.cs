using System.Numerics;

namespace RW.Library.Mathematics.RationalFunctions
{
    public enum LinearSolutionKind { Unique, Parametric, Inconsistent }

    /// <summary>The solution set of a linear system.</summary>
    /// <param name="Kind">Exactly one solution, a family of them, or none.</param>
    /// <param name="Particular">One solution (free variables set to 0); null when inconsistent.</param>
    /// <param name="Homogeneous">
    /// A basis of the solutions of the homogeneous system, one vector per free variable. Every
    /// solution is <paramref name="Particular"/> plus a combination of these. Empty when unique.
    /// </param>
    /// <param name="FreeColumns">The unknown each basis vector belongs to (zero-based).</param>
    public sealed record LinearSolution(
        LinearSolutionKind Kind,
        RationalFunction[]? Particular,
        RationalFunction[][] Homogeneous,
        int[] FreeColumns);

    /// <summary>
    /// Exact linear algebra over the field of rational functions Q(x1..xn).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every method is one algorithm: fraction-free Gauss-Jordan elimination (Bareiss). Each row
    /// is first multiplied by the least common multiple of its denominators, leaving a matrix of
    /// polynomials. Elimination then stays inside the polynomial ring, where every division is
    /// exact and "is this pivot zero?" is "is this dictionary empty?". Fractions are formed
    /// only once, when the answer is written out.
    /// </para>
    /// <para>
    /// <b>Results are generic.</b> The variables are indeterminates, not numbers: the rank of
    /// <c>[x 1; 1 1]</c> is 2 because <c>x - 1</c> is not the zero polynomial, although the rank
    /// drops to 1 at the particular value x = 1. No case analysis on parameter values is done.
    /// </para>
    /// </remarks>
    public static class RationalMatrix
    {
        /// <summary>Parses a rectangular grid of expressions, naming the cell that fails.</summary>
        public static RationalFunction[,] Parse(string[][] cells)
        {
            if (cells is not { Length: > 0 } || cells[0] is not { Length: > 0 })
                throw new ArgumentException("The matrix is empty.");
            if (cells.Any(row => row is null || row.Length != cells[0].Length))
                throw new ArgumentException("Every row must be the same length.");

            var m = new RationalFunction[cells.Length, cells[0].Length];
            for (int r = 0; r < cells.Length; r++)
                for (int c = 0; c < cells[0].Length; c++)
                {
                    try { m[r, c] = RationalFunction.Parse(cells[r][c]); }
                    catch (RationalFunctionDomainException ex)
                    {
                        throw new RationalFunctionDomainException($"Row {r + 1}, column {c + 1}: {ex.Message}");
                    }
                }
            return m;
        }

        /// <summary>The reduced row echelon form, and the columns that hold its leading ones.</summary>
        public static RationalFunction[,] Rref(RationalFunction[,] matrix, out int[] pivotColumns)
        {
            var e = Eliminate(matrix);
            pivotColumns = e.PivotColumns;
            int rows = matrix.GetLength(0), cols = matrix.GetLength(1);
            var result = new RationalFunction[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    result[r, c] = r < e.Rank
                        ? RationalFunction.Create(e.Reduced[r, c], e.LastPivot)
                        : RationalFunction.Zero;
            return result;
        }

        public static RationalFunction[,] Rref(RationalFunction[,] matrix) => Rref(matrix, out _);

        public static int Rank(RationalFunction[,] matrix) => Eliminate(matrix).Rank;

        /// <summary>
        /// A basis of { v : A v = 0 }, one vector per free column; empty when only v = 0 works.
        /// </summary>
        public static RationalFunction[][] NullSpace(RationalFunction[,] matrix)
            => NullSpace(Rref(matrix, out var pivots), pivots, matrix.GetLength(1), out _);

        private static RationalFunction[][] NullSpace(
            RationalFunction[,] rref, int[] pivots, int unknowns, out int[] freeColumns)
        {
            var coefficientPivots = pivots.Where(p => p < unknowns).ToArray();
            freeColumns = Enumerable.Range(0, unknowns).Except(coefficientPivots).ToArray();
            var basis = new RationalFunction[freeColumns.Length][];
            for (int k = 0; k < freeColumns.Length; k++)
            {
                var v = Enumerable.Repeat(RationalFunction.Zero, unknowns).ToArray();
                v[freeColumns[k]] = RationalFunction.One;
                for (int r = 0; r < coefficientPivots.Length; r++)
                    v[coefficientPivots[r]] = -rref[r, freeColumns[k]];
                basis[k] = v;
            }
            return basis;
        }

        /// <summary>
        /// Solves the system whose augmented matrix is given: the last column is the right-hand
        /// side, every other column an unknown.
        /// </summary>
        public static LinearSolution Solve(RationalFunction[,] augmented)
        {
            int unknowns = augmented.GetLength(1) - 1;
            if (unknowns < 1)
                throw new ArgumentException("An augmented matrix needs at least two columns: the unknowns and the right-hand side.");

            var rref = Rref(augmented, out var pivots);
            var basis = NullSpace(rref, pivots, unknowns, out var free);

            // A leading one in the right-hand column is the row 0 = 1.
            if (pivots.Contains(unknowns))
                return new LinearSolution(LinearSolutionKind.Inconsistent, null, basis, free);

            var particular = Enumerable.Repeat(RationalFunction.Zero, unknowns).ToArray();
            for (int r = 0; r < pivots.Length; r++)
                particular[pivots[r]] = rref[r, unknowns];

            return new LinearSolution(
                free.Length == 0 ? LinearSolutionKind.Unique : LinearSolutionKind.Parametric,
                particular, basis, free);
        }

        public static RationalFunction Determinant(RationalFunction[,] matrix)
        {
            int n = RequireSquare(matrix, "A determinant");
            var e = Eliminate(matrix);
            if (e.Rank < n) return RationalFunction.Zero;
            // With full rank the last Bareiss pivot is the determinant of the row-scaled,
            // row-swapped polynomial matrix.
            // Divide by the cleared denominators one small factor at a time: each step is a gcd
            // of something large with something small, never large with large.
            var det = RationalFunction.FromPolynomial(e.Sign < 0 ? -e.LastPivot : e.LastPivot);
            foreach (var factor in e.RowScale) det /= RationalFunction.FromPolynomial(factor);
            return det;
        }

        /// <exception cref="InvalidOperationException">The matrix is singular.</exception>
        public static RationalFunction[,] Inverse(RationalFunction[,] matrix)
        {
            int n = RequireSquare(matrix, "An inverse");
            var augmented = new RationalFunction[n, 2 * n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                {
                    augmented[r, c] = matrix[r, c];
                    augmented[r, n + c] = r == c ? RationalFunction.One : RationalFunction.Zero;
                }

            var rref = Rref(augmented, out var pivots);
            if (pivots.Length < n || pivots[n - 1] != n - 1)
                throw new InvalidOperationException("The determinant is 0, so the matrix has no inverse.");

            var inverse = new RationalFunction[n, n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    inverse[r, c] = rref[r, n + c];
            return inverse;
        }

        public static RationalFunction[,] Multiply(RationalFunction[,] a, RationalFunction[,] b)
        {
            if (a.GetLength(1) != b.GetLength(0))
                throw new ArgumentException("The column count of the first matrix must equal the row count of the second.");
            var product = new RationalFunction[a.GetLength(0), b.GetLength(1)];
            for (int r = 0; r < a.GetLength(0); r++)
                for (int c = 0; c < b.GetLength(1); c++)
                {
                    var sum = RationalFunction.Zero;
                    for (int k = 0; k < b.GetLength(0); k++) sum += a[r, k] * b[k, c];
                    product[r, c] = sum;
                }
            return product;
        }

        private static int RequireSquare(RationalFunction[,] m, string what)
            => m.GetLength(0) == m.GetLength(1)
                ? m.GetLength(0)
                : throw new ArgumentException($"{what} needs a square matrix; this one is {m.GetLength(0)}×{m.GetLength(1)}.");

        // ------------------------------------------------------------------ the one algorithm

        /// <param name="Reduced">Polynomial matrix; the first Rank rows, each divided by LastPivot, are the RREF.</param>
        /// <param name="LastPivot">The common value on every pivot position (1 when the rank is 0).</param>
        /// <param name="Sign">+1 or -1 from row swaps.</param>
        /// <param name="RowScale">The factors whose product cleared the denominators; the determinant was multiplied by all of them.</param>
        private sealed record Elimination(
            MultiPolynomial[,] Reduced, int[] PivotColumns, MultiPolynomial LastPivot, int Sign, List<MultiPolynomial> RowScale)
        {
            public int Rank => PivotColumns.Length;
        }

        private static Elimination Eliminate(RationalFunction[,] matrix)
        {
            int rows = matrix.GetLength(0), cols = matrix.GetLength(1);
            var m = new MultiPolynomial[rows, cols];
            var rowScale = new List<MultiPolynomial>();

            // Clear denominators: multiply each row by the lcm of its denominators.
            for (int r = 0; r < rows; r++)
            {
                var lcm = MultiPolynomial.One;
                for (int c = 0; c < cols; c++)
                {
                    var d = matrix[r, c].Denominator;
                    if (d.IsOne) continue;
                    var extra = d.DivideExact(MultiPolynomial.Gcd(lcm, d));
                    if (extra.IsOne) continue;
                    lcm *= extra;
                    rowScale.Add(extra);
                }
                for (int c = 0; c < cols; c++)
                    m[r, c] = matrix[r, c].Numerator * lcm.DivideExact(matrix[r, c].Denominator);
            }

            // Bareiss, Gauss-Jordan variant. After a pivot in row k, every entry is a minor of
            // the polynomial matrix, so the division by the previous pivot is always exact.
            var pivots = new List<int>();
            var previous = MultiPolynomial.One;
            int sign = 1;
            for (int col = 0, row = 0; col < cols && row < rows; col++)
            {
                // Any non-zero entry is a valid pivot; the one with fewest terms keeps things small.
                int best = -1;
                for (int r = row; r < rows; r++)
                    if (!m[r, col].IsZero && (best < 0 || m[r, col].TermCount < m[best, col].TermCount))
                        best = r;
                if (best < 0) continue;

                if (best != row)
                {
                    for (int c = 0; c < cols; c++) (m[row, c], m[best, c]) = (m[best, c], m[row, c]);
                    sign = -sign;
                }

                var pivot = m[row, col];
                for (int r = 0; r < rows; r++)
                {
                    if (r == row) continue;
                    var factor = m[r, col];
                    for (int c = 0; c < cols; c++)
                        m[r, c] = (pivot * m[r, c] - factor * m[row, c]).DivideExact(previous);
                }

                previous = pivot;
                pivots.Add(col);
                row++;
            }

            return new Elimination(m, pivots.ToArray(), previous, sign, rowScale);
        }
    }
}

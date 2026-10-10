using System.Numerics;
using System.Text;
using RW.Library.Mathematics.Polynomials;

namespace RW.Library.Mathematics.RationalFunctions
{
    /// <summary>
    /// Immutable polynomial in any number of single-letter variables with integer coefficients.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The existing <see cref="Polynomial"/> could not be reused: its coefficients are
    /// <see cref="int"/> (they overflow in elimination) and it has no subtraction or division.
    /// </para>
    /// <para>
    /// <b>Canonical form.</b> A dictionary from monomial to non-zero coefficient. A monomial is
    /// the sorted string of its letters with multiplicity (<c>x^2*y</c> is <c>"xxy"</c>), so two
    /// polynomials are equal exactly when their dictionaries are equal, and a polynomial is zero
    /// exactly when its dictionary is empty. That is the whole zero test; nothing is simplified
    /// heuristically.
    /// </para>
    /// <para>
    /// Integer rather than rational coefficients on purpose: a rational function over Q always
    /// has a representation with integer polynomials, and Z[x1..xn] is a unique factorisation
    /// domain whose only units are 1 and -1, which makes the reduced form unique.
    /// </para>
    /// </remarks>
    public sealed class MultiPolynomial : IEquatable<MultiPolynomial>
    {
        /// <summary>
        /// Ceiling on the number of terms in any one polynomial. Dense multivariate products
        /// grow factorially; past this the answer would be unreadable and the request unbounded.
        /// </summary>
        // ponytail: fixed ceiling instead of a cancellation token threaded through the
        // arithmetic. Make it a parameter if a caller ever needs a different budget.
        public const int MaxTerms = 100_000;

        private readonly Dictionary<string, BigInteger> _terms;
        private string? _leading;

        public static readonly MultiPolynomial Zero = new(new Dictionary<string, BigInteger>());
        public static readonly MultiPolynomial One = Constant(BigInteger.One);

        private MultiPolynomial(Dictionary<string, BigInteger> terms)
        {
            if (terms.Count > MaxTerms)
                throw new RationalFunctionTooLargeException(
                    $"The computation produced a polynomial with more than {MaxTerms:N0} terms and was stopped.");
            _terms = terms;
        }

        public static MultiPolynomial Constant(BigInteger value)
        {
            var terms = new Dictionary<string, BigInteger>();
            if (!value.IsZero) terms[""] = value;
            return new MultiPolynomial(terms);
        }

        public static MultiPolynomial Variable(char name)
            => new(new Dictionary<string, BigInteger> { [name.ToString()] = BigInteger.One });

        public bool IsZero => _terms.Count == 0;
        public bool IsConstant => _terms.Count == 0 || (_terms.Count == 1 && _terms.ContainsKey(""));
        public bool IsOne => _terms.Count == 1 && _terms.TryGetValue("", out var c) && c.IsOne;
        public int TermCount => _terms.Count;

        /// <summary>The constant term (0 when there is none).</summary>
        public BigInteger ConstantTerm => _terms.TryGetValue("", out var c) ? c : BigInteger.Zero;

        /// <summary>The letters that actually occur, in order.</summary>
        public IEnumerable<char> Variables => _terms.Keys.SelectMany(k => k).Distinct().OrderBy(c => c);

        // ------------------------------------------------------------------ monomials

        /// <summary>Product of two monomials: a merge of two sorted strings.</summary>
        private static string Mul(string a, string b)
        {
            if (a.Length == 0) return b;
            if (b.Length == 0) return a;
            var sb = new StringBuilder(a.Length + b.Length);
            int i = 0, j = 0;
            while (i < a.Length && j < b.Length) sb.Append(a[i] <= b[j] ? a[i++] : b[j++]);
            return sb.Append(a, i, a.Length - i).Append(b, j, b.Length - j).ToString();
        }

        /// <summary>Quotient a / b of monomials, or null when b does not divide a.</summary>
        private static string? Div(string a, string b)
        {
            if (b.Length > a.Length) return null;
            var sb = new StringBuilder(a.Length - b.Length);
            int j = 0;
            foreach (char ch in a)
            {
                if (j < b.Length && b[j] == ch) j++;
                else sb.Append(ch);
            }
            return j == b.Length ? sb.ToString() : null;
        }

        /// <summary>
        /// Graded lexicographic order with a &gt; b &gt; c ...: higher total degree first, then
        /// alphabetical. It is a genuine monomial order (compatible with multiplication), which
        /// exact division and the sign normalisation both rely on.
        /// </summary>
        private static int Compare(string a, string b)
            => a.Length != b.Length ? a.Length.CompareTo(b.Length) : string.CompareOrdinal(b, a);

        private static readonly Comparer<string> LeadingFirst = Comparer<string>.Create((a, b) => Compare(b, a));

        private string LeadingMonomial
        {
            get
            {
                if (_leading is null)
                {
                    string? best = null;
                    foreach (var key in _terms.Keys)
                        if (best is null || Compare(key, best) > 0) best = key;
                    _leading = best ?? "";
                }
                return _leading;
            }
        }

        /// <summary>Coefficient of the leading term in graded-lex order (0 for the zero polynomial).</summary>
        public BigInteger LeadingCoefficient => IsZero ? BigInteger.Zero : _terms[LeadingMonomial];

        // ------------------------------------------------------------------ ring operations

        public static MultiPolynomial operator +(MultiPolynomial a, MultiPolynomial b) => Combine(a, b, 1);
        public static MultiPolynomial operator -(MultiPolynomial a, MultiPolynomial b) => Combine(a, b, -1);
        public static MultiPolynomial operator -(MultiPolynomial a) => a.Scale(BigInteger.MinusOne);

        private static MultiPolynomial Combine(MultiPolynomial a, MultiPolynomial b, int sign)
        {
            if (b.IsZero) return a;
            var result = new Dictionary<string, BigInteger>(a._terms);
            foreach (var (mono, coeff) in b._terms) Accumulate(result, mono, sign * coeff);
            return new MultiPolynomial(result);
        }

        private static void Accumulate(Dictionary<string, BigInteger> into, string mono, BigInteger coeff)
        {
            if (into.TryGetValue(mono, out var existing))
            {
                coeff += existing;
                if (coeff.IsZero) { into.Remove(mono); return; }
            }
            into[mono] = coeff;
        }

        public static MultiPolynomial operator *(MultiPolynomial a, MultiPolynomial b)
        {
            if (a.IsZero || b.IsZero) return Zero;
            if (a.IsOne) return b;
            if (b.IsOne) return a;
            var result = new Dictionary<string, BigInteger>();
            foreach (var (ma, ca) in a._terms)
            {
                foreach (var (mb, cb) in b._terms) Accumulate(result, Mul(ma, mb), ca * cb);
                if (result.Count > MaxTerms) return new MultiPolynomial(result); // throws
            }
            return new MultiPolynomial(result);
        }

        public MultiPolynomial Scale(BigInteger factor)
        {
            if (factor.IsZero) return Zero;
            if (factor.IsOne) return this;
            return new MultiPolynomial(_terms.ToDictionary(t => t.Key, t => t.Value * factor));
        }

        public MultiPolynomial Pow(int exponent)
        {
            if (exponent < 0) throw new ArgumentOutOfRangeException(nameof(exponent));
            MultiPolynomial result = One, square = this;
            for (int e = exponent; e > 0; e >>= 1)
            {
                if ((e & 1) == 1) result *= square;
                if (e > 1) square *= square;
            }
            return result;
        }

        /// <summary>
        /// Exact division: true with the quotient when <paramref name="divisor"/> divides this
        /// polynomial in Z[x1..xn], false otherwise. No remainder is ever discarded.
        /// </summary>
        public bool TryDivideExact(MultiPolynomial divisor, out MultiPolynomial quotient)
        {
            quotient = Zero;
            if (divisor.IsZero) throw new DivideByZeroException();
            if (IsZero) return true;
            if (divisor.IsOne) { quotient = this; return true; }

            string leadMono = divisor.LeadingMonomial;
            BigInteger leadCoeff = divisor._terms[leadMono];

            if (divisor._terms.Count == 1)
            {
                // Monomial divisor: every term must divide on its own.
                var direct = new Dictionary<string, BigInteger>(_terms.Count);
                foreach (var (mono, coeff) in _terms)
                {
                    var q = Div(mono, leadMono);
                    if (q is null || !(coeff % leadCoeff).IsZero) return false;
                    direct[q] = coeff / leadCoeff;
                }
                quotient = new MultiPolynomial(direct);
                return true;
            }

            // Under a monomial order, if d divides p then lead(d) divides lead(p) at every step.
            // Sorted leading-first, so each step reads the top term in O(log n).
            var remainder = new SortedDictionary<string, BigInteger>(_terms, LeadingFirst);
            var result = new Dictionary<string, BigInteger>();
            while (remainder.Count > 0)
            {
                var (top, topCoeff) = remainder.First();
                var qMono = Div(top, leadMono);
                if (qMono is null || !(topCoeff % leadCoeff).IsZero) return false;

                BigInteger qCoeff = topCoeff / leadCoeff;
                result[qMono] = qCoeff;
                foreach (var (mono, coeff) in divisor._terms)
                {
                    string key = Mul(mono, qMono);
                    BigInteger updated = (remainder.TryGetValue(key, out var existing) ? existing : BigInteger.Zero) - qCoeff * coeff;
                    if (updated.IsZero) remainder.Remove(key); else remainder[key] = updated;
                }
            }
            quotient = new MultiPolynomial(result);
            return true;
        }

        /// <summary>Exact quotient; throws if the division is not exact (an internal invariant).</summary>
        public MultiPolynomial DivideExact(MultiPolynomial divisor)
            => TryDivideExact(divisor, out var q)
                ? q
                : throw new InvalidOperationException("Internal error: a division that must be exact was not.");

        // ------------------------------------------------------------------ greatest common divisor

        /// <summary>Greatest common divisor of all the integer coefficients (0 for zero).</summary>
        public BigInteger Content
        {
            get
            {
                BigInteger g = BigInteger.Zero;
                foreach (var c in _terms.Values)
                {
                    g = BigInteger.GreatestCommonDivisor(g, c);
                    if (g.IsOne) break;
                }
                return g;
            }
        }

        /// <summary>
        /// Greatest common divisor in Z[x1..xn], with a positive leading coefficient. Exact:
        /// integer content, then a recursive primitive polynomial remainder sequence, one
        /// variable at a time. Not fast on large dense inputs, but never approximate.
        /// </summary>
        public static MultiPolynomial Gcd(MultiPolynomial a, MultiPolynomial b)
        {
            if (a.IsZero) return b.LeadingCoefficient.Sign < 0 ? -b : b;
            if (b.IsZero) return a.LeadingCoefficient.Sign < 0 ? -a : a;

            BigInteger ca = a.Content, cb = b.Content;
            var primitiveA = a.DivideByInteger(ca * a.LeadingCoefficient.Sign);
            var primitiveB = b.DivideByInteger(cb * b.LeadingCoefficient.Sign);
            return GcdPrimitive(primitiveA, primitiveB).Scale(BigInteger.GreatestCommonDivisor(ca, cb));
        }

        private MultiPolynomial DivideByInteger(BigInteger divisor)
            => divisor.IsOne ? this : new MultiPolynomial(_terms.ToDictionary(t => t.Key, t => t.Value / divisor));

        /// <summary>Both arguments non-zero, integer-primitive, positive leading coefficient.</summary>
        private static MultiPolynomial GcdPrimitive(MultiPolynomial a, MultiPolynomial b)
        {
            if (a.IsConstant || b.IsConstant) return One;
            if (a.Equals(b)) return a;

            var varsA = a.Variables.ToList();
            var varsB = b.Variables.ToList();
            if (ProvablyCoprime(a, b, varsA.Intersect(varsB))) return One;

            // Cheap and very common in elimination: one is a multiple of the other.
            if (a.TryDivideExact(b, out _)) return b;
            if (b.TryDivideExact(a, out _)) return a;

            // A variable present in only one of them cannot be in the gcd, so the gcd divides
            // every coefficient of that polynomial viewed in that variable. Folding from the
            // other polynomial keeps the running gcd no larger than it.
            foreach (char v in varsA.Except(varsB)) return GcdWithCoefficients(b, a, v);
            foreach (char v in varsB.Except(varsA)) return GcdWithCoefficients(a, b, v);

            // Same variables: remainder sequence in the variable of lowest degree.
            char x = varsA.OrderBy(v => Math.Min(a.Degree(v), b.Degree(v))).First();
            var contentA = a.ContentIn(x);
            var contentB = b.ContentIn(x);
            var p = a.DivideExact(contentA);
            var q = b.DivideExact(contentB);
            if (p.Degree(x) < q.Degree(x)) (p, q) = (q, p);

            MultiPolynomial inX;
            while (true)
            {
                var r = PseudoRemainder(p, q, x);
                if (r.IsZero) { inX = q; break; }
                if (r.Degree(x) == 0) { inX = One; break; }
                p = q;
                q = r.DivideExact(r.ContentIn(x));
            }
            if (inX.LeadingCoefficient.Sign < 0) inX = -inX;
            return Gcd(contentA, contentB) * inX;
        }

        private int Degree(char variable)
        {
            int max = 0;
            foreach (var key in _terms.Keys)
            {
                int n = 0;
                foreach (char c in key) if (c == variable) n++;
                if (n > max) max = n;
            }
            return max;
        }

        /// <summary>This polynomial as a polynomial in one variable: power to coefficient.</summary>
        private Dictionary<int, MultiPolynomial> CoefficientsIn(char variable)
        {
            var groups = new Dictionary<int, Dictionary<string, BigInteger>>();
            string name = variable.ToString();
            foreach (var (mono, coeff) in _terms)
            {
                string rest = mono.Replace(name, "");
                int power = mono.Length - rest.Length;
                if (!groups.TryGetValue(power, out var group)) groups[power] = group = new();
                group[rest] = coeff;
            }
            return groups.ToDictionary(g => g.Key, g => new MultiPolynomial(g.Value));
        }

        /// <summary>The gcd of the coefficients when viewed as a polynomial in one variable.</summary>
        private MultiPolynomial ContentIn(char variable) => GcdWithCoefficients(Zero, this, variable);

        /// <summary>gcd of <paramref name="start"/> and every coefficient of <paramref name="p"/> in one variable.</summary>
        private static MultiPolynomial GcdWithCoefficients(MultiPolynomial start, MultiPolynomial p, char variable)
        {
            var g = start;
            foreach (var coefficient in p.CoefficientsIn(variable).Values.OrderBy(c => c.TermCount))
            {
                g = Gcd(g, coefficient);
                if (g.IsOne) break;
            }
            return g;
        }

        /// <summary>
        /// A proof, not a guess, that two polynomials share no factor. Replace every variable
        /// but x by an integer: if the leading coefficient in x survives, the gcd keeps its
        /// degree in x and still divides both images, so the degree of the images' gcd bounds
        /// it from above. A bound of 0 for every shared variable leaves only a constant. A
        /// "false" answer proves nothing (the point may be unlucky) and the caller carries on
        /// with the full algorithm.
        /// </summary>
        private static bool ProvablyCoprime(MultiPolynomial a, MultiPolynomial b, IEnumerable<char> sharedVariables)
        {
            foreach (char x in sharedVariables)
            {
                bool bounded = false;
                for (int attempt = 0; attempt < 3 && !bounded; attempt++)
                {
                    var imageA = a.ImageIn(x, attempt);
                    var imageB = b.ImageIn(x, attempt);
                    if (imageA.Length - 1 != a.Degree(x) || imageB.Length == 0) continue; // leading coefficient vanished
                    bounded = UnivariateGcdDegree(imageA, imageB) == 0;
                }
                if (!bounded) return false;
            }
            return true;
        }

        /// <summary>Coefficients by power of x after every other variable is set to a fixed integer.</summary>
        private BigInteger[] ImageIn(char x, int attempt)
        {
            var image = new BigInteger[Degree(x) + 1];
            foreach (var (mono, coeff) in _terms)
            {
                BigInteger value = coeff;
                int power = 0;
                foreach (char v in mono)
                {
                    if (v == x) power++;
                    else value *= 1009 + 37 * v + 7919 * attempt;
                }
                image[power] += value;
            }
            return Trim(image);
        }

        private static BigInteger[] Trim(BigInteger[] p)
        {
            int length = p.Length;
            while (length > 0 && p[length - 1].IsZero) length--;
            return length == p.Length ? p : p[..length];
        }

        /// <summary>Degree of the gcd of two non-zero integer polynomials in one variable (Euclid, no fractions).</summary>
        private static int UnivariateGcdDegree(BigInteger[] a, BigInteger[] b)
        {
            if (a.Length < b.Length) (a, b) = (b, a);
            while (b.Length > 0)
            {
                var r = (BigInteger[])a.Clone();
                while (r.Length >= b.Length)
                {
                    BigInteger leadR = r[^1], leadB = b[^1];
                    int shift = r.Length - b.Length;
                    for (int k = 0; k < r.Length; k++)
                        r[k] = leadB * r[k] - (k >= shift ? leadR * b[k - shift] : BigInteger.Zero);
                    r = Trim(r);
                    if (r.Length == 0) break;
                }
                BigInteger content = BigInteger.Zero;
                foreach (var c in r) content = BigInteger.GreatestCommonDivisor(content, c);
                for (int k = 0; k < r.Length; k++) r[k] /= content;
                (a, b) = (b, r);
            }
            return a.Length - 1;
        }

        private MultiPolynomial ShiftBy(char variable, int power)
        {
            if (power == 0) return this;
            string shift = new string(variable, power);
            return new MultiPolynomial(_terms.ToDictionary(t => Mul(t.Key, shift), t => t.Value));
        }

        /// <summary>Remainder of lead(b)^k * a by b in one variable; no fractions appear.</summary>
        private static MultiPolynomial PseudoRemainder(MultiPolynomial a, MultiPolynomial b, char x)
        {
            int degreeB = b.Degree(x);
            var leadB = b.CoefficientsIn(x)[degreeB];
            while (!a.IsZero)
            {
                int degreeA = a.Degree(x);
                if (degreeA < degreeB) break;
                var leadA = a.CoefficientsIn(x)[degreeA];
                a = leadB * a - (leadA * b).ShiftBy(x, degreeA - degreeB);
            }
            return a;
        }

        // ------------------------------------------------------------------ evaluation, equality, text

        /// <summary>Exact value when every variable is replaced by a rational number.</summary>
        public Fraction Evaluate(IReadOnlyDictionary<char, Fraction> values)
        {
            var sum = Fraction.Zero;
            foreach (var (mono, coeff) in _terms)
            {
                var term = new Fraction(coeff, 1);
                foreach (char v in mono)
                    term *= values.TryGetValue(v, out var value)
                        ? value
                        : throw new ArgumentException($"No value was given for '{v}'.");
                sum += term;
            }
            return sum;
        }

        public bool Equals(MultiPolynomial? other)
        {
            if (other is null || other._terms.Count != _terms.Count) return false;
            foreach (var (mono, coeff) in _terms)
                if (!other._terms.TryGetValue(mono, out var theirs) || theirs != coeff) return false;
            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as MultiPolynomial);

        public override int GetHashCode()
        {
            int hash = 0; // order-independent: the dictionary has no defined order
            foreach (var (mono, coeff) in _terms) hash ^= HashCode.Combine(mono, coeff);
            return hash;
        }

        /// <summary>Terms from the leading one down, e.g. <c>x^2 - 2*x*y + 3</c>.</summary>
        public override string ToString()
        {
            if (IsZero) return "0";
            var sb = new StringBuilder();
            foreach (var (mono, coeff) in _terms.OrderBy(t => t.Key, LeadingFirst))
            {
                if (sb.Length > 0) sb.Append(coeff.Sign < 0 ? " - " : " + ");
                else if (coeff.Sign < 0) sb.Append('-');

                var magnitude = BigInteger.Abs(coeff);
                if (mono.Length == 0) { sb.Append(magnitude); continue; }
                if (!magnitude.IsOne) sb.Append(magnitude).Append('*');

                for (int i = 0; i < mono.Length;)
                {
                    int run = 1;
                    while (i + run < mono.Length && mono[i + run] == mono[i]) run++;
                    if (i > 0) sb.Append('*');
                    sb.Append(mono[i]);
                    if (run > 1) sb.Append('^').Append(run);
                    i += run;
                }
            }
            return sb.ToString();
        }
    }
}

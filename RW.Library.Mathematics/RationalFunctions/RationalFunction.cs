using System.Numerics;
using RW.Library.Mathematics.Polynomials;

namespace RW.Library.Mathematics.RationalFunctions
{
    /// <summary>
    /// The input is not a rational function: it uses something other than numbers, single-letter
    /// variables, <c>+ - * /</c>, integer powers and parentheses.
    /// </summary>
    public sealed class RationalFunctionDomainException : FormatException
    {
        public RationalFunctionDomainException(string message) : base(message) { }
    }

    /// <summary>The computation was stopped because an intermediate polynomial grew too large.</summary>
    public sealed class RationalFunctionTooLargeException : InvalidOperationException
    {
        public RationalFunctionTooLargeException(string message) : base(message) { }
    }

    /// <summary>
    /// An element of the field Q(x1..xn): a quotient of two multivariate polynomials.
    /// </summary>
    /// <remarks>
    /// <b>Canonical form.</b> Numerator and denominator are integer polynomials with no common
    /// factor (not even an integer one), and the denominator's leading coefficient is positive.
    /// Zero is <c>0/1</c>. Because Z[x1..xn] has unique factorisation, every rational function has
    /// exactly one such form, so equality is structural and <see cref="IsZero"/> is exact.
    /// </remarks>
    public sealed class RationalFunction : IEquatable<RationalFunction>
    {
        /// <summary>Largest exponent the parser accepts; anything bigger is a request to hang.</summary>
        public const int MaxExponent = 1000;

        public MultiPolynomial Numerator { get; }
        public MultiPolynomial Denominator { get; }

        public static readonly RationalFunction Zero = new(MultiPolynomial.Zero, MultiPolynomial.One);
        public static readonly RationalFunction One = new(MultiPolynomial.One, MultiPolynomial.One);

        /// <summary>Trusted: the caller guarantees the pair is already in canonical form.</summary>
        private RationalFunction(MultiPolynomial numerator, MultiPolynomial denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        /// <summary>Builds the canonical form of numerator / denominator.</summary>
        public static RationalFunction Create(MultiPolynomial numerator, MultiPolynomial denominator)
        {
            if (denominator.IsZero) throw new DivideByZeroException("Division by zero.");
            if (numerator.IsZero) return Zero;

            var g = MultiPolynomial.Gcd(numerator, denominator);
            if (denominator.LeadingCoefficient.Sign < 0) g = -g;
            return new RationalFunction(numerator.DivideExact(g), denominator.DivideExact(g));
        }

        public static RationalFunction FromPolynomial(MultiPolynomial p) => new(p, MultiPolynomial.One);
        public static implicit operator RationalFunction(int value) => FromPolynomial(MultiPolynomial.Constant(value));

        public bool IsZero => Numerator.IsZero;
        public bool IsPolynomial => Denominator.IsOne;

        // The arithmetic below never multiplies out and then reduces. It takes the small gcds
        // first (Henrici's method), which gives the same canonical result for far less work.

        public static RationalFunction operator +(RationalFunction a, RationalFunction b)
        {
            if (a.IsZero) return b;
            if (b.IsZero) return a;
            if (a.Denominator.Equals(b.Denominator))
                return Create(a.Numerator + b.Numerator, a.Denominator);

            var g = MultiPolynomial.Gcd(a.Denominator, b.Denominator);
            if (g.IsOne)
            {
                var sum = a.Numerator * b.Denominator + b.Numerator * a.Denominator;
                return sum.IsZero ? Zero : new RationalFunction(sum, a.Denominator * b.Denominator);
            }

            var da = a.Denominator.DivideExact(g);
            var db = b.Denominator.DivideExact(g);
            var t = a.Numerator * db + b.Numerator * da;
            if (t.IsZero) return Zero;
            var g2 = MultiPolynomial.Gcd(t, g);
            return new RationalFunction(t.DivideExact(g2), da * b.Denominator.DivideExact(g2));
        }

        public static RationalFunction operator -(RationalFunction a) => new(-a.Numerator, a.Denominator);
        public static RationalFunction operator -(RationalFunction a, RationalFunction b) => a + (-b);

        public static RationalFunction operator *(RationalFunction a, RationalFunction b)
        {
            if (a.IsZero || b.IsZero) return Zero;
            var g1 = MultiPolynomial.Gcd(a.Numerator, b.Denominator);
            var g2 = MultiPolynomial.Gcd(b.Numerator, a.Denominator);
            return new RationalFunction(
                a.Numerator.DivideExact(g1) * b.Numerator.DivideExact(g2),
                a.Denominator.DivideExact(g2) * b.Denominator.DivideExact(g1));
        }

        public RationalFunction Reciprocal()
        {
            if (IsZero) throw new DivideByZeroException("Division by zero.");
            return Numerator.LeadingCoefficient.Sign < 0
                ? new RationalFunction(-Denominator, -Numerator)
                : new RationalFunction(Denominator, Numerator);
        }

        public static RationalFunction operator /(RationalFunction a, RationalFunction b) => a * b.Reciprocal();

        public RationalFunction Pow(int exponent)
        {
            // Powers of a reduced fraction are already reduced.
            var magnitude = exponent == int.MinValue ? throw new ArgumentOutOfRangeException(nameof(exponent)) : Math.Abs(exponent);
            var power = new RationalFunction(Numerator.Pow(magnitude), Denominator.Pow(magnitude));
            return exponent < 0 ? power.Reciprocal() : power;
        }

        /// <summary>Exact value at a rational point; throws if the denominator vanishes there.</summary>
        public Fraction Evaluate(IReadOnlyDictionary<char, Fraction> values)
            => Numerator.Evaluate(values) / Denominator.Evaluate(values);

        public bool Equals(RationalFunction? other)
            => other is not null && Numerator.Equals(other.Numerator) && Denominator.Equals(other.Denominator);

        public override bool Equals(object? obj) => Equals(obj as RationalFunction);
        public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

        /// <summary>
        /// Text that <see cref="Parse"/> reads back to the same value, e.g. <c>(x + 1)/(x - 1)</c>.
        /// </summary>
        public override string ToString()
        {
            string n = Numerator.ToString();
            if (Denominator.IsOne) return n;
            static string Wrap(string s) => s.All(c => char.IsLetterOrDigit(c) || c == '^') ? s : $"({s})";
            return $"{Wrap(n)}/{Wrap(Denominator.ToString())}";
        }

        // ------------------------------------------------------------------ parsing

        /// <summary>
        /// Parses an expression built from numbers (integers or decimals), single-letter
        /// variables, <c>+ - * /</c>, <c>^</c> with an integer exponent, and parentheses.
        /// A number may be written directly before a variable or a parenthesis (<c>2x</c>,
        /// <c>3(x+1)</c>, <c>(x+1)(x-1)</c>).
        /// </summary>
        /// <exception cref="RationalFunctionDomainException">
        /// Anything else: function calls, names longer than one letter, the imaginary unit,
        /// non-integer or symbolic exponents, division by zero, malformed text.
        /// </exception>
        public static RationalFunction Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new RationalFunctionDomainException("An entry is empty.");
            try
            {
                return new Parser(text).ParseAll();
            }
            catch (DivideByZeroException)
            {
                throw new RationalFunctionDomainException($"'{text.Trim()}' divides by zero.");
            }
        }

        private sealed class Parser
        {
            private readonly string _source;
            private readonly string _s;
            private int _i;

            public Parser(string text)
            {
                _source = text.Trim();
                _s = string.Concat(text.Where(c => !char.IsWhiteSpace(c)))
                    .Replace('−', '-').Replace('·', '*').Replace('×', '*');
            }

            private RationalFunctionDomainException Reject(string why)
                => new($"'{_source}' is not a rational function: {why}. Only numbers, single-letter variables, " +
                       "+ - * /, integer powers and parentheses are supported.");

            private char Peek => _i < _s.Length ? _s[_i] : '\0';

            public RationalFunction ParseAll()
            {
                var value = Sum();
                if (_i < _s.Length)
                    throw Reject(Peek == ')' ? "unbalanced ')'" : $"unexpected '{Peek}'");
                return value;
            }

            private RationalFunction Sum()
            {
                var value = Product();
                while (Peek is '+' or '-')
                    value = _s[_i++] == '+' ? value + Product() : value - Product();
                return value;
            }

            private RationalFunction Product()
            {
                var value = Signed();
                while (true)
                {
                    if (Peek == '*') { _i++; value *= Signed(); }
                    else if (Peek == '/') { _i++; value /= Signed(); }
                    // Implicit multiplication: 2x, 2(x+1), (x+1)(x-1), (x+1)y.
                    else if (Peek == '(' || char.IsAsciiLetter(Peek) || char.IsAsciiDigit(Peek)) value *= Power();
                    else return value;
                }
            }

            private RationalFunction Signed()
            {
                if (Peek == '-') { _i++; return -Signed(); }
                if (Peek == '+') { _i++; return Signed(); }
                return Power();
            }

            private RationalFunction Power()
            {
                var value = Atom();
                if (Peek != '^') return value;
                _i++;

                // Right-associative, and the exponent may carry its own sign: x^-2, x^2^3.
                var exponent = Signed();
                if (!exponent.Numerator.IsConstant || !exponent.Denominator.IsOne)
                    throw Reject($"the exponent '{exponent}' is not an integer");
                BigInteger n = exponent.Numerator.ConstantTerm;
                if (BigInteger.Abs(n) > MaxExponent)
                    throw Reject($"the exponent {n} is larger than the supported maximum of {MaxExponent}");
                if (n.IsZero && value.IsZero)
                    throw Reject("0^0 is undefined");
                return value.Pow((int)n);
            }

            private RationalFunction Atom()
            {
                char c = Peek;
                if (c == '(')
                {
                    _i++;
                    var inner = Sum();
                    if (Peek != ')') throw Reject("a '(' is never closed");
                    _i++;
                    return inner;
                }

                if (char.IsAsciiDigit(c) || c == '.')
                {
                    int start = _i;
                    while (char.IsAsciiDigit(Peek)) _i++;
                    int scale = 0;
                    if (Peek == '.')
                    {
                        _i++;
                        int fractionStart = _i;
                        while (char.IsAsciiDigit(Peek)) _i++;
                        scale = _i - fractionStart;
                    }
                    string digits = _s[start.._i].Replace(".", "");
                    if (digits.Length == 0) throw Reject("a '.' stands alone");
                    // A decimal is an exact rational: 0.25 is 25/100.
                    return Create(MultiPolynomial.Constant(BigInteger.Parse(digits)),
                                  MultiPolynomial.Constant(BigInteger.Pow(10, scale)));
                }

                if (char.IsAsciiLetter(c))
                {
                    int start = _i;
                    while (char.IsAsciiLetter(Peek)) _i++;
                    string name = _s[start.._i];

                    if (name.Length > 1)
                        throw Reject($"'{name}' is a function or a multi-letter name (write a product as {string.Join('*', name.ToCharArray())})");
                    if (name == "i")
                        throw Reject("'i' is the imaginary unit, which is not an independent variable");
                    if (Peek == '(')
                        throw Reject($"'{name}(' looks like a function call (write {name}*(...) for a product)");
                    if (char.IsAsciiDigit(Peek) || Peek == '.')
                        throw Reject($"a number follows '{name}' directly (write {name}*{Peek}... or {name}^{Peek}...)");
                    return FromPolynomial(MultiPolynomial.Variable(name[0]));
                }

                throw Reject(c == '\0' ? "the expression ends early" : $"unexpected '{c}'");
            }
        }
    }
}

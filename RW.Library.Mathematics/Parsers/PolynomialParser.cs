using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RW.Library.Mathematics.Polynomials;

// NOTE: Declared in the Polynomials namespace so that consumers who already
// `using RW.Library.Mathematics.Polynomials;` (including the test-suite) can reach
// the parser alongside Polynomial / Term without an extra import.
namespace RW.Library.Mathematics.Polynomials
{
    /// <summary>
    /// Parses a flat polynomial expression (a sum/difference of monomials) into a
    /// <see cref="Polynomial"/>. Supports implicit multiplication ("2xy^2"), explicit
    /// asterisks ("3*x*y^2"), unicode minus signs and arbitrary whitespace.
    /// Examples: "x^2-3x+4", "2xy^2 - y + 2x + 4", "3*x*y^2 - 5".
    /// </summary>
    public static class PolynomialParser
    {
        /// <summary>
        /// Parse a polynomial expression into a <see cref="Polynomial"/> with like terms combined.
        /// </summary>
        public static Polynomial Parse(string expression)
            => new Polynomial(ParseTerms(expression).ToArray());

        /// <summary>
        /// Parse into the raw list of <see cref="Term"/>s, in source order and
        /// <b>before</b> like-term combination.
        /// </summary>
        public static List<Term> ParseTerms(string expression)
        {
            if (expression is null) throw new ArgumentNullException(nameof(expression));

            var normalized = Normalize(expression);
            if (normalized.Length == 0)
                throw new FormatException("Empty polynomial expression.");

            var pieces = SplitIntoSignedTerms(normalized);
            var terms = new List<Term>(pieces.Count);

            foreach (var piece in pieces)
            {
                int sign = piece[0] == '-' ? -1 : 1;
                string body = piece.Substring(1); // drop the explicit leading sign
                if (body.Length == 0)
                    throw new FormatException($"Dangling sign in expression '{expression}'.");

                var (coefficient, variables) = ParseMonomial(body);
                terms.Add(new Term(sign * coefficient, variables));
            }

            return terms;
        }

        // ---------- internals ----------

        private static string Normalize(string expr)
        {
            // Unify the various unicode minus/dash glyphs, then strip whitespace.
            var s = expr
                .Replace("−", "-") // MINUS SIGN
                .Replace("—", "-") // EM DASH
                .Replace("–", "-") // EN DASH
                .Replace(" ", string.Empty)
                .Replace("\t", string.Empty);

            // Force a leading sign so term splitting is uniform.
            if (s.Length > 0 && s[0] != '+' && s[0] != '-')
                s = "+" + s;

            return s;
        }

        private static List<string> SplitIntoSignedTerms(string s)
        {
            var pieces = new List<string>();
            int i = 0;
            while (i < s.Length)
            {
                char sign = s[i];
                if (sign != '+' && sign != '-')
                    throw new FormatException($"Expected a sign at position {i} in '{s}'.");

                int start = i;
                i++; // consume the sign

                while (i < s.Length && s[i] != '+' && s[i] != '-')
                    i++;

                pieces.Add(s.Substring(start, i - start));
            }
            return pieces;
        }

        private static (int Coefficient, Dictionary<string, int> Variables) ParseMonomial(string body)
        {
            // Grammar:  monomial := coefficient? factor*
            //           factor   := '*'? VAR ('^' INT)?
            //           VAR      := single letter [a-zA-Z]
            int i = 0;
            int coefficient = 0;
            bool coefficientSet = false;
            var variables = new Dictionary<string, int>(StringComparer.Ordinal);

            // Optional leading integer coefficient.
            int digitsStart = i;
            while (i < body.Length && char.IsDigit(body[i]))
                i++;
            if (i > digitsStart)
            {
                coefficient = int.Parse(body.AsSpan(digitsStart, i - digitsStart), CultureInfo.InvariantCulture);
                coefficientSet = true;
            }

            // Zero or more variable factors, optionally separated by '*'.
            while (i < body.Length)
            {
                if (body[i] == '*')
                {
                    i++;
                    // A trailing or doubled asterisk (e.g. "3**x") is malformed.
                    if (i >= body.Length || !char.IsLetter(body[i]))
                        throw new FormatException($"Unexpected '*' placement in term '{body}'.");
                    continue;
                }

                if (!char.IsLetter(body[i]))
                    throw new FormatException($"Unexpected character '{body[i]}' at position {i} in term '{body}'.");

                string variableName = body[i].ToString();
                i++;

                int exponent = 1;
                if (i < body.Length && body[i] == '^')
                {
                    i++; // consume '^'
                    if (i >= body.Length || !char.IsDigit(body[i]))
                        throw new FormatException($"Missing exponent after '^' in term '{body}'.");

                    int expStart = i;
                    while (i < body.Length && char.IsDigit(body[i]))
                        i++;

                    exponent = int.Parse(body.AsSpan(expStart, i - expStart), CultureInfo.InvariantCulture);
                }

                if (exponent != 0)
                    variables[variableName] = variables.TryGetValue(variableName, out var existing)
                        ? existing + exponent
                        : exponent;
            }

            // No coefficient digits: it's 1 when variables are present (e.g. "x"),
            // otherwise a bare, malformed monomial (the constant case is handled above).
            if (!coefficientSet)
            {
                if (variables.Count == 0)
                    throw new FormatException($"Invalid monomial '{body}'.");
                coefficient = 1;
            }

            return (coefficient, variables);
        }
    }
}

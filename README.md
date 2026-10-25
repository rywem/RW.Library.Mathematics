# RW.Library.Mathematics

Exact algebra in C#: integers stay integers and rationals stay rationals. No floating point
anywhere in the public API.

- **Polynomials** (`RW.Library.Mathematics.Polynomials`): parse, expand and factor over the
  rationals (rational-root theorem), with integer-coefficient factors.
- **Rational functions** (`RW.Library.Mathematics.RationalFunctions`): quotients of
  multivariate polynomials, always in lowest terms, and exact linear algebra on matrices of them
  (RREF, rank, null space, solve, determinant, inverse).
- **No dependencies.** It references the .NET framework and nothing else (a test checks this),
  so one DLL is all a project needs.

It was built for RW.Web.Workbench. Private: not published to NuGet or anywhere else. You get it
by cloning this repository.

## Quick start

You need the .NET 10 SDK.

```bash
git clone <this repository> RW.Library.Mathematics
cd RW.Library.Mathematics
dotnet build RW.Library.Mathematics -c Release
# → RW.Library.Mathematics/bin/Release/net10.0/RW.Library.Mathematics.dll
#   (a net9.0 build sits beside it)
```

Reference the DLL from your own project:

```xml
<ItemGroup>
  <Reference Include="../RW.Library.Mathematics/RW.Library.Mathematics/bin/Release/net10.0/RW.Library.Mathematics.dll" />
</ItemGroup>
```

If the clone sits beside your project you can instead use a `<ProjectReference>` to
`RW.Library.Mathematics/RW.Library.Mathematics.csproj`. That is what RW.Web.Workbench does.

```csharp
using RW.Library.Mathematics.Polynomials;
using RW.Library.Mathematics.RationalFunctions;

var p = Polynomial.Parse("2x^2 - 7x - 4");
Console.WriteLine(PolynomialFactorization.Factorize(p));           // (2x+1)(1x-4)

var r = RationalFunction.Parse("(x^2 - 1)/(2x - 2)") + RationalFunction.Parse("1/2");
Console.WriteLine(r);                                               // (x + 2)/2
Console.WriteLine(r.Evaluate(new Dictionary<char, Fraction> { ['x'] = new Fraction(1, 3) }));  // 7/6

Console.WriteLine(new Fraction(6, 4) + new Fraction(1, 3));         // 11/6

var m = RationalMatrix.Parse([["a", "1"], ["1", "a"]]);
Console.WriteLine(RationalMatrix.Determinant(m));                   // a^2 - 1
Console.WriteLine(RationalMatrix.Inverse(m)[0, 0]);                 // a/(a^2 - 1)
```

## Using the API

**Variables are single letters.** `xy` means `x*y`, as on paper.

### Polynomials (`RW.Library.Mathematics.Polynomials`)

| Type | What it is |
|---|---|
| `Polynomial` | A sum of `Term`s with integer coefficients. `Polynomial.Parse("2xy^2 - 3y + 4")` combines like terms; `*` multiplies. |
| `Term` | A coefficient and a map of variable → exponent. |
| `PolynomialFactorization.Factorize(p)` | Factors a **one-variable** polynomial over the rationals into a `Factor` tree. Every leaf has integer coefficients, and a root p/q becomes the factor (q·x − p). Throws `InvalidOperationException` for more than one variable. |
| `Factor` | A leaf (`Polynomial`) or a product of child factors (`Children`). `Factor.Parse("(x-1)(x+2)")` reads one back. |
| `PolynomialExpansion.Expand(factor)` | Multiplies a factor tree back out: `Expand(Factorize(p))` equals `p`. |
| `PolynomialParser` | The one parser behind `Polynomial.Parse`. `ParseTerms` gives the terms before like terms are combined. |
| `Fraction` | An exact rational with `BigInteger` parts, kept in lowest terms with a positive denominator. `+ - * /`. |

`ToString()` on `Polynomial` and `Factor` is a plain debug form (`1x-4`, terms not sorted by
degree). Format for people yourself; RW.Web.Workbench's `PolynomialPresentation` is one example.

### Rational functions (`RW.Library.Mathematics.RationalFunctions`)

| Type | What it is |
|---|---|
| `RationalFunction` | An element of Q(x1..xn). `Parse` accepts numbers (integers or decimals, read exactly), single-letter variables, `+ - * /`, integer `^` and brackets, with `2x` and `(x+1)(x-1)` as products. Anything else throws `RationalFunctionDomainException`. Arithmetic: `+ - * /`, `Pow`, `Reciprocal`. `Evaluate` substitutes `Fraction`s. Equality is exact. |
| `MultiPolynomial` | The polynomial ring under it: `BigInteger` coefficients, any number of variables, `Gcd`, exact division. |
| `RationalMatrix` | Static methods on `RationalFunction[,]`: `Parse(string[][])`, `Rref`, `Rank`, `NullSpace`, `Solve` (returns a `LinearSolution`: unique, parametric or inconsistent), `Determinant`, `Inverse`, `Multiply`. |

Matrix results are **generic**: the letters are indeterminates, so `[x 1; 1 1]` has rank 2 even
though it drops to 1 at x = 1. No case analysis on parameter values is done.

Size limits stop runaway input rather than hang: exponents above `RationalFunction.MaxExponent`
(1000) are refused, and a polynomial past `MultiPolynomial.MaxTerms` (100,000) throws
`RationalFunctionTooLargeException`.

## Tests

```bash
dotnet test
```

## More

Design notes and history live in the sibling docs repo, `RW.Mathematics.Documentation/library-mathematics/`.
The CAS that builds on this library is `RW.Service.Mathematics`.

# HANDOFF — RW.Library.Mathematics

The **exact polynomial engine**. Pre-dates the modular architecture; wrapped by the
`polynomials` module.

**Read [`Solutions/Mathematics/handoff-consumed.md`](../../Solutions/Mathematics/handoff-consumed.md) first.**

**Path:** `E:\Development\Projects\RW.Library.Mathematics`
**Branch:** `feature/rw-math-workbench` (pushed to `github.com/rywem/RW.Library.Mathematics`)
**Tests:** 66

---

## 1. What it is

Exact integer/rational polynomial arithmetic. No floating point — the whole point is
exactness.

| Type | Role |
|---|---|
| `Term` | integer coefficient + variable→exponent map |
| `Polynomial` | list of terms; combines like terms on construction |
| `Factor` | a factor tree: leaf polynomial or product node |
| `PolynomialFactorization` | Rational Root Theorem + synthetic division |
| `PolynomialExpansion` | expand a factor tree back to a polynomial |
| `PolynomialParser` | **the** parser (see §3) |

Consumed by `RW.Mathematics.Modules.Polynomials`, which is a thin adapter. **Maths belongs
here; presentation belongs in the module.**

## 2. History — why it looks like this

This repo pre-dates everything else. It had been left mid-refactor: the factorizer had been
deleted from the working tree, and the tests referenced an API that no longer existed. The
factorizer was recovered from git history and the parser rebuilt around it.

That history explains the slightly odd shape — `PolynomialFactorization.cs` contains `Term`,
`Monomial`, `Factor`, `Polynomial`, `PolynomialExpansion` **and** the factorizer, all in one
file. It works and is well tested; splitting it is safe busywork, not urgent.

## 3. ⚠️ One parser, and it must stay that way

There were **two** parsers that silently disagreed (MISTAKES **M-008**):

- `Polynomial.Parse` — regex-based; accepted `x^`, `2x^`, `3**x`, `x +` without error, and
  did **not** normalise unicode minus, so `x^2 − 3x + 4` (U+2212) parsed differently from the
  ASCII form
- `PolynomialParser.Parse` — correct on all of those

The broken one was what the module, RPC and CLI actually called.

**`Polynomial.Parse` now delegates to `PolynomialParser`.** Do not reintroduce a second
parser. If you improve parsing, improve `PolynomialParser`.

Grammar: single-letter variables, so `xy` is `x·y` (not one variable named "xy"). Implicit
and explicit multiplication both accepted. Unicode minus signs (EN dash, EM dash, U+2212)
normalised. Malformed input throws `FormatException`.

## 4. Factorization

Rational Root Theorem + synthetic division over ℚ, emitting **integer-coefficient** factors:
a root p/q becomes `(qx − p)`, with the leftover scale folded into a single integer constant
factor.

**Univariate only** — multivariate input throws. `Factorize` is capped by nothing explicit;
degree is the practical limit.

**The governing invariant, used throughout the tests:**

```
Expand(Factorize(p)) == p     exactly, for every p
```

That is format-independent and catches coefficient, content and sign errors in one
assertion. Prefer it to comparing factor strings.

## 5. Testing

66 tests: parser (including the unicode and malformed cases), factorization breadth
(differences of squares, perfect squares, sums/differences of cubes, biquadratics, non-unit
and negative leading coefficients, content extraction, rational roots with denominators,
irreducibles), polynomial algebra, and the `NodeExpression` tree.

Cross-checked against Maxima's `factor` in the modules repo (`MaximaParityTests`).

## 6. Also in here

`Models/` holds a `NodeExpression` expression tree (Add/Multiply/Power/Negate/Rational…)
with a `Simplify()`. It is **not used** by the factorizer or by any module — it was an
in-progress parallel design. It is tested and harmless; leave it unless you have a use, and
do not assume it is wired into anything.

## 7. If you change something here

1. `dotnet test` in this repo, then the **whole solution** — the `polynomials` module, the
   RPC layer and the CLI all depend on this.
2. Keep the exactness. If you find yourself reaching for `double`, you are in the wrong repo:
   numeric work belongs in `RW.Mathematics.Modules.LinearAlgebra`.
3. Do not add a second parser.

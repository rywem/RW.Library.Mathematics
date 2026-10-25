using RW.Library.Mathematics.Polynomials;

namespace RW.Library.Mathematics.Tests
{
    public class PackagingTests
    {
        // The library stands alone (R-011 packaging): it references the .NET framework and nothing else, so one DLL is all a consumer needs.
        [Fact]
        public void TheLibraryReferencesOnlyTheFramework() =>
            Assert.All(typeof(Polynomial).Assembly.GetReferencedAssemblies(), a => Assert.True(a.Name!.StartsWith("System") || a.Name == "netstandard", a.Name));
    }
}

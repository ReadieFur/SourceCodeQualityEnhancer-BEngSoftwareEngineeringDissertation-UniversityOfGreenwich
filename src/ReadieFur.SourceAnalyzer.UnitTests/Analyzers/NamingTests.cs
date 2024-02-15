namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    [TestFixture]
    public class NamingTests
    {
        [SetUp]
        public async Task Setup() => await LoadConfiguration();

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}

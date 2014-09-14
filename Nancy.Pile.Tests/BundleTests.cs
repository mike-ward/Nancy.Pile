using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nancy.Pile.Tests
{
    [TestClass]
    public class BundleTests
    {
        [TestMethod]
        public void BuildAssetShouldReturnHashCode()
        {
            var hash = Bundle.BuildAssetBundle(new[] {"*.css"}, Bundle.MinificationType.None, ".");
            hash.Should().NotBe(0);
        }

        [TestMethod]
        public void BuildAssetShouldCreateTemplateModule()
        {
            var hash = Bundle.BuildAssetBundle(new[] {"js/app/templates/*.html"}, Bundle.MinificationType.JavaScript, ".");
            hash.Should().NotBe(0);
        }
    }
}
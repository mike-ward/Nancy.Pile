using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nancy.Testing;

namespace Nancy.Pile.Tests
{
    [TestClass]
    public class SampleTests
    {
        [TestMethod]
        public void UnminifiedStyleSheetBundleShouldBeCorrectLength()
        {
            var bootstrapper = new Sample.Bootstrapper();
            var browser = new Browser(bootstrapper);
            var result = browser.Get("/styles.css", with => with.HttpRequest());
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Body.ToArray().Length.Should().Be(35358);
        }

        [TestMethod]
        public void UnminifiedScriptBundleShouldBeCorrectLength()
        {
            var bootstrapper = new Sample.Bootstrapper();
            var browser = new Browser(bootstrapper);
            var result = browser.Get("/scripts.js", with => with.HttpRequest());
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Body.ToArray().Length.Should().Be(133503);            
        }
    }
}
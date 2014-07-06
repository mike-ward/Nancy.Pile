using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nancy.Testing;

namespace Nancy.Pile.Tests
{
    [TestClass]
    public class SampleTests
    {
        [TestMethod]
        public void UnminifiedStyleSheetBundleShouldContainCorrectContent()
        {
            var bootstrapper = new Sample.Bootstrapper();
            var browser = new Browser(bootstrapper);
            var result = browser.Get("/styles.css", with => with.HttpRequest());
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = Encoding.UTF8.GetString(result.Body.ToArray());
            body.Should().Contain("Pure v0.5.0");
        }

        [TestMethod]
        public void UnminifiedScriptBundleShouldContainCorrectContent()
        {
            var bootstrapper = new Sample.Bootstrapper();
            var browser = new Browser(bootstrapper);
            var result = browser.Get("/scripts.js");
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = Encoding.UTF8.GetString(result.Body.ToArray());
            body.Should().Contain("angular.module('app', ['app.constants', 'app.controllers']);");
        }

        [TestMethod]
        public void SecondRequestShouldReturnNotModified()
        {
            var browser = new Browser(new Sample.Bootstrapper());
            var result = browser.Get("/scripts.js");
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Headers["Cache-Control"].Should().Be("no-cache");
            var etag = result.Headers["ETag"];
            etag.Should().NotBeNullOrWhiteSpace();
            var result2 = browser.Get("/scripts.js", with => with.Header("If-None-Match", etag));
            result2.StatusCode.Should().Be(HttpStatusCode.NotModified);
            result2.Headers["Cache-Control"].Should().Be("no-cache");
            result2.Body.Count().Should().Be(0);
        }

        [TestMethod]
        public void IndexPageShouldReturnOkStatus()
        {
            var bootstrapper = new Sample.Bootstrapper();
            var browser = new Browser(bootstrapper);
            var result = browser.Get("/", with => with.HttpRequest());
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = Encoding.UTF8.GetString(result.Body.ToArray());
            body.Should().Contain("This view was rendered using the Nancy Razor view engine");
        }
    }
}
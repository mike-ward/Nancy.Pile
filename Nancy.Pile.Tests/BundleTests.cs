﻿using FluentAssertions;
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
            hash.Should().Be(186375109);
        }

        [TestMethod]
        public void BuildAssetShouldCreateTemplateModule()
        {
            var hash = Bundle.BuildAssetBundle(new[] {"js/app/templates/*.html"}, Bundle.MinificationType.JavaScript, ".");
            hash.Should().Be(1270530780);
        }
    }
}
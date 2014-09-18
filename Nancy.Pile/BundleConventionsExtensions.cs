using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Configuration;

namespace Nancy.Pile
{
    public static class BundleConventionsExtensions
    {
        private static bool Minify
        {
            get
            {
                var cfg = (CompilationSection)ConfigurationManager.GetSection("system.web/compilation");
                return cfg == null || !cfg.Debug;
            }
        }

        public static void StyleBundle(this IList<Func<NancyContext, string, Response>> conventions, string requestedPath,
            IEnumerable<string> files)
        {
            conventions.StyleBundle(requestedPath, Minify, files);
        }

        public static void StyleBundle(this IList<Func<NancyContext, string, Response>> conventions, string requestedPath,
            bool compress, IEnumerable<string> files)
        {
            var compression = compress ? Bundle.MinificationType.StyleSheet : Bundle.MinificationType.None;
            conventions.AddBundle(requestedPath, "text/css;charset=utf-8", compression, files);
        }

        public static void ScriptBundle(this IList<Func<NancyContext, string, Response>> conventions, string requestedPath,
            IEnumerable<string> files)
        {
            conventions.ScriptBundle(requestedPath, Minify, files);
        }

        public static void ScriptBundle(this IList<Func<NancyContext, string, Response>> conventions, string requestedPath,
            bool minify, IEnumerable<string> files)
        {
            var compression = minify ? Bundle.MinificationType.JavaScript : Bundle.MinificationType.None;
            conventions.AddBundle(requestedPath, "application/x-javascript;charset=utf-8", compression, files);
        }

        private static void AddBundle(this ICollection<Func<NancyContext, string, Response>> conventions, string requestedPath, string contentType,
            Bundle.MinificationType minificationType, IEnumerable<string> files)
        {
            conventions.Add(BundleConventionBuilder.AddBundle(requestedPath, contentType, minificationType, files));
        }
    }
}
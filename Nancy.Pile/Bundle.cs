using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Configuration;
using Microsoft.Ajax.Utilities;
using Nancy.Responses;

namespace Nancy.Pile
{
    public static class Bundle
    {
        public enum MinificationType
        {
            None,
            StyleSheet,
            JavaScript
        };

        private static readonly ConcurrentDictionary<int, AssetBundle> AssetBundles = new ConcurrentDictionary<int, AssetBundle>();

        public static Response ResponseFactory(int hash, string contentType, NancyContext context)
        {
            var bundle = AssetBundles[hash];
            var etag = context.Request.Headers.IfNoneMatch.FirstOrDefault();

            return (etag != null && etag.Equals(bundle.ETag, StringComparison.Ordinal))
                ? ResponseNotModified()
                : ResponseFromBundle(bundle, contentType);
        }

        public static int BuildAssetBundle(IEnumerable<string> files, MinificationType minificationType, string applicationRootPath)
        {
            if (files == null) throw new ArgumentNullException("files");
            if (applicationRootPath == null) throw new ArgumentNullException("applicationRootPath");

            var contents = BuildFileList(files, applicationRootPath).Select(path => BuildText(path, minificationType));
            var bytes = Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, contents));
            var hash = ComputeHash(bytes);
            AssetBundles.TryAdd(hash.GetHashCode(), new AssetBundle { ETag = hash, Bytes = bytes });
            return hash.GetHashCode();
        }

        private static Response ResponseNotModified()
        {
            var response = new Response
            {
                StatusCode = HttpStatusCode.NotModified,
                ContentType = null,
                Contents = Response.NoBody
            };
            response.Headers["Cache-Control"] = "no-cache";
            return response;
        }

        private static Response ResponseFromBundle(AssetBundle assetBundle, string contentType)
        {
            var stream = new MemoryStream(assetBundle.Bytes);
            var response = new StreamResponse(() => stream, contentType);
            response.Headers["ETag"] = assetBundle.ETag;
            response.Headers["Cache-Control"] = "no-cache";
            return response;
        }

        private class AssetBundle
        {
            public string ETag { get; set; }
            public byte[] Bytes { get; set; }
        }

        public static IEnumerable<string> BuildFileList(IEnumerable<string> fileEntries, string applicationRootPath)
        {
            var files = fileEntries as string[] ?? fileEntries.ToArray();

            var excludedFiles = files
                .Where(file => file.StartsWith("!"))
                .Select(file => Path.Combine(applicationRootPath, file.Substring(1)))
                .EnumerateFiles();

            var includedFiles = files
                .Where(file => file.StartsWith("!") == false)
                .Select(file => Path.Combine(applicationRootPath, file))
                .EnumerateFiles();

            return includedFiles.Except(excludedFiles);
        }

        private static IEnumerable<string> EnumerateFiles(this IEnumerable<string> paths)
        {
            var files = paths
                .Select(path => Directory.EnumerateFiles(Path.GetDirectoryName(path), Path.GetFileName(path), SearchOption.AllDirectories))
                .SelectMany(path => path.ToArray())
                .Distinct();
            return files;
        }

        private static string BuildText(string path, MinificationType minificationType)
        {
            var text = File.ReadAllText(path);
            if (path.EndsWith("html", StringComparison.OrdinalIgnoreCase)) text = BuildScriptTemplate(Path.GetFileName(path), text);
            if (minificationType == MinificationType.None) text = string.Format("\n/* {0} */\n{1}", path, text);
            if (path.IndexOf(".min.", StringComparison.OrdinalIgnoreCase) != -1) return text;
            if (minificationType == MinificationType.StyleSheet) return MinifyStyleSheet(text);
            if (minificationType == MinificationType.JavaScript) return MinifyJavaScript(text);
            return text;
        }

        private static string BuildScriptTemplate(string name, string text)
        {
            const string moduleName = "nancy.pile.templates";
            var script = string.Format(
                "angular.module('{0}').run(['$templateCache',function ($templateCache){{$templateCache.put('{1}','{2}');}}]);",
                moduleName, name, text.Replace("'", "\\'"));
            return script;
        }

        private static string MinifyStyleSheet(string text)
        {
            var minifier = new Minifier();
            return minifier.MinifyStyleSheet(text);
        }

        private static string MinifyJavaScript(string text)
        {
            var minifier = new Minifier();
            return minifier.MinifyJavaScript(text);
        }

        private static string ComputeHash(byte[] bytes)
        {
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(bytes);
            md5.Dispose();
            return "\"" + BitConverter.ToString(hash).Replace("-", "") + "\"";
        }
    }

    public static class BundleConventionBuilder
    {
        public static Func<NancyContext, string, Response> AddBundle(string bundlePath, string contentType,
            Bundle.MinificationType minificationType, IEnumerable<string> files)
        {
            var hash = 0;
            var reset = true;
            var sync = new object();
            List<FileSystemWatcher> monitors = null;
            if (bundlePath.StartsWith("/") == false) bundlePath = string.Concat("/", bundlePath);

            return (context, applicationRootPath) =>
            {
                var path = context.Request.Path;
                if (path.Equals(bundlePath, StringComparison.OrdinalIgnoreCase) == false)
                {
                    context.Trace.TraceLog.WriteLog(x => x.AppendLine(
                        string.Concat("[BundleStaticContentConventionBuilder] The requested resource '",
                            path, "' does not match convention mapped to '", bundlePath, "'")));
                    return null;
                }

                if (reset)
                {
                    Interlocked.Exchange(ref hash, Bundle.BuildAssetBundle(files, minificationType, applicationRootPath));
                    reset = false;
                    lock (sync)
                    {
                        if (monitors == null)
                        {
                            monitors = Bundle.BuildFileList(files, applicationRootPath)
                                .Select(f => Path.Combine(applicationRootPath, f))
                                .Select(Path.GetDirectoryName)
                                .Distinct()
                                .Select(d =>
                                {
                                    var fw = new FileSystemWatcher(d) { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.LastWrite };
                                    fw.Changed += (sender, args) => reset = true;
                                    fw.EnableRaisingEvents = true;
                                    return fw;
                                })
                                .ToList();
                        }
                    }
                }
                return Bundle.ResponseFactory(hash, contentType, context);
            };
        }
    }

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
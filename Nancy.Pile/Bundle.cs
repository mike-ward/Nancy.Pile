using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Configuration;
using dotless.Core;
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
            var match = etag != null && etag.Equals(bundle.ETag, StringComparison.Ordinal);
            return match ? ResponseNotModified() : ResponseFromBundle(bundle, contentType);
        }

        public static int BuildAssetBundle(IEnumerable<string> fileEntries, MinificationType minificationType, string applicationRootPath)
        {
            if (fileEntries == null) throw new ArgumentNullException("fileEntries");
            if (applicationRootPath == null) throw new ArgumentNullException("applicationRootPath");

            var files = fileEntries.BuildFileList(applicationRootPath).ToArray();

            var nonHtmlFileContents = files
                .Where(file => file.EndsWith(".html", StringComparison.OrdinalIgnoreCase) == false)
                .Select(file => string.Format("\n/* {0} */\n{1}", file, ReadFile(file)))
                .Aggregate(new StringBuilder(), (a, b) => a.Append("\n").Append(b));

            var htmlFileContents = files
                .Where(file => file.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                .AsAngluarModule(applicationRootPath);

            var contents = Minify(string.Join("\n", nonHtmlFileContents + htmlFileContents), minificationType);
            var bytes = Encoding.UTF8.GetBytes(contents);
            var etag = ETag(bytes);
            var hash = etag.GetHashCode();
            AssetBundles.TryAdd(hash, new AssetBundle {ETag = etag, Bytes = bytes});
            return hash;
        }

        private static string ReadFile(string file)
        {
            var text = File.ReadAllText(file);
            return file.EndsWith(".less") ? Less.Parse(text) : text;
        }

        private static string Minify(string text, MinificationType minificationType)
        {
            if (minificationType == MinificationType.StyleSheet) return MinifyStyleSheet(text);
            if (minificationType == MinificationType.JavaScript) return MinifyJavaScript(text);
            return text;
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

        public static IEnumerable<string> BuildFileList(this IEnumerable<string> fileEntries, string applicationRootPath)
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

        private static string AsAngluarModule(this IEnumerable<string> files, string rootPath)
        {
            if (files.Any() == false) return string.Empty;

            var templates = files
                .Select(file => new
                {
                    Name = file.Replace(rootPath, "").Replace('\\', '/'),
                    Template = Regex.Replace(File.ReadAllText(file), @"\r?\n", "\\n").Replace("'", "\\'")
                })
                .Select(nt => string.Format("\t$templateCache.put('{0}','{1}');\n", nt.Name, nt.Template))
                .Aggregate(new StringBuilder(), (a, b) => a.Append(b));

            return string.Format(
                "\n\nangular.module('nancy.pile.templates', []).run(['$templateCache',function ($templateCache){{\n{0}}}]);",
                templates);
        }

        private static string MinifyStyleSheet(string text)
        {
            var minifier = new Minifier();
            return minifier.MinifyStyleSheet(text);
        }

        private static string MinifyJavaScript(string text)
        {
            var minifier = new Minifier();
            return minifier.MinifyJavaScript(text, new CodeSettings {PreserveImportantComments = false});
        }

        private static string ETag(byte[] bytes)
        {
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(bytes);
            md5.Dispose();
            return string.Concat("\"", BitConverter.ToString(hash).Replace("-", ""), "\"");
        }

        public static void RemoveBundle(int hash)
        {
            AssetBundle bundle;
            AssetBundles.TryRemove(hash, out bundle);
        }
    }

    public static class BundleConventionBuilder
    {
        public static Func<NancyContext, string, Response> AddBundle(string bundlePath, string contentType,
            Bundle.MinificationType minificationType, IEnumerable<string> fileEntries)
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
                        string.Concat("[BundleConventionBuilder] The requested resource '",
                            path, "' does not match convention mapped to '", bundlePath, "'")));
                    return null;
                }

                if (reset)
                {
                    var files = fileEntries as string[] ?? fileEntries.ToArray();
                    var old = Interlocked.Exchange(ref hash, Bundle.BuildAssetBundle(files, minificationType, applicationRootPath));
                    reset = false;
                    if (old != hash) Bundle.RemoveBundle(old);
                    lock (sync)
                    {
                        if (monitors == null)
                        {
                            monitors = files
                                .BuildFileList(applicationRootPath)
                                .Select(f => Path.Combine(applicationRootPath, f))
                                .Select(Path.GetDirectoryName)
                                .Distinct()
                                .Select(d =>
                                {
                                    var fw = new FileSystemWatcher(d) {IncludeSubdirectories = true, NotifyFilter = NotifyFilters.LastWrite};
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
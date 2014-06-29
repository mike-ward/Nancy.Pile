using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Ajax.Utilities;
using Nancy.Responses;

namespace Nancy.Pile
{
    public static class Bundle
    {
        public enum CompressionType
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

        public static int BuildAssetBundle(IEnumerable<string> files, CompressionType compressionType, string applicationRootPath)
        {
            if (files == null) throw new ArgumentNullException("files");
            if (applicationRootPath == null) throw new ArgumentNullException("applicationRootPath");

            var contents = files
                .Select(file => Path.Combine(applicationRootPath, file))
                .Select(path => Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path), SearchOption.AllDirectories))
                .SelectMany(path => path)
                .Distinct()
                .Select(path => BuildText(path, compressionType));

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

        private static string BuildText(string path, CompressionType compressionType)
        {
            var text = File.ReadAllText(path);
            if (path.IndexOf(".min.", StringComparison.OrdinalIgnoreCase) != -1) return text;
            text = string.Format("/* {0} */\n{1}", path, text);
            if (compressionType == CompressionType.StyleSheet) return MinifyStyleSheet(text);
            if (compressionType == CompressionType.JavaScript) return MinifyJavaScript(text);
            return text;
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

        public static bool IsDebugMode
        {
#if DEBUG
            get { return true; }
#else
            get { return false; }
#endif
        }
    }

    public static class StaticContentBundleConventionBuilder
    {
        public static Func<NancyContext, string, Response> AddBundle(string bundlePath, string contentType,
            Bundle.CompressionType compressionType, IEnumerable<string> files)
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
                    Interlocked.Exchange(ref hash, Bundle.BuildAssetBundle(files, compressionType, applicationRootPath));
                    reset = false;
                    lock (sync)
                    {
                        if (monitors == null)
                        {
                            monitors = files
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

    public static class StaticContentBundleConventionsExtensions
    {
        public static void AddStylesBundle(this IList<Func<NancyContext, string, Response>> conventions, string requestedPath,
            bool compress, IEnumerable<string> files)
        {
            var compression = compress ? Bundle.CompressionType.StyleSheet : Bundle.CompressionType.None;
            conventions.AddBundle(requestedPath, "text/css", compression, files);
        }

        public static void AddScriptsBundle(this IList<Func<NancyContext, string, Response>> conventions, string requestedPath,
            bool compress, IEnumerable<string> files)
        {
            var compression = compress ? Bundle.CompressionType.JavaScript : Bundle.CompressionType.None;
            conventions.AddBundle(requestedPath, "application/x-javascript", compression, files);
        }

        public static void AddBundle(this IList<Func<NancyContext, string, Response>> conventions, string requestedPath, string contentType,
            Bundle.CompressionType compressionType, IEnumerable<string> files)
        {
            conventions.Add(StaticContentBundleConventionBuilder.AddBundle(requestedPath, contentType, compressionType, files));
        }
    }
}
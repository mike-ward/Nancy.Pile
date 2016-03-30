using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
        }

        private static readonly ConcurrentDictionary<int, AssetBundle> AssetBundles =
            new ConcurrentDictionary<int, AssetBundle>();

        public static Response ResponseFactory(int hash, string contentType, NancyContext context)
        {
            var bundle = AssetBundles[hash];
            var etag = context.Request.Headers.IfNoneMatch.FirstOrDefault();
            var match = etag != null && etag.Equals(bundle.ETag, StringComparison.Ordinal);
            return match ? ResponseNotModified() : ResponseFromBundle(bundle, contentType);
        }

        public static int BuildAssetBundle(IEnumerable<string> fileEntries, MinificationType minificationType,
            string applicationRootPath)
        {
            if (fileEntries == null) throw new ArgumentNullException(nameof(fileEntries));
            if (applicationRootPath == null) throw new ArgumentNullException(nameof(applicationRootPath));

            var files = fileEntries.BuildFileList(applicationRootPath).ToArray();

            var nonHtmlFileContents = files
                .Where(file => file.EndsWith(".html", StringComparison.OrdinalIgnoreCase) == false)
                .Select(file => $"\n/* {file} */\n{ReadFile(file)}")
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

        public static byte[] GetBundleBytes(int id)
        {
            return AssetBundles[id].Bytes;
        }

        private static string ReadFile(string file)
        {
            var text = File.ReadAllText(file);
            if (file.EndsWith(".less")) return Less.Parse(text);
            if (file.EndsWith(".coffee")) return CoffeeScript.Compile(text);
            if (file.EndsWith(".scss")) return Sass.Compile(text);
            return text;
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
                Contents = Response.NoBody,
                Headers = new Dictionary<string, string> {{"Cache-Control", "no-cache"}}
            };
            return response;
        }

        private static Response ResponseFromBundle(AssetBundle assetBundle, string contentType)
        {
            var stream = new MemoryStream(assetBundle.Bytes);
            var response = new StreamResponse(() => stream, contentType)
            {
                Headers = new Dictionary<string, string> {{"ETag", assetBundle.ETag}}
            };
            return response;
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

        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private static IEnumerable<string> EnumerateFiles(this IEnumerable<string> paths)
        {
            var files = paths
                .Select(
                    path =>
                        Directory.EnumerateFiles(Path.GetDirectoryName(path), Path.GetFileName(path),
                            SearchOption.AllDirectories))
                .SelectMany(path => path.ToArray())
                .Distinct();
            return files;
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private static string AsAngluarModule(this IEnumerable<string> files, string rootPath)
        {
            if (files.Any() == false) return string.Empty;

            var templates = files
                .Select(file => new
                {
                    Name =
                        string.IsNullOrEmpty(rootPath)
                            ? file.Replace('\\', '/')
                            : file.Replace(rootPath, "").Replace('\\', '/'),
                    Template = Regex.Replace(File.ReadAllText(file), @"\r?\n", "\\n").Replace("'", "\\'")
                })
                .Select(nt => $"\t$templateCache.put('{nt.Name}','{nt.Template}');\n")
                .Aggregate(new StringBuilder(), (a, b) => a.Append(b));

            return
                "\n\nangular.module('nancy.pile.templates', [])"
                + $".run(['$templateCache',function ($templateCache){{\n{templates}}}]);";
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

        private class AssetBundle
        {
            public string ETag { get; set; }
            public byte[] Bytes { get; set; }
        }
    }
}
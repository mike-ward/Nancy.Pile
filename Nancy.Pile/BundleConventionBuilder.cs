using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Nancy.Pile
{
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
}
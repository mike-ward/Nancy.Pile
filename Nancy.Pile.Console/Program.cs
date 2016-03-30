using System.Collections.Generic;
using System.Text;

namespace Nancy.Pile.Console
{
    internal class Program
    {
        private static bool _minifyJavascript;
        private static bool _minifyCss;
        private static bool _badOption;
        private static string _prefix = "";
        private static readonly List<string> Files = new List<string>();

        private static void Main(string[] args)
        {
            CommandLineArgs(args);

            if (_badOption || (_minifyCss && _minifyJavascript))
            {
                ShowHelp();
                return;
            }

            var minify = !_minifyJavascript && !_minifyCss
                ? Bundle.MinificationType.None
                : _minifyJavascript
                    ? Bundle.MinificationType.JavaScript
                    : Bundle.MinificationType.StyleSheet;

            var id = Bundle.BuildAssetBundle(Files, minify, _prefix);
            var bytes = Bundle.GetBundleBytes(id);
            var text = Encoding.UTF8.GetString(bytes);
            System.Console.Write(text);
        }

        private static void CommandLineArgs(IEnumerable<string> args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    SetOption(arg);
                }
                else
                {
                    Files.Add(arg);
                }
            }
        }

        private static void SetOption(string arg)
        {
            if (arg == "-js")
            {
                _minifyJavascript = true;
                return;
            }
            if (arg == "-css")
            {
                _minifyCss = true;
                return;
            }
            if (arg.StartsWith("-prefix:"))
            {
                _prefix = arg.Substring(8);
                return;
            }
            _badOption = true;
        }

        private static void ShowHelp()
        {
            var help = @"
usage: Nancy.Pile.Console [options] files
    -js          = minify as JavaScript
    -css         = minify as CSS
    -prefix:path = file path prefix (for html templates)
";
            System.Console.WriteLine(help);
        }
    }
}
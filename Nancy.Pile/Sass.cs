using NSass;

namespace Nancy.Pile
{
    public static class Sass
    {
        public static string Compile(string source)
        {
            var engine = new SassCompiler();
            return engine.Compile(source);
        }
    }
}
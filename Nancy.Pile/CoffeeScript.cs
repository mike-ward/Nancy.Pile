using Jint;
using Nancy.Pile.Properties;

namespace Nancy.Pile
{
    internal static class CoffeeScript
    {
        public static string Compile(string text)
        {
            var engine = new Engine();
            engine.SetValue("coffeein", text);
            engine.Execute(Resources.CoffeeScript);
            engine.Execute("coffeeout = CoffeeScript.compile(coffeein)");
            return engine.GetValue("coffeeout").AsString();
        }
    }
}
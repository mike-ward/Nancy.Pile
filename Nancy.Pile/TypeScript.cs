using System;
using Jint;
using Nancy.Pile.Properties;

namespace Nancy.Pile
{
    internal static class TypeScript
    {
        public static string Compile(string text)
        {
            try
            {
                var engine = new Engine();
                engine.SetValue("typescriptin", text);
                engine.Execute(Resources.TypeScript);
                engine.Execute(Resources.tsc);
                var js = engine.GetValue("typescriptout").AsString();
                return js;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}
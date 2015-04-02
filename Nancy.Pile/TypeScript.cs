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
                engine.SetValue("source", text);
                engine.SetValue("libSource", ""); //Resources.lib_d);
                engine.Execute(Resources.typescript);
                engine.Execute(Resources.typescript_api);
                var result = engine.GetValue("result").AsString();
                return result;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}
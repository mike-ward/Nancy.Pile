using Nancy.Conventions;
using Nancy.Diagnostics;

namespace Nancy.Pile.Sample
{
    using Nancy;

    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);

            nancyConventions.StaticContentsConventions.StyleBundle("styles.css",
                new[]
                {
                    "css/pure.css",
                    "css/*.less",
                    "css/*.scss"
                });

            nancyConventions.StaticContentsConventions.ScriptBundle("scripts.js", true,
                new[]
                {
                    "js/third-party/*.js",
                    "!js/third-party/bomb.js",
                    "js/app/*.js",
                    "js/coffee/*.coffee",
                    "js/app/templates/*.html"
                });
        }

        protected override DiagnosticsConfiguration DiagnosticsConfiguration => new DiagnosticsConfiguration { Password = @"secret" };
    }
}
﻿using Nancy.Conventions;

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

            nancyConventions.StaticContentsConventions.ScriptBundle("scripts.js",
                new[]
                {
                    "js/third-party/*.js",
                    "!js/third-party/bomb.js",
                    "js/app/*.js",
                    "js/coffee/*.coffee",
                    "js/typescript/*.ts",
                    "js/app/templates/*.html"
                });
        }
    }
}
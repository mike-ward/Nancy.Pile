Nancy.Pile
==========

Takes a pile of files and concatenates them into a single resource.  It's a super simple asset bundler for NancyFx.

## Features ##

Concats and minifies style sheets and javascript files.

Won't minify files with ".min." in the file name.

Nuget package or include a single file in your current package.

Detects when files change.

Wild card file matching with duplicate detection (useful when ordering matters)

Uncompressed bundles insert comment with file name for each file for easier debugging.


## Install ##

```
PM> Install-Package Nancy.Pile
```

or just copy the `Bundle.cs` file from the source repository and `PM> Install-Package AjaxMin`

## Example Usage ##

Update your bootstrapper.

```C#
 public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);

            nancyConventions.StaticContentsConventions.AddStylesBundle("styles.css", true,
                new[]
                {
                    "css/pure.css",
                    "css/*.css"
                });

            nancyConventions.StaticContentsConventions.AddScriptsBundle("scripts.js", true,
                new[]
                {
                    "js/third-party/*.js",
                    "js/app.js",
                    "js/app/*.js"
                });
        }
    }
```

And reference the bundles in html

```HTML
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>Nancy.Pile.Sample</title>
  <link href="~/styles.css" rel="stylesheet" />
  <script src="~/scripts.js"></script>
</head>
```
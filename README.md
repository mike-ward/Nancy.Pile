Nancy.Pile
==========

Takes a pile of files and concatenates them into a single resource.  It's a super simple asset bundler for NancyFx.

## Features

Concats and minifies style sheets and javascript files.

Won't minify files with ".min." in the file name.

Nuget package or include a single file in your current package.

Detects when files change and invalidates cache.

Wildcard characters with duplicate detection (useful when ordering matters)

Excludes file(s) if first character is "!".

Unminified bundles insert comment with file name for easier debugging.

Overloaded bundle methods automatically minify on release builds only.


## Install

```
PM> Install-Package Nancy.Pile
```

or just copy the `Bundle.cs` file from the source repository and `PM> Install-Package AjaxMin`

## Example Usage

Update your bootstrapper.

```C#
public class Bootstrapper : DefaultNancyBootstrapper
   {
       protected override void ConfigureConventions(NancyConventions nancyConventions)
       {
           base.ConfigureConventions(nancyConventions);

           nancyConventions.StaticContentsConventions.StyleBundle("styles.css",
               new[]
               {
                   "css/pure.css",
                   "css/*.css"
               });

           nancyConventions.StaticContentsConventions.ScriptBundle("scripts.js",
               new[]
               {
                   "js/third-party/*.js",
                   "!js/third-party/bomb.js",
                   "js/app.js",
                   "js/app/*.js"
               });
       }
   }
```

And reference the bundles in html (razor example)

```HTML
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>Nancy.Pile.Sample</title>
  <link href="~/styles.css" rel="stylesheet" />
  <script src="~/scripts.js"></script>
</head>
```

## Release Notes

- 0.3.3, 8/9/2014
 * Use EnumerateFiles for slightly more efficient processing
 * Add Nancy logo to nuget package

- 0.3.2, 7/17/2014
 * Suppress file path comment for sources that are already minified
 * Add charset=utf-8 to content type response header

- 0.3.1, 7/6/2014
 * detect debug/release settings from web.config

- 0.3.0, 7/6/2014
 * (Breaking) Change CompressionType enum to MinificationType enum
 * Exclude file specifications that start with "!"

- 0.2.0, 7/5/2014
 
 * (Breaking) Rename AddStylesBundle, AddScriptsBundle to StyleBundle, ScriptBundle
 * Add overloads to StyleBundle, AddScriptBundle

- 0.1.0, 7/1/2014

 * Initial release
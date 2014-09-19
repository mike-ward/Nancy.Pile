# Nancy.Pile [![Build status](https://ci.appveyor.com/api/projects/status/7h5n3c8ah28a5hx5)](https://ci.appveyor.com/project/mike-ward/nancy-pile)

Takes a pile of files and concatenates them into a single resource.  It's a super simple asset bundler for NancyFx.

## Features

- Bundles at runtime. No modifications to build process required.
- ETags insure bundle downloaded once. Additional calls return `304 Not Modified`
- Concats and minifies style sheets and JavaScript files.
- Preprocesses Less, Sass and CoffeeScript.
- Bundles AngularJS HTML templates.
- Detects when files change and rebuilds bundle.
- Wildcard characters with duplicate detection (useful when ordering matters).
- Excludes file(s) if first character is "!".
- Unminified bundles insert comment with file name for easier debugging.
- Overloaded bundle methods automatically minify on release builds only.
- [Nuget package](http://www.nuget.org/packages/Nancy.Pile/) available.
- Kittens are never in danger.

## Install

```
PM> Install-Package Nancy.Pile
```

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
                   "css/*.less",
                   "css/*.scss"
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

And reference the bundles in HTML (razor example)

```HTML
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>Nancy.Pile.Sample</title>
  <link href="~/styles.css" rel="stylesheet" />
  <script src="~/scripts.js"></script>
</head>
```

## Bundle HTML templates for AngularJS

HTML templates used in AngularJS directives and else where can be preloaded into the 
[template cache](https://docs.angularjs.org/api/ng/service/$templateCache#!).

Include HTML files to be bundled.

```C#
public class Bootstrapper : DefaultNancyBootstrapper
   {
       protected override void ConfigureConventions(NancyConventions nancyConventions)
       {
           base.ConfigureConventions(nancyConventions);

           nancyConventions.StaticContentsConventions.ScriptBundle("scripts.js",
               new[]
               {
                   "js/third-party/*.js",
                   "!js/third-party/bomb.js",
                   "js/app/*.js",
                   "js/app/templates/*.html"
               });
       }
   }
```

Update your JavaScript application to reference 'nancy.pile.templates'.

```JavaScript
angular.module('app', ['nancy.pile.templates']);
```

In your directive, refer to the template.

```JavaScript
angular.module('app.directives')
  .directive('silly', function() {
    return {
      templateUrl: 'js/app/templates/silly.html'
    }
  });
```

## Release Notes

- 0.5.0
 * Add Less support
 * Add CoffeScript support
 * Add Sass Support

- 0.4.2, 8/18/2014
 * Remove old bundles after updating.

- 0.4.1, 8/17/2014
 * Bundle HTML in one module declaration.
 * Remove need to define 'nancy.pile.templates' module
 * Slightly better compression

- 0.4.0, 8/14/2014
 * Add AngularJS template cache bundling

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

msbuild.exe Nancy.Pile.Console.csproj /property:Configuration=Release
IF ERRORLEVEL 0 nuget pack Nancy.Pile.Console.csproj -Prop Configuration=Release
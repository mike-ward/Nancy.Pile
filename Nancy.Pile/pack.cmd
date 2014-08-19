C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe Nancy.Pile.csproj /property:Configuration=Release
IF ERRORLEVEL 0 nuget pack Nancy.Pile.csproj -Prop Configuration=Release
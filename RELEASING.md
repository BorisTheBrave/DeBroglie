Steps to releasing:
 * Run tests
 * Run and validate samples
 * Choose new version
 * Update docs/articles/release_notes.md
 * Build NuGet package in Visual Studio
 * * Update package and assembly version in DeBroglie.csproj
 * * Use Release configuration, then `Build > Pack DeBroglie`
 * Publish:
    `dotnet nuget push DeBroglie.<version>.nupkg -k <api-key> -s https://api.nuget.org/v3/index.json`
 * Run release.py, then upload release-*.zip to GitHub, under a new tag
Steps to releasing:
 * Run tests
 * Choose new version
 * Update docs/articles/release_notes.md
 * Build NuGet package in Visual Studio
 * Publish:
    dotnet nuget push DeBroglie.<version>.nupkg -k <api-key> -s https://api.nuget.org/v3/index.json
 * Run release.py, then upload release.zip to GitHub, under a new tag
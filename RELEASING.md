Steps to releasing:
 * Choose new version
 * Update docs/articles/release_notes.md
 * Build NuGet package in Visual Studio
 * Publish:
    dotnet nuget push DeBroglie.0.1.0.nupkg -k <api-key> -s https://api.nuget.org/v3/index.json
 * Run release.py, then upload release.zip to GitHub, under a new tag
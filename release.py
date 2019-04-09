import subprocess
import os
import shutil
import re
RUNTIMES = ["any", "win-x64", "linux-x64", "osx-x64"]
# Build the docs
subprocess.check_call(["docfx","docs/docfx.json"])
# Start from fresh
shutil.rmtree("release", ignore_errors=True)
for runtime in RUNTIMES:
    # Build the project
    subprocess.check_call(["dotnet","publish","DeBroglie.Console/DeBroglie.Console.csproj","-c","Release","-r",runtime])
    # Move everything to a fresh folder
    subdir = "release/" +runtime
    shutil.copytree("DeBroglie.Console/bin/Release/netcoreapp2.0/" + runtime + "/publish", subdir + "/bin")
    shutil.copytree("docs-generated", subdir + "/docs")
    shutil.copy("README.md", subdir)
    shutil.copy("LICENSE.txt", subdir)
    # zip it up
    runtime_name = runtime
    if runtime is "any":
        runtime_name = "portable"
        readme_txt = open(subdir + "/README.md").read()
        extra_text = "This release contains a cross platform executable. To run it, you must install .NET Core 2.0 or above and use:\n\n    dotnet bin/DeBroglie.Console.dll\n"
        readme_txt = re.sub(r"(Usage\r?\n-*\r?\n)", r"\1"+extra_text, readme_txt)
        open(subdir + "/README.md", "w").write(readme_txt)
    shutil.make_archive("release-" + runtime_name, "zip", subdir)
# Cleanup
shutil.rmtree("release", ignore_errors=True)

import subprocess
import shutil
import re
RUNTIMES = ["any", "win-x64", "linux-x64", "osx-x64"]
# Build the docs
subprocess.check_call(["docfx","docs/docfx.json"])
# Start from fresh
shutil.rmtree("release", ignore_errors=True)
for runtime in RUNTIMES:
    # Build the project
    args = ["dotnet","publish","DeBroglie.Console/DeBroglie.Console.csproj","-c","Release"]
    if runtime != "any":
        args.extend(["-r",runtime, "--self-contained"])
    subprocess.check_call(args)
    # Move everything to a fresh folder
    subdir = "release/" +runtime
    path = "DeBroglie.Console/bin/Release/net6.0" + ("/" + runtime if runtime != "any" else "") + "/publish"
    shutil.copytree(path, subdir + "/bin")
    shutil.copytree("docs-generated", subdir + "/docs")
    shutil.copy("README.md", subdir)
    shutil.copy("LICENSE.txt", subdir)
    # zip it up
    runtime_name = runtime
    if runtime == "any":
        runtime_name = "portable"
        readme_txt = open(subdir + "/README.md").read()
        extra_text = "This release contains a cross platform executable. To run it, you must install .NET Core 6.0 or above and use:\n\n    dotnet bin/DeBroglie.Console.dll\n"
        readme_txt = re.sub(r"(Usage\r?\n-*\r?\n)", r"\1"+extra_text, readme_txt)
        open(subdir + "/README.md", "w").write(readme_txt)
    shutil.make_archive("release-" + runtime_name, "zip", subdir)
# Cleanup
shutil.rmtree("release", ignore_errors=True)

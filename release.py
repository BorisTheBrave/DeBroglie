import subprocess
import os
import shutil
# Build the project
subprocess.check_call(["dotnet","build","DeBroglie.Console/DeBroglie.Console.csproj","-c","Release"])
# Build the docs
subprocess.check_call(["docfx","docs/docfx.json"])
# Move everything to a fresh folder
shutil.rmtree("release", ignore_errors=True)
shutil.copytree("DeBroglie.Console/bin/Release", "release/bin")
shutil.copytree("docs-generated", "release/docs")
shutil.copy("README.md", "release")
shutil.copy("LICENSE.txt", "release")
# zip it up
shutil.make_archive("release", "zip", "release")
# Cleanup
shutil.rmtree("release", ignore_errors=True)

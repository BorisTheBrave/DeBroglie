import subprocess
import os
import glob

de_broglie = ["dotnet", "run", "--project", "../DeBroglie.Console", "--no-launch-profile", "--"]

for filename in glob.iglob('**/*.json', recursive=True):
    if filename.endswith("schema.json"):
        continue
    print(f"Running {filename}")
    subprocess.check_call(de_broglie + [filename])

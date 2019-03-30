import subprocess
import os
import glob

de_broglie_path = r"..\DeBroglie.Console\bin\Debug\DeBroglie.Console.exe"

for filename in glob.iglob('**/*.json', recursive=True):
    if filename.endswith("schema.json"):
        continue
    print(f"Running {filename}")
    subprocess.check_call([de_broglie_path, filename])

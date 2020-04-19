$DeBroglie="..\..\DeBroglie.Console\bin\Debug\netcoreapp2.0\DeBroglie.Console.dll"
$FfmpegDir="C:\Portable\ffmpeg\bin"
$env:Path += ";$TiledDir;$FfmpegDir"

Remove-Item -Recurse output
& dotnet $DeBroglie platformer.json

$LastFrame = (Get-ChildItem "output" -Filter *.png -Name|Select-String -Pattern "\d+" -AllMatches | % { $_.Matches.Value }|Measure-Object -Max).Maximum
ffmpeg -y  -i "output/platformer.%d.png" -crf 0 -vf "loop=50:1:$LastFrame" platformer.webm
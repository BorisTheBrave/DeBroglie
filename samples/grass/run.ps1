$DeBroglie="..\..\DeBroglie.Console\bin\Debug\DeBroglie.Console.exe"
$TiledDir="C:\Program Files (x86)\Tiled"
$FfmpegDir="C:\Unbacked up documents\Small Programs\ffmpeg\bin"
$env:Path += ";$TiledDir;$FfmpegDir"

Remove-Item -Recurse output
& $DeBroglie edged_path_constraint.json
& $DeBroglie map.json

Write-Output "Rasterising.."
Get-ChildItem "output" -Filter *.tmx |
Foreach-Object {
    $TmxName = $_.FullName
    $PngName = [io.path]::ChangeExtension($TmxName, "png")
    tmxrasterizer $TmxName $PngName
}

$LastFrame = (Get-ChildItem "output" -Filter *.tmx -Name|Select-String -Pattern "\d+" -AllMatches | % { $_.Matches.Value }|Measure-Object -Max).Maximum
ffmpeg -y  -i "output/output.%d.png" -crf 0 -vf "loop=50:1:$LastFrame" output.webm
$DeBroglie="..\..\DeBroglie.Console\bin\Debug\DeBroglie.Console.exe"
Get-ChildItem "." -Filter *.json |
Foreach-Object {
    if ($_.Name -ne "schema.json") {
        & $DeBroglie $_
    }
}
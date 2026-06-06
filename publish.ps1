Set-Location -LiteralPath $PSScriptRoot
& "C:\Program Files\dotnet\dotnet.exe" publish -c Release -r win-x64 `
  /p:PublishSingleFile=true `
  /p:SelfContained=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:PublishReadyToRun=true `
  /p:EnableCompressionInSingleFile=true

Write-Host ""
Write-Host "Publish complete. Output: bin\Release\net8.0-windows\win-x64\publish\BrightTime.exe"

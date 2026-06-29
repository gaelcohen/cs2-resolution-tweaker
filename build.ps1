# Compila CS2Toggle.exe usando el csc.exe incluido en Windows (.NET Framework).
# No requiere instalar nada.

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$src = Join-Path $PSScriptRoot "CS2Toggle.cs"
$out = Join-Path $PSScriptRoot "CS2Toggle.exe"

if (-not (Test-Path $csc)) {
    Write-Host "No se encontro csc.exe en $csc" -ForegroundColor Red
    exit 1
}

& $csc /target:winexe /platform:x64 /out:"$out" `
    /reference:System.Windows.Forms.dll `
    /reference:System.Drawing.dll `
    "$src"

if (Test-Path $out) {
    Write-Host "OK -> $out" -ForegroundColor Green
} else {
    Write-Host "Fallo la compilacion." -ForegroundColor Red
    exit 1
}

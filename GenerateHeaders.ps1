# Add-FileHeaders.ps1

$author = "Callen Betts Virott"
$project = "Earth Engine"
$year = (Get-Date).Year

$files = Get-ChildItem -Recurse -Filter *.cs | Where-Object {
    $_.Name -notmatch '\.g\.cs$' -and
    $_.Name -notmatch '\.Designer\.cs$' -and
    $_.DirectoryName -notmatch '\\(bin|obj)\\'
}

foreach ($file in $files) {
    try {
        $content = Get-Content $file.FullName -Raw

        if ($content -match "Copyright") {
            Write-Host "Skipping (already has header): $($file.FullName)"
            continue
        }

        $fileName = $file.Name

        $header = @"
/// -----------------------------------------------------------------------------
/// <Project>      $project 
/// <File>         $fileName
/// <Author>       $author 
/// <Copyright>    @$year $author. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------
"@ + "`r`n"

        # Write to temp file first for safety
        $tempPath = "$($file.FullName).tmp"

        Set-Content -Path $tempPath -Value ($header + "`r`n" + $content) -Encoding utf8

        # Overwrite only if successful
        Move-Item -Force $tempPath $file.FullName

        Write-Host "✅ Header added to: $($file.FullName)"
    }
    catch {
        Write-Warning "❌ Error processing $($file.FullName): $_"
    }
}

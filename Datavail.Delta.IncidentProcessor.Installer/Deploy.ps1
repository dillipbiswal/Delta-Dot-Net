Get-Location | Write-Host
Write-Host $OctopusPackageDirectoryPath

$app = Get-WmiObject -Class Win32_Product | Where-Object { 
    $_.Name -match "Delta Incident Processor" 
}

if($app)
{
$app.Uninstall()
}

& .\Datavail.Delta.IncidentProcessor.Installer.msi /qn
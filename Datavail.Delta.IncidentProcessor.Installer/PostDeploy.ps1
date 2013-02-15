Start-Sleep -s 30
Stop-Service DeltaIp

# get the full path and file name of the App.config file in the same directory as this script
$appConfigFile = [IO.Path]::Combine(${env:ProgramFiles}, 'Datavail\Delta Incident Processor\Datavail.Delta.IncidentProcessor.exe.config')

# initialize the xml object
$appConfig = New-Object XML

# load the config file as an xml object
$appConfig.Load($appConfigFile)

# iterate over the settings
foreach($connectionString in $appConfig.configuration.connectionStrings.add)
{
	if ($connectionString.key -match "Datavail.Delta.Repository.EfWithMigrations.DeltaDbContext")
	{
		$connectionString.connectionString = $ConfigDbConnectionString
	}

	if ($connectionString.key -match "QueuesConnectionString")
	{
		$connectionString.connectionString = $QueuesDbConnectionString
	}    
}

$appConfig.configuration.log4net.root.level.value = $LogLevel

foreach($appSetting in $appConfig.configuration.appSettings.add)
{
	if ($appSetting.key -match "IsCheckInProcessor")
	{
		$appSetting.value= $IsCheckInProcessor
	}

	if ($appSetting.key -match "NumberOfWorkerThreads")
	{
		$appSetting.value= $NumberOfWorkerThreads
	}	
}

# save the updated config file
$appConfig.Save($appConfigFile)

Stop-Service DeltaIp	
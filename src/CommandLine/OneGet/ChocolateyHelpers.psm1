function Get-BinRoot {
	return $request.GetChocolateyBinRoot();
}

function Install-ChocolateyPackage {
<#
	.SYNOPSIS
	Installs a package

	.DESCRIPTION
	This will download a file from a url and install it on your machine.

	.PARAMETER PackageName
	The name of the package we want to download - this is arbitrary, call it whatever you want.
	It's recommended you call it the same as your nuget package id.

	.PARAMETER FileType
	This is the extension of the file. This should be either exe or msi.

	.PARAMETER SilentArgs
	OPTIONAL - These are the parameters to pass to the native installer.
	Try any of these to get the silent installer - /s /S /q /Q /quiet /silent /SILENT /VERYSILENT
	With msi it is always /quiet. Please pass it in still but it will be overridden by chocolatey to /quiet.
	If you don't pass anything it will invoke the installer with out any arguments. That means a nonsilent installer.

	Please include the notSilent tag in your chocolatey nuget package if you are not setting up a silent package.

	.PARAMETER Url
	This is the url to download the file from.

	.PARAMETER Url64bit
	OPTIONAL - If there is an x64 installer to download, please include it here. If not, delete this parameter

	.EXAMPLE
	Install-ChocolateyPackage '__NAME__' 'EXE_OR_MSI' 'SILENT_ARGS' 'URL' '64BIT_URL_DELETE_IF_NO_64BIT'

	.OUTPUTS
	None

	.NOTES
	This helper reduces the number of lines one would have to write to download and install a file to 1 line.
	This method has error handling built into it.

	.LINK
	Get-ChocolateyWebFile
	Install-ChocolateyInstallPackage
#>
	param(
		[string] $packageName,
		[string] $fileType = 'exe',
		[string] $silentArgs = '',
		[string] $url,
		[string] $url64bit = '',
		$validExitCodes = @(0)
	)
	return $request.InstallChocolateyPackage( $packageName, $fileType , $silentArgs , $url, $url64bit , $validExitCodes , (get-location) );
}

function Install-ChocolateyZipPackage  {
<#
	.SYNOPSIS
	Downloads and unzips a package

	.DESCRIPTION
	This will download a file from a url and unzip it on your machine.

	.PARAMETER PackageName
	The name of the package we want to download - this is arbitrary, call it whatever you want.
	It's recommended you call it the same as your nuget package id.

	.PARAMETER Url
	This is the url to download the file from. 

	.PARAMETER Url64bit
	OPTIONAL - If there is an x64 installer to download, please include it here. If not, delete this parameter

	.PARAMETER UnzipLocation
	This is a location to unzip the contents to, most likely your script folder.

	.EXAMPLE
	Install-ChocolateyZipPackage '__NAME__' 'URL' "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

	.OUTPUTS
	None

	.NOTES
	This helper reduces the number of lines one would have to write to download and unzip a file to 1 line.
	This method has error handling built into it.

	.LINK
	  Get-ChocolateyWebFile
	  Get-ChocolateyUnzip
#>
	param(
		[string] $packageName, 
		[string] $url,
		[string] $unzipLocation,
		[string] $url64bit = $url,
		[string] $specificFolder =""
	)
	return $request.InstallChocolateyZipPackage($packageName, $url, $unzipLocation, $url64bit , $specificFolder , (get-location) );
}


function Install-ChocolateyPowershellCommand {
<#
	.Synopsis
	
	.Description
	this will install a powershell script as a command on your system.
	Like an executable can be run from a batch redirect, this will do the same,
	calling powershell with this command and passing your arguments to it.
	If you include a url, it will first download the powershell file.
	Has error handling built in.
	You do not need to surround this with try catch if it is the only thing in your chocolateyInstall.ps1.

	.Parameter packageName

	.Parameter psFileFullPath

	.Parameter url
#>
	param(
		[string] $packageName,
		[string] $psFileFullPath, 
		[string] $url ='',
		[string] $url64bit = $url
	)
	return $request.InstallChocolateyPowershellCommand( $packageName, $psFileFullPath, $url, $url64bit , (get-location) );
}

function Install-ChocolateyVsixPackage {
<#
	.SYNOPSIS
	Downloads and installs a VSIX package for Visual Studio

	.PARAMETER PackageName
	The name of the package we want to download - this is 
	arbitrary, call it whatever you want. It's recommended 
	you call it the same as your nuget package id.

	.PARAMETER VsixUrl
	The URL of the package to be installed

	.PARAMETER VsVersion
	The Major version number of Visual Studio where the 
	package should be installed. This is optional. If not 
	specified, the most recent Visual Studio installation 
	will be targetted.

	.EXAMPLE
	Install-ChocolateyVsixPackage "MyPackage" http://visualstudiogallery.msdn.microsoft.com/ea3a37c9-1c76-4628-803e-b10a109e7943/file/73131/1/AutoWrockTestable.vsix

	This downloads the AutoWrockTestable VSIX from the Visual Studio Gallery and installs it to the latest version of VS.

	.EXAMPLE
	Install-ChocolateyVsixPackage "MyPackage" http://visualstudiogallery.msdn.microsoft.com/ea3a37c9-1c76-4628-803e-b10a109e7943/file/73131/1/AutoWrockTestable.vsix 11

	This downloads the AutoWrockTestable VSIX from the Visual Studio Gallery and installs it to Visual Studio 2012 (v11.0). 

	.NOTES
	VSIX packages are Extensions for the Visual Studio IDE. 
	The Visual Sudio Gallery at 
	http://visualstudiogallery.msdn.microsoft.com/ is the 
	public extension feed and hosts thousands of extensions. 
	You can locate a VSIX Url by finding the download link 
	of Visual Studio extensions on the Visual Studio Gallery.

#>
	param(
		[string]$packageName,
		[string]$vsixUrl,
		[int]$vsVersion=0
	)
	return $request.InstallChocolateyVsixPackage( $packageName, $vsixUrl, $vsVersion  );
}

function Start-ChocolateyProcessAsAdmin {
<#
	.Synopsis
	
	.Description
	Runs a process as an administrator.
	If $exeToRun is not specified, it is run with powershell.

	.Parameter statements

	.Parameter exeToRun
	.Parameter minimized,
	.Parameter noSleep
	.Parameter validExitCodes
#>
	param(
		[string] $statements, 
		[string] $exeToRun = 'powershell',
		[switch] $minimized,
		[switch] $noSleep,
		$validExitCodes = @(0)
	)
	if( !$request.StartChocolateyProcessAsAdmin( $statements, $exeToRun, $minimized , $noSleep, $validExitCodes, (get-location) ) ) {
		throw "Process Failed"
	}
}

function Install-ChocolateyInstallPackage {
<#
	.SYNOPSIS
	Installs a package

	.DESCRIPTION
	This will run an installer (local file) on your machine.

	.PARAMETER PackageName
	The name of the package - this is arbitrary, call it whatever you want.
	It's recommended you call it the same as your nuget package id.

	.PARAMETER FileType
	This is the extension of the file. This should be either exe or msi.

	.PARAMETER SilentArgs
	OPTIONAL - These are the parameters to pass to the native installer.
	Try any of these to get the silent installer - /s /S /q /Q /quiet /silent /SILENT /VERYSILENT
	With msi it is always /quiet.
	If you don't pass anything it will invoke the installer with out any arguments. That means a nonsilent installer.

	Please include the notSilent tag in your chocolatey nuget package if you are not setting up a silent package.

	.PARAMETER File
	The full path to the native installer to run

	.EXAMPLE
	Install-ChocolateyInstallPackage '__NAME__' 'EXE_OR_MSI' 'SILENT_ARGS' 'FilePath'

	.OUTPUTS
	None

	.NOTES
	This helper reduces the number of lines one would have to write to run an installer to 1 line.
	There is no error handling built into this method.

	.LINK
	Install-ChocolateyPackage
#>
	param(
		[string] $packageName, 
		[string] $fileType = 'exe',
		[string] $silentArgs = '',
		[string] $file,
		$validExitCodes = @(0)
	)
	return $request.InstallChocolateyInstallPackage($packageName, $fileType, $silentArgs, $file, $validExitCodes , (get-location));
}


function Install-ChocolateyPath {
<#
	.Synopsis
	
	.Description
	This puts a directory on the PATH environment variable.
	This is used when the application/tool is not being linked by chocolatey (not in the lib folder).

	.Parameter pathToInstall

	.Parameter pathType
#>
	param(
		[string] $pathToInstall,
		$pathType
	)
	if ($pathType -eq [System.EnvironmentVariableTarget]::Machine  ) {
		$pathType = "machine"
	} else {
		$pathType = "user"
	}

	return $request.InstallChocolateyPath($pathToInstall, $pathType );
}


function Install-ChocolateyEnvironmentVariable {
<#
	.SYNOPSIS
	Creates a persistent environment variable

	.DESCRIPTION
	Install-ChocolateyEnvironmentVariable creates an environment variable 
	with the specified name and value. The variable is persistent and 
	will remain after reboots and accross multiple powershell and command 
	line sessions. The variable can be scoped either to the user or to 
	the machine. If machine level scoping is specified, the comand is 
	elevated to an administrative session.

	.PARAMETER variableName
	The name or key of the environment variable

	.PARAMETER variableValue
	A string value assigned to the above name.

	.PARAMETER variableType
	Specifies whether this variable is to be accesible at either the 
	individual user level or at the Machine level.

	.EXAMPLE
	Install-ChocolateyEnvironmentVariable "JAVA_HOME" "d:\oracle\jdk\bin"
	Creates a User environmet variable "JAVA_HOME" pointing to 
	"d:\oracle\jdk\bin".

	.EXAMPLE
	Install-ChocolateyEnvironmentVariable "_NT_SYMBOL_PATH" "symsrv*symsrv.dll*f:\localsymbols*http://msdl.microsoft.com/download/symbols" Machine
	Creates a User environmet variable "_NT_SYMBOL_PATH" pointing to 
	"symsrv*symsrv.dll*f:\localsymbols*http://msdl.microsoft.com/download/symbols". 
	The command will be elevated to admin priviledges.

#>
	param(
		[string] $variableName,
		[string] $variableValue,
		$variableType = [System.EnvironmentVariableTarget]::User
	)
	if ($variableType -eq [System.EnvironmentVariableTarget]::Machine  ) {
		$variableType = "machine"
	}
	if( $variableType -eq "machine" ) {
		if( !$request.IsElevated() ) {
			Start-ChocolateyProcessAsAdmin "Install-ChocolateyEnvironmentVariable `'$variableName`' `'$variableValue`' `'$variableType`'"
			return $True;
		}
	}
	return $request.SetEnvironmentVariable( $variableName , $variableValue , $variableType );
}
function Install-ChocolateyExplorerMenuItem {
<#
	.SYNOPSIS
	Creates a windows explorer context menu item that can be associated with a command

	.DESCRIPTION
	Install-ChocolateyExplorerMenuItem can add an entry in the context menu of 
	Windows Explorer. The menu item is given a text label and a command. The command 
	can be any command accepted on the windows command line. The menu item can be 
	applied to either folder items or file items.

	Because this command accesses and edits the root class registry node, it will be 
	elevated to admin.

	.PARAMETER MenuKey
	A unique string to identify this menu item in the registry

	.PARAMETER MenuLabel
	The string that will be displayed in the context menu

	.PARAMETER Command
	A command line command that will be invoked when the menu item is selected

	.PARAMETER Type
	Specifies if the menu item should be applied to a folder or a file

	.EXAMPLE
	C:\PS>$sublimeDir = (Get-ChildItem $env:systemdrive\chocolatey\lib\sublimetext* | select $_.last)
	C:\PS>$sublimeExe = "$sublimeDir\tools\sublime_text.exe"
	C:\PS>Install-ChocolateyExplorerMenuItem "sublime" "Open with Sublime Text 2" $sublimeExe

	This will create a context menu item in Windows Explorer when any file is right clicked. The menu item will appear with the text "Open with Sublime Text 2" and will invoke sublime text 2 when selected.
	.EXAMPLE
	C:\PS>$sublimeDir = (Get-ChildItem $env:systemdrive\chocolatey\lib\sublimetext* | select $_.last)
	C:\PS>$sublimeExe = "$sublimeDir\tools\sublime_text.exe"
	C:\PS>Install-ChocolateyExplorerMenuItem "sublime" "Open with Sublime Text 2" $sublimeExe "directory"

	This will create a context menu item in Windows Explorer when any folder is right clicked. The menu item will appear with the text "Open with Sublime Text 2" and will invoke sublime text 2 when selected.

	.NOTES
	Chocolatey will automatically add the path of the file or folder clicked to the command. This is done simply by appending a %1 to the end of the command.
#>
	param(
		[string]$menuKey, 
		[string]$menuLabel, 
		[string]$command, 
		[ValidateSet('file','directory')]
		[string]$type = "file"
	)
	return $request.InstallChocolateyExplorerMenuItem( $menuKey , $menuLabel , $command , $type );
}

function Uninstall-ChocolateyPackage {
<#
	.SYNOPSIS
	Uninstalls a package

	.DESCRIPTION
	This will uninstall a package on your machine.

	.PARAMETER PackageName
	The name of the package 

	.PARAMETER FileType
	This is the extension of the file. This should be either exe or msi.

	.PARAMETER SilentArgs
	Please include the notSilent tag in your chocolatey nuget package if you are not setting up a silent package.

	.PARAMETER File
	The full path to the native uninstaller to run

	.EXAMPLE
	Uninstall-ChocolateyPackage '__NAME__' 'EXE_OR_MSI' 'SILENT_ARGS' 'FilePath'

	.OUTPUTS
	None

	.NOTES
	This helper reduces the number of lines one would have to write to run an uninstaller to 1 line.
	There is no error handling built into this method.

	.LINK
	Uninstall-ChocolateyPackage
#>
	param(
		[string] $packageName, 
		[string] $fileType = 'exe',
		[string] $silentArgs = '',
		[string] $file,
		$validExitCodes = @(0)
	)
	return $request.UninstallChocolateyPackage($packageName, $fileType, $silentArgs, $file, $validExitCodes, (get-location));
}


function UnInstall-ChocolateyZipPackage {
<#
	.SYNOPSIS
	UnInstalls a previous installed zip package

	.DESCRIPTION
	This will uninstall a zip file if installed via Install-ChocolateyZipPackage

	.PARAMETER PackageName
	The name of the package the zip file is associated with

	.PARAMETER ZipFileName
	This is the zip filename originally installed.

	.EXAMPLE
	UnInstall-ChocolateyZipPackage '__NAME__' 'filename.zip' 

	.OUTPUTS
	None

	.NOTES
	This helper reduces the number of lines one would have to remove the files extracted from a previously installed zip file.
	This method has error handling built into it.
#>
	param(
		[string] $packageName, 
		[string] $zipFileName
	)
	return $request.UnInstallChocolateyZipPackage( $packageName, $zipFileName );
}

function Install-ChocolateyFileAssociation {
<#
.SYNOPSIS
Creates an association between a file extension and a executable

.DESCRIPTION
Install-ChocolateyFileAssociation can associate a file extension 
with a downloaded application. Once this command has created an 
association, all invocations of files with the specified extension 
will be opened via the executable specified.

This command will run with elevated privileges.

.PARAMETER Extension
The file extension to be associated.

.PARAMETER Executable
The path to the application's executable to be associated.

.EXAMPLE
C:\PS>$sublimeDir = (Get-ChildItem $env:systemdrive\chocolatey\lib\sublimetext* | select $_.last)
C:\PS>$sublimeExe = "$sublimeDir\tools\sublime_text.exe"
C:\PS>Install-ChocolateyFileAssociation ".txt" $sublimeExe

This will create an association between Sublime Text 2 and all .txt files. Any .txt file opened will by default open with Sublime Text 2.

#>
	param(
		[string] $extension,
		[string] $executable
	)
	return $request.InstallChocolateyFileAssociation( $extension, $executable );
}


function Update-SessionEnvironment {
<#
	.Synopsis
	
	.Description
	Refreshes the current powershell session with all environment settings possibly performed by chocolatey package installs.

#>
	$request.UpdateSessionEnvironment();
}
		
function Get-ChocolateyWebFile {
<#
.SYNOPSIS
Downloads a file from the internets.

.DESCRIPTION
This will download a file from a url, tracking with a progress bar.
It returns the filepath to the downloaded file when it is complete.

.PARAMETER PackageName
The name of the package we want to download - this is arbitrary, call it whatever you want.
It's recommended you call it the same as your nuget package id.

.PARAMETER FileFullPath
This is the full path of the resulting file name.

.PARAMETER Url
This is the url to download the file from.

.PARAMETER Url64bit
OPTIONAL - If there is an x64 installer to download, please include it here. If not, delete this parameter

.EXAMPLE
Get-ChocolateyWebFile '__NAME__' 'C:\somepath\somename.exe' 'URL' '64BIT_URL_DELETE_IF_NO_64BIT'

.NOTES
This helper reduces the number of lines one would have to write to download a file to 1 line.
There is no error handling built into this method.

.LINK
Install-ChocolateyPackage
#>
	param(
	  [string] $packageName,
	  [string] $fileFullPath,
	  [string] $url,
	  [string] $url64bit = ''
	)
	return $request.GetChocolateyWebFile($packageName, $fileFullPath , $url, $url64bit );
}

function Get-ChocolateyUnzip {
<#
	.SYNOPSIS
	Unzips a .zip file and returns the location for further processing.

	.DESCRIPTION
	This unzips files using the native windows unzipper.

	.PARAMETER FileFullPath
	This is the full path to your zip file.

	.PARAMETER Destination
	This is a directory where you would like the unzipped files to end up.

	.PARAMETER SpecificFolder
	OPTIONAL - This is a specific directory within zip file to extract.

	.PARAMETER PackageName
	OPTIONAL - This will faciliate logging unzip activity for subsequent uninstall

	.EXAMPLE
	$scriptPath = (Split-Path -parent $MyInvocation.MyCommand.Definition)
	Get-ChocolateyUnzip "c:\someFile.zip" $scriptPath somedirinzip\somedirinzip

	.OUTPUTS
	Returns the passed in $destination.

	.NOTES
	This helper reduces the number of lines one would have to write to unzip a file to 1 line.
	There is no error handling built into this method.

#>
	param(
		[string] $fileFullPath, 
		[string] $destination,
		[string] $specificFolder,
		[string] $packageName
	)
	return $request.GetChocolateyUnzip( $fileFullPath , $destination , $specificFolder , $packageName );
}


function Install-ChocolateyDesktopLink {
<#
	.Synopsis
	
	.Description
	This adds a shortcut on the desktop to the specified file path.

	.Parameter targetFilePath
#>
	param(
		 [string] $targetFilePath
	)
	$desktopFolder = $request.GetKnownFolder( "Desktop" );
	$linkName = "$([System.IO.Path]::GetFileName($targetFilePath)).lnk"
	$link = Join-Path $desktop $linkName 
	$workingDirectory = $([System.IO.Path]::GetDirectoryName($targetFilePath))
	if( !$request.CreateShortcutLink( $link, $targetFilePath , "", $workingDirectory , "" ) ) {
		throw "Failed";
	}
}

function Install-ChocolateyPinnedTaskBarItem {
<#
	.SYNOPSIS
	Creates an item in the task bar linking to the provided path.

	.PARAMETER TargetFilePath
	The path to the application that should be launched when clicking on the task bar icon.

	.EXAMPLE
	Install-ChocolateyPinnedTaskBarItem "${env:ProgramFiles(x86)}\Microsoft Visual Studio 11.0\Common7\IDE\devenv.exe"

	This will create a Visual Studio task bar icon.

#>
	param(
		[string] $targetFilePath
	)
	return $request.InstallChocolateyPinnedTaskBarItem($targetFilePath);
}

function Get-ProcessorBits {
<#
	.SYNOPSIS
	Get the system architecture address width.

	.DESCRIPTION
	This will return the system architecture address width (probably 32 or 64 bit).

	.PARAMETER compare
	This optional parameter causes the function _to return $True or $False, depending on wether or not the bitwidth matches.

	.NOTES
	When your installation script has to know what architecture it is run on, this simple function _comes in handy.
#>
	param(
		$compare # You can optionally pass a value to compare the system architecture and receive $True or $False in stead of 32|64|nn
	)
	if ("$compare" -ne '' ) {
		if ("$compare" -eq '64' ) {
			return  [System.Environment]::Is64BitOperatingSystem
		} 

		if ("$compare" -eq '32' ) {
			return  ![System.Environment]::Is64BitOperatingSystem
		}
		return $False;
	} 
	if ( [System.Environment]::Is64BitOperatingSystem ) {
		return 64;
	}
	return 32
}


function Write-Debug {
	param(
		[Parameter(Position=0,Mandatory=$false,ValueFromPipeline=$true)][object] $Message,
		[Parameter()][switch] $NoNewLine,                              # Deprecating -- messages must go thru OneGet's channels
		[Parameter(Mandatory=$false)][ConsoleColor] $ForegroundColor,  # Deprecating -- messages must go thru OneGet's channels
		[Parameter(Mandatory=$false)][ConsoleColor] $BackgroundColor,  # Deprecating -- messages must go thru OneGet's channels
		[Parameter(Mandatory=$false)][Object] $Separator    
	)
	# just forward to the OneGet Verbose channel. (Chocolatey didn't mean 'debug')
	$request.Verbose(  $Message , $Separator );
}

function Write-Verbose {
	param(
		[Parameter(Position=0,Mandatory=$false,ValueFromPipeline=$true)][object] $Message,
		[Parameter()][switch] $NoNewLine,                              # Deprecating -- messages must go thru OneGet's channels
		[Parameter(Mandatory=$false)][ConsoleColor] $ForegroundColor,  # Deprecating -- messages must go thru OneGet's channels
		[Parameter(Mandatory=$false)][ConsoleColor] $BackgroundColor,  # Deprecating -- messages must go thru OneGet's channels
		[Parameter(Mandatory=$false)][Object] $Separator    
	)
	# just forward to the OneGet Verbose channel. (Chocolatey didn't mean 'debug')
	$request.Verbose(  $Message , $Separator );
}

function Write-Error {
	param(
		[Parameter(Position=0,Mandatory=$true,ValueFromPipeline=$true)][string] $Message='',
		[Parameter(Mandatory=$false)][System.Management.Automation.ErrorCategory] $Category = '',
		[Parameter(Mandatory=$false)][string] $ErrorId,
		[Parameter(Mandatory=$false)][object] $TargetObject,
		[Parameter(Mandatory=$false)][string] $CategoryActivity,
		[Parameter(Mandatory=$false)][string] $CategoryReason,
		[Parameter(Mandatory=$false)][string] $CategoryTargetName,
		[Parameter(Mandatory=$false)][string] $CategoryTargetType,
		[Parameter(Mandatory=$false)][string] $RecommendedAction
	)
	# forward to the OneGet error channel.
  	$request.Error( $Category, $Message );

	# todo: what are we doing with the rest of the parameters? More Messages?
}

function Write-Host {
	param(
		[Parameter(Position=0,Mandatory=$false,ValueFromPipeline=$true, ValueFromRemainingArguments=$true)][object] $Object,
		[Parameter()][switch] $NoNewLine,								# Deprecating -- messages must go thru OneGet's channels
		[Parameter(Mandatory=$false)][ConsoleColor] $ForegroundColor,   # Deprecating -- messages must go thru OneGet's channels
		[Parameter(Mandatory=$false)][ConsoleColor] $BackgroundColor,   # Deprecating -- messages must go thru OneGet's channels
		[Parameter(Mandatory=$false)][Object] $Separator
	)
	# just forward to the OneGet Verbose channel.
	# Writing objects is reseved for output.
	$request.Verbose(  $Message , $Separator );
}

function Write-ChocolateySuccess {
	param(
		[string] $packageName
	)
	$request.Verbose( "Installation Successful", $packageName  );
}


function Write-ChocolateyFailure {
<#
	.Synopsis
	
	.Description
	Notes an unsuccessful chocolatey install.

	.Parameter packageName

	.Parameter failureMessage
#>
	param(
		[string] $packageName,
		[string] $failureMessage
	)
	$request.Error( "FAILURE" , "$packageName, $failureMessage"  );
}

function Get-EnvironmentVar {
	param(
		$key, 
		$scope
	) 
	return [Environment]::GetEnvironmentVariable($key, $scope)
}

function Generate-BinFile {
	param(
		[string] $name,
		[string] $path,
		[switch] $useStart
	)
	if( $useStart ) {
		return $request.GenerateGuiBin( $path, $name  );
	}
	return $request.GenerateConsoleBin( $path, $name );
}

function Remove-BinFile {
	param(
		[string] $name, 
		[string] $path
	)
	return $request.RemoveConsoleBin( $path, $name );
}

function Write-FileUpdateLog {
	param (
		[string] $logFilePath,
		[string] $locationToMonitor,
		[scriptblock] $scriptToRun
	)

	$snapshot = $request.SnapshotFolder( $locationToMonitor );

	& $scriptToRun

	$snapshot.WriteFileDiffLog( $logFilePath );
}

function Get-WebFile {
	param(
		$url = '', #(Read-Host "The URL to download"),
		$fileName = $null,
		$userAgent = 'chocolatey command line',
		[switch]$Passthru,
		[switch]$quiet
	)
	return $request.GetWebFile( $url, $fileName, $userAgent, $passthru, $quiet );
}

function Get-FtpFile {
	param(
		$url = '', #(Read-Host "The URL to download"),
		$fileName = $null,
		$username = $null,
		$password = $null,
		[switch]$quiet
	) 
	return $request.GetFtpFile( $url, $fileName, $username, $password, $quiet );
}
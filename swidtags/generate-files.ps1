
function process-file {
    param( $source , $dest , $version , $baseurl )
    $content = ( get-content $source -raw ) -replace '\+NUGET_VERSION\+',$version 
    $content = ( $content ) -replace '\+ROOT\+',$baseurl
    set-content $dest $content
}

ipmo coapp

if( test-path .\generated ) {
    erase -recurse .\generated
}

mkdir .\generated

$baseUrl = "https://oneget.org/"
$file = "..\..\output\v40\AnyCPU\Release\bin\Merged\NuGet-AnyCPU.exe"
$version = ([System.Diagnostics.FileVersionInfo]::GetVersionInfo( $file ).FileVersion)

copy $file .\generated\nuget-anycpu-$version.exe 

process-file .\nuget.swidtag.master .\generated\nuget-$version.swidtag $version $baseUrl
process-file .\chocolatey.swidtag.master .\generated\chocolatey-$version.swidtag  $version $baseUrl
process-file .\providers.swidtag.master .\generated\providers.swidtag  $version $baseUrl


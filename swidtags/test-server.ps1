
function Send-TextResponse {
    param ($response, $content)
    
    $buffer = [System.Text.Encoding]::UTF8.GetBytes($content)
    $response.ContentLength64 = $buffer.Length
    $response.OutputStream.Write($buffer, 0, $buffer.Length)
}

function Send-FileResponse {
  param ($response, $file)
    $content =  (get-content $file -raw -encoding byte)
    $response.ContentLength64 = $content.Length
    $response.OutputStream.Write($content, 0, $content.Length)
}

function Get-NuGetVersion {
    $nuget = "..\..\output\v40\AnyCPU\Release\bin\Merged\NuGet-AnyCPU.exe"
    $version = ([System.Diagnostics.FileVersionInfo]::GetVersionInfo( $nuget ).FileVersion)
    return $version
}


$url = 'http://localhost:81/'
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($url)
$listener.Start()
 
$base = (pwd).Path + "/"  
 
Write-Host "Listening at $url..."
 
 
while ($listener.IsListening)
{
    $context = $listener.GetContext()
    $requestUrl = $context.Request.Url
    $response = $context.Response
 
    Write-Host ''
    Write-Host "> $requestUrl"
 
    $localPath = $requestUrl.LocalPath
    $localPath = ($localPath) -replace '//','/' 
    
    if ($localPath -eq "/quit" ) {
        $listener.Stop()
        return;
    }
    
    $nuget_version = Get-NuGetVersion
    
    if( $localPath.startsWith( "/nuget-$nuget_version") ) {
        $localPath = "/nuget.swidtag"
    }
    
    if( $localPath.startsWith( "/chocolatey-$nuget_version") ) {
        $localPath = "/chocolatey.swidtag"
    }
    
    $filePath = $base + $localPath 
    
    
    
    if( test-path $filePath ) {
        $filePath = (resolve-path $filePath).Path
        Send-FileResponse  $response $filePath
    } else {
        # try .master
    
        if( test-path ($filePath+".master") ) {
            $filePath = (resolve-path ($filePath+".master") ).Path
            
            $swid = ( get-content $filePath -raw ) -replace '\+NUGET_VERSION\+',$nuget_version 
            $swid = ($swid) -replace '\+ROOT\+',$url
            
            Write-host $swid 
            Send-TextResponse $response $swid
        }
        else {
            # nuget-anycpu-+NUGET_VERSION+.exe
            if( $localPath.startsWith( "/nuget-anycpu" ) ) {
                Send-FileResponse $response "..\..\output\v40\AnyCPU\Release\bin\Merged\NuGet-AnyCPU.exe"
            } else {            
        
            Send-TextResponse $response "no file $localPath"
            }
        }        
    }
    
    $response.Close()
 
    $responseStatus = $response.StatusCode
    Write-Host "< $responseStatus"
}
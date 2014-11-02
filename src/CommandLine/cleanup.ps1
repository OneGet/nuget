

# MAKE SURE YOU SET EXECUTION POLICY UNRSTRICTED TO BOTH X86 and X64 shells:
# elevated: 
# %SystemRoot%\syswow64\WindowsPowerShell\v1.0\powershell.exe set-executionpolicy unrestricted
# %SystemRoot%\system32\WindowsPowerShell\v1.0\powershell.exe set-executionpolicy unrestricted

#kill any processes holding nuget-anycpu open.
(handle nuget-anycpu.exe | convertfrom-string).p3 | foreach { pskill $_}

erase $env:ALLUSERSPROFILE\OneGet\ProviderAssemblies\nuget-anycpu.* -force
erase $env:LOCALAPPDATA\OneGet\ProviderAssemblies\nuget-anycpu.* -force
    
    
sleep 2
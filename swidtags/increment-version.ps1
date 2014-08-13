# increments the version in the commonassemblyinfo.cs file

$file = "..\common\CommonAssemblyInfo.cs"
$content = get-content -raw $file

$c = (sls -pattern 'AssemblyVersion..(\d+)\.(\d+)\.(\d+)\.(\d+)' -InputObject $content)

$major = $c.Matches[0].Groups[1].Value
$minor = $c.Matches[0].Groups[2].Value
$release = $c.Matches[0].Groups[3].Value
$build = $c.Matches[0].Groups[4].Value

$next = 1 + $build

$last = "$major.$minor.$release.$build"
$newver = "$major.$minor.$release.$next"

$newcontent = $content -replace $last,$newver

set-content $file $newcontent

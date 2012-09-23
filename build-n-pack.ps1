properties {
	$base_dir  = resolve-path .
	$build_dir = "$base_dir\build"
	$global:configuration = "Release"
	$env:buildlabel = "0.3"
}

task default -depends DoRelease

task DoRelease -depends CleanOutputDirectory, `
	CreateOutputDirectories, `
	Compile, `
	CreateNugetPackages {	
	Write-Host "Done"
}

task Clean {
	Remove-Item -force -recurse $build_dir -ErrorAction SilentlyContinue
}

task Init -depends Clean {
	New-Item $build_dir -itemType directory -ErrorAction SilentlyContinue | Out-Null
	New-Item $build_dir\net35 -itemType directory -ErrorAction SilentlyContinue | Out-Null
	New-Item $build_dir\net40 -itemType directory -ErrorAction SilentlyContinue | Out-Null
}

task Compile -depends Init {
	
	$v4_net_version = (ls "$env:windir\Microsoft.NET\Framework\v4.0*").Name
	
	exec { &"C:\Windows\Microsoft.NET\Framework\$v4_net_version\MSBuild.exe" "$sln_file" /p:OutDir="$build_dir\net40\" /p:Configuration=$global:configuration }
	exec { &"C:\Windows\Microsoft.NET\Framework\$v4_net_version\MSBuild.exe" "$sln_file" /p:OutDir="$build_dir\net35\" /p:ToolsVersion=3.5 /p:Configuration=$global:configuration }
}

task CreateOutputDirectories {
	New-Item $build_dir -itemType directory -ErrorAction SilentlyContinue | Out-Null
	New-Item $build_dir\net35 -itemType directory -ErrorAction SilentlyContinue | Out-Null
	New-Item $build_dir\net40 -itemType directory -ErrorAction SilentlyContinue | Out-Null
}

task CleanOutputDirectory { 
	Remove-Item $build_dir -Recurse -Force -ErrorAction SilentlyContinue
}

task CopyRootFiles {
	cp $base_dir\license.txt $build_dir\Output\license.txt
	cp $base_dir\readme.txt $build_dir\Output\readme.txt
}

task CreateNugetPackages -depends Compile {

	Remove-Item $base_dir\*.nupkg
	
	$nuget_dir = "$build_dir\NuGet_packaging"
	Remove-Item $nuget_dir -Force -Recurse -ErrorAction SilentlyContinue
	New-Item $nuget_dir -Type directory | Out-Null
	
	New-Item $nuget_dir\Spatial4n.Core\lib\net40 -Type directory | Out-Null
	New-Item $nuget_dir\Spatial4n.Core\lib\net20 -Type directory | Out-Null
	New-Item $nuget_dir\Spatial4n.Core.NTS\lib\net40 -Type directory | Out-Null
	New-Item $nuget_dir\Spatial4n.Core.NTS\lib\net20 -Type directory | Out-Null
	Copy-Item $base_dir\.nuget\Spatial4n.Core.nuspec $nuget_dir\Spatial4n.Core\Spatial4n.Core.nuspec
	Copy-Item $base_dir\.nuget\Spatial4n.Core.NTS.nuspec $nuget_dir\Spatial4n.Core.NTS\Spatial4n.Core.NTS.nuspec
	
	@("Spatial4n.Core.???") |% { Copy-Item "$build_dir\net40\$_" $nuget_dir\Spatial4n.Core\lib\net40 }
	@("Spatial4n.Core.NTS.???") |% { Copy-Item "$build_dir\net40\$_" $nuget_dir\Spatial4n.Core.NTS\lib\net40 }

	@("Spatial4n.Core.???") |% { Copy-Item "$build_dir\net35\$_" $nuget_dir\Spatial4n.Core\lib\net20 }
	@("Spatial4n.Core.NTS.???") |% { Copy-Item "$build_dir\net35\$_" $nuget_dir\Spatial4n.Core.NTS\lib\net20 }
	
	$packages = Get-ChildItem $nuget_dir *.nuspec -recurse
	$packages |% { 
		$nuspec = [xml](Get-Content $_.FullName)
		$nuspec.package.metadata.version = $env:buildlabel
		$nuspec | Select-Xml '//dependency' |% {
			if($_.Node.Id.StartsWith('Spatial4n')){
				$_.Node.Version = "[$env:buildlabel]"
			}
		}
		$nuspec.Save($_.FullName);
		&"$base_dir\.nuget\nuget.exe" pack $_.FullName
	}
	
	# Upload packages
	$accessPath = "$base_dir\..\Nuget-Access-Key.txt"
	if ( (Test-Path $accessPath) ) {
		$accessKey = Get-Content $accessPath
		$accessKey = $accessKey.Trim()
		
		# Push to nuget repository
		$packages | ForEach-Object {
			&"$base_dir\.nuget\NuGet.exe" push "$($_.BaseName).$env:buildlabel.nupkg" $accessKey
		}
	}
	else {
		Write-Host "Nuget-Access-Key.txt does not exit. Cannot publish the nuget package." -ForegroundColor Red
	}
}

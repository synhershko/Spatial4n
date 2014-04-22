#!/bin/sh -x

mono --runtime=v4.0 .nuget/NuGet.exe install xunit.runners -Version 1.9.2 -o packages

runTest(){
    mono --runtime=v4.0 packages/xunit.runners.1.9.2/tools/xunit.console.exe $@
   if [ $? -ne 0 ]
   then   
     exit 1
   fi
}

#This is the call that runs the tests and adds tweakable arguments.
runTest $1

exit $?
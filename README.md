# update-local-nuget-cache
A utility to update the local cache of the installed nuget packages, to help with quick testing of new dlls.

## To install, run:
```
dotnet tool install -g update-local-nuget-cache
```

## To run
In the PostBuild event of your Visual Studio projects, add:
```
update-local-nuget-cache $(ProjectDir)
```

It will then do the following:

1. Identify the name of the package for the project
2. Detect the local nuget cache folder which is **C:\Users\{User}\.nuget\packages\{PackageName}**
3. If it does not exist, end the process.
4. Otherwise, finds the latest installed version directory
5. Inside its lib folder, it will replace the DLL file (in the appropriate sub-folder)

## Remarks
This allows local applications that use the nuget package, to automatically get the updated DLL when they are rebuilt.
This shortens the testing cycle and provides a convinient mechanism to test new versions of the package DLLs locally.

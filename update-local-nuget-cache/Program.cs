using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace update_local_nuget_cache
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Syntax error\nShould be called like this:\nupdate-local-nuget-cache $(ProjectPath) $(OutputPath) $(ProjectName)");
                return 1;
            }

            var projectPath = args[0];
            var projectDir = Path.GetDirectoryName(projectPath);
            var outputFile = args[1];
            var projectName = args[2];
            var nuspecPath = Path.Combine(projectDir, "Package.nuspec");
            var hasNuSpec = File.Exists(nuspecPath);

            var csProjXml = XElement.Load(projectPath);

            // we get it like this since other property groups might exist with similar elements like version
            var propertyGroup = csProjXml.Descendants("OutputType").First().Parent;
            var targetFramework = propertyGroup.Element("TargetFramework")?.Value;
            var version = "";
            var packageId = "";

            if (!hasNuSpec)
            {
                version = propertyGroup.Element("Version")?.Value;
                packageId = propertyGroup.Element("PackageId")?.Value;
                if (string.IsNullOrEmpty(version))
                    version = "1.0.0";
                if (string.IsNullOrEmpty(packageId))
                    packageId = projectName;
            }
            else
            {
                var nuSpecXml = XElement.Load(nuspecPath);
                // The nuspec file has a namespace so we need to use it in our operations
                var ns = nuSpecXml.GetDefaultNamespace();
                var metadata = nuSpecXml.Descendants(ns + "metadata").First();
                version = metadata.Element(ns + "version").Value;
                packageId = metadata.Element(ns + "id").Value;
            }

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var nuGetCache = userProfile + "\\.nuget\\packages\\";
            var packageFolder = nuGetCache + packageId + "\\" + version + "\\lib\\" + targetFramework;

            if (!Directory.Exists(packageFolder))
            {
                Console.WriteLine("did not exist : " + packageFolder);
                return 0;
            }

            var fileName = Path.GetFileName(outputFile);
            var nugetFilePath = Path.Combine(packageFolder, fileName);

            if (File.Exists(nugetFilePath))
            {
                File.Delete(nugetFilePath);
            }

            File.Copy(outputFile, Path.Combine(packageFolder, fileName));
            return 0;
        }
    }
}
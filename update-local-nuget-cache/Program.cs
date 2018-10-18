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
                Console.WriteLine("The project directory should be sent");
                return 1;
            }

            var projectPath = args[0];
            var outputFile = args[1];
            var projectName = args[2];

            var data = XElement.Load(projectPath);
            Console.WriteLine("---");

            // we get it like this since other property groups might exist with similar elements like version
            var propertyGroup = data.Descendants("OutputType").First().Parent;
            var version = propertyGroup.Element("Version")?.Value;
            var targetFramework = propertyGroup.Element("TargetFramework")?.Value;
            var packageId = propertyGroup.Element("PackageId")?.Value;
            if (string.IsNullOrEmpty(version))
                version = "1.0.0";
            if (string.IsNullOrEmpty(packageId))
                packageId = projectName;

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var nuGetCache = userProfile + "\\.nuget\\packages\\";
            var packageFolder = nuGetCache + packageId + "\\" + version + "\\lib\\" + targetFramework;

            if (!Directory.Exists(packageFolder))
            {
                Console.WriteLine("did not exist : " + packageFolder);
                return 0;
            }

            Console.WriteLine("***");
            Console.WriteLine(packageFolder);
            var index = outputFile.LastIndexOf("\\");
            Console.WriteLine(index);
            var fileName = outputFile.Substring(index + 1, outputFile.Length - (index + 1));
            var nugetFilePath = packageFolder + "\\" + fileName;
            Console.WriteLine("**");
            Console.WriteLine(nugetFilePath);
            if (File.Exists(nugetFilePath))
            {
                File.Delete(nugetFilePath);
            }

            File.Copy(outputFile, packageFolder + "\\" + fileName);
            return 0;
        }
    }
}
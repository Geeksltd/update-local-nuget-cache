using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Olive;

namespace update_local_nuget_cache
{
    class Program
    {
        static FileInfo Dll, Pdb, CsProj;
        static DirectoryInfo LocalNugetCacheFolder;
        static string TargetFramework;

        static FileInfo FindCsProj()
        {
            var parent = Dll.Directory;

            while (parent.Root != parent)
            {
                CsProj = parent.GetFiles("*.csproj")
                  .OrderByDescending(v => v.Name.ToLower() == Dll.Name.Substring(0, Dll.Name.Length - 3) + "csproj")
                  .FirstOrDefault();

                if (CsProj != null) return CsProj;
                parent = parent.Parent;
            }

            return null;
        }

        static void FindFiles(string arg)
        {
            Dll = arg.AsFile();
            if (!Dll.Exists()) throw new Exception("File not found: " + Dll.FullName);
            TargetFramework = Dll.Directory.Name;

            Pdb = Dll.Directory.GetFile(Dll.NameWithoutExtension() + ".pdb");

            if (FindCsProj() == null)
                throw new Exception("CsProj file not found in the parents of " + Dll.FullName);
        }

        static void FindLocalNugetCache()
        {
            var dotnet = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                .AsDirectory().GetSubDirectory("dotnet").GetFile("dotnet.exe");

            LocalNugetCacheFolder = dotnet.Execute("nuget locals global-packages -l").ToLines().Trim()
                 .Select(v => v.TrimStart("global-packages: "))
                 .Select(v => v.AsDirectory())
                 .FirstOrDefault();

            if (LocalNugetCacheFolder is null) throw new Exception("Nuget cache not found!");
        }

        static void Main(string[] args)
        {
            if (args.Length != 1)
                throw new Exception("Syntax: update-local-nuget-cache $(DllPath)");

            FindFiles(args.First());



            FindLocalNugetCache();

            var csProjXml = XElement.Load(CsProj.FullName);
            var propertyGroup = csProjXml.Descendants("TargetFramework").First().Parent;
            var packageId = propertyGroup.Element("PackageId").Value;

            var folder = LocalNugetCacheFolder.GetSubDirectory(packageId);

            if (!folder.Exists())
            {
                Console.WriteLine("Local installation not found: " + folder.FullName);
                return;
            }

            foreach (var version in folder.GetDirectories())
            {
                var target = version.GetSubDirectory("lib\\" + TargetFramework);

                if (!target.Exists())
                {
                    Console.WriteLine("Expected folder does not exists: " + target.FullName);
                    continue;
                }

                var targetFile = target.GetFile(Dll.Name);
                if (!targetFile.Exists())
                {
                    Console.WriteLine("File folder does not exists: " + targetFile.FullName);
                    continue;
                }

                Console.WriteLine("Copying to: " + targetFile.FullName);

                Dll.CopyTo(targetFile, overwrite: true);
                if (Pdb.Exists()) Pdb.CopyTo(targetFile.Directory.GetFile(Pdb.Name), overwrite: true);
            }
        }
    }
}
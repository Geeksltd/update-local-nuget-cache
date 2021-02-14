using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Olive;

namespace update_local_nuget_cache
{
    class Program
    {
        static DirectoryInfo Root;

        static void Main(string[] args)
        {
            Root = FindRoot(args.FirstOrDefault());

            foreach (var version in GetLocalCacheVersions(FindPackageId()))
                Deploy(version);
        }

        static DirectoryInfo FindRoot(string firstArg)
        {
            if (firstArg.IsEmpty())
                firstArg = Environment.CurrentDirectory;

            DirectoryInfo result;

            if (firstArg.AsFile().Exists())
            {
                result = FindParentDebugRelease(firstArg.AsFile().Directory);
                return result ?? throw new Exception("No debug or release folder found for: " + firstArg);
            }

            result = firstArg.AsDirectory();
            if (!result.Exists()) throw new Exception("Directory not found: " + firstArg);

            var parent = FindParentDebugRelease(result);
            if (parent != null) return parent;

            result = result.GetSubDirectory("bin");
            if (!result.Exists()) throw new Exception("Not found: " + result.FullName);

            return new[] { result.GetSubDirectory("debug"), result.GetSubDirectory("release") }
            .FirstOrDefault(x => x.Exists())
            ?? throw new Exception("Didn't find debug or release in " + result.FullName);
        }

        static DirectoryInfo FindParentDebugRelease(DirectoryInfo folder)
        {
            while (true)
            {
                if (folder.Name.ToLower().IsAnyOf("debug", "release")) return folder;
                folder = folder.Parent;
                if (folder.FullName == folder.Root.FullName) return null;
            }
        }

        static void Deploy(DirectoryInfo destination)
        {
            foreach (var target in destination.GetDirectories())
            {
                var newTarget = Root.GetDirectories()
                    .FirstOrDefault(x => x.Name.ToLower().Remove(".") == target.Name.ToLower().Remove("."));

                if (!newTarget.Exists())
                {
                    Console.WriteLine("Target " + target.Name + " not found in " + Root.FullName);
                    continue;
                }

                foreach (var file in target.GetFiles())
                {
                    var here = newTarget.GetFile(file.Name);
                    if (!here.Exists())
                    {
                        Console.WriteLine("Not found: " + here.FullName);
                    }
                    else
                    {
                        here.CopyTo(file, overwrite: true);
                        Console.WriteLine("Updated " + file.FullName);
                    }
                }
            }
        }

        static string FindPackageId()
        {
            var csProjXml = XElement.Load(FindCsProj().FullName);
            return csProjXml.Descendants("PackageId").FirstOrDefault()?.Value
                ?? throw new Exception("PackageId node not found in " + FindCsProj().FullName);
        }

        static FileInfo FindCsProj()
        {
            var parent = Root;

            while (parent.Root.FullName != parent.FullName)
            {
                var result = parent.GetFiles("*.csproj").WithMax(v => v.LastWriteTimeUtc);

                if (result != null) return result;
                parent = parent.Parent;
            }

            throw new Exception("Csproj file not found for: " + Root.FullName);
        }

        static DirectoryInfo[] GetLocalCacheVersions(string packageId)
        {
            var folder = FindLocalNugetCache().GetSubDirectory(packageId);

            if (!folder.Exists())
            {
                Console.WriteLine("Local installation not found: " + folder.FullName);
                return new DirectoryInfo[0];
            }

            return folder.GetDirectories().Select(v => v.GetSubDirectory("lib")).Where(v => v.Exists()).ToArray();
        }

        static DirectoryInfo FindLocalNugetCache()
        {
            var dotnet = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                .AsDirectory().GetSubDirectory("dotnet").GetFile("dotnet.exe");

            return dotnet.Execute("nuget locals global-packages -l").ToLines().Trim()
                 .Select(v => v.TrimStart("global-packages: "))
                 .Select(v => v.AsDirectory())
                 .FirstOrDefault()
                 ?? throw new Exception("Nuget cache not found!");
        }
    }
}
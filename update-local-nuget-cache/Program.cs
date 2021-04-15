using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Olive;

namespace update_local_nuget_cache
{
    class Program
    {
        static DirectoryInfo Debug, Folder;
        static FileInfo CsProj, Dll;
        static string PackageId;

        static void Main(string[] args)
        {
            Folder = args.Select(v => v.AsDirectory()).FirstOrDefault(v => v.Exists());
            CsProj = args.Where(v => v.EndsWith(".csproj")).Select(v => v.AsFile()).FirstOrDefault(v => v.Exists());
            Dll = args.Where(v => v.EndsWith(".dll")).Select(v => v.AsFile()).FirstOrDefault(v => v.Exists());

            FindDebug();
            FindPackageId();

            foreach (var version in GetLocalCacheVersions())
            {
                try { Deploy(version); }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed: " + version.FullName);
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                }
            }
        }

        static void FindDebug()
        {
            if (Dll != null)
            {
                Debug = FindParentDebugRelease(Dll.Directory)
                    ?? throw new Exception("No debug or release folder found for: " + Dll.FullName);
                return;
            }

            if (CsProj != null)
            {
                Debug = FindChildDebugRelease(CsProj.Directory);
                if (Debug != null) return;
            }

            if (Folder == null) Folder = Environment.CurrentDirectory.AsDirectory();

            Debug = FindChildDebugRelease(Folder) ?? FindParentDebugRelease(Folder);

            if (Debug == null)
                throw new Exception("Didn't find debug or release folder.");
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

        static DirectoryInfo FindChildDebugRelease(DirectoryInfo folder)
        {
            folder = folder.GetSubDirectory("bin");
            if (!folder.Exists()) return null;

            return new[] { folder.GetSubDirectory("debug"), folder.GetSubDirectory("release") }
            .FirstOrDefault(x => x.Exists());
        }

        static void Deploy(DirectoryInfo destination)
        {
            foreach (var target in destination.GetDirectories())
            {
                var newTarget = Debug.GetDirectories()
                    .FirstOrDefault(x => x.Name.ToLower().Remove(".") == target.Name.ToLower().Remove("."));

                if (!newTarget.Exists())
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Target " + target.Name + " not found in " + Debug.FullName);
                    Console.ResetColor();
                    continue;
                }

                var targetFiles = target.GetFiles().ToList();

                void AppendIfNotExist(string fileName)
                {
                    var file = target.GetFile(fileName);
                    if (targetFiles.All(x => x.Name != file.Name)) targetFiles.Add(file);
                }

                AppendIfNotExist($"{PackageId}.pdb");
                AppendIfNotExist($"{PackageId}.xml");

                foreach (var file in targetFiles)
                {
                    var here = newTarget.GetFile(file.Name);
                    if (!here.Exists())
                    {
                        Console.WriteLine("Not found: " + here.FullName);
                    }
                    else
                    {
                        here.CopyTo(file, overwrite: true);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Updated " + file.FullName);
                        Console.ResetColor();
                    }
                }
            }
        }

        static void FindPackageId()
        {
            var csProjXml = XElement.Load(FindCsProj().FullName);
            PackageId = csProjXml.Descendants("PackageId").FirstOrDefault()?.Value
                ?? throw new Exception("PackageId node not found in " + FindCsProj().FullName);
        }

        static FileInfo FindCsProj()
        {
            if (CsProj != null) return CsProj;

            var parent = Debug;

            while (parent.Root.FullName != parent.FullName)
            {
                var result = parent.GetFiles("*.csproj").WithMax(v => v.LastWriteTimeUtc);

                if (result != null) return result;
                parent = parent.Parent;
            }

            throw new Exception("Csproj file not found for: " + Debug.FullName);
        }

        static DirectoryInfo[] GetLocalCacheVersions()
        {
            var folder = FindLocalNugetCache().GetSubDirectory(PackageId);

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
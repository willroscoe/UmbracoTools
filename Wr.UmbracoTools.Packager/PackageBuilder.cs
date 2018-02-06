using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Wr.UmbracoTools.Packager
{
    public class PackageBuilder : IDisposable
    {
        private ZipArchive archive;
        private XDocument _packageFile;
        private string _pathToProjectRoot;

        private List<string> _filesAdded = new List<string>();

        public PackageBuilder(XDocument packageFile, string destination, string version, FilesAction filesAction = FilesAction.Overwrite)
        {
            _packageFile = packageFile;
            _pathToProjectRoot = Directory.GetCurrentDirectory();

            if (string.IsNullOrEmpty(version)) // no version number passed in, so try and get from the package.xml file.
            {
                try
                {
                    version = _packageFile.Element("umbPackage").Element("info").Element("package").Element("version").Value;
                    Console.WriteLine($"version found in package.xml file: {version}");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"version not found in package.xml file: {ex.Message}");
                }
            }
            else // a version number has been passed in, so update the package.xml with it
            {
                try
                {
                    _packageFile.Element("umbPackage").Element("info").Element("package").Element("version").Value = version;
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"version not found in package.xml file: {ex.Message}");
                }
            }

            // update output filename with version, if required
            if (destination.Contains("{version}"))
            {
                destination = destination.Replace("{version}", version);
            }

            if (filesAction == FilesAction.Overwrite)
            {
                // remove any exisiting files in the exisiting package.xml file
                _packageFile.Descendants().Where(e=>e.Name == "file").Remove();
            }

            var stream = new FileStream(destination, FileMode.Create);
            archive = new ZipArchive(stream, ZipArchiveMode.Create, false);
        }

        public void AddDll(string filename)
        {
            if (filename.StartsWith("/") || filename.StartsWith(@"\"))
                filename = filename.Remove(0,1); // remove the first character

            var file = new FileInfo(filename);
            if (!file.Exists)
                throw new InvalidOperationException($"The requested file {filename} does not exist");

            Console.WriteLine($"File Fullname {file.FullName}.");

            using (var fileStream = file.Open(FileMode.Open))
            {
                AddFile(file, fileStream, "/bin");
            }
        }

        public void AddPluginFolder(string path)
        {
            if (path.StartsWith("/") || path.StartsWith(@"\"))
                path = path.Remove(0,1); // remove the first character

            Console.WriteLine($"AddPluginFolder Path: {path}");

            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory)) // is Directory
            {
                var dirInfo = new DirectoryInfo(path);
                Console.WriteLine($"AddPluginFolder dirInfo: {dirInfo.Name}");

                var files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    Console.WriteLine($"Adding {file.FullName}");
                    using (var fileStream = file.Open(FileMode.Open))
                    {
                        AddFile(file, fileStream);
                    }
                }
            }
            else // is file
            {
                var file = new FileInfo(path);
                Console.WriteLine($"Adding {file.FullName}.");
                using (var fileStream = file.Open(FileMode.Open))
                {
                    AddFile(file, fileStream);
                }
            }
        }

        private void AddFile(FileInfo file, Stream fileStream, string overridePath = "")
        {
            if (!_filesAdded.Contains(file.FullName)) // don't add duplicate files
            {
                var pathToFileRelativeToProjectRoot = overridePath;
                if (string.IsNullOrEmpty(pathToFileRelativeToProjectRoot))
                {
                    pathToFileRelativeToProjectRoot = Path.GetDirectoryName(file.FullName).Replace(_pathToProjectRoot, "").Replace(@"\", "/");
                }

                var filesNode = _packageFile.Descendants("files").First();
                filesNode.Add(
                    new XElement("file",
                        new XElement("guid", file.Name),
                        new XElement("orgPath", pathToFileRelativeToProjectRoot),
                        new XElement("orgName", file.Name)
                ));

                AddEntryToArchive(file.Name, fileStream.CopyTo);

                _filesAdded.Add(file.FullName);
            }
        }

        private void AddEntryToArchive(string entryName, Action<Stream> streamAction)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using (var entryStream = entry.Open())
            {
                streamAction(entryStream);
            }
        }

        public void Done()
        {
            AddEntryToArchive("Package.xml", _packageFile.Save);
        }

        public void Dispose()
        {
            ((IDisposable)archive).Dispose();
        }
    }
}

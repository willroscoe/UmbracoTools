/*
Based on https://github.com/EndzoneSoftware/Umbraco.Tools/tree/master/src/Umbraco.Tools.Package

To build release: dotnet publish -c release --self-contained --runtime win10-x64

Example usage: Wr.UmbracoTools.Packager -set phonemanager/packagefiles.json -ver 1.2.2 -out phonemanager.{version}.zip
*/

using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Wr.UmbracoTools.Packager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CommandLineApplication app = new CommandLineApplication(throwOnUnexpectedArg: true);
            app.Name = "Umbraco package builder";
            app.Description = "Builds Umbraco package .zip file based on a settings .json file and package files.";
            var arg_SettingsFilePath = app.Option("-set |--settingsfilepath", "Path to settings json file", CommandOptionType.SingleValue);
            var arg_Version = app.Option("-ver |--version", "Set the package version. If not set then the version field in the package.xml will be used.", CommandOptionType.SingleValue);
            var arg_OutputFile = app.Option("-out |--outputfile", "Path and filename for the outputed file, relative to the settings .json file. Use optional placeholder {version} in the filename to automatically add the package version number.", CommandOptionType.SingleValue);
            var arg_DoNotOverwriteFilesInPackageXml = app.Option("-notover | --notoverwrite", "Do not overwrite the files already in the package.xml file. Append.", CommandOptionType.NoValue);

            var outputFilePathAndName = "package.zip";
            var outputVersion = string.Empty;
            FilesAction filesAction = FilesAction.Overwrite;

            app.HelpOption("-? | -hh | --hhelp");
            app.OnExecute(() =>
            {
                if (arg_SettingsFilePath.HasValue())
                {
                    if (File.Exists(arg_SettingsFilePath.Value()))
                    {
                        var configText = File.OpenText(arg_SettingsFilePath.Value()).ReadToEnd(); // load settings .json file
                        var config = (ApplicationSettings)JsonConvert.DeserializeObject(configText, typeof(ApplicationSettings), new JsonSerializerSettings()
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        });
                        Console.WriteLine("ProjectRoot: " + config.ProjectRoot);
                        Console.WriteLine("Dlls: " + string.Join(",", config.Dlls));
                        Console.WriteLine("PackageXmlTemplate: " + config.PackageXmlTemplate);
                        Console.WriteLine("IncludeFoldersOrFiles:" + (config.IncludeFoldersOrFiles.Any() ? string.Empty : " None"));
                        foreach (var folder in config.IncludeFoldersOrFiles)
                        {
                            Console.WriteLine($"  {folder}");
                        }

                        Console.WriteLine();
                        Console.WriteLine("Processing...");
                        Console.WriteLine();

                        var settingsDirectory = new FileInfo(arg_SettingsFilePath.Value()).Directory.FullName;
                        Directory.SetCurrentDirectory(settingsDirectory);
                        Console.WriteLine($"settingsDirectory:  {settingsDirectory}");

                        if (arg_Version.HasValue())
                        {
                            outputVersion = arg_Version.Value();
                            Console.WriteLine("outputVersion: " + outputVersion);
                        }

                        if (arg_OutputFile.HasValue())
                        {
                            outputFilePathAndName = arg_OutputFile.Value();
                            Console.WriteLine("outputFilePathAndName: " + outputFilePathAndName);
                        }

                        if (arg_DoNotOverwriteFilesInPackageXml.HasValue())
                        {
                            filesAction = FilesAction.Append;
                        }

                        // load package file from this directory
                        var packageFile = XDocument.Load(config.PackageXmlTemplate);

                        // set the current directory to the 'projectRoot' property in the package settings .json file, as the paths within are relative to the it
                        if (!settingsDirectory.EndsWith("/"))
                            settingsDirectory = settingsDirectory + "/"; // fix path concatination issue

                        if (!config.ProjectRoot.EndsWith("/"))
                            config.ProjectRoot = config.ProjectRoot + "/";
                            
                        var projectRootDirectory = new FileInfo(Path.GetFullPath(settingsDirectory + config.ProjectRoot)).Directory.FullName;
                        Console.WriteLine($"projectRootDirectory:  {projectRootDirectory}");
                        Directory.SetCurrentDirectory(projectRootDirectory);

                        using (var builder = new PackageBuilder(packageFile, outputFilePathAndName, outputVersion, filesAction))
                        {
                            foreach (var dll in config.Dlls)
                            {
                                Console.WriteLine($"Adding {dll}.");
                                builder.AddDll(dll);
                            }

                            if (config.IncludeFoldersOrFiles.Any())
                            {
                                foreach (var folder in config.IncludeFoldersOrFiles)
                                {
                                    Console.WriteLine($"Processing {folder}.");
                                    builder.AddPluginFolder(folder);
                                }
                            }

                            builder.Done();
                        }

                        Console.WriteLine();
                        Console.WriteLine("All done");
                    }
                    else
                    {
                        Console.WriteLine(@"The settings json file could not be found at: " + arg_SettingsFilePath.Value());
                        Console.WriteLine();
                        Console.WriteLine(@"Usage: package settings.json 
Output: a zip file with the package for Umbraco
settings.json structure:
{
    projectRoot: PATH_TO_PROJECT_ROOT,
    dlls: ['PATH_TO_DLL1', 'PATH_TO_DLL2'],
    packageXmlTemplate: 'PATH_TO_PACKAGE.XML'
    includeFoldersOrFiles: ['PATH_TO_PLUGIN', 'PATH_TO_XSLT']
}");
                        Console.WriteLine();
                        Console.WriteLine("Note: Except packageXmlTemplate, all paths are relative to the project root.");
                        Environment.Exit(-1); // Do not continue
                    }
                }
                return 0;
            });

            app.Execute(args);
        }
    }

    public class ApplicationSettings
    {
        private string[] _includeFoldersOrFiles;
        private string[] _dlls;

        public string[] Dlls
        {
            get { return _dlls ?? (_dlls = new string[0]); }
            set { _dlls = value; }
        }

        public string PackageXmlTemplate { get; set; }

        public string ProjectRoot { get; set; }

        public string[] IncludeFoldersOrFiles
        {
            get { return _includeFoldersOrFiles ?? (_includeFoldersOrFiles = new string[0]); }
            set { _includeFoldersOrFiles = value; }
        }
    }

    public enum FilesAction
    {
        Append,
        Overwrite
    }
}
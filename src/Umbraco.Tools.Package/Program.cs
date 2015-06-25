﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.ConfigurationModel;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Serialization;

namespace Umbraco.Tools.Package
{
    public class ApplicationSettings
    {
        public string[] Dlls { get; set; }
        public string PackageXmlTemplate { get; set; }
        public string[] IncludeFolders { get; set; }
    }

    public class Program
    {
        public void Main(string[] args)
        {
            Console.WriteLine(@"
  ______ _   _ _____ __________  _   _ ______      
 |  ____| \ | |  __ \___  / __ \| \ | |  ____|     
 | |__  |  \| | |  | | / / |  | |  \| | |__        
 |  __| | . ` | |  | |/ /| |  | | . ` |  __|       
 | |____| |\  | |__| / /_| |__| | |\  | |____      
 |______|_| \_|_____/_____\____/|_| \_|______|___  
 | |  | |  \/  |  _ \|  __ \     /\   / ____/ __ \ 
 | |  | | \  / | |_) | |__) |   /  \ | |   | |  | |
 | |  | | |\/| |  _ <|  _  /   / /\ \| |   | |  | |
 | |__| | |  | | |_) | | \ \  / ____ \ |___| |__| |
  \____/|_|  |_|____/|_|  \_\/_/    \_\_____\____/ 
                                                   
                                                  
");

            if (args.Length != 1 || !File.Exists(args[0]))
            {
                Console.WriteLine(@"Usage: package settings.json 
Output: a zip file with the package for Umbraco
settings.json structure:
{
    dlls: ['DLL1', 'DLL2'],
    packageXmlTemplate: 'PATH_TO_PACKAGE.XML'
    includeFolders: ['PATH_TO_PLUGIN', 'PATH_TO_XSLT']
}");
                Console.ReadLine();
                return;
            }

            var configText = File.OpenText(args[0]).ReadToEnd();
            var config = (ApplicationSettings)JsonConvert.DeserializeObject(configText, typeof (ApplicationSettings), new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            Console.WriteLine("Dlls: " + string.Join(",", config.Dlls));
            Console.WriteLine("PackageXmlTemplate: " + config.PackageXmlTemplate);
            Console.WriteLine("IncludeFolders:" + (config.IncludeFolders.Any() ? string.Empty : " None"));
            foreach (var folder in config.IncludeFolders)
            {
                Console.WriteLine($"  {folder}");
            }

            Console.WriteLine();
            Console.WriteLine("Processing...");
            Console.WriteLine();

            using (var builder = new PackageBuilder(config.PackageXmlTemplate, "Package.zip"))
            {
                foreach (var dll in config.Dlls)
                {
                    Console.WriteLine($"Adding {dll}.");
                    builder.AddDll(dll);
                }

                foreach (var folder in config.IncludeFolders)
                {
                    Console.WriteLine($"Processing {folder}.");
                    builder.AddPluginFolder(folder);
                }

                builder.Done();
            }

            Console.WriteLine();
            Console.WriteLine("All done");
        }
    }
}

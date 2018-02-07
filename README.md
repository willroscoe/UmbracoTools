 UmbracoTools.Packager
======================

[![Build status](https://ci.appveyor.com/api/projects/status/3p1vyxh8vkrcy0si?svg=true)](https://ci.appveyor.com/project/willroscoe/umbracotools-packager)

A windows 10 x64 .net core 2.0 console program to automatically create an Umbraco package .zip file which can be installed in the Umbraco backoffice Packages section.

The requirements to use the program are:
- a json settings file. Note: **Except packageXmlTemplate, all paths are relative to the project root defined by  the 'projectRoot' field**
  ```
  {
    "projectRoot": "../../My.Project.Root",
    "dlls": [ "/bin/Our.Package.dll" ],
    "packageXmlTemplate": "../../Our.Package/Package.xml",
    "includeFoldersOrFiles": [ "/App_Plugins/phonemanager/manifest.package", "/App_Plugins/anotherplugin" ]
    }
    ```
- an Umbraco package.xml file (specified as 'packageXmlTemplate' in the settings json file), pre-populated with the required fields. If you include [file] nodes these will be replaced by the program unless you use the -notover flag.
- the umbraco package project files

**Usage:**

```Wr.UmbracoTools.Packager -set PATH_FILENAME_TO_SETTINGS_JSON_FILE -ver VERSION_NUMBER [OPTIONAL] -out OUTPUT_PATH_FILENAME [OPTIONAL] -notover [OPTIONAL]```

**Package version**

The version number will either come from being passed in to the program or if not, then if it is present in the package.xml file then this will be used. Either way all version fields will be updated.

Note: OUTPUT_PATH_FILENAME can include '{version}'. If it does, this will be replaced with the version number (see below). 

**Example Usage:**

```Wr.UmbracoTools.Packager -set phonemanager/packagesetting.json -ver 1.2.2 -out phonemanager.{version}.zip```

## Acknowledgements
This project is based on https://github.com/EndzoneSoftware/Umbraco.Tools/tree/master/src/Umbraco.Tools.Package


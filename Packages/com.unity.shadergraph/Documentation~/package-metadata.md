---
uid: package-metadata
---

# Per-package metadata

To set specific metadata values for a package create a `projectMetadata.json` file in the package's `Documentation~` folder. The values in this file override values set in the DocFX configuration file. 

The supported metadata includes:

```
{
    "_appTitle": "Unity Documentation",
    "enableTocForManual": true,
    "hideGlobalNamespace": false,
    "memberLayout": "separatePages",
    "includeAssembliesInSrc":[
        path/to/assembly/can/include/**/globs
    ],
    "_noIndex": true,
    "_customCopyrightNotice": "This is a custom copyright notice",
    "xref": [
        "https://docs.unity3d.com/Packages/com.unity.addressables@1.19/xrefmap.yml",
        "com.unity.xr.compositionlayers@0.1",
        "com.unity.timeline"],
    "use-dotnet-xrefmap": true,
    "enableNewTab": false,
    "_imageZoomThreshold": 1200,
    "showScriptRef": true,
    "renameSamplesFolder": true
}
```

| **Name** | **Description** |
| :--- | :--- |
| `_appTitle`  | A string appended to the HTML page title. Shows up prominently in search results. By default, this value is set to the package display name.|
| `enableTocForManual`| Enables or disables the table of contents on the left side of the page for the user manual section of the documentation. Has no effect on the script reference section. By default, this is set to `false` for single-page manuals and `true` for multi-page manuals. |
| `hideGlobalNamespace` | Hides any classes in the global namespace so they don't appear in the script reference. **Tip:** It's useful to leave this value set to `false` so that when you generate the documentation you can spot any erroneous APIs left in the global namespace.|
| `memberLayout`| Defaults to `samePage` which includes the docs for a type and its members on the same HTML page. Set this to `separatePages` to generate a separate HTML page for each member of a type.|
| `includeAssembliesInSrc` | Include symbols from one or more assemblies. Use this when you want to document API symbols for pre-built assemblies.|
| `_noIndex` | Inserts a "robots" meta tag to the header in the generated HTML with a value of "noindex" to ask search engines not to index the content.|
| `_customCopyrightNotice` | Inserts a custom copyright notice that replaces the the standard one (eg. Copyright © 2021 Unity Technologies).|
| `xref` | Adds additional xrefmap.yml files for packages. Use an absolute URL to a xrefmap.yml file on the web, a package name/id with or without version. If you don't specify a version, PMDT tries to find the latest version on Unity's doc site. Only package docs have public xrefmap files. |
| `use-dotnet-xrefmap` | Adds an additional [xrefmap for .NET xrefs](https://github.com/dotnet/docfx/issues/9659). Setting this to true adds significant build time and should not be necessary to resolve [.NET base class library types for API reference documentation](https://dotnet.github.io/docfx/docs/links-and-cross-references.html#cross-reference-to-net-basic-class-library) |
| `enableNewTab` | Controls whether links to URLs outside the current set of docs open in a new browser tab. The default is true. Set this metadata variable to false to override the default behavior for the project. If false, all links open in the same browser tab. |
| `_imageZoomThreshold` | Specify the width threshold above which we apply a link treatment to an image. This treatement adds a link to the image to open it full-size in a new browser tab and adds the zoom cursor to let the reader know that they can zoom. The default threshold is 1200 pixels. Set to 0 to disable the feature. |
| `showScriptRef` | Whether to show the link to the API Script Reference on the package docs title bar. Default is `true`. |
| `renameSamplesFolder` | Whether to copy the `Samples~` folder to `Samples` when building the docs. This lets you reference code snippets in `Samples` using a [code inclusion statement](xref:pmdt-user-manual-content#auto-code-compilation). Defaults to `true`. |

By default, the `_appTitle` field is set to `DEFAULT_APP_TITLE`, and the `_packageVersion` field is set to `DEFAULT_PACKAGE_VERSION` in the DocFX config file. These values are automatically updated for each package and combined in the HTML page template as `PACKAGE DISPLAY NAME | VERSION`. This combined string is used in the page title and breadcrumb trail.

>[!WARNING]
>Do not override the following metadata values:

| **Name** | **Description** |
| :--- | :--- |
| `_enableSearch` | Turning this off disables search, but doesn't remove the search box. |
| `_appLogoPath` | Sets the path to the logo graphic. |
| `_disableToc`  | Turns off the table of contents displayed on the left side of all pages. Turning this off makes it difficult to navigate through a multi-page documentation set. |
| `_packageVersion` | The package version string, which is appended to the `_appTitle` to become part of each html page title and the breadcrumb trail. Set to an empty string to suppress the version display. |

<a id="code-gen"></a>
<a name="code-generation"/>
## Code generation settings

`projectMetadata.json` in the package's `Documentation~` folder also supports parameters that are used for API documentation code project generation. These parameters are not passed to DocFX directly. This replaces [config.json](preprocessor-directives.md) in PMDT 3.0.0-preview and above.

| **Name** | **Description** |
| :--- | :--- |
| `pmdt-additional-preprocessors` | A semicolon-separated string of preprocessor directives to add to code projects for [API documentation generation](code-project-generation.md). `PACKAGE_DOCS_GENERATION` is added to these by default. |
| `pmdt-dotnet-framework` | A .net version number string for use in [API documentation generation](code-project-generation.md). The default value is `v4.7.1`. |

Example:

```
{
    "pmdt-additional-preprocessors": "MY_CONSTANT;MY_OTHER_CONSTANT",
    "pmdt-dotnet-framework": "v4.7.1"
}
```
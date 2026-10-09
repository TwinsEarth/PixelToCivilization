# Batch generation

>[!NOTE]
>This is an experimental feature. Batch adding packages has limitations. Some packages require user interactions the first time they are added and sometimes adding more than one package at a time generates errors. This is probably related to packages that share dependencies (or are dependencies). 

You can batch generate docs for a set of packages from either inside Unity or by passing command line parameters to the Unity executable.

## Use a Package Set Asset

To use a Package Set Asset, first create one with the Unity **Create Asset** menu (menu: **Assets > Create > Doc Tools > Create Package Set Asset**).

Next, select the new Asset to display its Inspector:

![Package Set Inspector](Images/PackageSetInspector.png)

Paste a list of packages names (including @version) into the text field.

You can optionally set a destination path to override the normal output path for the docs (which is `c:/temp` on Windows and inside a hidden `Library` folder on macOS). This can be helpful when generating multiple sets of documentation so you can view them in a local web server.

After batch generating a set of docs, you can find the error report in the `Logs/DocToolReports` directory of the Unity project folder (or from the Package Manager window for an individual package).

## Use command line arguments to Unity

You can use the `Batch` class to generate docs by passing command line arguments to Unity. To use batch generation, you must have a project that already contains the Package Manager Doc Tools package (since that is where the batch code exists).

There are three batch commands supported, and you can give each one or more package IDs as input:

- `AddPackages`: Installs the packages through the Package Manager. This must be run unless the packages are already installed in the project.
- `GenerateDocsWithStatus`: Generates the docs for the packages. The files are saved to the usual place, hidden in the bowels of your file system.
- `RemovePackages`: Removes one or more packages using the Package Manager. This step is optional.

These commands must be run as separate steps. Therefore, to add, generate the docs for, and then remove a set of packages, you must invoke Unity three times. An example invocation to do that in a bash shell is:

```bash
/Applications/Unity/Hub/Editor/2020.1.0b9/Unity.app/Contents/MacOS/Unity   -quit -batchmode -projectPath . -executeMethod UnityEditor.PackageManager.DocumentationTools.UI.Batch.AddPackages -packages="com.unity.entities@0.10.0-preview.6 com.unity.ugui@1.0.0 com.unity.remote-config@1.3.2-preview.1"
/Applications/Unity/Hub/Editor/2020.1.0b9/Unity.app/Contents/MacOS/Unity   -quit -batchmode -projectPath . -executeMethod UnityEditor.PackageManager.DocumentationTools.UI.Batch.GenerateDocs -packages="com.unity.entities@0.10.0-preview.6 com.unity.ugui@1.0.0 com.unity.remote-config@1.3.2-preview.1"
/Applications/Unity/Hub/Editor/2020.1.0b9/Unity.app/Contents/MacOS/Unity   -quit -batchmode -projectPath . -executeMethod UnityEditor.PackageManager.DocumentationTools.UI.Batch.RemovePackages -packages="com.unity.entities com.unity.ugui com.unity.remote-config"

```

(At the time this was written, the RemovePackages function only takes a package name, not the full ID, which is contrary to the documentation.)

## Command line argument examples

Command line arguments for some documentation generation options are listed [here](generate-documentation.md#package-manager-documentation-tools-settings). Examples include the following:

### Debug mode
```bash
/Applications/Unity/Hub/Editor/2020.1.0b9/Unity.app/Contents/MacOS/Unity -quit -batchmode -projectPath . -executeMethod UnityEditor.PackageManager.DocumentationTools.UI.Batch.AddPackages -packages="com.unity.entities@0.10.0-preview.6" -debug
```

### Use built-in code project generation
```bash
/Applications/Unity/Hub/Editor/2020.1.0b9/Unity.app/Contents/MacOS/Unity -quit -batchmode -projectPath . -executeMethod UnityEditor.PackageManager.DocumentationTools.UI.Batch.AddPackages -packages="com.unity.entities@0.10.0-preview.6" -builtInCSProj
```

### Use built-in code project generation and export API Metadata
```bash
/Applications/Unity/Hub/Editor/2020.1.0b9/Unity.app/Contents/MacOS/Unity -quit -batchmode -projectPath . -executeMethod UnityEditor.PackageManager.DocumentationTools.UI.Batch.AddPackages -packages="com.unity.entities@0.10.0-preview.6" -builtInCSProj -exportAPIMetadata
```
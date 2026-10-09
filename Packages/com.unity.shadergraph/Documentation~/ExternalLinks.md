---
uid: external-links
---

# Links

The Package Manager Doc Tools (PMDT) package supports uid-based markdown cross-reference syntax to create links to the Unity Manual, Script Reference, dependent packages, and links within a package:

```
<xref:uid>
[displayed text](xref:uid)
```

Where `uid` is a unique ID defined as follows:

* **Unity user manual:** Refer to [UIDs to core Unity Manual](manual-xrefs.md). 
* **Package user manual:** You must assign the uid at the top of the markdown page you want to link to:

    ```
    ---
    uid: uid-you-assigned-to-topic
    ---
    ```

* **Unity Script Reference:** Use the full qualified class name. The link text is the unqualified type name. For a full list of Script Reference UIDs refer to [UIDS to Script Reference](engine-api.md).
* **Package Script Reference:** Use the full qualified class name as the UID (with some transformations). UIDs for APIs that take `T` arguments, or which have multiple overloads usually are suffixed with `*` or `1`. To see a full list of uids for a package's documentation, [generate the documentation](generate-documentation.md) and then refer to the `xrefmap.yml` file.

You can use these cross-reference syntaxes in the current package and to dependent packages. These types of cross references aren't supported for links to arbitrary packages.

> [!NOTE]
> Prior to Unity 6, the UIDs for the Unity Manual were created by taking the page file name without extension. This system had the drawbacks that the UIDs would change if the filename changed and wasn't really universal enough to prevent UID collisions with other sets of documentation. In Unity 6, we added the ability to manually assign UIDs to Unity Manual pages and did so. The default UIDs are created by transforming the original page name to kebab-case with the `um-` prefix. For example, the page, "GettingStarted.md" would be assigned the UID, `um-getting-started`. Since the UIDs are manually assigned, there can -- and are -- exceptions to this format. You should look them up in [UIDs to core Unity Manual](manual-xrefs.md). Both the old form (the filename without extension) and the new form (manually assigned) work for all versions of Unity, but the UID lookup tables are kept in sync manually and will inevitably drift apart over time. In addition, the manually assigned UIDs will be transferred to Hexadocs, where the old style won't work any longer.

## When to use UIDs for links

Besides saving typing and clutter in your markdown text, links created this way link to a specific version of Unity or a dependent package without having to specify a version in the link text. That means when your package is updated to use a newer version of Unity or a dependent package, you don’t have to manually change the links before publishing the new docs. It also means that the docs for the old version still link to the correct, older versions of Unity and dependent packages, even though the versionless URLs for those things has moved on.

To link to the latest version of the Unity docs, no matter what version of Unity that has become, continue to use a normal href-based link, such as `[Unity Manual](https://docs.unity3d.com/Manual/index.html)`.

## Assign a UID to a markdown page

To assign a UID to a markdown file, place the following YAML snippet as the first few lines of the file:

```
---
uid: your-uid-name
---
```

Replacing `your-uid-name` with the uid you want to use. UIDs must be unique in a project. As a convention, use dashes to separate words.

## Links to non-dependant packages

To add xrefs to other packages, include the package name or URLs to their xrefmap files in the [projectMetadata.json](package-metadata.md) file. You can use:

* A full, absolute URL, such as `https://docs.unity3d.com/Packages/com.unity.addressables@1.19/xrefmap.yml`. Note that the `@latest` version of the package URL doesn't work here (because that isn't really a URL, its a JavaScript hack!). You must specify a version.
* A package name, with version, such as `com.unity.xr.compositionlayers@0.1`.
* A package name without version, such as `com.unity.timeline`. In this case, PMDT tries to find the latest public version.

Here's an example link to the latest Addressables topic, [Using Addressables at runtime](xref:addressable-runtime) created with `[Using Addressables at runtime](xref:addressable-runtime)`, where `addressable-runtime` is a uid in the Addressables package docs. This package adds the addressables doc links using the full, absolute version of the URL.

## Links to the Core Unity documentation

Links to constructor functions in the core script reference use the form: `Namespace.Type.ctor` and use the parameter list in the same fashion as any method. For example, `UnityEngine.AI.NavMeshData.ctor(System.Int32)` is the UID for the [NavMesh(int) constructor](xref:UnityEngine.AI.NavMeshData.ctor(System.Int32) method. (Do not include empty parentheses for constructors with zero parameters.)

> [!IMPORTANT]
> These pages are complete and accurate for the version of Unity used to generate the page you are now reading (see [Links to Unity Manual](manual-xrefs.md)) at the time the xrefmaps were last updated -- which is a manual process. You can find the xrefmap files used to make links to the Unity core docs in the Package Manager Doc Tools package source (inside the `df~` folder). These xrefmap files are generated using the Python scripts in the [UnityDocTools repo](https://github.com/Unity-Technologies/UnityDocTools/tree/master/Archives/UnityXRefMappers). 
>
> Also note that these files have been purged of NDA-restricted strings, even where those strings don't refer to the NDA item. Thus if you are looking for an item using one of those strings, you might not find it (e.g. the name of the thing commonly used to turn lights on or off is one such string).

## Links to .NET library types

Refer to [this xrefmap](https://github.com/dotnet/docfx/blob/main/.xrefmap.json) for .NET library type UIDs. `docfx` documentation states that:
> For API reference documentation, docfx automatically resolves .NET base class library types and other types published to https://learn.microsoft.com by default, without cross reference maps.

For xrefs outside of API reference documentation, PMDT automatically includes a subset of the full .NET xrefmap including the following namespaces:
* [System.String](xref:System.String)
* [System.Int16 - System.Int128](xref:System.Int64)
* [System.Single](xref:System.Single)
* [System.Double](xref:System.Double)
* [System.Boolean](xref:System.Boolean)
* [System.Byte](xref:System.Byte)

If the subset is not sufficient for your package, directly include the full xrefmap in your `docfx` project by setting `use-dotnet-xrefmap` to `true` in [projectMetadata.json](package-metadata.md). Use of this xrefmap increases build time in proportion to the amount of xrefs in the project. There is no working example of an xref of this type in the PMDT documentation because there are 36500 xrefs in this project, and adding the .NET xrefmap increases its build time by tens of minutes.

An example of an xref that will not resolve without the full map, and is not resolved in this documentation, is `[Arm Intrinsic](xref:System.Runtime.Intrinsics.Arm)`: [Arm Intrinsic](xref:System.Runtime.Intrinsics.Arm).

## Finding xrefs and UIDs

The UIDS used in xrefs to a package published with this PMDT and recent versions of the core Unity docs can be found in `xrefmap.yml` files that are placed in the root of the package's html site for each version. For example:

* Latest version of the PMDT package: [https://docs.unity3d.com/Packages/com.unity.package-manager-doctools@latest?subfolder=/xrefmap.yml](https://docs.unity3d.com/Packages/com.unity.package-manager-doctools@latest?subfolder=/xrefmap.yml) 
* 3.3 version of the PMDT package: [https://docs.unity3d.com/Packages/com.unity.package-manager-doctools@3.3/xrefmap.yml](https://docs.unity3d.com/Packages/com.unity.package-manager-doctools@3.3/xrefmap.yml) 
* Unity Manual: [https://docs.unity3d.com/Manual/xrefmap.yml](https://docs.unity3d.com/Manual/xrefmap.yml)
* Unity Script Reference: [https://docs.unity3d.com/ScriptReference/xrefmap.yml](https://docs.unity3d.com/ScriptReference/xrefmap.yml)

Indexes for the Xrefs for the Unity Manual and Script Reference are found in this set of documentation:

* [Manual page links](manual-xrefs.md) (Note that UIDs to Manual pages are file names of the pages themselves, without extension.)
* [UnityEngine APIS](engine-api.html)

### Get the XREF from a published page

You can get the xref string from a publish paged by ALT + double-clicking an element on the page. You must enable the feature in your browser first, and the package must be published with a new-enough version of the PMDT to support this feature.

To enable the feature, add a URL parameter, `xrefs=on` to the URL of a published page. When enabled, holding the ALT key and double-clicking will open an alert dialog in your browser offering to copy the xref string to the clipboard. Click **Okay** and you can then paste the xref somewhere useful.

To turn the utility on in this set of docs, for example, the URL looks like: [.../ExternalLinks.html?xrefs=on](xref:external-links?xrefs=on#get-the-xref-from-a-published-page).

To turn the utility off again, set `xrefs` to `off`: [.../ExternalLinks.html?xrefs=off](xref:external-links?xrefs=off#get-the-xref-from-a-published-page).

(The feature is a bit hidden to avoid annoying regular readers of the docs who might be using the same input combo for something else.)

> [!TIP]
> * The feature stays on until you turn it off again.
> * You can generate docs for an installed package locally and use that to get xrefs and UIDs that way.

## Testing links

Sometimes, if you are working on a set of packages that haven't been released or their docs haven't been published yet, the Doc Tools will report bad links from one package to another simply because it requires the target package docs to already exist at an accessible web site. 

When package docs are published, they must be published in the correct order for the links to work. That is, if package A depends on package B, the docs for B must be published and pushed live before the docs for A are generated. 

When generating docs for local testing before release, this isn't possible. To solve this, you can build the docs for any unpublished packages you need to link to and host them on a localhost web server. If PMDT can't find the requested version at the usual place on the web, it checks for the xrefmap on localhost. You can use this for any packages in the manifests of the package you are generating the docs for and also for any packages that you added to the `projectMetaData.json` file as additional xrefs. In this latter case, you must use the `package.id@x.y` form of the xref.  

Python offers an easy way to run a localhost web server. Assuming you can run Python from a command prompt, you can use the following (on Windows, macOS and Linux should be similar):

```
pushd "d:\docs" 
start chrome localhost
python -m http.server 80
popd
```

Change `d:\docs` to the directory at which you generate the docs (there's an "Output path" field for the Doc Tools in the Package Manager window: use the same value).

## Examples:

### Links to Unity manual:

```
* [A Unity Manual](xref:UnityManual)
* [](xref:UnityManual)
* <xref:UnityManual>
* <xref:ScriptCompilationAssemblyDefinitionFiles>
* @UnityManual
* @ScriptCompilationAssemblyDefinitionFiles
* @"UnityManual?text=Da Manual"
* @class-MeshRenderer#Lighting
* @"class-MeshRenderer?text=Lighting#Lighting"
```

* [A Unity Manual](xref:UnityManual)
* [](xref:UnityManual)
* <xref:UnityManual>
* <xref:ScriptCompilationAssemblyDefinitionFiles>
* @UnityManual
* @ScriptCompilationAssemblyDefinitionFiles
* @"UnityManual?text=Da Manual"
* @class-MeshRenderer#Lighting -- Note the link text is the Page title, not the heading title. You must use `?text=title` here:
* @"class-MeshRenderer?text=Lighting#Lighting"

### Links to the Unity Script Reference

```
* <xref:UnityEngine.MonoBehaviour>
* <xref:Unity.Jobs.JobHandle>
* @"Unity.Jobs.JobHandle?text=JobFoo"
```

* <xref:UnityEngine.MonoBehaviour>
* <xref:Unity.Jobs.JobHandle>
* @"Unity.Jobs.JobHandle?text=JobFoo"

## Caveats and limitations

The link form that uses the `@` symbol may be convenient, but if you mistype the uid (or it is later removed), DocFX does not report the broken link (because the` @` sign has multiple uses). 

And finally, the package docs must be generated in the version of the Editor that matches the version of Unity you want to link to (currently 19.3, 20.1, and 20.2 are supported; everything else links to the current versionless Unity doc URL).
---
uid: package-variables
---

# Variables

You can use HTML "variables" to automatically insert the package version into page content. These "variables" reflect the package version at the time the page is generated (and which is written into the HTML page header). Variables are provided for:

* Long version: the full version number, for example, `2.4.1-exp.1`
* Short version: just the major.minor components, for example, `1.3`
* Kharma link: the Kharma-type deep link into the Package Manager window for installing that version of the package, for example, `com.unity3d.kharma:upmpackage/com.unity.xr.hands@1.4.1`

> [!TIP]
> The entire contents of any elements with these classes are replaced. However, behavior is undefined (untested) if the element has its own child elements.

## Versions

To use a dynamic version variable, add an inline HTML element to the text and assign either `long_version` or `short_version` to the element's `class` attribute. When the page loads, the entire contents of the HTML element are replaced with the version string. The element type determines the formatting. For example, use `<code>` to format the version in the code format or `<span>` to use the surrounding format.
    
For example:

| Example element | Result (for this version of the PMDT package) |
| :-------------- | :---------------- |
|  `<code class="long_version">X.Y.Z</code>` | <code class="long_version">X.Y.Z</code> |
|  `<span class="short_version">X.Y</span>` | <span class="short_version">X.Y</span> |

## Kharma link

A Kharma link opens the Unity Editor, opens the Package Manager Window, and displays the **Add package by name** dialog with the package ID and version filled in. After clicking a Kharma link, the user only has to click the **Add** button.

To use the dynamic kharma link variable, add an `A` element to the text and assign `kharma` to the `class` attribute.

| Example element | Result (for this version of the PMDT package)  |
| :-------------- | :---------------- |
|  `<a class="kharma">com.unity.package.id</a>` | <a class="kharma">com.unity.package.id</a> |


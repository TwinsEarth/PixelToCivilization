# Writing API documentation

This section contains high-level guidance that you can use to get started writing API documentation. For full, Documentation Standards compliant guidance, see [API docs content and formatting](https://confluence.unity3d.com/pages/viewpage.action?spaceKey=DOCS&title=API+docs+content+and+formatting).

PMDT uses **XMLDoc** which is parsed through [DocFX](https://dotnet.github.io/docfx/) to create and generate API script reference documentation. 

You can use [Markdown syntax](https://dotnet.github.io/docfx/spec/docfx_flavored_markdown.html?tabs=tabid-1%2Ctabid-a) inside the XML tags.

See Microsoft's documentation on [XML documentation comments](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/) for more information. You need to take Microsoft's information with a "grain of salt," though. The only standardized aspect is that the comments must be valid XML. Beyond that, it is up to each processor (tool, compiler, etc) to handle the various elements as they see fit. So there are conventions, but not rigid standards, which means interoperability across different tools can be inconsistent.

## What to write

Your goal is to be clear, concise, and accurate.

However, let the tech writer worry about the phrasing. Your main job is to identify the necessary information.

Our audience of developers want to know the following for each type and member:

* What is this thing?
* Why would I use it?
* When would I use it?
* How would I use it?
* What can go wrong?
* What other things does this relate to?

Depending on the XML tag you are writing, the recommended format changes. For templates on how to write different XML tags, see the **Format** column in [Requirements by element](https://confluence.unity3d.com/pages/viewpage.action?spaceKey=DOCS&title=API+docs+content+and+formatting#APIdocscontentandformatting-requirements-by-element).

For information on tone of voice and styling, see [Tone and style](https://confluence.unity3d.com/pages/viewpage.action?spaceKey=DOCS&title=API+docs+content+and+formatting#APIdocscontentandformatting-Toneandstyle).

## More resources

Slack:
* #ask-docs
* #docs-style-guide
* #docs-packman

Unity:
* [In depth Scripting API style guidelines](https://unity-docs.gitbook.io/style-guide/style/scripting-api)
* [Unity Doc Style Guide](https://unity-docs.gitbook.io/style-guide/)

External:
* [DocFX XMLDoc support](https://dotnet.github.io/docfx/spec/triple_slash_comments_spec.html)
* [DocFX Markdown support](https://dotnet.github.io/docfx/spec/docfx_flavored_markdown.html?tabs=tabid-1%2Ctabid-a)
* [Microsoft XMLDoc tutorial with examples](https://docs.microsoft.com/en-us/dotnet/csharp/codedoc)

# Landing pages

There are two landing page types in package documentation: one for the Manual, and one for the Script Reference.

## User manual landing page

For the Manual, DocFX creates the landing page from the `index.md` file found in the `Documentation~` folder of the package. If no such file exists, the tools uses the first markdown file it founds. This means that if you have more than one markdown file, you should always `index.md` as your first, or landing, page. 

>[!IMPORTANT]
>If you include a `TableOfContents.md` file, you must include an `index.md` file.

## Script reference landing page

For the Script Reference, you can optionally include a markdown file, `api_index.md` in the `Documentation~` folder of the package. If this file exists, the tool puts the contents onto the first page of the Script Reference. 

This is a good place to outline the structure of your API and list the major classes. When making links, note that the file ends up in the `api` folder, not the `manual` folder, so make sure that relative links reflect the final folder structure. You can also use [DocFX's `xref` link syntax and uids](ExternalLinks.md):

    [API class](xref:Unity.Entities.SystemBase)
    [Manual topic](xref:uid-you-assigned-to-topic)

For more information on linking within documentation, refer to [External links](ExternalLinks.md).

If you don't include the `api_index.md` file, the tools generate a generic, one-sentence, landing page.
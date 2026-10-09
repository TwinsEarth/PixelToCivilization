# Filter unwanted API documentation

By default, PMDT generates documentation for all public APIs. However, there are circumstances where public APIs are only intended for internal use. To prevent generating the documentation for these kind of APIs, and to also exclude them from PMDT's error checking, you can exclude them.

To exclude API documentation from being generated, create a `filter.yml` file in the `Documentation~` folder of your package. You can then use the custom filtering to filter out specific APIs from your package, or override the default filtering rules.

For information on the rules for custom filtering, refer to DocFX's documentation on [Filter APIs](https://dotnet.github.io/docfx/docs/dotnet-api-docs.html#filter-apis).

The following is an example of a `filter.yml` that excludes `System.Object` classes and anything in a `Test` namespace:

```
apiRules:
  - exclude:
      # inherited Object methods
      uidRegex: ^System\.Object\..*$
      type: Method
  - exclude:
      uidRegex: Tests$
      type: Namespace
  - exclude:
      uidRegex: ^Unity\..*\.Tests\..*$
      type: Namespace
```
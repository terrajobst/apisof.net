# Next Steps

* We should add more tests
  - Module references
  - Operations

* Support custom attributes on return type

* We should synthesize the built-in attributes, specifically the marshalling
  and security ones.

* We should review the exposure of the XxxAttribute types and flags from our
  types. Some of this information is now redundant and just causes clutter.

* We should add an external visibility
  - C# treats namespaces as always having public accessibility but hides them
    when there are no public types in them.
  - Especially useful for members and nested types that might be declared as
    public but aren't reachable because their containing type isn't.

* Support resolution. If we want to emit source code, we need to resolve because
  for enums we'd like to write their values using its field names. We also want
  to decode nullability of reference types which requires knowing whether a
  type reference refers to a value type or a reference type.

* Support detection of C# features
  - nullable reference types
  - tuples and their names
  - dynamic
  - records

* We should have a way to format the C# declaration of elements

* We should move the web site to use the Arroyo for crawling.
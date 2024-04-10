# API Catalog Schema

The format of the catalog is inspired by the .NET metadata format (ECMA 335). It
is comprised of heaps and tables.

The data is separated into two sections, the header and the body:

* `Header`
* Deflate compressed body

In order to read the format the header will be read first and then then body
will need to be decompressed.

## Header

| Offset | Length | Type        | Name        |
| ------ | ------ | ----------- | ----------- |
| 0      | 8      | `Char8[8]`  | Magic Value |
| 4      | 4      | `Int32`     | Version     |
| 8      | 56     | `Int32[14]` | Table Sizes |

- The magic value is `APICATFB`
- This is version `9`
- The table sizes are in the following order:
  1. [String Heap]
  1. [Blob Heap]
  1. [Platform Table]
  1. [Framework Table]
  1. [Package Table]
  1. [Assembly Table]
  1. [Usage Source Table]
  1. [API Table]
  1. [Root API Table]
  1. [Extension Methods Table]
  1. [Obsoletion Table]
  1. [Platform Support Table]
  1. [Preview Requirement Table]
  1. [Experimental Table]

## String Heap

Stores length-prefixed UTF8 encoded strings. Users will have an offset that
points into the heap.

| Type       | Length | Comment             |
| ---------- | ------ | ------------------- |
| `Int32`    | 4      | Number of bytes `N` |
| `char8[N]` | `N`    | UTF8 characters.    |

## Blob Heap

Stores arbitrary chunks of encoded data. The format depends.

## Platform Table

Each row looks as follows:

| Offset | Length | Type           | Name          |
| ------ | ------ | -------------- | ------------- |
| 0      | 4      | `StringHandle` | Platform Name |

## Framework Table

Each row looks as follows:

| Offset | Length | Type                               | Name           |
| ------ | ------ | ---------------------------------- | -------------- |
| 0      | 4      | `StringHandle`                     | Framework Name |
| 4      | 4      | `BlobHandle` -> `AssemblyHandle[]` | Assemblies     |

## Package Table

Each row looks as follows:

| Offset | Length | Type                                                  | Name            |
| ------ | ------ | ----------------------------------------------------- | --------------- |
| 0      | 4      | `StringHandle`                                        | Package Name    |
| 4      | 4      | `StringHandle`                                        | Package Version |
| 8      | 4      | `BlobHandle` -> `(FrameworkHandle, AssemblyHandle)[]` | Assemblies      |

## Assembly Table

Each row looks as follows:

| Offset | Length | Type                                                 | Name           |
| ------ | ------ | ---------------------------------------------------- | -------------- |
| 0      | 16     | `GUID`                                               | Fingerprint    |
| 16     | 4      | `StringHandle`                                       | Name           |
| 20     | 4      | `StringHandle`                                       | PublicKeyToken |
| 24     | 4      | `StringHandle`                                       | Version        |
| 28     | 4      | `BlobHandle` -> `ApiHandle[]`                        | Root APIs      |
| 32     | 4      | `BlobHandle` -> `FrameworkHandle[]`                  | Frameworks     |
| 36     | 8      | `BlobHandle` -> `(PackageHandle, FrameworkHandle)[]` | Packages       |

## Usage Source Table

Each row looks as follows:

| Offset | Length | Type           | Name              |
| ------ | ------ | -------------- | ----------------- |
| 0      | 4      | `StringHandle` | Usage Source Name |
| 4      | 4      | `Int32`        | Day number        |

> [!NOTE]
>
> The day number is used to construct the date indicating how recent the usage
> source is, via `DateOnly.FromDayNumber(int)`.

## API Table

Each row looks as follows:

| Offset | Length | Type                                             | Name         |
| ------ | ------ | ------------------------------------------------ | ------------ |
| 0      | 16     | `GUID`                                           | Fingerprint  |
| 16     | 1      | `Byte`                                           | API kind     |
| 17     | 4      | `ApiHandle`                                      | Parent       |
| 21     | 4      | `StringHandle`                                   | Name         |
| 25     | 4      | `BlobHandle` -> `ApiHandle[]`                    | Children     |
| 29     | 4      | `BlobHandle` -> `(AssemblyHandle, BlobHandle)[]` | Declarations |
| 33     | 4      | `BlobHandle` -> `(UsageSourceHandle, float)`     | Usages       |

## Root API Table

Each row looks as follows:

| Offset | Length | Type        | Name |
| ------ | ------ | ----------- | ---- |
| 0      | 4      | `ApiHandle` | API  |

## Extension Methods Table

Each row looks as follows:

| Offset | Length | Type        | Name                  |
| ------ | ------ | ----------- | --------------------- |
| 0      | 4      | `GUID`      | Extension Method GUID |
| 4      | 4      | `ApiHandle` | Extended Type         |
| 8      | 4      | `ApiHandle` | Extension Method      |

## Obsoletion Table

Each row looks as follows:

| Offset | Length | Type             | Name          |
| ------ | ------ | ---------------- | ------------- |
| 0      | 4      | `ApiHandle`      | API           |
| 4      | 4      | `AssemblyHandle` | Assembly      |
| 8      | 4      | `StringHandle`   | Message       |
| 12     | 1      | `Boolean`        | Is Error      |
| 13     | 4      | `StringHandle`   | Diagnostic ID |
| 17     | 4      | `StringHandle`   | URL Format    |

- The rows are sorted by API and Assembly, to allow binary search based on them.

## Platform Support Table

Each row looks as follows:

| Offset | Length | Type                                        | Name     |
| ------ | ------ | ------------------------------------------- | -------- |
| 0      | 4      | `ApiHandle`                                 | API      |
| 4      | 4      | `AssemblyHandle`                            | Assembly |
| 8      | 4      | `BlobHandle` -> `(StringHandle, Boolean)[]` | Support  |

- The rows are sorted by API and Assembly, to allow binary search based on them.

## Preview Requirement Table

Each row looks as follows:

| Offset | Length | Type             | Name     |
| ------ | ------ | ---------------- | -------- |
| 0      | 4      | `ApiHandle`      | API      |
| 4      | 4      | `AssemblyHandle` | Assembly |
| 8      | 4      | `StringHandle`   | Message  |
| 12     | 4      | `StringHandle`   | URL      |

- The rows are sorted by API and Assembly, to allow binary search based on them.

## Experimental Table

Each row looks as follows:

| Offset | Length | Type             | Name          |
| ------ | ------ | ---------------- | ------------- |
| 0      | 4      | `ApiHandle`      | API           |
| 4      | 4      | `AssemblyHandle` | Assembly      |
| 8      | 4      | `StringHandle`   | Diagnostic ID |
| 12     | 4      | `StringHandle`   | URL Format    |

- The rows are sorted by API and Assembly, to allow binary search based on them.

## Blobs

### Arrays

The most common types of blobs are arrays. Arrays are stored length-prefixed.
The length is stored as an `Int32`. Please note the length is number of
elements, not number of bytes.

Element types can either be simple types or tuples:

- `FrameworkHandle[]`
- `AssemblyHandle[]`
- `ApiHandle[]`
- `(FrameworkHandle, AssemblyHandle)[]`
- `(PackageHandle, FrameworkHandle)[]`
- `(AssemblyHandle, BlobHandle)[]`
- `(UsageSourceHandle, Float)[]`
- `(StringHandle, Boolean)[]`

Tuples are stored in sequence with no padding or length prefix.

### Syntax

The declaration syntax of an API is stored as stream of tokens.

* `Int32` - TokenCount
* Sequence of `Token`

Each `Token` has:

* `Byte` - [Kind](../../src/Terrajobst.ApiCatalog/Markup/MarkupTokenKind.cs)
* If the token kind isn't a keyword or operator token, it's followed by
  - `StringHandle` - Text
* If the token kind is `Reference` then the Text field is followed by an
  `ApiHandle`.

## Types

| Type                | Representation | Comment                                                    |
| ------------------- | -------------- | ---------------------------------------------------------- |
| `GUID`              | `Byte[16]`     | A GUID                                                     |
| `Boolean`           | `Byte`         | A Boolean with `True` being `1`, `0` otherwise             |
| `Float`             | `Single`       | A 32-bit floating point                                    |
| `StringHandle`      | `Int32`        | Offset to a length-prefixed string in the [string heap]    |
| `BlobHandle`        | `Int32`        | Offset to data in the [blob heap]. Representation depends. |
| `PlatformHandle`    | `Int32`        | Index of a row in the [Platform table]                     |
| `FrameworkHandle`   | `Int32`        | Index of a row in the [Framework table]                    |
| `PackageHandle`     | `Int32`        | Index of a row in the [Package table]                      |
| `AssemblyHandle`    | `Int32`        | Index of a row in the [Assembly table]                     |
| `UsageSourceHandle` | `Int32`        | Index of a row in the [Usage source table]                 |
| `ApiHandle`         | `Int32`        | Index of a row in the [API table]                          |

[String Heap]: #string-heap
[Blob Heap]: #blob-heap
[Platform Table]: #platform-table
[Framework Table]: #framework-table
[Package Table]: #package-table
[Assembly Table]: #assembly-table
[Usage Source Table]: #usage-source-table
[API Table]: #api-table
[Root API Table]: #root-api-table
[Obsoletion Table]: #obsoletion-table
[Platform Support Table]: #platform-support-table
[Preview Requirement Table]: #preview-requirement-table
[Experimental Table]: #experimental-table
[Extension Methods Table]: #extension-methods-table

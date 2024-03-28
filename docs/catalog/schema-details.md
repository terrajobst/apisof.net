# Schema Details

Considering that the schema has borrowed quite a bit of inspiration from
metadata it raises the question whether we can change it to represent the data
more truthfully, specifically parameters, signatures and custom attributes.

For example, instead of storing tables for information derived from custom
attributes we could just expose those by processing the custom attributes stored
in the schema.

The challenge is size and complexity. We can't borrow the metadata
representation directly because metadata doesn't need to store information
across multiple versions of an API, which we do.

Basically, the catalog has separate the version agnostic aspects of an API from
version-specific aspects.

For example, the version agnostic aspect of `System.String.Concat(object,
object)` is that it's a method on `System.String` and that it takes two
arguments of type `object`. Version specific aspects include the names of the
parameters, any custom attributes, and any modifier changes (virtual, abstract).

In the catalog this is modeled as `Api` (version agnostic information) and
`ApiDeclaration` (version specific information). An API has a sequence of
declarations, which points to an assembly and its encoded syntax.

## Metadata Format

Let's first look at how ECMA does it. I have simplified this a bit and omitted
parts I don't think are relevant to the catalog.

Types
    4 Namespace
    4 Name
    4 Base
    4 FirstField    (ends at the end of the field table or before the next type's FirstField)
    4 FirstMethod   (ends at the end of the method table or before the next type's FirstMethod)

Methods
    4 Name
    4 Signature
    4 FirstParam    Ends at the end of the parameter table or before the next method's FirstParam

Parameters
    4 Parent        Points to the method (Not clear why this is needed. It seems lookup doesn't need it)
    2 Sequence
    4 Name

Fields
    4 Name
    4 Signature     Blob with encoded signature

Properties
    4 Name
    4 Signature     Blob with encoded signature
    
PropertyMap
    4 Parent
    4 FirstProperty Ends at the end of the properties table or before the next property map's FirstProperty

Events
    4 Name
    4 Type          Type the event, not the containing type

EventMap
    4 Parent
    4 FirstEvent    Ends at the end of the events table or before the next event map's FirstEvent)

Generic Parameters
    4 Parent        Points to the method or type
    2 Number
    4 Name
    1 Flags         For variance and specific constraints

Generic Parameter Constraint
    4 Parent        Points to a Generic Parameter
    4 Constraint    Points to a Type or TypeSpec

Custom Attributes
    4 Parent        Points to a type, method, field, property, event, parameter, or generic parameter
    4 Constructor
    4 Value         Blob with encoded custom attribute data

## Catalog adaption

In order to apply this structure to the catalog we need to split the information
into data attached to a declaration.

Types
    4 Namespace
    4 Name
    4 FirstField
    4 FirstMethod
    4 FirstDeclaration

Methods
    4 Name
    4 FirstParam
    4 FirstDeclaration

Parameters
    4 Parent
    2 Sequence
    4 FirstDeclaration

Fields
    4 Name
    4 FirstDeclaration

Properties
    4 Name
    4 FirstDeclaration

PropertyMap
    4 Parent
    4 FirstProperty

Events
    4 Name
    4 FirstDeclaration

EventMap
    4 Parent
    4 FirstEvent

Generic Parameters
    4 Parent
    2 Number
    4 FirstDeclaration

--------------------------------

NamespaceDeclaration
    4 Assembly

TypeDeclaration
    4 Assembly
    4 Base

MethodDeclaration
    4 Assembly
    4 Signature

ParameterDeclaration
    4 Assembly
    4 Name

FieldDeclaration
    4 Assembly
    4 Signature

PropertyDeclaration
    4 Assembly
    4 Signature
    
EventDeclaration
    4 Assembly
    4 Type

GenericParameterDeclaration
    4 Assembly
    4 Name

GenericParameterConstraintDeclaration
    4 Assembly
    4 Parent

CustomAttributeDeclarations
    4 Assembly
    4 Parent
    4 Constructor
    4 Value
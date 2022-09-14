# cs module

Provides functions for parsing C# files.

This is a work in progress! Many of these functions are very buggy and
are likely to change.

Currently, re-inserting code into the syntax tree does not work well. A more reliable
method is to locate a code section and then use `string.replace` to replace the
text with a modified version.

### node objects

Node objects represent a syntax tree, such as a class or method or an expression.

Passing the node object to a function or pipe that expects a string
will convert the node into a source text representation.

`node.parent` can be used to access the node's parent. `cs.ancestors <node>`
returns a list of ancestors to the node.

### `cs.get_class`

```
cs.get_class <text> <classname>
```

#### Description

Get the text of a class from a source file

#### Returns

The class declaration node

#### Example

```
$origNode = $code | cs.get_class "HewnBenchRecipe"
$updatedNode = $origNode | string.replace "Wood" "Stone"

$code = $code | string.replace $origNode $updatedNode
```

### `cs.find_node_with_text`

```
cs.find_node_with_text <text> <node_type> <search_text>
```

#### Description

Finds the deepest node of a given type that contains the expected text.
This can be used to extract a method or expression (for use with `string.replace`)
without having to figure out where the curly braces end.

Node types include
- `class`
- `method`
- `expression`
- Any `*Syntax` subclass of [CSharpSyntaxNode](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpsyntaxnode).

You can use the [Visual Studio Syntax Analyzer](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/syntax-visualizer) to figure out the structure of piece of code.

Note that we currently only support filtering on Syntax node types, not Kinds.

#### Returns

The node object of the requested type which is the closest to the target text.

#### Example

```
func replace_detection_range(text, newRange)
    # Find a { } initializer containing the text "UserStatType.DetectionRange"
    $origNode = text | cs.find_node_with_text "InitializerExpressionSyntax" "UserStatType.DetectionRange"

    # Replace the value inside the node
    $updatedNode = $origNode | regex.replace `new ConstantValue\(\d+\)` ("new ConstantValue(" + newRange + ")")

    # Replace in the source code
    ret text | string.replace $origNode $updatedNode
end
```

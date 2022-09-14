# `regex` module

The `regex` module extends the [Scriban version](https://github.com/scriban/scriban/blob/master/doc/builtins.md#regex-functions)

### `regex.replace_capture`

```
regex.replace_capture <text> <regex> <replace> <options?>
```

#### Description

Allows substituting values matched by regex capture groups. This essentially makes
regexes into a mini-templating system.

Some notes and caveats:

- Groups must not overlap
- Make sure to escape any regex characters, particularly `(` and `)`

#### Examples

##### `replace` array

If the `replace` argument is an array, the capture groups (starting with 1) will be replaced
with the strings in the array.

This example sets the number of items (captured by `(\d+)`) to `5` wherever this regex matches.

```
$newCampsite = $campsite | regex.replace_capture `{ typeof\(Stone\w+Item\), (\d+) },` ["5"]
```

##### `replace` function

Suppose we want to modify this set of entries so that items starting with "Stone" are doubled:

```
{ typeof(PropertyClaimItem), 6 },
{ typeof(PropertyToolItem), 1 },
{ typeof(StoneMacheteItem), 1 },
{ typeof(StoneAxeItem), 1 },
```

You can write a function that modifies the named capture groups. The updated values
will be substituted back into the matching string. Capture groups can be accessed by
name or number (e.g. for unnamed capture groups).

```
$newCampsite = $campsite | regex.replace_capture `{ typeof\((?<item>.*?)\), (?<count>\d+) },` do
    $groups = $0
    
    if $groups.item | string.starts_with "Stone"
        # Replace 'count' with a new value
        $groups.count = (string.to_int $groups.count) * 2
    end
end
```

Returning a string from the `replace` function will replace the entire match.

An empty capture group `()` (or named group e.g. `(<?insert>)`) can be used to add content
before or after a match. Note that to match line endings, you should use `\r?$` (or `\r?\n`)
to handle carriage returns.

```
# Add text after last entry, before the closing `}`

$newCampsite = $campsite | regex.replace_capture `{ typeof\(.*?\), (\d+) },\r?$()\s+}` do
    $groups = $0
    $groups[1] = "    { typeof(BreakfastItem), 1 },\r\n"
end; options: 'm' # example of passing regex options
```

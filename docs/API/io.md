# io module

### `io.apply_patch`

```
io.apply_patch <text> <patch_text>
```

#### Description

Applies a UNIX-style patch file to the text contents.

#### Returns

Patched copy of text.

### `io.list_core_files`

```
io.list_core_files <path_prefix>
```

#### Description

Returns a list of files in the `__core__` directory

#### Returns

A list of `file` objects filtered by prefix. File objects contains

| name | description |
| --- | --- |
| path      | path to the file |
| name      | name of the file, excluding the directory |
| directory | path of the directory |

### `io.load_core_source`

```
io.load_core_source <source_file_path>
```

#### Description

Loads a source file from the Eco `__core__` directory

#### Returns

Text of source file.

### `io.load_yaml_file`

```
io.load_yaml_file <data_file_path>
```

#### Description

Loads a YAML file from your project's `Data` directory

#### Returns

Object deserialized from YAML.

### `io.write_override_file`

```
io.write_override_file <text> <source_file_path>
```

#### Description

Writes an override file. You should pass in the original file
name and the function will automatically set the extension to `.override.cs`.

# EcoTemplateGen

A code generator for [Eco Global Survival](http://play.eco) mods.

The purpose of this tool is to reduce some of the boilerplate of writing and
maintaining mods, keeping modifications to core files and simple items to
a minimum to make it easier to upgrade when new versions of Eco are released.

## API reference

- [Scriban language reference](https://github.com/scriban/scriban/blob/master/doc/language.md)
- [Scriban builtins](https://github.com/scriban/scriban/blob/master/doc/builtins.md)
- [API docs](docs/API/index.md)

## Getting started

`dotnet tool install --global EcoTemplateGen.Tool`

See other options on the [NuGet page](https://www.nuget.org/packages/EcoTemplateGen.Tool/)

After installing, you can run `eco-template-gen build --help` for help.

## Command line options

```
eco-template-gen build --help
Description:

Usage:
  EcoTemplateGen.Tool build <project-dir> [options]

Arguments:
  <project-dir>  Directory to build

Options:
  --eco-mods-dir <eco-mods-dir>                  Path to Mods directory
  --shared-templates-dir <shared-templates-dir>  Path to shared templates directory
  -o, --output-dir <output-dir>                  Path to write generated files
  --write-diffs                                  If true, write .patch files alongside overrides
  --copy, --copy-to-eco-mods                     If true, copy files to Eco mods directory when done
  -?, -h, --help                                 Show help and usage information
```

## Writing templates

Templates are written using [Scriban](https://github.com/scriban/scriban), a
powerful text templating engine.

EcoTemplateGen offers several styles of templates.

### Fully generated templates

Fully generated templates create a brand new file without referencing
an existing `__core__` file.

See the [Chemistry example](examples/chemistry/)

### Find-and-replace templates

These templates take an existing source file from the server `__core__`
and transform it through text replacement, regular expressions, and syntax
tree analyzers.

This is useful for creating override files for existing items, or creating
items that are very similar to an existing one.

See the [Concrete Bench example](examples/copy_and_replace/) which adapts
the Hewn Bench object into a Concrete Bench.

Also see the [overrides example](examples/overrides/) which shows several ways
of tweaking the `PlayerDefaults.cs` file.

### Bulk changes with control templates and data files

Control templates are used to generate a large number of files. Templates
with names starting with `__` are not outputted to the `UserCode` directory but
are still executed, allowing code using `io.write_file` and `io.write_override_file`
to write to arbitrary files.

Generally, writing bulk overrides should be limited to server admins, rather than
mod authors, since touching a large number of files has a high likelihood of
conflicting with other mods.

See the [bulk data example](examples/bulk_data/) which demonstrates modifying food
item properties like calories, based on a YAML file. This could be used to
build a system where large-scale rebalancing can be done from a spreadsheet or
even based on a voting system.

## Project setup

A basic project directory looks like this:

```
some_mod/
  .netconfig               # Default settings for command-line tool
  Templates/               # Helper templates
    CraftingTable.sbncs
  UserCode/                # Templates
    SomeMod/               # Organize code in the UserCode/Mods directory
      MyTable.cs.sbncs     # Used to generate SomeMod/MyTable.cs
    AutoGen/               # Override files need to match the original path
      Food/
        Huckleberry.override.cs.sbncs
shared_templates/          # Directory of templates to share between projects
  Shared/                  # Subdirectory to avoid naming conflicts
    RecipeClass.sbncs      # Can load with `import "Shared/RecipeClass"`
```

### `.netconfig`, `.netconfig.user`

You'll likely want to set up a `.netconfig` to save yourself from having to pass
the options to `eco-template-gen` every time. The format is described by
[dotnet-config](https://dotnetconfig.org/) which searches the project directory
and any directories above it, as well as your user/home directory.

For paths that shouldn't be checked into source control, use `.netconfig.user`.

For example:

```
[EcoTemplateGen]
    EcoModsDir = "C:\\Program Files\\Steam\\steamapps\\common\\Eco\\Eco_Data\\Server\\Mods"
    OutputDir = "out"
```

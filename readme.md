# CPPIADecompiler
A C# decompiler for hxcpp's CPPIA (`cppia` format, not `cppib`) script format.

## Usage
`CPPIADecompiler <file.cppia> [-o <dir>] [--all] [--dump-tables]`

### Options
    - <file.cppa> *(required)*
        Which filepath to decompile
    - [-o <dir>] *(optional)*
        Write output to a directory
    - [--all] *(optional)*
        Do not ignore boilerplate hxcpp code
    - [--dump-tables] *(optional)*
        Dumps several tables of information
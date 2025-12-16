## Tone of Code
- Professional
- Clean
- Minimal

## Documentation
- Keep docs short and practical
- Architecture + decisions documented markdown files (.md) in the folder where the comments are relevant (eg. in the root of the repo if it is an overview document, or in a specific module folder if it is about that module)

## Codying style
- do not use _ prefix for private fields
- use C# naming conventions for all identifiers (PascalCase for types and methods, camelCase for variables and parameters)
- prefer record types for immutable data structures
- code in a style that is AoT ready as much as possible, if it appears to be too complicated to go AoT, explain why to the user before continuing and wait for its decision, also document the decision in the relevant .md files
- code in a way that is testable, when practical, include unit tests
- avoid deep nesting, prefer early returns
- prefer dependency injection over static classes
- use string interpolation instead of string.Format or concatenation
- use pattern matching where appropriate
- use async/await for asynchronous programming
- prefer LINQ for collections manipulation
- handle exceptions gracefully, use specific exception types
- write XML documentation comments for public APIs
- follow SOLID principles
- follow DRY (Don't Repeat Yourself) principle
- follow KISS (Keep It Simple, Stupid) principle
- ensure code is clean and well-organized
- ensure proper spacing and indentation for readability
- ensure naming is clear and descriptive, avoiding abbreviations unless widely understood
- ensure methods are short and focused on a single task
- ensure classes have a single responsibility
- ensure proper use of access modifiers to encapsulate data
- ensure consistent use of braces, even for single-line statements
- ensure proper error handling and logging mechanisms are in place
- when implementing something that is functional (a class that perform operations) abstract the opetation behind an interface to allow future replacement or mocking
- favour conceptually correct implementation with abstracted operation that can be applied to many different implementation and situations
- find the meaning of the things, identify which are the conceptual minimal set of arguments to perform a task when abstracting an operations behind an interface
- favour reentrant code
- always program in a defensive way, write code that can tollerate unexpected inputs or states


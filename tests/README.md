Minimal test scaffold

This project does not include a full test runner to avoid adding NuGet dependencies here. The `tests/unit` folder contains self-contained C# tests using `Debug.Assert` that exercise core logic (damage, healing, death events).

Options to run:
- Create a .NET test project and reference the game scripts, then call the static `RunAll()` methods.
- Or copy the test classes into a temporary console app and run.

Note: `tests/` is ignored by Godot via `.gdignore` so the editor won’t import these files.


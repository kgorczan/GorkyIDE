# GorkyIDE Implementation Plan

## 1. Vision

GorkyIDE is a cross-platform C# IDE for Windows, Linux, and macOS with a terminal-first frontend built on `Spectre.Console`. It should provide fast project navigation, editing workflows, diagnostics, and C# code completion through OmniSharp while keeping memory allocation predictable and low.

The architecture must be modular so features can be added, removed, and updated independently without replacing the whole application.

## 2. Primary Goals

- Cross-platform .NET application targeting Windows, Linux, and macOS.
- Terminal UI powered by `Spectre.Console`.
- C# language intelligence through OmniSharp.
- Modular architecture with independently updateable modules.
- Low memory allocation in editor, completion, diagnostics, and UI paths.
- Clear separation between core IDE services, UI, language services, and plugins.
- Testable contracts for modules and IDE services.

## 3. Non-Goals For The First Version

- Full Visual Studio or Rider feature parity.
- Complex graphical UI.
- Debugger integration.
- Marketplace backend.
- Remote development.
- Multi-language support beyond C#.

These can be considered later once the core architecture is stable.

## 4. Proposed Solution Structure

```text
GorkyIDE.sln
src/
  GorkyIDE.Abstractions/
  GorkyIDE.Core/
  GorkyIDE.Host/
  GorkyIDE.ConsoleUI/
  GorkyIDE.OmniSharp/
  GorkyIDE.ModuleSystem/
  GorkyIDE.BuiltinModules/
tests/
  GorkyIDE.Core.Tests/
  GorkyIDE.ModuleSystem.Tests/
  GorkyIDE.OmniSharp.Tests/
benchmarks/
  GorkyIDE.Benchmarks/
docs/
  implementation-plan.md
modules/
  installed/
  cache/
  staging/
```

### Project Responsibilities

#### `GorkyIDE.Abstractions`

Stable public contracts shared by the host and modules.

Contains:

- Module interfaces.
- Service registration contracts.
- Command contracts.
- Editor abstractions.
- Diagnostic abstractions.
- Completion abstractions.
- Settings abstractions.

This project should change rarely because external modules depend on it.

#### `GorkyIDE.Core`

Core IDE logic independent from UI and OmniSharp.

Contains:

- Workspace model.
- Project/file model.
- Text buffer model.
- Command registry.
- Event/message bus.
- Settings service.
- Diagnostics store.
- Completion orchestration interfaces.

#### `GorkyIDE.Host`

Application composition root.

Contains:

- Startup sequence.
- Dependency injection setup.
- Configuration loading.
- Module discovery and activation.
- Lifetime management.
- Process-level logging.

#### `GorkyIDE.ConsoleUI`

Terminal frontend using `Spectre.Console`.

Contains:

- Main shell layout.
- File explorer pane.
- Editor pane.
- Diagnostics pane.
- Command palette.
- Status bar.
- Log/output pane.
- Keyboard input routing.

#### `GorkyIDE.OmniSharp`

OmniSharp integration layer.

Contains:

- OmniSharp process management.
- Request/response client.
- Completion adapter.
- Diagnostics adapter.
- Workspace/project synchronization.
- Cancellation and timeout handling.

OmniSharp should run as an external process to isolate memory use and crashes from the IDE host.

#### `GorkyIDE.ModuleSystem`

Module discovery, loading, versioning, and update mechanics.

Contains:

- Module manifest parser.
- Module loader.
- Version compatibility checks.
- Dependency resolution.
- Module isolation strategy.
- Update staging and activation.

#### `GorkyIDE.BuiltinModules`

Built-in features implemented as modules.

Initial built-in modules may include:

- File explorer.
- C# language support.
- Project commands.
- Git status integration.
- Theme support.
- Diagnostics viewer.

## 5. Modular Architecture

### Module Manifest

Each module should have a manifest file, for example:

```json
{
  "id": "gorkyide.csharp",
  "name": "C# Language Support",
  "version": "0.1.0",
  "entryAssembly": "GorkyIDE.CSharp.dll",
  "minimumHostVersion": "0.1.0",
  "dependencies": [
    {
      "id": "gorkyide.omnisharp",
      "versionRange": ">=0.1.0"
    }
  ]
}
```

### Module Lifecycle

Modules should follow a simple lifecycle:

1. Discover manifest.
2. Validate compatibility.
3. Resolve dependencies.
4. Load assembly.
5. Register services and commands.
6. Activate module.
7. Deactivate module on shutdown.
8. Dispose resources.

Suggested interface:

```csharp
public interface IIdeModule
{
    string Id { get; }
    ValueTask ConfigureAsync(IModuleConfigurationContext context, CancellationToken cancellationToken);
    ValueTask ActivateAsync(IModuleActivationContext context, CancellationToken cancellationToken);
    ValueTask DeactivateAsync(CancellationToken cancellationToken);
}
```

### Independent Module Updates

Modules should be installed into versioned folders:

```text
modules/
  installed/
    gorkyide.csharp/
      0.1.0/
      0.2.0/
```

Update flow:

1. Download module package to cache.
2. Verify checksum and manifest.
3. Extract into staging folder.
4. Validate dependencies and host compatibility.
5. Move staged module into installed folder.
6. Mark version as active in module registry.
7. Activate on next restart.

Avoid replacing active assemblies in place. This keeps updates restart-safe and reduces file locking issues on Windows.

## 6. Memory Allocation Strategy

Memory allocation is a core design constraint.

### General Rules

- Prefer long-lived services over frequently recreated objects.
- Avoid large object heap allocations in editor and UI paths.
- Use `ArrayPool<T>` for temporary buffers.
- Use `Memory<T>` and `ReadOnlyMemory<T>` where appropriate.
- Avoid unnecessary string copies for text processing.
- Keep caches bounded and observable.
- Make ownership and disposal explicit.
- Use cancellation tokens for long-running operations.
- Avoid retaining references to old editor snapshots.

### Text Buffer Design

The editor should not store every file as repeatedly copied strings.

Recommended first implementation:

- Use a piece table or rope-like structure for editable text.
- Represent immutable document snapshots for completion/diagnostics.
- Track changed ranges instead of replacing full text.
- Use line index caches with invalidation for edited ranges.
- Avoid allocating per-character objects.

Initial version can start with a simple piece table before optimizing further.

### Completion Strategy

- Debounce completion requests.
- Cancel stale requests when the user keeps typing.
- Reuse request objects where safe.
- Limit completion result count.
- Avoid rendering all completion items if only a page is visible.
- Cache recent completion results per document version only when useful.

### Diagnostics Strategy

- Store diagnostics as compact immutable records.
- Replace diagnostics per document/project in batches.
- Avoid UI allocations by rendering only visible diagnostics.
- Do not keep unlimited diagnostic history.

### UI Rendering Strategy

- Render only changed regions where possible.
- Avoid rebuilding large markup strings on every keypress.
- Keep view models small and disposable.
- Use bounded logs.
- Virtualize file tree and diagnostics lists.

### OmniSharp Isolation

OmniSharp should be treated as a separate memory domain.

Benefits:

- OmniSharp memory does not inflate IDE process memory.
- OmniSharp crashes can be restarted.
- Language service memory can be inspected independently.
- Future language services can use the same process-adapter pattern.

## 7. OmniSharp Integration

### Responsibilities

The OmniSharp adapter should provide:

- Start/stop OmniSharp process.
- Detect OmniSharp executable path.
- Initialize workspace.
- Send document open/change/save notifications.
- Request completions.
- Request diagnostics.
- Request go-to-definition later.
- Request hover info later.

### First Milestone Features

- Start OmniSharp for a selected solution or project.
- Open a C# document.
- Send current document content.
- Request completion at cursor position.
- Display completion items in the console UI.
- Display diagnostics in a diagnostics pane.

### Failure Handling

- If OmniSharp is missing, show setup guidance.
- If OmniSharp crashes, mark language service unavailable and allow restart.
- If requests time out, cancel and keep editor responsive.
- If a project cannot load, show project-level diagnostics.

## 8. Spectre.Console UI Plan

### Initial Layout

```text
+----------------------+----------------------------------------+
| File Explorer        | Editor                                 |
|                      |                                        |
|                      |                                        |
+----------------------+----------------------------------------+
| Diagnostics          | Output / Logs                          |
+----------------------+----------------------------------------+
| Status Bar                                                    |
+---------------------------------------------------------------+
```

### Initial Commands

- `open <path>`
- `save`
- `quit`
- `completion`
- `diagnostics`
- `reload-modules`
- `install-module <path>`
- `update-module <module-id>`

### Keyboard Goals

- Arrow key navigation.
- Text insertion and deletion.
- Save shortcut.
- Command palette shortcut.
- Completion popup navigation.

Terminal input handling should be isolated behind an abstraction so it can be tested and potentially replaced later.

## 9. Versioning And Compatibility

The host and module API should use explicit semantic versions.

Rules:

- Patch version changes should not break modules.
- Minor version changes may add optional APIs.
- Major version changes may break module compatibility.
- Module manifests must declare minimum host version.
- Modules can declare dependencies and compatible version ranges.

The `GorkyIDE.Abstractions` assembly version is especially important because external modules compile against it.

## 10. Observability

The IDE should include lightweight observability from the beginning.

Include:

- Structured logs.
- Module load timing.
- OmniSharp startup timing.
- Completion request duration.
- Diagnostics refresh duration.
- Current cache sizes.
- Optional allocation counters in debug builds.

This helps keep memory and performance regressions visible.

## 11. Testing Strategy

### Unit Tests

- Module manifest parsing.
- Dependency resolution.
- Module compatibility checks.
- Text buffer edits.
- Command registry behavior.
- Diagnostics store behavior.

### Integration Tests

- Module load from folder.
- Module activation/deactivation.
- OmniSharp process adapter with mocked transport.
- Console command routing.

### Benchmarks

Use BenchmarkDotNet for:

- Opening files.
- Editing text buffer.
- Mapping offsets to line/column.
- Rendering visible editor lines.
- Processing completion results.
- Updating diagnostics.

Memory benchmarks should track allocation per operation, not only elapsed time.

## 12. Milestone Roadmap

### Milestone 1: Foundation

- Create .NET solution and projects.
- Add core abstractions.
- Add simple host startup.
- Add logging.
- Add first tests.

### Milestone 2: Module System

- Add module manifest model.
- Add folder-based module discovery.
- Add module compatibility validation.
- Add activation/deactivation lifecycle.
- Add versioned module install layout.

### Milestone 3: Text Core

- Add text buffer abstraction.
- Implement simple piece table.
- Add document snapshots.
- Add line/column mapping.
- Add buffer benchmarks.

### Milestone 4: Console Shell

- Add Spectre.Console dependency.
- Render main shell layout.
- Add command palette prototype.
- Add file opening and saving.
- Add basic keyboard handling.

### Milestone 5: OmniSharp Prototype

- Add OmniSharp process runner.
- Add request client.
- Open C# workspace.
- Request completions.
- Display completions in UI.
- Display diagnostics in UI.

### Milestone 6: Module Updates

- Add module package format.
- Add checksum validation.
- Add staging folder.
- Add active version registry.
- Add safe update activation on restart.

### Milestone 7: Performance Pass

- Add allocation benchmarks.
- Add bounded caches.
- Optimize completion rendering.
- Optimize text buffer hot paths.
- Add memory usage diagnostics command.

## 13. Recommended First Implementation Slice

The first code implementation should stay small:

1. Create solution and projects.
2. Implement `GorkyIDE.Abstractions`.
3. Implement module manifest parsing.
4. Implement module discovery from `modules/installed`.
5. Implement minimal host startup.
6. Implement a minimal `Spectre.Console` welcome screen.
7. Add tests for manifest parsing and compatibility.

This establishes the architecture without prematurely coupling the IDE to OmniSharp or a complex editor implementation.

## 14. Risks

### Terminal Editor Complexity

Building a comfortable code editor in a terminal UI is hard. The first version should prioritize architecture and simple editing before advanced UX.

### OmniSharp Protocol Details

OmniSharp integration may require adapter changes depending on the selected communication mode. Keep this isolated behind interfaces.

### Module Isolation In .NET

Loading and unloading assemblies safely requires careful `AssemblyLoadContext` design. The first version can support restart-required updates before attempting hot unload.

### Memory Regressions

Memory goals can drift if not measured continuously. Benchmarks and diagnostic commands should be introduced early.

## 15. Design Principles

- Keep core independent from UI.
- Keep OmniSharp outside the IDE process.
- Keep module contracts stable and small.
- Prefer explicit lifetimes and disposal.
- Prefer restart-safe updates before hot updates.
- Measure allocations before optimizing heavily.
- Build vertical slices instead of large speculative systems.

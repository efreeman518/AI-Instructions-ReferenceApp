# INSTRUCTION-GAP — Compile-Projection Mapper Pattern

**For:** instruction-maintainer agent (sister repo: `AI-Instructions-Scaffold/`)
**Source:** review feedback from a separate scaffold-generated app that adopted a compile-projection mapper pattern, contrasted against the patterns produced by today's `data-mapping-template.md`.
**Status in reference-app:** pattern applied 2026-05-12 to all six TaskFlow entity mappers + 7 parity tests added. 32-project build green; 255 unit tests + 36 endpoint tests passing.

## What's missing in the current instructions

`templates/data-mapping-template.md` (lines 99-162) prescribes:

1. A hand-written `ToDto()` extension method, AND
2. A family of EF-translatable projectors — `ProjectorSearch`, `ProjectorFull`, `ProjectorStaticItems` — each independently authored.

For the **canonical full shape** (the projector whose fields match `ToDto`), this is two parallel hand-written expressions for the same logical mapping. They can — and over time will — silently drift: a new property added to `ToDto` but forgotten on `ProjectorFull` produces server-side rows missing that column, with no compile error and no test failure unless someone happens to compare them.

The template offers no mechanism to enforce parity between `ToDto` and its EF counterpart, and no required test to detect drift. The current `MapperTests` (per `test-templates-service.md` lines 150-188) only checks `ToDto` against the entity — it never compares `ToDto` to the projector that EF actually uses for `Search`/`Get` paged queries.

## The compile-projection pattern (recommended addition)

Define the full shape once as an `Expression<Func<TEntity, TDto>>`, compile it at static init, and route `ToDto` through the cached delegate:

```csharp
public static readonly Expression<Func<{Entity}, {Entity}Dto>> Projection =
    entity => new {Entity}Dto { /* full shape, EF-translatable */ };

private static readonly Func<{Entity}, {Entity}Dto> Compiled = Projection.Compile();

public static {Entity}Dto ToDto(this {Entity} entity) => Compiled(entity);
```

Wins:
- **Single source of truth.** EF uses `.Select(Projection)` server-side; services use `ToDto(entity)` on in-memory entities. One expression, both call sites. Drift is structurally impossible.
- **Parity by construction**, additionally verified by a `CompiledProjection_AgreesWith_ToDto` test (see "Required test addition" below).
- **One-time compile cost**, cached delegate — performance comparable to hand-written for simple shapes (`EntityMappingBenchmarks` in the reference app already measures `ToDto` on the hot path).

## Selective adoption — what stays, what changes

The compile-projection pattern is only correct for shapes that have **both** an EF call site **and** an in-memory call site. Query-shape-only projectors do not have an in-memory twin and gain nothing from compilation. Concretely:

| Existing template artifact | Recommended treatment |
|---|---|
| `ProjectorFull` + hand-written `ToDto` | **Replace** with `Projection` + `Compiled` + `ToDto = Compiled(entity)`. Rename `ProjectorFull` → `Projection`. |
| `ProjectorSearch` (minimal grid shape, no children) | **Keep unchanged.** No `ToDto` counterpart; no benefit from compilation. |
| `ProjectorStaticItems` (lookup/dropdown shape) | **Keep unchanged.** Different DTO type, no `ToDto` counterpart. |

For entities whose canonical full shape *is* the search shape (most simple entities — Comment, Tag, Category, Attachment, ChecklistItem in the reference app), there is only one projector, and it becomes `Projection`. The `ProjectorSearch` name disappears for those entities; the SearchAsync repository call uses `Projection`.

For entities with a genuinely distinct lean grid shape (TaskItem in the reference app), `ProjectorSearch` stays as a separate, intentionally minimal projector — the canonical `Projection` holds the full hydrated shape used by `Get` / detail endpoints.

## Required test addition

`test-templates-service.md` should add a parity test alongside the existing `Given_ValidEntity_When_MappedToDto_Then_AllPropertiesMapped`:

```csharp
[TestMethod]
[TestCategory("Unit")]
public void {Entity}_CompiledProjection_AgreesWith_ToDto()
{
    var entity = new {Entity}Builder().Build();

    var fromCompiled = {Entity}Mapper.Projection.Compile()(entity);
    var fromToDto    = entity.ToDto();

    Assert.AreEqual(fromToDto.Id, fromCompiled.Id);
    // ...assert every property
}
```

For parents with inlined child projections (since EF cannot translate `.ToDto()` calls on child entities, `Projection` must inline child constructions), add a second test asserting that the parent's inlined children produce the same DTO shape as each child mapper's own `ToDto`. This is the actual drift surface — between parent-inline and child-ToDto, not between Projection and ToDto.

Reference implementation: `src/Test/Test.Unit/Mappers/MapperProjectionParityTests.cs` in this repo (added 2026-05-12).

## Caveat to call out explicitly in the template

Owned-type flattening (e.g. `TaskItem.DateRange → StartDate / DueDate`, `TaskItem.RecurrencePattern → RecurrenceInterval / RecurrenceFrequency / RecurrenceEndDate`) is only safe under the compile-projection pattern if the expression:

1. Stays **EF-translatable** — only property access, null-conditional checks (`entity.X != null ? entity.X.Y : null`), and inline `.Select(...).ToList()` over child collections. No method calls, no `ToString(format)`, no complex ternaries.
2. **Evaluates correctly in-memory** — the same `null` check / property chain must produce the right DTO when the compiled delegate runs over a hydrated entity.

The two are not the same constraint. An expression can be EF-translatable but throw NRE in-memory (e.g. accessing a navigation that EF auto-includes but the in-memory entity has unset). The parity test catches this; the template should call it out so future contributors don't reach for C#-only constructs.

Reference: `src/Application/TaskFlow.Application.Mappers/TaskItemMapper.cs` shows the working pattern over `DateRange` and `RecurrencePattern`. The corresponding parity test (`TaskItem_CompiledProjection_AgreesWith_ToDto_ForScalarsAndOwnedTypes`) exercises both call sites.

## Concrete edits requested in `AI-Instructions-Scaffold/`

1. **`templates/data-mapping-template.md`**
   - Replace the `ProjectorFull` + hand-written `ToDto` section with the `Projection` + `Compiled` + `ToDto` pattern.
   - Rename "Multiple projectors per entity" guidance to clarify: `Projection` is the canonical full shape and source of `ToDto`; `ProjectorSearch` and `ProjectorStaticItems` remain query-shape-only with no `ToDto` twin.
   - Add a "Caveats" subsection covering EF-translatable AND in-memory-correct, with the owned-type flatten example.

2. **`templates/test-templates-service.md`**
   - Add the `{Entity}_CompiledProjection_AgreesWith_ToDto` test alongside the existing mapper test.
   - For parents with child collections, add the `InlinedChildren_AgreeWith_ChildMappers` test.

3. **`templates/repository-template.md`** (if it references `ProjectorSearch` for entities whose canonical full shape *is* the search shape) — update example to use `Projection`.

## Reference-app delta (already applied)

Files changed in `AI-Instructions-ReferenceApp/`:

- `src/Application/TaskFlow.Application.Mappers/{Attachment,Category,ChecklistItem,Comment,Tag}Mapper.cs` — single-shape entities: `ProjectorSearch` → `Projection` + `Compiled` + `ToDto = Compiled(entity)`.
- `src/Application/TaskFlow.Application.Mappers/TaskItemMapper.cs` — multi-shape entity: `ProjectorFull` → `Projection` + `Compiled` + `ToDto = Compiled(entity)`; `ProjectorSearch` retained as the lean grid shape.
- `src/Infrastructure/TaskFlow.Infrastructure.Repositories/{Attachment,Category,ChecklistItem,Comment,Tag}RepositoryQuery.cs` — search call sites updated from `ProjectorSearch` to `Projection`.
- `src/Infrastructure/TaskFlow.Infrastructure.Repositories/TaskItemRepositoryQuery.cs` — unchanged; still uses `TaskItemMapper.ProjectorSearch` for the lean grid path.
- `src/Test/Test.Unit/Mappers/MapperProjectionParityTests.cs` — new file, 7 parity tests.

Verification: `dotnet build src/TaskFlow.slnx` clean (0 errors, vuln warnings only); `dotnet test Test.Unit --filter TestCategory=Unit` 255 passing (was 245 → +10 from new parity coverage and parametrised cases); `dotnet test Test.Endpoints` 36 passing.

`REFERENCE-STATUS.md` test count line will need to bump from 245 → 255 in the same commit that lands these template edits.

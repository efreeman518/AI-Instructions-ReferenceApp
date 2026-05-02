# Playwright + Uno WASM + Blazor — Lessons Learned

## Uno WASM: Boot Once Per Describe

Each WASM cold-start takes ~20s. Never use the default `{ page }` fixture for Uno tests — it boots a new page per test.

**Pattern: shared `BrowserContext`/`Page` in `beforeAll`**

```typescript
import { Browser, BrowserContext, Page, chromium } from "@playwright/test";

let browser: Browser;
let context: BrowserContext;
let sharedPage: Page;

test.beforeAll(async () => {
  browser = await chromium.launch();
  context = await browser.newContext();
  sharedPage = await context.newPage();
  await sharedPage.goto("https://localhost:7069");
  await waitForApp(sharedPage); // wait for WASM boot
});

test.afterAll(async () => {
  await context.close();
  await browser.close();
});

test("my test", async () => {
  // use sharedPage, not page fixture
});
```

For re-navigation within the same shared page, call `waitForApp` again — it re-checks without full re-boot.

---

## Viewport with Shared Page

`test.use({ viewport })` is incompatible with `beforeAll` shared-page patterns — it applies per worker, not per context.

**Use `browser.newContext({ viewport })` instead:**

```typescript
context = await browser.newContext({ viewport: { width: 390, height: 844 } });
```

---

## Uno WASM: Elements Are Invisible to Playwright

Uno WASM renders via canvas/shadow DOM. Standard visibility checks fail. Use `getBoundingClientRect()` via `page.evaluate()` and `page.mouse.click()` for all interactions.

**Coordinate-click with retry loop:**

```typescript
for (let attempt = 0; attempt < 20; attempt++) {
  const coords = await page.evaluate(() => {
    for (const p of Array.from(document.querySelectorAll("p"))) {
      const txt = (p.textContent ?? "").trim();
      if (!txt.startsWith("E2E-")) continue; // filter to known prefix
      const r = p.getBoundingClientRect();
      if (r.width > 0 && r.height > 0 && r.y > 0 && r.x > 0) {
        return { x: r.x + r.width / 2, y: r.y + r.height / 2 };
      }
    }
    return null;
  });
  if (coords) {
    await page.mouse.click(coords.x, coords.y);
    break;
  }
  await page.waitForTimeout(500);
}
```

Filter by a known text prefix (e.g. `"E2E-"`) to avoid hitting status chips or other `<p>` elements that overlap your target.

---

## Uno WASM: Slow Router After Many Navigations

After many in-session navigations, the WASM router can lag. Increase assertion timeouts for pages that load late in the shared-page lifecycle.

```typescript
await expectBodyToContainAll(sharedPage, ["Categories", "ADD CATEGORY"], 60_000);
```

Use 60s for pages navigated to after 3+ prior navigations in the same shared page.

---

## Playwright Suite Timeout

Default `--timeout=30000` is too short for suites containing Uno WASM cold-start (~20s just to boot).

**Set in `package.json`:**

```json
"test:full:fast": "npx playwright test --retries=0 --max-failures=4 --timeout=120000"
```

---

## Never Assert Specific Task Counts or Seed Titles in E2E

Assertions like `"Showing 1 to 10 of 14 tasks"`, `"Page 1 of 2"`, or specific task titles (e.g. `"Build dashboard UI"`) are fragile against a shared dev DB with accumulating test data.

**Remove or replace with structural assertions:**

```typescript
// BAD
await expect(page.locator("body")).toContainText("Showing 1 to 10 of 14 tasks");

// GOOD — assert the pager exists, not its exact count
await expect(page.locator("[data-testid='pager']")).toBeVisible();
```

---

## MudBlazor: Input Field `waitFor` Before Click

MudBlazor inputs may not be visible immediately after navigation. Without a guard, `.click()` and `.fill()` fail.

```typescript
export async function fillTextField(page: Page, label: string, value: string) {
  const field = page.locator(
    `.mud-input-control:has(label:has-text("${label}")) input, ` +
    `.mud-input-control:has(label:has-text("${label}")) textarea`
  );
  await field.first().waitFor({ state: "visible" });
  await field.first().click();
  await field.first().fill(value);
}
```

---

## MudBlazor: Delete Dialog Needs 15s Timeout

MudBlazor dialogs animate in. 5s is too short in CI or under load.

```typescript
export async function confirmDeleteDialog(page: Page) {
  const dialog = page.locator(".mud-overlay-dialog, .mud-dialog-container").first();
  await expect(dialog).toBeVisible({ timeout: 15_000 });
  await dialog.getByRole("button", { name: /delete/i }).click();
}
```

---

## ASP.NET Core: Catch `OperationCanceledException` in the Service Layer

`OperationCanceledException` thrown from EF Core queries propagates through middleware. The VS debugger breaks at the throw site before `GlobalExceptionHandler` runs, so suppressing it in middleware is too late.

Catch it in the service method instead:

```csharp
public async Task<PagedResponse<TaskItemDto>> SearchAsync(
    SearchRequest<TaskItemSearchFilter> request, CancellationToken ct = default)
{
    try
    {
        return await repoQuery.SearchTaskItemsAsync(request, ct);
    }
    catch (OperationCanceledException)
    {
        logger.LogDebug("Search cancelled by client.");
        return new PagedResponse<TaskItemDto>();
    }
}
```

---

## ASP.NET Core MinAPI: Nullable `[FromBody]` for Search Endpoints

If the client sends an empty body (e.g. on cancellation or rapid navigation), a non-nullable `[FromBody]` parameter throws a 400 before the service layer is reached.

```csharp
// BAD
app.MapPost("/tasks/search", async (SearchRequest<TaskItemSearchFilter> request, ...) => { });

// GOOD
app.MapPost("/tasks/search", async ([FromBody] SearchRequest<TaskItemSearchFilter>? request, ...) => {
    request ??= new SearchRequest<TaskItemSearchFilter>();
    ...
});
```

---

## ASP.NET Core: `GlobalExceptionHandler` Must Check `HasStarted`

Before writing a response body in a global exception handler, guard against already-started responses:

```csharp
public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
{
    if (ctx.Response.HasStarted) return true;
    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await ctx.Response.WriteAsJsonAsync(new ProblemDetails { ... }, ct);
    return true;
}
```

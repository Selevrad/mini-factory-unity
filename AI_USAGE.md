# AI Usage

This project was built together with **Claude Code** (Claude Sonnet 5), used as a
coding agent connected live to this Unity Editor via the Unity Pipeline MCP
integration - not just for generating code snippets to paste in, but for directly
writing scripts to disk, creating/wiring GameObjects and UI in the actual scene,
running compiles and tests, driving Play Mode, and inspecting the console, all in
the same session.

## What was delegated to the AI

Essentially the full implementation, end to end:

- Architecture and all C# code: the `Config` / `Domain` / `Services` / `Runtime`
  layering, the `Factory`/`Machine` economy model, the `IConfigProvider` /
  `ISaveService` / `IAnalyticsProvider` / `IPurchasingService` abstractions and
  their implementations.
- Unity IAP v5 integration, including reading the installed package's own source
  under `Library/PackageCache` to confirm the current (non-deprecated) API surface
  rather than relying on possibly-stale training knowledge.
- Building the scene UI (Canvas, SafeArea, machine rows, buttons, colors) directly
  in the Editor through scripted Editor-API calls, not hand-authored YAML.
- The 6 EditMode unit tests.
- Android platform configuration (orientation, IL2CPP, ARM64, Active Input
  Handling, Run In Background).
- This Git history: branching, Conventional Commit messages, and the GitFlow
  merge structure.
- Diagnosing every bug listed below.

## What was done independently

- All product/scope decisions: what to build first, which ToR bonus items to
  defer, when a fix was "good enough" versus needed more work.
- Actually *playing* the running game in the Editor and reporting real UX
  problems by eye - the AI could run the game headlessly and read logs, but every
  visual bug below (see "Bugs found") was first noticed by a human looking at the
  screen, not caught by the AI on its own.
- Deciding on GitHub CLI (`gh`) over manual repo creation, and directing when to
  restart the Unity Editor for engine-level settings changes to take effect.
- Reviewing and approving each phase of work before moving to the next.

## A few representative prompts

- "сделай lock unlock для заводов и так далее по ТЗ" - after the base economy
  existed, this drove adding the actual lock/unlock UI state and interaction.
- "сделай улучшение (upgrade) машин тоже наглядно" - led to the per-level color
  badge and upgrade pulse animation on `MachineRowView`.
- "оптимизацию под экраны телефонов андроид обычных" - drove the Android
  platform settings pass and, later, the SafeArea helper.
- "нажимаю купить коины, в всплывающей менюшке кнопки не нажимаются но с
  открытым окном нажимаю на кнопку купить коины (которая находится за окном)
  все покупается" - this single bug report is what led to discovering the
  Active Input Handling / legacy OnGUI conflict below.
- "улучши немного графику, может цвета и фон какой нибудь" - drove the color
  palette, card backgrounds and rounded-sprite pass on the UI.

## Suggestions changed or self-corrected

Nothing was explicitly rejected by the reviewer in this session (proposed
approaches were generally accepted), but the AI corrected itself more than
once when its own first attempt turned out wrong on closer inspection:

- The first `UnityPurchasingService` draft used the classic
  `IStoreListener` / `ConfigurationBuilder` API, which still compiles but is
  marked obsolete in IAP v5. It was rewritten to use the new
  `StoreController` / `UnityIAPServices` API after checking the package's
  own source and changelog, instead of shipping code that compiles with
  warnings.
- `Machine.NextUpgradeCost`'s first version charged the growth multiplier
  starting from the very first upgrade, which one of the AI's own unit tests
  then caught as an off-by-one in the pricing curve; the formula was fixed
  rather than adjusting the test to match the bug.

## Bugs / questionable decisions the AI found

A genuinely useful side effect of building this interactively: several bugs
were found and root-caused during the session rather than left for later.

- **Upgrade pricing off-by-one** - the first upgrade after unlocking cost
  `baseCost * growth`, not `baseCost`, because `NextUpgradeCost` used the
  post-unlock level directly instead of the upgrade count. Caught by a
  failing unit test, not by manual play.
- **Crash on quit during init** - `GameBootstrap.OnApplicationQuit` could
  throw a `NullReferenceException` if it fired before `Awake` finished.
  Found via the Console, fixed with a null guard.
- **Fake Store "Cancel" appearing to hang** - `UnityPurchasingService`'s
  purchase-failure handler could throw when a cancelled order's cart was
  empty; the exception unwound into Unity IAP's own dialog-close code and
  left its window stuck. Root-caused by reading `UIFakeStore.cs` in the
  installed package, then fixed by making the handler null-safe and
  exception-safe.
- **Fake Store buttons not responding to clicks at all** - traced to the
  project's Active Input Handling being set to "Input System Package (New)"
  only; Unity's Fake Store dialog renders with legacy `OnGUI()`, which
  needs the old input backend. Switched to "Both".
- **UI elements inflated 15-45% beyond their intended size** - creating
  GameObjects under the (CanvasScaler-scaled) Canvas via Editor scripting
  causes Unity to write a compensating non-1 `localScale` to preserve
  apparent world size, which is meaningless for a freshly-created UI element
  and silently corrupts layout math. Found by comparing measured vs.
  expected on-screen bounds; fixed by resetting `localScale` to `(1,1,1)`
  across the whole UI hierarchy.
- **The whole game appeared to freeze intermittently** - `Application
  .runInBackground` defaulted to `false`, so the entire Player Loop
  (`Update`, coroutines, even deferred `Object.Destroy`) paused whenever the
  Editor window lost OS focus. This explained several previously-confusing
  "stuck" symptoms across the session and was fixed both in code
  (`GameBootstrap.Awake`) and in Player Settings.
- **UI overflowing the screen edge on some phones** - several elements used
  a fixed width sized for exactly the 1080-unit reference canvas instead of
  stretch anchors, so they clipped past the right edge on phone aspect
  ratios taller than the reference. Fixed by switching those elements to
  margin-based stretch anchoring.

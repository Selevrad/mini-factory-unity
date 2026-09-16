# Mini Factory

A small mobile idle/incremental game built for a Unity Developer take-home
assignment: unlock and upgrade production machines, use a temporary
production Boost, keep earning while offline, and buy a currency pack via
Unity IAP.

## Running it

1. Open the project in **Unity `6000.5.5f1`**.
   > The assignment specified `6000.3.22f1`; using `6000.5.5f1` instead was
   > confirmed as acceptable with whoever issued the assignment before
   > starting. Everything below was built and tested against `6000.5.5f1`.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press Play. The scene is fully wired - no extra setup needed.
4. In-Editor purchases go through Unity IAP's bundled **Fake Store** (no
   real store account or product needed).

## Android build

The project's active build target is already **Android**, with:

- Scripting Backend: **IL2CPP**
- Target Architectures: **ARM64 + ARMv7**
- Minimum API level: **26** (Android 8.0 - the floor this Editor version
  enforces)
- Default orientation: **Portrait**
- Active Input Handling: **Both** (see "Known limitations" - the new Input
  System drives gameplay UI, but Unity IAP's Fake Store dialog needs the
  legacy backend to receive clicks)

To build: **File > Build Settings** (scene list and target are already
configured) **> Build**, or from the CLI:

```
unity build /path/to/Fabric --target Android --allow-install
```

**Before shipping for real**, change `Player Settings > Other Settings >
Package Name` away from the Unity template default
(`com.UnityTechnologies.com.unity.template.urpblank`) to a real
application id.

## Architecture

```
Assets/MiniFactory/Scripts/
  Config/    - MachineDefinition, EconomyConfig (ScriptableObject), IConfigProvider, LocalConfigProvider
  Domain/    - Factory, Machine, SaveData - pure C#, no UnityEngine/MonoBehaviour dependency
  Services/
    Save/    - ISaveService, JsonFileSaveService
    Analytics/ - IAnalyticsProvider, AnalyticsService, ConsoleAnalyticsProvider
    IAP/     - IPurchasingService, UnityPurchasingService
  Runtime/   - GameBootstrap (composition root + mobile lifecycle), FactoryUIController,
               MachineRowView, SafeArea
Assets/MiniFactory/Tests/EditMode/ - FactoryTests (6 tests)
```

**Why this shape:** `Factory`/`Machine` hold all the economy math (unlock,
upgrade, production, boost, offline progress) as plain C# with zero Unity
API surface, so they're trivially unit-testable and can't accidentally pick
up a MonoBehaviour dependency. Everything else is a small interface
(`IConfigProvider`, `ISaveService`, `IAnalyticsProvider`, `IPurchasingService`)
with exactly one implementation today, so a concrete integration (Remote
Config, a real analytics SDK, a different save backend) can be swapped in
later without touching gameplay code. `GameBootstrap` is the single
composition root that wires all of this together and owns the parts that
*do* need MonoBehaviour - the per-frame production tick and
`OnApplicationPause`/`OnApplicationQuit` persistence.

No DI framework, no ScriptableObject event buses, no generic
"manager-of-managers" layer - the whole game is small enough that plain
constructor injection from one composition root covers it without adding
a framework dependency.

### Economy formulas

- **Production**: `baseProduction * productionGrowthPerLevel ^ (level - 1)`,
  summed across unlocked machines, multiplied by the boost multiplier while
  a boost is active.
- **Upgrade cost**: `baseUpgradeCost * upgradeCostGrowthPerLevel ^ (level - 1)`
  - the *first* upgrade after unlocking costs exactly `baseUpgradeCost`.
- **Offline progress**: real elapsed time since the last save, clamped to
  `maxOfflineSeconds`; only the portion of that window that was still
  covered by an active boost is multiplied, matching "boost учитывает
  реально прошедшее время" from the brief.

### Config -> Remote Config path

Gameplay code only ever reads through `IConfigProvider`. `LocalConfigProvider`
wraps the `EconomyConfig` ScriptableObject today; a `RemoteConfigProvider`
(or a composite that layers remote values over local as a fallback) can
implement the same interface later with no change to `Factory` or
`GameBootstrap`.

## Packages / SDKs used

- `com.unity.purchasing` **5.4.3** - Unity IAP v5, via the current
  `StoreController` / `UnityIAPServices` API (not the deprecated
  `IStoreListener` API). Verified against the bundled Fake Store.
- `com.unity.inputsystem` - drives all gameplay UI input.
- `com.unity.render-pipelines.universal` (URP) - from the base template;
  no custom rendering was added.
- `com.unity.test-framework` - EditMode unit tests.
- Standard uGUI (`UnityEngine.UI`, legacy `Text`/`Image`/`Button`) for all
  UI - no TextMeshPro, no external art assets.

## Known limitations / not done

Per the assignment, the bonus items were intentionally left out of this
pass:

- No Firebase Remote Config (local `EconomyConfig` only - see "Config ->
  Remote Config path" above for how it would plug in).
- No real Analytics Provider (Console only; `AnalyticsService` already
  supports registering more than one provider).
- No Edit/Play Mode tests beyond the 6 required-minimum EditMode tests.
- No Android build profiling.
- Art is intentionally minimal (standard uGUI + a small color/rounded-corner
  pass) - the brief says art isn't evaluated.

**Build/device testing status:** gameplay, UI and IAP were tested via Unity
Editor Play Mode with the Android platform active (simulating a
1080x2340-class portrait phone with a safe-area inset).

A real `Build > Android` run was also done from this exact commit and
**succeeded**: `Builds/Android/MiniFactory.apk`, ~73 MB, IL2CPP,
ARM64 + ARMv7, zero build errors (974 warnings, essentially all
`com.unity.ai.inference`/Sentis shader-variant noise - see next steps).
The full build took about 52 minutes end to end on this machine, most of
it IL2CPP native compilation and Gradle's first-time dependency download.
That `.apk` has **not** been installed and run on a physical device or
emulator - none was available in this environment - so first-launch
behavior (IAP init, safe-area insets, etc.) on real hardware is still
unverified. Installing and running it on a device/emulator is the
concrete next step before calling this shippable.

## Time spent / next steps

This was built interactively with an AI coding agent (see `AI_USAGE.md`)
rather than as solo heads-down coding, so "hours" doesn't map cleanly onto
the traditional solo-dev estimate the brief asks for. Session activity
spans roughly 4 hours on 2026-09-16.

**What's left / what I'd do next:**

1. Install `Builds/Android/MiniFactory.apk` on a real device or emulator
   and verify first-launch behavior - this hasn't happened yet (see
   above).
2. Remove `com.unity.ai.assistant` / `com.unity.ai.inference` (Sentis),
   both unused template leftovers responsible for nearly all 974 build
   warnings and likely a meaningful chunk of the ~52-minute build time and
   ~73 MB APK size.
3. Fix the Android package name away from the Unity template default
   before any real distribution.
4. Pick and wire one bonus item - Firebase Remote Config is the most
   valuable given the `IConfigProvider` seam is already there for it.
5. A couple of Play Mode tests around the mobile lifecycle
   (pause -> resume -> offline income applied) would strengthen coverage
   beyond the pure-logic EditMode tests.

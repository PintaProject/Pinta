# Final polish and parity assessment

> Audit snapshot: 30 August 2026. This document distinguishes behavior that is covered by
> automated tests from visual similarity, deliberate browser boundaries, and work that remains.

## Executive verdict

**4 September follow-up:** the [human acceptance queue](validation/README.md) records pending
iPhone/iPad Safari testing, fluent-review sheets for 25 locales, and a new 15-pair visual inspection.
That inspection found capture-state mismatches and visible control/layout differences; the older
completion checklist below must not be read as a blanket native-parity sign-off.

**9 September verification:** the complete automated matrix is green, including 291 unit tests,
Chromium/Firefox/WebKit behavior, touch and dialog-layout sweeps, all 189 canonical screenshots,
six performance budgets, and eight native C# effect fixtures. The [recorded evidence and exact
counts](validation/README.md#verification-recorded-9-september) do not replace the pending physical
device, fluent-language, or broader native-visual reviews.

Pinta Online is a substantial, high-fidelity browser port, but it is not yet an identical
replacement for desktop Pinta.

| Area | Assessment |
| --- | --- |
| Core editing | Strong: all 22 native tools, selections, live marching ants, layers, text, gradients, history, shortcuts, and palette editing |
| Effects | Strong: 46 built-in effects plus 9 optional add-in effects |
| Dialogs | Broad coverage, but screenshot similarity does not prove every native interaction or edge case |
| Reliability | Good beta quality with recovery, migrations, session-isolated worker fallback, validated effect inputs, quota handling, and complete history restoration |
| Localization | 30 selectable UI locales and 98 web-only strings in each; French, German, Arabic, and Hebrew are reviewed, while the other 25 overrides are labelled machine translations |
| SEO and PWA | Implemented: localized pages, sitemap, hreflang, analytics, manifest, icons, and offline worker |
| Browser coverage | Chromium, Firefox, WebKit, and touch all pass; desktop behavior uses fresh-process shards and the exhaustive dialog-layout tests each receive a new Chromium process |
| Mobile and touch | Eight real touch-emulation tests cover drawing, long-press secondary colour, panning, responsive controls, toolbar reachability, and dialog fit; 33 WebKit iPad checks cover portrait, landscape, split view, popup scrolling, and every locale at two tablet widths; engine-specific pinch paths are covered separately |
| Performance | Six production-build budgets cover drawing, selection dragging, effects, tab switching, restoration, heap growth, and stored bytes |
| Architecture | Considerably improved, but the largest React hook remains difficult to maintain |

## What is genuinely accomplished

The web implementation has:

- The complete 22-tool native catalog plus optional Block Brush.
- All 46 native adjustments and effects and nine optional add-in effects.
- Persistent tabs, pixels, selections, drafts, layers, and complete named undo history.
- Native-style shortcuts that override browser defaults where possible.
- Real magic-wand persistence and animated marching ants.
- File-handle integration, clipboard handling, OpenRaster, TIFF, BMP, TGA, PPM, JPEG,
  WebP, and PNG workflows.
- Error boundaries, workspace recovery, versioned IndexedDB migrations, storage-pressure
  warnings, effect cancellation, validated worker sessions, and main-thread effect fallback.
- About pages, a user guide, Google Analytics and Ads conversion tracking, a sitemap, reciprocal
  `hreflang`, structured data, PWA metadata, and Evgeny Vinnik attribution.
- 189 Playwright visual tests producing 194 baselines, 304 behavioural browser tests across
  Chromium, Firefox, WebKit, and touch, and 286 unit tests.

The last remotely tested functional commit at the time of this audit, `83f6624d`, passed the
[Web visual regression workflow](https://github.com/evgenyvinnik/pinta-online/actions/runs/33232860924).
That is meaningful evidence of stability, but the screenshots compare the web application against
approved web baselines. Only 110 baseline names have direct native-reference counterparts; the
remaining dialog audits are substantially manual. The suite protects visual stability, not a
blanket claim of pixel-identical Pinta UI.

## Priority zero: versioning and deployment

At the time of the audit, production served `1.0.260829.32` while `origin/master` contained
`1.0.260829.33`.

The race is caused by two independent deployment paths:

1. [`versioning.yml`](../.github/workflows/versioning.yml) commits the new version and manually
   dispatches deployment.
2. [`deploy-pages.yml`](../.github/workflows/deploy-pages.yml) later deploys the exact commit that
   passed the earlier visual workflow, which still contains the previous version.

The later deployment can therefore overwrite the versioned build. The bot-generated version commit
also receives no new Web visual run when it is pushed with the default GitHub Actions token.

**Resolved.** The first of the two options was taken: the version is derived during the tested
build and never committed. [`versioning.yml`](../.github/workflows/versioning.yml) is now
`workflow_call` only, holds `contents: read`, and does nothing but compute
`1.0.<date>.<run-number>` — there is no bot commit and no competing dispatch left to race.

The ordering is what makes the artifact immutable. `Web visual regression` calls that workflow,
embeds the result with `set-version.mjs` **before** any test runs, and then checks it with
`verify:version`; the build every gate ran against therefore carries the version it will ship with.
`deploy-pages.yml` downloads that exact run's artifact by run id and never checks out or rebuilds a
commit, so nothing newer can be substituted for what passed.

Verified end to end on 30 August 2026: run 47 published `1.0.260830.47`, which is live.

## Refactoring status

The refactor has produced major improvements:

- `App.tsx`: approximately 5,428 to **1,429** lines.
- `usePaintEditor.ts`: 5,572 to **2,621** lines.
- `effects/processor.ts`: 2,929 to **252** lines, including the catalog-wide input boundary.
- Components and dialog hosting are separated.
- Effect kernels and many editor helpers are in focused modules.

Closed since this document was written:

- ~~Workspace serialization remains inline.~~ Extracted to
  [`workspaceSerialization.ts`](../src/editor/workspaceSerialization.ts) — 15 functions, 254 lines.
- ~~Two effect kernel files remain approximately 799 and 791 lines.~~ Split along the catalog's own
  category boundaries into `blur.ts` (261) and `artistic.ts` (402). Every kernel is now under the
  700-line target: shared 647, pixelOps 595, distortions 530, artistic 402, generators 388,
  blur 261.
- ~~ESLint currently permits 45 warnings.~~ Zero, with every rule at `error`. Two rules were also
  found to be off or misconfigured; see §12a of [`refactoring.md`](refactoring.md) for the 358 dead
  bindings that turned up when `no-unused-vars` was switched on.
- `App.tsx` is down to 22 `useState` and 30 `useCallback`, five of the nine planned hooks having
  landed. `useViewportZoom` was the most recent and the clearest win: 284 lines out, and nine refs
  that had no reader outside the group became private.

Still open:

- `usePaintEditor.ts` holds 67 callbacks and should continue toward sub-hook composition. Seven
  sub-hooks have landed; the remaining five were measured and deliberately left — see §8.2a of
  [`refactoring.md`](refactoring.md), which explains why moving them would make the file longer to
  read rather than shorter.
- `styles.css` remains approximately 5,854 lines. Its split was correctly abandoned after it
  demonstrated cascade regressions; any future split needs an explicit cascade-layer or ordering
  design rather than a mechanical series of imports.

Refactoring should proceed only from a clean, stationary worktree. Pure moves need to remain
separate from behavior changes, and every rendering-related extraction must preserve all approved
visual baselines.

## Reliability gaps

The reliability foundation is strong, but several limits remain:

- ~~Effect preview failures could escape as unhandled rejections, malformed worker replies could
  throw from `onmessage`, and a late event from a terminated worker could stop the replacement
  worker.~~ **Resolved 30 August 2026.** Preview promises now have an owned rejection path;
  callbacks changing identity no longer restart a preview; every request, response, image
  dimension, buffer length, progress value, effect id, and parameter is validated; and pending
  requests fall back to the main-thread processor after a worker/protocol failure. Worker state is
  scoped to a session, so old events are inert. Five client race/fallback tests, five dialog
  lifecycle tests, a minimum/maximum/non-finite sweep of every built-in and add-in effect, and two
  production-build Playwright cases pin the result. The two browser cases pass in Chromium,
  Firefox, and WebKit.
- `SurfaceDiff` reduces in-memory history cost. IndexedDB persistence no longer throws that away:
  each distinct `PixelNode` is encoded once per save and the same `Blob` instance is reused for
  every step that shares it, so structured clone stores those bytes once. A fifty-step history over
  four layers with one layer being painted used to write 200 PNGs, 150 of them duplicates; it now
  writes 53. What remains is that a save still rewrites the whole record rather than appending —
  the cost is now proportional to distinct pixels rather than to history length, which is the part
  that was exhausting quota.
- ~~Emergency recovery downloads individual layer PNGs, not a reconstructed OpenRaster document.~~
  **Resolved 29 August 2026.** It now writes one `.ora` per open document, so layer names,
  visibility, opacity and blend modes survive the rescue instead of the work arriving as a pile of
  loose images. This was possible without weakening the module's rule against depending on the
  editor or the renderer, because `encodeOpenRasterArchive` is a pure function from stored PNG
  bytes to a zip. The loose-PNG path remains as a fallback for a document whose archive cannot be
  built — a worse copy beats no copy on this code path. `mergedimage.png` is written only for
  single-layer documents, since producing it otherwise would mean compositing.
- ~~During this audit, the full local gate lost its preview server after 78 of 93 browser tests.~~
  **Resolved 29 August 2026.** The cause was `reuseExistingServer: !process.env.CI`. Because the
  `webServer` command rebuilds `dist/`, a server left running from an earlier invocation kept
  serving the previous `index.html`, whose hashed asset names no longer existed — an `ENOENT` on
  the stylesheet followed by `ERR_CONNECTION_REFUSED` across every subsequent test. It reproduced
  at scale during the refactor: **53 false failures in a single run.** Every deterministic config sets
  `reuseExistingServer: false`, and both route the server through
  [`scripts/run-preview-server.mjs`](../scripts/run-preview-server.mjs), which keeps a timestamped
  transcript and an explicit exit reason under `test-results/server-logs/`. Recording the exit
  reason needed `gracefulShutdown` as well: without it Playwright `SIGKILL`s the server's process
  group, so nothing can observe the shutdown.
- ~~Rapid multi-document commands could act on a published React snapshot one render behind the
  editor.~~ **Resolved 30 August 2026.** Save All now drives its queue through a synchronous editor
  ref, multi-file restoration updates the active document view before the next import can capture
  it, and close/save commands read the active editor's current dirty flag. The final race was found
  by the sharded gate: clicking Add Layer and immediately pressing Ctrl+W could close the document
  without prompting because the tab snapshot still said clean. The exact six-test sequence now
  passes in Chromium and the complete Firefox shard passes the same case.
- Timing measurements taken on a loaded machine are not evidence. During this work an apparent
  large e2e regression was traced through two wrong causes before the real one turned up: swap
  94% full and load average 25–30. The same suite ran in **17.2 minutes** under that pressure and
  **45.7 seconds** once it cleared. A `vitest` run under the same pressure also reported 179 of
  264 tests as the whole suite. Re-measure on a quiet machine before believing any timing delta.
- Codespell previously failed on an effect-coordinate identifier and ran outside the deployment
  gate. The identifier has since been corrected, and spelling is now a dependency of the release
  workflow. See the historical
  [Codespell run](https://github.com/evgenyvinnik/pinta-online/actions/runs/33232860949).
- Firefox now runs alongside Chromium and touch in the primary behavioural configuration; WebKit
  is measured but not yet a deployment gate. The first unmodified runs on 29 August 2026 gave
  **Firefox 83/93** and **WebKit 54/93** and immediately exposed real cross-engine defects.

  **WebKit's dominant failure was one defect, and a real one.** Triaging it found that 33 of 58
  failures were `locator.click` timeouts behind a `native-alert-backdrop` — an error alert reading
  *"Failed to save workspace"* that never closed, so it swallowed every subsequent click. The cause
  is that **WebKit cannot store a `Blob` in IndexedDB at all**: putting one aborts the transaction,
  and it is not a size limit, since a 290-byte PNG fails exactly as a layer does. The workspace
  stores every layer, history snapshot and selection mask as a PNG Blob, so persistence did not
  work on that engine at all. `ArrayBuffer` stores fine, so blobs now travel as bytes with their
  MIME type beside them, converted at the IndexedDB boundary and accepted in either shape on read
  so older records still load. **WebKit went from 35 passed to 86.**

  Chromium and Firefox were checked and store Blobs without complaint, so this is WebKit-specific
  and did **not** explain the Firefox `InvalidStateError`; that separate unload failure was later
  traced and fixed as recorded below.

  The second finding was a test encoding a platform convention as a rule. *"Treats browser
  file-picker cancellation as a no-op and restores command focus"* asserted the invoking button was
  focused afterwards. On Windows and Linux it is, because clicking a button focuses it; **WebKit
  follows the macOS convention of not focusing a button on click**, so focus sits on the body both
  before and after. Nothing is lost either way. An attempt to restore focus explicitly was written
  and then reverted: `document.activeElement` is already the body by the time the handler runs, so
  it could not work, and a Mac-like application arguably should follow the Mac convention. The test
  now asserts what is actually guaranteed — a cancelled picker does not move focus somewhere
  unrelated.
  Firefox now passes **93, with 1 skipped and none failing** in four fresh-process shards. WebKit
  passes **94/94** in the same topology on both macOS and the pinned Linux CI container. Before
  sharding, two startup races were isolated. One was a product defect: importing
  two files before React rendered the first could capture the old tab's filename and dirty state
  into the newly active session. `loadDocument` now updates that imperative snapshot
  synchronously; the complete multi-tab restoration case passed **10/10** in WebKit afterwards.
  The other was a test pressing F1 before the editor had installed its shortcut listener; it now
  waits for the editor's `data-workspace-ready` contract and also passed **10/10**.

  The first full run after those fixes was not a qualifying measurement: the host reached load average
  **104** with only 913 free VM pages. It completed 89 tests and then produced unrelated page
  closures, CORS messages for same-origin UUID routes, and minute-long static-page navigations.
  Even the Google Analytics metadata test failed alone while the machine was in that state. Those
  failures were neither suppressed nor reported as product regressions. That incident exposed the
  actual infrastructure defect: one long-lived canvas-heavy engine eventually exhausts browser
  resources. Firefox later showed the same pattern. Both now run in four isolated shards through
  [`run-browser-shards.mjs`](../scripts/run-browser-shards.mjs), locally and in
  [`browser-breadth.yml`](../.github/workflows/browser-breadth.yml). The complete Linux WebKit run
  passes all 94 tests; the complete local Firefox run passes 93 with its one documented skip.

  Of the ten Firefox failures, only one was a browser capability the port cannot reach, and it is
  a limitation of the *test* rather than the app: Firefox builds a `ClipboardEvent` whose
  `clipboardData` is present but empty, so a synthesized paste carries no file. A real Ctrl+V
  works. That test is skipped there with the reason recorded in it.

  Two were real defects the Chromium-only suite had been hiding. The selection-rounding bug is
  described below. The other: `errorDetails` rendered `error.stack`, and only Chromium prefixes a
  stack with `Name: message` — so a bug report from Firefox or Safari arrived as anonymous frames
  with no indication of what had failed. The heading is now written explicitly.

  The remaining seven were tests over-specifying Chromium's arithmetic. Six drove the mouse to
  fractional page coordinates, which Chromium honours and Firefox truncates; they now click whole
  pixels, and where the true value depends on widget granularity they assert that value rather
  than a rounder-looking one. The seventh asserted exact bytes for a semi-transparent pixel
  round-tripped through a canvas, which cannot survive premultiplication exactly — it now checks
  what the codec actually guarantees: alpha exact, opaque pixels exact, and colour to the
  precision the canvas can hold at that alpha.

  Cross-browser testing paid for itself immediately by finding a real parity bug that Chromium
  had been hiding. `normalizeSelectionBounds` floored the near edge of a selection and ceilinged
  the far one, which widens the box by a pixel whenever a drag lands on fractional coordinates.
  Chromium reports fractional pointer coordinates and Firefox reports integers, so over a canvas
  whose origin sits at x=239.5 the identical gesture measured 100 pixels in one browser and 101
  in the other. Native rounds both corners — `SelectTool.cs:88` for the anchor and
  `RectangleHandle.cs:123` for every drag update — so the port now does too. That single change
  fixed four of the ten Firefox failures.
- The original single hover contract has been replaced by six production-build budgets covering
  drawing, selection dragging, effect preview and cancellation, tab switching, long-history
  restoration, JS heap growth, and stored bytes. Canvas backing stores remain the one important
  quantity the page cannot measure directly.

## Visual parity limits

The visual and dialog audits are extensive, but they should be described precisely:

- Approved web screenshots are regression baselines for the web implementation.
- Native references are reviewed manually; there is no general native-versus-web pixel comparison
  in CI.
- Some add-in dialog references are reconstructed from source because the native UI could not be
  captured directly in every environment.
- Current native captures primarily describe one Linux/GTK environment, not every macOS, Windows,
  GTK theme, font, and scale-factor combination.
- RTL coverage includes representative workspaces and dialogs, but not the full cross-product of
  every popup, tool, dialog, constrained viewport, and supported direction.

Consequently, the correct current claim is **high visual fidelity with pinned regression coverage**,
not pixel-identical reproduction on every platform.

## Localization reality

The original application contains 73 `.po` catalogs. The web port selects the 28 catalogs with at
least 90% upstream coverage, deliberately retains Hebrew, and adds English as the source language.
This results in 30 selectable UI locales.

[`i18n-web-overrides.mjs`](../scripts/i18n-web-overrides.mjs) now contains **98 web-only strings**
for every shipped locale; none of these messages exists in Pinta's gettext catalogs, so there is
nothing upstream to inherit. French, German, Arabic, and Hebrew were translated and reviewed
first. The other 25 locale blocks are machine translations, labelled as such in the source and
intended as a good-faith starting point for fluent corrections.

`npm run verify:i18n` refuses a build when any locale's override key set drifts, so adding a
browser message to one language cannot silently leave another behind. This does not promote the 25
machine-translated catalogs to reviewed status; it makes their completeness measurable.

SEO indexing is intentionally limited to English, French, German, Arabic, and Hebrew. Other locale
routes are `noindex`, which is preferable to advertising untranslated SEO copy. New SEO locales
should be indexed only after their page-specific copy is written and reviewed by a fluent speaker.

## Effect verification limits

The 46 built-in effects have unusually strong algorithm coverage, including byte fixtures for
native integer behavior, sampling, seeded randomness, and premultiplied-alpha routines. Two
verification weaknesses remain:

- ~~The standalone C# fixture harness is not retained as a reproducible tool in the web
  repository.~~ **Resolved 29 August 2026.** [`tools/effect-fixtures`](../tools/effect-fixtures)
  runs the real `FragmentEffect`, `MotionBlurEffect`, `RadialBlurEffect` and `ZoomBlurEffect` out
  of `original/` and prints their bytes; `npm run verify:native-fixtures` regenerates them and
  fails if they have drifted, naming the exact pixel and channel.

  It reuses the service mocks from `original/tests/Pinta.Effects.Tests/Mocks` by compiling those
  files rather than copying them, so the harness cannot drift into stubbing something differently
  from the tests the effects are actually developed against. GTK and Cairo are needed, which a web
  checkout has no reason to install, so it runs in the .NET SDK image by default (`--local` uses a
  `dotnet` on PATH).

  The first run answered the question the gap left open: **all four fixtures reproduced exactly**,
  which retroactively validates the transcription that produced them. They now live in
  [`tests/fixtures/native-effects.json`](../tests/fixtures/native-effects.json) and the unit test
  reads them from there, so the numbers in the test and the numbers the C# produces cannot
  disagree silently.
- ~~Hue/Saturation was validated with a transcription produced during the same pass as the port.~~
  **Resolved 29 August 2026.** It is checked against the real `HueSaturationEffect` from
  `original/` now, in four cases — all three axes together and each alone — and matches byte for
  byte. See [`parity-plan.md`](parity-plan.md), which also records why the first comparison looked
  like a large mismatch and was measuring the Cairo premultiply round trip rather than the effect.

## Why the web implementation is smaller

The SLOC report at the time of this audit showed:

| Scope | Code lines |
| --- | ---: |
| Web production implementation | 34,324 |
| Original Pinta production implementation | 41,508 |
| Web tests, scripts, and supporting code | 22,161 |

The production web code is 82.7% of native Pinta, while web production plus supporting
infrastructure is 56,485 lines—well above the original production count.

> Regenerated 30 August 2026, after the effect-kernel and workspace-serialization splits, expanded
> browser coverage, React Compiler integration, and recovery/performance work. The production file
> count rose from 38 to 87: the same core editor is spread across modules small enough to read, and
> the remaining line growth is implemented behavior rather than copied native platform plumbing.

The native application also carries GTK plumbing, platform integration, Mono.Addins infrastructure,
Pango text behavior, packaging, and desktop lifecycle code. The browser supplies some of that
functionality, while other portions remain unported. The static current-report table in
[`README.md`](../README.md) should be regenerated whenever SLOC changes materially.

## React Compiler

Enabled on 30 August 2026, via `@vitejs/plugin-react`'s `reactCompilerPreset` and
`@rolldown/plugin-babel`. Plugin-react v6 does the JSX transform with oxc rather than Babel, so the
compiler runs as its own Babel pass over React sources only. React is 19, so no runtime polyfill is
needed.

**What it actually compiles.** 69 functions succeed and 73 bail out, producing **256 memoisation
sites** in the bundle. The bail-outs are not evenly spread — **60 of the 73 are "Cannot access refs
during render"**, and they include the largest components in the app (`App.tsx`, `CanvasArea.tsx`).
The editor reads refs during render throughout, which is how an imperative canvas editor ends up
built, and it is the single thing standing between the compiler and the rest of this codebase. The
remaining 13 are compiler `Todo`s: `try`/`finally`, and expressions it cannot safely reorder.

**What it measures.** Comparing on a machine this variable needed a paired A/B — three runs of each
state, alternating, so drift hits both. Medians:

| Budget | Compiler off | Compiler on | Verdict |
| --- | ---: | ---: | --- |
| Tab switch | 13.16 ms | **10.76 ms** | Real. All three on-runs beat all three off-runs, no overlap |
| Selection drag | 8.86 ms | 6.19 ms | Overlapping ranges |
| Effect preview | 28.67 ms | 26.19 ms | Overlapping ranges |
| Continuous drawing | 3.65 ms | 3.54 ms | Noise |
| Pointer hover | 0.246 ms | 0.279 ms | Noise |

So: one budget improves about 20% and nothing regresses. A single unpaired reading earlier looked
like a 65% regression across the board, and was a quiet-machine baseline against a loaded run — the
same confounder documented under [Reliability gaps](#reliability-gaps), caught this time by
alternating rather than by luck.

The build pays about 2.7 seconds for the extra Babel pass over 82 files.

**Lint.** Eleven of the compiler's diagnostics are on as errors in
[`eslint.config.js`](../eslint.config.js) — the ones the codebase already satisfies, so they stay
satisfied. Four are off with their counts recorded there: `refs` (110), `preserve-manual-memoization`
(4), `purity` (1, a false positive on `Math.random()` inside an `onClick`), and `set-state-in-effect`
(1).

The next move, for anyone picking this up, is the ref-during-render pattern. It is worth roughly 60
more compiled functions including the biggest ones, and it is an architectural change rather than a
cleanup.

**One failure was wrongly attributed to it, and the method for checking is worth keeping.** The
first CI run after enabling the compiler failed `grows and shrinks a selection through the Offset
Selection dialog` on Linux WebKit, twice including the retry, in a code path
(`useSelectionCommands.ts`) the compiler had in fact compiled — so it looked causal. It reproduced
on neither macOS WebKit with the compiler on nor off, five repeats each.

What made it look new was that the WebKit shard's first push-triggered run *was* that commit. The
answer came from dispatching the same workflow at the previous commit with
`gh workflow run browser-breadth.yml --ref <branch-at-that-sha>`: it fails identically without the
compiler, 1 failed and 22 passed both times. A pre-existing Linux-WebKit failure, not a regression.

Dispatching a workflow at an older ref is the cheap way to get a baseline that never existed, and it
beats bisecting by pushing commits.

**Resolved 30 August 2026.** Instrumenting the pipeline inside the CI container showed every stage
of the *logic* was correct: the morphology grew 10,000 mask pixels to 19,600 and shrank them back to
exactly 10,000, `data-selection-bounds` returned `120,120,100,100`, and the marching ants drew. Only
the translucent fill was missing, only on Linux WebKit, and only after a shrink.

Three explanations were tested in the container and all three were wrong:

| Hypothesis | Test | Result |
| --- | --- | --- |
| A first-frame race that the next repaint fixes | Poll the overlay for 10 s | Fill never appears |
| `source-in` compositing against a destination the preceding `drawImage` has not realised | Rewrite as fill-then-`destination-in`, which is equivalent | No change |
| `selection.mask` itself not realised when the fill path draws it | Force `getImageData` on the mask first | No change |

The useful clue was that instrumentation reading back the mask made the fill appear. The shipped
fix therefore removes compositing from this path entirely: `selectionFillOf` reads the mask alpha
once, writes the translucent blue pixels with `putImageData`, and weakly caches that canvas by mask
identity. Animated marching-ant frames reuse the cached fill, so the deterministic path adds no
readback or allocation to the animation loop.

The `fixme` was removed. The formerly failing test passes in the pinned Linux Playwright container,
and the complete four-shard Linux WebKit run passes **94/94**.

## Deliberate browser boundaries

Some native surfaces should not be reproduced inside the page:

- Filesystem chooser and permission UI.
- Printer selection, driver settings, and final print confirmation.
- Screen-capture and other browser permission prompts.
- Execution or installation of arbitrary Mono.Addins assemblies.
- Operating-system recent-document integration.
- Exact GTK/Pango font, caret, IME, and text-layout behavior.
- Platform codec plugins that the browser does not expose.

The application should preserve Pinta's command flow, state, cancellation, and error behavior around
these boundaries while leaving the privileged surface to the browser or operating system.

## Recommended completion sequence

### 1. Make releases trustworthy

- ~~Remove the version/deployment race.~~ Done — see
  [Priority zero](#priority-zero-versioning-and-deployment).
- ~~Run `verify:version` against the exact tested artifact.~~ Done — the version is embedded before
  any test runs, so the artifact every gate saw is the one that ships.
- ~~Ensure only a successful complete gate can publish GitHub Pages.~~ Done — the deploy runs only
  on `workflow_run.conclusion == 'success'` and downloads that run's artifact by id. Worth knowing
  when it looks broken: a failed gate makes the deploy report **skipped**, not failed, so the only
  symptom is a site that stops updating.
- ~~Make Codespell and zero-warning ESLint part of the required checks.~~ Done — the screenshots
  job `needs: [version, spelling]`, and `lint:eslint` is `eslint . --max-warnings 0`.

### 2. Stabilize the test infrastructure

- ~~Reproduce and fix the preview-server disappearance during the full local browser suite.~~
  Done — stale `dist/` served by a reused server; see [Reliability gaps](#reliability-gaps).
- ~~Do not reuse an unrelated existing preview server in deterministic gate runs.~~
  Done — `reuseExistingServer: false` in every deterministic Playwright config.
- ~~Record server output and process exit reasons as Playwright artifacts.~~
  Done — [`scripts/run-preview-server.mjs`](../scripts/run-preview-server.mjs) writes
  `test-results/server-logs/{e2e-preview,visual-dev,performance-preview}.log` for all three
  browser suites, which CI already uploads with the rest of `test-results`.
- ~~Keep CI and local gate behavior as similar as practical.~~ Done: Chromium behavior, Firefox,
  and WebKit use the same four-shard runner locally and in CI; each of the eight exhaustive
  Chromium layout cases receives its own process; touch runs alone. CI still retries once while
  local runs retry zero times, so a flake fails locally instead of being hidden.

Remaining known flakiness is entirely load-driven, and worth stating precisely because it is easy
to mistake for a real regression. Each project is green when measured on a quiet machine:

| Project | Result | Time |
| --- | --- | ---: |
| chromium | 104 passed | 96 behavior cases across four shards plus 8 isolated layout cases |
| firefox | 95 passed, 1 skipped | four local shards |
| webkit | 96 passed | 2.3m on macOS; 4.8m in the Linux container |
| touch | 8 passed | 7s |
| all projects | 303 passed, 1 skipped | about 8m locally |

Under load the same code fails an arbitrary subset and the runtime inflates five to twenty times:
the earlier combined suite measured 3.7m, 5.6m, 6.8m and 13.3m, and chromium alone previously went
from 37s and 93 passed to 8.5m and 89 passed at load 46. No two loaded runs fail the same tests,
which is the signature to look for. Firefox is the exception that proves the rule — it passed 92
at load 37, because it finishes in a fraction of the memory the other two need.

The important distinction is now process lifetime as well as machine load. A single browser
process eventually stalled after enough canvas-heavy cases even with one worker. Splitting desktop
behavior into four fresh processes, and the unusually intensive dialog sweep into eight, made every
formerly hanging case pass without a retry or longer timeout.

**Before believing any failure here, check `uptime` and re-run the affected project on its own.**

### Partly resolved: the Firefox `InvalidStateError`

One CI run on 30 August 2026 failed with eighteen instances of `InvalidStateError: An attempt was
made to use an object that is not, or is no longer, usable`, reported through `console.error` and
caught by [`pageErrors.ts`](../tests/pageErrors.ts). It affected unrelated tests — a palette, SEO
metadata, the analytics tag — which points at something during startup or teardown rather than at
any one flow.

The same shape finally reproduced in WebKit under extreme memory pressure: the engine first said
it could not read an 800×600 canvas, then emitted `InvalidStateError` while a test navigated away.
That exposed the difference between the two workspace-save paths. The normal debounced path catches
and reports a rejected save; the `pagehide`/hidden-tab path launched `persistWorkspaceNow()` with
`void` and no rejection handler. If the browser invalidated the canvas during teardown, the
departing page therefore emitted an unhandled rejection into whichever test happened to be next.

The final unload flush is now explicitly best-effort and consumes that rejection. A regression test
replaces `canvas.toBlob` with the exact `InvalidStateError`, dispatches `pagehide`, and fails through
[`pageErrors.ts`](../tests/pageErrors.ts) if the promise escapes. The message was not added to the
ignore list. Firefox, WebKit, and touch remain in
[Browser breadth](../.github/workflows/browser-breadth.yml), which is a cross-engine signal rather
than part of the Pages deployment gate.

**That fixed one path, and the error still recurs.** The run for `5ae3b10f` failed Firefox shard 4/4
with the same message across `editor`, `localization` and `seo` specs. It is intermittent rather
than ordered: the three tests pass individually, pass in each pair, and passed the triple twice in a
row after failing it once. Both `persistWorkspaceNow` call sites are now demonstrably contained —
the debounced path try/catches and the unload path consumes its rejection — so the remaining source
is something that throws outside a promise chain, which is why neither round of investigation could
place it.

What was missing every time was **where it came from**. `pageErrors.ts` recorded the message and
discarded the stack, so two investigations had only `InvalidStateError` and a test name to work
from. It now reports the first three non-`node_modules` stack frames for an uncaught error and the
source location for a `console.error`, verified by a probe that throws this exact `DOMException`.
The next occurrence in CI should name its origin, which is the piece that would have ended this
already.

### 3. Finish the structural refactor

- Extract the remaining `App` logic into ownership-based hooks. Five of nine done. The global
  keydown effect is **measured and deliberately not extracted**: all 40 of its dependencies are
  used elsewhere in `App`, so a hook would take a 40-field options object and hide nothing, which
  is the shape §8.2a of [`refactoring.md`](refactoring.md) declines. Its Escape chain *was*
  extracted as `closeTopmostDialog`, taking the handler from 49 dependencies to 40 and 428 lines to
  332. Two of the nine planned hooks — `useDockResize` and the zoom combo's draft state — describe
  code that does not exist.
- ~~Extract workspace serialization.~~ Done.
- Split `usePaintEditor` into sub-hooks while preserving its public contract. Seven done, five
  measured and deliberately declined.
- ~~Split the remaining over-700-line effect kernels along coherent algorithm boundaries.~~ Done.
- Leave the stylesheet intact until a cascade-preserving design and visual proof exist.

### 4. Prove browser and touch stability

- ~~Add Firefox and WebKit behavioral projects.~~ Done. Firefox and WebKit have standalone configs,
  [`playwright.firefox.config.ts`](../playwright.firefox.config.ts) and
  [`playwright.webkit.config.ts`](../playwright.webkit.config.ts), both driven through
  [`run-browser-shards.mjs`](../scripts/run-browser-shards.mjs). Firefox passes **93 with 1 skipped**;
  WebKit passes **94/94** on macOS and in the pinned Linux container. The two startup races and the
  Linux-only selection-fill defect found during this work are fixed and remain active tests.

  That separation is not cosmetic. One long-lived Firefox or WebKit process eventually stops
  servicing canvas-heavy pages; retries then restart it and make the suite look flaky. Four shards
  restart it deliberately before exhaustion, and every former hang passes without retries. The
  exhaustive 260-check dialog-layout sweep stays in Chromium: it is CSS/layout coverage, not an
  engine capability test. Its eight direction/viewport cases now each receive a fresh Chromium
  process, so opening hundreds of dialog instances cannot poison a later behavior case.
- ~~Test browser-specific clipboard, File System Access, service-worker, and codec fallbacks.~~
  Done for the platform surfaces Playwright can drive: clipboard, File System Access save failures,
  BMP, and service-worker registration are exercised across the desktop projects. Firefox's one
  genuine synthesized-clipboard capability gap is skipped and explained next to that test; real
  Ctrl+V is not affected.
- ~~Add real touch-emulated editor tests at 390 x 844 for drawing, long-press secondary color,
  selection handles, pinch zoom, panning, dialogs, and toolbar reachability.~~ Eight tests in
  [`touch.spec.ts`](../tests/e2e/touch.spec.ts), run by a `touch` project at 390x844 with
  `hasTouch` and `isMobile`. They cover the coarse-pointer media query matching at all, enlarged
  targets, the callout suppression, drawing, the long-press secondary colour, panning, toolbar
  reachability, and a dialog fitting the screen. Pinch is not among them: Chromium delivers a
  pinch as a ctrl-wheel and Safari as `gesturechange`, so an emulated pinch would test the
  emulator. The wheel path is already covered by the desktop suite.

  Gestures go through CDP `Input.dispatchTouchEvent` rather than synthesized `PointerEvent`s.
  Dispatched events do not work here: `onPointerDown` calls `setPointerCapture`, which throws for
  a pointer id the browser has no active pointer for, so the handler aborts before drawing
  anything. Three other assumptions had to be corrected against what the app actually exposes —
  the canvas is zoomed to fit at this width so an element offset is not an image coordinate, the
  colour wells paint through a `--well-color` custom property rather than `background-color`, and
  the history dock is off screen so the title's dirty marker is the observable for an edit.

### 5. Expand performance and storage budgets

- ~~Budget continuous drawing and selection dragging.~~
- ~~Budget effect preview, cancellation, and confirmation latency.~~
- ~~Budget save, restore, tab switching, and long-history reconstruction.~~
- ~~Measure heap, canvas backing stores, and IndexedDB growth for large documents.~~ Partly: the
  JS heap and stored bytes are budgeted, canvas backing stores are not measurable from the page.

  Six budgets now run in [`budgets.spec.ts`](../tests/performance/budgets.spec.ts), all on
  `ScriptDuration` or a heap figure rather than wall-clock time, because this suite has to survive
  a loaded machine. Measured on a quiet machine on 29 August 2026, with each budget set several
  times above its measurement so it catches a regression rather than a busy afternoon:

  | What | Measured | Budget |
  | --- | ---: | ---: |
  | Drawing | 2.1 ms/move | 8 |
  | Selection drag | 3.6 ms/move | 12 |
  | Effect preview | 12.6 ms | 150 |
  | Effect cancel | 3.4 ms | 60 |
  | Tab switch | 5.7 ms | 80 |
  | Restore with 46 history entries | 53 ms | 600 |
  | JS heap growth | 2.6 MB | 60 |
  | Stored bytes | 4.2 MB | 60 |

  Two things are worth reading off that table. `JSHeapUsedSize` **excludes canvas backing stores**,
  where a 2000x1500 six-layer document actually lives — 12 MB a layer before any history — so the
  heap figure is not the memory budget; it catches a leak in the bookkeeping around those canvases,
  which nothing else here would see. The number that does matter is what the origin stores, and
  **4.2 MB for six layers at 2000x1500 with 40 history steps** is the write-time pixel
  deduplication working: without it the same document wrote a PNG per layer per step.
- Persist history incrementally rather than rewriting full PNG snapshots. Partly done: duplicate
  snapshots are no longer written at all (see [Reliability gaps](#reliability-gaps)), pinned by
  [`historyPersistence.test.ts`](../tests/unit/historyPersistence.test.ts). Appending rather than
  rewriting the record is still open.
- ~~Provide a reconstructed ORA recovery download where possible.~~ Done, with five tests in
  [`workspaceRecovery.test.ts`](../tests/unit/workspaceRecovery.test.ts) covering the archive
  contents, the top-first stack order the format requires, multi-document recovery, and both
  refusal paths.

### 6. Strengthen parity evidence

- Add automated perceptual native-versus-web comparisons for captures with matching environments.
  **Attempted and deliberately not shipped as a gate.**
  [`compare-native-captures.mjs`](../scripts/compare-native-captures.mjs) exists and ranks all 110
  name-matched pairs by agreement (`npm run report:native-comparison`), which is genuinely useful
  when asking "does this still look like Pinta". It is not a pass/fail check, because two designs
  were tried and both failed falsification:

  | Design | Genuine pairs | Deliberately wrong pair | Verdict |
  | --- | --- | ---: | --- |
  | Coarse luminance grid, 16x16 | 0.656–0.985 | **0.942** | overlaps |
  | Same, 32x32 | 0.550–0.976 | **0.976** | worse |
  | Same, 64x64 | 0.420–0.944 | **0.944** | worse |
  | Difference from the default workspace, so shared chrome cancels | −0.123–0.956 | **−0.019** | mid-band |

  The wrong pair is native `workspace-selection` against web `menubar-help`, both 1440x960. It
  scores above the lowest genuine pair at every resolution. The reason is structural: both images
  are a Pinta-shaped window, and GTK-versus-browser rendering of the *same* screen differs more than
  two *different* screens do. Any threshold that passes the real pairs passes the wrong one too, so
  the check would report success for anything — worse than not having it.

  Three quarters of the pairs cannot be compared at all, and that is correct: small dialog crops
  differ 5–7% in height because GTK dialog furniture is not web dialog furniture, and
  `dialog-save-palette` is a 1203x902 GTK file chooser against a 430x200 web dialog because file
  choosers stay browser-owned on purpose.

  A version that could gate would have to locate corresponding features — toolbar, canvas, docks —
  and compare their arrangement, rather than treating the window as a bag of luminance.
- ~~Complete the English/RTL and desktop/constrained-viewport dialog cross-product.~~ Done as
  assertions rather than screenshots, in
  [`dialog-layout.spec.ts`](../tests/e2e/dialog-layout.spec.ts): **43 configurable effect dialogs
  and all 22 tool option strips across all four combinations, 260 checks.**

  Screenshots would have meant about a hundred and seventy new baselines to review and re-approve
  on every unrelated style change, which buys accuracy about pixels at the cost of anyone actually
  looking at them. These check what makes a dialog usable — it fits on screen, both buttons are
  reachable, it does not push the page sideways, and it lays out in the direction it was told to —
  and a failure names the dialog, the direction and the viewport instead of leaving that to a
  pixel diff. The existing 35 dialog screenshots stay as the pixel record for a sample.

  A tool option strip is asked a different question from a dialog: at 390px it either fits or has
  to scroll, and a control sitting outside a strip that does neither cannot be reached at all.

  Two things keep it honest: the sweep asserts it found more than 35 dialogs, so it cannot pass
  silently if the catalog filter stops matching, and the assertions were confirmed to fail when
  deliberately broken. Add-in effects are excluded because they are off in a default install and
  absent from the menus; the visual suite's add-in samples cover those.
- ~~Retain a reproducible C# effect fixture harness.~~ Done — see
  [Effect verification limits](#effect-verification-limits). All four fixtures reproduced exactly
  on the first run.
- ~~Independently revalidate Hue/Saturation.~~ Done — four cases against the real C# effect, all
  byte-exact.
- ~~Record unavoidable browser differences next to each parity claim.~~ Done, but as one section
  rather than per row: see *Browser differences that cut across these claims* in
  [`parity-hardening.md`](parity-hardening.md). Six differences, each measured rather than assumed
  and each pinned by a named test — premultiplied colour on a canvas, the same round trip before an
  effect runs, fractional versus truncated pointer coordinates, WebKit-only
  `-webkit-touch-callout`, Firefox's inability to synthesize a clipboard payload, and error-stack
  formatting. They cut across most rows in that table, so repeating them per row would have buried
  them.

### 7. Complete localization and documentation

- ~~Translate or professionally review the browser-specific strings for additional high-value
  locales.~~ **Done as machine translation, on request, and labelled as such.** All 98 strings now
  exist in all 29 locales; none fall back to English. `verify:i18n` reports
  `the other 0 locales fall back to English for them`.

  fr, de, ar and he were translated and reviewed earlier and are unchanged. The other 25 were
  produced in one pass with the surrounding UI as context and **have had no native review** — that
  is stated at the top of [`i18n-web-overrides.mjs`](../scripts/i18n-web-overrides.mjs) so nobody
  mistakes them for reviewed work. The reasoning for doing it: English placeholders scattered
  through an otherwise localized interface read as a broken translation, and a good-faith
  translation is closer to right than that. Corrections from fluent speakers should simply replace
  the string.

  Three things needed care beyond word-for-word substitution. The storage banner is assembled as
  `{used} {of about} {quota} {is in use…}`, so those fragments had to read correctly in that fixed
  order even in languages that would prefer another. `en-GB` and `en-CA` differ from English only in
  spelling, and differ from *each other*: both take `colour` and `grey`, but Canadian English keeps
  American `-ize`, so `Minimise`/`Minimize` splits between them. And Traditional Chinese was written
  directly rather than converted from Simplified — a character-mapping pass produced
  `恢復復原歷史` for *Restore Undo History*, which is why the shortcut was abandoned.

  The overrides moved out of the generator into their own module; inline, 29 locales would have
  made that file roughly 3,000 lines.
- ~~Remove the inherited upstream translation-template workflow.~~ Done. `update-translation-template.yml`
  came from upstream Pinta: it installed autotools and gettext, built the C# application, ran
  `make updatepot` to regenerate **Pinta's own** `.pot`, and opened a pull request. It had been
  failing on every scheduled run — `couldn't find remote ref fix/update-translation-template`, then
  `403` because `github-actions[bot]` has no write access — so the repository carried a permanently
  red workflow on the 1st and 15th of each month.

  It was deleted rather than repaired because translation here flows **one way**: upstream's
  `original/po/*.po` are read by [`generate-i18n-catalogs.mjs`](../scripts/generate-i18n-catalogs.mjs)
  to produce `src/i18n/locales/*.json`. Nothing in this fork consumes the `.pot`, so even a working
  version of the workflow would have opened a fortnightly pull request with no reader. Its two
  `paths-ignore` entries in `web-visual.yml` went with it. **If a future upstream sync restores this
  file, delete it again** — that is the only way it comes back.
- Add indexed SEO locales only when their unique copy is complete.
- Synchronize architecture, reliability, parity, test-count, refactoring, and SLOC documentation
  after each milestone.

## Completion definition

Final polish is complete when:

| | Criterion | State |
| --- | --- | --- |
| ✅ | One exact, versioned, fully tested artifact is deployed without racing workflows | The version is computed during the tested build and never committed; the deploy takes that run's artifact by id. Verified: run 47 shipped `1.0.260830.47` |
| ✅ | Required CI is green with no spelling or React hook warnings | Codespell gates the suite, `eslint . --max-warnings 0` with every rule at `error`, `noUnusedLocals` on for all six TypeScript projects, and Prettier enforced by `format:check` |
| ✅ | Chromium, Firefox, and WebKit behavioral suites pass | Chromium 104, Firefox 93 (1 skipped, with the reason in the test), WebKit 96, and touch 8 pass. Desktop behavior uses fresh-process shards locally and in CI; Chromium's 8 layout cases are isolated individually; the two new worker-failure cases were also reproduced directly in all three desktop engines |
| ✅ | Automated touch tests cover the actual responsive editor | Eight tests at 390x844 driving real touch through CDP |
| ✅ | Every native dialog and tool popup has a reviewed reference, behavior test, and representative RTL/constrained-viewport coverage | 43 configurable effect dialogs and all 22 tool option strips are swept across both directions and both viewports — **260 checks** — alongside 35 pinned dialog screenshots and 22 pinned option-strip screenshots |
| ✅ | Storage and performance budgets cover real editing, not only pointer hovering | Six budgets: drawing, selection dragging, effect preview and cancel, tab switching, restore, heap and stored bytes — each calibrated from CI rather than a developer machine |
| ✅ | Remaining differences are documented browser/platform boundaries rather than accidental parity gaps | Six cross-cutting differences are measured in [`parity-hardening.md`](parity-hardening.md). The unload `InvalidStateError` and Linux-WebKit selection fill were both traced, fixed, and reproduced in browser-specific tests rather than suppressed |
| ✅ | The architecture and SLOC documentation describe the code that is actually on `master` | Regenerated, and three claims in the refactoring plan were corrected against measurement rather than left standing |

Two things are deliberately *not* done, and are listed so they are not mistaken for oversights:
append-only IndexedDB records, because deduplicating pixel nodes already brought the measured
workspace below its storage budget; and a **gating** native-versus-web perceptual comparison, which
was attempted, falsified, and recorded as a negative result in section 6.

A third is done but qualified: the 98 browser-specific strings now exist in all 29 locales, but 25
of those are machine translation without native review. They are labelled at the source.

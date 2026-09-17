# Promo page and promotion plan

A second, differently angled landing page now lives at <https://paint.rip/promo/>. Where
`/about/` is a feature tour ("here is everything the editor does"), `/promo/` argues a single
use case: **the free paint app you open when a design has to exist in the next ten minutes.**

The angle is borrowed from an
[XDA piece on desktop Pinta](https://www.xda-developers.com/open-source-app-secret-weapon-for-quick-designs/),
which frames Pinta as a quick-design tool rather than a Photoshop replacement. That framing is
the useful part and it is not owned by anyone. **Every word on `/promo/` is original and every
screenshot is Pinta Online's own** — no XDA copy, no XDA images, and no suggestion that XDA
endorsed this port. If the page ever quotes or cites the article, it must say plainly that the
article is about desktop Pinta.

## What shipped

| Path | Purpose |
| --- | --- |
| `promo/index.html` | The landing page. Hand-maintained, English only for now. |
| `promo/promo.css` | Its stylesheet. Same design tokens as `about.css`, different layout. |
| `web-assets/promo/*.webp` | 25 screenshots, copied to `/promo/assets/` at build time. |

Build wiring, all in `vite.config.ts`:

- `promo` added to `build.rollupOptions.input`, so the page is processed and the version
  placeholder is substituted.
- A `viteStaticCopy` target maps `web-assets/promo/*` to `promo/assets`.
- `promo` added to the PWA `navigateFallbackDenylist`. **This one matters**: without it the
  service worker answers `/promo/` with the editor shell for anyone who has the PWA cached.

Elsewhere:

- `web-assets/analytics.js` labels the route `Promo`, so it is a distinct page in GA4 and Ads
  instead of falling into `Other`.
- `scripts/generate-seo-locales.mjs` emits `/promo/` into the sitemap. The sitemap is generated,
  never hand-edited — run `npm run seo:sync` and `npm run verify:seo`.
- `about/index.html` footer links to `/promo/`. An orphan page with no inbound internal link is
  a page Google will crawl late and rank poorly.
- `tests/e2e/seo.spec.ts` gained a promo case (canonical, title, description, ≥20 screenshots
  that all return real bytes, intrinsic dimensions on every image, `FAQPage` entries that match
  the rendered questions) and the sitemap assertion moved from 11 entries to 12.
- `tests/unit/analytics.test.ts` covers the new `Promo` label.

## Screenshot map

The screenshots are **not** the visual suite's fixtures. Those exist to make pixel regressions
obvious, so they are deliberately synthetic — an empty white canvas, or a saturated test
pattern — which shows the UI without showing what the UI is *for*.

Instead, `scripts/capture-promo-shots.mjs` drives the real editor, composes an actual piece of
artwork, and photographs the app working on it:

```bash
npm run preview   # in one shell
node scripts/capture-promo-shots.mjs
```

The composition begins with the original `web-assets/source/cosmic-garden.png` illustration and
adds a shape-tool portal halo, a line-tool signal, and live on-canvas type on four layers named
Cosmic Garden / Portal Halo / Signal Line / Headline. The Layers dock in the hero therefore shows
a real stack and real thumbnails rather than one "Background" row. Everything visible in the UI
is genuine application output; nothing is mocked or painted in afterwards.

| Section | Shots |
| --- | --- |
| It's lightweight | `new-image`, `palette`, then `step-backdrop` → `step-circle` → `step-shapes` → `step-headline`, the campaign gaining an editable layer in each frame |
| Full layer support | `file-drop`, `layer-menu`, `layer-properties`, `layer-rotate-zoom` — UI crops, still taken from the visual suite |
| An impressive toolkit | `select-rectangle`, `select-ellipse`, `select-magic-wand`, `draw-line`, `draw-freeform`, `gradient-radial` — every one over the finished artwork |
| A real photo editor | `adjustments-menu`, `adjust-hue-saturation`, `effect-oil-painting`, `effect-motion-blur`, `effect-pencil-sketch`, `effects-menu` |
| Cross-platform | `save-as`, `workspace-light`, `workspace-narrow` |

Three decisions in that script worth keeping:

- **Effects are captured after Flatten.** An effect applies to the active layer, so on the
  layered document it repaints the backdrop and leaves the shapes and headline crisp on top —
  which reads as "nothing happened" at thumbnail size.
- **Effects are shown by their result, not their dialog.** A modal dims and blurs whatever is
  behind it, so a whole-window shot of an open dialog sells the artwork badly. The cropped
  Adjustments and Effects menus already prove the controls exist.
- **The gradient is drawn inside a selection.** A gradient fills its entire layer, which would
  bury the picture; confining it shows the tool and the artwork in one frame.

Encoding is `cwebp -q 85`. Lossless tripled the page weight for a difference invisible even on
the dock's 11px labels — though the small flat UI crops (the menus, the dialogs) are still
lossless, because for those lossless is genuinely smaller. About 480 KB of imagery total,
everything below the hero lazy-loaded, and every `<img>` carries intrinsic `width`/`height` so
the gallery cannot shift layout as it loads.

If a cropped menu shot needs redoing, recrop with
`magick <src>.png -crop WxH+X+Y +repage <out>.png` and update the `width`/`height` attributes
in the HTML to match, or the e2e dimension check fails.

## On-page SEO

| Field | Value |
| --- | --- |
| Title | `Free Online Paint & Photo Editor for Quick Designs \| Pinta Online` |
| Canonical | `https://paint.rip/promo/` |
| Structured data | `WebPage`, `ImageObject`, `BreadcrumbList`, `WebSite`, `SoftwareApplication`, `FAQPage` |
| OG type | `article` (the editor and about pages use `website`) |

`FAQPage` is the piece `/about/` does not have. Do not expect FAQ rich results from it: since
2023 Google shows those only for well-known government and health sites. The markup still
describes the page accurately, and each answer in the JSON-LD is a verbatim match for a rendered
`<summary>` — Google penalizes markup that describes content the page does not actually show, and
the e2e test enforces the match. The visible questions are what earn rankings for question-style
queries.

### Keyword targets

`/about/` chases feature queries. `/promo/` deliberately chases intent and comparison queries so
the two pages do not compete for the same results:

| Query family | Examples |
| --- | --- |
| Intent | free online paint app, quick design tool, make a graphic fast |
| No-install | image editor no download, browser paint no install, online paint no account |
| Comparison | paint.net alternative online, MS Paint alternative browser, free Photoshop alternative online |
| Platform | image editor for Chromebook, paint app for Linux, online editor that works offline |
| Capability | online image editor with layers, free editor with curves adjustment |
| Generic editor | edit images online, paint online photo editor, online image paint, online painting editor |

`/about/` owns the Pinta-branded queries instead: pinta online, pinta editor, pinta image
editor, pinta photo, pinta blur, pinta website. See [`search-queries.md`](search-queries.md) for
the Search Console audit behind both lists.

### The duplicate-content risk, stated plainly

Two pages selling the same product to the same search engine is a doorway-page pattern if the
copy overlaps. It does not here — different headline, different argument, different screenshots,
different FAQ, different keyword families — and that separation has to be maintained. **If a
future edit makes `/promo/` read like `/about/`, consolidate them rather than keeping both.**
The signal to watch in Search Console is the two URLs trading positions for the same query.

## Promotion checklist

Ordered roughly by effort-to-payoff. Nothing here involves buying links, spinning up satellite
domains, or posting under assumed identities; those tactics get a domain deindexed and are not
worth it for a free open-source project.

### Do first — indexing and listings

- [ ] **Google Search Console** — verify `paint.rip`, submit `https://paint.rip/sitemap.xml`,
      then request indexing for `/promo/` directly. Watch Core Web Vitals and the FAQ rich-result
      report over the following weeks.
- [ ] **Bing Webmaster Tools** — same sitemap. Bing feeds DuckDuckGo, and it indexes small sites
      faster than Google does.
- [ ] **AlternativeTo** — list Pinta Online as a web alternative to Paint.NET, MS Paint,
      Photoshop, and GIMP. This is consistently the highest-value listing for a tool like this,
      because the comparison queries above are exactly what it ranks for.
- [ ] **Product Hunt** — a launch is worth one shot; pick a Tuesday–Thursday, lead with the
      offline-PWA and no-upload angles rather than "it's like Paint".
- [ ] **Slant, SaaSHub, Openbase-style directories** — low effort, steady trickle.
- [ ] **Wikipedia's Pinta article** — do *not* add a promotional link. If a web port is
      genuinely notable it can be mentioned neutrally, but let someone else make that edit.

### Open-source channels

- [ ] **Tell the upstream Pinta project.** Open a discussion (not a PR) on
      `PintaProject/Pinta` introducing the port. This is a courtesy first and a link second —
      and it matters that the page already credits Pinta and links back to
      <https://www.pinta-project.com>.
- [ ] **`awesome-selfhosted`, `awesome-webapps`, `awesome-design-tools`** — read each list's
      contribution rules before opening a PR; several require a specific entry format and reject
      anything that reads like an ad.
- [ ] **GitHub repo topics** — `image-editor`, `pwa`, `webassembly`-adjacent tags, `pinta`,
      `paint`, `react`. Add the `paint.rip` link to the repo's About sidebar.

### Communities — read the rules first

Every one of these has a self-promotion policy, and a project account that only ever posts its
own link gets banned. Participate normally, then post the project once.

- [ ] **Hacker News** — `Show HN: Pinta Online – the Pinta image editor, ported to the browser`.
      Post it yourself, do not ask for votes, and be present in the comments for the first few
      hours. The port story (C#/GTK to React, effect parity testing against the native
      implementation) is the interesting part to that audience, not the feature list.
- [ ] **Reddit** — r/opensource, r/webdev, r/chromeos (genuinely useful there), r/linux_gaming
      and r/GIMP only if relevant to a thread. Answer "what can I use instead of Paint?" threads
      honestly, including when the answer is not this.
- [ ] **Lobsters, /r/InternetIsBeautiful, Hacker News' `Ask HN` threads** — same rules.

### Earned coverage

- [ ] **Pitch the outlets that already cover this niche** — XDA, It's FOSS, OMG! Ubuntu,
      Linux Uprising, Ghacks, FossMint. A short pitch beats a press release: what it is, why a
      browser port is non-obvious, one screenshot, one link. XDA in particular has already
      written about desktop Pinta, so a browser port is a natural follow-up for them — but pitch
      it as news, never imply they have covered it.
- [ ] **Have the assets ready before pitching**: `/about/assets/pinta-online-og.jpg` for social
      cards, the workspace shots in `web-assets/promo/`, and a two-sentence description that a
      journalist can paste.

### Measurement

- [ ] Confirm `Promo` appears as its own page in GA4 (it will only fire on `paint.rip` itself —
      the analytics bootstrap is disabled off the production host).
- [ ] Set up a Search Console query report filtered to `/promo/` and check after four weeks
      whether it is winning the comparison queries or cannibalizing `/about/`.
- [ ] Track the Google Ads page-view conversion already wired into the page.

## Possible follow-ups

- **Localize `/promo/`.** The `fr`, `de`, `ar`, and `he` about pages are generated by
  `scripts/generate-seo-locales.mjs`; the promo page is hand-maintained and English only.
  Localizing it means moving its copy into that generator, adding `hreflang` alternates, and
  extending the sitemap's alternate handling to a third page kind.
- **More angle pages.** `/promo/` is one intent. Others worth a page each, if and only if each
  gets genuinely distinct copy: a Chromebook/school-device page, a "no upload, nothing leaves
  your machine" privacy page, and a head-to-head comparison page.
- **Visual regression coverage.** `tests/visual/about.spec.ts` pins the about and guide heroes.
  The promo page has no baseline yet; adding one means generating it through the containerized
  runner (`npm run test:visual`), not locally, or the baseline will not match CI.

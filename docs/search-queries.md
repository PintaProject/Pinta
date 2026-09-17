# Search query audit: `/about/` and `/promo/`

Search Console snapshot reviewed on 2026-09-16 (query, clicks, impressions, CTR, average
position), checked against the two static landing pages and against what currently ranks for
each query.

| Query | Clicks | Impr. | CTR | Pos. | Owner page | Verdict before this change |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| pinta online | 1 | 14 | 7.1% | 3.2 | `/about/` (and `/`) | Brand in titles, but neither H1 said "Pinta Online" or "online" |
| pinta blur | 0 | 3 | 0% | 9.3 | `/about/` | "blur" appeared once, in a feature list; no how-to |
| pintrip | 0 | 3 | 0% | 43.0 | — | Not ours: Pintrip is a Danish motorhome app |
| pinte online | 0 | 2 | 0% | 23.0 | — | Misspelling; Google corrects it to "pinta online" |
| pinta editor | 0 | 2 | 0% | 26.5 | `/about/` | Only "Free Web Image Editor" in the title |
| pinta photo | 0 | 2 | 0% | 45.5 | `/about/` | "photo editor" appeared nowhere on either page |
| редактировать картинку онлайн paint | 0 | 2 | 0% | 80.0 | — | No indexable Russian page exists (see below) |
| pinta website | 0 | 1 | 0% | 5.0 | `/about/` | Navigational for pinta-project.com; relationship FAQ existed |
| edit images online | 0 | 1 | 0% | 7.0 | `/promo/` | Phrase was only in the editor shell's meta description |
| pinta image editor | 0 | 1 | 0% | 50.0 | `/about/` | Exact phrase absent from title, H1, and body |
| paint online photo editor | 0 | 1 | 0% | 82.0 | `/promo/` | "online paint" in title, "photo editor" absent |
| online image paint | 0 | 1 | 0% | 86.0 | `/promo/` | Title only |
| online painting editor | 0 | 1 | 0% | 87.0 | `/promo/` | Weak: generic head term, heavily contested |

The split follows [`promo.md`](promo.md): `/about/` owns Pinta-branded queries, `/promo/` owns
generic editor and intent queries, so the two pages do not compete with each other.

## Who ranks above us

For "pinta online" the results are the upstream project, Wikipedia, download mirrors, and two
services that stream desktop Pinta from a remote machine (rollApp and OnWorks), plus an
unrelated Latin American art fair called Pinta. Paint.rip is the only result where Pinta
actually runs in the browser, and the pages did not say so. The About hero now does, without
naming the streaming services.

For "pinta blur" the results are the upstream user guide and third-party how-to posts. A short,
accurate answer on our own feature page can compete with those.

## What changed

### `/about/`

- **Title:** `Pinta Online Features – Free Pinta Image & Photo Editor | Paint.rip`. It keeps
  "Pinta Online Features" at the front, so the localized-page test regex and the brand position
  are unchanged.
- **Meta description:** leads with "Use Pinta online: a free Pinta image and photo editor in your
  browser", then lists layers, selections, text, blurs, curves, and 55 effects in under 160
  characters.
- **H1:** `The Pinta image editor, ready in your browser.` (was "Pinta's desktop soul, ready in your
  browser."). Eyebrow: `Pinta Online · free image & photo editor`. The lede now says it runs
  natively in a tab, with no remote desktop streaming your files, which answers the rollApp/OnWorks
  results directly.
- **Features lede** says "photo editing" and "online editor", and adds an in-body link to
  `/promo/` with descriptive anchor text. The only earlier link was the footer's "Quick designs".
- **"Adjust & transform" card** names Gaussian and motion blur, noise reduction, and sharpening.
- **FAQ, three questions:**
  - *How do I blur an image in Pinta Online?* Effects › Blurs, the six blur effects, blurring
    only a selection, and Pixelate for censoring. Targets "pinta blur".
  - *Can I use it as a photo editor?* Crop, resize, rotate, Auto Level/Curves/Levels, the Photo
    and Noise effects, and JPEG/PNG/WebP output. Targets "pinta photo".
  - *Is this the official Pinta website?* Replaces "How is this related to desktop Pinta?" with
    the same facts, and now links to pinta-project.com. Someone searching "pinta website" usually
    wants the upstream site, so the honest answer, with the link, is the right result.

### `/promo/`

- **Title:** `Free Online Paint & Photo Editor for Quick Designs | Pinta Online`.
- **Meta description:** starts with "Edit images online in a free, open-source paint and photo
  editor".
- **H1:** `The online paint app I open when a design has to be done now.` The only change is the
  word "online".
- **Hero lede** calls it "a free online paint and photo editor".
- **Photos chapter H2:** `It's a real photo editor too` (was "image editor"). The body names the
  blur types, red-eye removal, and noise reduction, and says blurs stay inside a selection.
- **CTA anchor** to `/about/` reads "every Pinta Online feature" instead of "every feature".

Every feature claim was checked against `src/effects/types.ts`, the Image menu in `src/App.tsx`,
and `src/editor/useEffectRunner.ts` (effects are clipped to the active selection).

`tests/e2e/seo.spec.ts` now asserts the new titles, descriptions, and full H1s, and the
`about-desktop-hero` / `about-mobile-hero` visual baselines were regenerated for the new hero copy.

## Deliberately not done

- **No misspelling or "pintrip" targeting.** Putting "pinte" or "pintrip" on the page would read
  as keyword stuffing and would not win a query that belongs to another product.
- **No `FAQPage` markup on `/about/`.** Google restricted FAQ rich results to government and
  health sites in 2023, so it would add maintenance without adding a result. The visible
  questions do the ranking work.
- **Localized About pages untouched.** These queries are English. The French, German, Arabic, and
  Hebrew copy lives in `scripts/generate-seo-locales.mjs` and was not edited.

## Follow-ups worth considering

1. **A Russian SEO locale.** The Russian query cannot rank today: `/ru/` is a UI-only shell with
   `noindex` and a canonical pointing to `/`, and no Russian About page exists. Adding `ru` to
   `SEO_LOCALE_CODES` with a full copy block in `scripts/generate-seo-locales.mjs` would publish
   `/ru/` and `/ru/about/` with hreflang. Russian "paint online" searches are a large market, which
   probably makes this the biggest single opportunity on the list. It also commits the project to
   maintaining a sixth reviewed translation, and the tests change: `localePages`, the language-menu
   count, and the sitemap length (12 → 14).
2. **Blur how-to in the user guide.** `/user-guide/` lists the blur effects in one line. A short
   "blur part of a photo" walkthrough with a screenshot would back up the About FAQ answer and is
   the natural landing page for how-to searches.
3. **Re-check in four weeks.** In Search Console, filter to `/about/` and `/promo/` and watch
   "pinta image editor", "pinta photo", "pinta blur", and "edit images online". If `/about/` and
   `/` start trading positions for "pinta online", tone down the About title rather than the
   editor's.

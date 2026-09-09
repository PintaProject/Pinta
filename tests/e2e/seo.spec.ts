import { expect, test } from '../pageErrors';
import { readFileSync } from 'node:fs';

const packageMetadata = JSON.parse(readFileSync(new URL('../../package.json', import.meta.url), 'utf8')) as {
  version: string;
};
const localePages = [
  {
    locale: 'en',
    direction: 'ltr',
    editor: '/',
    about: '/about/',
    title: /Pinta Online Features/,
    heading: /ready in your browser/i,
  },
  {
    locale: 'fr',
    direction: 'ltr',
    editor: '/fr/',
    about: '/fr/about/',
    title: /Fonctionnalités de Pinta Online/,
    heading: /prête dans votre navigateur/i,
  },
  {
    locale: 'de',
    direction: 'ltr',
    editor: '/de/',
    about: '/de/about/',
    title: /Pinta-Online-Funktionen/,
    heading: /bereit in deinem Browser/i,
  },
  {
    locale: 'ar',
    direction: 'rtl',
    editor: '/ar/',
    about: '/ar/about/',
    title: /ميزات بِنْتا أونلاين/,
    heading: /جاهزة في متصفحك/i,
  },
  {
    locale: 'he',
    direction: 'rtl',
    editor: '/he/',
    about: '/he/about/',
    title: /תכונות Pinta Online/,
    heading: /מוכנה בדפדפן שלכם/i,
  },
] as const;

function absolute(path: string) {
  return `https://paint.rip${path}`;
}

async function alternateMap(page: import('@playwright/test').Page) {
  return page
    .locator('link[rel="alternate"][hreflang]')
    .evaluateAll((links) =>
      Object.fromEntries(links.map((link) => [link.getAttribute('hreflang'), link.getAttribute('href')])),
    );
}

test.describe('search and sharing metadata', () => {
  test('installs one production-only Google tag for Analytics and Ads on every public HTML surface', async ({
    page,
  }) => {
    const source = readFileSync(new URL('../../web-assets/analytics.js', import.meta.url), 'utf8');
    expect(source).toContain("window.gtag('config', measurementId");
    expect(source).toContain("window.gtag('config', googleAdsId");
    expect(source).toContain("window.gtag('event', 'conversion'");
    expect(source.match(/window\.gtag\('config'/g)).toHaveLength(2);
    expect(source.match(/window\.gtag\('event', 'conversion'/g)).toHaveLength(1);

    // The editor puts the open file's name in document.title, and GA4 fills page_title from it
    // on every event unless told otherwise. Sending that would leak file names, which are
    // frequently personal, so the bootstrap must pin page_title and never read the title.
    const code = source.replace(/\/\*[\s\S]*?\*\//g, '').replace(/^\s*\/\/.*$/gm, '');
    expect(code).not.toContain('document.title');
    expect(code).not.toContain('location.search');
    expect(code).not.toContain('location.hash');
    expect(code.match(/page_title/g)).toHaveLength(4);

    const routes = [...localePages.flatMap(({ editor, about }) => [editor, about]), '/promo/', '/user-guide/'];
    for (const route of routes) {
      await page.goto(route);
      await expect(page.locator('meta[name="google-tag-id"]')).toHaveAttribute('content', 'GT-TNLLJZ63');
      await expect(page.locator('meta[name="google-analytics-id"]')).toHaveAttribute('content', 'G-BZKV3EDF46');
      await expect(page.locator('meta[name="google-ads-id"]')).toHaveAttribute('content', 'AW-998871174');
      await expect(page.locator('meta[name="google-ads-page-view-conversion-id"]')).toHaveAttribute(
        'content',
        'AW-998871174/TDzECNTY5-ocEIahptwD',
      );
      const reported = await page.evaluate(
        () =>
          (
            window as typeof window & {
              __pintaAnalytics?: {
                googleTagId?: string;
                measurementId?: string;
                googleAdsId?: string;
                pageViewConversionId?: string;
                pageTitle: string;
                pagePath: string;
                enabled: boolean;
              };
            }
          ).__pintaAnalytics,
      );
      expect(reported).toMatchObject({
        googleTagId: 'GT-TNLLJZ63',
        measurementId: 'G-BZKV3EDF46',
        googleAdsId: 'AW-998871174',
        pageViewConversionId: 'AW-998871174/TDzECNTY5-ocEIahptwD',
        enabled: false,
      });
      // Whatever the route, the reported page is one of a small fixed set — never the title.
      expect(['Editor', 'About', 'Promo', 'User Guide']).toContain(reported!.pageTitle);
      expect(reported!.pagePath).toBe(new URL(route, 'https://paint.rip').pathname);
      await expect(page.locator('script[src^="https://www.googletagmanager.com/"]')).toHaveCount(0);
    }
  });

  test('publishes complete editor metadata and structured software data', async ({ page, request }) => {
    const source = await request.get('/');
    expect(source.ok()).toBe(true);
    expect(await source.text()).toContain('<title>Pinta Online – Free Browser Image Editor | Paint.rip</title>');

    await page.goto('/');
    await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', 'https://paint.rip/');
    await expect(page.locator('link[rel="alternate"][hreflang="x-default"]')).toHaveAttribute(
      'href',
      'https://paint.rip/',
    );
    await expect(page.locator('meta[name="description"]')).toHaveAttribute('content', /edit images online with Pinta/i);
    await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', /max-image-preview:large/);
    await expect(page.locator('meta[property="og:image"]')).toHaveAttribute(
      'content',
      'https://paint.rip/about/assets/pinta-online-og.jpg',
    );
    await expect(page.getByRole('heading', { level: 1 })).toContainText('free browser-based paint and image editor');

    const graph = await page.locator('script[type="application/ld+json"]').evaluateAll((scripts) =>
      scripts.flatMap((script) => {
        const value = JSON.parse(script.textContent ?? '{}') as { '@graph'?: unknown[] };
        return value['@graph'] ?? [];
      }),
    );
    expect(graph).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ '@type': 'WebSite', url: 'https://paint.rip/' }),
        expect.objectContaining({
          '@type': 'SoftwareApplication',
          name: 'Pinta Online',
          applicationCategory: 'DesignApplication',
          isAccessibleForFree: true,
          softwareVersion: packageMetadata.version,
          offers: expect.objectContaining({ price: '0' }),
        }),
      ]),
    );

    await page.getByRole('button', { name: 'Main Menu', exact: true }).click();
    await page
      .locator('.main-menu-popover .menu-item')
      .filter({ hasText: /^About/ })
      .click();
    await expect(page.locator('.about-version')).toHaveText(
      `Pinta Online ${packageMetadata.version} · based on Pinta 3.2`,
    );
    await expect(page.locator('.about-port-credit a')).toHaveText('Evgeny Vinnik');
    await expect(page.locator('.about-port-credit a')).toHaveAttribute(
      'href',
      'https://github.com/evgenyvinnik/pinta-online',
    );
  });

  test('publishes reciprocal editor alternates with English as x-default', async ({ page }) => {
    const expected = Object.fromEntries([
      ...localePages.map(({ locale, editor }) => [locale, absolute(editor)]),
      ['x-default', absolute('/')],
    ]);

    for (const localePage of localePages) {
      const response = await page.goto(localePage.editor);
      expect(response?.status()).toBe(200);
      await expect(page.locator('html')).toHaveAttribute('lang', localePage.locale);
      await expect(page.locator('html')).toHaveAttribute('dir', localePage.direction);
      await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', absolute(localePage.editor));
      expect(await alternateMap(page)).toEqual(expected);

      if (localePage.locale !== 'en') {
        const pageEntity = await page.locator('script[type="application/ld+json"]').evaluate((script) => {
          const value = JSON.parse(script.textContent ?? '{}') as {
            '@graph': Array<{ '@type': string; [key: string]: unknown }>;
          };
          return value['@graph'].find((entry) => entry['@type'] === 'WebPage');
        });
        expect(pageEntity).toMatchObject({ url: absolute(localePage.editor), inLanguage: localePage.locale });
      }
    }
  });

  test('serves UI-only locales without falsely advertising untranslated SEO copy', async ({ page, request }) => {
    const source = await request.get('/cs/');
    expect(source.ok()).toBe(true);
    const html = await source.text();
    expect(html).toContain('<html lang="cs" dir="ltr">');
    expect(html).toContain('<meta name="robots" content="noindex, follow" />');
    expect(html).toContain('<link rel="canonical" href="https://paint.rip/" />');
    expect(html).not.toContain('hreflang=');

    await page.goto('/cs/');
    await expect(page.locator('html')).toHaveAttribute('lang', 'cs');
    await expect(page.locator('[data-menu-name="file"]')).toContainText('Soubor');
    await expect(page.locator('.dock-header').first()).toContainText('Vrstvy');
  });

  test('serves a crawlable visual feature page at its canonical URL', async ({ page, request }) => {
    const response = await page.goto('/about/');
    expect(response?.status()).toBe(200);
    await expect(page).toHaveTitle('Pinta Online Features – Free Web Image Editor | Paint.rip');
    await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', 'https://paint.rip/about/');
    await expect(page.locator('meta[name="description"]')).toHaveAttribute(
      'content',
      /drawing tools, layers, selections, text, 55 built-in and optional effects/i,
    );
    await expect(page.getByRole('heading', { level: 1 })).toContainText('ready in your browser');
    await expect(page.getByRole('link', { name: /start painting now/i })).toHaveAttribute('href', '/');

    const screenshots = page.locator('main img[src^="/about/assets/"]');
    expect(await screenshots.count()).toBeGreaterThanOrEqual(20);
    const screenshotUrls = await screenshots.evaluateAll((images) =>
      images.map((image) => (image as HTMLImageElement).getAttribute('src') ?? ''),
    );
    const screenshotResponses = await Promise.all(screenshotUrls.map((url) => request.get(url)));
    expect(screenshotResponses.every((asset) => asset.ok() && Number(asset.headers()['content-length']) > 1_000)).toBe(
      true,
    );

    const software = await page.locator('script[type="application/ld+json"]').evaluate((script) => {
      const value = JSON.parse(script.textContent ?? '{}') as {
        '@graph': Array<{ '@type': string; [key: string]: unknown }>;
      };
      return value['@graph'].find((entry) => entry['@type'] === 'SoftwareApplication');
    });
    expect(software).toMatchObject({
      name: 'Pinta Online',
      url: 'https://paint.rip/',
      softwareVersion: packageMetadata.version,
      featureList: expect.arrayContaining([
        '23 available drawing and editing tools',
        '55 built-in and optional adjustments and effects',
      ]),
    });
    await expect(page.locator('[data-app-version]')).toHaveText(packageMetadata.version);
  });

  test('serves a crawlable promo landing page with FAQ structured data', async ({ page, request }) => {
    const response = await page.goto('/promo/');
    expect(response?.status()).toBe(200);
    await expect(page).toHaveTitle('Free Online Paint App for Quick Designs | Pinta Online');
    await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', 'https://paint.rip/promo/');
    await expect(page.locator('meta[name="description"]')).toHaveAttribute(
      'content',
      /layers, selections, shapes, gradients, curves, and 55 effects/i,
    );
    await expect(page.getByRole('heading', { level: 1 })).toContainText('when a design has to be done now');
    await expect(page.getByRole('link', { name: /open the editor/i })).toHaveAttribute('href', '/');
    // The promo page is only worth publishing if it feeds the editor and the deeper pages.
    await expect(page.locator('main a[href="/about/"]')).toHaveCount(1);
    await expect(page.locator('main a[href="/user-guide/"]')).toHaveCount(1);

    const screenshots = page.locator('main img[src^="/promo/assets/"]');
    expect(await screenshots.count()).toBeGreaterThanOrEqual(20);
    const screenshotUrls = await screenshots.evaluateAll((images) =>
      images.map((image) => (image as HTMLImageElement).getAttribute('src') ?? ''),
    );
    const screenshotResponses = await Promise.all(screenshotUrls.map((url) => request.get(url)));
    expect(screenshotResponses.every((asset) => asset.ok() && Number(asset.headers()['content-length']) > 1_000)).toBe(
      true,
    );
    // Every screenshot carries intrinsic dimensions, so the gallery cannot shift layout as it loads.
    const unsized = await screenshots.evaluateAll(
      (images) => images.filter((image) => !image.getAttribute('width') || !image.getAttribute('height')).length,
    );
    expect(unsized).toBe(0);

    const graph = await page.locator('script[type="application/ld+json"]').evaluate((script) => {
      const value = JSON.parse(script.textContent ?? '{}') as {
        '@graph': Array<{ '@type': string; [key: string]: unknown }>;
      };
      return value['@graph'];
    });
    expect(graph).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ '@type': 'WebPage', url: 'https://paint.rip/promo/', inLanguage: 'en' }),
        expect.objectContaining({ '@type': 'SoftwareApplication', softwareVersion: packageMetadata.version }),
      ]),
    );
    const faq = graph.find((entry) => entry['@type'] === 'FAQPage') as
      { mainEntity: Array<{ '@type': string; name: string; acceptedAnswer: { text: string } }> } | undefined;
    expect(faq?.mainEntity.length).toBeGreaterThanOrEqual(4);
    // A FAQPage whose answers are empty is a structured-data penalty, not a rich result.
    expect(faq?.mainEntity.every((item) => item['@type'] === 'Question' && item.acceptedAnswer.text.length > 20)).toBe(
      true,
    );
    // Every rendered question must exist in the markup too, or the markup misrepresents the page.
    for (const item of faq?.mainEntity ?? []) {
      await expect(page.locator('.faq-list summary', { hasText: item.name })).toHaveCount(1);
    }
    await expect(page.locator('[data-app-version]')).toHaveText(packageMetadata.version);
  });

  test('serves fully translated About pages with reciprocal alternates', async ({ page }) => {
    const expected = Object.fromEntries([
      ...localePages.map(({ locale, about }) => [locale, absolute(about)]),
      ['x-default', absolute('/about/')],
    ]);

    for (const localePage of localePages) {
      await page.goto(localePage.about);
      await expect(page.locator('html')).toHaveAttribute('lang', localePage.locale);
      await expect(page.locator('html')).toHaveAttribute('dir', localePage.direction);
      await expect(page).toHaveTitle(localePage.title);
      await expect(page.getByRole('heading', { level: 1 })).toContainText(localePage.heading);
      await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', absolute(localePage.about));
      expect(await alternateMap(page)).toEqual(expected);
      await expect(page.locator('.language-menu a')).toHaveCount(5);
      await expect(page.locator('.language-menu a[aria-current="page"]')).toHaveAttribute(
        'hreflang',
        localePage.locale,
      );
      await expect(page.locator('main img[src^="/about/assets/"]')).toHaveCount(20);
      await expect(page.locator('.site-footer').getByRole('link', { name: 'Evgeny Vinnik' })).toHaveAttribute(
        'href',
        'https://github.com/evgenyvinnik/pinta-online',
      );
      await expect(page.locator('.site-footer a[href*="/issues/new"]')).toHaveAttribute(
        'href',
        'https://github.com/evgenyvinnik/pinta-online/issues/new?template=bug.md',
      );

      const pageEntity = await page.locator('script[type="application/ld+json"]').evaluate((script) => {
        const value = JSON.parse(script.textContent ?? '{}') as {
          '@graph': Array<{ '@type': string; [key: string]: unknown }>;
        };
        return value['@graph'].find((entry) => entry['@type'] === 'WebPage');
      });
      expect(pageEntity).toMatchObject({ url: absolute(localePage.about), inLanguage: localePage.locale });
    }
  });

  test('keeps page discovery and editor language links in static footers', async ({ browser, request }) => {
    const routes = [...localePages.map(({ about }) => about), '/promo/', '/user-guide/'];
    const editorLanguages = localePages.map(({ editor }) => editor);
    const noJavaScript = await browser.newContext({ javaScriptEnabled: false });
    const staticPage = await noJavaScript.newPage();

    try {
      for (const route of routes) {
        const response = await request.get(route);
        expect(response.ok()).toBe(true);
        const source = await response.text();
        expect(source).toContain('class="footer-languages"');
        expect(source).toContain('href="/he/"');

        await staticPage.goto(route);
        const footer = staticPage.locator('footer');
        const pageNavigation = footer.locator(':scope > nav[aria-label="Footer navigation"]');
        const expectedAbout = localePages.find(({ about }) => about === route)?.about ?? '/about/';

        await expect(pageNavigation.locator('a[href="/promo/"]')).toHaveCount(1);
        await expect(pageNavigation.locator('a[href="/user-guide/"]')).toHaveCount(1);
        await expect(pageNavigation.locator(`a[href="${expectedAbout}"]`)).toHaveCount(1);

        const languageLinks = footer.locator('.footer-languages nav a');
        await expect(languageLinks).toHaveCount(editorLanguages.length);
        expect(await languageLinks.evaluateAll((links) => links.map((link) => link.getAttribute('href')))).toEqual(
          editorLanguages,
        );
      }
    } finally {
      await noJavaScript.close();
    }
  });

  test('serves a searchable visual Pinta Online user guide and routes F1 to it', async ({ page, request }) => {
    const response = await page.goto('/user-guide/');
    expect(response?.status()).toBe(200);
    await expect(page).toHaveTitle('Pinta Online User Guide – Learn Browser Image Editing | Paint.rip');
    await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', 'https://paint.rip/user-guide/');
    await expect(page.locator('meta[name="description"]')).toHaveAttribute(
      'content',
      /layers, selections, drawing, text, effects, transformations, restoration, export/i,
    );
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Learn Pinta Online');
    await expect(page.locator('[data-chapter]')).toHaveCount(16);
    await expect(page.locator('[data-app-version]')).toHaveText(packageMetadata.version);
    await expect(page.getByRole('link', { name: 'Evgeny Vinnik' }).first()).toHaveAttribute(
      'href',
      'https://github.com/evgenyvinnik/pinta-online',
    );

    const screenshots = page.locator('main img');
    expect(await screenshots.count()).toBeGreaterThanOrEqual(10);
    const screenshotUrls = await screenshots.evaluateAll((images) =>
      images.map((image) => (image as HTMLImageElement).getAttribute('src') ?? ''),
    );
    const screenshotResponses = await Promise.all(screenshotUrls.map((url) => request.get(url)));
    expect(screenshotResponses.every((asset) => asset.ok() && Number(asset.headers()['content-length']) > 1_000)).toBe(
      true,
    );

    const workspaceScreenshots = page.locator('#workspace img');
    await workspaceScreenshots.first().scrollIntoViewIfNeeded();
    await expect
      .poll(() =>
        workspaceScreenshots.evaluateAll((images) =>
          images.map((image) => {
            const screenshot = image as HTMLImageElement;
            if (!screenshot.complete || !screenshot.naturalWidth || !screenshot.clientHeight) return false;
            const intrinsicRatio = screenshot.naturalWidth / screenshot.naturalHeight;
            const renderedRatio = screenshot.clientWidth / screenshot.clientHeight;
            return Math.abs(renderedRatio / intrinsicRatio - 1) < 0.01;
          }),
        ),
      )
      .toEqual([true, true]);

    const guide = await page.locator('script[type="application/ld+json"]').evaluate((script) => {
      const value = JSON.parse(script.textContent ?? '{}') as {
        '@graph': Array<{ '@type': string; [key: string]: unknown }>;
      };
      return value['@graph'].find((entry) => entry['@type'] === 'TechArticle');
    });
    expect(guide).toMatchObject({
      headline: 'Pinta Online User Guide',
      inLanguage: 'en',
      author: { name: 'Evgeny Vinnik', url: 'https://github.com/evgenyvinnik/pinta-online' },
    });

    await page.locator('[data-guide-search]').fill('temporary/private profile');
    await expect(page.locator('[data-search-status]')).toHaveText('1 matching section');
    await expect(page.locator('[data-chapter]:visible')).toHaveCount(1);
    await expect(page.locator('#history')).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(page.locator('[data-search-status]')).toHaveText('16 guide sections');

    await page.goto('/');
    await expect(page.locator('.app-shell')).toHaveAttribute('data-workspace-ready', 'true');
    const popupPromise = page.waitForEvent('popup');
    await page.keyboard.press('F1');
    const popup = await popupPromise;
    await popup.waitForLoadState('domcontentloaded');
    expect(new URL(popup.url()).pathname).toBe('/user-guide/');
    await popup.close();
  });

  test('advertises every localized canonical page to crawlers', async ({ page, request }) => {
    const [robots, sitemap] = await Promise.all([request.get('/robots.txt'), request.get('/sitemap.xml')]);
    expect(robots.ok()).toBe(true);
    expect(await robots.text()).toContain('Sitemap: https://paint.rip/sitemap.xml');
    expect(sitemap.ok()).toBe(true);
    expect(sitemap.headers()['content-type']).toContain('xml');
    const sitemapText = await sitemap.text();
    const sitemapEntries = await page.evaluate((xml) => {
      const sitemapNamespace = 'http://www.sitemaps.org/schemas/sitemap/0.9';
      const xhtmlNamespace = 'http://www.w3.org/1999/xhtml';
      const document = new DOMParser().parseFromString(xml, 'application/xml');
      const parseError = document.querySelector('parsererror')?.textContent;
      if (parseError) throw new Error(parseError);
      return [...document.getElementsByTagNameNS(sitemapNamespace, 'url')].map((url) => ({
        location: url.getElementsByTagNameNS(sitemapNamespace, 'loc')[0]?.textContent,
        alternates: Object.fromEntries(
          [...url.getElementsByTagNameNS(xhtmlNamespace, 'link')].map((link) => [
            link.getAttribute('hreflang'),
            link.getAttribute('href'),
          ]),
        ),
      }));
    }, sitemapText);

    const expectedEditorAlternates = Object.fromEntries([
      ...localePages.map(({ locale, editor }) => [locale, absolute(editor)]),
      ['x-default', absolute('/')],
    ]);
    const expectedAboutAlternates = Object.fromEntries([
      ...localePages.map(({ locale, about }) => [locale, absolute(about)]),
      ['x-default', absolute('/about/')],
    ]);
    for (const localePage of localePages) {
      expect(sitemapEntries).toContainEqual({
        location: absolute(localePage.editor),
        alternates: expectedEditorAlternates,
      });
      expect(sitemapEntries).toContainEqual({
        location: absolute(localePage.about),
        alternates: expectedAboutAlternates,
      });
    }
    expect(sitemapEntries).toContainEqual({
      location: 'https://paint.rip/promo/',
      alternates: {},
    });
    expect(sitemapEntries).toContainEqual({
      location: 'https://paint.rip/user-guide/',
      alternates: {},
    });
    expect(sitemapEntries).toHaveLength(12);
  });
});

import { createHash } from 'node:crypto';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';
import { webOverrides } from './i18n-web-overrides.mjs';

const root = path.resolve(import.meta.dirname, '..');
const output = path.resolve(root, process.env.PINTA_REVIEW_OUTPUT ?? 'playwright-report/translations');
const previouslyReviewed = new Set(['en', 'fr', 'de', 'ar', 'he']);
const escapeHtml = (value) =>
  value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');

function contextFor(key, catalog) {
  if (key === 'Add-in Manager')
    return `Dialog title. Compare with the actual menu command from the combined catalog: “${catalog['Add-in Manager…'] ?? catalog[key]}”. Native gettext and web overrides can disagree on terminology; flag differences for the fluent reviewer.`;
  if (/^(Minimize|Restore|Resize) (Layers|History)/.test(key))
    return 'Dock controls. Restore means expand a minimized panel here, not recover a lost image.';
  if (/of about|^is in use/.test(key))
    return 'Storage banner fragment. Review the assembled example above, including which number means used space.';
  if (/recover|restor|reload|saving|storage|discard|overwrit|memory|stopped|failed|usable/i.test(key))
    return 'Recovery/storage/error UI. Preserve the warning and the exact action; do not promise recovery that the English does not promise.';
  if (/Primary|Palette/.test(key))
    return 'Palette action. Primary is the foreground drawing color; a palette is a saved set of color swatches.';
  if (/scale|scaling|margin|orientation|portrait|landscape|center|page|ppi/i.test(key))
    return 'Print/page setup. PPI is pixels per inch; fit-to-page and actual size are different operations.';
  return 'Editor UI / optional add-ins. Search this English key in src/ to inspect the owning control; preserve product and package names.';
}

await mkdir(output, { recursive: true });
// The generated manifest is already checked against every upstream gettext catalog by
// `npm run verify:i18n`. Re-parsing all 73 .po files here made this human-review helper take
// tens of seconds on a loaded machine and caused its unit test to time out.
const localeInventory = JSON.parse(await readFile(path.join(root, 'src/i18n/locales.generated.json'), 'utf8'));
const locales = localeInventory.locales.filter(({ code }) => !previouslyReviewed.has(code));
const manifest = [];
for (const locale of locales) {
  const strings = webOverrides[locale.code];
  if (!strings) throw new Error(`Missing web overrides for ${locale.code}`);
  const catalog = JSON.parse(await readFile(path.join(root, 'src/i18n/locales', `${locale.code}.json`), 'utf8'));
  const entries = Object.entries(strings).map(([source, translation]) => ({
    source,
    translation,
    context: contextFor(source, catalog),
  }));
  const digest = createHash('sha256').update(JSON.stringify(entries)).digest('hex');
  const review = { locale: locale.code, name: locale.name, digest, status: 'awaiting-fluent-review', entries };
  manifest.push({ locale: locale.code, name: locale.name, digest, strings: entries.length, status: review.status });
  const assembled = `120 MB ${strings['of about']} 1 GB ${strings['is in use. Close images you have already exported to free more space.']}`;
  const html = `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>${escapeHtml(locale.name)} translation review</title>
<style>
body { font: 16px/1.5 system-ui; max-width: 1200px; margin: 28px auto; padding: 0 20px; background: #f6f6f3; color: #1b2930; }
a { color: #145a91; } header { border-bottom: 1px solid #aaa; padding-bottom: 20px; } h1 { margin-bottom: 8px; }
.example { padding: 18px; background: #fff2cc; } article { background: white; margin: 18px 0; padding: 20px; border: 1px solid #ccc; border-radius: 8px; }
.pair { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; overflow-wrap: anywhere; } .source { color: #344c59; } .target { font-size: 18px; }
.context { color: #56616a; font-size: 14px; } textarea { width: 100%; min-height: 65px; box-sizing: border-box; font: inherit; }
select, input, button { font: inherit; padding: 6px; } #progress { position: sticky; top: 0; background: #1b2930; color: white; padding: 12px; }
@media (max-width: 700px) { .pair { grid-template-columns: 1fr; } }
</style></head><body>
<header><a href="index.html">← All locales</a><h1>${escapeHtml(locale.name)} · ${locale.code}</h1>
<p>${entries.length} browser-specific messages. This sheet is preparation for a fluent human review, not a claim of linguistic approval. Upstream gettext translations and SEO copy are outside this sheet.</p>
<p>Check meaning, idiom, consistency with Pinta's existing terminology and fit in the actual UI. English spellings can be correct in en-GB/en-CA; shared technical names need not be translated.</p>
<p><a href="https://paint.rip/${locale.code}/" target="_blank" rel="noopener noreferrer">Open the localized editor</a> · SHA-256: <code>${digest}</code></p>
<p><label>Reviewer name <input id="reviewer" autocomplete="name"></label> <label><input id="fluent" type="checkbox"> I am fluent in this locale and checked the UI context</label></p>
<button id="export" type="button">Download review JSON</button> <span id="save-status"></span>
<p>Draft choices are saved only in this browser when storage is available. Export to keep/share them. Exporting does not change the app's translations or grant approval.</p></header>
<h2>Read the sentence as rendered</h2><p>Here 120 MB is used and 1 GB is the quota. Confirm the translation does not reverse them.</p>
<p class="example" lang="${locale.code}" dir="${locale.direction}">${escapeHtml(assembled)}</p>
<p id="progress" role="status"></p>
${entries
  .map(
    ({ source, translation, context }, index) => `<article data-index="${index}">
<div class="pair"><p class="source" lang="en">${escapeHtml(source)}</p><p class="target" lang="${locale.code}" dir="${locale.direction}">${escapeHtml(translation)}</p></div>
<p class="context">${escapeHtml(context)}</p>
<label>Review <select aria-label="Review message ${index + 1}"><option value="pending">Not reviewed</option><option value="accept">Accept</option><option value="change">Needs correction</option><option value="context">Need more context</option></select></label>
<p><label>Suggested wording / rationale<textarea aria-label="Notes for message ${index + 1}"></textarea></label></p></article>`,
  )
  .join('')}
<script type="application/json" id="review-data">${JSON.stringify(review).replaceAll('<', '\\u003c')}</script>
<script>
const data = JSON.parse(document.querySelector('#review-data').textContent);
const storageKey = 'pinta-translation-review:' + data.locale + ':' + data.digest;
const cards = [...document.querySelectorAll('article')];
const reviewer = document.querySelector('#reviewer');
const fluent = document.querySelector('#fluent');
function snapshot() {
  const entries = cards.map((card, index) => ({ ...data.entries[index], decision: card.querySelector('select').value, notes: card.querySelector('textarea').value }));
  const complete = entries.every((entry) => entry.decision === 'accept');
  return { ...data, reviewer: reviewer.value.trim(), fluent: fluent.checked, reviewedAt: new Date().toISOString(), status: complete && fluent.checked && reviewer.value.trim() ? 'submitted-for-maintainer-review' : 'incomplete', entries };
}
function update() {
  const result = snapshot();
  document.querySelector('#progress').textContent = result.entries.filter((entry) => entry.decision !== 'pending').length + ' / ' + cards.length + ' examined · ' + result.status;
  try { localStorage.setItem(storageKey, JSON.stringify(result)); } catch { document.querySelector('#save-status').textContent = 'Local storage unavailable. Export before closing.'; }
}
try {
  const saved = JSON.parse(localStorage.getItem(storageKey) || 'null');
  if (saved && saved.digest === data.digest) {
    reviewer.value = saved.reviewer || ''; fluent.checked = saved.fluent === true;
    cards.forEach((card, index) => { card.querySelector('select').value = saved.entries[index]?.decision || 'pending'; card.querySelector('textarea').value = saved.entries[index]?.notes || ''; });
  }
} catch { /* An invalid draft never blocks a fresh review. */ }
document.addEventListener('input', update); document.addEventListener('change', update);
document.querySelector('#export').addEventListener('click', () => {
  const url = URL.createObjectURL(new Blob([JSON.stringify(snapshot(), null, 2)], { type: 'application/json' }));
  const link = document.createElement('a'); link.href = url; link.download = 'pinta-' + data.locale + '-review.json'; link.click(); setTimeout(() => URL.revokeObjectURL(url), 1000);
});
update();
</script></body></html>`;
  await writeFile(path.join(output, `${locale.code}.html`), html);
}
await writeFile(path.join(output, 'manifest.json'), `${JSON.stringify(manifest, null, 2)}\n`);
await writeFile(
  path.join(output, 'index.html'),
  `<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Pinta translation review</title><body style="font:18px/1.6 system-ui;max-width:900px;margin:40px auto;padding:20px"><h1>Fluent-review queue</h1><p>${manifest.length} locales · ${manifest.reduce((sum, row) => sum + row.strings, 0)} messages. All await fluent review; generated sheets do not certify translations.</p><ul>${manifest.map((row) => `<li><a href="${row.locale}.html">${escapeHtml(row.name)} (${row.locale})</a> — ${row.strings} messages</li>`).join('')}</ul></body></html>`,
);
console.log(`Translation review: ${path.join(output, 'index.html')} (${manifest.length} locales)`);

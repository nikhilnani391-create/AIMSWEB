/**
 * Shared UI component renderers.
 * Each function returns an HTML string for a repeating UI pattern.
 */

/* ── SVG Icon helpers ─────────────────────────────────────────── */

function svgIcon(path, cls = 'w-5 h-5') {
  return `<svg class="${cls}" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="${path}"></path></svg>`;
}

const ICON_CHECKMARK = 'M5 13l4 4L19 7';
const ICON_ARROW_RIGHT = 'M9 5l7 7-7 7';
const ICON_LONG_ARROW = 'M14 5l7 7m0 0l-7 7m7-7H3';

/* ── Checklist item (checkmark + label) ───────────────────────── */

function renderChecklistItem(text) {
  return `<li class="flex items-center">${svgIcon(ICON_CHECKMARK, 'w-5 h-5 text-blue-500 mr-2')} ${text}</li>`;
}

/* ── Solution hub card (Railway / Industrial) ─────────────────── */

function renderSolutionCard(data) {
  const items = data.features.map(renderChecklistItem).join('\n');
  return `
    <div class="bg-gray-50 border border-gray-200 rounded-lg overflow-hidden hover:shadow-lg transition">
      <div class="h-48 bg-gray-300">
        <img src="${data.image}" alt="${data.imageAlt}" class="w-full h-full object-cover" />
      </div>
      <div class="p-8">
        <h3 class="text-2xl font-bold text-gray-900 mb-4">${data.title}</h3>
        <p class="text-gray-600 mb-6">${data.description}</p>
        <ul class="mb-8 space-y-2 text-gray-700">${items}</ul>
        <a href="${data.linkHref}" class="text-blue-600 font-semibold hover:text-blue-800 transition flex items-center">
          ${data.linkText} ${svgIcon(ICON_ARROW_RIGHT, 'w-4 h-4 ml-1')}
        </a>
      </div>
    </div>`;
}

/* ── Product card ─────────────────────────────────────────────── */

function renderProductCard(data) {
  return `
    <div class="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-xl transform hover:-translate-y-1 transition duration-300">
      <div class="h-40 bg-gray-100 flex items-center justify-center mb-6 rounded overflow-hidden">
        <img src="${data.image}" alt="${data.imageAlt}" class="w-full h-full object-cover grayscale hover:grayscale-0 transition duration-500"/>
      </div>
      <div class="flex justify-between items-start mb-2">
        <h3 class="text-xl font-bold text-gray-900">${data.title}</h3>
        <span class="bg-${data.badgeColor}-100 text-${data.badgeColor}-800 text-xs px-2 py-1 rounded font-semibold">${data.badgeText}</span>
      </div>
      <p class="text-sm text-gray-600 mb-4">${data.description}</p>
      ${renderProductButtons()}
    </div>`;
}

function renderProductButtons() {
  return `
    <div class="flex gap-2">
      <a href="#" class="text-sm border border-gray-300 hover:bg-gray-100 text-gray-700 hover:text-blue-600 py-2 px-4 rounded transition duration-200 flex-1 text-center font-medium">Datasheet</a>
      <a href="#consultation" class="text-sm bg-gray-900 hover:bg-blue-600 text-white py-2 px-4 rounded transition duration-200 flex-1 text-center font-medium">Details</a>
    </div>`;
}

/* ── Comparison table row ─────────────────────────────────────── */

function renderComparisonRow(data) {
  const cells = data.values.map(v => `<td class="p-4 border-b border-gray-200">${v}</td>`).join('');
  return `
    <tr class="hover:bg-blue-50 transition duration-150">
      <td class="p-4 border-b border-gray-200 font-medium text-gray-900">${data.feature}</td>
      ${cells}
    </tr>`;
}

/* ── Machinery card ───────────────────────────────────────────── */

function renderMachineryCard(data) {
  return `
    <div class="bg-white rounded-lg overflow-hidden shadow-md hover:shadow-xl transform hover:-translate-y-1 transition duration-300">
      <div class="h-64 w-full">
        <img src="${data.image}" alt="${data.imageAlt}" class="w-full h-full object-cover" />
      </div>
      <div class="p-6">
        <h3 class="text-xl font-bold text-gray-900 mb-2">${data.title}</h3>
        <p class="text-gray-600 text-sm">${data.description}</p>
      </div>
    </div>`;
}

/* ── Innovation feature card ──────────────────────────────────── */

function renderInnovationCard(data) {
  return `
    <div class="text-center p-6 rounded-lg hover:bg-gray-50 transition duration-300">
      <div class="w-16 h-16 mx-auto bg-${data.color}-100 text-${data.color}-600 rounded-full flex items-center justify-center mb-6">
        ${svgIcon(data.iconPath, 'w-8 h-8')}
      </div>
      <h3 class="text-xl font-bold text-gray-900 mb-3">${data.title}</h3>
      <p class="text-gray-600">${data.description}</p>
    </div>`;
}

/* ── Resource card ────────────────────────────────────────────── */

function renderResourceCard(data) {
  return `
    <div class="border border-gray-200 p-8 rounded-lg hover:shadow-lg transition duration-300 transform hover:-translate-y-1">
      <div class="w-12 h-12 bg-${data.color}-100 text-${data.color}-600 rounded mb-6 flex items-center justify-center">
        ${svgIcon(data.iconPath, 'w-6 h-6')}
      </div>
      <h3 class="text-xl font-bold text-gray-900 mb-3">${data.title}</h3>
      <p class="text-gray-600 mb-6">${data.description}</p>
      <a href="${data.linkHref}" class="text-${data.color}-600 font-semibold hover:text-${data.color}-800 transition">${data.linkText} &rarr;</a>
    </div>`;
}

/* ── Mount helpers ────────────────────────────────────────────── */

function renderInto(id, items, renderer) {
  const el = document.getElementById(id);
  if (el) el.innerHTML = items.map(renderer).join('');
}

function renderTableBody(id, rows) {
  const el = document.getElementById(id);
  if (el) el.innerHTML = rows.map(renderComparisonRow).join('');
}

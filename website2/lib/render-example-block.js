const fs = require("node:fs");
const path = require("node:path");

function readIfExists(filePath) {
  return fs.existsSync(filePath) ? fs.readFileSync(filePath, "utf8") : "";
}

function createExampleCatalog(examplesRoot) {
  if (!fs.existsSync(examplesRoot)) {
    return {};
  }

  const catalog = {};

  for (const entry of fs.readdirSync(examplesRoot, { withFileTypes: true })) {
    if (!entry.isDirectory()) {
      continue;
    }

    const exampleDir = path.join(examplesRoot, entry.name);
    const metaPath = path.join(exampleDir, "meta.json");
    if (!fs.existsSync(metaPath)) {
      continue;
    }

    const meta = JSON.parse(fs.readFileSync(metaPath, "utf8"));
    catalog[entry.name] = {
      id: entry.name,
      title: meta.title || entry.name,
      renderedHtml: readIfExists(path.join(exampleDir, "rendered.html")),
      sourceHtml: readIfExists(path.join(exampleDir, "source.html"))
    };
  }

  return catalog;
}

function renderExampleBlock(example) {
  return [
    '<div class="code-block interactive-example">',
    '  <div class="code-header">',
    `    <span class="code-title">${example.title}</span>`,
    '    <div class="example-controls">',
    '      <div class="view-toggle">',
    '        <button class="toggle-btn active" data-view="rendered">Rendered</button>',
    '        <button class="toggle-btn" data-view="source">Source</button>',
    '      </div>',
    '      <button class="fullscreen-btn" aria-label="Toggle Fullscreen">⛶</button>',
    '    </div>',
    '  </div>',
    '  <div class="example-content">',
    `    <div class="view-pane rendered-view active">${example.renderedHtml}</div>`,
    `    <div class="view-pane source-view">${example.sourceHtml}</div>`,
    '  </div>',
    '</div>'
  ].join("\n");
}

module.exports = {
  createExampleCatalog,
  renderExampleBlock
};
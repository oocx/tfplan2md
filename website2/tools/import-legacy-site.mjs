import fs from "node:fs/promises";
import path from "node:path";
import { load } from "cheerio";

const websiteRoutes = [
  "index.html",
  "getting-started.html",
  "docs.html",
  "examples.html",
  "architecture.html",
  "ai-workflow.html",
  "contributing.html",
  "features/index.html",
  "features/azdo-variable-groups.html",
  "features/azure-optimizations.html",
  "features/custom-templates.html",
  "features/firewall-rules.html",
  "features/inline-diffs.html",
  "features/large-values.html",
  "features/misc.html",
  "features/module-grouping.html",
  "features/nsg-rules.html",
  "features/semantic-icons.html",
  "features/sensitive-masking.html",
  "features/static-analysis.html",
  "features/value-formatting.html",
  "providers/index.html",
  "providers/azuread.html",
  "providers/azuredevops.html",
  "providers/azurerm.html",
  "providers/msgraph.html"
];

const complexity = new Map([
  ["architecture.html", "Low"],
  ["providers/index.html", "Low"],
  ["providers/azuread.html", "Low"],
  ["providers/azuredevops.html", "Low"],
  ["providers/azurerm.html", "Low"],
  ["providers/msgraph.html", "Low"],
  ["ai-workflow.html", "Medium"],
  ["contributing.html", "Medium"],
  ["getting-started.html", "Medium"],
  ["docs.html", "Medium"],
  ["features/index.html", "Medium"],
  ["features/misc.html", "Medium"],
  ["features/semantic-icons.html", "Medium"],
  ["features/value-formatting.html", "Medium"],
  ["features/custom-templates.html", "Medium"],
  ["features/inline-diffs.html", "Medium"],
  ["index.html", "Medium"],
  ["examples.html", "High"],
  ["features/firewall-rules.html", "High"],
  ["features/nsg-rules.html", "High"],
  ["features/module-grouping.html", "High"],
  ["features/sensitive-masking.html", "High"],
  ["features/azure-optimizations.html", "High"],
  ["features/large-values.html", "High"],
  ["features/static-analysis.html", "High"],
  ["features/azdo-variable-groups.html", "High"]
]);

const repoRoot = path.resolve(path.dirname(new URL(import.meta.url).pathname), "..", "..");
const legacyRoot = path.join(repoRoot, "website");
const website2Root = path.join(repoRoot, "website2");
const srcRoot = path.join(website2Root, "src");
const generatedRoot = path.join(srcRoot, "_generated");
const generatedContentRoot = path.join(generatedRoot, "content");
const generatedExamplesRoot = path.join(generatedRoot, "examples");
const pagesRoot = path.join(srcRoot, "pages");
const mediaRoot = path.join(srcRoot, "media-root");
const assetsRoot = path.join(srcRoot, "assets");

function normalizePageId(route) {
  return route.replace(/\.html$/, "");
}

function navKeyForRoute(route) {
  if (route.startsWith("features/")) {
    return "features";
  }

  if (route.startsWith("providers/")) {
    return "providers";
  }

  switch (route) {
    case "getting-started.html":
      return "install";
    case "docs.html":
      return "docs";
    case "examples.html":
      return "examples";
    case "architecture.html":
      return "architecture";
    case "ai-workflow.html":
      return "ai-workflow";
    case "contributing.html":
      return "contributing";
    default:
      return "";
  }
}

function rootPrefixForRoute(route) {
  return route.includes("/") ? "../" : "";
}

function outputTemplatePath(route) {
  return path.join(pagesRoot, route.replace(/\.html$/, ".njk"));
}

async function ensureDir(dirPath) {
  await fs.mkdir(dirPath, { recursive: true });
}

async function resetGeneratedContent() {
  await fs.rm(generatedRoot, { recursive: true, force: true });
  await fs.rm(pagesRoot, { recursive: true, force: true });
  await fs.rm(path.join(srcRoot, "style.css"), { force: true });
  await fs.rm(mediaRoot, { recursive: true, force: true });
  await fs.rm(assetsRoot, { recursive: true, force: true });
}

async function copyDirectory(sourceDir, destinationDir) {
  await ensureDir(destinationDir);
  const entries = await fs.readdir(sourceDir, { withFileTypes: true });

  for (const entry of entries) {
    const sourcePath = path.join(sourceDir, entry.name);
    const destinationPath = path.join(destinationDir, entry.name);
    if (entry.isDirectory()) {
      await copyDirectory(sourcePath, destinationPath);
    } else {
      if (sourcePath.endsWith(".html")) {
        continue;
      }

      await fs.copyFile(sourcePath, destinationPath);
    }
  }
}

async function copyLegacyAssets() {
  await fs.copyFile(path.join(legacyRoot, "style.css"), path.join(srcRoot, "style.css"));
  await copyDirectory(path.join(legacyRoot, "assets"), assetsRoot);
  await ensureDir(mediaRoot);

  const rootEntries = await fs.readdir(legacyRoot, { withFileTypes: true });
  for (const entry of rootEntries) {
    if (!entry.isFile()) {
      continue;
    }

    if (!/\.(svg|png|jpg|jpeg|gif)$/i.test(entry.name)) {
      continue;
    }

    await fs.copyFile(path.join(legacyRoot, entry.name), path.join(mediaRoot, entry.name));
  }
}

function collectContentNodes($) {
  const nodes = [];
  $("body").children().each((_, element) => {
    const tagName = element.tagName?.toLowerCase();
    const id = $(element).attr("id");
    if (tagName === "nav" || tagName === "footer" || tagName === "script" || id === "lightbox") {
      return;
    }

    nodes.push($.html(element));
  });
  return nodes.join("\n\n").trim();
}

async function writeExampleFiles(exampleId, title, renderedHtml, sourceHtml) {
  const exampleDir = path.join(generatedExamplesRoot, exampleId);
  await ensureDir(exampleDir);
  await fs.writeFile(path.join(exampleDir, "meta.json"), JSON.stringify({ title }, null, 2));
  await fs.writeFile(path.join(exampleDir, "rendered.html"), renderedHtml);
  await fs.writeFile(path.join(exampleDir, "source.html"), sourceHtml);
}

async function importRoute(route) {
  const inputPath = path.join(legacyRoot, route);
  const html = await fs.readFile(inputPath, "utf8");
  const $ = load(html, { decodeEntities: false });
  const pageId = normalizePageId(route);
  const title = $("title").first().text().trim();
  const description = $("meta[name='description']").attr("content") || "";

  $("script").remove();
  $("#lightbox").remove();
  $("[onclick]").removeAttr("onclick");

  let exampleIndex = 1;
  const interactiveExamples = $(".interactive-example");
  for (const element of interactiveExamples.toArray()) {
    const example = $(element);
    const exampleId = `${pageId.replaceAll("/", "--")}--example-${exampleIndex}`;
    const exampleTitle = example.find(".code-title").first().text().trim() || `Example ${exampleIndex}`;
    const renderedHtml = example.find(".rendered-view").first().html()?.trim() || "";
    const sourceHtml = example.find(".source-view").first().html()?.trim() || "";

    await writeExampleFiles(exampleId, exampleTitle, renderedHtml, sourceHtml);
    example.replaceWith(`<!-- EXAMPLE:${exampleId} -->`);
    exampleIndex += 1;
  }

  const contentHtml = collectContentNodes($);
  const contentPath = path.join(generatedContentRoot, `${pageId}.html`);
  await ensureDir(path.dirname(contentPath));
  await fs.writeFile(contentPath, contentHtml);

  const templatePath = outputTemplatePath(route);
  await ensureDir(path.dirname(templatePath));
  await fs.writeFile(templatePath, `---\nlayout: layouts/base.njk\ntitle: ${JSON.stringify(title)}\ndescription: ${JSON.stringify(description)}\npermalink: /${route}\nrootPrefix: ${JSON.stringify(rootPrefixForRoute(route))}\nnavKey: ${JSON.stringify(navKeyForRoute(route))}\npageId: ${JSON.stringify(pageId)}\n---\n{% legacyContent pageId %}\n`);
}

async function writeRouteInventory() {
  const lines = [
    "# Website2 Route Inventory",
    "",
    "| Route | Complexity | Status |",
    "| --- | --- | --- |"
  ];

  for (const route of websiteRoutes) {
    lines.push(`| /${route} | ${complexity.get(route) || "Unclassified"} | Migrated to website2/src/pages |`);
  }

  await fs.writeFile(path.join(website2Root, "route-inventory.md"), `${lines.join("\n")}\n`);
}

async function writeSharedComponentInventory() {
  const content = `# Website2 Shared Component Inventory

## Shared shell

- Base layout
- Navbar
- Footer
- Theme toggle
- Mobile navigation

## Content components

- Feature card
- Provider card
- Example block

## Shared behaviors

- Theme-aware image swapping
- Copy buttons
- Interactive example tabs
- Interactive example fullscreen
- Homepage carousel
- Screenshot lightbox
- Generic code tab groups
`;

  await fs.writeFile(path.join(website2Root, "shared-component-inventory.md"), content);
}

async function writeParityChecklists() {
  const lines = ["# Website2 Parity Checklists", ""];
  websiteRoutes.forEach((route, index) => {
    lines.push(
      `## /${route}`,
      "",
      "- [ ] Route exists at the correct output path",
      "- [ ] Title and key metadata match intent",
      "- [ ] Navigation links and active state are correct",
      "- [ ] Footer links are correct",
      "- [ ] Theme toggle works",
      "- [ ] Mobile navigation works",
      "- [ ] Section order matches the legacy page",
      "- [ ] Screenshots and assets load correctly",
      "- [ ] Internal and external links are correct",
      "- [ ] Interactive examples behave correctly, if present",
      "- [ ] Content is complete and not simplified accidentally",
      "- [ ] Desktop and mobile layouts are acceptable"
    );

    if (index < websiteRoutes.length - 1) {
      lines.push("");
    }
  });

  await fs.writeFile(path.join(website2Root, "parity-checklists.md"), `${lines.join("\n")}\n`);
}

await resetGeneratedContent();
await ensureDir(generatedContentRoot);
await ensureDir(generatedExamplesRoot);
await copyLegacyAssets();

for (const route of websiteRoutes) {
  await importRoute(route);
}

await writeRouteInventory();
await writeSharedComponentInventory();
await writeParityChecklists();
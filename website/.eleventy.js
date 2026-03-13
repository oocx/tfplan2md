const path = require("node:path");
const cheerio = require("cheerio");
const hljs = require("highlight.js");
const MarkdownIt = require("markdown-it");
const { createExampleCatalog, renderExampleBlock } = require("./lib/render-example-block");

function normalizeMarkdown(value) {
  const text = String(value ?? "").replaceAll("\r\n", "\n");
  const lines = text.split("\n");

  while (lines.length > 0 && lines[0].trim() === "") {
    lines.shift();
  }

  while (lines.length > 0 && lines.at(-1)?.trim() === "") {
    lines.pop();
  }

  const nonEmptyLines = lines.filter((line) => line.trim() !== "");
  if (nonEmptyLines.length === 0) {
    return "";
  }

  const commonIndent = nonEmptyLines.reduce((smallestIndent, line) => {
    const match = /^\s*/.exec(line);
    const indent = match?.[0].length ?? 0;
    return Math.min(smallestIndent, indent);
  }, Number.POSITIVE_INFINITY);

  return lines.map((line) => line.slice(commonIndent)).join("\n");
}

function inferCodeLanguage($, block) {
  const className = $(block).attr("class") || "";
  const explicitLanguage = /(?:^|\s)language-([\w-]+)/.exec(className)?.[1];
  const text = $(block).text().trim();
  const title = $(block).closest(".code-block").find(".code-title").first().text().trim().toLowerCase();

  if (explicitLanguage && explicitLanguage !== "plaintext") {
    return explicitLanguage;
  }

  if (title.endsWith(".json") || text.startsWith("{") || text.startsWith("[")) {
    return "json";
  }

  if (title.endsWith(".yml") || title.endsWith(".yaml")) {
    return "yaml";
  }

  if (title.endsWith(".ps1") || text.includes("ConvertFrom-Json") || text.includes("ConvertTo-Json") || text.includes("Write-Host")) {
    return "powershell";
  }

  if (text.includes('resource "') || text.includes(" will be created") || text.includes(" will be updated") || text.includes(" will be destroyed") || text.includes(" must be replaced")) {
    return "bash";
  }

  if (text.includes("<") && text.includes(">") && (text.includes("</") || text.includes("/>"))) {
    return "xml";
  }

  if (text.startsWith("# ") || text.startsWith("## ") || text.includes("| ---")) {
    return "markdown";
  }

  return "bash";
}

function shouldSkipHighlight($, block) {
  const className = $(block).attr("class") || "";

  return className.split(/\s+/).includes("nohighlight")
    || $(block).closest(".source-view, .code-tab-content, .cicd-tab-content, .rendered-view").length > 0;
}

function highlightStaticCodeBlocks(content, outputPath) {
  if (!outputPath?.endsWith(".html")) {
    return content;
  }

  const $ = cheerio.load(content, { decodeEntities: false });

  $("pre code").each((_, block) => {
    if (shouldSkipHighlight($, block)) {
      return;
    }

    const rawCode = $(block).text();
    const language = inferCodeLanguage($, block);
    const result = language && hljs.getLanguage(language)
      ? hljs.highlight(rawCode, { language, ignoreIllegals: true })
      : hljs.highlightAuto(rawCode);

    $(block)
      .html(result.value)
      .attr("class", `hljs language-${result.language || language || "plaintext"}`);
  });

  return $.html();
}

module.exports = function configureEleventy(eleventyConfig) {
  const projectRoot = __dirname;
  const examplesRoot = path.join(projectRoot, "src", "examples");
  const markdownRenderer = new MarkdownIt({
    html: true,
    linkify: true,
    breaks: false
  });

  eleventyConfig.addPassthroughCopy({ "src/assets": "assets" });
  eleventyConfig.addPassthroughCopy({ "src/site-assets/js": "assets/js" });
  eleventyConfig.addPassthroughCopy({ "src/styles": "styles" });
  eleventyConfig.addPassthroughCopy({ "src/style.css": "style.css" });
  eleventyConfig.addPassthroughCopy({ "src/media-root": "." });

  eleventyConfig.addFilter("markdown", function(value) {
    return markdownRenderer.render(normalizeMarkdown(value));
  });

  eleventyConfig.addFilter("markdownInline", function(value) {
    return markdownRenderer.renderInline(normalizeMarkdown(value));
  });

  function renderGeneratedExample(exampleId) {
    const examples = createExampleCatalog(examplesRoot);
    const example = examples[exampleId];
    if (!example) {
      throw new Error(`Missing example asset: ${exampleId}`);
    }

    return renderExampleBlock(example);
  }

  eleventyConfig.addShortcode("exampleBlock", function(exampleId) {
    return renderGeneratedExample(exampleId);
  });

  eleventyConfig.addTransform("highlightStaticCodeBlocks", function(content, outputPath) {
    return highlightStaticCodeBlocks(content, outputPath);
  });

  return {
    dir: {
      input: "src",
      includes: "_includes",
      data: "_data",
      output: "dist"
    },
    htmlTemplateEngine: "njk",
    markdownTemplateEngine: "njk",
    templateFormats: ["njk", "md"]
  };
};
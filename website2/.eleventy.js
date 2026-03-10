const fs = require("node:fs");
const path = require("node:path");
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

module.exports = function configureEleventy(eleventyConfig) {
  const projectRoot = __dirname;
  const generatedRoot = path.join(projectRoot, "src", "_generated");
  const contentRoot = path.join(generatedRoot, "content");
  const examplesRoot = path.join(generatedRoot, "examples");
  const markdownRenderer = new MarkdownIt({
    html: true,
    linkify: true,
    breaks: false
  });

  eleventyConfig.addPassthroughCopy({ "src/assets": "assets" });
  eleventyConfig.addPassthroughCopy({ "src/site-assets/js": "assets/js" });
  eleventyConfig.addPassthroughCopy({ "src/style.css": "style.css" });
  eleventyConfig.addPassthroughCopy({ "src/media-root": "." });

  eleventyConfig.addWatchTarget("src/_generated/");

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
      throw new Error(`Missing generated example: ${exampleId}`);
    }

    return renderExampleBlock(example);
  }

  function injectGeneratedExamples(html) {
    return html.replaceAll(/<!--\s*EXAMPLE:([^\s]+)\s*-->/g, (_, exampleId) => {
      return renderGeneratedExample(exampleId);
    });
  }

  eleventyConfig.addShortcode("legacyContent", function(pageId) {
    const contentFile = path.join(contentRoot, `${pageId}.html`);
    if (!fs.existsSync(contentFile)) {
      throw new Error(`Missing generated content file: ${contentFile}`);
    }

    let html = fs.readFileSync(contentFile, "utf8");
    return injectGeneratedExamples(html);
  });

  eleventyConfig.addShortcode("exampleBlock", function(exampleId) {
    return renderGeneratedExample(exampleId);
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
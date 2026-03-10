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
  const examplesRoot = path.join(projectRoot, "src", "examples");
  const markdownRenderer = new MarkdownIt({
    html: true,
    linkify: true,
    breaks: false
  });

  eleventyConfig.addPassthroughCopy({ "src/assets": "assets" });
  eleventyConfig.addPassthroughCopy({ "src/site-assets/js": "assets/js" });
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
const fs = require("node:fs");
const path = require("node:path");
const { createExampleCatalog, renderExampleBlock } = require("./lib/render-example-block");

module.exports = function configureEleventy(eleventyConfig) {
  const projectRoot = __dirname;
  const generatedRoot = path.join(projectRoot, "src", "_generated");
  const contentRoot = path.join(generatedRoot, "content");
  const examplesRoot = path.join(generatedRoot, "examples");

  eleventyConfig.addPassthroughCopy({ "src/assets": "assets" });
  eleventyConfig.addPassthroughCopy({ "src/site-assets/js": "assets/js" });
  eleventyConfig.addPassthroughCopy({ "src/style.css": "style.css" });
  eleventyConfig.addPassthroughCopy({ "src/media-root": "." });

  eleventyConfig.addWatchTarget("src/_generated/");

  eleventyConfig.addShortcode("legacyContent", function(pageId) {
    const contentFile = path.join(contentRoot, `${pageId}.html`);
    if (!fs.existsSync(contentFile)) {
      throw new Error(`Missing generated content file: ${contentFile}`);
    }

    const examples = createExampleCatalog(examplesRoot);
    let html = fs.readFileSync(contentFile, "utf8");
    html = html.replaceAll(/<!--\s*EXAMPLE:([^\s]+)\s*-->/g, (_, exampleId) => {
      const example = examples[exampleId];
      if (!example) {
        throw new Error(`Missing generated example: ${exampleId}`);
      }

      return renderExampleBlock(example);
    });

    return html;
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
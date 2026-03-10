module.exports = [
  {
    files: ["src/site-assets/js/**/*.js"],
    languageOptions: {
      ecmaVersion: 2024,
      sourceType: "module",
      globals: {
        window: "readonly",
        document: "readonly",
        localStorage: "readonly",
        console: "readonly",
        navigator: "readonly",
        location: "readonly",
        setTimeout: "readonly",
        clearTimeout: "readonly",
        setInterval: "readonly",
        clearInterval: "readonly",
        CustomEvent: "readonly",
        IntersectionObserver: "readonly"
      }
    },
    rules: {
      "no-undef": "error"
    }
  },
  {
    files: ["tools/**/*.mjs", "lib/**/*.js", ".eleventy.js"],
    languageOptions: {
      ecmaVersion: 2024,
      sourceType: "module",
      globals: {
        module: "readonly",
        require: "readonly",
        __dirname: "readonly",
        process: "readonly",
        console: "readonly"
      }
    },
    rules: {
      "no-undef": "error"
    }
  }
];
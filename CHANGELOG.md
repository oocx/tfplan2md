# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<a name="0.9.0"></a>
## [0.9.0](https://github.com/oocx/tfplan2md/compare/v0.8.0...v0.9.0) (2025-12-18)

### ✨ Features

* Add agent definitions and workflows for project development ([439cc91](https://github.com/oocx/tfplan2md/commit/439cc9119146b7d0f4a3091d6b20a5b734ac36e9))

<a name="0.8.0"></a>
## [0.8.0](https://github.com/oocx/tfplan2md/compare/v0.7.0...v0.8.0) (2025-12-17)

### ✨ Features

* **renderer:** enhance firewall rule rendering ([26200ae](https://github.com/oocx/tfplan2md/commit/26200aef4a855e4bbd14b32300298bedd97e017b))

### 🐛 Bug Fixes

* **renderer:** apply resource-specific templates automatically and add regression test ([92032f5](https://github.com/oocx/tfplan2md/commit/92032f51dcad9b521f4d0a76ae3234638ced465e))

<a name="0.7.0"></a>
## [0.7.0](https://github.com/oocx/tfplan2md/compare/v0.6.0...v0.7.0) (2025-12-16)

### ✨ Features

* **module-grouping:** group resource changes by module; add grouping tests and documentation ([bbe5850](https://github.com/oocx/tfplan2md/commit/bbe5850db19ef9866ffb1c57ffd087c1c6a21e6d))

<a name="0.6.0"></a>
## [0.6.0](https://github.com/oocx/tfplan2md/compare/v0.5.0...v0.6.0) (2025-12-16)

### ✨ Features

* per-action attribute tables in template; add docs and tests for edge cases ([ab62571](https://github.com/oocx/tfplan2md/commit/ab62571de19565d1b33b96e688f295b81825254a))

<a name="0.5.0"></a>
## [0.5.0](https://github.com/oocx/tfplan2md/compare/v0.4.0...v0.5.0) (2025-12-16)

### ✨ Features

* Implement resource-specific templates for azurerm_firewall_network_rule_collection ([31bcfb6](https://github.com/oocx/tfplan2md/commit/31bcfb6a32bf187295e37d56967274a06d7bd469))

### ♻️ Refactoring

* update assertions to use FluentAssertions for improved readability ([01c04c2](https://github.com/oocx/tfplan2md/commit/01c04c29d8080954a2925f3789c6580584c0756e))

### 📚 Documentation

* update documentation ([c0474ad](https://github.com/oocx/tfplan2md/commit/c0474adb14e048bfeab79fa7a9e89d6670cf15fa))

<a name="0.4.0"></a>
## [0.4.0](https://github.com/oocx/tfplan2md/compare/v0.3.0...v0.4.0) (2025-12-15)

### ✨ Features

* add handling for empty plans to display "No changes" message ([0420035](https://github.com/oocx/tfplan2md/commit/042003574585155931c772f93a1c22a924deb783))

<a name="0.3.0"></a>
## [0.3.0](https://github.com/oocx/tfplan2md/compare/v0.2.0...v0.3.0) (2025-12-15)

### ✨ Features

* filter no-op resources from detailed changes to reduce output noise and fix errors with large plans ([c65f879](https://github.com/oocx/tfplan2md/commit/c65f8790d4883eb6914380d7946c87cbdde66221))

<a name="0.2.0"></a>
## [0.2.0](https://github.com/oocx/tfplan2md/compare/v0.1.3...v0.2.0) (2025-12-15)

### ✨ Features

* simplify default template ([580d27f](https://github.com/oocx/tfplan2md/commit/580d27f1fe087151b2efa8aa87e8a1b31a346646))
* update action symbols to use emojis in report generation and tests ([5d64bb1](https://github.com/oocx/tfplan2md/commit/5d64bb1bff37268e0c3617915321cfabeb1386b1))

### 📚 Documentation

* add example project to generate a valid plan file ([39dffa8](https://github.com/oocx/tfplan2md/commit/39dffa84df1422fc32c14b28f977098cbc5a7bb4))

<a name="0.1.3"></a>
## [0.1.3](https://github.com/oocx/tfplan2md/compare/v0.1.2...v0.1.3) (2025-12-15)

### 🐛 Bug Fixes

* apply whitespace control to fix table formatting ([d587f3c](https://github.com/oocx/tfplan2md/commit/d587f3c3952899ce165de6dd6f6d43279c219e55))
* remove extra newlines in attribute changes tables ([d6d185b](https://github.com/oocx/tfplan2md/commit/d6d185b9306ae4523cc20875e6d47d78512057bc))
* strip trailing newlines in attribute changes table rows ([7120d2c](https://github.com/oocx/tfplan2md/commit/7120d2cebfecabf76472e6ae5a6cc5e5a2efc3f9))
* strip trailing newlines in attribute changes table rows ([a03e444](https://github.com/oocx/tfplan2md/commit/a03e444751dac2e08a800ccebb86de32ce76af57))

### 📚 Documentation

* add bug fixing guideline to documentation ([152accd](https://github.com/oocx/tfplan2md/commit/152accdb86221f5df78d8964dbd107d046ea557e))

<a name="0.1.2"></a>
## [0.1.2](https://github.com/oocx/tfplan2md/compare/v0.1.1...v0.1.2) (2025-12-14)

### 🐛 Bug Fixes

* improve Markdown rendering by enhancing template context handling and error reporting ([0c86c01](https://github.com/oocx/tfplan2md/commit/0c86c016f5b11d02bb9ce314ce683189996b6bad))

<a name="0.1.1"></a>
## [0.1.1](https://github.com/oocx/tfplan2md/compare/v0.1.0...v0.1.1) (2025-12-14)

### 🐛 Bug Fixes

* trigger release ([e01f730](https://github.com/oocx/tfplan2md/commit/e01f730541ad72db370b5c10f1b53965c9149904))

<a name="0.1.0"></a>
## [0.1.0](https://github.com/oocx/tfplan2md/compare/v0.1.0...v0.1.0) (2025-12-14)

### ✨ Features

* add architectural decision records for Scriban templating, Chiseled Docker image, and modern C# patterns ([7612fe7](https://github.com/oocx/tfplan2md/commit/7612fe71fdb947a4d517313aafe0ee5474cfdda6))
* Enhance documentation and setup instructions ([3180246](https://github.com/oocx/tfplan2md/commit/31802463ec3ae92460dbf82497f25835ed0b67cb))
* initial implementation ([ea57cb1](https://github.com/oocx/tfplan2md/commit/ea57cb138a82133c4bf0cdb106767ae807a555e8))

### 📚 Documentation

* initial project specification ([6a0cc4e](https://github.com/oocx/tfplan2md/commit/6a0cc4e89dc7afbb77436292615bf4a9fbbf25a0))
* update test documentation ([f8fdb0f](https://github.com/oocx/tfplan2md/commit/f8fdb0fa049e9a1a152a24382334046c5c0d6ca9))


# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<a name="1.37.2"></a>
## [1.37.2](https://github.com/oocx/tfplan2md/compare/v1.37.1...v1.37.2) (2026-03-12)

### 🐛 Bug Fixes

* add permissions: contents: read to uat-validate and copilot-setup-steps workflows ([1786076](https://github.com/oocx/tfplan2md/commit/1786076db12bcd2302f0b5816be85b6fd1b36b7a))
* apply all CodeQL security fixes from GitHub Security tab ([093de62](https://github.com/oocx/tfplan2md/commit/093de6221329e9781a9fef48c3e7261ecc80fa28))
* correct codeql workflow - add dotnet restore, update action versions ([a82a8c4](https://github.com/oocx/tfplan2md/commit/a82a8c43d0fa8f3da176fc3cbb83a53e69ac89d5))
* patch embedded highlight.js regex - fix A-z range and html tag filter (CodeQL A/C) ([771f0ee](https://github.com/oocx/tfplan2md/commit/771f0ee5522676cb63b5b58c4d3578a787cbeb6c))
* remove duplicate codeql.yml - GitHub already runs managed CodeQL scanning ([dc02a63](https://github.com/oocx/tfplan2md/commit/dc02a63923b6518a10f4f997b5554479f1ab299b))
* remove re-tracked artifact html files and inline CDN deps in example (remaining alerts) ([a485bec](https://github.com/oocx/tfplan2md/commit/a485bec0967aedf82cff9cb6e6d69c50be05c997))
* untrack artifact html files that were committed before .gitignore entries were added ([8d709bc](https://github.com/oocx/tfplan2md/commit/8d709bc59bc34a65c0a83090d8009083b3721be6))
* update docker actions versions, hardcode dockerhub username, add codeql workflow ([db49de8](https://github.com/oocx/tfplan2md/commit/db49de8908b28c3cd30ce215b8fab4766f5e6d73))
* use urlparse hostname check to prevent URL substring sanitization bypass ([402ca73](https://github.com/oocx/tfplan2md/commit/402ca73fe96340e1a4200d5fafe083eebfd165a2))

### 📚 Documentation

* add code review for security fixes ([80c27af](https://github.com/oocx/tfplan2md/commit/80c27af86186105c0e7dc735f1d2413664a9a2d1))
* add developer work log entry to work protocol ([92eba68](https://github.com/oocx/tfplan2md/commit/92eba682abe31d8eaef80a690da0309e0ee448b7))
* add github security tab analysis ([b147cf2](https://github.com/oocx/tfplan2md/commit/b147cf20b3961aa00040451f17afb25e4558e176))
* add release-notes.md for fix-security-issues to pass PR validation ([0da5adb](https://github.com/oocx/tfplan2md/commit/0da5adb70d0df1d1345b5b2163f9ed022e9dc891))
* add security issue analysis for fix-security-issues ([07c54f9](https://github.com/oocx/tfplan2md/commit/07c54f9e10872ca89cdbcbf6e27392cb9b0213a1))
* detailed analysis of all 30 codeql security issues ([64d7aa9](https://github.com/oocx/tfplan2md/commit/64d7aa9cbbd908624d47e0638fdc6f3ae47f8447))
* technical writer review - add CodeQL workflow to CI/CD tables ([f58b9bd](https://github.com/oocx/tfplan2md/commit/f58b9bd28686582eca2770e591eaf2c607854ae6))
* update code review - codeql.yml technical fixes approved, process blockers remain ([417ba4c](https://github.com/oocx/tfplan2md/commit/417ba4c771aa3ccdd7c3390a00511f7061d97d66))

<a name="1.37.1"></a>
## [1.37.1](https://github.com/oocx/tfplan2md/compare/v1.37.0...v1.37.1) (2026-03-12)

### 🐛 Bug Fixes

* nsg rule rendering ([7e09012](https://github.com/oocx/tfplan2md/commit/7e09012e703d7f0994c10cf525b857ebd2cd565f))

<a name="1.37.0"></a>
## [1.37.0](https://github.com/oocx/tfplan2md/compare/v1.36.0...v1.37.0) (2026-03-11)

### ✨ Features

* Enhance code highlighting and add YAML support in code tabs ([84c2180](https://github.com/oocx/tfplan2md/commit/84c2180ed98b026c8c15c1567e3fc533bcdcd60a))

### 🐛 Bug Fixes

* **website:** minor layout fixes ([750b7b1](https://github.com/oocx/tfplan2md/commit/750b7b19753194da7101cbe4c8e47380ed56053d))

### ♻️ Refactoring

* AzDO mapper base class and formatter helper (Tasks 3-5) ([60ad64d](https://github.com/oocx/tfplan2md/commit/60ad64dd8e6b89b8e01c99067b156f14005ed409))
* completed refactoring, now 1:1 parity with previous website ([1e88b29](https://github.com/oocx/tfplan2md/commit/1e88b29e1052ebcc95f9750b49198698510094f9))
* continue website refactoring ([30dde46](https://github.com/oocx/tfplan2md/commit/30dde46e791909342981aa317cb7b03fe1b8de6c))
* continue website refactoring ([5669bf8](https://github.com/oocx/tfplan2md/commit/5669bf8e1a7f53d6ed60e126f99419372bba9703))
* convert ServiceResolutionContext to positional record ([e28ec29](https://github.com/oocx/tfplan2md/commit/e28ec29cb5ea097f1a23a613913511bca7369c70))
* extract duplicate helpers to shared utilities ([4ede8de](https://github.com/oocx/tfplan2md/commit/4ede8de5e1678d3b400c9a6d7be927592828beeb))
* fix code review issues - remove unused principalMapper param, fix naming, add comment ([5ed6f85](https://github.com/oocx/tfplan2md/commit/5ed6f852582c3bdb133c893f6995c216b3549fb7))
* fix pre-existing build errors from Tasks 1-16 and implement Tasks 17-22 ([872f894](https://github.com/oocx/tfplan2md/commit/872f894b2c2e3835022f8d3c788d595a583c2506))
* fix two minor code review findings ([b0eca92](https://github.com/oocx/tfplan2md/commit/b0eca92fced2cc7cf2ecf70d9cc27586cf1c4962))
* implement code review suggestions ([54c7f54](https://github.com/oocx/tfplan2md/commit/54c7f54bf4b1224008f42b9e55ac5cd1b991e73b))
* introduce ApplyViewModelContext record (Tasks 1-2) ([5592b89](https://github.com/oocx/tfplan2md/commit/5592b89180e86d539c3020c475c1f6a0439794df))
* introduce ReportModelBuilder option/service records and reduce Render method complexity ([a8c9536](https://github.com/oocx/tfplan2md/commit/a8c95363c3efd7e0ebecc10fb01ae2e3cfbeaf75))
* Part 3 — modern C# language features and implementation consistency fixes ([f3176c8](https://github.com/oocx/tfplan2md/commit/f3176c8d057946c7d418d328bb7b79ec20acda30))
* remove BuildDefinitionRenderer, vestigial factories, add TryGetFactory, remove unused registry params (Tasks 6-9) ([f802abf](https://github.com/oocx/tfplan2md/commit/f802abf4a9f22a06c5ccaf1e2b329fd4c32ef0a6))
* remove unused useWideSeparators parameter and isNoOpParentChildScenario variable from ReportRenderer ([2ba6f98](https://github.com/oocx/tfplan2md/commit/2ba6f98fe75cec7fc5e43c057e92597094418d95))
* rename DiffFormatterStringExtensions to MarkdownStringExtensions ([e9663ba](https://github.com/oocx/tfplan2md/commit/e9663baaa9c8a1c77646f933d9d5a673c66380c7))
* update acceptance criteria to reflect completed tasks in code simplification ([b5b8b14](https://github.com/oocx/tfplan2md/commit/b5b8b14cdf0fc4a96880b364f6440610e4631ba9))
* use explicit-type collection expressions (string[], ChildTableColumn[]) instead of cast syntax ([61d361e](https://github.com/oocx/tfplan2md/commit/61d361e939f329c653f17626be7fa9b6c15d6c07))
* **website:** add static site generator ([829dad9](https://github.com/oocx/tfplan2md/commit/829dad9ca544bf9a939477353f0298494ad2429f))
* **website:** replace website with new version ([e4d0492](https://github.com/oocx/tfplan2md/commit/e4d0492be4e3f22dc86d1610e0c8b95691b9079c))

### 📚 Documentation

* add architecture for feature 111 code simplification ([7ac8a16](https://github.com/oocx/tfplan2md/commit/7ac8a162758c2aa7ea81e73984bedf990baf36ed))
* add detailed code quality findings report ([a609b6a](https://github.com/oocx/tfplan2md/commit/a609b6ac35cc2b883e2252770f83a0cd062e2e7e))
* add Feature 111 entry to features.md and technical writer work protocol ([7cba5a7](https://github.com/oocx/tfplan2md/commit/7cba5a778a0cf2f885ed4e1b7d3836acc6df58a8))
* add feature specification for 111-code-simplification ([f68d6b9](https://github.com/oocx/tfplan2md/commit/f68d6b9af31c73ed6c9d3fb06c1a8b048d5666ac))
* add release notes and release manager entry for Feature 111 code simplification ([dbb43f9](https://github.com/oocx/tfplan2md/commit/dbb43f94f202017b97039640a6f3d10abb47331a))
* add task breakdown for feature 111 code simplification ([5dee0ea](https://github.com/oocx/tfplan2md/commit/5dee0ea854034cd385715c65936538dc8b605824))
* add test plan and quality engineer/task planner work protocol entries for Feature 111 ([0a0b83d](https://github.com/oocx/tfplan2md/commit/0a0b83d712e5af16b57e36d1e764525c79612be5))
* clarify copilot PR branch exception ([01dbf2b](https://github.com/oocx/tfplan2md/commit/01dbf2b8964a4038a1dc8020e0608107edfd109e))
* log developer entry 12 in work-protocol ([dc7ae6e](https://github.com/oocx/tfplan2md/commit/dc7ae6e470c4825b7b713180380df6e212c7e3a8))
* second code review pass - approve Feature 111 code simplification ([6425060](https://github.com/oocx/tfplan2md/commit/6425060d2cb823bbcac2aeee0f22f823b934226f))
* update code review for feature 111 code simplification ([113afce](https://github.com/oocx/tfplan2md/commit/113afceff16d4a2a3e58120d4e0766b4d62a0fd0))
* update code review for feature 111 code simplification ([58ac4b3](https://github.com/oocx/tfplan2md/commit/58ac4b300be84229c61a6c6375c643f8453ec420))

<a name="1.36.0"></a>
## [1.36.0](https://github.com/oocx/tfplan2md/compare/v1.35.0...v1.36.0) (2026-03-07)

### ✨ Features

* complete feature 110 refactoring work ([69ed445](https://github.com/oocx/tfplan2md/commit/69ed445c81a9b45ca547bd272fe3a2c342f532e9))

### ♻️ Refactoring

* complete feature 110 review rework for tasks 7-9 ([b21b74c](https://github.com/oocx/tfplan2md/commit/b21b74c68362bd4f9d1a110ce0e32ec090f2ab56))

### 📚 Documentation

* add code review 3 for feature 110 — changes requested (uncommitted Tasks 7-9) ([b4e9334](https://github.com/oocx/tfplan2md/commit/b4e9334e6adc461928903193cfcf9d81b9f60483))
* add code review 4 for feature 110 — approved ([74de6b8](https://github.com/oocx/tfplan2md/commit/74de6b88200817c4cccb7131076bc5959a13a14a))
* add release manager entry to work protocol for feature 110 ([ab542db](https://github.com/oocx/tfplan2md/commit/ab542db18f726468535d44fba877017a79c92793))
* add release notes for feature 110 ([f49991a](https://github.com/oocx/tfplan2md/commit/f49991a3b653ed5777d171a5d62a16c70b7f0422))
* extend test plan with Tasks 6-9 coverage (TC-30 through TC-48) ([fcb9daf](https://github.com/oocx/tfplan2md/commit/fcb9daff0b60efa2e3ba25bc4dd53f34f4da8148))

<a name="1.35.0"></a>
## [1.35.0](https://github.com/oocx/tfplan2md/compare/v1.34.1...v1.35.0) (2026-03-06)

### ✨ Features

* add casing-filter test, feature comment, and help text update ([12df0e3](https://github.com/oocx/tfplan2md/commit/12df0e354b5912e6dff825057d32ef0a0c4c49c4))
* emit Azure DevOps pipeline variable tfplan2md_haschanges ([c991e28](https://github.com/oocx/tfplan2md/commit/c991e2839fc927b6a9802102df0924f79ea1b5a9))
* suppress haschanges variable when no output file specified ([93c8ec7](https://github.com/oocx/tfplan2md/commit/93c8ec7bb50e2bdd48d8185780a9912a31f3e5ae))

### 🐛 Bug Fixes

* correct feature reference comment in ProgramEntry.cs ([0c2c509](https://github.com/oocx/tfplan2md/commit/0c2c5093de6ed767609d5ac545e0389edaec24c0))
* correct feature reference comment in ProgramEntry.cs ([ade71ae](https://github.com/oocx/tfplan2md/commit/ade71ae9d86f71078f2c4fb411d18ed466c23916))

### 📚 Documentation

* add feature analysis for Azure DevOps has-changes variable ([c319247](https://github.com/oocx/tfplan2md/commit/c319247a1f31b3ddbfd6e4876493e6377aaf53ff))
* add release notes and complete work protocol for feature 109 ([7500ef9](https://github.com/oocx/tfplan2md/commit/7500ef9c01b8bb13f164a9d6d3766b21b2bd4d33))
* document Azure DevOps haschanges pipeline variable feature ([96d439a](https://github.com/oocx/tfplan2md/commit/96d439a7e2bf5e1cb317b095aa88ffead893eb35))
* note rebase conflict root cause and resolution steps in work-protocol ([7c2d6b5](https://github.com/oocx/tfplan2md/commit/7c2d6b599384deb1ac8322237cd614061d5a8b80))

<a name="1.34.1"></a>
## [1.34.1](https://github.com/oocx/tfplan2md/compare/v1.34.0...v1.34.1) (2026-03-06)

### 🐛 Bug Fixes

* remove 5 code review findings (dead code, visibility, cache leak) ([e190e92](https://github.com/oocx/tfplan2md/commit/e190e928ed5e9c7bafdd3151a0da8caf0d3dd643))
* remove model: from coding agent files to fix 400 error on GitHub.com ([eea23ce](https://github.com/oocx/tfplan2md/commit/eea23cea0ea334050323c1b9a0dee1f2d088e541))
* remove snapshot-compatibility heuristics and dead code from rendering pipeline ([82e4e7d](https://github.com/oocx/tfplan2md/commit/82e4e7de5daba1ebe8d3f40806990003939aee35))

### 📚 Documentation

* add issue analysis for code review top 5 findings (issue 109) ([b91b0f5](https://github.com/oocx/tfplan2md/commit/b91b0f5f582bab2f38438d71dfeae593f6dd7ff4))
* add release notes and release manager log for issue 109 ([b6732c3](https://github.com/oocx/tfplan2md/commit/b6732c3984c8b0b774576c57687c4ed43a051fed))

<a name="1.34.0"></a>
## [1.34.0](https://github.com/oocx/tfplan2md/compare/v1.33.1...v1.34.0) (2026-03-04)

### ✨ Features

* add UPX compression for Linux and Windows binaries ([b7000d3](https://github.com/oocx/tfplan2md/commit/b7000d3724d2f67860d38a4bc5023e1e21d27cc1))

### 🐛 Bug Fixes

* filter out casing-only Azure ID changes in azapi body comparison ([cc9ed44](https://github.com/oocx/tfplan2md/commit/cc9ed4456e24849fd64db7a528a39254f183b04d))
* reimplement azapi casing filter on post-Scriban C# rendering pipeline ([bc468c2](https://github.com/oocx/tfplan2md/commit/bc468c213824a9b6fa123560327c22b07347fe9d))

### 📚 Documentation

* add code review report for issue 108 azapi body casing filter ([8cb02c0](https://github.com/oocx/tfplan2md/commit/8cb02c0e21871b9d6b28cd87062ae900866625e4))
* add developer work protocol entry for issue 108 ([05e37cb](https://github.com/oocx/tfplan2md/commit/05e37cbc641170a68fb2eb9a3a0cb998b15de44a))
* add issue analysis for azapi body casing-only Azure ID filter ([#108](https://github.com/oocx/tfplan2md/issues/108)) ([60473da](https://github.com/oocx/tfplan2md/commit/60473dab7122932b2fa705eb86735a3df96d09c9))
* add release notes for issue 108 azapi casing filter ([bbcb7da](https://github.com/oocx/tfplan2md/commit/bbcb7daff6bf4617332b77a2106ab9c6b4803089))
* update documentation for azapi casing filter ([62dce7a](https://github.com/oocx/tfplan2md/commit/62dce7a2aa26365866af94f54ec85a9a6fdbbd34))

<a name="1.33.1"></a>
## [1.33.1](https://github.com/oocx/tfplan2md/compare/v1.33.0...v1.33.1) (2026-03-04)

### 🐛 Bug Fixes

* isolate AOT publish from JsonEmbedGenerator build ([6630ba5](https://github.com/oocx/tfplan2md/commit/6630ba59742196537e16c913c4fe158743704eb5))

### 📚 Documentation

* add issue analysis for binary build failure ([4bfe5a7](https://github.com/oocx/tfplan2md/commit/4bfe5a7cbcf561c4d523f8dda5e7ac0c7a8d508f))
* add release notes for fix/108-binary-builds-failed ([6452fc2](https://github.com/oocx/tfplan2md/commit/6452fc2fcceb4043041e9c3382752a07cf58ba5a))

<a name="1.33.0"></a>
## [1.33.0](https://github.com/oocx/tfplan2md/compare/v1.32.0...v1.33.0) (2026-03-04)

### ✨ Features

* add build step for source generators in PR validation workflow ([b814bf9](https://github.com/oocx/tfplan2md/commit/b814bf959c1f3d1e68f3368ab7d96f2fe1e760a9))
* finalize reflection-free embedded json generator integration ([03412b3](https://github.com/oocx/tfplan2md/commit/03412b3508bb43126cffc5ed1acc9a001af10579))
* implement azapi-specific C# rendering pipeline for fix [#1](https://github.com/oocx/tfplan2md/issues/1) ([9e42b3a](https://github.com/oocx/tfplan2md/commit/9e42b3a7a32654e93fb57b6091cba3b45e6267de))

### 🐛 Bug Fixes

* drop System.Xml.Linq to reduce NativeAOT size ([6c209a5](https://github.com/oocx/tfplan2md/commit/6c209a5c01610797780b39c023f323a70c2f3089))
* finalize azapi output values snapshot parity ([6281e40](https://github.com/oocx/tfplan2md/commit/6281e40a0eed17d36aeb883cc0ab674398fe100c))
* implement snapshot parity fixes 11 6 7 8 ([694c2e9](https://github.com/oocx/tfplan2md/commit/694c2e9b030bab7ddb33ec510f2cb6695654ba60))
* implement snapshot parity fixes 12, 4, and 5 ([e78690a](https://github.com/oocx/tfplan2md/commit/e78690ab3576d14947266c525cbcbe768f0f849f))
* revert snapshots to main ([10f2c13](https://github.com/oocx/tfplan2md/commit/10f2c134c4a1e0bb4634df8f2be3ed4768d527c6))

### ♻️ Refactoring

* enhance Dockerfile by adding lld to toolchain and specifying LinkerFlavor for publishing ([764fd93](https://github.com/oocx/tfplan2md/commit/764fd93aef08cd639d9634bb02dcb50475fdeec6))
* optimize Dockerfile and csproj for reduced image size ([3e74e12](https://github.com/oocx/tfplan2md/commit/3e74e1236cc0ec0e139b377a1541666d2243ad75))
* optimize Dockerfile by removing unnecessary library installations and clarifying comments ([133014c](https://github.com/oocx/tfplan2md/commit/133014cdc5b6abb80fbe0744ff7f97b5a7bc796f))
* remove Scriban and migrate to pure C# rendering ([47980b8](https://github.com/oocx/tfplan2md/commit/47980b8cf917edd455e51563ae318adcaa294069))
* remove summary separator mode flag ([3a5b947](https://github.com/oocx/tfplan2md/commit/3a5b94725dc9c0dbba09a3e28ffd5811a0488702))
* remove unnecessary tfplan2md.xml from publish output ([ca97c7b](https://github.com/oocx/tfplan2md/commit/ca97c7b693f2c6a83d69ead82c6d7ce1ffbaaefd))
* streamline Dockerfile by enhancing publish command and removing unnecessary base stage ([514b7dc](https://github.com/oocx/tfplan2md/commit/514b7dc658582ea5835d0a7040d1f9c31feaf1ad))
* update README and integration tests to reflect removal of comprehensive demo files from Docker image ([980436a](https://github.com/oocx/tfplan2md/commit/980436a461fe35a39ecfdcca11aa2aa42427b8b5))

### 📚 Documentation

* add code review for 107-remove-scriban ([ee357fe](https://github.com/oocx/tfplan2md/commit/ee357fe5cc53e8ed428621fb08a51abc40ac4c64))
* add code review round 3 for feature 107 ([fe780f7](https://github.com/oocx/tfplan2md/commit/fe780f7cda9370aae5fbf2e93a66331ab895745f))
* add feature 107 architecture baseline ([be8c97c](https://github.com/oocx/tfplan2md/commit/be8c97cb005a32194483a11080fcdfc0431d6678))
* add feature specification for 107-remove-scriban ([ee361bb](https://github.com/oocx/tfplan2md/commit/ee361bbc608e7d12010506db690c86ada0b6ce11))
* add release notes for feature 107 - remove Scriban ([1617fdc](https://github.com/oocx/tfplan2md/commit/1617fdc56e81a9987e299f58e00b0e70b0e03a00))
* add snapshot analysis report and detailed fix plan for feature/107-remove-scriban ([8016527](https://github.com/oocx/tfplan2md/commit/8016527f00fec684db41ed10b8e04a5ef636918a))
* document reflection removal plan ([a73b089](https://github.com/oocx/tfplan2md/commit/a73b089c6fdcdfc27cfcba35ea9e4f4fbffc414b))
* fix stale Scriban references in architecture.md, features.md, and Providers/README.md ([e041177](https://github.com/oocx/tfplan2md/commit/e04117741363220a90d1fe2caddec7309b0d6717))
* remove Scriban guidance from agent instructions ([1929bb7](https://github.com/oocx/tfplan2md/commit/1929bb7111a60ac035bfa5845f89d53309235291))
* update code review for feature 107 (round 2) - changes requested ([f836c26](https://github.com/oocx/tfplan2md/commit/f836c264d442a8126ca91d3e02e891203e972704))
* **107:** add QE test plan with 100% branch coverage for new rendering types ([d30158d](https://github.com/oocx/tfplan2md/commit/d30158db9f6b18dfaadba9a6fc920c182df22436))

<a name="1.32.0"></a>
## [1.32.0](https://github.com/oocx/tfplan2md/compare/v1.31.2...v1.32.0) (2026-03-01)

### ✨ Features

* add UAT plan artifacts for feature 106 (uat-plan.json, uat-plan.md) ([d03a50f](https://github.com/oocx/tfplan2md/commit/d03a50ff2412dc9237a0e0c36ef0258e8bb5c0f1))
* expose after_unknown to Scriban templates for output-unknown detection ([5f3562f](https://github.com/oocx/tfplan2md/commit/5f3562f733891ed20c28cbaa6c9d8776fe6f9f3e))
* render azapi output values in a separate table (feature 106) ([b99f591](https://github.com/oocx/tfplan2md/commit/b99f591317828d77909e65e8050696611daf634a))

### 🐛 Bug Fixes

* add Output Values heading for known-after-apply and replace-before cases SNAPSHOT_UPDATE_OK ([b82b209](https://github.com/oocx/tfplan2md/commit/b82b2090d9518bf9982464f64fd760af538dea3b))
* remove output values section when all outputs unknown; add display name mapping to UAT (SNAPSHOT_UPDATE_OK) ([d065a92](https://github.com/oocx/tfplan2md/commit/d065a923f95cad3e31e955054a98e99417400c7b))
* resolve MD024 markdownlint error, fix MD049 emphasis style, add azapi output to demo ([3c836c5](https://github.com/oocx/tfplan2md/commit/3c836c5fc33da737cea1d957670fbea42bbfa9b0))
* update UAT plan to show grouped output (sku sub-object) and distribute resources across modules ([2f661ea](https://github.com/oocx/tfplan2md/commit/2f661eac7d713d822465d009eae5f53903a24044))

### ♻️ Refactoring

* extract output values rendering to partial template ([d3f1a9c](https://github.com/oocx/tfplan2md/commit/d3f1a9ca55c772d4be1e82d9c16be00a3934d078))

### 📚 Documentation

* add architecture for feature 106 azapi output values ([c9fbeba](https://github.com/oocx/tfplan2md/commit/c9fbeba5cb6615cf078ce3ddd1c556e6c6395989))
* add minor template comment issue (m-1) found by automated code review ([67e7582](https://github.com/oocx/tfplan2md/commit/67e7582c1fc73cfb28c34a495f8f04138dbf800c))
* add release notes and screenshots for feature 106 azapi output values ([6489cbe](https://github.com/oocx/tfplan2md/commit/6489cbe38d97c60f5d387d22acc5260d72ceb842))
* add specification for azapi output values feature 106 ([6aa3e16](https://github.com/oocx/tfplan2md/commit/6aa3e165f13534c323dc4f4a2c9d1bec605818fb))
* add tasks for feature 106 azapi output values ([2e1052c](https://github.com/oocx/tfplan2md/commit/2e1052cd80a6e35436088125f1a21a3c4e782c43))
* add tasks for feature 106 azapi output values ([2611bfa](https://github.com/oocx/tfplan2md/commit/2611bfaaadd577b51a22ccab775802792dcc11e9))
* add test plan for feature 106 azapi output values ([a7e8f98](https://github.com/oocx/tfplan2md/commit/a7e8f981dd3e5d339c690c390b65d306300729fe))
* add UAT report for feature 106 azapi output values - PASSED ([e107bc6](https://github.com/oocx/tfplan2md/commit/e107bc62b61cfa76dadcc3fbe0b26f7782995ebf))
* code review approved for feature 106 - ready for UAT ([c644440](https://github.com/oocx/tfplan2md/commit/c6444405c8ef19ecb24b53b2506b1da1b703000d))
* code review round 3 for feature 106 - changes requested (B-8a, B-8b) ([404c536](https://github.com/oocx/tfplan2md/commit/404c536ef5c0a31f827a947649b3bdee6e5beb4d))
* code review round 5 for feature 106 - changes requested (M-1, M-2, M-3 doc inconsistencies) ([5395ca6](https://github.com/oocx/tfplan2md/commit/5395ca60064f5b1b188da4b8f2492fa1a6b6bbe0))
* log Developer B-8a/B-8b fix in work protocol ([7eaa29c](https://github.com/oocx/tfplan2md/commit/7eaa29c2687ef34b185da19a080a693c775ebb13))
* mark code review complete for feature 106 ([77e0212](https://github.com/oocx/tfplan2md/commit/77e0212f4eae6457e45c9e1fa5e0f7abee2e6b9d))
* mark code review complete for feature 106 (changes requested) ([fae3b49](https://github.com/oocx/tfplan2md/commit/fae3b49e9621f6588019c1987f662157f0978fa1))
* mark code review re-review complete for feature 106 (changes requested - B-8, B-9) ([356cbef](https://github.com/oocx/tfplan2md/commit/356cbef740e002f019d6e93d72ee10ceeb30a6ac))
* update architecture.md with after_unknown, before_sensitive, after_sensitive template properties ([74ed94a](https://github.com/oocx/tfplan2md/commit/74ed94a346e6d6ad0a5e0ce0ef813db0d510fbb0))
* update features.md for feature 106 azapi output values ([69bf247](https://github.com/oocx/tfplan2md/commit/69bf247a9d6257069594477f164540a1567fee3c))
* update spec, test-plan, and snapshot test docs to reflect suppressed output section ([2e11728](https://github.com/oocx/tfplan2md/commit/2e11728f52069cfeab454c1c0b36ad108e36e3b6))
* update work protocol with Developer log for feature 106 ([5b8b99f](https://github.com/oocx/tfplan2md/commit/5b8b99f555b61dcc78b0a8486450d72fe72901d7))
* update work protocol with Developer rework log for feature 106 code review blockers ([96e948f](https://github.com/oocx/tfplan2md/commit/96e948f12bbbd6e2b834dd89daf416e43b84cef5))
* update work-protocol release manager round 2 entry for feature 106 ([f0eed81](https://github.com/oocx/tfplan2md/commit/f0eed81f5d97aeba5b3818d2b4e0b4b506f785ed))

<a name="1.31.2"></a>
## [1.31.2](https://github.com/oocx/tfplan2md/compare/v1.31.1...v1.31.2) (2026-02-28)

### 🐛 Bug Fixes

* change OutputChange.AfterUnknown from bool to object? ([8ab23d4](https://github.com/oocx/tfplan2md/commit/8ab23d41de1b4fd3b2db3149ef539b8c02051001))

### 📚 Documentation

* add code review report and complete work protocol for issue [#106](https://github.com/oocx/tfplan2md/issues/106) ([df5e14b](https://github.com/oocx/tfplan2md/commit/df5e14b8b92138542b16918166ca9fa0de731e29))
* add issue analysis for OutputChange after_unknown type mismatch ([#106](https://github.com/oocx/tfplan2md/issues/106)) ([b85b239](https://github.com/oocx/tfplan2md/commit/b85b2399f012078c88e2812763f4ae65087a818e))
* add release notes for OutputChange.AfterUnknown fix ([ae7d2e9](https://github.com/oocx/tfplan2md/commit/ae7d2e90d31464f489ba2b7b6855d48ae30ba599))

<a name="1.31.1"></a>
## [1.31.1](https://github.com/oocx/tfplan2md/compare/v1.31.0...v1.31.1) (2026-02-27)

### 🐛 Bug Fixes

* address code review findings — remove redundant HashSet copy and fix CSS style consistency ([650124c](https://github.com/oocx/tfplan2md/commit/650124c119e3a350762c7866b0e28a2eeb256ea4))

### 🚀 Performance

* add LCS matrix size guard to prevent O(m×n) blowup with large values ([8dd0148](https://github.com/oocx/tfplan2md/commit/8dd01488ac4828307dc8bd943bb9a680c02f2912))
* implement findings 2-9 performance optimizations ([b5af1bb](https://github.com/oocx/tfplan2md/commit/b5af1bb5ad9bad3d93d663638cc23136855bfaa0))

### 📚 Documentation

* add code review report for performance investigation [#105](https://github.com/oocx/tfplan2md/issues/105) ([c2a0eff](https://github.com/oocx/tfplan2md/commit/c2a0eff3c57b57ee60b1c5ba9fcef2e37ff7c7fb))
* add per-finding fix proposals with user-facing impact descriptions ([3168b61](https://github.com/oocx/tfplan2md/commit/3168b61f8a38a00e3a23d1455128d6c63688c0ba))
* add performance investigation analysis for potential O(n²) patterns ([4e5e152](https://github.com/oocx/tfplan2md/commit/4e5e15260a3c87fdf9c8bd9ed9c1d01b9f2f429b))
* add release notes for performance investigation [#105](https://github.com/oocx/tfplan2md/issues/105) ([ee36484](https://github.com/oocx/tfplan2md/commit/ee36484bf5ce0a905300ab9f7b14113f9f321e36))
* clarify ThreadStatic rationale in BuildLineDiff cache documentation ([e795afb](https://github.com/oocx/tfplan2md/commit/e795afbe4e18ef88bce18db3a414ae88f2601c2d))
* rewrite release notes to frame as edge-case performance improvements ([81f3eb8](https://github.com/oocx/tfplan2md/commit/81f3eb8ad9a748c510a68dc06989ba973c293316))
* update findings 2, 8, 9 per maintainer feedback — caching, 50-char cutoff, JSON/XML heuristics ([8adeee6](https://github.com/oocx/tfplan2md/commit/8adeee68ca4cf4b6cd3cddfff1089d0181ba7858))
* update performance investigation documentation with implementation status ([71feda5](https://github.com/oocx/tfplan2md/commit/71feda575c0f50d3a8f7cd25df8f3a1798938b8f))

<a name="1.31.0"></a>
## [1.31.0](https://github.com/oocx/tfplan2md/compare/v1.30.0...v1.31.0) (2026-02-26)

### ✨ Features

* add --ignore-case-changes flag to suppress Azure resource ID casing noise ([6233032](https://github.com/oocx/tfplan2md/commit/623303290682a2d52f12bc35f5741386958fb7fa))
* add filtering note to report when resources are suppressed by --ignore-case-changes ([c12465a](https://github.com/oocx/tfplan2md/commit/c12465a4c9f89d5bfe75d82202f1a1447f54b328))
* rename --ignore-case-changes to --ignore-azure-id-case-changes, default true ([348951b](https://github.com/oocx/tfplan2md/commit/348951bb92a03af74724c97dd6035bf1895ad48b))
* rename --ignore-case-changes to --ignore-azure-id-case-changes, default true ([c3b13bf](https://github.com/oocx/tfplan2md/commit/c3b13bfd305d3cd5d9bfb1205bdb57234796b09d))

### 🐛 Bug Fixes

* pass raw values (not display values) to AttributeChangeFilterContext ([a4c73b6](https://github.com/oocx/tfplan2md/commit/a4c73b68b6870f174593c240d25e2981017803a7))
* resolve screenshot generation failures in headless environments ([ff28f04](https://github.com/oocx/tfplan2md/commit/ff28f049146f400f25fcee100dcc6f7f009581cc))
* suppress update resources with no remaining changes after attribute filtering ([e8524e8](https://github.com/oocx/tfplan2md/commit/e8524e852e3f702aea26c97acc1d0e8b06f37521))
* **review:** fix RoleAssignmentViewModelFactory filter bypass for update/replace actions ([345273f](https://github.com/oocx/tfplan2md/commit/345273fc0f1e8ba8ae929eb9df025e949eeea8a7))

### 📚 Documentation

* add architecture for case-insensitive attribute change filter (feature 103) ([94edd2b](https://github.com/oocx/tfplan2md/commit/94edd2b095a896746589772b091bd49124a05e8a))
* add code review for issue 575 test assertion improvements ([96e609a](https://github.com/oocx/tfplan2md/commit/96e609a48411ba653795be5fb09993e983304f52))
* add feature 103 (--ignore-case-changes) to features.md and README features list ([80be88a](https://github.com/oocx/tfplan2md/commit/80be88a7f3c80d6ae37c361d2948f302ea338550))
* add feature specification for 103-azure-id-case-insensitive-filter ([5aeb287](https://github.com/oocx/tfplan2md/commit/5aeb287449c536a619a2001e3778e1f816447670))
* add release manager entry to work protocol for feature 103 ([70852c2](https://github.com/oocx/tfplan2md/commit/70852c2407d10709e81713bb13f0f212b0277ab9))
* add release notes for feature 103 (--ignore-case-changes flag) ([ba7b0fc](https://github.com/oocx/tfplan2md/commit/ba7b0fc125394ef55d6908301eab9187c0aeea67))
* add release notes for issue 575 test assertion improvements ([3b26c28](https://github.com/oocx/tfplan2md/commit/3b26c281eae50b009fa416f70516235b0c600799))
* add tasks for feature 103 — azure-id-case-insensitive-filter ([4dcbc73](https://github.com/oocx/tfplan2md/commit/4dcbc739dd372153ede5a08e08b60dc9f09165b1))
* add test plan for feature 103 - azure ID case-insensitive filter ([4cfc618](https://github.com/oocx/tfplan2md/commit/4cfc61825f9d72a4a1cdb9ff89d9559448859dc0))
* mark all tasks complete for feature 103 ([28d2fff](https://github.com/oocx/tfplan2md/commit/28d2fff677e0a61404c96fdbff595b093e08958b))
* regenerate demo artifacts for feature 103 release ([9e3069c](https://github.com/oocx/tfplan2md/commit/9e3069c81110082f2376d9f6480a6654997bb754))
* revise architecture for feature 103 to scope filter to Azure resource IDs only ([e011474](https://github.com/oocx/tfplan2md/commit/e0114743c9a27a42d3f6004e43a72c1c7be463a6))
* revise tasks for feature 103 to reflect IAttributeChangeFilter extension point ([54c2d20](https://github.com/oocx/tfplan2md/commit/54c2d202e75a604d9385cb662cce68f8963cd2b6))
* revise test plan for feature 103 to align with updated architecture ([4943f7a](https://github.com/oocx/tfplan2md/commit/4943f7af1e0947b0f4e63719b78df2a08a5932c5))
* update release notes to reflect --ignore-azure-id-case-changes is on by default ([2e1d695](https://github.com/oocx/tfplan2md/commit/2e1d695fb6c3281ae1a6123497ab7ca09815b5a3))
* update specification, test-plan, and README to reflect --ignore-azure-id-case-changes default of true ([740fb99](https://github.com/oocx/tfplan2md/commit/740fb99d42b92b0351385ea334d43d23e9a46cab))
* update work protocol with developer log for feature 103 ([1f775a1](https://github.com/oocx/tfplan2md/commit/1f775a109e0968530032fd81a005ebdf25956d5a))

<a name="1.30.0"></a>
## [1.30.0](https://github.com/oocx/tfplan2md/compare/v1.29.0...v1.30.0) (2026-02-26)

### ✨ Features

* implement known-after-apply rendering scenarios ([e111267](https://github.com/oocx/tfplan2md/commit/e111267b4eec2bc22c10e37724c491a2c0213842))

### 📚 Documentation

* 10-run validation confirms 100% subagent git commit reliability ([ec26e4d](https://github.com/oocx/tfplan2md/commit/ec26e4d86165fbf63a3fa69c70db396cf593a6cf))
* add architecture for known-after-apply rendering ([64b6f8c](https://github.com/oocx/tfplan2md/commit/64b6f8c04ec36cb9a051ed40b6bf511974e532ae))
* add code review for known-after-apply rendering ([92af0cd](https://github.com/oocx/tfplan2md/commit/92af0cdb5f4e2a5441c2789436e7f8fa765041d7))
* add feature specification for 575-known-after-apply-rendering ([006ba78](https://github.com/oocx/tfplan2md/commit/006ba7892411ba98d7bd560a8b503b8fec8fa3b6))
* add known-after-apply docs and uat artifact ([cf5654c](https://github.com/oocx/tfplan2md/commit/cf5654c66a8a77e9f1da9036ccff2c1d7ff05e1e))
* add release notes for known-after-apply rendering (feature 102) ([e326e29](https://github.com/oocx/tfplan2md/commit/e326e29607f5f5c9d2a633e1a4080d49a52c636d))
* add tasks for known-after-apply rendering ([7a949a1](https://github.com/oocx/tfplan2md/commit/7a949a131dddcc0eea550cef2bc6b8b5e92b8818))
* add test plan and UAT test plan for known-after-apply rendering ([90d85d6](https://github.com/oocx/tfplan2md/commit/90d85d6b1fbd707d9bf45e1429be6c360dfa71dc))
* add UAT report for known-after-apply rendering ([8ac51d3](https://github.com/oocx/tfplan2md/commit/8ac51d372076b9f476a236c4bb868507ee9618cd))
* regenerate demo artifacts and examples ([6a5b748](https://github.com/oocx/tfplan2md/commit/6a5b7485d45d96ce473959fbf9461e7bf8b7ea4a))
* renumber issues 573→100, 574→101, feature 575→102 ([86520fe](https://github.com/oocx/tfplan2md/commit/86520fe8fef5cec2b667252a55ec6e63c5a102be))
* resolve OQ-01 - whole-resource-unknown shows note instead of placeholder ([2cf1982](https://github.com/oocx/tfplan2md/commit/2cf1982f4a4aa0c933a94042d34743f81de36355))
* subagent commit/push research report and skill corrections ([2847df0](https://github.com/oocx/tfplan2md/commit/2847df08419a9c59a39a291993464f853a86c406))
* update issue references from [#573](https://github.com/oocx/tfplan2md/issues/573) to [#100](https://github.com/oocx/tfplan2md/issues/100) in work protocol and code review documents ([eb494b5](https://github.com/oocx/tfplan2md/commit/eb494b54744109c1f91bedb06336f641daf3b557))
* update work protocol with UAT Tester entry for feature 102 ([6aa7b25](https://github.com/oocx/tfplan2md/commit/6aa7b25e61853050168fb7c48976421750e1e0c8))

<a name="1.29.0"></a>
## [1.29.0](https://github.com/oocx/tfplan2md/compare/v1.28.0...v1.29.0) (2026-02-24)

### ✨ Features

* show subscription name without key icon in role assignment summary when mapped ([b5043ae](https://github.com/oocx/tfplan2md/commit/b5043ae377ad5f63ce713cefd1061f2eb063c086))

### 🐛 Bug Fixes

* address code review issues for GetSubscriptionName ([61a188e](https://github.com/oocx/tfplan2md/commit/61a188eb2523187e4a7844fa1c4e44b8c7933922))
* include key icon with subscription name in role assignment summary ([b9327f8](https://github.com/oocx/tfplan2md/commit/b9327f8b6890eb7de0b612b272c6348420175c09))

### 📚 Documentation

* add code review for issue 574 subscription name in role assignment summary ([867f0fa](https://github.com/oocx/tfplan2md/commit/867f0fac65536f95f929ca9faba67926e1ade48f))
* add issue analysis for subscription name in role assignment summary ([561baa3](https://github.com/oocx/tfplan2md/commit/561baa35f743d2c077514ce94c7bc7360793020f))
* add release notes for subscription name in role assignment summary fix ([998faf9](https://github.com/oocx/tfplan2md/commit/998faf90d67c5675c5755338024dc5e706fa4622))
* fix expected output in analysis.md to include 🔑 icon before subscription name ([6e0890a](https://github.com/oocx/tfplan2md/commit/6e0890afb99cb88a193f1449581556748f09cc36))

<a name="1.28.0"></a>
## [1.28.0](https://github.com/oocx/tfplan2md/compare/v1.27.0...v1.28.0) (2026-02-23)

### ✨ Features

* add support for 'open' action and ['create', 'forget'] replace variant ([a67a4e7](https://github.com/oocx/tfplan2md/commit/a67a4e7cf20ac3b7c9ceff364a78be2f917d108c))

### 📚 Documentation

* add code review report for open action support ([07e3392](https://github.com/oocx/tfplan2md/commit/07e3392e4fc73a9605c9ae0c91b9c0a2e439cd88))
* add issue analysis for OpenTofu open action support ([030a2f7](https://github.com/oocx/tfplan2md/commit/030a2f78d50049c04fc337509381f65f301055e2))
* add release notes for ephemeral resource 'open' action support ([41353b5](https://github.com/oocx/tfplan2md/commit/41353b5388f9826f4bc2e8a32125074cff4495d2))
* add retrospective analysis for issue [#573](https://github.com/oocx/tfplan2md/issues/573) (open action support) ([6cd7087](https://github.com/oocx/tfplan2md/commit/6cd7087a341dd27a8f080c82c488004c7ef1eb54))
* complete work protocol with all required agent entries ([d0077a0](https://github.com/oocx/tfplan2md/commit/d0077a0857223860541883f57a911509b07fe279))
* update release notes with commit hash and add Release Manager verification ([4d9b515](https://github.com/oocx/tfplan2md/commit/4d9b515617dc193f18a60cc391ad2ce3c60b8311))

<a name="1.27.0"></a>
## [1.27.0](https://github.com/oocx/tfplan2md/compare/v1.26.2...v1.27.0) (2026-02-23)

### ✨ Features

* add Alpine musl binaries (linux-musl-x64 and linux-musl-arm64) to release ([162a202](https://github.com/oocx/tfplan2md/commit/162a20294bf9393b7df0ceadd807b640a0c51e12))
* add change type column to outputs table (097) SNAPSHOT_UPDATE_OK ([1b93920](https://github.com/oocx/tfplan2md/commit/1b93920f7d4c075fe940b2eb5745f61021086b19))
* add rendering layer for Terraform outputs ([f03cc6a](https://github.com/oocx/tfplan2md/commit/f03cc6af9650d448e78c141afcfecde419904b43))
* add Terraform outputs parsing and model building ([8cd2aa7](https://github.com/oocx/tfplan2md/commit/8cd2aa7729e13d46bf94ad58eac4b815f740a472))

### 🐛 Bug Fixes

* add expression.references+azurerm resources to uat-plan.json, add principal mapping, regenerate uat-plan.md (097) ([aa2dbc5](https://github.com/oocx/tfplan2md/commit/aa2dbc534c9a83045d43baa5502cd2fdbba15550))
* always parse Azure resource IDs in output values regardless of provider (097) SNAPSHOT_UPDATE_OK ([917b8eb](https://github.com/oocx/tfplan2md/commit/917b8ebb0b18da4c38784a1e57e2efcaf98fcdb3))
* expand PrincipalIdFormatter MatchPattern to include user_principal_id and object_id (097) SNAPSHOT_UPDATE_OK ([3395e72](https://github.com/oocx/tfplan2md/commit/3395e729c86028b5c0fff4f8337e3a3241bce36a))
* fix subscription_id icon, add user_principal_id with correct reference, regenerate uat-plan.md and demo artifacts (097) ([007a2d8](https://github.com/oocx/tfplan2md/commit/007a2d89b8647a0dddddc3d118395f80c16999be))
* parse trimmed value in TryCompactJsonString (code review) (097) ([fb8d850](https://github.com/oocx/tfplan2md/commit/fb8d850bd44db3daebd64be0c0deb7e4f6c07529))
* properly resolve output value formatting - provider-aware icons, JSON string compaction, principal mapping (097) SNAPSHOT_UPDATE_OK ([f2f7994](https://github.com/oocx/tfplan2md/commit/f2f79949ba46044cfc4af5e8ece4b82c68e65d99))
* regenerate output snapshots with Change column (097) SNAPSHOT_UPDATE_OK ([e937989](https://github.com/oocx/tfplan2md/commit/e93798923c50bcbbe61457649c9ec0d6d0e9bdd8))
* remove incorrect 👤 fallback icon rule for principal_id/tenant_id UUIDs; update snapshots (097) SNAPSHOT_UPDATE_OK ([8e59ca0](https://github.com/oocx/tfplan2md/commit/8e59ca0ef2fb2df092e80fbfd6be454d3b6cde0c))
* rename test method to UsesLaptopIcon per code review (097) ([8b2d9ab](https://github.com/oocx/tfplan2md/commit/8b2d9ab9801c807314753b0912a432cde0bf2f1b))
* restore accidentally deleted nsg-with-separate-rule-updates.md snapshot (097) ([e0bfda0](https://github.com/oocx/tfplan2md/commit/e0bfda0a5000fb9d1ce055d7716eea463d69060b))
* use 💻 for service principals per style guide; add type-aware icon tests; expand snapshot coverage (097) SNAPSHOT_UPDATE_OK ([418634d](https://github.com/oocx/tfplan2md/commit/418634d3c08769957d64ab34ab1fb8727180c9b6))
* use build-binaries matrix for musl builds (load+push incompatible in buildx) ([d3b3ba5](https://github.com/oocx/tfplan2md/commit/d3b3ba5ccd6b38e4240881fd934e04c334643960))
* use docker run for musl builds instead of Alpine job containers ([6c84dc4](https://github.com/oocx/tfplan2md/commit/6c84dc4f3caaf462dfac5f00abe63e8ced0980f5))
* use provider from expression references and semantic formatting for output values (097) SNAPSHOT_UPDATE_OK ([f61cc8e](https://github.com/oocx/tfplan2md/commit/f61cc8ece938c9fcc8dc291ff8189b999cc6d29c))
* use referenced attribute name for output value formatting; fix Scriban OR bug via formatting_attribute_name (097) SNAPSHOT_UPDATE_OK ([3e9bd62](https://github.com/oocx/tfplan2md/commit/3e9bd620d29ebec16156aa63047a32e4588aac44))
* use runtime-deps:10.0-alpine for musl smoke test (no apk needed) ([f75e822](https://github.com/oocx/tfplan2md/commit/f75e8229d624b41058baa033fd6b0e01638dd588))
* **outputs:** fix principal_id icon, large JSON rendering, and stub resource attributes ([7a85c66](https://github.com/oocx/tfplan2md/commit/7a85c6685ab6131a2f3178285221bd08870f12b5))

### 📚 Documentation

* add architecture design for Terraform outputs support ([a159f90](https://github.com/oocx/tfplan2md/commit/a159f904c70e396e6219e242e17ace10a661362f))
* add feature specification for terraform outputs (097) ([fbcdb17](https://github.com/oocx/tfplan2md/commit/fbcdb170366ce36e898728feb7acb0363f19fee8))
* add implementation tasks for terraform outputs feature (097) ([f5a8da0](https://github.com/oocx/tfplan2md/commit/f5a8da04dc9d7037234ea70e5585362512b1e71f))
* add Release Manager entry to work protocol (097) ([5e26414](https://github.com/oocx/tfplan2md/commit/5e2641442dcc27fad4b0cf02a489257cbd727c52))
* add release notes and features.md entry for terraform outputs (097) ([939f196](https://github.com/oocx/tfplan2md/commit/939f1964859ee0876936a43f34dfd6a926970ef9))
* add test plan and UAT plan for terraform outputs feature ([7da9c09](https://github.com/oocx/tfplan2md/commit/7da9c09f8367a2d3933b7bdaefd0eced8b08c0c9))
* add UAT Tester work log entry for feature 097 ([d7cb764](https://github.com/oocx/tfplan2md/commit/d7cb764260d87d092e59c3610f208a8c070e979e))
* fix typos in release notes examples for terraform outputs (097) ([b61e864](https://github.com/oocx/tfplan2md/commit/b61e8641f6e6a40b639765808128f82ac26b626f))
* update work protocol with Developer progress ([ac667de](https://github.com/oocx/tfplan2md/commit/ac667decf781f9b5439efe7269b8d3b990c2681e))
* update work protocol with rendering layer progress ([27c236b](https://github.com/oocx/tfplan2md/commit/27c236b8fec00b6a728be7890bed175a9ef2660a))

<a name="1.26.2"></a>
## [1.26.2](https://github.com/oocx/tfplan2md/compare/v1.26.1...v1.26.2) (2026-02-22)

### 🐛 Bug Fixes

* address issue 099 code review findings ([92afb4f](https://github.com/oocx/tfplan2md/commit/92afb4fc617d968d35b5d86a1642bfbc4e30cad4))
* close issue 099 remaining security findings ([6af028c](https://github.com/oocx/tfplan2md/commit/6af028c66cb770a9fbee0f4370a8249fe824a426))
* resolve remaining security findings and enhance report accuracy ([6c8ee7f](https://github.com/oocx/tfplan2md/commit/6c8ee7fe13840011bfc0cbc14d5026a4c5c82a24))
* update documentation for new action types and error handling in security findings ([75f4fc4](https://github.com/oocx/tfplan2md/commit/75f4fc424dcc5b10a61c094c40f6be7ea926d620))
* update generated report version and format links for security findings ([4785064](https://github.com/oocx/tfplan2md/commit/4785064b523017764eefa86fd0a25c9e7072e660))

### 📚 Documentation

* add code review for fix/099-remaining-security-findings ([1c5a17b](https://github.com/oocx/tfplan2md/commit/1c5a17b73e6bcd13b57db90cd602102c6a61d088))
* add issue analysis for remaining security findings ([d6a100f](https://github.com/oocx/tfplan2md/commit/d6a100f3b008f67d6aa4f00703dd61968b63ec63))
* add release notes and restore work protocol for issue 099 ([21de94d](https://github.com/oocx/tfplan2md/commit/21de94dbf589a23effe21df6586e9808878f5b5a))
* add UAT report for issue-099 remaining security findings ([ddc9d61](https://github.com/oocx/tfplan2md/commit/ddc9d6199f23a9596e2b6e9b80b7934484fb3bc1))
* fix screenshot URLs in release notes (v1.27.0 -> v1.26.1) ([399b4a4](https://github.com/oocx/tfplan2md/commit/399b4a4c30b404395fdd6053e8dd4249a186fd45))
* update code review — approved after rework (round 2) ([b2a3157](https://github.com/oocx/tfplan2md/commit/b2a3157cef48811827545027fb6c930de4635348))
* update code review with Docker build result ([345f184](https://github.com/oocx/tfplan2md/commit/345f1840a55ff1173468105503754f5663fdf1cd))

<a name="1.26.1"></a>
## [1.26.1](https://github.com/oocx/tfplan2md/compare/v1.26.0...v1.26.1) (2026-02-22)

### 🐛 Bug Fixes

* add Subresource Integrity (SRI) to HTML templates ([8836217](https://github.com/oocx/tfplan2md/commit/8836217694158257f4c489780e95adc392285023))
* add UAT plan artifacts for issue 098 sensitive info exposure ([2af4c79](https://github.com/oocx/tfplan2md/commit/2af4c79b3023ef648275785764db62a970f7c205))
* deduplicate GetHierarchicalPaths for multi-level indexed keys ([e4fea50](https://github.com/oocx/tfplan2md/commit/e4fea50a0280e7ab786939123aa19714aeaa3a0c))
* handle root boolean and top-level array parent sensitivity ([5c94218](https://github.com/oocx/tfplan2md/commit/5c942183b3dfc8faae1908857c3c2632a466e000))
* handle ScriptArray per-element sensitivity in MaskSensitiveLeaves ([27634a3](https://github.com/oocx/tfplan2md/commit/27634a364645542bc1145563b92864feb800bb74))
* mask before_json/after_json at mapper level + fix AzApi comparison ([a422c87](https://github.com/oocx/tfplan2md/commit/a422c87e94bc21ac7af93734c9a38f20df6396db))
* mask sensitive values in AzApi create/delete/replace body rendering ([02aa2fb](https://github.com/oocx/tfplan2md/commit/02aa2fb16f30ecdc5266077f9ca4efe5be006135))
* mask sensitive values in AzApi update body rendering ([bec087f](https://github.com/oocx/tfplan2md/commit/bec087ff49beae399cc76140dad94944afcf3e3f))
* mask Variable Group values when either side is secret ([9c2f4d6](https://github.com/oocx/tfplan2md/commit/9c2f4d65f4e23e65da281f279af4741ac951ca67))
* remove unused SecretValue property from BuildDefinitionVariableValues ([0b1c424](https://github.com/oocx/tfplan2md/commit/0b1c424b9271da54f2255738a6a8883a39fc01c5))
* update generation timestamp in comprehensive-demo.md and change ADR-009 status to accepted ([156c2af](https://github.com/oocx/tfplan2md/commit/156c2affca27a50f72578e16599245ae39bd3920))

### 📚 Documentation

* add ADR for template JSON masking ([4f9c423](https://github.com/oocx/tfplan2md/commit/4f9c42360b5d9a883e2140a99294ada4a044e7a9))
* add code review for 098-sensitive-info-exposure ([76a444b](https://github.com/oocx/tfplan2md/commit/76a444b3515a425d0fd976461fa5914543ecfaf3))
* add code review report for SRI security fix ([673bbac](https://github.com/oocx/tfplan2md/commit/673bbac6be2c2fb81a5e7293c33befd601d480d9))
* add code review round 2 for 098-sensitive-info-exposure ([fe8c64f](https://github.com/oocx/tfplan2md/commit/fe8c64ff9d6f761fa327449138e18c346946b02f))
* add Developer work protocol entry for issue 098 ([fa07cee](https://github.com/oocx/tfplan2md/commit/fa07cee58512efe6a99db0a24684c3fe4c782ba8))
* add internal release notes stub for 097-security-analysis ([b1f9805](https://github.com/oocx/tfplan2md/commit/b1f98050742dab6246b67ea1f5df68c3684cd692))
* add Release Manager work log entry for 098-sensitive-info-exposure ([cae6562](https://github.com/oocx/tfplan2md/commit/cae65629010cdae24ba2f3b2fada1e4314a83c9f))
* add release notes and work protocol for SRI security fix (issue 096) ([1cfdfee](https://github.com/oocx/tfplan2md/commit/1cfdfeea7dab768dc8e636c8a031c1c4f5cdb419))
* add release notes for fix/098-sensitive-info-exposure SNAPSHOT_UPDATE_OK ([7dd39ff](https://github.com/oocx/tfplan2md/commit/7dd39ff06f78563c551591d90138d4567e372dde))
* add release screenshots for 098-sensitive-info-exposure ([4b3288e](https://github.com/oocx/tfplan2md/commit/4b3288e8e82d6e4f35eb3d874d0ce11ff3d1da49))
* add tasks for 098-sensitive-info-exposure ([bb8d343](https://github.com/oocx/tfplan2md/commit/bb8d34355d80e684e87b31f38d83b2e351975f8a))
* add test plan and UAT test plan for 098-sensitive-info-exposure ([0c103a8](https://github.com/oocx/tfplan2md/commit/0c103a8920c88063fa0348687421399b5c523c9b))
* add UAT report for 098-sensitive-info-exposure ([f113020](https://github.com/oocx/tfplan2md/commit/f1130206d302f9176a3d9bccc999ee420d255f69))
* analyze sensitive information exposure ([5e7d153](https://github.com/oocx/tfplan2md/commit/5e7d1531d79d5e772a92f09054ddbb6664d6a315))
* approve code review round 2 for 098-sensitive-info-exposure ([258e24e](https://github.com/oocx/tfplan2md/commit/258e24e2b48f177930f029211fd7c1268b84ba9f))
* mark Task 8 as complete ([3387d5a](https://github.com/oocx/tfplan2md/commit/3387d5a3af1ec4aff75aa41814fedb97ad3beef9))
* mark Task 9 as complete ([f71ea0d](https://github.com/oocx/tfplan2md/commit/f71ea0d7fe519285fd641385314ad107e665532e))
* mark tasks 1-6 as complete ([f5a814f](https://github.com/oocx/tfplan2md/commit/f5a814fa1104ade3e4a1261fd20f8ae3d70f27f0))
* mark Tasks 9, 10, 11 as complete ([fafdbc4](https://github.com/oocx/tfplan2md/commit/fafdbc47ccf7385553703a0563babccb604cfc79))
* regenerate demo artifacts with sensitive value masking ([4616732](https://github.com/oocx/tfplan2md/commit/4616732856d159bb45bf4da264bbdbbf8ec8729d))
* update features.md with sensitive value masking coverage ([e1ad015](https://github.com/oocx/tfplan2md/commit/e1ad01512a259c6837086ffdf9855def2f794440))
* update work protocol with Developer rework log entry ([6f54227](https://github.com/oocx/tfplan2md/commit/6f54227a6d57776653573a9f707c7201e2d3af55))

<a name="1.26.0"></a>
## [1.26.0](https://github.com/oocx/tfplan2md/compare/v1.25.0...v1.26.0) (2026-02-21)

### ✨ Features

* add Azure DevOps repository mapping and branch/repo icons ([bc804b5](https://github.com/oocx/tfplan2md/commit/bc804b5efcd62402c2d1edb99978f009e1b3f5c3))
* add azuredevops-feature-096 artifact showing 🗃️/⎇ icons for UAT ([2fe8253](https://github.com/oocx/tfplan2md/commit/2fe8253a9f8080d333a1b0490b382084eb6274c3))
* apply 🗃️/⎇ icons in build definition repository table and add release screenshot ([c9937b1](https://github.com/oocx/tfplan2md/commit/c9937b1f83b880f85155bc1ae8f41012d5e3929d))
* Azure DevOps repository mapping and 🗃️/⎇ semantic icons (feature 096) ([95c93f1](https://github.com/oocx/tfplan2md/commit/95c93f1da43364c3f00ce439a7cec6311b01ab28))

### 🐛 Bug Fixes

* regenerate release screenshot with azdoRepositories mapping applied ([cb97549](https://github.com/oocx/tfplan2md/commit/cb97549fbab209594575612eda519c6725a91f4e))
* wire AzdoRepositoryMapper into BuildDefinitionFactory so repo IDs resolve to display names ([51e69d1](https://github.com/oocx/tfplan2md/commit/51e69d12bc99c2097c889aca518128dc9406b366))

### ♻️ Refactoring

* rename feature 095 to 096 (conflict with parallel feature branch) ([6c5bafa](https://github.com/oocx/tfplan2md/commit/6c5bafa1d2048c69d991875946b8c3a518af44c6))

### 📚 Documentation

* add architecture for Azure DevOps repository mapping and branch/repo icons ([94aa708](https://github.com/oocx/tfplan2md/commit/94aa708a906eecc5f4cecd2ee6dbfac205246189))
* add code review for feature 095 - azure devops repository mapping and icons ([53046ad](https://github.com/oocx/tfplan2md/commit/53046add2dbb5964aadbfe82f9b4fb636571b5e7))
* add documentation for Feature 095 Azure DevOps repository mapping and icons ([78b6e74](https://github.com/oocx/tfplan2md/commit/78b6e74619778e95b846f4d1ec5533dcd4618b1c))
* add feature specification for 095-azdo-repo-mapping-and-icons ([292636d](https://github.com/oocx/tfplan2md/commit/292636d07a94fda79dc1fbce7a5e71d9fccfdd12))
* add task plan for feature 095-azdo-repo-mapping-and-icons ([a3c8ac7](https://github.com/oocx/tfplan2md/commit/a3c8ac7866b80f3d8d2f3f84b726c19e328fefef))
* add test plan for feature 095 - azdo repository mapping and icons ([6c22c26](https://github.com/oocx/tfplan2md/commit/6c22c26bfcb8c74fea8e903d61a0c306a55d70be))
* update work protocol with UAT re-run for feature 096 ([9a2ae3a](https://github.com/oocx/tfplan2md/commit/9a2ae3a74a621f63de50f5290b44369a0d720e9c))

<a name="1.25.0"></a>
## [1.25.0](https://github.com/oocx/tfplan2md/compare/v1.24.0...v1.25.0) (2026-02-20)

### ✨ Features

* implement azapi_update_resource template with attribute grouping ([5c1daf4](https://github.com/oocx/tfplan2md/commit/5c1daf49be18c7b454288b77ff63b89d6efa7c62))

### 📚 Documentation

* add code review report for Feature 095 ([22c4e0c](https://github.com/oocx/tfplan2md/commit/22c4e0c86dfefffe7fe70503311190aa4ec12731))
* add code review report for Feature 095 ([aa270f4](https://github.com/oocx/tfplan2md/commit/aa270f4c37f869b4516199767020f9b0fe125c03))
* add documentation for Feature 095 azapi_update_resource grouping ([745ec8a](https://github.com/oocx/tfplan2md/commit/745ec8a0cc6d53eb7c1b819232cef02780c2f0d7))
* add feature 095 specification for azapi_update_resource grouping ([865b732](https://github.com/oocx/tfplan2md/commit/865b7324113157ff06b234858cc3b882ffd47e9b))
* add screenshot to feature 095 release notes ([1523e15](https://github.com/oocx/tfplan2md/commit/1523e1544057701cbc57037c24e90807b72febd8))
* update generate-release-screenshots skill with --details open instruction ([31a656b](https://github.com/oocx/tfplan2md/commit/31a656beaf2bcbdf96d112e7601d87d66899f74b))

<a name="1.24.0"></a>
## [1.24.0](https://github.com/oocx/tfplan2md/compare/v1.23.1...v1.24.0) (2026-02-20)

### ✨ Features

* add semantic icons for boolean and name values in azuredevops_build_definition tables ([fd4685d](https://github.com/oocx/tfplan2md/commit/fd4685d3fa420ef208c0b574ad13ffe21dcb9aeb))
* implement build definition view models, extractors, formatters, and template ([1899878](https://github.com/oocx/tfplan2md/commit/18998785a7eacfa5798f89d57b331e63f734133d))

### 🐛 Bug Fixes

* split build_definition.sbn into partial templates to pass line count test ([d89c818](https://github.com/oocx/tfplan2md/commit/d89c81835ae65196ce9285df163278bfa25c4766))
* update test assertions to match formatter output with semantic icons ([b20821e](https://github.com/oocx/tfplan2md/commit/b20821ea0b2e13ceefa127070dc4833a88b942a3))

### 📚 Documentation

* add architecture for build definition tables ([f853963](https://github.com/oocx/tfplan2md/commit/f8539634eb48e79b7042a5063a6a19746bcd9a84))
* add code review for feature 094 build definition tables ([27367c8](https://github.com/oocx/tfplan2md/commit/27367c80201a62d38823c6d1a8edec121e3da5c1))
* add feature specification for build definition tables (094) ([a259d6b](https://github.com/oocx/tfplan2md/commit/a259d6b9b6b55dbf25b88355369d84a16914a2af))
* Add past advisories section to SECURITY.md ([18643e2](https://github.com/oocx/tfplan2md/commit/18643e29ee79bcacb56a4ee88cf9ef2ce11ef545))
* add release screenshot and update release notes for feature 094 ([e0eb9c6](https://github.com/oocx/tfplan2md/commit/e0eb9c62e0d3b6c9190dc124806e7a34d2c6354f))
* add release screenshot and update release notes for feature 094 ([fd4e529](https://github.com/oocx/tfplan2md/commit/fd4e5297e0de62af2b10c7f1fc99437d5a961d96))
* add retrospective for feature 094 build definition tables ([3e487ba](https://github.com/oocx/tfplan2md/commit/3e487bafea1e0ef8f541479a7181196d9c8d1f85))
* add tasks for feature 094 build definition tables ([d04499d](https://github.com/oocx/tfplan2md/commit/d04499da7b35f5024d0c6d95a16b63f7193e8937))
* add test plan and UAT test plan for feature 094 ([bc02ad6](https://github.com/oocx/tfplan2md/commit/bc02ad6bffe77782d89c0d3b021b0ff980556f67))
* add UAT report for feature 094 - all validations passed ([5c7dbae](https://github.com/oocx/tfplan2md/commit/5c7dbae918263bc251fa263a4d38c1ac538db846))
* Fix typo in 'Past Advisoties' section header ([dde0a04](https://github.com/oocx/tfplan2md/commit/dde0a0461c1073e46fd112186e9b0901d6d7e21c))
* mark tasks 11-15 as complete ([793fff6](https://github.com/oocx/tfplan2md/commit/793fff6375963ddc44e8be156850de5242ee3972))
* update documentation for build definition tables feature ([01f60dd](https://github.com/oocx/tfplan2md/commit/01f60dde75d607cdddb8ffd2a26900dbe6b132bf))
* update release notes with correct commits and work protocol entry for release manager session 2 ([dbe031b](https://github.com/oocx/tfplan2md/commit/dbe031bb47fe9eaa558b4856b5ef5fffe185042d))
* update UAT report with final PASS result for feature 094 ([317bc62](https://github.com/oocx/tfplan2md/commit/317bc62eb9a762fa9f426bd9ec776c985fe451e2))
* update work protocol with Developer completion entry ([42ec806](https://github.com/oocx/tfplan2md/commit/42ec806283f285c3eb3f3d524d9213e2908a0d47))
* **uat:** re-run UAT for feature 094 with regenerated artifacts ([1727a0d](https://github.com/oocx/tfplan2md/commit/1727a0da1cf8108d4c3f2af08c63b3013960d356))

<a name="1.23.1"></a>
## [1.23.1](https://github.com/oocx/tfplan2md/compare/v1.23.0...v1.23.1) (2026-02-20)

### 🐛 Bug Fixes

* prevent sensitive data disclosure for array/nested attributes ([7491896](https://github.com/oocx/tfplan2md/commit/749189657ef3a31dabf3560e668bfaa3fb9374d8))

### 📚 Documentation

* add code review for sensitive attribute disclosure security fix ([af106e7](https://github.com/oocx/tfplan2md/commit/af106e7b92a896c1f10817fa0c0922f912a25235))
* add code review report for sensitive attribute disclosure fix ([97b0361](https://github.com/oocx/tfplan2md/commit/97b0361e63809ffc65759b7414037bfc57f06c2e))
* add issue analysis for sensitive attribute disclosure vulnerability ([5530ae4](https://github.com/oocx/tfplan2md/commit/5530ae41a8672d640007d07a35d888a69d8e559a))
* add release manager entry to work protocol for issue 093 ([59d1659](https://github.com/oocx/tfplan2md/commit/59d16596de4109b89c26eb0a0c0d425677cfae94))
* add release notes for sensitive attribute disclosure fix (issue 093) ([6c30486](https://github.com/oocx/tfplan2md/commit/6c3048639542b06ab6a1440e09e49d399a47a674))
* update work protocol for sensitive attribute fix ([a2222d8](https://github.com/oocx/tfplan2md/commit/a2222d83ba545fff189e3dfabe247ad14cc4c8ea))

<a name="1.23.0"></a>
## [1.23.0](https://github.com/oocx/tfplan2md/compare/v1.22.1...v1.23.0) (2026-02-19)

### ✨ Features

* add --details CLI argument for resource details display control ([a14b8c1](https://github.com/oocx/tfplan2md/commit/a14b8c1a24ce1bce5f6e6e2ca38e653d14d2a2a3))

### 🐛 Bug Fixes

* resource-specific template support for --details mode ([3cab10e](https://github.com/oocx/tfplan2md/commit/3cab10ed501e629d020ae34cb7c94d38f2af0228))
* update all provider-specific templates to use details_open_attr helper; add architecture and integration tests (SNAPSHOT_UPDATE_OK) ([54d1c29](https://github.com/oocx/tfplan2md/commit/54d1c29708b174fcd25f9edb66801c336cfdd959))

### 📚 Documentation

* add architecture for details display mode CLI feature ([3b321c7](https://github.com/oocx/tfplan2md/commit/3b321c73a66c54938db609aae68ee3b166e6f07e))
* add comprehensive test plan for details display mode feature ([9e68ae3](https://github.com/oocx/tfplan2md/commit/9e68ae3771dd6f6083eba228aefc89e9de4cad85))
* add documentation for --details CLI option ([9ff9a5f](https://github.com/oocx/tfplan2md/commit/9ff9a5f4ada0ca4d83d90aaa7c1a3dd77d1b53bd))
* add feature specification for 092-details-display-mode ([03945d3](https://github.com/oocx/tfplan2md/commit/03945d3b69a0e8391b49140f3d52a7bd36f8544b))
* add implementation tasks for 092-details-display-mode ([464b8d2](https://github.com/oocx/tfplan2md/commit/464b8d23ca4571ca013bf1f359c7776a225e67b7))
* add retrospective analysis for feature 092 (--details CLI option) ([cda6ac7](https://github.com/oocx/tfplan2md/commit/cda6ac7d40fcb08a96330b9f389da178940fba6b))
* update work protocol with Release Manager entry for feature 092 ([5db0a0b](https://github.com/oocx/tfplan2md/commit/5db0a0b10c3052007bdc70dddfddd1f0274b2b86))

<a name="1.22.1"></a>
## [1.22.1](https://github.com/oocx/tfplan2md/compare/v1.22.0...v1.22.1) (2026-02-19)

### 🐛 Bug Fixes

* homebrew formula update fails when formula file missing in tap repo ([a24dc4f](https://github.com/oocx/tfplan2md/commit/a24dc4fdbe8c17639b9d07215645ef367311b850))
* resources only expanded by default when code analysis warnings exist ([71709bf](https://github.com/oocx/tfplan2md/commit/71709bfd73104fa6d782e41e1d0ea1fcbfa957ea))
* **release:** fix linux-arm64 cross-compilation and macOS sha256sum failures ([f61e284](https://github.com/oocx/tfplan2md/commit/f61e284eb16f60e1d5600188fe4481ad95b7960d))

### 📚 Documentation

* add Release Manager entry to work protocol ([8588ca5](https://github.com/oocx/tfplan2md/commit/8588ca5a99acd3ba03e778ed9b898ec93d61ce92))
* add release notes for issue 091 - AzAPI resources expansion fix ([ded25f3](https://github.com/oocx/tfplan2md/commit/ded25f39e1e82c9e0d6eee9f42a35267a3e6db5a))

<a name="1.22.0"></a>
## [1.22.0](https://github.com/oocx/tfplan2md/compare/v1.21.1...v1.22.0) (2026-02-19)

### ✨ Features

* add Homebrew formula update job to release workflow ([843ab42](https://github.com/oocx/tfplan2md/commit/843ab422f7ad20fc22faec202118c5a56ef8d8b6))
* add Homebrew formula update script ([0136c61](https://github.com/oocx/tfplan2md/commit/0136c61a21ee30ab8e2f3fb32bc74daa276f630d))

### 🐛 Bug Fixes

* add Xcode CLT for macOS builds and remove windows-arm64 ([7ebde9a](https://github.com/oocx/tfplan2md/commit/7ebde9a3f5598cb75fb239e975139a608584a679))
* array diff rendering - skip markers when property didn't exist, add colored spans ([1b28aa0](https://github.com/oocx/tfplan2md/commit/1b28aa0a1f0b60f22d8c7186ceb40a6659512ca7))
* array diff rendering for new elements and Azure DevOps coloring ([7a3cd8f](https://github.com/oocx/tfplan2md/commit/7a3cd8f5a648cdd80f2ba31563132b80d3a9767b))
* filter array items to show only changed items in update mode ([2daf2aa](https://github.com/oocx/tfplan2md/commit/2daf2aab3b5a158652a9eb78e8f01a6547e8f64a))
* improve array diff rendering for new/removed elements and Azure DevOps coloring ([d92a224](https://github.com/oocx/tfplan2md/commit/d92a22432cf5b1abd5e4391213b2c405dd8c3e71))
* prevent empty metadata tables in AzAPI resource template ([c02df89](https://github.com/oocx/tfplan2md/commit/c02df89ee639fe1daf3d13ae47e7f2e8d84d030d))

### 📚 Documentation

* add architecture for Homebrew installation support ([817a1e2](https://github.com/oocx/tfplan2md/commit/817a1e2c7eac8fc895a9ca25b824428a6eac9356))
* add code review approval for nested array fix (issue [#089](https://github.com/oocx/tfplan2md/issues/089)) ([98aed09](https://github.com/oocx/tfplan2md/commit/98aed09b78e78ba7ab3c0f7c8cfb2ad86678c3f3))
* add code review report for Feature 089 (Homebrew installation) ([ad930d3](https://github.com/oocx/tfplan2md/commit/ad930d38da083cfd02e0659ef7ba9c296c117f68))
* add Developer work log entry to work protocol ([4fb2bd4](https://github.com/oocx/tfplan2md/commit/4fb2bd45ea7806b5f9769c1bb2917770ca8b280d))
* add feature specification for Homebrew installation support ([6e2b3e4](https://github.com/oocx/tfplan2md/commit/6e2b3e4b54cf0d6a0658cf1fe1a51049e6c4f77c))
* add Homebrew installation documentation ([177e878](https://github.com/oocx/tfplan2md/commit/177e8785276d9581254cd2a2370408f596acda56))
* add implementation summary for Feature 089 ([4e8aacf](https://github.com/oocx/tfplan2md/commit/4e8aacf915ee4d18091f763024b8f405725d4dea))
* add implementation tasks for homebrew installation (089) ([95ed54a](https://github.com/oocx/tfplan2md/commit/95ed54ae663be14cbc5673fb1bda62f1a87a6117))
* add issue analysis for nested array rendering showing all items instead of only changed items ([ab9f4fa](https://github.com/oocx/tfplan2md/commit/ab9f4faac17d608d3401860946f5751d08c15fc9))
* add Release Manager work log and merge checklist for Feature 089 ([b83e8ca](https://github.com/oocx/tfplan2md/commit/b83e8ca15f68ffe8a4660927a44c64dccf42d62d))
* add Release Manager work log to work protocol (issue [#089](https://github.com/oocx/tfplan2md/issues/089)) ([d7807d4](https://github.com/oocx/tfplan2md/commit/d7807d40c4be4df55ebd6405b62525fcd23d43a1))
* add release notes and update feature docs for nested array fix ([ad4a962](https://github.com/oocx/tfplan2md/commit/ad4a9624ae5b1bb018d85031f558d5f679a1133b))
* add retrospective analysis for Feature 089 (Homebrew installation) ([0dd32d6](https://github.com/oocx/tfplan2md/commit/0dd32d660206b0c9d967725e67af7ef3f8a9ddf9))
* add test plan for homebrew installation support (089) ([364029d](https://github.com/oocx/tfplan2md/commit/364029d1d108685100d7a245498d0018434dbe11))
* add UAT report documenting authentication blocker (issue [#089](https://github.com/oocx/tfplan2md/issues/089)) ([b2b2e78](https://github.com/oocx/tfplan2md/commit/b2b2e78367d7e38fadb089a5bba331222279fd2d))
* improve UAT agent instructions and rename issue 089 to 090 ([a25656f](https://github.com/oocx/tfplan2md/commit/a25656fbff4e7d982936bd143db5824e1e4e374b))
* mark TASK-001, 002, 003, 007, 008 as complete ([ac82c48](https://github.com/oocx/tfplan2md/commit/ac82c481119234279551b1dc723066b209c77035))
* update release notes to include both array rendering fixes ([2cedb5d](https://github.com/oocx/tfplan2md/commit/2cedb5d7acd428e46b451e8860f077ccbbffc2a9))
* update UAT report with successful PR creation (issue [#089](https://github.com/oocx/tfplan2md/issues/089)) ([6b4ed6a](https://github.com/oocx/tfplan2md/commit/6b4ed6a8b799b97d5adba910720a48e9050cdbe9))

<a name="1.21.1"></a>
## [1.21.1](https://github.com/oocx/tfplan2md/compare/v1.21.0...v1.21.1) (2026-02-18)

### 🐛 Bug Fixes

* preserve no-op parents with child changes in Resource Changes section ([f395bd6](https://github.com/oocx/tfplan2md/commit/f395bd6f9aac56b685daec57123271685c5528f7))
* refine no-op parent filter to check for child changes ([5860386](https://github.com/oocx/tfplan2md/commit/5860386120f2f6678ad526102de604e9b48b7210))

### 📚 Documentation

* add code review report for no-op parent bug fix (issue [#088](https://github.com/oocx/tfplan2md/issues/088)) ([5cee65e](https://github.com/oocx/tfplan2md/commit/5cee65e3db6ca0a80d5180b40fe490057cb5f1f0))
* add issue analysis for no-op parent hiding child changes ([5ce1947](https://github.com/oocx/tfplan2md/commit/5ce1947f25ed62d7babd9fc0f35cb3a51d7cc052))
* add release notes for no-op parent hiding child changes bug fix ([99eb58d](https://github.com/oocx/tfplan2md/commit/99eb58d224f3186652ed2dd1adf494bb04082c97))

<a name="1.21.0"></a>
## [1.21.0](https://github.com/oocx/tfplan2md/compare/v1.20.1...v1.21.0) (2026-02-18)

### ✨ Features

* show 'No changes' for zero-change plans ([2529909](https://github.com/oocx/tfplan2md/commit/25299098b0e85dc8ceb4ec3fcd5d8d84ce5de477))
* wrap debug section in collapsible details block ([03c6b40](https://github.com/oocx/tfplan2md/commit/03c6b40c6a06c0c699673d567029d5bfc66c1e93))

### 🐛 Bug Fixes

* correct template files with proper non-breaking spaces ([e65e64b](https://github.com/oocx/tfplan2md/commit/e65e64b3f05d68c60c50ac27a21d84725558f0b7))

### 📚 Documentation

* add architecture for output display enhancements ([0da8764](https://github.com/oocx/tfplan2md/commit/0da8764e33799bb4e3ed5480b66cded686c454da))
* add Claude Sonnet 4.6 and GPT-5.3-Codex to model reference ([73b6b49](https://github.com/oocx/tfplan2md/commit/73b6b49c559bb080e719d4740ef33fb9c886c61b))
* add feature specification for 086-output-display-enhancements ([c0a58cf](https://github.com/oocx/tfplan2md/commit/c0a58cf0095ed1a753a320e01da53c7037d835e0))
* add release notes and screenshots for Feature 086 ([f9a54bd](https://github.com/oocx/tfplan2md/commit/f9a54bd9ba86e33ac6068b4d26959c3793732f0d))
* add test plan and UAT test plan for feature 086 ([da6e5e7](https://github.com/oocx/tfplan2md/commit/da6e5e7506ac87ed71747a880889d051ca153099))
* approve Feature 086 for UAT after blocker resolution ([4d27dda](https://github.com/oocx/tfplan2md/commit/4d27ddab35ef1c7df6d1d6360ee9c280adf10714))
* complete code review for Feature 086 - changes requested ([3bebfc9](https://github.com/oocx/tfplan2md/commit/3bebfc9e081a034c9cb47f50be01cf6c4822de1e))
* mark Task 7 as complete ([1931041](https://github.com/oocx/tfplan2md/commit/19310417169151a27e6d909ab06976cd47377c1f))
* mark Tasks 1-3 as complete ([f3711ec](https://github.com/oocx/tfplan2md/commit/f3711ec488c309f6b12c0fc7762008d87818c269))
* remove incorrect proxy benchmark scores ([1106230](https://github.com/oocx/tfplan2md/commit/1106230e9e55d0cc3cf3bce896d09c95b4d08166))
* update documentation for Feature 086 (output display enhancements) ([a41ba37](https://github.com/oocx/tfplan2md/commit/a41ba37f3f6c29a8b3b72431c605c73c5fc8ec45))
* update work protocol with blocker fix session ([18ed819](https://github.com/oocx/tfplan2md/commit/18ed8191cf05c7282fd1a6d427ceafe31208e81d))
* update work protocol with implementation summary ([5bee287](https://github.com/oocx/tfplan2md/commit/5bee2872d77848761c4ffe60cd540385cf0efbcd))

<a name="1.20.1"></a>
## [1.20.1](https://github.com/oocx/tfplan2md/compare/v1.20.0...v1.20.1) (2026-02-17)

### 🐛 Bug Fixes

* keep release matrix builds running after single platform failure ([0706fe9](https://github.com/oocx/tfplan2md/commit/0706fe9e7ddb48d4e417031b476f5b55c66f3025))
* prevent decimal numbers from being rendered with IP icon ([15256e4](https://github.com/oocx/tfplan2md/commit/15256e4de4201cbf14374df1d23cfb6d55489512))
* prevent release workflow from overwriting release notes on re-run ([4ea1c87](https://github.com/oocx/tfplan2md/commit/4ea1c87d5d49c032b5c97a496d83b3c199db8727))
* run release binary publish step with bash shell ([5dce4e7](https://github.com/oocx/tfplan2md/commit/5dce4e72abb53719cad0e9f9b464d98d1a7ad8c5))

### 📚 Documentation

* add issue analysis for decimal IP icon bug (issue 087) ([5c80527](https://github.com/oocx/tfplan2md/commit/5c80527eeb0c4d8cc93ecdb52b4911d2da6d2be7))

<a name="1.20.0"></a>
## [1.20.0](https://github.com/oocx/tfplan2md/compare/v1.19.0...v1.20.0) (2026-02-17)

### ✨ Features

* add generation commands for all regenerable artifacts ([4c881b5](https://github.com/oocx/tfplan2md/commit/4c881b51f49309528e138214d1fdc6567f8e7f07))
* delete 5 legacy UAT artifacts without source JSON ([aea39d8](https://github.com/oocx/tfplan2md/commit/aea39d8627bb61b6a6012966073926cd2eca31b2))
* exclude old UAT artifacts from style guide compliance tests ([48f2893](https://github.com/oocx/tfplan2md/commit/48f2893523d3b5b4fee34e1184ec5e707cb7a75f))
* expand demo artifact generation script ([37dcc48](https://github.com/oocx/tfplan2md/commit/37dcc481e6dbe2da55830e67e2a9d8dbd45f151a))
* fix style guide compliance violations (issue 086) ([8030c71](https://github.com/oocx/tfplan2md/commit/8030c7133b8dca19ed3a9bf1934cbe90a3e22e13))

### 🐛 Bug Fixes

* add non-breaking space before wrench icon in firewall rule summaries ([0133a84](https://github.com/oocx/tfplan2md/commit/0133a84c9e47c758d277f2be98ec2b47dc4d5fcf))
* correct H3 detection regex in style guide compliance test ([8ba04f0](https://github.com/oocx/tfplan2md/commit/8ba04f0ca1a0f5791b60065147b6f71a698a8d92))
* restore deleted artifacts and only exclude docs/* from tests ([88079d2](https://github.com/oocx/tfplan2md/commit/88079d291248c381e364eb6755749b0b2336804e))
* style guide compliance - fix 3 high/medium priority violations ([54e61a9](https://github.com/oocx/tfplan2md/commit/54e61a99d2b5b2c26aae5256cf2654c4efdc4ace))
* use non-breaking space after emoji icons in templates ([e1006d6](https://github.com/oocx/tfplan2md/commit/e1006d69df42830cece6c5f9ff90190ad7f76d64))

### 📚 Documentation

* add issue analysis for style guide compliance violations (issue 086) ([05afe10](https://github.com/oocx/tfplan2md/commit/05afe1031bb99475788ca8b90f8f3cd36ca9a246))
* add release notes for style guide compliance fixes ([639c9af](https://github.com/oocx/tfplan2md/commit/639c9af325056528c28adc2fbabaa8034e29058c))
* add screenshots to release notes for style guide fixes ([e7c64c9](https://github.com/oocx/tfplan2md/commit/e7c64c911218312c69f9c439aa66cf3e0ac38c8c))
* developer agent snapshot update work protocol entry ([32417ba](https://github.com/oocx/tfplan2md/commit/32417ba34b3d8c0e4e6362341cc56eefb73c3197))
* update documentation for style guide compliance fixes (issue 086) ([3080880](https://github.com/oocx/tfplan2md/commit/3080880f634bfd5d38f92924ea51fccd7e943d76))
* update work protocol with developer implementation summary ([b6468a6](https://github.com/oocx/tfplan2md/commit/b6468a66ecb4877d089fa128c9768b5efce33243))
* update work protocol with final test fixes completion ([45718b6](https://github.com/oocx/tfplan2md/commit/45718b653f13e4b78d88dfcec75e5b2e7bc6a840))

<a name="1.19.0"></a>
## [1.19.0](https://github.com/oocx/tfplan2md/compare/v1.18.1...v1.19.0) (2026-02-17)

### ✨ Features

* add multi-architecture binary builds to release workflow ([4a83a84](https://github.com/oocx/tfplan2md/commit/4a83a84e04c710900c5ed0856827450121080301))

### 🐛 Bug Fixes

* address code review feedback ([761dd5c](https://github.com/oocx/tfplan2md/commit/761dd5cf9f3b1798cec2afc0a34b81ae61535dd5))
* improve checksum file existence check ([15b06ed](https://github.com/oocx/tfplan2md/commit/15b06ed374ed051245b6363cba3050744a72a003))

### 📚 Documentation

* add distribution versions for Linux ARM64 binary ([bb7306e](https://github.com/oocx/tfplan2md/commit/bb7306eec73c53c7d743480d7accb40b360dc3cf))
* address code review feedback in architecture.md ([fa7f18a](https://github.com/oocx/tfplan2md/commit/fa7f18ab0df081dfb905f0914ca936b66277cf8c))
* comprehensive update to architecture.md reflecting current implementation ([d9de7f3](https://github.com/oocx/tfplan2md/commit/d9de7f3cd4b0ad839fed0afd499cdabb2d2c5158))
* update README with multi-platform binary installation instructions ([56487a6](https://github.com/oocx/tfplan2md/commit/56487a6ed8c49c87fa61c57d34606f4f5f87b07b))

<a name="1.18.1"></a>
## [1.18.1](https://github.com/oocx/tfplan2md/compare/v1.18.0...v1.18.1) (2026-02-16)

### 🐛 Bug Fixes

* handle null inputs in TerraformPlanParser ([89451c1](https://github.com/oocx/tfplan2md/commit/89451c1016fe487c71eeb632dac94e6ebf903943))
* move null check before GetName call in PrincipalMapper ([7a10ee5](https://github.com/oocx/tfplan2md/commit/7a10ee5399f32ab0803a56082c3d90ccbd0a257e))
* preserve escaped backticks in plain text values in FormatChildValue ([200ba31](https://github.com/oocx/tfplan2md/commit/200ba31326bcadb51904982f2223f3afffefbd5b))
* prevent silent overwrite on multiple positional arguments in CliParser ([581b332](https://github.com/oocx/tfplan2md/commit/581b332b4422cc6378726eafabbf50c0a85e4bca))
* remove redundant ternary in BuildDeleteSummary ([78beeac](https://github.com/oocx/tfplan2md/commit/78beeacd1367a53f2c86327349e1d0ba0f8f13fc))

<a name="1.18.0"></a>
## [1.18.0](https://github.com/oocx/tfplan2md/compare/v1.17.3...v1.18.0) (2026-02-15)

### ✨ Features

* add Azure DevOps entity mappers (users, groups, projects) ([6ac5b83](https://github.com/oocx/tfplan2md/commit/6ac5b83ce575fcbfcde4c98297b9c73018d6065e))
* add Azure DevOps principal mapping data model and parser ([b55aaf3](https://github.com/oocx/tfplan2md/commit/b55aaf31ade0e491ee6fed855de6dd944dc14abe))
* add Azure DevOps sections to example mapping files and create TC-19 test ([c8106b4](https://github.com/oocx/tfplan2md/commit/c8106b43d218ad975279b74db28ef2ecdcca920f))
* add value formatters for Azure DevOps entities ([a0104a9](https://github.com/oocx/tfplan2md/commit/a0104a9397183afe6bda7abe3b8e1f5c4d49d4ee))
* complete azdo mapping integration and examples ([a531b22](https://github.com/oocx/tfplan2md/commit/a531b222d44962f181d7cda838dbd50bd338b037))
* integrate Azure DevOps mappers with Scriban helpers and diagnostic output ([d00ed98](https://github.com/oocx/tfplan2md/commit/d00ed98b20420da5b0974db7b3642634231131e0))

### 🐛 Bug Fixes

* add icons to Azure DevOps principal formatters and fix team member/admin array formatting ([342005e](https://github.com/oocx/tfplan2md/commit/342005e819efbb5788796730c0a61b6f5fcab7e8))
* release workflow now checks main branch HEAD for tag instead of workflow_run.head_sha ([1ce8473](https://github.com/oocx/tfplan2md/commit/1ce8473825c3a0241eea906a9f2090fd1a920f65))
* **uat:** regenerate UAT artifacts using real tfplan2md CLI output ([e8130c8](https://github.com/oocx/tfplan2md/commit/e8130c881c94cfbe5baac63abff4c736d5de0372))

### 📚 Documentation

* add architecture for Azure DevOps principal mapping ([ab97e72](https://github.com/oocx/tfplan2md/commit/ab97e72fe3283ca8b9cc2295657d7af9ea48d656))
* add Azure DevOps principal mapping documentation ([b499e38](https://github.com/oocx/tfplan2md/commit/b499e385e74c637cd4c0b8a49737532ff7f02345))
* add comprehensive code review for Azure DevOps principal mapping ([252a061](https://github.com/oocx/tfplan2md/commit/252a06175d58794be3fc942dce89eac6bfa9adbf))
* add comprehensive test plan and test cases for Azure DevOps principal mapping ([506627d](https://github.com/oocx/tfplan2md/commit/506627db24bfc35ba4cf0096a90124a5bba4ef06))
* add feature specification for Azure DevOps principal mapping (085) ([c85fffe](https://github.com/oocx/tfplan2md/commit/c85fffef2f88a6c9af88042d5e2f87041f2895cb))
* add task breakdown for Azure DevOps principal mapping (085) ([c3e71b1](https://github.com/oocx/tfplan2md/commit/c3e71b13eeb83fd9da6f5d880f98a3a77e7ef844))
* add UAT Tester work protocol entry for feature 085 ([cf87f3b](https://github.com/oocx/tfplan2md/commit/cf87f3bd114ac600dbda8e7deabb4ab1bc811a7f))
* architectural decision for azdo value formatters ([e2539d3](https://github.com/oocx/tfplan2md/commit/e2539d34a16569fc42bce307211ad25b6b7c85b6))
* update work protocol with Release Manager entry ([69982be](https://github.com/oocx/tfplan2md/commit/69982bea7556e47d207cf1ba23734fedbeefb596))

<a name="1.17.3"></a>
## [1.17.3](https://github.com/oocx/tfplan2md/compare/v1.17.2...v1.17.3) (2026-02-15)

### 🐛 Bug Fixes

* add icons for resource names and groups in Azure resource ID rendering ([2433b10](https://github.com/oocx/tfplan2md/commit/2433b10311b4463644a4124c76ce3ae9eb5dd22e))
* restore code analysis in comprehensive-demo.md and fix agent instructions ([9f9ee0e](https://github.com/oocx/tfplan2md/commit/9f9ee0e259d6a70a8a09ee4cbc1a54a408ac9721))

### 📚 Documentation

* add code review for issue 465 (changes requested) ([9b95d18](https://github.com/oocx/tfplan2md/commit/9b95d18a18a449da6d0590e245ede65d87c376d8))
* add issue analysis for missing icons in Azure resource ID rendering ([759355b](https://github.com/oocx/tfplan2md/commit/759355b5c2182a6131a817c6836e91422d9fad97))
* add release manager coordination for issue 465 ([7578287](https://github.com/oocx/tfplan2md/commit/75782871a276ba7fec41145f7ab323f879b798a9))
* add retrospective analysis for issue 465 bug fix workflow ([9526256](https://github.com/oocx/tfplan2md/commit/95262560154952f1a727fcb4357f4d0f8780357e))
* approve bug fix after successful test corrections (issue 465) ([dce9ad7](https://github.com/oocx/tfplan2md/commit/dce9ad7d221de41be58aca4211ea9fe4f7d1c1db))
* approve bug fix after successful test corrections (issue 465) ([73a7843](https://github.com/oocx/tfplan2md/commit/73a78435e170e891599e64cf00dcf2ea9648d03d))
* update documentation for missing icons bug fix (issue 465) ([7d8e5b2](https://github.com/oocx/tfplan2md/commit/7d8e5b281b25bb3f8ebad8fe15c5d43c3a2b24f4))

<a name="1.17.2"></a>
## [1.17.2](https://github.com/oocx/tfplan2md/compare/v1.17.1...v1.17.2) (2026-02-14)

### 🐛 Bug Fixes

* add issue release notes for linux x64 linker fix ([d3fde60](https://github.com/oocx/tfplan2md/commit/d3fde6093d07dd5e5b295993f0a3b34c10cc170d))
* handle Terraform 'read' action to prevent false 'Already imported' warnings ([c720e31](https://github.com/oocx/tfplan2md/commit/c720e31ecb15b27e6077a51793198e5028ec7327))
* install clang for linux x64 NativeAOT publish ([6eda117](https://github.com/oocx/tfplan2md/commit/6eda1178dac8b7c1cba16a3a278baa88a250a47a))

### 📚 Documentation

* add code review for issue [#464](https://github.com/oocx/tfplan2md/issues/464) bug fix ([6a46146](https://github.com/oocx/tfplan2md/commit/6a46146b8e00db33f312c0b5b960bda7e38729f1))
* add issue analysis for false positive 'Already imported' warning ([327104a](https://github.com/oocx/tfplan2md/commit/327104a4252062c9e31d737c68c97c919b58f126))
* add release notes for issue [#464](https://github.com/oocx/tfplan2md/issues/464) bug fix ([1725311](https://github.com/oocx/tfplan2md/commit/172531193c15a99f49fd812b2f024b0bc0623fdf))
* add work protocol for issue [#464](https://github.com/oocx/tfplan2md/issues/464) bug fix ([057be65](https://github.com/oocx/tfplan2md/commit/057be65795eec66412ff140243114738a52fbc05))
* update work protocol with Release Manager actions ([4235c1d](https://github.com/oocx/tfplan2md/commit/4235c1d48cf3922b6fd7c875d21a165ce181595a))

<a name="1.17.1"></a>
## [1.17.1](https://github.com/oocx/tfplan2md/compare/v1.17.0...v1.17.1) (2026-02-14)

### 🐛 Bug Fixes

* correct Scriban capture syntax and re-enable MD009 (SNAPSHOT_UPDATE_OK) ([e71a079](https://github.com/oocx/tfplan2md/commit/e71a079803c6bd4e47b9ea178982a7e715b11fe6))
* disable MD009 trailing-spaces rule in markdownlint ([efecb99](https://github.com/oocx/tfplan2md/commit/efecb994ff0b55f3a51bff9f9a776fbed3d6de2d))
* eliminate trailing spaces in child resource tables (SNAPSHOT_UPDATE_OK) ([4291555](https://github.com/oocx/tfplan2md/commit/4291555f210698fb4d60442cde48d7dcbc8c4bec))
* remove trailing spaces from child resource tables (SNAPSHOT_UPDATE_OK) ([e6c88a6](https://github.com/oocx/tfplan2md/commit/e6c88a65c53a55dcf7097fdc5084d4dc26fe8bd1))
* replace non-existent Docker base image with .NET 10 Ubuntu 24.04 ([d1d023c](https://github.com/oocx/tfplan2md/commit/d1d023c46a842cf3ad4891f35b2152afb287b4f0))
* restore code analysis sections in comprehensive-demo.md ([2fcf3e3](https://github.com/oocx/tfplan2md/commit/2fcf3e3be0afb6c89891446ef4470391de5bf6ef))

### 📚 Documentation

* add tooling & instruction analysis to retrospective (dotnet 10 dual runner, UAT auth, screenshots) ([fdde0e1](https://github.com/oocx/tfplan2md/commit/fdde0e1b51c60caf034535ee5af3109cc10faeba))
* comprehensive retrospective analysis for Feature 072 (PR [#469](https://github.com/oocx/tfplan2md/issues/469)) ([2070aec](https://github.com/oocx/tfplan2md/commit/2070aec629c4ebcdcf3d93e34c18fd9900602ee8))
* update issue 462 with .NET 10 image reality check ([3765c67](https://github.com/oocx/tfplan2md/commit/3765c670ca73e735a4abd3780fda95deefbfa01d))

<a name="1.17.0"></a>
## [1.17.0](https://github.com/oocx/tfplan2md/compare/v1.16.3...v1.17.0) (2026-02-14)

### ✨ Features

* add actual PNG screenshots to release notes using screenshot generator tools ([acfaecb](https://github.com/oocx/tfplan2md/commit/acfaecbc20cb3475b0097254e7b495d04732320f))
* add Azure RM parent-child row extractors and registrations ([0dd221f](https://github.com/oocx/tfplan2md/commit/0dd221fc2b5da8adad0b930f99587c20ceb59a05))
* add Linux x64 binary build to release workflow ([9ff802d](https://github.com/oocx/tfplan2md/commit/9ff802d49671c5690156271450e26dcc6552db36))
* extend parent-child grouping to Azure RM network resources ([26d1814](https://github.com/oocx/tfplan2md/commit/26d1814a76eb20b730af71ec833d1550a2f63f26))
* generate real tfplan2md output for Azure RM Batch 2 UAT ([65f5792](https://github.com/oocx/tfplan2md/commit/65f57928e9ab3d7b1b12dce04f6c00d91abe2805))
* implement parent-child resource grouping for Azure RM resources ([c3a221e](https://github.com/oocx/tfplan2md/commit/c3a221e10176501e02461b39401e8825e92c9db9))
* restore Feature 016 NSG columns to parent-child framework ([1a9d1d8](https://github.com/oocx/tfplan2md/commit/1a9d1d8b703d3ecb6a18f824b4bfcaea5781fb7f))
* **uat:** add Azure RM Batch 2 UAT artifact ([ac254b5](https://github.com/oocx/tfplan2md/commit/ac254b559c926d58d7a8e7f7dc58ef0f98453bae))

### 🐛 Bug Fixes

* add backticks to all non-diff values, preserve HTML diffs (SNAPSHOT_UPDATE_OK) ([0b32d08](https://github.com/oocx/tfplan2md/commit/0b32d085057a8a57bb4f4053a69555b8ade0dee4))
* add space after value before pipe in NSG table template (fixes 4 test failures) ([823fe86](https://github.com/oocx/tfplan2md/commit/823fe868afde71649d949496cf5282e6812680d7))
* add spaces before pipes in NSG security rules table (fixes 5 test failures) ([42b3679](https://github.com/oocx/tfplan2md/commit/42b3679a70bf4e1491b813c2ca44dd354d77feea))
* add XML documentation and remove unused method ([1fa0d6d](https://github.com/oocx/tfplan2md/commit/1fa0d6db5a250c6232bc2b7d48f911611b5e5fda))
* always show Terraform Resource column in child resource tables ([9bcfe48](https://github.com/oocx/tfplan2md/commit/9bcfe48d5a9677f4871b3c702923175ac66b3461))
* bare dash without code tags, newlines instead of br in GitHub diffs (SNAPSHOT_UPDATE_OK) ([6b8a7b1](https://github.com/oocx/tfplan2md/commit/6b8a7b193dbdea13b94b9ebd80ffb67790e227da))
* calculate summary counts before parent-child merging ([9721f83](https://github.com/oocx/tfplan2md/commit/9721f836bb4106e9177deefbf467f4e46a7e03a2))
* complete inline diff implementation for child resources ([4c7084e](https://github.com/oocx/tfplan2md/commit/4c7084e210c02843b55ba6975f5d85fe662b4b00))
* convert screenshot image syntax to working markdown links in release notes ([6369ae8](https://github.com/oocx/tfplan2md/commit/6369ae84e0af7908ebb693526b336306784005f4))
* correct table rendering - no trailing spaces, proper newlines (SNAPSHOT_UPDATE_OK) ([926d35d](https://github.com/oocx/tfplan2md/commit/926d35d596528e801c51405a760f02b0add121b1))
* eliminate DNS and NSG table duplication in Feature 068 ([926cf28](https://github.com/oocx/tfplan2md/commit/926cf28f039c03fc479b13ee562984a5977987cc))
* improve UAT script error handling and agent instructions ([21c89e1](https://github.com/oocx/tfplan2md/commit/21c89e1d983b463880791a4720708656476f57e4))
* prevent HTML tags in diff rendering for Azure RM row extractors ([dd8a567](https://github.com/oocx/tfplan2md/commit/dd8a56741869b57d19b282d98b9c64cc215d9e05))
* regenerate azure-rm-batch-2-feature-test.md with ONLY Azure RM resources ([f5c331a](https://github.com/oocx/tfplan2md/commit/f5c331abf0e6f5899bee41890846fce096318048))
* remove backticks from diff output for proper markdown rendering ([608bb17](https://github.com/oocx/tfplan2md/commit/608bb177d57e9f4a42d0e3fc5e821c8c20b44eba))
* remove trailing spaces from _child_resources.sbn template (SNAPSHOT_UPDATE_OK) ([7c396bf](https://github.com/oocx/tfplan2md/commit/7c396bf1e27434446a8cba3188d4637a481a4332))
* remove unnecessary Azure subscription check in uat-azdo.sh ([119ff0d](https://github.com/oocx/tfplan2md/commit/119ff0dc66e2bbb281d5f397f6eb2fae5f0c1e5c))
* replace HTML-styled inline diffs with plain markdown format ([c856516](https://github.com/oocx/tfplan2md/commit/c8565163d811b119a50c3c510fe579a21ba88bad))
* restore HTML span diff formatting with character-level highlighting ([885c3aa](https://github.com/oocx/tfplan2md/commit/885c3aa0bf753753332142e42ba57f25f020812a))
* revert table template to working version from b33d08a (fixes all test failures) ([e05e792](https://github.com/oocx/tfplan2md/commit/e05e79230fbf6f8b2b53e53ba40db3fcc2ab1865))
* revert to <br> tags in simple diffs, add detection in FormatChildValue (SNAPSHOT_UPDATE_OK) ([0b87a5b](https://github.com/oocx/tfplan2md/commit/0b87a5bf13e17dd9d42ea1fdd0a4a7ff7ce33542))
* UAT issues - conditional Terraform Resource column, DNS types, inline UPDATE demos ([bed3adf](https://github.com/oocx/tfplan2md/commit/bed3adf2cf5f4c0e71656dd01a406f3de3f8eba9))
* use Ubuntu 22.04 container for Linux x64 binary build to support Debian 12 ([039de6b](https://github.com/oocx/tfplan2md/commit/039de6b9e4dca7870f053b45f989866fe90018e9))
* **azure-rm:** add ParentIdAttribute for name-based child matching ([898f65d](https://github.com/oocx/tfplan2md/commit/898f65d5df62f6860c3944942d0228af21c57b45))

### ♻️ Refactoring

* move Azure RM feature docs from 068 to new folder 072 ([9881476](https://github.com/oocx/tfplan2md/commit/9881476ab804cd85790cd598626ed6328833b8b9))

### 📚 Documentation

* add architecture for linux x64 binary distribution (Phase 1) ([7b48583](https://github.com/oocx/tfplan2md/commit/7b48583823be3cf9569f104ea9cc8a3151fcc842))
* add Azure RM batch 2 architecture for parent-child resource grouping ([194d702](https://github.com/oocx/tfplan2md/commit/194d702c394f778c16457dea03453b21779c92ce))
* add Azure RM batch 2 requirements for parent-child grouping ([6af7b5f](https://github.com/oocx/tfplan2md/commit/6af7b5f710ee35a5375fc4a1791b1a672073fba4))
* add code review for HTML span diff restoration (692fcf0) ([cb2f40f](https://github.com/oocx/tfplan2md/commit/cb2f40f0257e48a4624a2f7d936bf28097cf9073))
* add comprehensive code review for conditional Terraform Resource column ([b1caae7](https://github.com/oocx/tfplan2md/commit/b1caae705c5d69adf8d45b8498f47c3f1ff30e28))
* add comprehensive test expectation update plan for HTML inline diff restoration ([3a6deec](https://github.com/oocx/tfplan2md/commit/3a6deec92e217f812b0b816a7fe4afb04e9ee57b))
* add Developer entry to work protocol for Phase 1 implementation ([7f529ff](https://github.com/oocx/tfplan2md/commit/7f529ff58bdf45656c00332e8c2afa0e83f12a28))
* add Developer work protocol entry for blocker fixes ([bf66da8](https://github.com/oocx/tfplan2md/commit/bf66da87518a1e077bdf00be15274f2a9a3d64de))
* add feature specification for 461-multi-platform-binary-distribution (Phase 1: Linux x64) ([9f0b89f](https://github.com/oocx/tfplan2md/commit/9f0b89f95dcb1a06033b5b7978017743628f1953))
* add feature test plan to GitHub UAT PR [#72](https://github.com/oocx/tfplan2md/issues/72) ([e4ed4ca](https://github.com/oocx/tfplan2md/commit/e4ed4ca37b956f2037218c6dfa67de3403a091b8))
* add test plan and UAT test plan for Azure RM Batch 2 ([75080b1](https://github.com/oocx/tfplan2md/commit/75080b1e52e149418e3a7a050eb2f21857c72f88))
* add UAT report and updated artifacts for commit 74f93d7 ([ff10400](https://github.com/oocx/tfplan2md/commit/ff10400bdddb649a233afe1ae41c4b2a58a02042))
* add UAT tester work log entry for comprehensive fixes validation ([c848839](https://github.com/oocx/tfplan2md/commit/c84883967c93616ee87077710b00d880abff829f))
* add work protocol entry for Azure DevOps PR [#74](https://github.com/oocx/tfplan2md/issues/74) artifact posting ([1506158](https://github.com/oocx/tfplan2md/commit/1506158b88df779f3af0ac8c005cf90bb7c73ef4))
* code review completed - ADR-008 Phase 1 approved for UAT ([7f2e688](https://github.com/oocx/tfplan2md/commit/7f2e688aea49c23fbb2f2f50313818a694d3367c))
* code review of backticks formatting fix (9c1079d + 98167ed) ([6c17237](https://github.com/oocx/tfplan2md/commit/6c172374d53b4a849769551360b23c58e9ac87b0))
* code review of HTML inline diff test expectations update (e5971f1) ([c05cd6d](https://github.com/oocx/tfplan2md/commit/c05cd6d6b796edf390c2cd009eee4c812e417a1c))
* code reviewer re-review approves Azure RM Batch 2 fixes ([07ebcc2](https://github.com/oocx/tfplan2md/commit/07ebcc2379fdb05685d0fa14d0001e4a96e04a21))
* complete Phase 2 testing documentation (T008-T013) ([7c8e158](https://github.com/oocx/tfplan2md/commit/7c8e15818bfd8609ea7104af5f53d9840f4723ca))
* complete UAT process for Azure RM Batch 2 ([52cc3ff](https://github.com/oocx/tfplan2md/commit/52cc3ffc976f6255a3eb5fb966ae3c7690a38ccb))
* confirm Azure DevOps PR [#74](https://github.com/oocx/tfplan2md/issues/74) UAT comments were actually posted ([244e357](https://github.com/oocx/tfplan2md/commit/244e3578fee731fb0e2f959edec1cc5363a6b90c))
* document Linux x64 binary distribution feature ([83250c0](https://github.com/oocx/tfplan2md/commit/83250c0b6303cd11841d906930055b2507af7a4a))
* document UAT run with comprehensive fixes (commit 9f0db75) ([e77a0d9](https://github.com/oocx/tfplan2md/commit/e77a0d9a17babae2e10e689690011963f254e69c))
* mark tasks T001-T007 as complete ([ba2151c](https://github.com/oocx/tfplan2md/commit/ba2151c217d040cc84f82e9328b099407cd737a4))
* release coordination complete for ADR-008 Phase 1 ([5fbb16b](https://github.com/oocx/tfplan2md/commit/5fbb16b9f9cf4514b48130d2f442c0bb3fc59d46))
* update agents.md with UAT plan workflow requirements ([1da415e](https://github.com/oocx/tfplan2md/commit/1da415ee0de03c440ef9495e725a10a1ed847bd9))
* update documentation for Azure RM parent-child resource grouping batch 2 ([8ce94f5](https://github.com/oocx/tfplan2md/commit/8ce94f50863dbe64fdece61e5a7cee345d11a6a0))
* update release notes for Feature 068 with Azure RM implementation and screenshots ([3f72248](https://github.com/oocx/tfplan2md/commit/3f722480983a92202e7000d28f92e58474302006))
* update status and work protocol for Azure RM Batch 2 ([96248e7](https://github.com/oocx/tfplan2md/commit/96248e7d09dd14bc82b81f1cfc79e91812ac9162))
* update UAT PRs with inline diff fix artifacts ([cb23b20](https://github.com/oocx/tfplan2md/commit/cb23b2048c4c57efec0198fb973e7c9a9ef1bb2a))
* update UAT report with Azure DevOps PR [#74](https://github.com/oocx/tfplan2md/issues/74) artifact posting details ([21eaaa0](https://github.com/oocx/tfplan2md/commit/21eaaa0482d4824b432a99db356e530618625d5a))
* update UAT report with HTML inline diff verification results ([3ca7c94](https://github.com/oocx/tfplan2md/commit/3ca7c94051e8b9c99b1539df5574a3617c75e4b3))
* update UAT report with real tfplan2md output and GitHub PR [#72](https://github.com/oocx/tfplan2md/issues/72) ([8657b51](https://github.com/oocx/tfplan2md/commit/8657b512c7b9804ad9264f7f4e77be1406162413))
* verify Azure DevOps PR [#74](https://github.com/oocx/tfplan2md/issues/74) thread 271 created with feature test artifact ([9c0e5d3](https://github.com/oocx/tfplan2md/commit/9c0e5d351d96005460a421eeb3189b8326bdc111))
* **uat:** add Azure RM Batch 2 UAT report and work protocol entry ([5e9d0bd](https://github.com/oocx/tfplan2md/commit/5e9d0bdbb41aa5c27fa2d35d4ebf75e9aaad19f7))

<a name="1.16.3"></a>
## [1.16.3](https://github.com/oocx/tfplan2md/compare/v1.16.2...v1.16.3) (2026-02-12)

### 🐛 Bug Fixes

* handle array-typed expression properties in ConfigurationReferenceResolver ([e22ba25](https://github.com/oocx/tfplan2md/commit/e22ba2544c70d7285c6b95669e5344a86d8aba6d))

### 📚 Documentation

* add ADR-008 for multi-platform binary distribution ([5de4773](https://github.com/oocx/tfplan2md/commit/5de477374e114db74804f4526e822980c7d97533))
* add code review for JsonElementHasWrongType bug fix ([8439e36](https://github.com/oocx/tfplan2md/commit/8439e36cf4092c814bd9d4bd954780bd09e75883))

<a name="1.16.2"></a>
## [1.16.2](https://github.com/oocx/tfplan2md/compare/v1.16.1...v1.16.2) (2026-02-12)

### 🐛 Bug Fixes

* enhance defensive checks for JSON array enumeration ([6493652](https://github.com/oocx/tfplan2md/commit/649365279f4905ffb97aebfc6095327e4355b426))

### 📚 Documentation

* add 5 new agent skills to docs/agents.md ([d75b16e](https://github.com/oocx/tfplan2md/commit/d75b16ebb90eeb65ed7eaba8a00f436295ff2afe))
* add issue analysis for JSON parsing error with Azure resources (v1.16.0/v1.16.1) ([1c118d4](https://github.com/oocx/tfplan2md/commit/1c118d4039aa551afc88dd17ccccb6805e7dd497))
* add release notes for issue 070 (v1.16.1) ([5d1d573](https://github.com/oocx/tfplan2md/commit/5d1d5731d2b48ec80f079b109a9a943b3c0213f0))
* add release notes for v1.16.2 patch ([7b705f2](https://github.com/oocx/tfplan2md/commit/7b705f261dbc3a4c07dcb4711ffb638249233128))
* update work protocol with Developer implementation summary ([1e3d998](https://github.com/oocx/tfplan2md/commit/1e3d998d3e5117ffd5aaebdf8d3de2543c552b7a))
* update work protocol with Release Manager entry ([b3627af](https://github.com/oocx/tfplan2md/commit/b3627af87d0f6fc11ce9f5d478812414bf5075fa))

<a name="1.16.1"></a>
## [1.16.1](https://github.com/oocx/tfplan2md/compare/v1.16.0...v1.16.1) (2026-02-11)

### 🐛 Bug Fixes

* update Azure AD group member counts after parent-child merging ([82a4533](https://github.com/oocx/tfplan2md/commit/82a4533fae929706b38de5490c1127c84efa2e1e))

### ♻️ Refactoring

* Improve pragma directive ordering in Program.cs ([e90cd4f](https://github.com/oocx/tfplan2md/commit/e90cd4f21e22c7424aeb3d7825e2a17d321ff56d))
* move Azure AD group summary logic to provider layer ([2bd97e2](https://github.com/oocx/tfplan2md/commit/2bd97e2e9c765f5de4e4635622ed8d23cec8994f))

### 📚 Documentation

* add code review report for callback mechanism ([aa63a69](https://github.com/oocx/tfplan2md/commit/aa63a692221ccc39b17610d89e42494d5be7fedf))
* add coding agent report_progress documentation ([98ebdf3](https://github.com/oocx/tfplan2md/commit/98ebdf334f8ba136c0487752d0f171196bd3d1f9))
* add issue analysis for parent-child summary member counts ([0aeb548](https://github.com/oocx/tfplan2md/commit/0aeb548ec2f3a7dfd18d64f896197a8efd18fb72))
* add UAT report and update work protocol (manual UAT required) ([fe6cd64](https://github.com/oocx/tfplan2md/commit/fe6cd640d2d94bd7ca60b0e9ed8b24b92b0fd9f9))
* clarify askQuestions tool is VS Code-only ([df5450a](https://github.com/oocx/tfplan2md/commit/df5450a397a8e3ef10b6f49edcffdd270a8a4b94))
* code review approval for parent-child summary member count fix ([d044711](https://github.com/oocx/tfplan2md/commit/d044711dfe7e592beacef68a514fc49a17bb073d))
* confirm documentation accuracy after member count fix ([7a756ce](https://github.com/oocx/tfplan2md/commit/7a756ce0c08e8ddda849a17444b84b3b8040513e))
* finalize retrospective for feature 068 ([b2e692b](https://github.com/oocx/tfplan2md/commit/b2e692bec88d4c1887e1bf326dcdb1d00acc1dda))
* Fix documentation reference in Program.cs ([d1cc8fd](https://github.com/oocx/tfplan2md/commit/d1cc8fd96f7501aa3468e5fef799cc448457bc95))
* update work protocol with Developer implementation notes ([69b79ec](https://github.com/oocx/tfplan2md/commit/69b79ec5fff56a4fa579a742f1e6e8cb586ed952))
* Use standard reference format in Program.cs ([87f6b12](https://github.com/oocx/tfplan2md/commit/87f6b128aa2c8518dd1d240a01858a6e4b71ca0a))

<a name="1.16.0"></a>
## [1.16.0](https://github.com/oocx/tfplan2md/compare/v1.15.1...v1.16.0) (2026-02-11)

### ✨ Features

* add azuread group member inline rendering ([2fa7066](https://github.com/oocx/tfplan2md/commit/2fa70667be27dbe691059f5639c24c2bf2c988ec))
* add azuredevops group/team inline rendering ([ea1b317](https://github.com/oocx/tfplan2md/commit/ea1b317bbda8de8f10ae61a33fc86b4306ed7a6c))
* add child resource rendering pipeline ([d32e82c](https://github.com/oocx/tfplan2md/commit/d32e82c2a70eb7fba26faebf0535be4f4bec6aea))
* add configuration reference matching ([566971c](https://github.com/oocx/tfplan2md/commit/566971c2223fbd7adfd409b6ba20b441ba4a8b55))
* add parent-child relationship registry ([15bae7f](https://github.com/oocx/tfplan2md/commit/15bae7f00ce314cfe3478666dc367e810339df96))
* add provider hook for parent-child relationships ([a95a126](https://github.com/oocx/tfplan2md/commit/a95a12637061bbcd998eed8432b6228920ebdb2f))
* merge parent-child resources in report model ([12d4514](https://github.com/oocx/tfplan2md/commit/12d45146944903dff9810c6b8114bb065d92f90a))
* remove inline child attributes from parent tables ([2d33fcb](https://github.com/oocx/tfplan2md/commit/2d33fcb83a5613c105eabd439295c044277fecbf))

### 🐛 Bug Fixes

* change Platforms → MarkdownGeneration from forbidden to allowed dependency ([8389718](https://github.com/oocx/tfplan2md/commit/838971863412142cfd648d98bad86645d181f456))
* refresh azuread group summaries and UAT artifact ([f92e619](https://github.com/oocx/tfplan2md/commit/f92e619b1a36c9a1b14e392111814d454cfe7da9))

### ♻️ Refactoring

* align architecture boundaries and tests ([10e8e5d](https://github.com/oocx/tfplan2md/commit/10e8e5d56d592fa3304ad4d73ec39b2153375f15))
* move ProviderRegistry to MarkdownGeneration.Services ([36d88cc](https://github.com/oocx/tfplan2md/commit/36d88cc0623f9410cbe6006af1f76ba43150ea67))

### 📚 Documentation

* add architecture design for boundary enforcement (feature 066) ([96dd500](https://github.com/oocx/tfplan2md/commit/96dd5007133e3a1385df70e72651b6b533fd406a))
* add architecture for parent-child resource grouping ([0b79f48](https://github.com/oocx/tfplan2md/commit/0b79f48ea160f833730b189f39e951cf78c4ee63))
* add code review for parent-child-resource-grouping ([509c339](https://github.com/oocx/tfplan2md/commit/509c33963ffed01febe98c1c4a0e1c8349ccd917))
* add comprehensive code review for architecture boundary enforcement ([05bd8e4](https://github.com/oocx/tfplan2md/commit/05bd8e4152aa08b06daa2487aa50a6f10d8168f7))
* add feature specification for 066-architecture-boundary-enforcement ([d71784d](https://github.com/oocx/tfplan2md/commit/d71784d0c05c0a20a9159d06cbde92747d818191))
* add feature specification for 068-parent-child-resource-grouping ([c05d802](https://github.com/oocx/tfplan2md/commit/c05d802f0db1337ad0e50142e5898e76c1eb0b7b))
* Add implementation tasks for architecture boundary enforcement ([e15350b](https://github.com/oocx/tfplan2md/commit/e15350b17374010741df0fa9bc416258366505e4))
* add post-UAT fix code review for feature 068 ([c3b8939](https://github.com/oocx/tfplan2md/commit/c3b893976e1dcaff48efdfcde6454beac179c80c))
* add release notes and finalize work protocols for Feature 066 ([fd62569](https://github.com/oocx/tfplan2md/commit/fd6256996b7237e3406870171e39c3ef99037bbb))
* add release notes for parent-child-resource-grouping ([e59952f](https://github.com/oocx/tfplan2md/commit/e59952f7759d05fa71ddca1c2a246728cbbe66ab))
* add tasks for 068-parent-child-resource-grouping ([e8ebf23](https://github.com/oocx/tfplan2md/commit/e8ebf2326f16c882da9206d6f9a8c453ae811b42))
* add test plan and UAT plan for 068-parent-child-resource-grouping ([87e9fd9](https://github.com/oocx/tfplan2md/commit/87e9fd9e68fbfcd3cfe81d27f22de9bf05a8bacb))
* add test plan and UAT plan for architecture boundary enforcement ([2b1851c](https://github.com/oocx/tfplan2md/commit/2b1851cccf97b2c9a37114fc860172d54a66bf32))
* add UAT report for feature 068 (FAILED) ([93c2a5d](https://github.com/oocx/tfplan2md/commit/93c2a5df12b7dc26a69278b10e98dec8fd4d02a3))
* clarify scope for examples 7-10 ([3afa2ee](https://github.com/oocx/tfplan2md/commit/3afa2eeb68642efc1f2c13c99a0a040511c5ac58))
* clarify that Platforms layer includes platform-specific rendering, not just metadata ([56aaaa8](https://github.com/oocx/tfplan2md/commit/56aaaa83a62ec85c9eb6265c1867f41681fd7ce5))
* complete work protocol for parent-child-resource-grouping release ([edd5423](https://github.com/oocx/tfplan2md/commit/edd5423e0976f67dac4e14375420474f73ce603d))
* comprehensive test coverage for parent-child configuration reference matching ([adda9cc](https://github.com/oocx/tfplan2md/commit/adda9cc4c745cfadc88e971f8e71fa4447f2e96b))
* create comprehensive architecture rules documentation ([4d37f47](https://github.com/oocx/tfplan2md/commit/4d37f47ef5da7b2015a9a0c51983f13d51aa9a63))
* finalize work protocol for feature 068 release ([6a74f8f](https://github.com/oocx/tfplan2md/commit/6a74f8f592e97b10eed685b36756fb01dda4af9e))
* reference GitHub issues for code review suggestions ([52f734a](https://github.com/oocx/tfplan2md/commit/52f734a60a7c24d560482a6928889bb386a42999))
* reference issue [#446](https://github.com/oocx/tfplan2md/issues/446) for agent instruction improvements ([c1f247f](https://github.com/oocx/tfplan2md/commit/c1f247faaf6835479b826cb2a53bbf5e40559fed))
* refresh tasks for feature 068 review fixes ([89f0e00](https://github.com/oocx/tfplan2md/commit/89f0e00c3ab33a4798e80e97259c7f63438708a3))
* UAT passed for parent-child resource grouping (issue [#447](https://github.com/oocx/tfplan2md/issues/447) tracked separately) ([0d3fa88](https://github.com/oocx/tfplan2md/commit/0d3fa88b69e30d4bf7960a5b94b2927804ef7368))
* update demo artifacts for inline child tables ([5f757b3](https://github.com/oocx/tfplan2md/commit/5f757b36090f57d71dc26b877378e10087b32008))
* update documentation ([338e3e3](https://github.com/oocx/tfplan2md/commit/338e3e3a190743eeeefb00114c5587f5e8af4966))
* update global documentation for architecture boundary enforcement ([9d4035b](https://github.com/oocx/tfplan2md/commit/9d4035b1d7ba05145372d8fc7c137cf1b2dca226))
* update release notes with correct SHAs ([72cdf9e](https://github.com/oocx/tfplan2md/commit/72cdf9e80274c528d6dc360ccbd9ea827fa2a207))
* update task status for 068 ([e9e6296](https://github.com/oocx/tfplan2md/commit/e9e629619081b9b6ed564f41dc79c60863bcb8ab))
* update tasks and UAT artifact ([e813e47](https://github.com/oocx/tfplan2md/commit/e813e47ebe075c51e7b8c9de239b6a47d0e51ee4))
* update work protocol for 068 ([8fb9459](https://github.com/oocx/tfplan2md/commit/8fb9459104b0e0fc4e947d67434d0b05b17b5d26))
* update work protocol with Developer implementation summary ([5819da9](https://github.com/oocx/tfplan2md/commit/5819da955e3fb8ef4e34e79760de431ab2d1d54b))

<a name="1.15.1"></a>
## [1.15.1](https://github.com/oocx/tfplan2md/compare/v1.15.0...v1.15.1) (2026-02-10)

### 🐛 Bug Fixes

* remove duplicate headers in azapi_resource and azuredevops_variable_group templates ([c412792](https://github.com/oocx/tfplan2md/commit/c412792db8379af8a85054efe0196fe2e0f6440f))
* use --no-follow-tags when pushing commit to ensure tag triggers Release workflow ([156c70f](https://github.com/oocx/tfplan2md/commit/156c70f1691d7da1bfa8ddb85aa24fe7d61cbeb1))

### 📚 Documentation

* add issue analysis for duplicate headers in azapi_resource and variable_group templates ([603b2ef](https://github.com/oocx/tfplan2md/commit/603b2ef994a917cc2d353c13ba357882194c0443))
* add release notes for duplicate header bug fix ([f3b2d6a](https://github.com/oocx/tfplan2md/commit/f3b2d6a8ec13e235d0b98cbf8722f0075c8355fe))
* add retrospective for Feature 065 with chat metrics ([bc2752d](https://github.com/oocx/tfplan2md/commit/bc2752d29cefca79b76364c89db64f2cbe2a44a2))
* approve duplicate header fix after code review ([a5819ed](https://github.com/oocx/tfplan2md/commit/a5819edd19c77cc14b5e764103dd7640495a3406))
* document new release screenshot wrapper script ([8ae62e8](https://github.com/oocx/tfplan2md/commit/8ae62e8719f63ac50ea249c9ceaad04ec9e10ed8))
* update demo artifacts for duplicate header fix ([b211e0b](https://github.com/oocx/tfplan2md/commit/b211e0b8a2dbd06d3967cf629cc9776ead723a12))
* update examples in features.md to remove duplicate headers ([43c4a0e](https://github.com/oocx/tfplan2md/commit/43c4a0e95d3df1fae41801726a4e19ab182ab64b))
* update work protocol with developer completion ([d73e08a](https://github.com/oocx/tfplan2md/commit/d73e08aeac885e1c4835439b4021ec6c46f35ff8))
* update work protocol with Release Manager entry ([708f2ed](https://github.com/oocx/tfplan2md/commit/708f2ed40e1666381dad51948f0c8dbbdd8c99fb))

<a name="1.15.0"></a>
## [1.15.0](https://github.com/oocx/tfplan2md/compare/v1.14.0...v1.15.0) (2026-02-09)

### ✨ Features

* add architectural and snapshot guardrails to agent prompts ([c6753da](https://github.com/oocx/tfplan2md/commit/c6753dae42ce8f1b823dc2bab89dbbf4e1176b63))
* add tenant and management group formatting ([ab1955c](https://github.com/oocx/tfplan2md/commit/ab1955c0c704fd5497a0b3175f2213af94b93bab))

### 🐛 Bug Fixes

* align management group scope formatting ([32494fd](https://github.com/oocx/tfplan2md/commit/32494fd15a3a8eb1f2bfd598fa628ea6ca458301))
* align management group scope summaries ([2cb39a1](https://github.com/oocx/tfplan2md/commit/2cb39a10d5ef413ba0547fcd280d7fd37543b3f4))
* align management group summary formatting ([1745c9f](https://github.com/oocx/tfplan2md/commit/1745c9fe8799695d0c0f68686202c0d2339fca04))
* correct management group scope formatting ([50aa85a](https://github.com/oocx/tfplan2md/commit/50aa85ac9351f37b51b7fb9fccb12b64e142a838))
* keep management group scope labels in code spans ([16b3887](https://github.com/oocx/tfplan2md/commit/16b38877464a522c3e19e5543ce28b801de7fad6))
* place Azure icons inside code spans ([d73102d](https://github.com/oocx/tfplan2md/commit/d73102d75ee0ede523a146c29648e02aa50cfa4f))

### 📚 Documentation

* add architecture for tenant display mapping ([8fa4c8e](https://github.com/oocx/tfplan2md/commit/8fa4c8e5285136b45e9b2c4676806ec0d112db0a))
* add code review for tenant display name mapping ([da54da8](https://github.com/oocx/tfplan2md/commit/da54da84c23c587e78d1e5e14585abdc87c1573b))
* add feature specification for tenant display name mapping ([8ff289b](https://github.com/oocx/tfplan2md/commit/8ff289beeb7fcd67b9e11b27091e8644db6948d9))
* add release notes and work protocol for 063 and 065 ([c514122](https://github.com/oocx/tfplan2md/commit/c5141227552ac98b4e4c7ad28dd8ed05092df7cf))
* add tasks for tenant display mapping ([7cb44b7](https://github.com/oocx/tfplan2md/commit/7cb44b786456f63da2dcaf0cd5f0b93f308da18d))
* add test plan and uat test plan for 065-tenant-display-mapping ([daee7e2](https://github.com/oocx/tfplan2md/commit/daee7e2196d55b8c4999b2a98bde11c83f32ac24))
* add UAT report for 065-tenant-display-mapping ([3b549f2](https://github.com/oocx/tfplan2md/commit/3b549f2c58ac65b2f9b2d2dc73b3d209d95dd5b2))
* add UAT report for 065-tenant-display-mapping (PASSED) ([a9d6353](https://github.com/oocx/tfplan2md/commit/a9d63535b289b420648a986be196ba8a5d218a9e))
* approve Feature 065 for release and fix icon docs ([3be60b7](https://github.com/oocx/tfplan2md/commit/3be60b77e760b2e030f628785fb4f6bf9bbb4c6a))
* correct UAT report for 065 with accurate results ([39e7fa5](https://github.com/oocx/tfplan2md/commit/39e7fa5140066b557bb5b340e557f0392511e399))
* correct UAT report formatting for 065 ([84709ab](https://github.com/oocx/tfplan2md/commit/84709abc39d53bb0620c22b15d6eda3a5d79f5e0))
* correct work protocol entry ([975b5f3](https://github.com/oocx/tfplan2md/commit/975b5f326ed55d57a7cf79df6190dbe4b9376d7f))
* document Azure Display Enhancements with visual icons ([3b4ec60](https://github.com/oocx/tfplan2md/commit/3b4ec60159145faba98e1b91142b96c7f036677a))
* fix work protocol duplication and content ([e279566](https://github.com/oocx/tfplan2md/commit/e279566c0140c03d7b9d78e0f91e95c98115b07a))
* log Code Reviewer work in protocol ([554c7cb](https://github.com/oocx/tfplan2md/commit/554c7cbe64357058ce1def81beb7c60b04b9e132))
* update demo artifacts for tenant mapping ([8bb40ec](https://github.com/oocx/tfplan2md/commit/8bb40ec5daf2b9c7c415c4edb37329d27af35233))
* update tenant display name and management group icon formatting in specifications and UAT test plan ([97323be](https://github.com/oocx/tfplan2md/commit/97323bee460febe356bc78b26446355df859b096))
* update tenant mapping guidance ([e6fa85e](https://github.com/oocx/tfplan2md/commit/e6fa85ec4a813f1486b8fa22d979b0071863e0ca))
* update UAT report and work protocol for corrected test ([3d1427e](https://github.com/oocx/tfplan2md/commit/3d1427e7994b6d4c5488278f5362c6d0efcae22b))
* update UAT report with correct PR links ([6612a3a](https://github.com/oocx/tfplan2md/commit/6612a3a7ac70f94752efc7ec6e3cbdca9f19ea39))
* update UAT report with minimal feature plan PRs ([6606ebc](https://github.com/oocx/tfplan2md/commit/6606ebc16aa7952568d016a820f9d2159efd75c4))
* update work protocol for snapshot fix ([47fc22f](https://github.com/oocx/tfplan2md/commit/47fc22fd49fbdb74021fafdf09dc3a21012ce6b8))
* update work protocol with UAT tester feedback and problems encountered ([7ed1bf8](https://github.com/oocx/tfplan2md/commit/7ed1bf888a081364f691f445e307bed57711ad33))

<a name="1.14.0"></a>
## [1.14.0](https://github.com/oocx/tfplan2md/compare/v1.13.1...v1.14.0) (2026-02-08)

### ✨ Features

* extract CompositionRoot from ProgramEntry per ADR-006 ([b6c2291](https://github.com/oocx/tfplan2md/commit/b6c22913a621ad95ac2555e0839a6d2dfafb588b))

### 🐛 Bug Fixes

* correct ADR path reference in CompositionRoot documentation ([14ecfb6](https://github.com/oocx/tfplan2md/commit/14ecfb6091d7f281eb5df5661a210ab33098b36a))

### ♻️ Refactoring

* implement Pure DI with CompositionRoot class ([a1f1e20](https://github.com/oocx/tfplan2md/commit/a1f1e20e8441cf80c34eda33b92106d45f8d704b))

<a name="1.13.1"></a>
## [1.13.1](https://github.com/oocx/tfplan2md/compare/v1.13.0...v1.13.1) (2026-02-08)

### 🐛 Bug Fixes

* use non-breaking space after emoji icons in Scriban templates ([37e7ca7](https://github.com/oocx/tfplan2md/commit/37e7ca7bb0745541231d581b6a32b51ec4f62d2f))

<a name="1.13.0"></a>
## [1.13.0](https://github.com/oocx/tfplan2md/compare/v1.12.0...v1.13.0) (2026-02-08)

### ✨ Features

* add pim and role policy summaries ([502024d](https://github.com/oocx/tfplan2md/commit/502024d3ad16167ff175aaa64d30c84a93768b9c))
* add private dns a record summaries ([38144cd](https://github.com/oocx/tfplan2md/commit/38144cd5719d70e65eb032b121b76666220a7f37))
* broaden azure resource id detection ([84e0d6c](https://github.com/oocx/tfplan2md/commit/84e0d6c5f0945c696dca740134ec1fb3317ae6d4))
* enrich azure display formatting ([6748677](https://github.com/oocx/tfplan2md/commit/6748677dc6fb8f4a574ffa53c5b1f6a47456b0c7))
* enrich azure scope formatting ([e4f46f5](https://github.com/oocx/tfplan2md/commit/e4f46f5f41423e4ec4568404d879a468f6c59aff))
* extend azure mapping loader and tests ([cd1b458](https://github.com/oocx/tfplan2md/commit/cd1b458f3539a778dbdc7ddc3b3ef2f39441f5a9))
* move private dns summary to azurerm factory ([0c673f0](https://github.com/oocx/tfplan2md/commit/0c673f06919d06a057e5f1c6b12798c06acdb146))
* resolve azure role definitions ([fa3e744](https://github.com/oocx/tfplan2md/commit/fa3e744be41967c462d4f2ae2711957a81b3bf34))
* track failed resolution diagnostics ([56b1178](https://github.com/oocx/tfplan2md/commit/56b1178ac754fce8b60b0f8e74d6b3b07f23d422))

### 🐛 Bug Fixes

* adjust azure display summaries ([bdbd94c](https://github.com/oocx/tfplan2md/commit/bdbd94c4ef5a7dbbe434b1233837592205925993))

### 📚 Documentation

* add ADR-006 dependency injection strategy investigation ([923cfee](https://github.com/oocx/tfplan2md/commit/923cfee5abce7faf4f36e576145ad2fb27e751a7))
* add architecture for azure display enhancements ([2bcea57](https://github.com/oocx/tfplan2md/commit/2bcea57cd78baec281964f36ca836d4abe6d3d6c))
* add code review for azure-display-enhancements ([c78a9dd](https://github.com/oocx/tfplan2md/commit/c78a9ddf99daa91fd6d2e2a4a8e69e4ca7707f06))
* add feature specification for 063-azure-display-enhancements ([6d442a2](https://github.com/oocx/tfplan2md/commit/6d442a24e5120ab0bda53ead97b6c4d68e227d17))
* add focused azure display uat artifacts ([efcbf88](https://github.com/oocx/tfplan2md/commit/efcbf8883007064afb2d491b4cd82554fdb2087e))
* add release notes for azure display enhancements ([8f619da](https://github.com/oocx/tfplan2md/commit/8f619daca63dc76320ff304a653b6a91871addb4))
* add tasks for azure display enhancements ([f866ee3](https://github.com/oocx/tfplan2md/commit/f866ee38d236fcdf0a10e15e438c9eafcfebf536))
* add test and UAT plans for azure display enhancements ([6d7344b](https://github.com/oocx/tfplan2md/commit/6d7344b9b205d3347b8c97fcf83df546c7679eed))
* add UAT report for Azure display enhancements (FAILED) ([ec1f9c8](https://github.com/oocx/tfplan2md/commit/ec1f9c8ec1bd3cfb2ec8613cb2e8331f4befc759))
* include artifact inconsistency finding in UAT report ([8996f5a](https://github.com/oocx/tfplan2md/commit/8996f5a201494d3461c81d25e6ea3a6eca96fc03))
* mark task 1 complete ([869c1e4](https://github.com/oocx/tfplan2md/commit/869c1e46dafc2e76b25182d29906736d78571a4c))
* mark task 2 complete ([1dd66d9](https://github.com/oocx/tfplan2md/commit/1dd66d9fee623b0eabd9386c2269c4d191af0984))
* mark task 3 complete ([c71d14e](https://github.com/oocx/tfplan2md/commit/c71d14e9d4293306760e44366a363a13ed0c060a))
* mark task 4 complete ([5ce33a5](https://github.com/oocx/tfplan2md/commit/5ce33a53713cdde6891c84f9ed382ee5baa1cd25))
* mark task 6 complete ([c258009](https://github.com/oocx/tfplan2md/commit/c258009376d29beb1b3f1b3de13964a1fbade391))
* refresh demo artifacts for azure display ([e881f4d](https://github.com/oocx/tfplan2md/commit/e881f4dfadf6df838015e69fdff2ac2679f51dda))
* regenerate demo artifacts for azure display enhancements ([87faf56](https://github.com/oocx/tfplan2md/commit/87faf5697e9c9bc73845b6e945f64828954a20e3))
* update demo artifacts for azure display enhancements ([cafb432](https://github.com/oocx/tfplan2md/commit/cafb4328ab3f4b65cda89d6e104391570f882be2))
* update UAT report with failure findings and retrospective ([f5e00e0](https://github.com/oocx/tfplan2md/commit/f5e00e0341c87c87dd2baa16b0cefb674e14a2bb))

<a name="1.12.0"></a>
## [1.12.0](https://github.com/oocx/tfplan2md/compare/v1.11.0...v1.12.0) (2026-02-07)

### ✨ Features

* add change icons to variable groups ([6fa6253](https://github.com/oocx/tfplan2md/commit/6fa6253cca687846792e25b80fd10c880c0b1f2f))
* add formatter and icon registries ([d18359c](https://github.com/oocx/tfplan2md/commit/d18359c5c0e93c2be804648b997305cb12532415))
* add json-based icon provider ([87303a0](https://github.com/oocx/tfplan2md/commit/87303a0c6383782ae3a587d1627309fb3a7baf09))
* add pattern-matching registry core ([277c8a1](https://github.com/oocx/tfplan2md/commit/277c8a102e9bc6101b55dd863f5ffa2a15f31548))
* add provider snapshot coverage ([2553d87](https://github.com/oocx/tfplan2md/commit/2553d87b48751a31f805bd53290f6f4a609e76e7))
* integrate registries into scriban helpers ([f79be9f](https://github.com/oocx/tfplan2md/commit/f79be9f48c4fc27717e88504c3aa2f00c9b916bd))
* migrate provider icon rules to registry ([6a1fe80](https://github.com/oocx/tfplan2md/commit/6a1fe80df77417cfd7d30cccd06dbe7f3105bb31))
* migrate VS Code agents to use askQuestions tool ([2680a37](https://github.com/oocx/tfplan2md/commit/2680a378710b41260c7de37c2cb8fbd09c190dd4))
* remove get_icon template usage ([c6ce911](https://github.com/oocx/tfplan2md/commit/c6ce911f15390025d36fb234981a4e4c9600417c))
* wire provider registries into startup ([edd49ec](https://github.com/oocx/tfplan2md/commit/edd49ec7a5988e77cbb859fbe22fc6d63e6a19f7))
* **workflow:** add workflow_dispatch to release workflow for manual triggering ([b11e481](https://github.com/oocx/tfplan2md/commit/b11e48143b4607e5372beb06ae67296ff7ca7eb2))

### 🐛 Bug Fixes

* centralize action icons and restore azuread icons ([db2a796](https://github.com/oocx/tfplan2md/commit/db2a7968a0dae171c09f301cb5cbbf293cae0532))
* prevent git credential helper hang in WSL when interop is disabled ([8f2df53](https://github.com/oocx/tfplan2md/commit/8f2df5350681dec404c8a0f219462f3d64cabcb5))
* revert ci.yml changes and remove RELEASE_TRIGGER_INSTRUCTIONS.md ([ce3c959](https://github.com/oocx/tfplan2md/commit/ce3c959d9d6d31e3e31c28f12d0c1fd2a469c46b))
* **uat:** restore 4-argument signature for validate_artifact ([2c88b38](https://github.com/oocx/tfplan2md/commit/2c88b3844636cbbd43c1c7c924403a90668123c3))
* **workflow:** move tag after commit amend in CI workflow ([5b84833](https://github.com/oocx/tfplan2md/commit/5b84833442996a06adfd390357324cc40ce60fcd))
* **workflow:** remove [skip ci] to allow release workflow to run ([c3fbed0](https://github.com/oocx/tfplan2md/commit/c3fbed03b4b25543306afc886d552430abe9f8a3))

### ♻️ Refactoring

* address code review feedback ([0de6377](https://github.com/oocx/tfplan2md/commit/0de63779312a751aa9abaddabf2e0ad4c3c1cdcd))

### 📚 Documentation

* add architecture documentation for extensible provider registry system ([e9cbca2](https://github.com/oocx/tfplan2md/commit/e9cbca283133f60ad8a9d5c82684b6c196bb5941))
* add code review for extensible-provider-registry ([880d8cc](https://github.com/oocx/tfplan2md/commit/880d8cc030adccc0b5ade525bae21fc5a30f1c17))
* add feature specification for 061-extensible-provider-registry ([bfedf2e](https://github.com/oocx/tfplan2md/commit/bfedf2ebf6350be65b0aa5af32c900d9c060de7a))
* add instructions for triggering v1.11.0 release ([a506960](https://github.com/oocx/tfplan2md/commit/a506960a54ab93b1df7eae87b8815d4eaa258f85))
* add release notes for 061-extensible-provider-registry ([7ad9ee8](https://github.com/oocx/tfplan2md/commit/7ad9ee88c08f729b20010a982909b0798dd5ab98))
* add sub-agent strategy, billing guidance, and best practices for context rot reduction ([5d96e3b](https://github.com/oocx/tfplan2md/commit/5d96e3baac3acfd7847678c70212ffe5cad59ff0))
* add tasks for extensible-provider-registry ([2e2f446](https://github.com/oocx/tfplan2md/commit/2e2f446719a80977e86b616fb000b09c0213f12c))
* add test plan and uat plan for 061-extensible-provider-registry ([4a80433](https://github.com/oocx/tfplan2md/commit/4a80433a8f956953d3d44508b8c2fb1fa8f25ac7))
* add UAT report for extensible provider registry ([9ff9cb1](https://github.com/oocx/tfplan2md/commit/9ff9cb1a854dae410c85504233f316bf967f5e76))
* clarify task tool vs task agent type naming in sub-agent documentation ([bda1081](https://github.com/oocx/tfplan2md/commit/bda10819818e37093fcf7f8f2cf4d3f840be6dcb))
* mark task 1 as complete ([62cce1a](https://github.com/oocx/tfplan2md/commit/62cce1a079c020f5ec3ea42f1c415e0990e90487))
* mark task 2 as complete ([4d95553](https://github.com/oocx/tfplan2md/commit/4d95553a605a35b33f8b47a3c42abc39ed12859c))
* mark task 3 as complete ([b69c33f](https://github.com/oocx/tfplan2md/commit/b69c33f18cbb97946d7fc9a7887198d800af088e))
* mark task 4 as complete ([5839453](https://github.com/oocx/tfplan2md/commit/58394537693fa70893f6fe8d6b154810fc63b4c2))
* mark task 5 as complete ([21da240](https://github.com/oocx/tfplan2md/commit/21da240ff2f2dc52fc542c9c48746b4ccf5312b9))
* mark task 6 as complete ([685002a](https://github.com/oocx/tfplan2md/commit/685002abd2bc13dab34145601adc2fba41c3fe4f))
* mark task 8 complete ([ec08fc4](https://github.com/oocx/tfplan2md/commit/ec08fc4d34b5233be9451e9bc83e050d9d542d1f))
* note action icon centralization ([aafea28](https://github.com/oocx/tfplan2md/commit/aafea2849e98006215176d8e737d6953c74ffe40))
* update agents.md to reference askQuestions tool ([7e1354d](https://github.com/oocx/tfplan2md/commit/7e1354d08a9d5b1ffd0777c35704bb7d608508c1))
* update architecture for extensible provider registry post-implementation review ([b9ce37a](https://github.com/oocx/tfplan2md/commit/b9ce37a38e651e2a4619110c5f36efcdbe59cabf))
* update code review — approve extensible provider registry ([5f0733c](https://github.com/oocx/tfplan2md/commit/5f0733c6b0597ed74ffaec2198f46b1f86345aaa))
* update comprehensive demo and code review artifacts ([6d5f633](https://github.com/oocx/tfplan2md/commit/6d5f633df9c0ce6385989499a1a0808744ea0f3e))
* update task 7 checklist ([2e49cb7](https://github.com/oocx/tfplan2md/commit/2e49cb7c0a05007234507b63efb207f8a8d0144e))
* **workflow:** improved instructions for chrome devtools ([985c058](https://github.com/oocx/tfplan2md/commit/985c05829335743bc36a84c01ff36555e258a7fe))

<a name="1.11.0"></a>
## [1.11.0](https://github.com/oocx/tfplan2md/compare/v1.10.0...v1.11.0) (2026-02-04)

### ✨ Features

* add custom template for azurerm_firewall_application_rule_collection ([685b37c](https://github.com/oocx/tfplan2md/commit/685b37cbcc9f8ace4649602dea8de6acc7b4e1c4))
* add Scriban template for firewall application rules ([e310a23](https://github.com/oocx/tfplan2md/commit/e310a23060cdf4590f8907ba0df8385597647ce6))
* add view model factory for firewall application rules ([7ebe723](https://github.com/oocx/tfplan2md/commit/7ebe723c7bd33d676986177b792ddf61b1620500))
* add view models for firewall application rule collection ([04052ff](https://github.com/oocx/tfplan2md/commit/04052ffc8d3751c3bc12f26b702a9eb88c8559c5))
* integrate firewall application rule factory into rendering pipeline ([b5008b3](https://github.com/oocx/tfplan2md/commit/b5008b3a772379e5fe30583d9bfdfe0109e12f44))

### 🐛 Bug Fixes

* add FirewallApplicationRuleCollection mapping to AotScriptObjectMapper ([88161b3](https://github.com/oocx/tfplan2md/commit/88161b3317ae3cb7ff00216890c4440001a2fb70))
* handle number values in GetString for protocol port parsing ([42cc49c](https://github.com/oocx/tfplan2md/commit/42cc49c9d6bb42d518651e3d470d034759e87466))
* use correct 'protocols' property name in firewall application rule factory ([df9f0e6](https://github.com/oocx/tfplan2md/commit/df9f0e6d47c5fcd1063add8da3b01018e6253970))

### 📚 Documentation

* add architecture for azurerm_firewall_application_rule_collection template ([941026b](https://github.com/oocx/tfplan2md/commit/941026b08ed9c455e5ae60cbd55bec8e34f440c4))
* add azurerm_firewall_application_rule_collection to documentation ([74a402e](https://github.com/oocx/tfplan2md/commit/74a402e1bc6bea0fee55ff8bdfee0c916f9d7525))
* add code review for azurerm_firewall_application_rule_collection feature ([1cd0aee](https://github.com/oocx/tfplan2md/commit/1cd0aee3ad2a3267496d0e3034773781ad072c5f))
* add feature specification for 060-azurerm-firewall-application-rule-template ([2882198](https://github.com/oocx/tfplan2md/commit/2882198040ef624269d4b7ac44c08010eed78d28))
* add release notes for azurerm_firewall_application_rule_collection ([a989b58](https://github.com/oocx/tfplan2md/commit/a989b584f589c2c87172ac9072eccd3b4dc7e504))
* add test plan and UAT plan for firewall application rule template ([4cb40e5](https://github.com/oocx/tfplan2md/commit/4cb40e5a2931616b8033e472380b01b49bdedda2))
* approve azurerm_firewall_application_rule_collection after protocol fix ([e714af1](https://github.com/oocx/tfplan2md/commit/e714af1b2212e6b0dea6cc2c0e04a33d7f796d78))
* regenerate demo artifacts after protocol fix ([6bc65a6](https://github.com/oocx/tfplan2md/commit/6bc65a6b5cbe4d7748f0a0ff04f9cea603b4cc27))

<a name="1.10.0"></a>
## [1.10.0](https://github.com/oocx/tfplan2md/compare/v1.9.0...v1.10.0) (2026-02-04)

### ✨ Features

* add Tool column to code analysis findings tables ([35ffcf9](https://github.com/oocx/tfplan2md/commit/35ffcf98fa5e6a778613ebb8186163a5f0c7bd66))
* update agents for dual UAT artifacts workflow ([dc0c66d](https://github.com/oocx/tfplan2md/commit/dc0c66d8c549813a17ac3e1470ac8487c148e0a8))
* **059:** Add architecture design for tool column feature ([9f678ce](https://github.com/oocx/tfplan2md/commit/9f678ce6ea455868bb2ef7c9f6470009fc7c5506))
* **agents:** enhance code reviewer strictness with skeptical review mindset ([fb0cce9](https://github.com/oocx/tfplan2md/commit/fb0cce955c1acbc49e7b95b77b70fdd731fa7878))

### 🐛 Bug Fixes

* handle empty tool names correctly in templates ([6bdea73](https://github.com/oocx/tfplan2md/commit/6bdea734a2c5b901c8c595aa6ab3d091569ba996))
* **screenshots:** make GitHub light-mode screenshots actually light ([f22eea4](https://github.com/oocx/tfplan2md/commit/f22eea4a4207b5fd8764032e41a88a7ba7ebd71a))

### ♻️ Refactoring

* **agents:** address code review feedback on strictness language ([5376d9c](https://github.com/oocx/tfplan2md/commit/5376d9ce39ba7921e655d7ffc305211370bb8bca))

### 📚 Documentation

* add code review report for Tool column feature ([782d31b](https://github.com/oocx/tfplan2md/commit/782d31b27fdf8b22c23b252dd2f6a39d209c7b75))
* add multi-model-review findings ([35f478c](https://github.com/oocx/tfplan2md/commit/35f478c9d3ccfdef78db623e4a8a50a303b9bc37))
* add release notes for Tool column feature ([d3d25f5](https://github.com/oocx/tfplan2md/commit/d3d25f5462e2b397dfb2a4d2180c1ca554c5e70a))
* add test plan and UAT test plan for feature 059 tool column ([4a09a5e](https://github.com/oocx/tfplan2md/commit/4a09a5edfafc1e62a3b3b20d32f6b9e41076c14f))
* finalize architecture for tool column in findings tables ([a20e635](https://github.com/oocx/tfplan2md/commit/a20e635b1ee0e25ee4f64c861207bc57b9af636b))
* generate demo artifacts with Tool column feature ([91161ad](https://github.com/oocx/tfplan2md/commit/91161ada078e4913edc1d7f4bf0e4543123312ae))
* mark all tasks complete for Tool column feature ([c2a4a11](https://github.com/oocx/tfplan2md/commit/c2a4a11daee98fbe3ffb818354e86a4f7a2f2977))
* update documentation for dual UAT artifacts workflow ([47ac3a2](https://github.com/oocx/tfplan2md/commit/47ac3a223e612f9cac4f03ccd7a944e89668753e))
* update features.md with Tool column documentation ([e2e1614](https://github.com/oocx/tfplan2md/commit/e2e161476ec776f28b349624adc6bbe3538651b4))
* **059:** Add implementation tasks breakdown ([23ef199](https://github.com/oocx/tfplan2md/commit/23ef19901222e599e864b71991953f4c70dfddf0))

<a name="1.9.0"></a>
## [1.9.0](https://github.com/oocx/tfplan2md/compare/v1.8.0...v1.9.0) (2026-02-03)

### ✨ Features

* enhance Release Manager to generate user-focused release notes ([94c28f4](https://github.com/oocx/tfplan2md/commit/94c28f40e0120858493fcb18a6405f667b6604ad))

### 🐛 Bug Fixes

* address NSG rendering and escaping issues found in UAT - SNAPSHOT_UPDATE_OK ([ca27b9b](https://github.com/oocx/tfplan2md/commit/ca27b9b1116230c431b727765fab108308d71438))
* align azurerm template fallbacks ([bed8e64](https://github.com/oocx/tfplan2md/commit/bed8e64f945b02f384d73208ce0ba890ca5081d1))

### 📚 Documentation

* add code review for NSG rendering issues ([0cc4c2c](https://github.com/oocx/tfplan2md/commit/0cc4c2cdd44d1ac9713617fa1b387e3e1e2895b8))
* add example user-focused release notes for feature 057 ([c682e36](https://github.com/oocx/tfplan2md/commit/c682e36291e198f9ec65093eef76c8c436962457))
* add focused UAT artifacts for 058 - SNAPSHOT_UPDATE_OK ([29dca9d](https://github.com/oocx/tfplan2md/commit/29dca9dee9ed926b51a6d76ce2a61e578d7532cb))
* add issue analysis for NSG rendering issues ([14a973a](https://github.com/oocx/tfplan2md/commit/14a973aa33124914ba4ef0aa56367a0a8dc9d3b5))
* add UAT report for issue 058 (FALIED) ([6d80d58](https://github.com/oocx/tfplan2md/commit/6d80d58c48334b1f501c3e9093495e9ba2c8de30))
* add user-focused release notes for NSG rendering improvements ([dc03e6c](https://github.com/oocx/tfplan2md/commit/dc03e6c28c0e26f068bc191cb8f1150976c4cb69))
* regenerate artifacts for 058 UAT - SNAPSHOT_UPDATE_OK ([9b8ef5a](https://github.com/oocx/tfplan2md/commit/9b8ef5a12c6c067554942d512e3ff09342a32422))
* update code review for NSG rendering issues - approved ([af085f4](https://github.com/oocx/tfplan2md/commit/af085f40fd9bdd761bf4f0688362ccec04061e00))
* update CONTRIBUTING.md to explain release notes process ([e7c6038](https://github.com/oocx/tfplan2md/commit/e7c6038b87dfaa61d54eb89547c5e046509ba72f))
* update UAT report for issue 058 (PASSED) ([57aff0a](https://github.com/oocx/tfplan2md/commit/57aff0a864f8f27f916327b4b95f2920cef6822a))

<a name="1.8.0"></a>
## [1.8.0](https://github.com/oocx/tfplan2md/compare/v1.7.0...v1.8.0) (2026-02-01)

### ✨ Features

* add refactoring metadata to report model ([dce7095](https://github.com/oocx/tfplan2md/commit/dce7095890bc45fcc2ea5a12ebdb25a6780867ab))
* annotate summary lines for refactoring ([d20ec23](https://github.com/oocx/tfplan2md/commit/d20ec23c02e9f6bb903e0b3aa3d6645603658a97))
* improve refactoring summary details ([5e8f0e0](https://github.com/oocx/tfplan2md/commit/5e8f0e043f04a8219fa55fce75953f76a6beec49))
* parse import and moved metadata ([83fb1a1](https://github.com/oocx/tfplan2md/commit/83fb1a139fe7f946b1d8ce7318e31768cc6feb9c))
* raise Scriban loop limit ([7c38c1b](https://github.com/oocx/tfplan2md/commit/7c38c1b6257430275086c5d55e53184b2750575a))
* render refactoring summary section ([2c2c4a4](https://github.com/oocx/tfplan2md/commit/2c2c4a40940caba0be708999bc08db6f5a570ad7))

### 🐛 Bug Fixes

* align refactoring summary table formatting ([1b21e8f](https://github.com/oocx/tfplan2md/commit/1b21e8fc64c4742fccdf94d5c36baa4de700dd5f))

### 📚 Documentation

* add architecture for terraform refactoring visibility ([7aef9f7](https://github.com/oocx/tfplan2md/commit/7aef9f7154da039dc3b2ed5a07be8891d4e1876d))
* add code review for terraform-import-moved-blocks ([95bc614](https://github.com/oocx/tfplan2md/commit/95bc61414bd6634ce8b3ca5707b3be39e5625be9))
* add feature specification for 057-terraform-import-moved-blocks ([b3d3ceb](https://github.com/oocx/tfplan2md/commit/b3d3cebaef54d3264ace258509a4ac7b053a1e51))
* add tasks for terraform import and moved blocks ([eb1353f](https://github.com/oocx/tfplan2md/commit/eb1353f25452c4dbe41348600517b3a3fecb35a7))
* add test plan for 057-terraform-import-moved-blocks ([add57ed](https://github.com/oocx/tfplan2md/commit/add57ed5ee27250d98e455f410a49aafbac4b2f5))
* add UAT report for Terraform import and moved blocks ([2e2479b](https://github.com/oocx/tfplan2md/commit/2e2479bd6c2782fdb53313fc18eca356f8cb7fdf))
* mark task 1 parsing complete ([2c0f8b9](https://github.com/oocx/tfplan2md/commit/2c0f8b98e720f46fe609eab0d8a8baf450cd269b))
* mark task 2 loop limit complete ([edd21e8](https://github.com/oocx/tfplan2md/commit/edd21e891c16954f0686fd56db04c3b864f54336))
* mark task 3 report model complete ([3cbf291](https://github.com/oocx/tfplan2md/commit/3cbf291bbf9732772230762204014cfa92cd90a6))
* mark task 4 summary html complete ([4a97353](https://github.com/oocx/tfplan2md/commit/4a973531acfba707317328e2ffce95dede3b0767))
* mark task 5 templates complete ([c732351](https://github.com/oocx/tfplan2md/commit/c73235153d149a9061d90d0f8cac1556235c6a00))
* mark UAT task complete ([58d56bc](https://github.com/oocx/tfplan2md/commit/58d56bc6fdc313a7e9c7ad9d1cae7cda3b69d24e))
* regenerate demo artifacts ([e339e9c](https://github.com/oocx/tfplan2md/commit/e339e9c60f60f0006c7114cebf7e1368a4608686))
* regenerate demo artifacts for refactoring ([967fdd6](https://github.com/oocx/tfplan2md/commit/967fdd687430cb1fe5c266a054d20ad07c1c34a5))
* regenerate demo artifacts for refactoring ([ca87097](https://github.com/oocx/tfplan2md/commit/ca87097ef125cfc5b51a850f42098bb38a6c3590))
* update code review with blocker for missing code analysis ([5ab8c32](https://github.com/oocx/tfplan2md/commit/5ab8c324f800f3450440ddbfb33be528512605e1))
* update demo artifacts for refactoring summary ([3fce43c](https://github.com/oocx/tfplan2md/commit/3fce43cb10ed569bf5d357167431de6b0baee247))

<a name="1.7.0"></a>
## [1.7.0](https://github.com/oocx/tfplan2md/compare/v1.6.0...v1.7.0) (2026-02-01)

### ✨ Features

* add --open-details argument to screenshot generator ([9acc96b](https://github.com/oocx/tfplan2md/commit/9acc96b70ec3611f7f39a8da5c12001bbf383cde))
* add fail-on code analysis exit codes ([bc233c1](https://github.com/oocx/tfplan2md/commit/bc233c193a565dfa90fe76b81c8d0ddaba141140))
* add module-level and global static analysis findings support ([cd2d35a](https://github.com/oocx/tfplan2md/commit/cd2d35aa4647c3c9395b87448b5fca7a28001473))
* add retrospective documentation for static code analysis integration ([6d9eb52](https://github.com/oocx/tfplan2md/commit/6d9eb5294a3c539d540e08fd9d64c9f82032db9b))
* add SARIF parser foundation ([37eb4cc](https://github.com/oocx/tfplan2md/commit/37eb4cc3bed86f533915cb203b84621695c7fd6f))
* add static analysis UAT artifact and example SARIF files ([9a54a44](https://github.com/oocx/tfplan2md/commit/9a54a44326c534ab18b4f582426ecf9c8c5d9f8a))
* include code analysis findings in all comprehensive demo artifacts ([57e73e0](https://github.com/oocx/tfplan2md/commit/57e73e00ae9fc6542cda21751280c99b0898a780))
* integrate code analysis into report model ([fab2ece](https://github.com/oocx/tfplan2md/commit/fab2ece72caff3926c3125c7c7a13afb08fa3a45))
* integrate static analysis tools and update release process documentation ([e99e52a](https://github.com/oocx/tfplan2md/commit/e99e52a0ebe1602602b3c290b5ae36137f489705))
* map code analysis severity and resources ([982a4cd](https://github.com/oocx/tfplan2md/commit/982a4cd88ddc95d1a8351295f3848714deb99d1c))
* refine code analysis findings layout ([301e913](https://github.com/oocx/tfplan2md/commit/301e91350d1df01e556cff6bf051052475192768))
* regenerate static analysis comprehensive demo artifact ([c956017](https://github.com/oocx/tfplan2md/commit/c956017c1a6ce0fe6a85c08e4175ea95f6e0a07d))
* render code analysis findings ([3333a8c](https://github.com/oocx/tfplan2md/commit/3333a8c04e76e72291ba71afd9a5e44771108ab4))
* render other findings and warnings ([8b6cb9d](https://github.com/oocx/tfplan2md/commit/8b6cb9dd51c7052f8e13ea0fc5929bbde96fdb82))
* update all artifacts after regeneration ([fe7e179](https://github.com/oocx/tfplan2md/commit/fe7e179512aea270228bfb88201eaf67cbe02518))
* update artifacts and code analysis models ([edca9f1](https://github.com/oocx/tfplan2md/commit/edca9f15f2371c9cfa2b49c48f4e8522cb0b0dbe))
* **cli:** implement static analysis CLI flags and wildcard expansion\n\n- Add --code-analysis-results, --code-analysis-minimum-level, --fail-on-static-code-analysis-errors flags\n- Implement wildcard expansion utility for SARIF patterns\n- Add and fix tests for CLI and wildcard logic\n- Mark Task 2 as complete in static analysis integration feature\n\nRelated: docs/features/037-static-analysis-integration/specification.md ([123641d](https://github.com/oocx/tfplan2md/commit/123641d75df326c9980e776bc7b5abaaed70eee6))
* **website:** add v1.6.0 static code analysis feature and Azure AD enhancements ([d99a690](https://github.com/oocx/tfplan2md/commit/d99a6908f5b8a0b056b0c0fd1eac1dbafcba9386))

### 🐛 Bug Fixes

* address static analysis rendering issues ([d68e1c3](https://github.com/oocx/tfplan2md/commit/d68e1c381a036bc992ed7a7ce1f9cf6cb3c9a4e4))
* clean corrupted SARIF files (remove concatenated AWS content) ([df0d3c3](https://github.com/oocx/tfplan2md/commit/df0d3c3899ff148d1aa2bdaf1832e358d4d8d12c))
* handle recursive wildcard patterns ([daf19a9](https://github.com/oocx/tfplan2md/commit/daf19a99ba0865798bfabe36affdafa10b26a74d))
* remove blank line in findings table ([ad6daea](https://github.com/oocx/tfplan2md/commit/ad6daea7f3f0a8a851012331d5daf6d017d0023e))
* restore interactive example controls for static-analysis summary view ([fd8d40d](https://github.com/oocx/tfplan2md/commit/fd8d40dcb94b4fae28285c3b90a1acda8fd512af))
* **hooks:** add solution path to pre-commit format and build tasks ([0323102](https://github.com/oocx/tfplan2md/commit/0323102dd4f1f76cb81f841f3a2e194400733ab6))
* **website:** address 6 issues identified in website v1.6.0 updates ([9b28c80](https://github.com/oocx/tfplan2md/commit/9b28c80bf1ae017ce89d95b5cf53e21b391baed8))

### 📚 Documentation

* add architecture for static analysis integration ([2eeedf7](https://github.com/oocx/tfplan2md/commit/2eeedf7021fd188fd75d73555b8b9699cd2e6a68))
* add code analysis example ([f589f17](https://github.com/oocx/tfplan2md/commit/f589f1783ac40cdc0cd4755e018b195ee7ffb787))
* add code review for static-analysis-integration ([7ffb2d4](https://github.com/oocx/tfplan2md/commit/7ffb2d4eb89d825e6526b6f855ced755debbbd38))
* add feature specification for 056-static-analysis-integration ([d0a7766](https://github.com/oocx/tfplan2md/commit/d0a77668f889c583119282058f3d671288f4fc33))
* add tasks for static-analysis-integration ([250b06a](https://github.com/oocx/tfplan2md/commit/250b06a43458d10698a2e508ef4f92197c32c78d))
* add test plans for 056-static-analysis-integration ([720db74](https://github.com/oocx/tfplan2md/commit/720db74c0b89060eddf48bfd799edb51a04645cc))
* add UAT report for static-analysis-integration (FAILED) ([aba356b](https://github.com/oocx/tfplan2md/commit/aba356b5e6a7f77661e1c6f8d3fc7b01d0c4bd4e))
* add UAT report for static-analysis-integration (FAILED) ([2282ab5](https://github.com/oocx/tfplan2md/commit/2282ab5f77ad3254ba9ace7723922183b70fb352))
* add UAT report for static-analysis-integration (FAILED) ([d9dc5f4](https://github.com/oocx/tfplan2md/commit/d9dc5f43b2f96dbc91258a6cd3967624f90f3d89))
* add UAT report for static-analysis-integration (PASSED) SNAPSHOT_UPDATE_OK ([be002fe](https://github.com/oocx/tfplan2md/commit/be002fe48a03fe5ad43f8e19c2416ac18dc70f35))
* mark Task 1 complete ([c9b0b24](https://github.com/oocx/tfplan2md/commit/c9b0b243cdf8c8c380267534dfb127ed37ecbdca))
* mark task 3 as complete ([c9710a4](https://github.com/oocx/tfplan2md/commit/c9710a4985292df671e4ddd3a0b1afbc9e9bf937))
* mark task 4 as complete ([615de7a](https://github.com/oocx/tfplan2md/commit/615de7a1aec39f37d43926760c8f959853a20171))
* mark task 5 as complete ([44c3bcb](https://github.com/oocx/tfplan2md/commit/44c3bcbee35286103962cd0100770fc7de550650))
* mark task 8 as complete ([8ecccc1](https://github.com/oocx/tfplan2md/commit/8ecccc19081208d67561824addfdf5addfdf7aa0))
* mark task 9 as complete ([0a80a35](https://github.com/oocx/tfplan2md/commit/0a80a3533fa19bd430028c59994f1f26006ec592))
* mark tasks 6 and 7 as complete ([1e0aad0](https://github.com/oocx/tfplan2md/commit/1e0aad0cb1dfdd16b998d3fb850be5978789b414))
* regenerate all demo artifacts ([9af23f2](https://github.com/oocx/tfplan2md/commit/9af23f29a0ac0c1320bd9eeffc9a3dcd16ba14e3))
* update screenshot generation documentation ([cf85c75](https://github.com/oocx/tfplan2md/commit/cf85c75b108e7486dcb408d5aabdeea29cae83da))

<a name="1.6.0"></a>
## [1.6.0](https://github.com/oocx/tfplan2md/compare/v1.5.1...v1.6.0) (2026-01-31)

### ✨ Features

* add fail-on code analysis exit codes ([b3a2c95](https://github.com/oocx/tfplan2md/commit/b3a2c95d169278e996e6a48f7065241e22b9d83a))
* add module-level and global static analysis findings support ([00f8748](https://github.com/oocx/tfplan2md/commit/00f8748a0392a0551ce71dc206aed7d9b5b4901e))
* add SARIF parser foundation ([7ab5373](https://github.com/oocx/tfplan2md/commit/7ab5373040c6eee784f6f80cb4064270099d7e5c))
* add static analysis UAT artifact and example SARIF files ([36de4dc](https://github.com/oocx/tfplan2md/commit/36de4dc506836ca095f03ed3dc3cbcce173c4e1d))
* include code analysis findings in all comprehensive demo artifacts ([168e997](https://github.com/oocx/tfplan2md/commit/168e9976d267b980bcf670a1ac08a81626c87b4a))
* integrate code analysis into report model ([8688e63](https://github.com/oocx/tfplan2md/commit/8688e639ead101bcd8c36f35273fe5ec2092d379))
* integrate static analysis tools and update release process documentation ([c38a5c8](https://github.com/oocx/tfplan2md/commit/c38a5c89f7fabc11025fd68d24adf9b1d0bc7813))
* map code analysis severity and resources ([88335bd](https://github.com/oocx/tfplan2md/commit/88335bdca42da856ec8717ee54562f390bfc7ab2))
* refine code analysis findings layout ([5b5ffc4](https://github.com/oocx/tfplan2md/commit/5b5ffc49c9d305917eca196539bd37113f75d456))
* regenerate static analysis comprehensive demo artifact ([5c7fd57](https://github.com/oocx/tfplan2md/commit/5c7fd5741ecb9519a9ea7b2501b34ea9b2089501))
* render code analysis findings ([f276ff8](https://github.com/oocx/tfplan2md/commit/f276ff806d1cb56d2c7002107e6179a405ce95f7))
* render other findings and warnings ([ab40cda](https://github.com/oocx/tfplan2md/commit/ab40cdab8c34e1c099f7259eadefc29b3c3ea03f))
* update all artifacts after regeneration ([62d0d08](https://github.com/oocx/tfplan2md/commit/62d0d0868161b4eb3ce34c36a7288bbbd36b2004))
* update artifacts and code analysis models ([4ddc8f7](https://github.com/oocx/tfplan2md/commit/4ddc8f74b5c4ff4cb02a8e9b58834b410455f88e))
* **cli:** implement static analysis CLI flags and wildcard expansion\n\n- Add --code-analysis-results, --code-analysis-minimum-level, --fail-on-static-code-analysis-errors flags\n- Implement wildcard expansion utility for SARIF patterns\n- Add and fix tests for CLI and wildcard logic\n- Mark Task 2 as complete in static analysis integration feature\n\nRelated: docs/features/037-static-analysis-integration/specification.md ([8a9f5f4](https://github.com/oocx/tfplan2md/commit/8a9f5f40c2a3f7664706dfa712654ad568765ca6))

### 🐛 Bug Fixes

* address static analysis rendering issues ([f5a5bcb](https://github.com/oocx/tfplan2md/commit/f5a5bcb40f16c9bacca5584e3dca3bddfb029fd3))
* clean corrupted SARIF files (remove concatenated AWS content) ([8af7277](https://github.com/oocx/tfplan2md/commit/8af7277d98e43f4c276f28abf14fa8ff2efb3fae))
* handle recursive wildcard patterns ([fb7642e](https://github.com/oocx/tfplan2md/commit/fb7642ec52bb0b8022d52067162b20a158035cef))
* remove blank line in findings table ([23e4ae7](https://github.com/oocx/tfplan2md/commit/23e4ae734dbce832d71e95070291349aa3b509c4))

### 📚 Documentation

* add architecture for static analysis integration ([61bf904](https://github.com/oocx/tfplan2md/commit/61bf90461ce906d4c2126efd4b97fafb6bf4aab2))
* add code analysis example ([782b17c](https://github.com/oocx/tfplan2md/commit/782b17c5502974f5eae99b7530da211b789f785e))
* add code review for static-analysis-integration ([bc8b014](https://github.com/oocx/tfplan2md/commit/bc8b014defad5b795f9de17680362b1d61ff2f4c))
* add feature specification for 056-static-analysis-integration ([28a17cd](https://github.com/oocx/tfplan2md/commit/28a17cd9e16131558d9234932ac22f8af098d80d))
* add tasks for static-analysis-integration ([f0d4023](https://github.com/oocx/tfplan2md/commit/f0d40238975bb611f561fbda7eba2408822dc3f7))
* add test plans for 056-static-analysis-integration ([ac223b6](https://github.com/oocx/tfplan2md/commit/ac223b6db86d9e4fd203a045fb13374ec7919b74))
* add UAT report for static-analysis-integration (FAILED) ([a910b82](https://github.com/oocx/tfplan2md/commit/a910b82f06d0d947ad18f27f3c032fb61b4af4a6))
* add UAT report for static-analysis-integration (FAILED) ([5ddd2ed](https://github.com/oocx/tfplan2md/commit/5ddd2edd8023ef36977f4b0dfc4e33fb356199a5))
* add UAT report for static-analysis-integration (FAILED) ([4e948c8](https://github.com/oocx/tfplan2md/commit/4e948c8ae2f28592d9a780689a58c9a1f6e3e510))
* add UAT report for static-analysis-integration (PASSED) SNAPSHOT_UPDATE_OK ([7a07bc2](https://github.com/oocx/tfplan2md/commit/7a07bc2ccd1bb78fc10dfbf1e66601ee38a3310f))
* mark Task 1 complete ([b6b4aa3](https://github.com/oocx/tfplan2md/commit/b6b4aa3e6fc52f540666449b44d335e73a92ab47))
* mark task 3 as complete ([d2c8532](https://github.com/oocx/tfplan2md/commit/d2c853225bfd361f6ce539588c3e715b80078cfd))
* mark task 4 as complete ([b68b48c](https://github.com/oocx/tfplan2md/commit/b68b48c7bbd11afe642aac0b184adbf67c528212))
* mark task 5 as complete ([2342995](https://github.com/oocx/tfplan2md/commit/2342995bdd964b940c60946de297e3859bab6a3b))
* mark task 8 as complete ([8f0a7cc](https://github.com/oocx/tfplan2md/commit/8f0a7cce1fa430924507c99a06da7ed8a5353464))
* mark task 9 as complete ([bd32b17](https://github.com/oocx/tfplan2md/commit/bd32b17396c8a3f73e40daa6fbfe1c069b191503))
* mark tasks 6 and 7 as complete ([0615fae](https://github.com/oocx/tfplan2md/commit/0615fae6a280ea48d23bb008db811d2d2dbb1673))
* regenerate all demo artifacts ([f3e63cf](https://github.com/oocx/tfplan2md/commit/f3e63cf22d5c4775b914b741eef8d9abfa2af062))

<a name="1.5.1"></a>
## [1.5.1](https://github.com/oocx/tfplan2md/compare/v1.5.0...v1.5.1) (2026-01-29)

### 🐛 Bug Fixes

* **ci:** publish coverage history outside PRs ([97b691e](https://github.com/oocx/tfplan2md/commit/97b691e88ec7c40ee9b844d18508dcbff8591a1f))

### 📚 Documentation

* update coverage badge and history ([636a446](https://github.com/oocx/tfplan2md/commit/636a446b5ddeca385a49937319079bfe803efc28))

<a name="1.5.0"></a>
## [1.5.0](https://github.com/oocx/tfplan2md/compare/v1.4.0...v1.5.0) (2026-01-29)

### ✨ Features

* add azure ad helper formatting ([7bbfa94](https://github.com/oocx/tfplan2md/commit/7bbfa9474d8b932878876b2bafb61a844b031ca8))
* add Azure AD provider module ([d9b5364](https://github.com/oocx/tfplan2md/commit/d9b53644a5f1a1dfab093cbba2a6c236b5a69153))
* add azuread group member counts ([14ebe9b](https://github.com/oocx/tfplan2md/commit/14ebe9b61426d0d680d27a231974b8d1f80bcf03))
* add azuread group member template ([bcdab29](https://github.com/oocx/tfplan2md/commit/bcdab293b96bd2e24b3d3b26df91bc9d1d66d5a8))
* add azuread group without members template ([82ce0ff](https://github.com/oocx/tfplan2md/commit/82ce0fff3d14ab61bba8bd7256b283b4d5cb197d))
* add azuread invitation template ([988ee14](https://github.com/oocx/tfplan2md/commit/988ee1452dd3410db05dc089db97227fbaa92360))
* add azuread service principal template ([b897ea1](https://github.com/oocx/tfplan2md/commit/b897ea10cb302f2b7b479a91685a831d71f855a4))
* add azuread user template ([8ef0836](https://github.com/oocx/tfplan2md/commit/8ef08365d903477a3e731dffd3f8704435e21cc0))

### ♻️ Refactoring

* remove azuread summary mappings ([0cb99f5](https://github.com/oocx/tfplan2md/commit/0cb99f5e989e92658799adc1a5022ce18c259a79))

### 📚 Documentation

* add architecture for 053-azuread-resources-enhancements ([cafe892](https://github.com/oocx/tfplan2md/commit/cafe892dc024135a7c8628ae890ecf8f4a07326c))
* add azuread demo assets ([96f7dec](https://github.com/oocx/tfplan2md/commit/96f7decb4635b68ebcf62ab8b127ff09f3f33267))
* add code review for azuread-resources-enhancements ([581fa39](https://github.com/oocx/tfplan2md/commit/581fa3998ac5448f3c2798636ef94ee68044b4af))
* add feature specification for 053-azuread-resources-enhancements ([839ee24](https://github.com/oocx/tfplan2md/commit/839ee24492410f370eaf133fb1ac9083abdcab1e))
* add tasks for enhanced azure ad resource display ([2782afe](https://github.com/oocx/tfplan2md/commit/2782afec4a8f5e87e6ac639f015ab31f662e9102))
* add test plans for azuread-resources-enhancements ([fe247a2](https://github.com/oocx/tfplan2md/commit/fe247a28f375558eb4a69675f65e5825764908e3))
* add UAT report for Azure AD resource enhancements ([86f17d0](https://github.com/oocx/tfplan2md/commit/86f17d01016f153817f52dbd9282d4ab03904e6c))
* mark task 1 complete ([e33413e](https://github.com/oocx/tfplan2md/commit/e33413ef231f773ed77e323ba3bea81a2c5662a9))
* mark task 10 complete ([1f9f96a](https://github.com/oocx/tfplan2md/commit/1f9f96a96164245517015b851a8d60ee3f018516))
* mark task 2 complete ([b3474c1](https://github.com/oocx/tfplan2md/commit/b3474c100f623d4c8bdd45d654c4e45597bad7d5))
* mark task 3 complete ([38e5ec4](https://github.com/oocx/tfplan2md/commit/38e5ec4665aa783723579af148aca5ea833c3dca))
* mark task 4 complete ([6746c44](https://github.com/oocx/tfplan2md/commit/6746c446d9669eda06ce75a9a49a542bbb001111))
* mark task 5 complete ([924aa59](https://github.com/oocx/tfplan2md/commit/924aa599532c866b2b03b767b4f693711a5be4ff))
* mark task 6 complete ([09a4b88](https://github.com/oocx/tfplan2md/commit/09a4b88272b1ab1917a26030498cdde9ff74a9d3))
* mark task 7 complete ([9a48106](https://github.com/oocx/tfplan2md/commit/9a48106af43fcaef379b84e98d4e94da93e887cd))
* mark task 8 complete ([554472f](https://github.com/oocx/tfplan2md/commit/554472f9da704baa318f558cbba002962aa9a878))
* mark task 9 complete ([93ad460](https://github.com/oocx/tfplan2md/commit/93ad460b2c0f511b15f2fa5ca7ea0c12331f4b0f))
* update coverage badge and history ([7e8f412](https://github.com/oocx/tfplan2md/commit/7e8f41292adc14dc2353ea1839890754c9f6bd32))
* update demo artifacts for azuread enhancements ([1444994](https://github.com/oocx/tfplan2md/commit/1444994e94ffb7eda277ee78b222badefeaed72a))
* update documentation for azuread enhancements ([338291d](https://github.com/oocx/tfplan2md/commit/338291d8d9cb4d2db7ffcb9a6d089d213ea58e04))

<a name="1.4.0"></a>
## [1.4.0](https://github.com/oocx/tfplan2md/compare/v1.3.0...v1.4.0) (2026-01-28)

### ✨ Features

* simplify interactive UAT flow ([4a63f7b](https://github.com/oocx/tfplan2md/commit/4a63f7b243c4911fa8a7123d669a5e9a02578001))

### 📚 Documentation

* **retro:** add metrics and evidence for feature 051 retrospective ([34ab7d0](https://github.com/oocx/tfplan2md/commit/34ab7d0400373db6fd395363b6a96cff60ec0c65))
* **retro:** finalize movement of technical writer chat log ([278908d](https://github.com/oocx/tfplan2md/commit/278908dea12b5c6c8d8195ba6c989f00f69a9b5a))
* **retro:** finalize retrospective for feature 051 with linked issues ([2fea46e](https://github.com/oocx/tfplan2md/commit/2fea46efa2ec4bafe691733749b514b993001cd9))
* **retro:** move and stage technical writer chat log (forced) ([a1c392c](https://github.com/oocx/tfplan2md/commit/a1c392c55f33a3cf7eb2d59581ce004a822b99af))

<a name="1.3.0"></a>
## [1.3.0](https://github.com/oocx/tfplan2md/compare/v1.2.0...v1.3.0) (2026-01-27)

### ✨ Features

* add subscription attribute emoji formatting ([db0d5f3](https://github.com/oocx/tfplan2md/commit/db0d5f38877073bf40d3042e5f35aadaae1dd751))
* enrich apim summary html ([82797ac](https://github.com/oocx/tfplan2md/commit/82797ac53a92dde746e92661b89a0d12d2b7541e))
* fix display enhancements regressions ([25755c2](https://github.com/oocx/tfplan2md/commit/25755c2e3535037c9b5cbebb141860d8313a5c7d))
* highlight large json and xml values ([a0f0d07](https://github.com/oocx/tfplan2md/commit/a0f0d0704a4852046bf9319c0ff338785c71b153))
* honor apim named value secret flag ([15e8afb](https://github.com/oocx/tfplan2md/commit/15e8afba33c2f31e5bfdad03d5750eff7f34cdb6))
* move apim summaries to azurerm factories ([6cf9e3f](https://github.com/oocx/tfplan2md/commit/6cf9e3f49dc8510ee6f550ed310e87937adeba1b))

### 🐛 Bug Fixes

* **test:** remove invalid returns tag to fix CI blocker ([af51057](https://github.com/oocx/tfplan2md/commit/af51057025edba93db6509dec70c94da16fc79b5))

### ♻️ Refactoring

* reduce semantic formatting complexity ([4e35912](https://github.com/oocx/tfplan2md/commit/4e35912093d28adc6718e2442d5d03d7152345b8))

### 📚 Documentation

* add architecture for 051-display-enhancements ([df387a6](https://github.com/oocx/tfplan2md/commit/df387a6796241cfd5e5f3a356545391a05d14d70))
* add code review for display-enhancements (Changes Requested) ([815f700](https://github.com/oocx/tfplan2md/commit/815f700f5d1cb5594ca251b07010eb408dac70b8))
* add feature specification for 051-display-enhancements ([aabf933](https://github.com/oocx/tfplan2md/commit/aabf93343d8caebce69532a7d18b6a45e335d97b))
* add retrospective and metrics for azapi-attribute-grouping ([0e8928f](https://github.com/oocx/tfplan2md/commit/0e8928fa96f644fcb7ef4ce84e152acaf57e7e48))
* add tasks for display-enhancements ([b370153](https://github.com/oocx/tfplan2md/commit/b37015347df7ed8b084b04ac2f5402fbcb171c95))
* add test plan and UAT test plan for 051-display-enhancements ([c91f9ed](https://github.com/oocx/tfplan2md/commit/c91f9ede929753032816d0c050405820b58e6c73))
* add UAT report for display-enhancements (Failed) ([6316d7b](https://github.com/oocx/tfplan2md/commit/6316d7b494f21bd2d7b667c7768e481c65a3117e))
* add UAT report for display-enhancements (Passed) ([d252837](https://github.com/oocx/tfplan2md/commit/d252837038aa412f1bb2a154106be9b5beae4ba8))
* add updated test plan and UAT test plan for 051-display-enhancements ([dafe752](https://github.com/oocx/tfplan2md/commit/dafe752a07e18e33c8f3338a3488b12e4a676885))
* clarify UAT handling for display enhancements ([e1e35d2](https://github.com/oocx/tfplan2md/commit/e1e35d2d340305d4f165c6962664ca598b9b40fe))
* mark task 1 as complete ([912a523](https://github.com/oocx/tfplan2md/commit/912a52362c0fa1b701b1df1beaad4ab0f9501144))
* mark task 2 as complete ([52ca0b2](https://github.com/oocx/tfplan2md/commit/52ca0b252a06bdea50ed58af7b462480cd5a3347))
* mark task 3 as complete ([6cc9d45](https://github.com/oocx/tfplan2md/commit/6cc9d45317de1d6f302dacfbe889b0d1a58f7c51))
* mark task 4 as complete ([3d2c7bc](https://github.com/oocx/tfplan2md/commit/3d2c7bc5c6f453ade0910b1e56a382587826f1e8))
* regenerate demo artifacts for display enhancements ([3396c10](https://github.com/oocx/tfplan2md/commit/3396c10119d386297991b08e501dcfa7f5fe0caa))
* update coverage badge and history ([e2fea93](https://github.com/oocx/tfplan2md/commit/e2fea932c640e400ff25f1c354f7c50320cb9f1b))
* update demo artifacts for display enhancements ([2b24e77](https://github.com/oocx/tfplan2md/commit/2b24e77d5af247458430559d41b7b2832a08d774))
* update display enhancements demo artifacts ([8c6e401](https://github.com/oocx/tfplan2md/commit/8c6e4019a25402864df2ebcaad73f299490259dd))
* update tasks for display-enhancements based on architecture revision ([9d01dd6](https://github.com/oocx/tfplan2md/commit/9d01dd62710ae7be28c17b676e6aaa6581315c72))
* update UAT report for display-enhancements (fix branch regression details) ([f288b57](https://github.com/oocx/tfplan2md/commit/f288b574873a1c867275869fecfa8055c473b7ff))
* update UAT report for display-enhancements with full maintainer feedback ([2d36e8f](https://github.com/oocx/tfplan2md/commit/2d36e8fa06b811eee8159dff4083779c5225c7f1))

<a name="1.2.0"></a>
## [1.2.0](https://github.com/oocx/tfplan2md/compare/v1.1.0...v1.2.0) (2026-01-27)

### ✨ Features

* improve azapi body grouping and array rendering ([cb0caca](https://github.com/oocx/tfplan2md/commit/cb0caca69b1bd6dd38fef454ba5f4d49f2a71322))
* **uat:** add automated failure detection in UAT polling scripts ([684ca4f](https://github.com/oocx/tfplan2md/commit/684ca4f4cde05c80de797bec4a25baa0704ad67d))

### 🐛 Bug Fixes

* **azapi:** normalize metadata formatting ([e264337](https://github.com/oocx/tfplan2md/commit/e2643372f0fa8f9f82b001e9f66d0e3ab6707725))

### 📚 Documentation

* add architecture for azapi attribute grouping ([ad23faf](https://github.com/oocx/tfplan2md/commit/ad23faf3c9ba685b807b18a4c16c1b8521bd78ea))
* add code review for azapi-attribute-grouping ([61542ac](https://github.com/oocx/tfplan2md/commit/61542ac1ffaf12bc316e8c890ac1ca95990295b5))
* add comprehensive test plan and UAT scenarios for feature 050 ([80318ea](https://github.com/oocx/tfplan2md/commit/80318ea518eca423f6653e742428cf5094464338))
* add feature specification for azapi attribute grouping ([24491c0](https://github.com/oocx/tfplan2md/commit/24491c023ccc6b29b92ea9162540651c1f4132f4))
* add tasks for improved azapi attribute grouping and array rendering ([a730f89](https://github.com/oocx/tfplan2md/commit/a730f89f4e4dec8464941a238d53602a36dabcfe))
* add UAT report for azapi-attribute-grouping (Passed) ([ca3e30f](https://github.com/oocx/tfplan2md/commit/ca3e30f9fcfb11067c000eed5070d8a92ab116e4))
* add UAT report for azapi-attribute-grouping showing failures ([89c7fe6](https://github.com/oocx/tfplan2md/commit/89c7fe6d23b0885f8a126743b34cc37cdd8b108e))
* approve code review for azapi-attribute-grouping after rework ([14b7051](https://github.com/oocx/tfplan2md/commit/14b70517f56a495506fbc333fcb28fe7362124cf))
* refresh azapi UAT artifact ([0e8116c](https://github.com/oocx/tfplan2md/commit/0e8116c4b8a3dab55d476159f1e727d1312d6d92))
* update coverage badge and history ([654d36d](https://github.com/oocx/tfplan2md/commit/654d36dbf2965d0a680a5a26907394e3a3d849b4))
* update UAT artifacts for azapi-attribute-grouping ([16d78bf](https://github.com/oocx/tfplan2md/commit/16d78bfb495ffe1985be9d865c6cfc8c2be1f439))

<a name="1.1.0"></a>
## [1.1.0](https://github.com/oocx/tfplan2md/compare/v1.0.2...v1.1.0) (2026-01-25)

### ✨ Features

* add discovery script for Azure API documentation mappings ([836ede8](https://github.com/oocx/tfplan2md/commit/836ede815de0de8644adc8d8d0084f3fd03c7dc8))
* create AzureApiDocumentationMapper with JSON loading ([f8ee198](https://github.com/oocx/tfplan2md/commit/f8ee19808e8b741af226f87e9d79b4f82fca235b))
* create comprehensive unit tests for AzureApiDocumentationMapper ([c4dc82b](https://github.com/oocx/tfplan2md/commit/c4dc82be131937163195bfcd4f1a5090d9fb8abe))
* generate initial Azure API documentation mappings ([43eb0fb](https://github.com/oocx/tfplan2md/commit/43eb0fbc58f097118ae05c7cff7673da0c9728d3))
* remove '(best-effort)' disclaimer from API documentation links ([2a50688](https://github.com/oocx/tfplan2md/commit/2a5068839f036aefee6a846335925e77b26cac84))
* replace Azure API doc URL guessing with official mappings ([87596f9](https://github.com/oocx/tfplan2md/commit/87596f94a1271a421c8407fdf3646ce261647887))
* update AzureApiDocLink helper to use mapper ([55adb0a](https://github.com/oocx/tfplan2md/commit/55adb0ac54c53ef78c65011666e594893da288ad))
* update existing Scriban helper tests for mapping-based behavior ([4d411cd](https://github.com/oocx/tfplan2md/commit/4d411cd2715932184d49fd98c4e9e145abae31a6))

### ♻️ Refactoring

* move ADR-005 into feature folder per new requirement ([6942132](https://github.com/oocx/tfplan2md/commit/6942132601a548574410c6c7aa14c74dade89618))

### 📚 Documentation

* add code review report for feature 048 - APPROVED ([e156323](https://github.com/oocx/tfplan2md/commit/e156323179b0ae2e1191c65277594de1d2b127ba))
* add feature specification for 048-azure-api-doc-mapping ([8bc0f25](https://github.com/oocx/tfplan2md/commit/8bc0f257c7eea1ae309e687557cbe78a64d2bbce))
* add implementation tasks for feature 048 ([cf57a4f](https://github.com/oocx/tfplan2md/commit/cf57a4f9ca81a0b8dbddd6c297b7a477d36be78e))
* add test plan and UAT test plan for feature 048 (Azure API documentation mapping) ([a5a5c55](https://github.com/oocx/tfplan2md/commit/a5a5c55d5b764234fee174aabf25246a6614dd19))
* add UAT report for feature 048 ([f48f60e](https://github.com/oocx/tfplan2md/commit/f48f60e09e824f285aa002b13ee9d0090b2a452a))
* update architecture per maintainer feedback ([e10f63a](https://github.com/oocx/tfplan2md/commit/e10f63ab73f432da2238f7d7f5c2a50f90bb86cd))
* update coverage badge and history ([43a798c](https://github.com/oocx/tfplan2md/commit/43a798c6db83e49ab7aa4618fd021aa5ef1d6830))
* update coverage badge and history ([d969129](https://github.com/oocx/tfplan2md/commit/d969129513a1734adf3f4882c81317138a227af0))
* update documentation for Azure API documentation mapping feature ([9279576](https://github.com/oocx/tfplan2md/commit/927957695add12304b073f5ba2eddca06625d36f))
* update GitHub instructions to prioritize MCP tools over CLI ([f220447](https://github.com/oocx/tfplan2md/commit/f22044706ea55696ea53c3befbdbfec30429ef22))
* update terminology from "GitHub chat tools" to "GitHub MCP tools" ([78c2445](https://github.com/oocx/tfplan2md/commit/78c2445acd373cc12a3a16d3b9230cfe2f6dc51b))

<a name="1.0.2"></a>
## [1.0.2](https://github.com/oocx/tfplan2md/compare/v1.0.1...v1.0.2) (2026-01-25)

### 🐛 Bug Fixes

* summarize firewall rule changes in updates ([11627d7](https://github.com/oocx/tfplan2md/commit/11627d7f7e25b12fb1513c521d3c8e5e04a170d0))
* update comprehensive-demo artifact with semantic firewall summary ([76e46f2](https://github.com/oocx/tfplan2md/commit/76e46f226f39aabbf7298c71932507f5d6c1fc91))

### 📚 Documentation

* add code review for firewall summary fix ([5cb6faa](https://github.com/oocx/tfplan2md/commit/5cb6faa7f64c8726a02ffa93d7510739f6aa9f79))
* add issue analysis for firewall summary ([535dc26](https://github.com/oocx/tfplan2md/commit/535dc26b4483b606b58e06a9864e3447ec3ce421))
* add UAT report for firewall summary fix ([4606144](https://github.com/oocx/tfplan2md/commit/46061449c8a32418dee3e0d0c66a6e25c651f985))
* update coverage badge and history ([5b4191f](https://github.com/oocx/tfplan2md/commit/5b4191fc55fca47de6eeddb8591b0a92a5b8711c))

<a name="1.0.1"></a>
## [1.0.1](https://github.com/oocx/tfplan2md/compare/v1.0.0...v1.0.1) (2026-01-25)

### 🐛 Bug Fixes

* infer principal type from mappings ([0bce648](https://github.com/oocx/tfplan2md/commit/0bce64887c6a532d2af3aa86600abae4b5989b3b))

### 📚 Documentation

* add issue analysis for principal type inference ([193141f](https://github.com/oocx/tfplan2md/commit/193141fe071f5e7c32bdf5630556de8c12f1e614))
* update coverage badge and history ([13057ce](https://github.com/oocx/tfplan2md/commit/13057ced571f3bb383a631b4a68946553fce8bb5))

<a name="1.0.0"></a>
## [1.0.0](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.46...v1.0.0) (2026-01-25)

### ✨ Features

* remove alpha pre-release configuration to enable stable releases ([ea5f347](https://github.com/oocx/tfplan2md/commit/ea5f347c76fe75522b2a57dd04aec67adba4436f))

### 🐛 Bug Fixes

* keep original markdown link format in changelog header ([5edb38e](https://github.com/oocx/tfplan2md/commit/5edb38e19f68ae593e7613edb6b4dbb627573df8))

### 📚 Documentation

* add prominent link to official website in README ([7709c78](https://github.com/oocx/tfplan2md/commit/7709c787274d91413a5fa1f0fca53b5a62235ba9))
* add retrospective and metrics for feature 047 ([9971ca3](https://github.com/oocx/tfplan2md/commit/9971ca3065b697bf0c61c8652f9b0129fb49561a))
* add workflow tasks for GPT-5.2-Codex update ([ec4db93](https://github.com/oocx/tfplan2md/commit/ec4db93b9dfc607e7f9079b4d414432e5df8cddd))
* switch to GPT-5.2-Codex ([50d8f38](https://github.com/oocx/tfplan2md/commit/50d8f38c00d1a25012444a680c61a161092c100c))
* update coverage badge and history ([fc824cd](https://github.com/oocx/tfplan2md/commit/fc824cdc9433b18f031b72a3c08c6ff98c40106f))
* update GPT-5.2-Codex model reference ([9a44ee7](https://github.com/oocx/tfplan2md/commit/9a44ee705e73cccdd1c30f9e1c868e378b55a2d7))
* **website:** fix navigation anchor links for render targets ([9723336](https://github.com/oocx/tfplan2md/commit/972333619871df88bda1a35867076e38b2aedcba))
* **website:** update CLI docs to use --render-target flag ([c8b4722](https://github.com/oocx/tfplan2md/commit/c8b47224d47b6804c8173699ffe87daabfa3d13e))

<a name="1.0.0-alpha.46"></a>
## [1.0.0-alpha.46](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.45...v1.0.0-alpha.46) (2026-01-24)

### ✨ Features

* add provider registration infrastructure ([068398f](https://github.com/oocx/tfplan2md/commit/068398f8c93621e5f6abf2cea91f958e532d0b7c))
* add provider registration infrastructure ([ce27324](https://github.com/oocx/tfplan2md/commit/ce2732496c226feef45091665155968426115f44))
* implement RenderTargets and diff formatter dispatching ([6696c32](https://github.com/oocx/tfplan2md/commit/6696c32f698b1d2335eb902813f908f4f51b2aa6))
* migrate AzApi provider to Providers/AzApi namespace ([13f91c2](https://github.com/oocx/tfplan2md/commit/13f91c253aeace5f233c7a210d68411d7028c151))
* migrate AzureDevOps provider to pluggable architecture ([1c45c4c](https://github.com/oocx/tfplan2md/commit/1c45c4c99ddbc486a10edd9b68a7b5386e6e5cfb))
* migrate AzureRM provider to pluggable architecture ([4c77762](https://github.com/oocx/tfplan2md/commit/4c77762e0f1f81642843b08926c8947a2c2b0714))
* **cli:** add --render-target flag to replace --large-value-format ([e0bf38a](https://github.com/oocx/tfplan2md/commit/e0bf38a581b7f02bd715e706c692412f33e0bd1a))

### 🐛 Bug Fixes

* correct AzApi template path structure and test setup ([c92d1c9](https://github.com/oocx/tfplan2md/commit/c92d1c912bf6bae367f8e6e3628319db9a0e37c6))
* **ci:** resolve path mismatch and formatting issues in PR validation ([561b9dc](https://github.com/oocx/tfplan2md/commit/561b9dc89363b48754df8bd61951d5e9c0370dd2))
* **ci:** use absolute path for results-directory to ensure root placement ([9164927](https://github.com/oocx/tfplan2md/commit/91649272b1ca9de5d1e34561c72bdd57d994db32))
* **ci:** use root-level TestResults for better path consistency ([8213d23](https://github.com/oocx/tfplan2md/commit/8213d2353f372040fee033fb7bbecbe7ee8428a6))

### ♻️ Refactoring

* move configuration files from root to src/ ([aed6deb](https://github.com/oocx/tfplan2md/commit/aed6debc31147be51a09c2c1ca28344a4389569f))
* move remaining 3 ScribanHelpers files to subdirectory ([24279ab](https://github.com/oocx/tfplan2md/commit/24279abb95722ebe8964e3bcaf0eddc304baa5bd))
* move ViewModelFactory classes to AzureRM provider ([659e246](https://github.com/oocx/tfplan2md/commit/659e246c54fde4acf67cfba5040695d63828627e))
* reorganize ScribanHelpers into subdirectories and simplify calls ([031a7c4](https://github.com/oocx/tfplan2md/commit/031a7c46638a524644c5c736edc68c011f9f471a))
* **platforms:** restructure Azure utilities into Platforms/Azure namespace ([f759260](https://github.com/oocx/tfplan2md/commit/f759260a8fdb0ba0a5d6d00cbd009837890fa499))

### 📚 Documentation

* add architecture for provider code separation ([1a75d55](https://github.com/oocx/tfplan2md/commit/1a75d55c38349becf170a7a53790a891b35a7fe7))
* add code review for provider code separation ([a4d9273](https://github.com/oocx/tfplan2md/commit/a4d92730d795ada4c619aa8bb6173577cb176f0e))
* add coverage threshold validation to developer and code reviewer agents ([7e2e81e](https://github.com/oocx/tfplan2md/commit/7e2e81ecff60388073b38adb0908605bfb5c7e58))
* add feature specification for 047-provider-code-separation ([7aa0dcb](https://github.com/oocx/tfplan2md/commit/7aa0dcb9f3be372627494999137a79129dd6f147))
* add Project Structure section to CONTRIBUTING.md ([819d006](https://github.com/oocx/tfplan2md/commit/819d006028065b94d15e0b9316ef47cd9d373b37))
* add tasks for provider code separation ([eabcd29](https://github.com/oocx/tfplan2md/commit/eabcd29739ef8a67cd1eb70b18ac2bacb0362ed9))
* add test and UAT plans for 047-provider-code-separation ([6645842](https://github.com/oocx/tfplan2md/commit/6645842f9e965bb1c73ea9e3ae1aafe78f3b91c1))
* add test and UAT plans for 047-provider-code-separation ([db2ca6d](https://github.com/oocx/tfplan2md/commit/db2ca6d33cca5ebc3e8ae0ccb35fb16cbe219262))
* create Providers/README.md with comprehensive guide ([8273d67](https://github.com/oocx/tfplan2md/commit/8273d6705c3c05e2ff46603b34cd370ee862e8c0))
* mark task 2 as complete ([5f90e93](https://github.com/oocx/tfplan2md/commit/5f90e93629f1c0e269201b3ea3ec69059b2d27b5))
* mark task 3 (CLI --render-target) as complete ([1e9876d](https://github.com/oocx/tfplan2md/commit/1e9876d58305e93a98286d4abe0cdbadb6e25605))
* mark task 4 (Platform utilities restructure) as complete ([84e03df](https://github.com/oocx/tfplan2md/commit/84e03dffbbbb3e3737c56d8cf81600040bef387d))
* mark task 5 (Migrate AzApi Provider) as complete ([de1f43f](https://github.com/oocx/tfplan2md/commit/de1f43f94b5957638191462f3312000dccc90691))
* mark Task 8 (Cleanup and Test Suite Alignment) as complete ([4675b22](https://github.com/oocx/tfplan2md/commit/4675b2213a7d2ade843f6e9322f8ea19d7fe07e1))
* mark Task 9 as complete in tasks.md ([cfdae43](https://github.com/oocx/tfplan2md/commit/cfdae430ab44e97ce2f8e005d75a57fd90bbfc64))
* mark tasks 6 and 7 as complete ([62f539f](https://github.com/oocx/tfplan2md/commit/62f539fca06badd3c98442f49fe50cd580930d35))
* refresh demo artifact metadata ([aec5d49](https://github.com/oocx/tfplan2md/commit/aec5d49c3a83856de4799e4d98bd06b6b169747b))
* update architecture.md with provider structure ([338603b](https://github.com/oocx/tfplan2md/commit/338603be3042a321cb4fd66b38e9acee6bea13b7))
* update code review status ([f9546f7](https://github.com/oocx/tfplan2md/commit/f9546f7ce29d3ff8a736db04143c4f9af96ab2bc))
* update code review with coverage blocker ([0522c7d](https://github.com/oocx/tfplan2md/commit/0522c7dd31d66be39935b25db10f9989dad8148d))
* update coverage badge and history ([78b820c](https://github.com/oocx/tfplan2md/commit/78b820ccd7ea863be1158e9e674cce18ac8ebbc9))

<a name="1.0.0-alpha.45"></a>
## [1.0.0-alpha.45](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.44...v1.0.0-alpha.45) (2026-01-23)

### ✨ Features

* baseline code metrics violations ([5a87bdb](https://github.com/oocx/tfplan2md/commit/5a87bdb55dccb2e5229a30b279fdd890aa8121a6))
* configure code metrics enforcement ([9dbf38e](https://github.com/oocx/tfplan2md/commit/9dbf38e014fc9154fd060d3bd5eb00d5266b4ad6))
* refactor Azure role definitions to data-driven approach ([f1109e7](https://github.com/oocx/tfplan2md/commit/f1109e7d078cd6f1567b84f6825343bbeccd4185))
* refactor ResourceSummaryBuilder to reduce file size and class coupling ([61a7c96](https://github.com/oocx/tfplan2md/commit/61a7c962eabfa414bcd297f8dd5962312e6fae2e))

### 🐛 Bug Fixes

* restore CA1506 suppression for ReportModelBuilder ([49249d2](https://github.com/oocx/tfplan2md/commit/49249d2802d0fea15b785ceaf90747c20465e939))

### ♻️ Refactoring

* extract summary and JSON helpers to reduce ReportModelBuilder coupling ([9cc9e6f](https://github.com/oocx/tfplan2md/commit/9cc9e6f05d2ef7bbc54a289ad992fef68fdea6db))
* introduce factory registry to reduce ReportModelBuilder coupling ([fccc201](https://github.com/oocx/tfplan2md/commit/fccc2015feef2f510439d47bf3f2fe1c6f87b618))
* split azapi scriban helpers ([b537d21](https://github.com/oocx/tfplan2md/commit/b537d2190888739b22ab6372a52a5f67119b44ab))
* split VariableGroupViewModelFactory into focused helpers ([8885219](https://github.com/oocx/tfplan2md/commit/8885219f2fd935b309c239af6020548e4b693ac8))

### 📚 Documentation

* add architecture for 046-code-quality-metrics-enforcement ([b49125e](https://github.com/oocx/tfplan2md/commit/b49125e764cbf6e56c70e1f6a193f7dc7b6f2326))
* add code review for code-quality-metrics-enforcement ([9d03e35](https://github.com/oocx/tfplan2md/commit/9d03e351a89afd5157d8af9d724a5d20cf69120d))
* add feature specification for 046-code-quality-metrics-enforcement ([fab1a4c](https://github.com/oocx/tfplan2md/commit/fab1a4ced46f04e8947d50e75ec4b78b7d2ff052))
* add retrospective and redacted chat logs for feature 043 ([58a47c8](https://github.com/oocx/tfplan2md/commit/58a47c8ae2774819ada2e302d61b8928308cbb49))
* add tasks for 046-code-quality-metrics-enforcement ([fdbf5a3](https://github.com/oocx/tfplan2md/commit/fdbf5a3ca22bbc68ba4ae7b03e923df85d6fe0c2))
* add test plan and UAT test plan for 046-code-quality-metrics-enforcement ([6849f14](https://github.com/oocx/tfplan2md/commit/6849f14e608edddf3140e3b18c7f63b694aea888))
* document quality metric suppressions ([d29545d](https://github.com/oocx/tfplan2md/commit/d29545dd419c751559918a01855191c0b4eded8e))
* link improvement opportunities to github issues in retrospective ([a55e7b1](https://github.com/oocx/tfplan2md/commit/a55e7b10e1301f78f57fe5e8666da27f54c36e81))
* mark Task 6 complete - VariableGroupViewModelFactory refactored ([8fec3b1](https://github.com/oocx/tfplan2md/commit/8fec3b1355c9e212beff5127c4371b1de2dfeb1f))
* mark Task 7 as complete ([43b9a6e](https://github.com/oocx/tfplan2md/commit/43b9a6eb229bfd225560d896257a60367b29e79c))
* mark Task 8 as complete ([25b8af1](https://github.com/oocx/tfplan2md/commit/25b8af1d5590dd366de65d8c304c267789b29f08))
* mark Task 9 as complete with audit results ([2a253cc](https://github.com/oocx/tfplan2md/commit/2a253cc7f4a2121978be9af1f14b5797b78b6698))
* update coverage badge and history ([3083f4f](https://github.com/oocx/tfplan2md/commit/3083f4fa14b71491c308de1923ce93ab656b71bc))
* update documentation for code quality metrics enforcement ([5546664](https://github.com/oocx/tfplan2md/commit/55466642490bfbe723d74a51b6178b024d77b7e3))
* update Task 5 progress - coupling reduced 24% (50→38 types) ([0baa651](https://github.com/oocx/tfplan2md/commit/0baa6511c3d847a5aaddc13cdeac7a6779395940))
* update Task 5 status to reflect partial completion ([a7d0051](https://github.com/oocx/tfplan2md/commit/a7d005105cbb919d7680ff2066ffca55ae1e52a5))

<a name="1.0.0-alpha.44"></a>
## [1.0.0-alpha.44](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.43...v1.0.0-alpha.44) (2026-01-23)

### ✨ Features

* add quiet mode to CI polling scripts for agent consumption ([#346](https://github.com/oocx/tfplan2md/issues/346)) ([a00b829](https://github.com/oocx/tfplan2md/commit/a00b829a4611017cf97904101eac4317ce966014))

<a name="1.0.0-alpha.43"></a>
## [1.0.0-alpha.43](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.42...v1.0.0-alpha.43) (2026-01-23)

### ✨ Features

* add Meziantou.Analyzer v2.0.127 (Phase 3) ([b2b8533](https://github.com/oocx/tfplan2md/commit/b2b8533badf69f9eb017e565ba213bb228069bdb))
* add SonarAnalyzer.CSharp v9.16.0.82469 for code quality analysis ([92b3ad5](https://github.com/oocx/tfplan2md/commit/92b3ad58f50f2c1afbb9833753fe22b0af75dd65))
* add StyleCop.Analyzers v1.2.0-beta.556 package ([49962e4](https://github.com/oocx/tfplan2md/commit/49962e4ecc7935defab2501e4a4c14473ebc4479))
* configure Meziantou.Analyzer rules in .editorconfig (Phase 3) ([af21724](https://github.com/oocx/tfplan2md/commit/af217248b9df04a403cb50f9c324f6f231911460))
* configure SonarAnalyzer.CSharp rules in .editorconfig ([704178b](https://github.com/oocx/tfplan2md/commit/704178bfbd941a43c7bff42afde9aa16a726449d))
* configure StyleCop rules and fix violations ([418422a](https://github.com/oocx/tfplan2md/commit/418422a8a8f83698e9875abb76303f1925b6e11a))
* enable XML documentation generation for main project ([d556140](https://github.com/oocx/tfplan2md/commit/d556140ca3b850749e26f47e06397431d0bfb911))
* promote critical SonarAnalyzer rules to error severity ([8d5c106](https://github.com/oocx/tfplan2md/commit/8d5c1064a38dfe9f9e4f62a1f37a1649f4b5d238))
* suppress 7 SonarAnalyzer violations in TerraformShowRenderer with documented justifications ([773ca36](https://github.com/oocx/tfplan2md/commit/773ca3668a3b07e11086e3912b433851be2b7684))
* **analyzer:** add Roslynator.Analyzers v4.12.11 (P4-T1) ([46a350b](https://github.com/oocx/tfplan2md/commit/46a350bf7e6e2f8162a41802deb17dee6c56806c))
* **analyzer:** configure Roslynator rules with selective enabling (P4-T3) ([54158d9](https://github.com/oocx/tfplan2md/commit/54158d93ce4f5c2a2649a3ef96e76f9ed098790b))
* **analyzer:** promote MA0009 to error, MA0013 to warning (P3-T6) ([ce3c8ed](https://github.com/oocx/tfplan2md/commit/ce3c8ed199ae9dd0452e64873028a45a4779b994))
* **workflow:** orchestrator must forward agent questions to maintainer ([2cc9f8c](https://github.com/oocx/tfplan2md/commit/2cc9f8cdb6552152f4906b81e485283a90f7b841))

### 🐛 Bug Fixes

* disable SA documentation rules for test project ([f2e3da0](https://github.com/oocx/tfplan2md/commit/f2e3da0c70108cc8ad9431cee8d6a7137df99b6f))
* resolve critical SonarAnalyzer violations (logic errors and bugs) ([dd5f01b](https://github.com/oocx/tfplan2md/commit/dd5f01b3fc829e29a420b22f31c63facafe3424f))
* resolve inherited SonarAnalyzer violations in test code (Phase 2 cleanup) ([525cba3](https://github.com/oocx/tfplan2md/commit/525cba3eded58b5be6515a193bba2992b758c4c2))
* resolve remaining SonarAnalyzer violations (except S6618) ([816e769](https://github.com/oocx/tfplan2md/commit/816e769ea434a8728b63463cf541e597d6604417))
* resolve S6618 performance warnings and fix syntax error ([557f037](https://github.com/oocx/tfplan2md/commit/557f037bab2951d44f76e589df2e128e86351a00))
* resolve SonarAnalyzer code readability violations (S3267, S3358) ([7e045cd](https://github.com/oocx/tfplan2md/commit/7e045cdfb05acc1de576fad942887e90aa1e6163))
* suppress remaining TerraformShowRenderer SonarAnalyzer violations ([051a97e](https://github.com/oocx/tfplan2md/commit/051a97ee73e50bd95d7522ec771016aad3e7b215))
* **analyzer:** add regex timeouts to prevent ReDoS attacks (MA0009) ([97defe2](https://github.com/oocx/tfplan2md/commit/97defe265a9e158d54c2312ba47e917f04b56392))
* **analyzer:** replace ApplicationException with Exception (MA0013) ([f60fc7d](https://github.com/oocx/tfplan2md/commit/f60fc7d53e6cf4d503ec8e0f3ad45868f2f0b886))
* **analyzer:** resolve Roslynator violations in source code (P4-T4) ([b6c8206](https://github.com/oocx/tfplan2md/commit/b6c820674867aa1c08eca8011005f966528fb2f7))
* **analyzer:** resolve Roslynator violations in tests (P4-T5) ([1dccc87](https://github.com/oocx/tfplan2md/commit/1dccc87489ae0987459825bb2b407cb3fb5fa502))
* **workflow:** prohibit raw gh commands when repository scripts exist ([0b5dbdb](https://github.com/oocx/tfplan2md/commit/0b5dbdbf31809c89cd58303b6fb4e34af8c58de4))

### 🚀 Performance

* **analyzer:** add RegexOptions.ExplicitCapture where applicable (MA0023) ([42705a0](https://github.com/oocx/tfplan2md/commit/42705a0467f10cf41cd9c22a47ceb11d8d9d08a4))

### ♻️ Refactoring

* disable culture-specific Meziantou rules for Docker deployment ([52e9a7d](https://github.com/oocx/tfplan2md/commit/52e9a7d3d09046541e298abf9c3f747d2e7d5300))

### 📚 Documentation

* add architecture design for 044-enhanced-static-analysis ([7f81281](https://github.com/oocx/tfplan2md/commit/7f812819099dd3434bde1723fdfb6af6189b8285))
* add implementation tasks for 044-enhanced-static-analysis ([8960b45](https://github.com/oocx/tfplan2md/commit/8960b452ec658a1e1a328bff4305d9af206da95d))
* add missing XML documentation to fix SA1600 violations ([ec4e5e9](https://github.com/oocx/tfplan2md/commit/ec4e5e9902a3031c29ae1a0389a8e0a2f0e6c622))
* add Phase 1 code review for feature [#044](https://github.com/oocx/tfplan2md/issues/044) ([1f74fa4](https://github.com/oocx/tfplan2md/commit/1f74fa4f53eba477b4dccd6635f658b538f422a9))
* add Phase 1 code review report ([052f820](https://github.com/oocx/tfplan2md/commit/052f8206529a6c96b3e57d1f0c8861eb88811572))
* add Phase 2 code review report ([559d3b2](https://github.com/oocx/tfplan2md/commit/559d3b233c0d73a187274a9dea59d0623c856d5f))
* add Phase 2 code review report - APPROVED ([f3d5e08](https://github.com/oocx/tfplan2md/commit/f3d5e08ce02ad4c16b8f54cef179a9d6f39cecb4))
* add Phase 2 completion summary for SonarAnalyzer integration ([2f950c2](https://github.com/oocx/tfplan2md/commit/2f950c2cb8ec450b6d3777c1593848f32c7c2abb))
* add Phase 3 code review report - APPROVED ([c05ba0d](https://github.com/oocx/tfplan2md/commit/c05ba0d112ca072d7c68c8c1a5488f88d7d00814))
* add Phase 3 code review report - APPROVED ([1bc13a4](https://github.com/oocx/tfplan2md/commit/1bc13a4b9c351a49c29bc669d3165c6ac8241288))
* add Phase 4 code review - CHANGES REQUIRED ([118312a](https://github.com/oocx/tfplan2md/commit/118312ac3afd8d3745727fea5af18133ba96f134))
* add test plan for feature 044 (enhanced static analysis) ([c3f6721](https://github.com/oocx/tfplan2md/commit/c3f6721d17df38fe7eab3b5e5b0dd9fd02f4ceb4))
* add XML documentation to Azure and CLI classes ([559003b](https://github.com/oocx/tfplan2md/commit/559003bd33e8fde23aee9e96454b79f0fef71990))
* add XML documentation to remaining classes ([aea9a10](https://github.com/oocx/tfplan2md/commit/aea9a10ac70a9f5c98c8ae9a7c9f18c1aedc4c32))
* complete Phase 4 with test validation and performance analysis (P4-T6 through P4-T9) ([0a8416c](https://github.com/oocx/tfplan2md/commit/0a8416c6cf414f1396802dff3aaf0af084ed6a48))
* document architecture decision for culture invariance ([90be154](https://github.com/oocx/tfplan2md/commit/90be1542e718b130b29525a8be0699859867cdab))
* document Phase 3 Meziantou.Analyzer baseline violations ([cbdeeb4](https://github.com/oocx/tfplan2md/commit/cbdeeb4c39360e7184122d488162f6ef44b191f0))
* enforce mandatory commit before agent handoff ([ae50cec](https://github.com/oocx/tfplan2md/commit/ae50cecd4034d3e1b07548b884bdafbdfb8f19b0))
* fix duplicate and malformed XML documentation tags ([d108709](https://github.com/oocx/tfplan2md/commit/d1087098469dd0b95df80c79567312564a44fd7c))
* fix XML documentation parameter and constructor errors ([206b80a](https://github.com/oocx/tfplan2md/commit/206b80a8b3dcf8f99160fe1e5840d4ffaf4dc25d))
* Phase 4 re-review - APPROVED after test regression fix ([94b1983](https://github.com/oocx/tfplan2md/commit/94b1983214bd2f1e87066841410b5ca06f8508ab))
* update coverage badge and history ([5ba73f3](https://github.com/oocx/tfplan2md/commit/5ba73f3b3e44a1ede3dae1c5339a74e07314334a))

<a name="1.0.0-alpha.42"></a>
## [1.0.0-alpha.42](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.41...v1.0.0-alpha.42) (2026-01-21)

### ✨ Features

* trigger release after ci workflow fix ([399d40c](https://github.com/oocx/tfplan2md/commit/399d40c069c31327d1d3d6b8da81add4526e382e))

### 🐛 Bug Fixes

* update Dockerfile path in release workflow ([bae9fc5](https://github.com/oocx/tfplan2md/commit/bae9fc5b56e8cf90157187be2c26e68259e1e528))

<a name="1.0.0-alpha.41"></a>
## [1.0.0-alpha.41](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.40...v1.0.0-alpha.41) (2026-01-21)

### ✨ Features

* add coverage badge and history ([26bf292](https://github.com/oocx/tfplan2md/commit/26bf292ff9ec6f79cec51a0d71e3034ede0962dd))
* add coverage enforcement tool ([f3453b2](https://github.com/oocx/tfplan2md/commit/f3453b297f665d916356c8058ea74c1cc403ebe2))
* add coverage override support ([d7f7cbd](https://github.com/oocx/tfplan2md/commit/d7f7cbd65d6b78d925e65292aea52dd98aa861a0))
* publish coverage summary ([8df9f2e](https://github.com/oocx/tfplan2md/commit/8df9f2e5d43df5ce6284e031d56b37362dd44a9f))

### 🐛 Bug Fixes

* correct demo artifact paths ([b7810c3](https://github.com/oocx/tfplan2md/commit/b7810c33974237c0ef6ab698308786da17b36266))
* stabilize coverage history update in ci ([0ad7672](https://github.com/oocx/tfplan2md/commit/0ad7672c26305bde76b9b7da265a5d58884106cb))

### ♻️ Refactoring

* use raw string literals in badge generator ([298c276](https://github.com/oocx/tfplan2md/commit/298c2768d49ff7e728d6a35d8cd137d52a90e264))

### 📚 Documentation

* add architecture for code coverage CI ([ec15b4f](https://github.com/oocx/tfplan2md/commit/ec15b4f089c743a0c04ca3f4978713860bd690bf))
* add coverage documentation updates ([9726773](https://github.com/oocx/tfplan2md/commit/9726773c881b6d486dbe39fd8df5ec4188671af9))
* add feature specification for 043-code-coverage-ci ([0787bf5](https://github.com/oocx/tfplan2md/commit/0787bf5573294515a4d4c3fe0dd84309e9452356))
* add tasks for code coverage ci ([7457311](https://github.com/oocx/tfplan2md/commit/745731172d0bc9488d0928e6d788497f63d09d71))
* add test plan and uat test plan for 043-code-coverage-ci ([01de8c4](https://github.com/oocx/tfplan2md/commit/01de8c4c571a8f6667965198ebcfba81a4e6f867))
* approve code coverage implementation after fixes ([10bda7a](https://github.com/oocx/tfplan2md/commit/10bda7ab59634bbf3d91136226b5b400333039de))
* finalize coverage task checklist ([092fd1d](https://github.com/oocx/tfplan2md/commit/092fd1dd8a93f503d32acb70549281bd1c62226a))
* update coverage badge and history ([38b1138](https://github.com/oocx/tfplan2md/commit/38b1138926367a0c7575d8f5474034588aa5cd31))
* update coverage badge and history ([875c00a](https://github.com/oocx/tfplan2md/commit/875c00a65db6ebc70c87f172bf69c4de57deccab))
* update demo artifacts for coverage ([db4dc87](https://github.com/oocx/tfplan2md/commit/db4dc878d8d38733a51754ea5e8297ab1c0d7996))
* update uat status for coverage ([129b4e0](https://github.com/oocx/tfplan2md/commit/129b4e00d1eacc4be1e83ab03388847635cd0ad4))

<a name="1.0.0-alpha.40"></a>
## [1.0.0-alpha.40](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.39...v1.0.0-alpha.40) (2026-01-18)

### 🐛 Bug Fixes

* correct shell test repo root ([3db6466](https://github.com/oocx/tfplan2md/commit/3db6466d6656b25640d78dc5a364038a99689112))
* use src TestResults in CI ([202a9e3](https://github.com/oocx/tfplan2md/commit/202a9e3fd413755bbdc70337386b69b0507f2e58))

### ♻️ Refactoring

* move sources under src and update test guidance ([91cc449](https://github.com/oocx/tfplan2md/commit/91cc44984a9269e2b3cc5a68d22dbcfd8cf07894))

<a name="1.0.0-alpha.39"></a>
## [1.0.0-alpha.39](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.38...v1.0.0-alpha.39) (2026-01-17)

### ✨ Features

* add ScriptObject handling to FlattenJson and create integration tests ([f5839b5](https://github.com/oocx/tfplan2md/commit/f5839b536ef4fce7ed20629b8dbc78b9c581c25b))
* add target attributes to all agents ([4391fb5](https://github.com/oocx/tfplan2md/commit/4391fb52866d861eed1214a03fda3248b6848ac0))
* create azapi/resource.sbn template ([9f2645f](https://github.com/oocx/tfplan2md/commit/9f2645fdea761ed952e04792d7f5f5ee376270fc))
* implement AzureApiDocLink helper (Task 5, 11 partial) ([b23754f](https://github.com/oocx/tfplan2md/commit/b23754fc9e6b9a154351e97f879162819bad50b0))
* implement CompareJsonProperties helper for azapi_resource ([699920d](https://github.com/oocx/tfplan2md/commit/699920d97dd00e33b890c94f5bfab94dca5bd75a))
* implement ExtractAzapiMetadata helper (Task 6, 12) ([4f4982e](https://github.com/oocx/tfplan2md/commit/4f4982e9c27ea69ab96f52be4eb691926b1fad77))
* implement FlattenJson and ParseAzureResourceType helpers (Task 2, 4, 9, 11 partial) ([3f9e439](https://github.com/oocx/tfplan2md/commit/3f9e439e07e9bf813c509de924304841fab461b6))
* improve azapi body rendering with prefix removal and nested grouping ([41485f2](https://github.com/oocx/tfplan2md/commit/41485f250b95f7d9c1eeab31aaef7f6da6d2f6cc))
* integrate azapi template with semantic formatting features ([63ffb7a](https://github.com/oocx/tfplan2md/commit/63ffb7a406c45779c2cc7e00507e3529272ca93d))
* register azapi helpers in Scriban registry ([609ee3c](https://github.com/oocx/tfplan2md/commit/609ee3c5161b670e3fbbb03a510ace344ee09f16))
* split agents into local and coding variants ([dfe4f7b](https://github.com/oocx/tfplan2md/commit/dfe4f7bf2460a7ce09cf1139921e81d86e258ca5))

### 🐛 Bug Fixes

* final cleanup of coding agent environment references ([5c10a1b](https://github.com/oocx/tfplan2md/commit/5c10a1b87612d04372eb92fbcf855da9d8ee8f0a))
* regenerate azapi artifacts with latest tfplan2md version ([733ac75](https://github.com/oocx/tfplan2md/commit/733ac752223c994abbac94f7076daafb9b2414b5))
* remove conditional wrapper from metadata table in azapi template ([b63ee23](https://github.com/oocx/tfplan2md/commit/b63ee237bf03a738436e9fe2291a8ec44386b808))
* remove environment-specific instructions from coding agents ([1b5c7c3](https://github.com/oocx/tfplan2md/commit/1b5c7c3d21c76c85f73eb8943c89e527427ae117))
* remove handoffs from coding agents and add workflow instructions ([bf3e673](https://github.com/oocx/tfplan2md/commit/bf3e67340474c192718cef065aa65d42c26eef0d))
* resolve all code review blocker issues for azapi template ([09fd713](https://github.com/oocx/tfplan2md/commit/09fd713df297c0f55207e2a80627fa365c4dd52d))
* resolve azapi template blocker issues ([4f902bf](https://github.com/oocx/tfplan2md/commit/4f902bf56713a393d3b1a9ec9114b5670e4e6756))

### ♻️ Refactoring

* simplify heading from "Body Configuration" to "Body" ([6e74ea7](https://github.com/oocx/tfplan2md/commit/6e74ea792d084905f1215b14a3188c73b2fd1b99))

### 📚 Documentation

* add architecture for azapi_resource template feature ([256348b](https://github.com/oocx/tfplan2md/commit/256348b8e966fc0d1d3521d4bb968d83f4da4ef9))
* add code review report for azapi_resource template ([232767c](https://github.com/oocx/tfplan2md/commit/232767c9d48c895c2a904ac17f6c2398069f4603))
* add code review report for azapi_resource template feature ([f5c975c](https://github.com/oocx/tfplan2md/commit/f5c975c722147af691ce8a389fe7b6913c6250dc))
* add feature specification for azapi_resource template (040) ([d1ba522](https://github.com/oocx/tfplan2md/commit/d1ba5227391f468ae2382afd6e20cd1ffff73c98))
* add implementation tasks for azapi_resource template ([b01dafc](https://github.com/oocx/tfplan2md/commit/b01dafc18022a5f6062490636e57f674aafb7d70))
* add test plan and UAT test plan for azapi_resource template feature ([e434b3e](https://github.com/oocx/tfplan2md/commit/e434b3e7a131ee4fb6774d50ab54de2f71278d53))
* Add UAT report for azapi_resource template (blocked on auth) ([4a2296c](https://github.com/oocx/tfplan2md/commit/4a2296c00a3e5404e51adff6832369359e5ce8b7))
* code review re-approval - azapi_resource ready for UAT ([a4191b0](https://github.com/oocx/tfplan2md/commit/a4191b0930f6ef3f9867892841cf6a751fadd4b4))
* code review re-approval - azapi_resource template ready for UAT ([2c37434](https://github.com/oocx/tfplan2md/commit/2c37434f3620ea41a106bc519e50db588f5fdcb9))
* document azapi template refactoring in architecture ([e258bb3](https://github.com/oocx/tfplan2md/commit/e258bb3625976a788cc27ef4b9cd5e300a8998c2))
* document azapi_resource template feature ([2cc7c4a](https://github.com/oocx/tfplan2md/commit/2cc7c4a9adfd69286cb871a79adfda039eba4387))
* remove execution-context-detection skill and update agents.md ([25fd21e](https://github.com/oocx/tfplan2md/commit/25fd21e95dab0d8962ea220f5939571c2b1d43a9))

<a name="1.0.0-alpha.38"></a>
## [1.0.0-alpha.38](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.37...v1.0.0-alpha.38) (2026-01-16)

### ✨ Features

* support nested principal mapping format ([1daa7fc](https://github.com/oocx/tfplan2md/commit/1daa7fcf9f5ab1d3b774bd146f14bb5dba6a2b67))

<a name="1.0.0-alpha.37"></a>
## [1.0.0-alpha.37](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.36...v1.0.0-alpha.37) (2026-01-16)

### ✨ Features

* enhance debug output for principal loading errors with detailed diagnostics ([a477fba](https://github.com/oocx/tfplan2md/commit/a477fba54160a48fe55860ed26f31a43dcdfa1d9))
* **diagnostics:** add enhanced error context for principal loading ([497483f](https://github.com/oocx/tfplan2md/commit/497483f174702a0005d23486315582d9e1097bc8))
* **principal-mapper:** implement enhanced error diagnostics ([5df0a91](https://github.com/oocx/tfplan2md/commit/5df0a91a680e01e5ecdeb4629d8b98fe15fb7d69))

### 🐛 Bug Fixes

* change diagnostic types to internal per code review ([ff15e98](https://github.com/oocx/tfplan2md/commit/ff15e98a422e6ec7d6da0d0bbe40345329903aed))
* **workflow:** enforce PR coding-agent branch safety ([e058856](https://github.com/oocx/tfplan2md/commit/e05885647a176c7fbd837ff04bbbb169f48ba592))
* **workflow:** respect GitHub PR coding agent branches ([1aa3060](https://github.com/oocx/tfplan2md/commit/1aa306095d15ee050342c1e3d8cd691c6f8d75b4))

### 📚 Documentation

* add issue analysis for enhanced debug context in principal/template loading ([a1e345c](https://github.com/oocx/tfplan2md/commit/a1e345cd034bd0efd1601038c1a230714b8f9217))
* update debug output documentation with enhanced error diagnostics ([1b8094d](https://github.com/oocx/tfplan2md/commit/1b8094d39f49f2f2bd571c5ec50c9620c41cf8a7))

<a name="1.0.0-alpha.36"></a>
## [1.0.0-alpha.36](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.35...v1.0.0-alpha.36) (2026-01-15)

### ✨ Features

* add Azure DevOps variable groups feature detail page ([a5d9ccf](https://github.com/oocx/tfplan2md/commit/a5d9ccf53212440a4986856f552d78f39533523a))
* convert operation examples and key vault integration to interactive component ([c64a4eb](https://github.com/oocx/tfplan2md/commit/c64a4ebde15f343cc0b81e46fc7933d248dd30f3))
* convert operation examples and key vault integration to interactive component ([4f1b585](https://github.com/oocx/tfplan2md/commit/4f1b585fb92cccaf26825913cc8b5bc886889ea9))
* update website with latest features and architecture decisions ([b0fb6c9](https://github.com/oocx/tfplan2md/commit/b0fb6c96f3f998ad7e4d41bba11c5537c93be596))

### 🐛 Bug Fixes

* improve toggle button text contrast in light mode ([2cf6333](https://github.com/oocx/tfplan2md/commit/2cf633397640eb65c787ee4c4f24460f6ff3d695))

### 📚 Documentation

* add --debug and --hide-metadata flags to CLI reference in docs.html ([216db88](https://github.com/oocx/tfplan2md/commit/216db88dcd39d223fac7b44bdf845218c23c7c03))
* add Docker/Kubernetes examples to docs.html for principal mapping ([dfa03f1](https://github.com/oocx/tfplan2md/commit/dfa03f12d29800c45b0749bdf5585d90b8cff21a))
* remove Docker Compose and Kubernetes examples ([24350c4](https://github.com/oocx/tfplan2md/commit/24350c47318e9d253eb36c4ab0d7e37b86432b7a))
* update documentation for variable groups, debug flag, and principal mapping in containers ([8134a25](https://github.com/oocx/tfplan2md/commit/8134a25ee3e1b6049f5e1931051cf81d7f25f270))

<a name="1.0.0-alpha.35"></a>
## [1.0.0-alpha.35](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.34...v1.0.0-alpha.35) (2026-01-15)

### ✨ Features

* **azdo-variable-group:** add ViewModel classes and Factory with tests ([48441e7](https://github.com/oocx/tfplan2md/commit/48441e79add4c87772eb352853cc4b78d8bc172b))
* **azdo-variable-group:** create Scriban template ([32e2be6](https://github.com/oocx/tfplan2md/commit/32e2be67af066e095429dcc993921ae2e630b84b))
* **azdo-variable-group:** fix template action handling ([a6a553b](https://github.com/oocx/tfplan2md/commit/a6a553b706327159a0a3228169ab4354033411da))
* **azdo-variable-group:** register ViewModel in ResourceChangeModel ([0dcb42f](https://github.com/oocx/tfplan2md/commit/0dcb42ff06a4e1436f7e106f60345f8ed0454318))
* **azdo-variable-group:** wire Factory in ReportModelBuilder ([d858782](https://github.com/oocx/tfplan2md/commit/d858782fe09b6ff58136bc33bfd04babfc461f8a))

### 🐛 Bug Fixes

* show placeholder for null values in variable group diffs ([1c76e1c](https://github.com/oocx/tfplan2md/commit/1c76e1c64265501db2a38c2c1a0fcbeadb20a76b))
* **azdo-variable-group:** add VariableGroup ViewModel mapping to AotScriptObjectMapper ([afda024](https://github.com/oocx/tfplan2md/commit/afda02415723fbb09c087623f916895a63f91799))

### ♻️ Refactoring

* **azdo-variable-group:** remove DEBUG line from template ([3c55071](https://github.com/oocx/tfplan2md/commit/3c550718f4b3d127dc1edabaecc5349dcb7b5933))

### 📚 Documentation

* add code review report for variable group template feature ([3c2d0c3](https://github.com/oocx/tfplan2md/commit/3c2d0c3f410dd158ec885f603cb28aa74adc9671))
* add documentation for Azure DevOps variable group template feature ([c827d25](https://github.com/oocx/tfplan2md/commit/c827d25237b0b6a34247e077554ebae870795ff5))
* add feature specification for 039-azdo-variable-group-template ([799cdac](https://github.com/oocx/tfplan2md/commit/799cdac4e9ab4dd768e62b037a0d22e79d2646f2))
* add test plan and UAT plan for feature 039 (Azure DevOps variable group template) ([bcdbfec](https://github.com/oocx/tfplan2md/commit/bcdbfeccfabf040b86ddf6c3fbb6d08629c67aba))
* mark Task 6 and Task 7 as complete ([79c28c3](https://github.com/oocx/tfplan2md/commit/79c28c30faa356f284ca1127a54ec9de6ab2479d))
* update specification per maintainer feedback and add architecture ([eb0753b](https://github.com/oocx/tfplan2md/commit/eb0753b99c71df3c9d18b4afe8210ed8c8fd61d1))

<a name="1.0.0-alpha.34"></a>
## [1.0.0-alpha.34](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.33...v1.0.0-alpha.34) (2026-01-15)

### ✨ Features

* add --debug CLI flag ([7799a43](https://github.com/oocx/tfplan2md/commit/7799a438292c9660429612bdf45f94d4742d0e74))
* add workflow orchestrator agent with automated delegation ([9b995a1](https://github.com/oocx/tfplan2md/commit/9b995a10dd7f2d1c82c688b4ce11d65f08b2485d))
* create DiagnosticContext infrastructure ([dda88b3](https://github.com/oocx/tfplan2md/commit/dda88b3dd2c3d023f48dc48f33c75f4ffe6fa33a))
* integrate DiagnosticContext with MarkdownRenderer ([a5c677a](https://github.com/oocx/tfplan2md/commit/a5c677a620d1f89a21d5e38306f188d57a9d562c))
* integrate DiagnosticContext with PrincipalMapper ([75cd196](https://github.com/oocx/tfplan2md/commit/75cd19638aa980aac45c65c5beb91859aeef9653))
* wire up DiagnosticContext in Program.cs ([2e54eee](https://github.com/oocx/tfplan2md/commit/2e54eeea64a4715b44b25dce8a86d7f8a77a8885))

### 🐛 Bug Fixes

* pass resource address to principal mapper for diagnostic context ([5e58578](https://github.com/oocx/tfplan2md/commit/5e585783bf652ffc0a036ff2002c776b125a24ac))
* preserve principal type in azure_principal_name helper ([301f754](https://github.com/oocx/tfplan2md/commit/301f75429a4281e564c18eb2310a09cb6bbe8848))
* remove clarifying questions from workflow orchestrator, strengthen delegation-only behavior ([aa1dea5](https://github.com/oocx/tfplan2md/commit/aa1dea52b80d42c4d373d04ea2b129614739a796))
* support type-aware principal resolution in interface default implementations ([e6bb3ba](https://github.com/oocx/tfplan2md/commit/e6bb3ba3f6ef6b631afc2e3b7ee89bde17e30071))
* **workflow:** prevent orchestrator from implementing work directly ([cbf0911](https://github.com/oocx/tfplan2md/commit/cbf09112077a2e63edf3a799893a05801ef35d9f))

### 📚 Documentation

* add comprehensive implementation summary ([929c0c8](https://github.com/oocx/tfplan2md/commit/929c0c8f0d87922cc92f74cdaf5284056669512d))
* add comprehensive test plan for feature 038 (debug output) ([8a4bd12](https://github.com/oocx/tfplan2md/commit/8a4bd123904b3503626acc50c9b964610481ebfe))
* update documentation for feature 038 debug output ([71561ea](https://github.com/oocx/tfplan2md/commit/71561ea599dbd7016955deb0f864f86e9d9e012d))
* update workflow orchestrator documentation to clarify delegation-only behavior ([1e991c4](https://github.com/oocx/tfplan2md/commit/1e991c437ea01a0f6bacd9c3bde06482bc621a65))

<a name="1.0.0-alpha.33"></a>
## [1.0.0-alpha.33](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.32...v1.0.0-alpha.33) (2026-01-14)

### 🐛 Bug Fixes

* correct import ordering in DockerIntegrationTests ([818db03](https://github.com/oocx/tfplan2md/commit/818db03b796c963790bbbeb909ea2bf50066da58))

### 📚 Documentation

* clarify snapshot removal in testing strategy ([9765c58](https://github.com/oocx/tfplan2md/commit/9765c58ac74f127e59065af000b7477ca56ecc7f))
* consolidate initial ADRs and update references ([0be1285](https://github.com/oocx/tfplan2md/commit/0be12852a47ef1f7ba6e3c9d9e2576c4a01bd37e))

<a name="1.0.0-alpha.32"></a>
## [1.0.0-alpha.32](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.31...v1.0.0-alpha.32) (2026-01-14)

### ✨ Features

* Add non-interactive workflow status script for Release Manager ([e30a8c6](https://github.com/oocx/tfplan2md/commit/e30a8c60e13dcb128bb21951b4d3ab3da70b8a6c))

### 📚 Documentation

* add architecture for debug output feature (038) ([541fa1a](https://github.com/oocx/tfplan2md/commit/541fa1a551d272a0c69cda087b0e9e8d0f71b8fb))
* add feature specification for 038-debug-output ([4ff2ecd](https://github.com/oocx/tfplan2md/commit/4ff2ecda25f42592505006ccaa08f09717bc12ce))
* update debug output spec with maintainer decisions ([17ad729](https://github.com/oocx/tfplan2md/commit/17ad729288ab08f48fac6b73b22c4013d11dd225))

<a name="1.0.0-alpha.31"></a>
## [1.0.0-alpha.31](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.30...v1.0.0-alpha.31) (2026-01-14)

### ✨ Features

* add prepare-test-image script and optimize docker test timeouts ([28ce12b](https://github.com/oocx/tfplan2md/commit/28ce12baf4d93c52616fd071228a8977b40ac80e))
* enable all agents as GitHub cloud coding agents ([0845192](https://github.com/oocx/tfplan2md/commit/0845192b9bfaa3f9eb0f72ae9bb34c1b206828ab))

### 🐛 Bug Fixes

* clarify manual export with multiple chat files per agent session ([c05427a](https://github.com/oocx/tfplan2md/commit/c05427aaf3313022986be659e733382d7a24f979))
* clarify multiple chat export files per agent session ([6eede3b](https://github.com/oocx/tfplan2md/commit/6eede3b5d6c0fdbfee4a606ef66373b1327fc787))
* correct YAML indentation in agent handoffs ([b006d60](https://github.com/oocx/tfplan2md/commit/b006d6053de7f1affb33ff84c898e8d89e8909be))
* remove target: vscode from all agents to enable cloud usage ([041906d](https://github.com/oocx/tfplan2md/commit/041906d4bae01d872ce1d6c6f1fafdcf3e5efe6a))
* update Retrospective agent to reflect automated chat export ([b105e72](https://github.com/oocx/tfplan2md/commit/b105e723e86a2b31acced9130d1c4b7678427d88))

### ♻️ Refactoring

* extract execution context detection to skill ([16f123f](https://github.com/oocx/tfplan2md/commit/16f123fbc9270f9e1265800943dba47b9e9e6c79))
* remove duplicated execution context details from agents ([74ad679](https://github.com/oocx/tfplan2md/commit/74ad6797e04f2e5c23b4231ed0ee79f35fe96d10))

### 📚 Documentation

* add --project flag to TUnit CLI examples for clarity ([e657191](https://github.com/oocx/tfplan2md/commit/e6571912066257ec34cc64e7c9fda7539667060a))
* add retrospective for 037-aot-trimmed-image ([b8cc0d8](https://github.com/oocx/tfplan2md/commit/b8cc0d85ba44f04c100d3d3daf7fc36626c620d3))
* standardize TUnit CLI arguments across documentation and agent prompts ([de9e51b](https://github.com/oocx/tfplan2md/commit/de9e51be1ad934a88f3ca4a2b55ede26dd91fa4f))
* update agents.md to reflect all agents support cloud mode ([85fd025](https://github.com/oocx/tfplan2md/commit/85fd02513e5bfaa8b3d200e53e5b60e7770084b1))

<a name="1.0.0-alpha.30"></a>
## [1.0.0-alpha.30](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.29...v1.0.0-alpha.30) (2026-01-14)

### ✨ Features

* add presentation.html for project overview ([214d5d2](https://github.com/oocx/tfplan2md/commit/214d5d2bc2d24dfc4fda4827d675fe4377ca4125))
* **aot:** add aggressive size optimization flags ([b8ca4b8](https://github.com/oocx/tfplan2md/commit/b8ca4b8d9fb79bca00eebc60bf0615d0ae95d821))
* **aot:** add explicit ScriptObject mapper for NativeAOT compatibility ([f939dc0](https://github.com/oocx/tfplan2md/commit/f939dc098870706e322931c96aaf2546066a5534))
* **aot:** enable NativeAOT with JSON source generation ([bdcba62](https://github.com/oocx/tfplan2md/commit/bdcba62e403883e084f834ca9eaac0b3a126ad13))
* **aot:** enable NativeAOT with JSON source generation ([ce9c6e3](https://github.com/oocx/tfplan2md/commit/ce9c6e34ec112599c6d62c7d6b1ca64d2ad2c425))
* **aot:** reduce to minimal essential libraries (18.3MB) ([5bb2a2f](https://github.com/oocx/tfplan2md/commit/5bb2a2f5992df5f1f3c0b3aecf6417e6be23965a))
* **aot:** switch to musl for smaller image (14.7MB) ([1baf642](https://github.com/oocx/tfplan2md/commit/1baf64272618e073b38acab985ae1d5f7a4989dc))
* **aot:** update Dockerfile for NativeAOT and fix trimming warnings ([5c1c13b](https://github.com/oocx/tfplan2md/commit/5c1c13bdca985e288bbab3378bea957c4d167a23))

### 🐛 Bug Fixes

* Add checks:write permission for test result publishing ([3f6c1e4](https://github.com/oocx/tfplan2md/commit/3f6c1e438e479302034aa024c471767dc4667d7a))

### 📚 Documentation

* add architecture for 037-aot-trimmed-image ([4e76b23](https://github.com/oocx/tfplan2md/commit/4e76b23d5be387ffc34def121f7ddad55e13537c))
* add feature specification for 037-aot-trimmed-image ([10e57c4](https://github.com/oocx/tfplan2md/commit/10e57c4b922f60440e8fa058d4289b0475639d5f))
* add tasks for aot-trimmed-image ([3fdc64f](https://github.com/oocx/tfplan2md/commit/3fdc64fdf030f5ae0326e920fb5c29ecf721c209))
* add test plan and UAT plan for 037-aot-trimmed-image ([8ad0e18](https://github.com/oocx/tfplan2md/commit/8ad0e18ffddad816c4c25fbfe4b2f392cad82472))
* add UAT report for AOT-trimmed image ([b2cb94f](https://github.com/oocx/tfplan2md/commit/b2cb94f867711658b077231b9df3fd4908935fff))
* mark task 4 complete, update task 5 metrics ([e81f2f9](https://github.com/oocx/tfplan2md/commit/e81f2f9e3f1bec8d2d36f2227aa76f6ec9b0cbc9))
* mark tasks 5-6 complete with final metrics ([6ca0d5f](https://github.com/oocx/tfplan2md/commit/6ca0d5f0208fce2f4800f57d98fc7bfb64d3c9e4))
* update code review to approved status ([39b00c8](https://github.com/oocx/tfplan2md/commit/39b00c8e5204e178003628424eedf87c83b230e4))
* update demo artifacts for AOT feature ([bef3889](https://github.com/oocx/tfplan2md/commit/bef3889c50af9f56e22f97d0bba268845e726aad))
* update specification with final 14.7MB metrics ([19a496d](https://github.com/oocx/tfplan2md/commit/19a496d7f7c1b24d183b81f5d452dcedbeb48c08))

<a name="1.0.0-alpha.29"></a>
## [1.0.0-alpha.29](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.28...v1.0.0-alpha.29) (2026-01-13)

### ✨ Features

* achieve 100% test coverage in TUnit - all 393 tests converted ([24568b6](https://github.com/oocx/tfplan2md/commit/24568b65e587e2326af2eb02694779d8bcd29d01))
* add Docker integration tests to TUnit and document removed tests ([ef4db7a](https://github.com/oocx/tfplan2md/commit/ef4db7a8d4ec4e31ebf02a3e28e56c8b55575ab1))
* adopt TUnit as primary testing framework for all pipelines and development ([9e6490a](https://github.com/oocx/tfplan2md/commit/9e6490a2fa3be24bad7343ac8f4ad4701d4594ea))
* complete TUnit v1.9.26 conversion with all 370 tests passing ([686e658](https://github.com/oocx/tfplan2md/commit/686e658243b296f02aaf0749ea6b814fda0a83aa))
* convert all tests from xUnit to MSTest v4 and begin TUnit exploration ([eab5c34](https://github.com/oocx/tfplan2md/commit/eab5c34ad7fff372cb463f772fedf7da3cb19fb1))
* convert HtmlRenderer tests to MSTest v4 ([7e9e2ac](https://github.com/oocx/tfplan2md/commit/7e9e2acfb20b7ccb39b5a2fa8d318aa91dfbec93))
* convert main tests to MSTest v4 (work in progress) ([d3cb6c9](https://github.com/oocx/tfplan2md/commit/d3cb6c9dcd7c903cb64d6a11ea361694d1707077))
* convert ScreenshotGenerator tests to MSTest v4 ([76a5d41](https://github.com/oocx/tfplan2md/commit/76a5d415749680370020c6980219c3fb6aeb019c))

### 🐛 Bug Fixes

* correct StringAssert.Matches usage in MSTest ([59227fb](https://github.com/oocx/tfplan2md/commit/59227fb497c011e9112142579f044006a4be3fb3))
* resolve all build errors and complete MSTest v4 conversion ([1fcde99](https://github.com/oocx/tfplan2md/commit/1fcde99fefbefa1213146df4d1804d023bdd7f2f))

### 📚 Documentation

* comprehensive reliability and diagnostics analysis ([4b2d5f2](https://github.com/oocx/tfplan2md/commit/4b2d5f275c84332c691eb5da2ba0c621fddc5e66))

<a name="1.0.0-alpha.28"></a>
## [1.0.0-alpha.28](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.27...v1.0.0-alpha.28) (2026-01-12)

### ✨ Features

* Create detect-diagram-crossings skill with comprehensive testing ([d238db3](https://github.com/oocx/tfplan2md/commit/d238db3bd1bb395a23dd59ff26dac63d8b2f89b1))
* replace three workflow diagrams with single blueprint-styled SVG ([1923f1e](https://github.com/oocx/tfplan2md/commit/1923f1eca08eb381b9a22067eb518611dd19953d))
* update ai-workflow diagram to blueprint style (Design 7) ([bb47358](https://github.com/oocx/tfplan2md/commit/bb47358420ddfee6ad4afefadb73552b9d3f0214))

### 🐛 Bug Fixes

* correct ai-workflow diagram layout and paths ([52b4642](https://github.com/oocx/tfplan2md/commit/52b46424c2344733e4708fd656c24fda25fcd879))
* Eliminate all diagram crossings with improved detection and routing ([ed906ef](https://github.com/oocx/tfplan2md/commit/ed906ef40f8e1e046e49f99dcc5890797c118db9))
* improve readability of homepage and feature detail page links in dark mode ([34f7844](https://github.com/oocx/tfplan2md/commit/34f78440dedc9bb3109a7b3963ed70fcc33e28de))
* redesign ai-workflow diagram with compact layout and no crossing paths ([3e43b60](https://github.com/oocx/tfplan2md/commit/3e43b606edeeb0647b65f968dc1e4a22b79e2acb))
* **website:** improve ai-workflow diagram routing and add enhanced detection ([73f6a1b](https://github.com/oocx/tfplan2md/commit/73f6a1b4b5ce1e4f6719260462f52526aee6630d))

<a name="1.0.0-alpha.27"></a>
## [1.0.0-alpha.27](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.26...v1.0.0-alpha.27) (2026-01-11)

### ✨ Features

* **workflow:** add website accessibility check skill ([7d2fc7f](https://github.com/oocx/tfplan2md/commit/7d2fc7fc7c73db1ed1de96c59cec59beeebe55d5))

### 🐛 Bug Fixes

* **workflow:** pr-github script avoids merged PR reuse ([cf4b53b](https://github.com/oocx/tfplan2md/commit/cf4b53b289d7e81775c0779ed089bb1505ee7fbf))
* **workflow:** prevent website verify no-ops and enforce DevTools ([8b07b22](https://github.com/oocx/tfplan2md/commit/8b07b22ca8c1b6b83cb08dbbebe30ae34510d908))

### ♻️ Refactoring

* **workflow:** improve Web Designer agent effectiveness ([154243e](https://github.com/oocx/tfplan2md/commit/154243e9c92feb5112ded75bca3b5eb2a450dfbd))
* **workflow:** simplify web designer agent prompt ([a6193ae](https://github.com/oocx/tfplan2md/commit/a6193ae2cf132c20df7fe4c0d768972cfc4262f8))

### 📚 Documentation

* **workflow:** add web designer agent refactor tasks ([c91ba27](https://github.com/oocx/tfplan2md/commit/c91ba27ed6881b444469756011d05a286fbdf3db))
* **workflow:** mark web designer model as wont-change ([3e543cc](https://github.com/oocx/tfplan2md/commit/3e543cc16d09c6c8efb891a9b7310c7599d0262c))
* **workflow:** mark web designer refactor task 3 done ([5049fe5](https://github.com/oocx/tfplan2md/commit/5049fe52eab44ee49fa5acc99db86dd3f6ef62ad))
* **workflow:** require devtools mcp for preview navigation ([9ddbdc9](https://github.com/oocx/tfplan2md/commit/9ddbdc98931e933c8bd3a6be120f511e8359b2d5))

<a name="1.0.0-alpha.26"></a>
## [1.0.0-alpha.26](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.25...v1.0.0-alpha.26) (2026-01-11)

### ✨ Features

* **workflow:** add website verify wrapper ([dd5d4fc](https://github.com/oocx/tfplan2md/commit/dd5d4fca643d4266c7afba3f2d37313a0198936e))

<a name="1.0.0-alpha.25"></a>
## [1.0.0-alpha.25](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.24...v1.0.0-alpha.25) (2026-01-11)

### ✨ Features

* add API Management policy example for large value handling ([e231eab](https://github.com/oocx/tfplan2md/commit/e231eab573144df9a49afc615ba78d1c1a94af68))
* add comparison section headers and dynamic height alignment ([6a70705](https://github.com/oocx/tfplan2md/commit/6a707050b1da56369f973000c568ce1d89746d49))
* update website examples with nbsp after emojis ([1345998](https://github.com/oocx/tfplan2md/commit/134599818d7aeab038c53556996e181bf951fa41))
* **website:** add Azure CLI scripts for generating principal mapping files ([eefdceb](https://github.com/oocx/tfplan2md/commit/eefdceb66292f05053eab73d7fc5d2a96adf80aa))
* **website:** add community provider request card to providers page ([6e8837c](https://github.com/oocx/tfplan2md/commit/6e8837ce43f6d76b5333ebd1e5d8bf578fc223bf))
* **website:** add Scriban helper functions documentation to custom templates section ([6d8d919](https://github.com/oocx/tfplan2md/commit/6d8d9190d816c4628dd605562c582a4ba93e6450))
* **website:** add syntax highlighting and copy buttons to code blocks in docs ([3ce1e9b](https://github.com/oocx/tfplan2md/commit/3ce1e9bab223946063d82746c8b3a9762cb10848))
* **website:** add syntax highlighting and update Azure Pipelines to bash ([b78e1f6](https://github.com/oocx/tfplan2md/commit/b78e1f69300ce7dff8f6ca77815427f86b6ea6d2))
* **website:** regenerate screenshots with feature 031 improvements ([378522d](https://github.com/oocx/tfplan2md/commit/378522d0890a02cac4b284c7a1584796faccac08))

### 🐛 Bug Fixes

* use VS Code preview for website ([be82086](https://github.com/oocx/tfplan2md/commit/be820869c02dbb7660b0ca7206da3b1b3e7d13a8))
* **website:** add brand-logo-full class to all pages for consistent logo styling ([1383a90](https://github.com/oocx/tfplan2md/commit/1383a90dc4c1e00f44e61efecac29686f7fa307d))
* **website:** add CSS variable for theme-aware borders in website styles ([fd85f92](https://github.com/oocx/tfplan2md/commit/fd85f92014794bb2808464508db693584a4a951b))
* **website:** add position relative to code containers for proper copy button placement ([66e43b0](https://github.com/oocx/tfplan2md/commit/66e43b0a4818f7d6d2f4bb023ce34e3a02e3f40b))
* **website:** add theme toggle icon and logo styling to provider pages ([57f8ade](https://github.com/oocx/tfplan2md/commit/57f8ade0cba543f7048f23a18092b68c89a8a7ca))
* **website:** convert template examples to interactive component format ([fc2c3eb](https://github.com/oocx/tfplan2md/commit/fc2c3eb669ed7985cf3b82e7c8e01d65bc113c71))
* **website:** correct anchor link to custom templates documentation ([1685ad9](https://github.com/oocx/tfplan2md/commit/1685ad9f2b89c59fe5ae4d67ee48bb38f5fd2269))
* **website:** correct compact card layout structure on providers page ([88c5ef4](https://github.com/oocx/tfplan2md/commit/88c5ef4b256165b514044ace807ecdaadf9a301a))
* **website:** enable CI/CD tab switching on homepage with syntax highlighting ([37dbb49](https://github.com/oocx/tfplan2md/commit/37dbb498a2d22ba245069f66f4490c410c6a7f41))
* **website:** improve contributing page content and spacing ([3cf6b9b](https://github.com/oocx/tfplan2md/commit/3cf6b9b8bf115f84b3b7e107105ba8f88ba600bd))
* **website:** improve contributing page structure and spacing ([3ac54fc](https://github.com/oocx/tfplan2md/commit/3ac54fc76ca299784430a13aff53b4e9423fd635))
* **website:** improve logo contrast in dark mode ([d7d419f](https://github.com/oocx/tfplan2md/commit/d7d419fe7857df29b13535b1207d899702a26bf2))
* **website:** improve providers page layout and styling ([90013af](https://github.com/oocx/tfplan2md/commit/90013afe38d6132e78b15eae03fddee02a57ae3d))
* **website:** improve syntax highlighting detection on getting-started page ([875032b](https://github.com/oocx/tfplan2md/commit/875032b329a4717c19aa5ea58f4730f3791dd8fb))
* **website:** improve tab contrast in light mode ([024dd6f](https://github.com/oocx/tfplan2md/commit/024dd6fdca1bf4886a5ce564fc9e2849ae62ce2f))
* **website:** increase theme toggle icon size and fix dark mode appearance ([5c3cc74](https://github.com/oocx/tfplan2md/commit/5c3cc7462026344b2e0a6c821687bad746063726))
* **website:** make all buttons change background to accent color on hover ([73d914c](https://github.com/oocx/tfplan2md/commit/73d914caa0dd3f19f970d5219c5eb07bc19d10b2))
* **website:** make code blocks theme-aware in docs page ([59d8e31](https://github.com/oocx/tfplan2md/commit/59d8e3102f7c6fc27ca1f0edb79940a364d049d3))
* **website:** make code blocks visible in light mode ([40bd35f](https://github.com/oocx/tfplan2md/commit/40bd35f07ee0f5bc066b26681163adba323a31e8))
* **website:** make copy buttons follow btn-secondary style pattern ([4640df0](https://github.com/oocx/tfplan2md/commit/4640df063152a587a269a03e1821a1305a5c8462))
* **website:** modernize providers page styling and improve content structure ([922aaaf](https://github.com/oocx/tfplan2md/commit/922aaaf78c04e1005c83bda12c1edc581e7b1675))
* **website:** prevent copy button from overlapping command text on homepage ([bba5766](https://github.com/oocx/tfplan2md/commit/bba5766b510b2efb565a5cdbca9aacf4b5533bed))
* **website:** reduce section spacing from 160px to 100px total ([f8cfadd](https://github.com/oocx/tfplan2md/commit/f8cfadd717ea81064daeb888b9a5e425c0711dcc))
* **website:** remove 'Tables not rendering' troubleshooting section ([e94e789](https://github.com/oocx/tfplan2md/commit/e94e7894bc06c1346dd732a08e29651451fb5bd2))
* **website:** remove background mismatch in code blocks ([03407e8](https://github.com/oocx/tfplan2md/commit/03407e8a471a3e12a4322cccba82f49167101afc))
* **website:** remove incorrect feature cards from 'How Provider Templates Work' section ([b96d8e2](https://github.com/oocx/tfplan2md/commit/b96d8e202dbedf91f770877e1cee9aec9c896973))
* **website:** replace broken Input icon with document emoji on architecture page ([556acc1](https://github.com/oocx/tfplan2md/commit/556acc18d90761f49152c25235bd911e2a0ccef8))
* **website:** replace emoji copy icons with SVG icons for consistency ([bc61836](https://github.com/oocx/tfplan2md/commit/bc61836bf54ca45ab768383f380892723c45d561))
* **website:** replace Prism.js with highlight.js for consistency ([ba7426e](https://github.com/oocx/tfplan2md/commit/ba7426ed91c58a255cbe2e910c9399d6d9a7c29f))
* **website:** resolve duplicate currentTheme variable causing syntax highlighting failure ([eefcd3f](https://github.com/oocx/tfplan2md/commit/eefcd3fffae900a0a1bcac728b463d9bdfc7dacf))
* **website:** unify copy button styles across all pages ([ba6a28b](https://github.com/oocx/tfplan2md/commit/ba6a28ba2d6aa96d5bc13677f1fba847410aa632))
* **website:** update all examples to use improved border colors ([4cbf0b5](https://github.com/oocx/tfplan2md/commit/4cbf0b5039409768af77e2e2429751aa1680e0f0))
* **website:** update summary template use cases to reflect notification-focused purpose ([0ea78cb](https://github.com/oocx/tfplan2md/commit/0ea78cbd134a9401a40a4db893fd3581ce48aa2e))
* **website:** use dark-light-mode.svg icon for theme toggle across all pages ([2f16213](https://github.com/oocx/tfplan2md/commit/2f1621339df1bd328d60c2993970085917537914))

<a name="1.0.0-alpha.24"></a>
## [1.0.0-alpha.24](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.23...v1.0.0-alpha.24) (2026-01-09)

### 🐛 Bug Fixes

* use non-breaking spaces between icons and labels ([362add0](https://github.com/oocx/tfplan2md/commit/362add05636c6624e6aa6ad58a48383e7b0d3f26))

### 📚 Documentation

* add test plan and UAT report for [#033](https://github.com/oocx/tfplan2md/issues/033) ([882ef83](https://github.com/oocx/tfplan2md/commit/882ef83af17fe255ad0fbf49d224949d6c1a087a))

<a name="1.0.0-alpha.23"></a>
## [1.0.0-alpha.23](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.22...v1.0.0-alpha.23) (2026-01-08)

### ✨ Features

* **website:** update header to use full text logo ([6271e3c](https://github.com/oocx/tfplan2md/commit/6271e3c09831af0cddbf1faf84b4be7fac89e78a))

### 🐛 Bug Fixes

* **assets:** optimize SVGs for GitHub rendering compatibility ([c85d5fb](https://github.com/oocx/tfplan2md/commit/c85d5fb28856de89069f9eba933622a8b2132aa3))

<a name="1.0.0-alpha.22"></a>
## [1.0.0-alpha.22](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.21...v1.0.0-alpha.22) (2026-01-08)

### ✨ Features

* add logo prototype v19 with overlap and color adjustments ([9bc4a3b](https://github.com/oocx/tfplan2md/commit/9bc4a3be97de8d207973dc10200338b798449052))
* add logo prototype v20 refined based on feedback ([25f98b1](https://github.com/oocx/tfplan2md/commit/25f98b13a425413b148e99bbd97efeaccb17f861))
* add v10 refined logo concepts with splits, bubbles, cards and pipes ([ea735ab](https://github.com/oocx/tfplan2md/commit/ea735abdf856dc7bf58c8d98cc2a8c85b640782a))
* add v11 translator variations with different code bubble representations ([d8ef7a8](https://github.com/oocx/tfplan2md/commit/d8ef7a82c7d082027f21bd9b6faa07e2be0099b9))
* add v12 diff style logo concepts combining diff colors and markdown mark ([0d6a773](https://github.com/oocx/tfplan2md/commit/0d6a773d0461469ef9ffc9f63c2ccb31f69fcdda))
* add V13 logo prototypes refining cards, terminals and capsules ([6bd64b9](https://github.com/oocx/tfplan2md/commit/6bd64b9028a8d11742d5e763addf7fa52e2333de))
* add V14 logo prototypes focusing on transformation ([b5ff687](https://github.com/oocx/tfplan2md/commit/b5ff68757bc3f56a66ccff59aa238e7b4e7284e9))
* add V15 logo prototypes based on user feedback (fav [#8](https://github.com/oocx/tfplan2md/issues/8) with '2') ([6defe2e](https://github.com/oocx/tfplan2md/commit/6defe2e4e51c57ffdd2a7be501d24f3d5468d85f))
* add V16 logo prototypes focusing on seamless integration ([d49fadd](https://github.com/oocx/tfplan2md/commit/d49fadda966648aa5c9b9aa3f5fc36f05b808280))
* add V17 logo prototypes refining shadow and arrow concepts ([b0955b0](https://github.com/oocx/tfplan2md/commit/b0955b0837d76a1096330c2763d1c0579bd34de8))
* add V18 logo prototypes with compact chevron designs ([44a52c1](https://github.com/oocx/tfplan2md/commit/44a52c1c8bac49a226a6175594ab000b23f7ce02))
* add v21 logo text variants and save selected icon ([e4012c7](https://github.com/oocx/tfplan2md/commit/e4012c7ef1306679db6525f5499710f3aa94b3df))
* add v22 logo text refinements for two-tone purple concept ([b81bddb](https://github.com/oocx/tfplan2md/commit/b81bddb37c4a8cf53827abde4179be4517e4ecbd))
* add v8 hybrid logo concepts with strict markdown mark geometry ([b867bd6](https://github.com/oocx/tfplan2md/commit/b867bd6e6e4d918eef553231d4325e33c3ce1db5))
* add v9 extensive logo concepts combining chaos and structure ([71c765b](https://github.com/oocx/tfplan2md/commit/71c765b345e2b53bd3cb62c6a07bdff2770d45cf))
* create 15 logo design options for tfplan2md ([b6f44e5](https://github.com/oocx/tfplan2md/commit/b6f44e587a0b7cbfdaabcfce116939deb93a01f7))
* implement selected logo design and update all usages ([0430a45](https://github.com/oocx/tfplan2md/commit/0430a45a36f437b9b53f91f6a7300aa77ea401b4))
* **website:** add 10 CNCF-inspired logo designs ([ca9ed6e](https://github.com/oocx/tfplan2md/commit/ca9ed6e8de7e3c4f61a476072754695e2c1c52a6))
* **website:** add 10 concept-focused logo designs (v4) ([cef7bce](https://github.com/oocx/tfplan2md/commit/cef7bcece8beb0139b5400b4496d66be8b83089b))
* **website:** add 10 markdown-focused transition logo designs (v5) ([7903731](https://github.com/oocx/tfplan2md/commit/7903731ea6c9c864c94f59356d8876a91be73252))
* **website:** add 10 refined logos with compliant markdown mark (v6) ([ae63273](https://github.com/oocx/tfplan2md/commit/ae63273dc60ef768e7cb36cd250585f4a8c5c9d6))
* **website:** add 10 strict compliance markdown logo designs (v7) ([8c4cde7](https://github.com/oocx/tfplan2md/commit/8c4cde75203dad300891fece39ceb42afe257cf3))
* **website:** add 12 completely new logo designs options (v2) ([349a6bb](https://github.com/oocx/tfplan2md/commit/349a6bbf29be6837969e8dfd8ec56a23abe781f6))

### 📚 Documentation

* add comprehensive README for logo design options ([e36fb66](https://github.com/oocx/tfplan2md/commit/e36fb66b660cdbd9076e24424bb1aed90f7aa309))
* add retrospective for 031-azdo-dark-theme-support ([4d38679](https://github.com/oocx/tfplan2md/commit/4d38679e7970060d99bda778f861e6c4ccfa17e7))
* update backlog for logo redesign progress ([a5ebf7b](https://github.com/oocx/tfplan2md/commit/a5ebf7b4a914a0d667098a6ae26c8d0c27404ff3))
* **website:** update backlog status for logo redesign ([cc02daf](https://github.com/oocx/tfplan2md/commit/cc02daf7e60fa0779ad77634f0141d5f488c43a3))

<a name="1.0.0-alpha.21"></a>
## [1.0.0-alpha.21](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.20...v1.0.0-alpha.21) (2026-01-08)

### ✨ Features

* add Azure DevOps CSS variables to preview wrapper ([89d95d0](https://github.com/oocx/tfplan2md/commit/89d95d01305c8d8db7b57f434f0c97c16cff5a1a))
* update templates to use theme-adaptive border colors ([d27fe74](https://github.com/oocx/tfplan2md/commit/d27fe74e0038330e163a6faf098a468612aab6c6))

### 📚 Documentation

* add architecture for 031-azdo-dark-theme-support ([9c8deca](https://github.com/oocx/tfplan2md/commit/9c8deca9974f3d9ec7276d1950b8f5e76d981ab4))
* add feature specification for 031-azdo-dark-theme-support ([d9dae14](https://github.com/oocx/tfplan2md/commit/d9dae1455c8ff6fcb825f6776c4a32f27c79759e))
* add tasks for 031-azdo-dark-theme-support ([9f4b634](https://github.com/oocx/tfplan2md/commit/9f4b63452ef5dbc5f7c3f004162c14adad78c201))
* add test plan for 031-azdo-dark-theme-support ([9580595](https://github.com/oocx/tfplan2md/commit/9580595ac8ee0769ae930d757717c9364a14b99c))
* add UAT report for 031-azdo-dark-theme-support ([50a5985](https://github.com/oocx/tfplan2md/commit/50a5985aeb8b7187876f2cfdb93d397c37420b1a))
* mark task 4 as complete ([3561b1c](https://github.com/oocx/tfplan2md/commit/3561b1c80f1e60a1bacbb463cfbd7f44e260fec2))
* mark task 5 as complete ([517aff1](https://github.com/oocx/tfplan2md/commit/517aff1d016dbd17069c4394774f5247fc12f37d))
* mark tasks 1-3 as complete ([2a6d73d](https://github.com/oocx/tfplan2md/commit/2a6d73d5ad40bd1079b263c05fbc07c54e3f4525))
* update demo artifacts with theme-adaptive borders ([5bff449](https://github.com/oocx/tfplan2md/commit/5bff4493f546d61936185bc32501c09821646cea))
* update examples with current commit hash ([3bd0662](https://github.com/oocx/tfplan2md/commit/3bd066256698dfcc7ccaa37aa1309c55349c6bc2))
* update feature list and code review notes for 031-azdo-dark-theme-support ([f414d89](https://github.com/oocx/tfplan2md/commit/f414d891e89615b34e2211ce253a0b820301a113))

<a name="1.0.0-alpha.20"></a>
## [1.0.0-alpha.20](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.19...v1.0.0-alpha.20) (2026-01-08)

### ✨ Features

* add Dark/Light Mode feature card to Also Included section ([8f63d92](https://github.com/oocx/tfplan2md/commit/8f63d92e34f92c1f8369551a4f271b7b9f46dc74))

### 📚 Documentation

* add retrospective for feature 030 terraform-show-approximation ([4c0b5e7](https://github.com/oocx/tfplan2md/commit/4c0b5e7784d7b1da494cad76eec9ba6a7c4ee8ce))
* update site-structure.md with Dark/Light Mode addition ([2d64e1c](https://github.com/oocx/tfplan2md/commit/2d64e1c8740c04a8ef05afc7d750feec48640a4c))

<a name="1.0.0-alpha.19"></a>
## [1.0.0-alpha.19](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.18...v1.0.0-alpha.19) (2026-01-07)

### ✨ Features

* add AI workflow page with interactive diagram ([59080fb](https://github.com/oocx/tfplan2md/commit/59080fbf6f2f91e6b92766100245b0ae81f873b0))
* add GitHub Pages deployment workflow ([3147130](https://github.com/oocx/tfplan2md/commit/3147130432a2b8c2548325e2200bdd310bf8b558))
* checkpoint terraform show renderer ([8188099](https://github.com/oocx/tfplan2md/commit/81880995ab8fdb2a623f0a70d0f2908aebcecf38))
* complete website foundation with SVG icons, dark mode, and style isolation ([90ec95a](https://github.com/oocx/tfplan2md/commit/90ec95ac584fced8274a6c65a26aa7b0b6fac141))
* enable website agent for cloud execution with GitHub issues ([974f6c2](https://github.com/oocx/tfplan2md/commit/974f6c2d9566f1f01b76dd7fd7c74720a4242a5c))
* enable Workflow Engineer as dual-mode agent (local + cloud) ([9b379da](https://github.com/oocx/tfplan2md/commit/9b379daa3bede842bcf03c7f09cc9e1dcf1a63ee))
* fix ANSI formatting and improve attribute rendering ([0cfe719](https://github.com/oocx/tfplan2md/commit/0cfe719171721de17117ec345d3221e8970cb92f))
* implement attribute ordering and improve indentation/formatting ([440464e](https://github.com/oocx/tfplan2md/commit/440464ea4b81f1e6885fadded5aa56236fdd6989))
* **website:** add backlog tasks for homepage screenshot and terraform show comparison ([3d29de5](https://github.com/oocx/tfplan2md/commit/3d29de5e0a3add3a4fdbdb509c81e8730510b451))
* **website:** replace all hand-crafted examples with real generated output ([71770fb](https://github.com/oocx/tfplan2md/commit/71770fb4470f895b8945a3132420b6591d1cae8a))
* **website:** replace hand-crafted examples with real tfplan2md output ([28d2a68](https://github.com/oocx/tfplan2md/commit/28d2a68c5de12d1a6f19a8af0e47154780911da2))
* **website:** replace homepage interactive example with screenshot and lightbox modal ([4abf3bf](https://github.com/oocx/tfplan2md/commit/4abf3bf90ecbdfbd4d87a18ccc610d8a31d03958))
* **workflow:** add next-issue-number skill and update agents to use it ([853f155](https://github.com/oocx/tfplan2md/commit/853f1553551258b8be2abcfee994490b4a0accc3))

### 🐛 Bug Fixes

* add alphabetical sorting to nested block properties ([e932a8c](https://github.com/oocx/tfplan2md/commit/e932a8c3e3b7894b1e2a6e150121cbe3546990a3))
* add Deployment and Other options to website issue template, make pages-affected optional ([22bb0c6](https://github.com/oocx/tfplan2md/commit/22bb0c619d7df2f3ee47c663c18a840291572189))
* apply dark mode icon filter to all icon classes ([3e9028d](https://github.com/oocx/tfplan2md/commit/3e9028d3b31c2d38247ba1eaaea39367aba36b7f))
* backslash escaping, map key quoting, and unchanged comment variants ([b5a112d](https://github.com/oocx/tfplan2md/commit/b5a112df09d3c1a85b5cc3096d5ad82bb7c91c33))
* column width calculation excludes nested blocks and empty arrays ([196cee7](https://github.com/oocx/tfplan2md/commit/196cee7af42377248f145026c3f6a3245dab6deb))
* compute width from all properties before filtering for rendering ([96e1bc2](https://github.com/oocx/tfplan2md/commit/96e1bc2c02897a874936a944657e93a7a08a439d))
* correct map key quoting, comment text, unchanged count, and update rendering order ([cccdb1a](https://github.com/oocx/tfplan2md/commit/cccdb1a745efe1633559917e14e58e5c1b9f838d))
* correct property alignment by filtering before width calculation ([90b0645](https://github.com/oocx/tfplan2md/commit/90b0645b64b9b79e80d5c9ed48a37bdc3ec19970))
* correct read action properties to use + marker and fix alignment ([8420dd3](https://github.com/oocx/tfplan2md/commit/8420dd3f55e36d90c047fac640b7e008f08cb2f3))
* correct width calculation for nested blocks vs top-level properties ([dacd9c9](https://github.com/oocx/tfplan2md/commit/dacd9c91311dd08dc46a2631ce6ad6af5117ac7b))
* correct YAML indentation and add coding agent tools to web-designer ([dde86af](https://github.com/oocx/tfplan2md/commit/dde86af54b9d2595cbd086920b89947deb5e680e))
* improve context detection and add workflow improvement template ([8797aae](https://github.com/oocx/tfplan2md/commit/8797aae2088f2132a94a2783fb2eb5c86274be44))
* improve TerraformShowRenderer output formatting (WIP) ([1c1abe0](https://github.com/oocx/tfplan2md/commit/1c1abe047c06cc36ec3ccda651fb9e08b162a752))
* remove target field to enable web-designer for both VS Code and GitHub Copilot coding agent ([f22ea4a](https://github.com/oocx/tfplan2md/commit/f22ea4aea1b94d4ce439c1215bc107d2a0d1e3b2))
* render unchanged identifier scalars in update resources ([954ac4f](https://github.com/oocx/tfplan2md/commit/954ac4f5082122eb1edb9d4baa1249e48aea7c22))
* resolve extra blank line issue in TerraformShowRenderer ([84bf6c3](https://github.com/oocx/tfplan2md/commit/84bf6c3c4ec4a93a1a09a540feb72f21df6eb137))
* restore version to 1.0.0-alpha.18 ([d195965](https://github.com/oocx/tfplan2md/commit/d195965e9b60eded359c23ce1bd73b19326147bb))
* update HTML rendering baselines to match current version and commit hash SNAPSHOT_UPDATE_OK ([aa4f894](https://github.com/oocx/tfplan2md/commit/aa4f894633753d6374fc9b55092ae5f57c2554b4))
* use correct marker and color for read actions ([582e436](https://github.com/oocx/tfplan2md/commit/582e4363406e3e76ac201b6b69eda4016f711be3))
* **terraform-show:** avoid duplicate blank lines in no-color output and add tests ([b2289af](https://github.com/oocx/tfplan2md/commit/b2289af6d0e5232ed3c7e8524457058aedbc5c08))
* **website:** improve code block contrast in examples ([d5eccce](https://github.com/oocx/tfplan2md/commit/d5eccce99d0685bd87217eb4d8c0d90df11968de))
* **website:** improve dark mode rendering to match Azure DevOps ([33a7b38](https://github.com/oocx/tfplan2md/commit/33a7b38bf44c7b591813632dd164a14809f2dbe9))

### 🚀 Performance

* skip PR validation for website-only changes ([b2fe3e3](https://github.com/oocx/tfplan2md/commit/b2fe3e3ba8aa2b75f8ecf8d199800f5c22a328ca))

### ♻️ Refactoring

* move CI/CD Integration to Built-In Capabilities section ([9c1c207](https://github.com/oocx/tfplan2md/commit/9c1c207536361a132ddbd034d6111d007eecfbf5))
* optimize width calculation by filtering first ([d015d7e](https://github.com/oocx/tfplan2md/commit/d015d7e60a803377ee7a8e725258cc0c5c9555a4))
* simplify workflow improvement template ([d82582f](https://github.com/oocx/tfplan2md/commit/d82582f29889fa788bc4bc9b9f76db52c9dc116c))

### 📚 Documentation

* add architecture for terraform show approximation ([253863d](https://github.com/oocx/tfplan2md/commit/253863d0dee58f9d55c554d7bf0d37ac057b8999))
* add cloud orchestrator pattern section with sub-issue coordination ([06ac141](https://github.com/oocx/tfplan2md/commit/06ac141e623dd577d2f297f9b1efc5fe1a860e12))
* add comprehensive section on multi-agent handoffs and label-based routing ([3e4e03d](https://github.com/oocx/tfplan2md/commit/3e4e03d9f0f2446414192628995824b8ed7741e9))
* add deployment workflow documentation ([5fc6663](https://github.com/oocx/tfplan2md/commit/5fc6663173a4d3437802838a0610506a816dc0bc))
* add feature specification for 030-terraform-show-approximation ([7f02b4c](https://github.com/oocx/tfplan2md/commit/7f02b4cc1e2eb62e7cdda5236f4bc40dff4f253e))
* add implementation summary for cloud agent support ([64061b7](https://github.com/oocx/tfplan2md/commit/64061b782ad83e556e67e5146f125ab1e094a990))
* add README for cloud agents analysis folder ([42ca09c](https://github.com/oocx/tfplan2md/commit/42ca09c88b4403d3e0fa733002e8f437fd670e81))
* add tasks for terraform show approximation tool ([8bd0065](https://github.com/oocx/tfplan2md/commit/8bd0065696142d206a8927b44c9a99de8820e915))
* add test plan and update specification for 030-terraform-show-approximation ([732225b](https://github.com/oocx/tfplan2md/commit/732225b0a594a1a7a23ad3bf8b9005278307124f))
* comprehensive cloud agents analysis for tfplan2md workflow ([8900017](https://github.com/oocx/tfplan2md/commit/8900017010340643e3489881fb1654428a92da6e))
* enhance Copilot instructions with project overview and tech stack ([9e5f1c5](https://github.com/oocx/tfplan2md/commit/9e5f1c5bb50ed87077a35c4135c2461c1697e22c))
* finalize 030-terraform-show-approximation with UAT results and documentation ([cd135df](https://github.com/oocx/tfplan2md/commit/cd135df41f99eb781643aa7bd15bdcbcd10155f2))
* update branch naming convention for website work ([c0462ba](https://github.com/oocx/tfplan2md/commit/c0462babe0cbcfe82f0755d7f1cc9d82d9801ad6))
* **website:** add backlog task [#22](https://github.com/oocx/tfplan2md/issues/22) for replacing examples on all pages ([469ed16](https://github.com/oocx/tfplan2md/commit/469ed1625f16c54538fb9674a42474ed10d5a534))

<a name="1.0.0-alpha.17"></a>
## [1.0.0-alpha.17](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.16...v1.0.0-alpha.17) (2026-01-03)

### ✨ Features

* **workflow:** add website memory backlog ([ee2073b](https://github.com/oocx/tfplan2md/commit/ee2073bfde2f85dbc827aa8b7f0778d921b2c56f))

### 📚 Documentation

* **workflow:** add retrospective and metrics for feature 029 ([836a258](https://github.com/oocx/tfplan2md/commit/836a2584dd78726b799363b69bf98c7595c4c871))

<a name="1.0.0-alpha.16"></a>
## [1.0.0-alpha.16](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.15...v1.0.0-alpha.16) (2026-01-03)

### ✨ Features

* add deterministic metadata provider ([fe9ed1e](https://github.com/oocx/tfplan2md/commit/fe9ed1ec20c8ea844b9b867dc8c649c8ce6ac48e))
* add header metadata flag and rendering ([c119a8c](https://github.com/oocx/tfplan2md/commit/c119a8c30876a62e44f35c91b508f636dd27b532))
* add semantic icons for names ([cb003e5](https://github.com/oocx/tfplan2md/commit/cb003e5205299c21c8453b45686b5d813c871763))
* style resource details with borders\n\nSNAPSHOT_UPDATE_OK ([c419ccb](https://github.com/oocx/tfplan2md/commit/c419ccb10ea3d9f1c81c2a0f07229ebef6a0bada))

### 🐛 Bug Fixes

* add semantic icons to Azure scope formatting ([144ccda](https://github.com/oocx/tfplan2md/commit/144ccda28cd6a6ea3b7bc066ab532b8ebf1de383))
* improve change constructors and named args ([f341649](https://github.com/oocx/tfplan2md/commit/f341649049ed189464ac090e4d7945bc31cb6f9f))
* remove duplicate Generated timestamp from report header ([fc079c8](https://github.com/oocx/tfplan2md/commit/fc079c87c07576887f0ab36593505689668c5286))

### 📚 Documentation

* add architecture for report presentation enhancements ([1f39c8a](https://github.com/oocx/tfplan2md/commit/1f39c8abc555e6278cf183467bad11e7360e86b6))
* add feature specification for 025-report-presentation-enhancements ([80fe64f](https://github.com/oocx/tfplan2md/commit/80fe64f952b7be86a9ec914fb21f87ad42b7f576))
* add tasks for report-presentation-enhancements ([69dcb10](https://github.com/oocx/tfplan2md/commit/69dcb10185e62f85b363df4574c75db3c09b320a))
* add test plan and UAT test plan for 029-report-presentation-enhancements ([ed37465](https://github.com/oocx/tfplan2md/commit/ed37465cbca0ba1663350e0530c08fa23c04b785))
* mark task 1 complete ([a78aff7](https://github.com/oocx/tfplan2md/commit/a78aff7420bbcc9ae43a19bdbb7a84df0c833e78))
* mark task 2 complete ([4bc2ebf](https://github.com/oocx/tfplan2md/commit/4bc2ebfaf78681235f43d1a5c02a6f06bc3402bc))
* regenerate demo artifacts with UAT fixes ([5a94766](https://github.com/oocx/tfplan2md/commit/5a94766a5b87098ccd805db4ca2607df933c9fcf))
* regenerate HTML artifacts with updated header format ([3ae23d4](https://github.com/oocx/tfplan2md/commit/3ae23d40fc57d200394e323f8d6aa43b9e34aad0))
* update rendering snapshots for report presentation enhancements ([2bc1900](https://github.com/oocx/tfplan2md/commit/2bc19001d9180f6b8c671e97e48e0e563db36f48))
* update task 6 checklist ([4b16b15](https://github.com/oocx/tfplan2md/commit/4b16b154e80eafafeea9077fe6fa9d618c6b76da))
* update UAT results for report presentation enhancements ([ef7f2f8](https://github.com/oocx/tfplan2md/commit/ef7f2f8577498358971178a0a5dbe9d1fc9947cd))
* **feature-029:** add code review report ([9049245](https://github.com/oocx/tfplan2md/commit/9049245e32475915f25232526f1137cdadfd607d))
* **feature-029:** document report presentation enhancements ([4fd8553](https://github.com/oocx/tfplan2md/commit/4fd8553035e5c580f66cdc4ab748ce1d29548501))
* **feature-029:** update comprehensive demo artifact with latest enhancements ([2cc7cec](https://github.com/oocx/tfplan2md/commit/2cc7cec49b2e3ac82afe05c91ccf632483264214))
* **workflow:** add retrospective for feature 028 ([221ccfc](https://github.com/oocx/tfplan2md/commit/221ccfcc4c078761da0f6426c7124ca67bf1fa53))

<a name="1.0.0-alpha.15"></a>
## [1.0.0-alpha.15](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.14...v1.0.0-alpha.15) (2026-01-02)

### ✨ Features

* add HTML screenshot generator tool ([51ba5b2](https://github.com/oocx/tfplan2md/commit/51ba5b2477cfc6982d4e39ffc94ce914e68d7268))

### 📚 Documentation

* add architecture for 028 html screenshot generation ([6de3100](https://github.com/oocx/tfplan2md/commit/6de31003f5e92a78af9995eecab54bdbd8659daf))
* add feature specification for 028-html-screenshot-generation ([221f146](https://github.com/oocx/tfplan2md/commit/221f146e72fc8d8ee29937d296464fe8aca7a147))
* add tasks for 028-html-screenshot-generation ([f65ab17](https://github.com/oocx/tfplan2md/commit/f65ab17a3adb0b42b83d9163568c1b2a77812ac0))
* add test plans for 028-html-screenshot-generation ([ab38f8e](https://github.com/oocx/tfplan2md/commit/ab38f8ef8445fac4839bc2780094a2d3d4c3fdd8))
* mark feature 028 as implemented and approved ([df1e73b](https://github.com/oocx/tfplan2md/commit/df1e73b493f5e34d0e0e9643869240dbb885759b))

<a name="1.0.0-alpha.14"></a>
## [1.0.0-alpha.14](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.13...v1.0.0-alpha.14) (2026-01-01)

### ✨ Features

* improve workflow prompts and handoffs ([a608196](https://github.com/oocx/tfplan2md/commit/a6081962d3db68a3ad8c2e04c030ed262c12359a))
* **workflow:** add per-agent starter prompts ([5ace647](https://github.com/oocx/tfplan2md/commit/5ace6471cf2e0e91a62e468a37342a692071c826))

### 🐛 Bug Fixes

* **workflow:** avoid option vs task id ambiguity ([6d770db](https://github.com/oocx/tfplan2md/commit/6d770dbc8e0a209277ebc763cb1cbb04e9ecc76d))
* **workflow:** improve developer progress visibility ([e71cfe5](https://github.com/oocx/tfplan2md/commit/e71cfe50717cd99c2f462ea63267b93ce2524766))
* **workflow:** show 3 options before selection ([16e9923](https://github.com/oocx/tfplan2md/commit/16e99239e66ed2fc973b5ac8e28e1f91162bfbad))

### ♻️ Refactoring

* **workflow:** sync prompt files with handoffs ([945a7ea](https://github.com/oocx/tfplan2md/commit/945a7ea8e4a76210acd48c233a3ff9555b7e0470))

### 📚 Documentation

* add redacted chat logs for feature 027 ([4f9b8db](https://github.com/oocx/tfplan2md/commit/4f9b8db151f117bc0a09cf14ca7c598085df8710))
* add retrospective for feature 027 and fix extract-metrics.sh ([5f410ca](https://github.com/oocx/tfplan2md/commit/5f410caf47673a42ef74f03387be3fbb59303d34))
* **workflow:** add 028 improvement opportunities ([21b4531](https://github.com/oocx/tfplan2md/commit/21b4531a0ae9a26240b274d16dff800fc4292ab0))
* **workflow:** mark task 1 done (PR [#187](https://github.com/oocx/tfplan2md/issues/187)) ([558e7ea](https://github.com/oocx/tfplan2md/commit/558e7ea6ab1e200c6585feda0756cf339d6c23aa))
* **workflow:** mark task 2 done ([6422b6a](https://github.com/oocx/tfplan2md/commit/6422b6ac89df94ee26aca746cf565acfa5443eef))
* **workflow:** require global unique NNN across change types ([5b5fe16](https://github.com/oocx/tfplan2md/commit/5b5fe168410da7ecdbb23bc25fd7803d72c20cfc))
* **workflow:** require recap and next steps when blocked ([243247d](https://github.com/oocx/tfplan2md/commit/243247d10099a4a13ca291042e98823132eb44d0))
* **workflow:** standardize branch naming for prompt inference ([281c61f](https://github.com/oocx/tfplan2md/commit/281c61fd9b1fa9bd35843d247b980ca79cda9d25))

<a name="1.0.0-alpha.13"></a>
## [1.0.0-alpha.13](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.12...v1.0.0-alpha.13) (2026-01-01)

### ✨ Features

* add html renderer tool and tests ([382ab00](https://github.com/oocx/tfplan2md/commit/382ab008872dc09debd8cf7c01af5f2c91d17926))
* add merge conflict resolution skill ([d4ee8fa](https://github.com/oocx/tfplan2md/commit/d4ee8fa561d6ca8cabf117d4f0557173f296440b))
* add wrapper templates and applier ([7d7bf38](https://github.com/oocx/tfplan2md/commit/7d7bf387d365fee5c911995878410b8c2322a6a1))

### 🐛 Bug Fixes

* align renderer markup with gold outputs ([eeb3fc5](https://github.com/oocx/tfplan2md/commit/eeb3fc5e5a5bf53c7a0ad3b76809fb085ef3f904))
* **agents:** remove notebook tool references ([08fb150](https://github.com/oocx/tfplan2md/commit/08fb1502fb8b27e6f7cb4216272cf190b2c759c3))

### ♻️ Refactoring

* split html post-processing ([e867476](https://github.com/oocx/tfplan2md/commit/e867476f2d9b8c379bdfe738aef26bffd50ea877))

### 📚 Documentation

* add architecture for markdown html rendering ([8cd93af](https://github.com/oocx/tfplan2md/commit/8cd93affecc94fe8e85d4a351bda959582b0dccb))
* add code review report for feature 027 ([d6ed5bd](https://github.com/oocx/tfplan2md/commit/d6ed5bd044a73151747da8b49b1f1939e3545686))
* add feature specification for markdown HTML rendering tool ([161e666](https://github.com/oocx/tfplan2md/commit/161e66623d3a5339e1be253e2adac41f2194c4b7))
* add html renderer usage ([15462d8](https://github.com/oocx/tfplan2md/commit/15462d83286ed8bcb8c014f2c3a5bc1b4869f100))
* add tasks for markdown-html-rendering ([47a0123](https://github.com/oocx/tfplan2md/commit/47a01232920a3617030636a282ea6350baa2e7ca))
* add test plan and UAT test plan for 027-markdown-html-rendering ([9dc9a0e](https://github.com/oocx/tfplan2md/commit/9dc9a0ebc050905872053ada72ba78bb16a708e8))
* expand HTML renderer documentation in features.md ([35d43b5](https://github.com/oocx/tfplan2md/commit/35d43b50eec744befca4e568842aca59e59dd860))
* mark markdown html rendering tasks complete ([0a4e627](https://github.com/oocx/tfplan2md/commit/0a4e6279009212e993dbc5a0d0bea388ff20541b))
* **agents:** treat non-functional improvements as features ([ca81754](https://github.com/oocx/tfplan2md/commit/ca81754c2e816478ef6ee37cdab771d20e2e58b6))
* **workflow:** update 025/026 task statuses ([4660b98](https://github.com/oocx/tfplan2md/commit/4660b98bbf7b9038f0d79fcd1210aaad5d43e1c1))

<a name="1.0.0-alpha.12"></a>
## [1.0.0-alpha.12](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.11...v1.0.0-alpha.12) (2026-01-01)

### 🐛 Bug Fixes

* stop guessing agent attribution in retros ([bd1b8f9](https://github.com/oocx/tfplan2md/commit/bd1b8f9c3f039709004e5904f4e7aa2619a5958e))

### 📚 Documentation

* **workflow:** clarify numeric option selection ([0962b07](https://github.com/oocx/tfplan2md/commit/0962b07d1019b88436862160567690d684e188cf))
* **workflow:** require snapshot justification in reviews ([c41d0ff](https://github.com/oocx/tfplan2md/commit/c41d0ffd5aad4dae6f8c4571ead182d0a5beff82))
* **workflow:** require tests before marking done ([329e866](https://github.com/oocx/tfplan2md/commit/329e866161ea5a40ea8edd245cad123fdcf4ebbb))
* **workflow:** update improvement tracker statuses ([7d55a5a](https://github.com/oocx/tfplan2md/commit/7d55a5a12481257aae5c5c4ba2bef9ea3cf43ea6))

<a name="1.0.0-alpha.11"></a>
## [1.0.0-alpha.11](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.10...v1.0.0-alpha.11) (2025-12-31)

### ✨ Features

* **workflow:** add snapshot integrity guardrail ([1f256c7](https://github.com/oocx/tfplan2md/commit/1f256c757a708b54c6939b0a26d349ab8d487d1b))

### 📚 Documentation

* **workflow:** prefer direct script invocation ([8cbe3f6](https://github.com/oocx/tfplan2md/commit/8cbe3f65e0f6210db59e432bc82d2727bb8814c6))
* **workflow:** require direct script invocation ([6895152](https://github.com/oocx/tfplan2md/commit/68951523e703504c4429f130dcf0145416ce4747))

<a name="1.0.0-alpha.10"></a>
## [1.0.0-alpha.10](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.9...v1.0.0-alpha.10) (2025-12-31)

### ✨ Features

* **workflow:** add dotnet test timeout wrapper ([f1b13dd](https://github.com/oocx/tfplan2md/commit/f1b13dd69e18891a6d8e7a7c8807a8636fc9cd2a))

<a name="1.0.0-alpha.9"></a>
## [1.0.0-alpha.9](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.8...v1.0.0-alpha.9) (2025-12-31)

### 🐛 Bug Fixes

* **uat:** stop polling on abandoned PRs ([6332dab](https://github.com/oocx/tfplan2md/commit/6332dabc53f17926bff8066d17631604e1d5ddee))

<a name="1.0.0-alpha.8"></a>
## [1.0.0-alpha.8](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.7...v1.0.0-alpha.8) (2025-12-31)

### ✨ Features

* **workflow:** add workspace-local temp file policy ([a43a52b](https://github.com/oocx/tfplan2md/commit/a43a52b89c783635bb5b8cc4c70c2b3c330c6b48))

### 🐛 Bug Fixes

* **agent:** prevent web-designer from making unrelated changes and starting implementation without approval ([01dd0d7](https://github.com/oocx/tfplan2md/commit/01dd0d7f78d2fe376754c67e42a073ad881520d5))

### 📚 Documentation

* finalize retrospective for feature 026 and improve analyze-chat.py ([01eef99](https://github.com/oocx/tfplan2md/commit/01eef9981fab48097d92274100d4b5ac8ccff0ff))

<a name="1.0.0-alpha.7"></a>
## [1.0.0-alpha.7](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.6...v1.0.0-alpha.7) (2025-12-31)

### ✨ Features

* add formatted value wrappers ([7585207](https://github.com/oocx/tfplan2md/commit/758520756edc6e13d18c56dbd1dba0e5b249e27e))
* add template loader and resolver ([8f8ef92](https://github.com/oocx/tfplan2md/commit/8f8ef92ac128d1026485dd8ba7001f8e15131a50))
* add view models and factories for NSG, firewall, and role assignment ([1fcdfe8](https://github.com/oocx/tfplan2md/commit/1fcdfe88c061b68dbadff0f98ced31d894c9b26b))
* add Web Designer agent for website development and maintenance ([c538de8](https://github.com/oocx/tfplan2md/commit/c538de86f8411f08520e816b8a7a6d7306fdbcaf))
* implement template rendering simplification and fix naming inconsistencies ([926cbd0](https://github.com/oocx/tfplan2md/commit/926cbd0ea2bcc0ccc6e800d6ad7a0bb08f0575bb))
* split scriban helpers ([690fb81](https://github.com/oocx/tfplan2md/commit/690fb818a711bc56fd3e68c27671301f6b6e6c09))
* **workflow:** add improvements from feature 024 retrospective ([a82c96c](https://github.com/oocx/tfplan2md/commit/a82c96c5ade28a3725ba56dc75f77fab21c0cf03))

### 🐛 Bug Fixes

* resolve CA1859 warnings in view model factories ([a1f007f](https://github.com/oocx/tfplan2md/commit/a1f007fdd989513f5b36eda6ed448de252079cb4))
* restore principal name display and boolean lowercase formatting ([4d5e9ff](https://github.com/oocx/tfplan2md/commit/4d5e9ffd610b48ae5ed2f7acede109f5b195017b))
* restore simple-diff markdown output and rename from standard-diff ([5ed981b](https://github.com/oocx/tfplan2md/commit/5ed981bb879075325e94ef5049f5bb98a4adc497))
* **rendering:** deduplicate principal type and icon in role assignments ([c0fc85c](https://github.com/oocx/tfplan2md/commit/c0fc85c4a1c2bce5f8a6a7694db14930b51abe27))

### ♻️ Refactoring

* migrate role assignment template to view model pattern ([cfe9f19](https://github.com/oocx/tfplan2md/commit/cfe9f19aaa5f795a22ee05e32c005abdae179e34))
* remove HTML anchor comments from templates ([cadf930](https://github.com/oocx/tfplan2md/commit/cadf930c71ba5b6ee8c0a542663a1f6d5ef1dd02))
* **firewall:** complete view model migration for firewall template ([0bfa22c](https://github.com/oocx/tfplan2md/commit/0bfa22c9a781ce1135499ad5568555df4ed85945))
* **nsg:** complete view model migration for NSG template ([486795c](https://github.com/oocx/tfplan2md/commit/486795ca6b301793a9875b447971e8d5d1e5d837))
* **nsg:** use view model for update scenario rules table ([160a556](https://github.com/oocx/tfplan2md/commit/160a556216c0b546e9f32a045c15c974916f6151))
* **rendering:** implement single-pass template dispatch ([5fdc4f2](https://github.com/oocx/tfplan2md/commit/5fdc4f29ab2a9b93effebc86346e2c04db840c02))

### 📚 Documentation

* add architecture for 026-template-rendering-simplification ([7f1e487](https://github.com/oocx/tfplan2md/commit/7f1e487783cf4cebb550d331f274cac96bc1cbba))
* add feature specification for 026-template-rendering-simplification ([ef68478](https://github.com/oocx/tfplan2md/commit/ef68478d0640d21d3153c4a6982ba9e0add98d00))
* add retrospective for visual report enhancements and analysis script ([47cab26](https://github.com/oocx/tfplan2md/commit/47cab264c674ff81eb286306329efcf66932f9b5))
* add tasks for template rendering simplification ([7b99331](https://github.com/oocx/tfplan2md/commit/7b993311e6b0fe205a6d618878bf82f188feee6a))
* add test plan for 026-template-rendering-simplification ([c2dd6f1](https://github.com/oocx/tfplan2md/commit/c2dd6f1c32e3caf6151f01570a76a09da9d720d2))
* add UAT report for template rendering simplification (failed) ([8b4543b](https://github.com/oocx/tfplan2md/commit/8b4543b69b82c168e0da55e5fb8f0251b2f1b5dd))
* adopt numbered feature and issue folders ([9c52f29](https://github.com/oocx/tfplan2md/commit/9c52f29330b25311d3846ec423befcdb8079e694))
* align workflow numbering with chronology ([706c9f0](https://github.com/oocx/tfplan2md/commit/706c9f08634ac8e3be0b0af088c34cfbaa66de0d))
* implement global chronological numbering for features, issues, and workflow ([d3573c5](https://github.com/oocx/tfplan2md/commit/d3573c59c2bc82b49fa59688cbe790842cc18221))
* number workflow docs folders ([0d493ee](https://github.com/oocx/tfplan2md/commit/0d493eef1e2cb0bff45a41b6077659a129333827))
* update demo artifacts for single-pass rendering ([2ddcaca](https://github.com/oocx/tfplan2md/commit/2ddcaca2dc9cf534ce019573ae2b22ebf5e79d1c))
* **workflow:** add visual feedback analysis and reprioritize ([73219cd](https://github.com/oocx/tfplan2md/commit/73219cd80859a530c3735f9eca1952689101aa15))

<a name="1.0.0-alpha.6"></a>
## [1.0.0-alpha.6](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.5...v1.0.0-alpha.6) (2025-12-29)

### ✨ Features

* **visual:** enhance report with semantic icons, collapsible sections, and improved layout ([9549ad1](https://github.com/oocx/tfplan2md/commit/9549ad1293c973b21834246b43b0ace5324fb31e))

### 📚 Documentation

* replace architecture.md with comprehensive arc42 documentation ([7ca9ad3](https://github.com/oocx/tfplan2md/commit/7ca9ad3b526ee2ba3068c2c58278fd868fd1228b))

<a name="1.0.0-alpha.5"></a>
## [1.0.0-alpha.5](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.4...v1.0.0-alpha.5) (2025-12-29)

### ✨ Features

* add arc42 architecture documentation skill for Architect agent ([176e00b](https://github.com/oocx/tfplan2md/commit/176e00b6efc09699a83f9235c2c01bac546dad45))

### ♻️ Refactoring

* enforce tool preference and todo tracking in arc42 skill ([a847cfb](https://github.com/oocx/tfplan2md/commit/a847cfb3f0f7c6a5d60a2443986438e68bb3d3d2))
* strengthen arc42 skill requirements for documentation integrity ([497b142](https://github.com/oocx/tfplan2md/commit/497b142c41f94b0ed1f94f426db30b69c6d84fba))

### 📚 Documentation

* add Use Cases section highlighting pull request review challenges ([06de975](https://github.com/oocx/tfplan2md/commit/06de9759baa83dd52bec1da3d543489b58460b61))

<a name="1.0.0-alpha.4"></a>
## [1.0.0-alpha.4](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.3...v1.0.0-alpha.4) (2025-12-28)

### ✨ Features

* add rejection tracking to retrospective analysis and update checklist ([e43b88c](https://github.com/oocx/tfplan2md/commit/e43b88cafc424f7ba088df9c3380e9c369b7b8db))

### 📚 Documentation

* add GitHub community standards and open source readiness ([5160a17](https://github.com/oocx/tfplan2md/commit/5160a17c40cb81bab26b17c4d8f7e0a472b286b4))
* add retrospective for workflow improvement cycle ([b92db7b](https://github.com/oocx/tfplan2md/commit/b92db7b0fc90d7df4ac946f047a074a3e88749fa))

<a name="1.0.0-alpha.3"></a>
## [1.0.0-alpha.3](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.2...v1.0.0-alpha.3) (2025-12-28)

### ✨ Features

* **workflow:** add agent validation tool and update improvement statuses ([3a0d9f9](https://github.com/oocx/tfplan2md/commit/3a0d9f9811491542f3839ff96ff21e3028b8077b))
* **workflow:** add validate-agent skill ([455eabb](https://github.com/oocx/tfplan2md/commit/455eabbbb7e875a5d70db33751ab2bdf4e02f7f0))

<a name="1.0.0-alpha.2"></a>
## [1.0.0-alpha.2](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.1...v1.0.0-alpha.2) (2025-12-28)

### ✨ Features

* **workflow:** add analyze-run.sh and update improvement statuses ([54dc66c](https://github.com/oocx/tfplan2md/commit/54dc66c2a41de5d02ee6a51f2f93c12665f0be51))

<a name="1.0.0-alpha.1"></a>
## [1.0.0-alpha.1](https://github.com/oocx/tfplan2md/compare/v1.0.0-alpha.0...v1.0.0-alpha.1) (2025-12-28)

### ✨ Features

* **retrospective:** add automation suggestions and DoD checklist ([b043372](https://github.com/oocx/tfplan2md/commit/b043372f3bac611e55309ce383b1bf66c3fda2e0))
* **workflow:** enforce pager suppression in scripts and agent instructions ([adb32ec](https://github.com/oocx/tfplan2md/commit/adb32ec29b4932fbb059821825587b585e8adb1e))

### 🐛 Bug Fixes

* **agent:** enforce Task Planner boundary - no implementation without approval ([7efc708](https://github.com/oocx/tfplan2md/commit/7efc708b834631a8ea264fa50d34937f8b5e2a5a))
* **agents:** implement 6 quick-win workflow improvements ([83c9294](https://github.com/oocx/tfplan2md/commit/83c9294c075876880fa7a71d931818ab162a808d))
* **ci:** ensure PR validation runs for all PRs ([b5f17a3](https://github.com/oocx/tfplan2md/commit/b5f17a3cfae7308112b95bf7c0b4d8fe9962f74a))
* **ci:** remove path filters from UAT validation workflow ([d7b19e4](https://github.com/oocx/tfplan2md/commit/d7b19e41b9f56945a33ad32ed7639143e440249d))
* **ci:** run PR validation on workflow changes ([9252b83](https://github.com/oocx/tfplan2md/commit/9252b832c82e4ac17b108ef3ed7fb0adf41b314d))
* **retrospective:** strengthen retrospective rubric ([2a5acce](https://github.com/oocx/tfplan2md/commit/2a5acced7db8beabad21329a6ef7866797d1c74e))
* **workflow:** adjust agent models based on retrospectives ([45a4d77](https://github.com/oocx/tfplan2md/commit/45a4d77fa34b8a24eafc1c097ae7b98ac9a18ba6))

### 📚 Documentation

* add improvement [#21](https://github.com/oocx/tfplan2md/issues/21) - fix UAT artifact validation check ([475297c](https://github.com/oocx/tfplan2md/commit/475297ce1e5bc55f23f904461fc9eaeec3b68c26))
* add Issue Analyst to retrospective agent performance ([fcf86b6](https://github.com/oocx/tfplan2md/commit/fcf86b6d02181cd09237f9b6ecf60824f4efa9f6))
* update retrospective with critical user feedback ([5664e39](https://github.com/oocx/tfplan2md/commit/5664e394dd393ab767bcd5e4ba9fa8d6f218e3a7))
* **workflow:** mark release/versionize improvements complete ([b2842fd](https://github.com/oocx/tfplan2md/commit/b2842fd62a766f22e7b6c71edfe0f02ccdff6d15))
* **workflow:** note tests unnecessary for docs-only changes ([07a6f31](https://github.com/oocx/tfplan2md/commit/07a6f31e712601892f338e215b2c86f273c08a4c))
* **workflow:** update improvement statuses ([df83a0e](https://github.com/oocx/tfplan2md/commit/df83a0ea0a10d278574898edefba556ed6054703))

<a name="1.0.0-alpha.0"></a>
## [1.0.0-alpha.0](https://github.com/oocx/tfplan2md/compare/v0.49.0...v1.0.0-alpha.0) (2025-12-27)

### ✨ Features

* add simulation mode constraints to UAT agents ([f4881f1](https://github.com/oocx/tfplan2md/commit/f4881f1301205ebc14bcb6641476e81d6c46eadd))
* add test instructions to UAT PR bodies ([cf1507b](https://github.com/oocx/tfplan2md/commit/cf1507b47f56bc06fcaf875cea7a2de3e1af3662))
* allow custom report title ([275d884](https://github.com/oocx/tfplan2md/commit/275d88404889510c6f0d28b27d7a683f7b3cf3a8))
* implement strict simulation mode for UAT workflow ([5779737](https://github.com/oocx/tfplan2md/commit/5779737cb20753ccd206b6a4a5e2e80e9b0a4c01))
* implement subagent pattern for autonomous UAT execution ([5b38711](https://github.com/oocx/tfplan2md/commit/5b38711fe2d8a9361309194610e5701b050632f6))
* **agent:** post PR overview links before running UAT script ([775f4a6](https://github.com/oocx/tfplan2md/commit/775f4a6e15d1ac1d4cfb2dda47d66a91c2033f3f))
* **retrospective:** add agent-grouped analysis and automation insights ([fd203a0](https://github.com/oocx/tfplan2md/commit/fd203a014d84ccbd7c99703822ba40fa222c05fd))
* **retrospective:** add rejection analysis and time breakdown ([8d33fc5](https://github.com/oocx/tfplan2md/commit/8d33fc58646e94c4fbce5547b8b463eb1d165d9a))
* **scripts:** add stdin support to PR scripts to avoid temp files ([7677f99](https://github.com/oocx/tfplan2md/commit/7677f998d4744144ab7e5048a323de28773f6e46))
* **skill:** add extract-metrics.sh script for chat export analysis ([b8c9559](https://github.com/oocx/tfplan2md/commit/b8c9559868278cbed0736bc30be2b11ce176e604))
* **skill:** add JSON output to extract-metrics.sh for cross-feature analysis ([fe4e2a1](https://github.com/oocx/tfplan2md/commit/fe4e2a15dda6353420d066d27e9100c854cceb27))
* **skills:** add analyze-chat-export skill for retrospective metrics ([00cfb41](https://github.com/oocx/tfplan2md/commit/00cfb4138790be09dd3540070acb167903fdd639))
* **uat:** add background agent for autonomous UAT execution ([0176192](https://github.com/oocx/tfplan2md/commit/0176192bd586a407200323aab04415deead95b59))
* **uat:** configure GitHub UAT to use dedicated tfplan2md-uat repository ([6e882cc](https://github.com/oocx/tfplan2md/commit/6e882cc894a01a0ed8fad3a0b0e6fab3a25f1c7c))
* **uat:** output PR URLs in UAT scripts ([8216229](https://github.com/oocx/tfplan2md/commit/82162299c65ffc725d0b9495225f235063b1f1ff))

### 🐛 Bug Fixes

* address report-title review feedback ([21dd74c](https://github.com/oocx/tfplan2md/commit/21dd74c1b815477e6ed84a3994de441d5e095f7d))
* remove quotes from EOF delimiter in release workflow ([a5e4090](https://github.com/oocx/tfplan2md/commit/a5e4090ae59aa7def2fe3f1d034fb2868b4e1698))
* **agent:** simplify UAT Tester to run single command without monitoring ([892eb7e](https://github.com/oocx/tfplan2md/commit/892eb7efd910c5b3b57f7f2584d3808897ad9c60))
* **agent:** UAT Tester should run script in blocking mode, not background ([231ee5e](https://github.com/oocx/tfplan2md/commit/231ee5ee1a601a1522f299a6f713bb4add03edbf))
* **uat:** correct background agent tool definitions ([b94fe59](https://github.com/oocx/tfplan2md/commit/b94fe59800ef28cca6c76db1faf6b4ff56adca29))
* **uat:** enable polling in simulation mode to allow approval testing ([3ccf23e](https://github.com/oocx/tfplan2md/commit/3ccf23eddb9e944a743a49bf0367ce43147af172))
* **uat:** remove leftover simulation artifact template ([7982930](https://github.com/oocx/tfplan2md/commit/79829303ee039e87d5e4ae07616454406bcb4c94))

### ♻️ Refactoring

* require feature-specific test descriptions in UAT PRs ([a771882](https://github.com/oocx/tfplan2md/commit/a77188296885f994d835c00ab26b7709c2505f5d))
* simplify UAT to single agent calling uat-run.sh directly ([77bc2e3](https://github.com/oocx/tfplan2md/commit/77bc2e3f1535f9d97e9b492b65e1fc370dc04189))
* **scripts:** remove --body-file and --description options, enforce stdin-only ([4338625](https://github.com/oocx/tfplan2md/commit/4338625bf16197c1e8c34b58a7b354b7c6b170b9))
* **uat:** simplify simulation to use default artifacts ([50827df](https://github.com/oocx/tfplan2md/commit/50827dfec1c98c4d66cadfdd4b558d966b0656e0))

### 📚 Documentation

* add architecture for custom report title ([83fa608](https://github.com/oocx/tfplan2md/commit/83fa608bc15f6211fd19f231a0e99f94a2bf9f46))
* add detailed examples for feature-specific test descriptions ([7154403](https://github.com/oocx/tfplan2md/commit/71544034bff0e8702f8896eb7fa2c71f618e2d2f))
* add feature specification for custom report title ([37cfabf](https://github.com/oocx/tfplan2md/commit/37cfabfccd57f265b317e89c36b6ab9d02aadd4a))
* add implementation tasks for custom report title ([3a7eef4](https://github.com/oocx/tfplan2md/commit/3a7eef4876ce31c5bd355e1e689bf87ed5b8657e))
* add mandatory artifact regeneration checklist for bug fixes ([fd54ab8](https://github.com/oocx/tfplan2md/commit/fd54ab88052cb9f83af3706d8c0c459a6904a3dc))
* add retrospective for custom report title feature ([cf5c4e3](https://github.com/oocx/tfplan2md/commit/cf5c4e3cc20008027bad1e91e00193f04c0eeadc))
* add retrospective improvements tracker ([91ac9a5](https://github.com/oocx/tfplan2md/commit/91ac9a515f1d4af88573ff81b41533c42461cf27))
* add test plan for custom report title ([ae2e922](https://github.com/oocx/tfplan2md/commit/ae2e9229092e7145e7efd72eb18a70a368f30f06))
* add user note about automation rate to retrospective ([f7d844b](https://github.com/oocx/tfplan2md/commit/f7d844b85bfc5a8e0c1964538fcb7be95fcd6a4d))
* align release docs with tag-only triggers ([e77d817](https://github.com/oocx/tfplan2md/commit/e77d817e01f5c38d31ac514036d01683b72381e2))
* clarify that templates add the # character, not the tool ([2a0f496](https://github.com/oocx/tfplan2md/commit/2a0f496e57c04301997cef452f31b099293cb5b9))
* clarify that templates control their own default titles ([f444dd7](https://github.com/oocx/tfplan2md/commit/f444dd7e6498f56618e63fcef9a428843e99669c))
* document prerelease + tag-only release rationale ([674ee00](https://github.com/oocx/tfplan2md/commit/674ee006632be231fbf6df26a3c874ef66774689))
* enforce artifact ownership boundaries across agents ([89a5e8b](https://github.com/oocx/tfplan2md/commit/89a5e8b002b5331ce5b4f94dfe9979dfd15f878b))
* improve Release Manager safety and efficiency ([142d939](https://github.com/oocx/tfplan2md/commit/142d9393721fa6112c7046de06c529ce168d3434))
* mark issue [#6](https://github.com/oocx/tfplan2md/issues/6) as completed in retrospective tracker ([c4eeb1c](https://github.com/oocx/tfplan2md/commit/c4eeb1ce2d9f59b45922c1da7915aafa752873ae))
* mark issue [#7](https://github.com/oocx/tfplan2md/issues/7) as done - 12/13 completed (92%) ([4846cae](https://github.com/oocx/tfplan2md/commit/4846cae87fd62dc691b538a85bf3686e0c3f67e4))
* mark issues [#9](https://github.com/oocx/tfplan2md/issues/9) and [#10](https://github.com/oocx/tfplan2md/issues/10) as completed in retrospective tracker ([b23ce8c](https://github.com/oocx/tfplan2md/commit/b23ce8cb671dbf07a0b563463718a10aab9e22f8))
* move UAT test plan responsibility to Quality Engineer ([8900515](https://github.com/oocx/tfplan2md/commit/8900515a59bd3edff6fe6242ed5c8bfed6160d12))
* move UAT test plan to feature folder and update agent instructions ([924c26f](https://github.com/oocx/tfplan2md/commit/924c26fd10fea05159799f30dd3ef8b62f787f52))
* prevent Task Planner from starting implementation ([388ce98](https://github.com/oocx/tfplan2md/commit/388ce985ac0631e384a86bbecae1bcf05657d82f))
* remove UAT Background agent references from agents.md ([c95489e](https://github.com/oocx/tfplan2md/commit/c95489e663d92649fe391569c6c622e1ad051c80))
* require full lifecycle analysis and mandatory metrics in Retrospective ([1a44576](https://github.com/oocx/tfplan2md/commit/1a44576c837838fc4b49820c318f46382d1db6d2))
* update custom report title UAT report with results ([e3109ab](https://github.com/oocx/tfplan2md/commit/e3109abc682e7c7bbed8c432185fe5d296179cda))
* update retrospective agent performance table ([a1ac4eb](https://github.com/oocx/tfplan2md/commit/a1ac4ebceb2355c55892701c4698217130ca39a6))
* update retrospective with interactive feedback and agent improvements ([16e41b6](https://github.com/oocx/tfplan2md/commit/16e41b69050221d0ca15d8d5f220a298242cf620))
* update retrospective with more critical evaluation ([fee1860](https://github.com/oocx/tfplan2md/commit/fee186036834a64ae736fdd554e0fa7b96ac259a))
* update retrospective with user observations and new action items ([fe88c2d](https://github.com/oocx/tfplan2md/commit/fe88c2d86e67d18c7ee443547fa43069ecd69caf))
* update tasks for custom report title ([745b4b9](https://github.com/oocx/tfplan2md/commit/745b4b9e1d5ed3da92fed448e64bf7495c708b1b))
* update test plan with UAT artifact instructions ([43dcada](https://github.com/oocx/tfplan2md/commit/43dcada5d51151329f9de364d1d838b539fdb580))
* update UAT artifact instructions in QE and Tester agents ([8a2ecad](https://github.com/oocx/tfplan2md/commit/8a2ecad98fe2e462856ac740d9465b748ab598df))
* **agent:** instruct UAT Tester to run script in background and report PR links immediately ([afdaf6f](https://github.com/oocx/tfplan2md/commit/afdaf6f87b30b76a846b6101071d981f3934a205))
* **retrospective:** add chat log ([41ea358](https://github.com/oocx/tfplan2md/commit/41ea358bbe9925b07036330bd6d6200c38326d48))
* **skill:** remove agent-dependent metrics from chat analysis ([137d0c5](https://github.com/oocx/tfplan2md/commit/137d0c59cebefc2ff493e3dcc9f25687fd8b47db))
* **skill:** remove misleading workaround for agent limitation ([c304731](https://github.com/oocx/tfplan2md/commit/c304731f3df8a65f9ddbdbd31ebefcaa11f6ab7a))
* **skills:** add VS Code source-based chat export format specification ([88beaf2](https://github.com/oocx/tfplan2md/commit/88beaf29238e3853ddc4a2f1177e22886049d083))
* **skills:** update create-pr-github skill to use stdin instead of --body-file ([2046e43](https://github.com/oocx/tfplan2md/commit/2046e43ff34f33921bf671a0622cc92ba8802869))

### Breaking Changes

* **scripts:** remove --body-file and --description options, enforce stdin-only ([4338625](https://github.com/oocx/tfplan2md/commit/4338625bf16197c1e8c34b58a7b354b7c6b170b9))

<a name="0.49.0"></a>
## [0.49.0](https://github.com/oocx/tfplan2md/compare/v0.48.0...v0.49.0) (2025-12-26)

### ✨ Features

* implement backtick formatting for Azure resource ID values ([ee95011](https://github.com/oocx/tfplan2md/commit/ee9501196e00f1f8481fae5b9f229c26b9e0064f))
* implement model-driven large value detection for Azure resource IDs ([2969f9b](https://github.com/oocx/tfplan2md/commit/2969f9bd5864fb2f6ea186a5b64149d7cfcb8d5b))

### 🐛 Bug Fixes

* **ci:** ensure workflows can push tags — persist-credentials and authenticated push; disable gh pager in release step ([03a8a5c](https://github.com/oocx/tfplan2md/commit/03a8a5c8db3616357804ab0183d667d915945e31))
* **uat:** block minimal artifacts and add validation helper + tests ([50a5ae3](https://github.com/oocx/tfplan2md/commit/50a5ae31f21bd510291acf80151d651efe0605ee))
* **uat:** keep validate_artifact stdout clean ([68d0c70](https://github.com/oocx/tfplan2md/commit/68d0c706b8b111bb3b3bef67e49644986381a684))

### 📚 Documentation

* add architecture for universal Azure resource ID formatting ([e214ab3](https://github.com/oocx/tfplan2md/commit/e214ab36692d5e393478ae0c6aae4ac9b2096c63))
* add feature specification for universal Azure resource ID formatting ([8804f2e](https://github.com/oocx/tfplan2md/commit/8804f2ea8be80caedb6fa051a7c93a446aebe295))
* add tasks for universal Azure resource ID formatting ([8a0d8f0](https://github.com/oocx/tfplan2md/commit/8a0d8f0ac127e831535ddd1a96b48904ae0380a4))
* add test plan for universal Azure resource ID formatting ([1536620](https://github.com/oocx/tfplan2md/commit/15366208fc3cf40608ae2e988c420479a23288c7))
* align Azure ID formatting examples and add review report ([b48e36f](https://github.com/oocx/tfplan2md/commit/b48e36f1bc0aec67423fb805a69f7d0a0c78bdb2))
* enforce rebase-only release merges ([2295e7b](https://github.com/oocx/tfplan2md/commit/2295e7bc042015b7ad3ade8cbe2cd81fd6551857))
* mark q4 workflow roadmap complete ([91c8eb3](https://github.com/oocx/tfplan2md/commit/91c8eb3ecfe5503519483797f93039425bfe5342))
* **release:** prefer scripts/pr-github.sh for create+merge (rebase-and-merge) ([929f726](https://github.com/oocx/tfplan2md/commit/929f72690ca47bf6110d00cd2f7703cadfc6c98c))

<a name="0.48.0"></a>
## [0.48.0](https://github.com/oocx/tfplan2md/compare/v0.47.0...v0.48.0) (2025-12-26)

### ✨ Features

* **workflow:** add comprehensive-demo-standard-diff artifact for GitHub UAT ([c05f367](https://github.com/oocx/tfplan2md/commit/c05f36717e2fa97af471d74a07c1575b5184fb36))
* **workflow:** add stable scripts for demo generation and snapshot updates ([57b0cf2](https://github.com/oocx/tfplan2md/commit/57b0cf22117d65fb0bc7aa079daa6219823a98c7))
* **workflow:** update Developer agent to use stable demo and snapshot scripts ([8090d24](https://github.com/oocx/tfplan2md/commit/8090d24f1e72e4a008d68ec6faf789cb83201b5f))

### 🐛 Bug Fixes

* **workflow:** copy snapshots from bin output to source directory ([975b753](https://github.com/oocx/tfplan2md/commit/975b753b2056a4f3becd765d3b5af133d22273b3))
* **workflow:** correct CLI option to --large-value-format in demo generation script ([003b080](https://github.com/oocx/tfplan2md/commit/003b0802b362932ace6d8592a8533de7940c3a1f))

### 📚 Documentation

* update retrospective and roadmap with PR [#117](https://github.com/oocx/tfplan2md/issues/117) progress ([ecb7f96](https://github.com/oocx/tfplan2md/commit/ecb7f9621b068f6c188ba15a2b430231585e2581))

<a name="0.47.0"></a>
## [0.47.0](https://github.com/oocx/tfplan2md/compare/v0.46.0...v0.47.0) (2025-12-26)

### ✨ Features

* **uat:** enforce simulation blocking and add smart platform-specific defaults ([4780e58](https://github.com/oocx/tfplan2md/commit/4780e580d3416f9c3d1febf77da96ea3d363f93c))

<a name="0.46.0"></a>
## [0.46.0](https://github.com/oocx/tfplan2md/compare/v0.45.1...v0.46.0) (2025-12-26)

### ✨ Features

* **workflow:** add documentation alignment gate to Code Reviewer ([ac88d1a](https://github.com/oocx/tfplan2md/commit/ac88d1aaa40ab25dd7df20238fce6d807691bbf5))
* **workflow:** add Release Manager → Retrospective handoff ([6869a63](https://github.com/oocx/tfplan2md/commit/6869a63c9685f0af221d18d05d8e33eb1578854a))
* **workflow:** add role boundaries and handoff/status templates ([94e2b55](https://github.com/oocx/tfplan2md/commit/94e2b55f12209f6e3484c7412dea5ac50b40bbc2))
* **workflow:** wire report style guide into agents ([8f50bc7](https://github.com/oocx/tfplan2md/commit/8f50bc7ee4a94e8a9b3f453ca2279b140da7f71d))

### 🐛 Bug Fixes

* **workflow:** add quotes to Retrospective agent handoff ([2e46ee0](https://github.com/oocx/tfplan2md/commit/2e46ee08d5b810879bf2a9731769db1048c82c4a))

### 📚 Documentation

* update retrospective tracking with completed PRs [#111](https://github.com/oocx/tfplan2md/issues/111)-114 ([d9e6c7c](https://github.com/oocx/tfplan2md/commit/d9e6c7c38c4707ab13ad8e1b6f090c69941e905a))
* **agents:** add GitHub tools to Retrospective ([0647648](https://github.com/oocx/tfplan2md/commit/0647648a29617579d1aeef02b815ae17dfdb9511))
* **agents:** prefer GitHub tools over gh ([efb0e07](https://github.com/oocx/tfplan2md/commit/efb0e0740b937bfe99cb27990453851301e06262))
* **workflow:** prefer GitHub tools for PR inspection ([ba1f88f](https://github.com/oocx/tfplan2md/commit/ba1f88fe6e4e056195d7a2830313ac58c01e9d9c))
* **workflow:** prefer GitHub tools over gh ([88f697f](https://github.com/oocx/tfplan2md/commit/88f697f94c4f3360bea5387e47b6ad7b6c11675b))

<a name="0.45.1"></a>
## [0.45.1](https://github.com/oocx/tfplan2md/compare/v0.45.0...v0.45.1) (2025-12-26)

### 🐛 Bug Fixes

* **workflow:** require explicit PR title and body ([1621124](https://github.com/oocx/tfplan2md/commit/1621124ee8f59793e46a5680534451d2d6866d9d))

### 📚 Documentation

* **workflow:** add PR comment retrieval guidance ([133d840](https://github.com/oocx/tfplan2md/commit/133d840b5351ce37b24d4e9a198c73b0d396ddb2))

<a name="0.45.0"></a>
## [0.45.0](https://github.com/oocx/tfplan2md/compare/v0.44.2...v0.45.0) (2025-12-26)

### ✨ Features

* **workflow:** add view GitHub PR skill ([a79d500](https://github.com/oocx/tfplan2md/commit/a79d50081d1dabd7c12ad6a559543edc4d188b74))

<a name="0.44.2"></a>
## [0.44.2](https://github.com/oocx/tfplan2md/compare/v0.44.1...v0.44.2) (2025-12-26)

### 🐛 Bug Fixes

* **workflow:** fix Husky commit-msg args variable ([7156326](https://github.com/oocx/tfplan2md/commit/71563269d7f43e878ac7d1d998317b1d77022f5c))

<a name="0.44.1"></a>
## [0.44.1](https://github.com/oocx/tfplan2md/compare/v0.44.0...v0.44.1) (2025-12-25)

### 🐛 Bug Fixes

* **workflow:** require PR description previews ([05af78e](https://github.com/oocx/tfplan2md/commit/05af78eb95c5cd0129af5f0f450e48a6f4986ecb))
* **workflow:** standardize response style and preview guidance ([4f5214f](https://github.com/oocx/tfplan2md/commit/4f5214f66acb46fb105d34f373ec28b32033790f))

### 📚 Documentation

* **gh:** prefer explicit PR body ([8f07fb2](https://github.com/oocx/tfplan2md/commit/8f07fb2a58e1cf2dd703dd4cf1b4faa2fff425bd))
* **release:** prefer explicit PR body ([6acee4a](https://github.com/oocx/tfplan2md/commit/6acee4aa76f5e61b7efc54e54e88eb9a36fa966b))
* **skills:** make explicit PR body default ([e745a9b](https://github.com/oocx/tfplan2md/commit/e745a9bb458cc2b485e2eba463fda17df9c247c9))
* **workflow:** define when to use todo lists ([cb5ff60](https://github.com/oocx/tfplan2md/commit/cb5ff60120a36414d3a6606eedb463db117d1ee9))
* **workflow:** prefer explicit PR body ([c0a4ee7](https://github.com/oocx/tfplan2md/commit/c0a4ee705aad328ae34739724ea424bc4cb55080))

<a name="0.44.0"></a>
## [0.44.0](https://github.com/oocx/tfplan2md/compare/v0.43.0...v0.44.0) (2025-12-25)

### ✨ Features

* **workflow:** default UAT artifacts per platform ([94e1418](https://github.com/oocx/tfplan2md/commit/94e1418b2a4cb4ed2074d111c43bc78c6fefda72))

### 📚 Documentation

* **testing:** update UAT instructions to use scripts ([acc9a06](https://github.com/oocx/tfplan2md/commit/acc9a0688588451c1a00a616789005a055dfab71))
* **workflow:** link latest retrospective follow-ups ([5903082](https://github.com/oocx/tfplan2md/commit/590308242087c9ca3a4df8dc99edb29433c8f74b))
* **workflow:** prefer PR wrapper scripts in gh instructions ([1362f99](https://github.com/oocx/tfplan2md/commit/1362f9941a422db426994c6265309cb3c01c0fa3))
* **workflow:** require PR preview in agent prompts ([82d84ce](https://github.com/oocx/tfplan2md/commit/82d84ce7fa5d77c6457cc39a69574450052e03a3))

<a name="0.43.0"></a>
## [0.43.0](https://github.com/oocx/tfplan2md/compare/v0.42.0...v0.43.0) (2025-12-25)

### ✨ Features

* **workflow:** add PR preview commands ([44349ba](https://github.com/oocx/tfplan2md/commit/44349ba81d01d85b5e1f20454574f1af4d80546a))

<a name="0.42.0"></a>
## [0.42.0](https://github.com/oocx/tfplan2md/compare/v0.41.1...v0.42.0) (2025-12-25)

### ✨ Features

* **workflow:** add uat-run wrapper and artifact guardrails ([e7dca79](https://github.com/oocx/tfplan2md/commit/e7dca79be2b55565f9a5372cecc8299c28a8829f))

### 📚 Documentation

* **workflow:** show PR title and summary before create ([d3c48f8](https://github.com/oocx/tfplan2md/commit/d3c48f80aa6adb3833a0e656ff1260cce3a6786e))
* **workflow:** track retrospective follow-up progress ([e31c619](https://github.com/oocx/tfplan2md/commit/e31c6190682a6c688e77cb01aa3c85128f61294f))

<a name="0.41.1"></a>
## [0.41.1](https://github.com/oocx/tfplan2md/compare/v0.41.0...v0.41.1) (2025-12-25)

### 🐛 Bug Fixes

* **workflow:** harden GitHub UAT polling ([ed28a6d](https://github.com/oocx/tfplan2md/commit/ed28a6de21fc0555515a6175a9e51934d85c7fbc))

<a name="0.41.0"></a>
## [0.41.0](https://github.com/oocx/tfplan2md/compare/v0.40.0...v0.41.0) (2025-12-25)

### ✨ Features

* **workflow:** add UAT PR watch skills ([9cb7b9c](https://github.com/oocx/tfplan2md/commit/9cb7b9cfd9fc0d651faf5fa2461dc65e005df78b))

<a name="0.40.0"></a>
## [0.40.0](https://github.com/oocx/tfplan2md/compare/v0.39.0...v0.40.0) (2025-12-25)

### ✨ Features

* **workflow:** add azdo PR abandon wrapper ([ad05196](https://github.com/oocx/tfplan2md/commit/ad051963730d30ff6a9099dce214a99446c6e251))

<a name="0.39.0"></a>
## [0.39.0](https://github.com/oocx/tfplan2md/compare/v0.38.0...v0.39.0) (2025-12-25)

### ✨ Features

* **workflow:** add one-command Azure DevOps PR script ([30d3864](https://github.com/oocx/tfplan2md/commit/30d38649a24a4ce2be54dcf126be106aca11ef6f))

<a name="0.38.0"></a>
## [0.38.0](https://github.com/oocx/tfplan2md/compare/v0.37.0...v0.38.0) (2025-12-25)

### ✨ Features

* **workflow:** add one-command GitHub PR script ([8017fef](https://github.com/oocx/tfplan2md/commit/8017fef6adbfdf82af1a8665f9e578510957eade))

### 🐛 Bug Fixes

* **workflow:** fix pr-github wrapper arg parsing ([878b6fe](https://github.com/oocx/tfplan2md/commit/878b6fec5ccc7b37ed64e1b0d2e5a002417df9c1))
* **workflow:** fix pr-github wrapper option parsing ([07bebbd](https://github.com/oocx/tfplan2md/commit/07bebbdaad7a1652831f50aba6c5e3ba8826f768))

<a name="0.37.0"></a>
## [0.37.0](https://github.com/oocx/tfplan2md/compare/v0.36.0...v0.37.0) (2025-12-25)

### ✨ Features

* **workflow:** add PR creation skills ([321ffab](https://github.com/oocx/tfplan2md/commit/321ffab4e07027e50c2da0012dfaa5675deec83d))

### 📚 Documentation

* **workflow:** add skill approval-minimization guidance ([b30cf52](https://github.com/oocx/tfplan2md/commit/b30cf525720b5d2490cc7e2ba2664a5c0b99818b))

<a name="0.36.0"></a>
## [0.36.0](https://github.com/oocx/tfplan2md/compare/v0.35.0...v0.36.0) (2025-12-25)

### ✨ Features

* **workflow:** add agent skills and UAT skills ([693e51f](https://github.com/oocx/tfplan2md/commit/693e51f275c697263f991c9d1d7ebd591ce42adc))

### 📚 Documentation

* **retrospective:** lead time + metrics guidance ([d5406e3](https://github.com/oocx/tfplan2md/commit/d5406e39db76d673c64c5edab2b3de269da4e8bc))

<a name="0.35.0"></a>
## [0.35.0](https://github.com/oocx/tfplan2md/compare/v0.34.0...v0.35.0) (2025-12-25)

### ✨ Features

* align consistent value formatting ([b4892aa](https://github.com/oocx/tfplan2md/commit/b4892aa42cf98e88034606633ef0e37a85e714c1))

### 🐛 Bug Fixes

* add +/- markers to inline diffs for readability ([e86b7c5](https://github.com/oocx/tfplan2md/commit/e86b7c54e8a8377d40191563303949ca7a8b4793))

### 📚 Documentation

* add architecture for consistent-value-formatting ([adb8fdb](https://github.com/oocx/tfplan2md/commit/adb8fdbc4b8e47f5cd452b3eccc351219052de7b))
* add feature specification for consistent value formatting ([f00a05a](https://github.com/oocx/tfplan2md/commit/f00a05a3e170d82138a06eb2b50043b84b3c207a))
* add tasks for consistent-value-formatting ([aee0f34](https://github.com/oocx/tfplan2md/commit/aee0f348b3cb7c2fca77354a96065680cc16675e))
* add test plan for consistent-value-formatting ([23681c5](https://github.com/oocx/tfplan2md/commit/23681c5d13e203ef1885b697d718099fb89305b1))
* mark consistent value formatting review approved ([38c55f0](https://github.com/oocx/tfplan2md/commit/38c55f09d6d40be186840590fab63e32ee882228))
* refine architecture to specify table-compatible diff formatting ([af43ae7](https://github.com/oocx/tfplan2md/commit/af43ae7130419d73b5abcded888f71bfcd188ebe))
* regenerate comprehensive demo ([88db09e](https://github.com/oocx/tfplan2md/commit/88db09ed09fc4567b4bbc37f021c7965968a1fdf))
* update architecture to use registered helper configuration ([e50c1fd](https://github.com/oocx/tfplan2md/commit/e50c1fd4014d71a0a13c87c1e11c215aa39222f4))
* update examples with proper rendering and refined role assignment formatting ([a0ae233](https://github.com/oocx/tfplan2md/commit/a0ae23304a3c67aa35d192b8ac86c37a060f1416))
* update tasks for registered helper configuration ([24ee0c3](https://github.com/oocx/tfplan2md/commit/24ee0c3f0eae555bb525a5eea1ee479871e77c73))
* update tasks with actionable user stories ([ce79cdc](https://github.com/oocx/tfplan2md/commit/ce79cdc65880d8efd8bed9322ee4fe92a7d6f0ce))
* **uat:** capture consistent value formatting findings ([30cb80a](https://github.com/oocx/tfplan2md/commit/30cb80acb7d146550fbe766509421d9487d99d04))

<a name="0.34.0"></a>
## [0.34.0](https://github.com/oocx/tfplan2md/compare/v0.33.0...v0.34.0) (2025-12-25)

### ✨ Features

* add UAT Tester agent and refactor Code Reviewer ([bd4318e](https://github.com/oocx/tfplan2md/commit/bd4318efe029facdf3d4f940292f479ee87cc602))
* improve UAT workflow with helper scripts and autonomous polling ([9b0b142](https://github.com/oocx/tfplan2md/commit/9b0b1429e1e5ba16deeffe2aa23342033d781d05))
* replace acceptance notebooks with UAT PRs ([3ae417b](https://github.com/oocx/tfplan2md/commit/3ae417b8bdb3fd950a23f4bdc6b82d9d887f51e7))

### 🐛 Bug Fixes

* address UAT simulation findings ([a56a1ab](https://github.com/oocx/tfplan2md/commit/a56a1abc511071fedf817c59470b42edc4a3e598))
* improve UAT workflow reliability ([41bcb39](https://github.com/oocx/tfplan2md/commit/41bcb39b0ee74f644b08636c4130c83092669a3f))
* reduce UAT polling to 15s and restore branch after cleanup ([b10ec24](https://github.com/oocx/tfplan2md/commit/b10ec2423870620993d055a582e9ea1ba1cca7fb))
* strengthen UAT Tester autonomous execution instructions ([7568c27](https://github.com/oocx/tfplan2md/commit/7568c27459127cd57893655491c5239f955ce9be))
* tighten AzDO UAT approval and simplify simulation ([688c126](https://github.com/oocx/tfplan2md/commit/688c126a30d80220a5fdfcf19ac596e167d4958e))
* update UAT Tester agent with GPT-5.2, correct tool names, and simulation instructions ([20f39fb](https://github.com/oocx/tfplan2md/commit/20f39fbeb97f2781bc9886f0e14fd1b06623706a))

### ♻️ Refactoring

* use GPT-5 mini for UAT Tester agent ([57103c5](https://github.com/oocx/tfplan2md/commit/57103c5c5ab55e9eae2f7637c9bcad0777582193))

### 📚 Documentation

* finalize PR-based UAT workflow ([e3aaa46](https://github.com/oocx/tfplan2md/commit/e3aaa465cdb20deb2b116c245fd4e03f87bcd341))
* update terminal command guidelines ([69b52d9](https://github.com/oocx/tfplan2md/commit/69b52d9da308e122406cfc597b040454d996fb17))

<a name="0.33.0"></a>
## [0.33.0](https://github.com/oocx/tfplan2md/compare/v0.32.0...v0.33.0) (2025-12-24)

### ✨ Features

* add retrospective agent and update workflow documentation ([7ca2847](https://github.com/oocx/tfplan2md/commit/7ca2847eaf594335a1a18a736ce557ff9a65f0c8))

<a name="0.32.0"></a>
## [0.32.0](https://github.com/oocx/tfplan2md/compare/v0.31.1...v0.32.0) (2025-12-24)

### ✨ Features

* add inline diff formatting with char highlighting ([7a3e34d](https://github.com/oocx/tfplan2md/commit/7a3e34d97a08e028f10b08013e0522e12439a52f))
* add large value detection helper ([696d7f5](https://github.com/oocx/tfplan2md/commit/696d7f5e3e77d0f5e81f204dd89f5e6afd5d0a2b))
* add large-value-format cli option ([18ce46d](https://github.com/oocx/tfplan2md/commit/18ce46d75ddacc8121273e6384a24d0cf34e432f))
* add standard diff formatting for large values ([0c29164](https://github.com/oocx/tfplan2md/commit/0c29164757865ebba1dceaebf64afb5443361bbc))
* complete Task 7 - template integration ([05fbb86](https://github.com/oocx/tfplan2md/commit/05fbb86772cd5fe440db9530d079db5ec243d10f))
* implement large-attribute-value-display feature ([424b4c6](https://github.com/oocx/tfplan2md/commit/424b4c6ab70565ec1c740a74447b491710435d00))

### 🐛 Bug Fixes

* handle empty before blocks and remove stray template separator ([e3fa75a](https://github.com/oocx/tfplan2md/commit/e3fa75adc454c1d605b622df64ab285b74f85b89))

### 📚 Documentation

* add architecture for large-attribute-value-display ([2709dca](https://github.com/oocx/tfplan2md/commit/2709dca05c6fdae3bdab29e5f1aa1b7920da7e50))
* add feature specification for large attribute value display ([5a4cd2c](https://github.com/oocx/tfplan2md/commit/5a4cd2cf60e1cd8201f60ce876c4ce016ca249bd))
* add tasks for large-attribute-value-display ([74dc831](https://github.com/oocx/tfplan2md/commit/74dc8312b26a4a28e4c41bc60723577577d6fc0a))
* add test plan for large-attribute-value-display ([dd768f5](https://github.com/oocx/tfplan2md/commit/dd768f504453ee4ed251033d7178b5c3e7f77fac))
* add text color for dark mode compatibility in example outputs ([43777e6](https://github.com/oocx/tfplan2md/commit/43777e67e4c85c687c88a9432dcc80890c95ddf4))
* complete Task 8 - documentation updates ([993ff09](https://github.com/oocx/tfplan2md/commit/993ff09a5d313e2a98d585054af446c8822c9e52))
* finalize documentation and code review for large value display ([2525400](https://github.com/oocx/tfplan2md/commit/252540081f29619da3c73606d31ddba106420d50))
* mark inline diff tasks as done ([3a64660](https://github.com/oocx/tfplan2md/commit/3a646602916cef8cc200b76ff0fbb5c7d4072d56))
* mark task1 large-value-format cli as done ([e2a8358](https://github.com/oocx/tfplan2md/commit/e2a8358c560ce8b9f543f0ae7728487eb60b4202))
* mark task2 large value detection as done ([8efaa0f](https://github.com/oocx/tfplan2md/commit/8efaa0ffa5d7b192bfd5db5b106047ee96d8847b))
* mark task3 standard diff as done ([3e470cb](https://github.com/oocx/tfplan2md/commit/3e470cbf4e95f022e402a7733eaf28b70cf1bdd5))
* update large value feature docs ([d46a9b6](https://github.com/oocx/tfplan2md/commit/d46a9b64bd1774a7af52493c1d14fb8db78541f0))

<a name="0.31.1"></a>
## [0.31.1](https://github.com/oocx/tfplan2md/compare/v0.31.0...v0.31.1) (2025-12-24)

### 🐛 Bug Fixes

* update agent models — Documentation Author, Quality Engineer, Support Engineer, Code Reviewer ([3e7a552](https://github.com/oocx/tfplan2md/commit/3e7a552f31fc29bb4d30e9f2a1e98ecadfb64051))

### ♻️ Refactoring

* rename agents for clarity and consistency ([6b74f79](https://github.com/oocx/tfplan2md/commit/6b74f7917f0c07e17667c2e3cdc0fa44208bc2fa))

### 📚 Documentation

* add independent LiveBench mappings for AI model recommendations ([005380d](https://github.com/oocx/tfplan2md/commit/005380dbf01c21248938cfd1614286bb37fe09df))
* update AI model reference and agent configurations ([8dbabf0](https://github.com/oocx/tfplan2md/commit/8dbabf0b2091b53de008d643ddf02d263f9b349a))
* update remaining references to use new agent names ([d6baf4a](https://github.com/oocx/tfplan2md/commit/d6baf4a39c2fe3717b21b5f0940472a86068549a))

<a name="0.31.0"></a>
## [0.31.0](https://github.com/oocx/tfplan2md/compare/v0.30.0...v0.31.0) (2025-12-23)

### ✨ Features

* add user acceptance testing with interactive notebooks ([42b0fa9](https://github.com/oocx/tfplan2md/commit/42b0fa9a7c703a901cf847af318518c6e3037495))

<a name="0.30.0"></a>
## [0.30.0](https://github.com/oocx/tfplan2md/compare/v0.29.1...v0.30.0) (2025-12-23)

### ✨ Features

* add specialized template for Azure Network Security Group rules ([dffb253](https://github.com/oocx/tfplan2md/commit/dffb2538e42a18e98255e4afcbb8204509b71023))

### 🐛 Bug Fixes

* avoid markdownlint errors for NSG empty descriptions ([5beeaff](https://github.com/oocx/tfplan2md/commit/5beeaffde75a0c6f63f537483ddfd67b6ea59496))

### 📚 Documentation

* add architecture for NSG security rule template ([eb58b20](https://github.com/oocx/tfplan2md/commit/eb58b20c482a7139a9e0bd18181761c0fed521df))
* add feature specification for NSG security rule template ([2c2082c](https://github.com/oocx/tfplan2md/commit/2c2082cc69b9ecd8e63983b3984e5ab9af2a0568))
* add tasks for NSG security rule template ([4b043e0](https://github.com/oocx/tfplan2md/commit/4b043e0fbc3d1b1e88b87e62b1bc6855ae8cff77))
* add test plan for NSG security rule template ([e30b553](https://github.com/oocx/tfplan2md/commit/e30b553084343e5631602778cfe6a94c7939fc57))

<a name="0.29.1"></a>
## [0.29.1](https://github.com/oocx/tfplan2md/compare/v0.29.0...v0.29.1) (2025-12-23)

### 🐛 Bug Fixes

* exclude no-op resources from summary table Total count ([0fc5bdf](https://github.com/oocx/tfplan2md/commit/0fc5bdf3aac323f0261853d9aa6fd799296852af))

### 📚 Documentation

* add issue analysis for summary table totals mismatch ([61b8252](https://github.com/oocx/tfplan2md/commit/61b8252d34ab8e768a836bd89ada57943f70b546))

<a name="0.29.0"></a>
## [0.29.0](https://github.com/oocx/tfplan2md/compare/v0.28.0...v0.29.0) (2025-12-22)

### ✨ Features

* implement replacement reasons and resource summaries ([391b2be](https://github.com/oocx/tfplan2md/commit/391b2be1d09f8022728470c74183160452d9fc17))

### 📚 Documentation

* add architecture for replacement reasons and summaries ([2699a8f](https://github.com/oocx/tfplan2md/commit/2699a8f85dd6800ecb946ad0bc8e15658602caa2))
* add tasks for replacement reasons and summaries ([751879c](https://github.com/oocx/tfplan2md/commit/751879c9cd4b24ee0f0d356ae78eaeb38eba3825))
* add test plan for replacement reasons and summaries ([b04324f](https://github.com/oocx/tfplan2md/commit/b04324fb4a6e2c1e022519da9c07e9fb53f5c0fe))

<a name="0.28.0"></a>
## [0.28.0](https://github.com/oocx/tfplan2md/compare/v0.27.1...v0.28.0) (2025-12-22)

### ✨ Features

* add --show-unchanged-values CLI option to filter attribute tables ([1f9984b](https://github.com/oocx/tfplan2md/commit/1f9984b6ad6470019ffade96b3783a8ebaa37bd2))
* add diagnostic tools to agent definitions ([8020673](https://github.com/oocx/tfplan2md/commit/8020673f63704590f2501ade160c400d5343b37c))

### 🐛 Bug Fixes

* correct MCP server name from microsoft-learn/* to microsoftdocs/mcp/* ([3b6fbcd](https://github.com/oocx/tfplan2md/commit/3b6fbcd3658d73f8bd9e45f213ac04558268101f))
* strengthen agent boundaries and tool assignments ([c7cc9a0](https://github.com/oocx/tfplan2md/commit/c7cc9a0cf12dcadc1f63eecc7514c2ec43973755))
* **ci:** add .github/workflows/** to paths-ignore to skip CI for workflow-only changes ([ef186f0](https://github.com/oocx/tfplan2md/commit/ef186f031d5d427e392a6c4e30553eee905e9534))
* **ci:** add concurrency control and workflows permission to CI pipeline ([cb9816a](https://github.com/oocx/tfplan2md/commit/cb9816a503d534b81da466736b90390938e05975))
* **ci:** remove invalid workflows permission ([bd96929](https://github.com/oocx/tfplan2md/commit/bd96929951c0968d7e083913c3213fb8fc56abc8))

### 📚 Documentation

* add architecture for unchanged values CLI option ([df95a47](https://github.com/oocx/tfplan2md/commit/df95a47c0fff869019671ce270f4e215a9038905))
* add feature specification for unchanged values CLI option ([ed47492](https://github.com/oocx/tfplan2md/commit/ed47492fb6cc6da02fcf3685cbcdba6c123c5fae))
* add tasks for unchanged values CLI option ([b151f57](https://github.com/oocx/tfplan2md/commit/b151f570d5f2d854afa785e9682212288da22965))
* add test plan for unchanged values CLI option ([9697f51](https://github.com/oocx/tfplan2md/commit/9697f51ade134e3efc85bc470fd3f5b13c078186))
* update AI model reference with GPT-5.2 benchmark data ([294d18e](https://github.com/oocx/tfplan2md/commit/294d18ea0127c8eac2a084dd95fa9454bed6b1a9))

<a name="0.27.1"></a>
## [0.27.1](https://github.com/oocx/tfplan2md/compare/v0.27.0...v0.27.1) (2025-12-22)

### 🐛 Bug Fixes

* render line breaks in tables correctly by escaping values internally in format_diff ([4849a2e](https://github.com/oocx/tfplan2md/commit/4849a2eb012856c16371d9e8679ebeb6be3e4ddc))

### 📚 Documentation

* add issue analysis for literal br tags in tables ([3636750](https://github.com/oocx/tfplan2md/commit/363675099f22ae4f68b994968a53499bb8ec4645))

<a name="0.27.0"></a>
## [0.27.0](https://github.com/oocx/tfplan2md/compare/v0.26.0...v0.27.0) (2025-12-21)

### ✨ Features

* implement selective markdown escaping and comprehensive quality validation ([65fe49f](https://github.com/oocx/tfplan2md/commit/65fe49f24f3f42275315c7814cf035a4d6a34505))

### 🐛 Bug Fixes

* resolve markdown rendering issues and enhance test coverage ([4bc947a](https://github.com/oocx/tfplan2md/commit/4bc947aa14f51b9400ebcc40b3ab44504a90cc58))

### 📚 Documentation

* add issue analysis for markdown rendering errors in v0.26.0 ([6174b65](https://github.com/oocx/tfplan2md/commit/6174b65ac3d507418072372bece59fde7674c254))

<a name="0.26.0"></a>
## [0.26.0](https://github.com/oocx/tfplan2md/compare/v0.25.0...v0.26.0) (2025-12-21)

### ✨ Features

* implement markdown quality validation and linting ([7cc7632](https://github.com/oocx/tfplan2md/commit/7cc7632d2f7e3b40385db6bb88e32c8c83035e7d))

### 📚 Documentation

* add architecture for markdown quality validation ([b9857ab](https://github.com/oocx/tfplan2md/commit/b9857ab7e3b3bbb310d5e5a8f648b1b48781db9e))
* add feature specification for markdown quality validation ([ff3b823](https://github.com/oocx/tfplan2md/commit/ff3b823179d9bf3b58e045af7834e6b714167d8e))
* add tasks for markdown quality validation ([f92046b](https://github.com/oocx/tfplan2md/commit/f92046bbf33a9047f400a8e4f03f5b153f735c8d))
* add test plan for markdown quality validation ([9b4b04b](https://github.com/oocx/tfplan2md/commit/9b4b04b6610da178ee20fa12615ba81dc70b3b39))

<a name="0.25.0"></a>
## [0.25.0](https://github.com/oocx/tfplan2md/compare/v0.24.0...v0.25.0) (2025-12-21)

### ✨ Features

* **azure:** implement table format for role assignments ([7535945](https://github.com/oocx/tfplan2md/commit/75359455c0d19d43e449e003e674f0a1f89c9923))

### 📚 Documentation

* add architecture and test plan for role assignment table format ([dd2804a](https://github.com/oocx/tfplan2md/commit/dd2804a23d403ecdcd43efdf86bfd621e278eb8a))
* add feature specification for role assignment table format ([ba7dfb4](https://github.com/oocx/tfplan2md/commit/ba7dfb49e74e42c2af061ca43322c9eb41e75e0a))
* add tasks for role assignment table format ([6e513bf](https://github.com/oocx/tfplan2md/commit/6e513bfbf6ddd7d4500e180418461d454f442605))

<a name="0.24.0"></a>
## [0.24.0](https://github.com/oocx/tfplan2md/compare/v0.23.0...v0.24.0) (2025-12-20)

### ✨ Features

* add Scriban reference and comprehensive demo requirements to agents ([9d6fae6](https://github.com/oocx/tfplan2md/commit/9d6fae6e7747aa610e13d2f4b79afea2dbdfea9a))

<a name="0.23.0"></a>
## [0.23.0](https://github.com/oocx/tfplan2md/compare/v0.22.0...v0.23.0) (2025-12-20)

### ✨ Features

* add comprehensive demo and normalize markdown heading spacing ([fa03bb2](https://github.com/oocx/tfplan2md/commit/fa03bb28e7f4195e75c929e08cca03a94be1a79a))

### 📚 Documentation

* add specification and architecture for comprehensive-demo ([0bbb619](https://github.com/oocx/tfplan2md/commit/0bbb61985be6ff0d4b4e1f72071620f3b66e048e))
* add tasks for comprehensive-demo ([a318082](https://github.com/oocx/tfplan2md/commit/a31808202746f43a2a56d0d2bd2ed9c47ea75bfb))
* add test plan for comprehensive-demo ([56a2ed0](https://github.com/oocx/tfplan2md/commit/56a2ed03706f6d86d7b5e18ce7b9b40d784476f9))

<a name="0.22.0"></a>
## [0.22.0](https://github.com/oocx/tfplan2md/compare/v0.21.0...v0.22.0) (2025-12-20)

### ✨ Features

* implement built-in summary template and plan timestamp support ([a41965f](https://github.com/oocx/tfplan2md/commit/a41965f04a453ef9631b695de5f19c19c52e2f33))

### 📚 Documentation

* add architecture for built-in templates ([7b7c812](https://github.com/oocx/tfplan2md/commit/7b7c812af2feb7d29f83e7c748bd995c1e48c9f1))
* add feature specification for built-in templates ([bc8af99](https://github.com/oocx/tfplan2md/commit/bc8af995ea6b40ca625012d6ace38582c4f1e076))
* add tasks for built-in templates ([c21d7b8](https://github.com/oocx/tfplan2md/commit/c21d7b8fd4e50df1abc017d350149072b5d4bdb4))
* add test plan for built-in templates ([d40c327](https://github.com/oocx/tfplan2md/commit/d40c327593cd0e857c482a634c801fb38f95402a))

<a name="0.21.0"></a>
## [0.21.0](https://github.com/oocx/tfplan2md/compare/v0.20.0...v0.21.0) (2025-12-20)

### ✨ Features

* add MCP server tools to agents based on role requirements ([555007a](https://github.com/oocx/tfplan2md/commit/555007ab976a2cd79d48a9ef1f4f8bf46bbab53b))

### 🐛 Bug Fixes

* add runCommands tool to Requirements Engineer agent ([02abe9a](https://github.com/oocx/tfplan2md/commit/02abe9a74bef3e1c3dfdc8ff99378a249021120a))
* update agent file links to use correct relative paths ([c3b26d2](https://github.com/oocx/tfplan2md/commit/c3b26d2a4b68a62c837aa192ee696826f1e19cc6))

<a name="0.20.0"></a>
## [0.20.0](https://github.com/oocx/tfplan2md/compare/v0.19.0...v0.20.0) (2025-12-20)

### ✨ Features

* enhanced Azure role assignment display with comprehensive role mapping and scope parsing ([72458bb](https://github.com/oocx/tfplan2md/commit/72458bb4c77284073d43ea24e45385798160e46f))

### 📚 Documentation

* add architecture for role-assignment-readable-display ([eb2dd44](https://github.com/oocx/tfplan2md/commit/eb2dd445ad42088759fc57db6fc933ee98ce44da))
* add feature specification for role assignment readable display ([825a1e5](https://github.com/oocx/tfplan2md/commit/825a1e5cf99a2665cd93c613e7ffd660d2d3ff87))
* add tasks for role-assignment-readable-display ([31c77d5](https://github.com/oocx/tfplan2md/commit/31c77d50265c513a3cd90a3df7d7040951e688b6))
* add test plan for role-assignment-readable-display ([23b7573](https://github.com/oocx/tfplan2md/commit/23b7573a8d6f417879c11322647af3304ce65f68))

<a name="0.19.0"></a>
## [0.19.0](https://github.com/oocx/tfplan2md/compare/v0.18.0...v0.19.0) (2025-12-20)

### ✨ Features

* make Architect defer decisions to maintainer when multiple options exist ([004e685](https://github.com/oocx/tfplan2md/commit/004e6859ae947e747c8b834370ef0c3f98c2c56f))

<a name="0.18.0"></a>
## [0.18.0](https://github.com/oocx/tfplan2md/compare/v0.17.0...v0.18.0) (2025-12-20)

### ✨ Features

* make Release Manager agent more autonomous ([3c73a06](https://github.com/oocx/tfplan2md/commit/3c73a06ef59f668e031f2433868910d2e9eb5ce8))

<a name="0.17.0"></a>
## [0.17.0](https://github.com/oocx/tfplan2md/compare/v0.16.5...v0.17.0) (2025-12-20)

### ✨ Features

* add resource type breakdown to summary table ([16cd606](https://github.com/oocx/tfplan2md/commit/16cd606fd775d5210bb6decaced8ff2d01d19cd8))

### 📚 Documentation

* add architecture for summary resource type breakdown ([dd8457d](https://github.com/oocx/tfplan2md/commit/dd8457d31b2846fdcc7a3451b2377670d182387a))
* add feature specification for summary resource type breakdown ([5883ac2](https://github.com/oocx/tfplan2md/commit/5883ac2e887213ae8ad19b33f785e95f9feb1f39))
* add procedure for fixing agents during feature development ([188163d](https://github.com/oocx/tfplan2md/commit/188163d0521a4f1ea4cf14beee7e6f9f12d965c3))
* add tasks for summary resource type breakdown ([8ad1733](https://github.com/oocx/tfplan2md/commit/8ad17332c25d2beb082505294d23c484e73b2e31))
* add test plan for summary resource type breakdown ([1b322de](https://github.com/oocx/tfplan2md/commit/1b322deed9e770962704bde56aaa3fa8c9d7903a))

<a name="0.16.5"></a>
## [0.16.5](https://github.com/oocx/tfplan2md/compare/v0.16.4...v0.16.5) (2025-12-20)

### 🐛 Bug Fixes

* require Developer agent to handle skipped tests before marking work complete ([deee1b4](https://github.com/oocx/tfplan2md/commit/deee1b4036d5916fc26708ccd5234dfa92708e1d))

<a name="0.16.4"></a>
## [0.16.4](https://github.com/oocx/tfplan2md/compare/v0.16.3...v0.16.4) (2025-12-20)

### 🐛 Bug Fixes

* add runInTerminal tool to Requirements Engineer and strengthen branch creation instructions ([2b9b2ec](https://github.com/oocx/tfplan2md/commit/2b9b2ec2c95c7bb234596ae50c63b7dbf30c4c10))

<a name="0.16.3"></a>
## [0.16.3](https://github.com/oocx/tfplan2md/compare/v0.16.2...v0.16.3) (2025-12-19)

### 🐛 Bug Fixes

* improve agent reliability and workflow consistency ([3726b10](https://github.com/oocx/tfplan2md/commit/3726b103448707f72288ba604c3500790507108e))

<a name="0.16.2"></a>
## [0.16.2](https://github.com/oocx/tfplan2md/compare/v0.16.1...v0.16.2) (2025-12-19)

### 🐛 Bug Fixes

* make changelog extraction POSIX AWK compatible ([b43573d](https://github.com/oocx/tfplan2md/commit/b43573dc262ddc18df34c6d817f2ecb95c2f2ef3))

<a name="0.16.1"></a>
## [0.16.1](https://github.com/oocx/tfplan2md/compare/v0.16.0...v0.16.1) (2025-12-19)

### 🐛 Bug Fixes

* ensure gh cli calls are non-blocking in support engineer agent ([e561653](https://github.com/oocx/tfplan2md/commit/e561653750c24cc4fabae12d9af64100b81d0db0))

<a name="0.16.0"></a>
## [0.16.0](https://github.com/oocx/tfplan2md/compare/v0.15.1...v0.16.0) (2025-12-19)

### ✨ Features

* add Support Engineer agent for bug fixes and incidents ([b079c79](https://github.com/oocx/tfplan2md/commit/b079c79ec10ea41d6ec8ee55eab3c6596996735d))

<a name="0.15.1"></a>
## [0.15.1](https://github.com/oocx/tfplan2md/compare/v0.15.0...v0.15.1) (2025-12-19)

### 🐛 Bug Fixes

* improve agent workflow consistency and reliability ([f7e5ea9](https://github.com/oocx/tfplan2md/commit/f7e5ea9d052df3af81647ddebd49c4df34380351))

### 📚 Documentation

* improve agent workflow consistency and visual appearance ([e88c5d8](https://github.com/oocx/tfplan2md/commit/e88c5d8bf81f4f2c7d57862d8581c81d665c98f7))

<a name="0.15.0"></a>
## [0.15.0](https://github.com/oocx/tfplan2md/compare/v0.14.0...v0.15.0) (2025-12-19)

### ✨ Features

* enhance Architect agent documentation with new tools and clarify constraints ([7a0842c](https://github.com/oocx/tfplan2md/commit/7a0842c8f79982b3604a908c5c12fdef78aa6dbb))
* implement cumulative release notes for docker deployments ([d3d89a8](https://github.com/oocx/tfplan2md/commit/d3d89a89ac3386b9953a20e36a68f91165947092))
* show before and after values for modified firewall rules ([b3b5bbd](https://github.com/oocx/tfplan2md/commit/b3b5bbdc4f9db9dc994e43d6e1931d81be6dcc4d))

### 🐛 Bug Fixes

* require Requirements Engineer to use local git commands for branch creation ([27a3334](https://github.com/oocx/tfplan2md/commit/27a3334d6d38b85da30d9083fd1b898d42ace265))

### 📚 Documentation

* add architecture and tasks for cumulative release notes ([08c516d](https://github.com/oocx/tfplan2md/commit/08c516d4db54be0b0240d974c610859c086739bb))
* add feature specification for cumulative release notes ([3a75a12](https://github.com/oocx/tfplan2md/commit/3a75a1273795ca21d1a15503f5d5d18724928638))
* add test plan for cumulative release notes ([91aba8a](https://github.com/oocx/tfplan2md/commit/91aba8a190ae3f0d14e5a0e930a7ff66d398b75b))

<a name="0.14.0"></a>
## [0.14.0](https://github.com/oocx/tfplan2md/compare/v0.13.1...v0.14.0) (2025-12-18)

### ✨ Features

* enhance Architect agent documentation with new tools and clarify constraints ([f8ce5c4](https://github.com/oocx/tfplan2md/commit/f8ce5c468a8460eb01d70014cbb80f38830fe01c))
* show before and after values for modified firewall rules ([542d202](https://github.com/oocx/tfplan2md/commit/542d202be61d9488a8aad104fd93888be10a3398))

### 🐛 Bug Fixes

* fix agent handoffs ([9dc407f](https://github.com/oocx/tfplan2md/commit/9dc407ff7bb9ddb0da5e8b2d2672b0bb04c6c5c8))

### 📚 Documentation

* add feature specification ([1cc5cc1](https://github.com/oocx/tfplan2md/commit/1cc5cc16a24d46896833e4bb6db0f033a958849d))
* architecture, tasks and test plan for new feature ([7375a88](https://github.com/oocx/tfplan2md/commit/7375a88a9836c39ba653c263d2ae9e9af6fc5aaf))

<a name="0.13.1"></a>
## [0.13.1](https://github.com/oocx/tfplan2md/compare/v0.13.0...v0.13.1) (2025-12-18)

### 🐛 Bug Fixes

* require Requirements Engineer to use local git commands for branch creation ([9a12a91](https://github.com/oocx/tfplan2md/commit/9a12a91e4393f278a92c94b3d51328d861ac60f4))

<a name="0.13.0"></a>
## [0.13.0](https://github.com/oocx/tfplan2md/compare/v0.12.0...v0.13.0) (2025-12-18)

### ✨ Features

* move branch creation to Requirements Engineer and add commits to planning agents ([bf1c6cb](https://github.com/oocx/tfplan2md/commit/bf1c6cb19a23a05402f2056850af4e4e000553aa))

<a name="0.12.0"></a>
## [0.12.0](https://github.com/oocx/tfplan2md/compare/v0.11.0...v0.12.0) (2025-12-18)

### ✨ Features

* improve all agents with data-driven model selection and comprehensive boundaries ([61e8089](https://github.com/oocx/tfplan2md/commit/61e8089057a139a18725ba92d524040dd712d3f2))

<a name="0.11.0"></a>
## [0.11.0](https://github.com/oocx/tfplan2md/compare/v0.10.0...v0.11.0) (2025-12-18)

### ✨ Features

* add Workflow Engineer agent for managing development workflow ([7d7a42b](https://github.com/oocx/tfplan2md/commit/7d7a42b09aa38056ace799c3bb4e9b00936b27de))
* enhance Architect agent documentation with new tools and clarify constraints ([ee9c9de](https://github.com/oocx/tfplan2md/commit/ee9c9deeb9d62b630db4d7614acd3f779dc8e29f))

### 🐛 Bug Fixes

* fix agent handoffs ([4b76700](https://github.com/oocx/tfplan2md/commit/4b767000926532f1fdb96942e2a384fe2ef0df82))

### 📚 Documentation

* add feature specification ([b66c25c](https://github.com/oocx/tfplan2md/commit/b66c25cffa38a6dde212d96d8908c8b1561ebdb6))
* architecture, tasks and test plan for new feature ([bfc7218](https://github.com/oocx/tfplan2md/commit/bfc721877972d7e807ccd26b0f433835b3485f67))

<a name="0.10.0"></a>
## [0.10.0](https://github.com/oocx/tfplan2md/compare/v0.9.0...v0.10.0) (2025-12-18)

### ✨ Features

* Update agent configurations to use VS Code tool IDs and enhance documentation ([249c096](https://github.com/oocx/tfplan2md/commit/249c096183c35575ba288dcc3de9386d03ce4314))
* Update agent tool lists to include new functionalities and improve integration ([a8fdaad](https://github.com/oocx/tfplan2md/commit/a8fdaad65777e9e91716684628394cf61c1de2a8))

### 📚 Documentation

* **agents:** require updating local main and creating feature branch before implementing features ([d8176ff](https://github.com/oocx/tfplan2md/commit/d8176ff5d82dcc186a2db9c14b5b5febb1171ae9))

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


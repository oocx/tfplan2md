# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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


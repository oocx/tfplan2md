# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<a name="1.2.0"></a>
## [1.2.0](https://github.com/oocx/tfplan2md/compare/v1.2.0...v1.2.0) (2025-12-27)

### ✨ Features

* add --show-unchanged-values CLI option to filter attribute tables ([1f9984b](https://github.com/oocx/tfplan2md/commit/1f9984b6ad6470019ffade96b3783a8ebaa37bd2))
* Add agent definitions and workflows for project development ([439cc91](https://github.com/oocx/tfplan2md/commit/439cc9119146b7d0f4a3091d6b20a5b734ac36e9))
* add architectural decision records for Scriban templating, Chiseled Docker image, and modern C# patterns ([7612fe7](https://github.com/oocx/tfplan2md/commit/7612fe71fdb947a4d517313aafe0ee5474cfdda6))
* add comprehensive demo and normalize markdown heading spacing ([fa03bb2](https://github.com/oocx/tfplan2md/commit/fa03bb28e7f4195e75c929e08cca03a94be1a79a))
* add diagnostic tools to agent definitions ([8020673](https://github.com/oocx/tfplan2md/commit/8020673f63704590f2501ade160c400d5343b37c))
* add handling for empty plans to display "No changes" message ([0420035](https://github.com/oocx/tfplan2md/commit/042003574585155931c772f93a1c22a924deb783))
* add inline diff formatting with char highlighting ([7a3e34d](https://github.com/oocx/tfplan2md/commit/7a3e34d97a08e028f10b08013e0522e12439a52f))
* add large value detection helper ([696d7f5](https://github.com/oocx/tfplan2md/commit/696d7f5e3e77d0f5e81f204dd89f5e6afd5d0a2b))
* add large-value-format cli option ([18ce46d](https://github.com/oocx/tfplan2md/commit/18ce46d75ddacc8121273e6384a24d0cf34e432f))
* add MCP server tools to agents based on role requirements ([555007a](https://github.com/oocx/tfplan2md/commit/555007ab976a2cd79d48a9ef1f4f8bf46bbab53b))
* add resource type breakdown to summary table ([16cd606](https://github.com/oocx/tfplan2md/commit/16cd606fd775d5210bb6decaced8ff2d01d19cd8))
* add retrospective agent and update workflow documentation ([7ca2847](https://github.com/oocx/tfplan2md/commit/7ca2847eaf594335a1a18a736ce557ff9a65f0c8))
* add Scriban reference and comprehensive demo requirements to agents ([9d6fae6](https://github.com/oocx/tfplan2md/commit/9d6fae6e7747aa610e13d2f4b79afea2dbdfea9a))
* add simulation mode constraints to UAT agents ([f4881f1](https://github.com/oocx/tfplan2md/commit/f4881f1301205ebc14bcb6641476e81d6c46eadd))
* add specialized template for Azure Network Security Group rules ([dffb253](https://github.com/oocx/tfplan2md/commit/dffb2538e42a18e98255e4afcbb8204509b71023))
* add standard diff formatting for large values ([0c29164](https://github.com/oocx/tfplan2md/commit/0c29164757865ebba1dceaebf64afb5443361bbc))
* add Support Engineer agent for bug fixes and incidents ([b079c79](https://github.com/oocx/tfplan2md/commit/b079c79ec10ea41d6ec8ee55eab3c6596996735d))
* add test instructions to UAT PR bodies ([cf1507b](https://github.com/oocx/tfplan2md/commit/cf1507b47f56bc06fcaf875cea7a2de3e1af3662))
* add UAT Tester agent and refactor Code Reviewer ([bd4318e](https://github.com/oocx/tfplan2md/commit/bd4318efe029facdf3d4f940292f479ee87cc602))
* add user acceptance testing with interactive notebooks ([42b0fa9](https://github.com/oocx/tfplan2md/commit/42b0fa9a7c703a901cf847af318518c6e3037495))
* add Workflow Engineer agent for managing development workflow ([7d7a42b](https://github.com/oocx/tfplan2md/commit/7d7a42b09aa38056ace799c3bb4e9b00936b27de))
* align consistent value formatting ([b4892aa](https://github.com/oocx/tfplan2md/commit/b4892aa42cf98e88034606633ef0e37a85e714c1))
* allow custom report title ([275d884](https://github.com/oocx/tfplan2md/commit/275d88404889510c6f0d28b27d7a683f7b3cf3a8))
* complete Task 7 - template integration ([05fbb86](https://github.com/oocx/tfplan2md/commit/05fbb86772cd5fe440db9530d079db5ec243d10f))
* enhance Architect agent documentation with new tools and clarify constraints ([7a0842c](https://github.com/oocx/tfplan2md/commit/7a0842c8f79982b3604a908c5c12fdef78aa6dbb))
* enhance Architect agent documentation with new tools and clarify constraints ([ee9c9de](https://github.com/oocx/tfplan2md/commit/ee9c9deeb9d62b630db4d7614acd3f779dc8e29f))
* enhance Architect agent documentation with new tools and clarify constraints ([f8ce5c4](https://github.com/oocx/tfplan2md/commit/f8ce5c468a8460eb01d70014cbb80f38830fe01c))
* Enhance documentation and setup instructions ([3180246](https://github.com/oocx/tfplan2md/commit/31802463ec3ae92460dbf82497f25835ed0b67cb))
* enhanced Azure role assignment display with comprehensive role mapping and scope parsing ([72458bb](https://github.com/oocx/tfplan2md/commit/72458bb4c77284073d43ea24e45385798160e46f))
* filter no-op resources from detailed changes to reduce output noise and fix errors with large plans ([c65f879](https://github.com/oocx/tfplan2md/commit/c65f8790d4883eb6914380d7946c87cbdde66221))
* implement backtick formatting for Azure resource ID values ([ee95011](https://github.com/oocx/tfplan2md/commit/ee9501196e00f1f8481fae5b9f229c26b9e0064f))
* implement built-in summary template and plan timestamp support ([a41965f](https://github.com/oocx/tfplan2md/commit/a41965f04a453ef9631b695de5f19c19c52e2f33))
* implement cumulative release notes for docker deployments ([d3d89a8](https://github.com/oocx/tfplan2md/commit/d3d89a89ac3386b9953a20e36a68f91165947092))
* implement large-attribute-value-display feature ([424b4c6](https://github.com/oocx/tfplan2md/commit/424b4c6ab70565ec1c740a74447b491710435d00))
* implement markdown quality validation and linting ([7cc7632](https://github.com/oocx/tfplan2md/commit/7cc7632d2f7e3b40385db6bb88e32c8c83035e7d))
* implement model-driven large value detection for Azure resource IDs ([2969f9b](https://github.com/oocx/tfplan2md/commit/2969f9bd5864fb2f6ea186a5b64149d7cfcb8d5b))
* implement replacement reasons and resource summaries ([391b2be](https://github.com/oocx/tfplan2md/commit/391b2be1d09f8022728470c74183160452d9fc17))
* Implement resource-specific templates for azurerm_firewall_network_rule_collection ([31bcfb6](https://github.com/oocx/tfplan2md/commit/31bcfb6a32bf187295e37d56967274a06d7bd469))
* implement selective markdown escaping and comprehensive quality validation ([65fe49f](https://github.com/oocx/tfplan2md/commit/65fe49f24f3f42275315c7814cf035a4d6a34505))
* implement strict simulation mode for UAT workflow ([5779737](https://github.com/oocx/tfplan2md/commit/5779737cb20753ccd206b6a4a5e2e80e9b0a4c01))
* implement subagent pattern for autonomous UAT execution ([5b38711](https://github.com/oocx/tfplan2md/commit/5b38711fe2d8a9361309194610e5701b050632f6))
* improve all agents with data-driven model selection and comprehensive boundaries ([61e8089](https://github.com/oocx/tfplan2md/commit/61e8089057a139a18725ba92d524040dd712d3f2))
* improve UAT workflow with helper scripts and autonomous polling ([9b0b142](https://github.com/oocx/tfplan2md/commit/9b0b1429e1e5ba16deeffe2aa23342033d781d05))
* initial implementation ([ea57cb1](https://github.com/oocx/tfplan2md/commit/ea57cb138a82133c4bf0cdb106767ae807a555e8))
* make Architect defer decisions to maintainer when multiple options exist ([004e685](https://github.com/oocx/tfplan2md/commit/004e6859ae947e747c8b834370ef0c3f98c2c56f))
* make Release Manager agent more autonomous ([3c73a06](https://github.com/oocx/tfplan2md/commit/3c73a06ef59f668e031f2433868910d2e9eb5ce8))
* move branch creation to Requirements Engineer and add commits to planning agents ([bf1c6cb](https://github.com/oocx/tfplan2md/commit/bf1c6cb19a23a05402f2056850af4e4e000553aa))
* per-action attribute tables in template; add docs and tests for edge cases ([ab62571](https://github.com/oocx/tfplan2md/commit/ab62571de19565d1b33b96e688f295b81825254a))
* replace acceptance notebooks with UAT PRs ([3ae417b](https://github.com/oocx/tfplan2md/commit/3ae417b8bdb3fd950a23f4bdc6b82d9d887f51e7))
* show before and after values for modified firewall rules ([b3b5bbd](https://github.com/oocx/tfplan2md/commit/b3b5bbdc4f9db9dc994e43d6e1931d81be6dcc4d))
* show before and after values for modified firewall rules ([542d202](https://github.com/oocx/tfplan2md/commit/542d202be61d9488a8aad104fd93888be10a3398))
* simplify default template ([580d27f](https://github.com/oocx/tfplan2md/commit/580d27f1fe087151b2efa8aa87e8a1b31a346646))
* update action symbols to use emojis in report generation and tests ([5d64bb1](https://github.com/oocx/tfplan2md/commit/5d64bb1bff37268e0c3617915321cfabeb1386b1))
* Update agent configurations to use VS Code tool IDs and enhance documentation ([249c096](https://github.com/oocx/tfplan2md/commit/249c096183c35575ba288dcc3de9386d03ce4314))
* Update agent tool lists to include new functionalities and improve integration ([a8fdaad](https://github.com/oocx/tfplan2md/commit/a8fdaad65777e9e91716684628394cf61c1de2a8))
* **agent:** post PR overview links before running UAT script ([775f4a6](https://github.com/oocx/tfplan2md/commit/775f4a6e15d1ac1d4cfb2dda47d66a91c2033f3f))
* **azure:** implement table format for role assignments ([7535945](https://github.com/oocx/tfplan2md/commit/75359455c0d19d43e449e003e674f0a1f89c9923))
* **module-grouping:** group resource changes by module; add grouping tests and documentation ([bbe5850](https://github.com/oocx/tfplan2md/commit/bbe5850db19ef9866ffb1c57ffd087c1c6a21e6d))
* **renderer:** enhance firewall rule rendering ([26200ae](https://github.com/oocx/tfplan2md/commit/26200aef4a855e4bbd14b32300298bedd97e017b))
* **retrospective:** add agent-grouped analysis and automation insights ([fd203a0](https://github.com/oocx/tfplan2md/commit/fd203a014d84ccbd7c99703822ba40fa222c05fd))
* **retrospective:** add rejection analysis and time breakdown ([8d33fc5](https://github.com/oocx/tfplan2md/commit/8d33fc58646e94c4fbce5547b8b463eb1d165d9a))
* **scripts:** add stdin support to PR scripts to avoid temp files ([7677f99](https://github.com/oocx/tfplan2md/commit/7677f998d4744144ab7e5048a323de28773f6e46))
* **skill:** add extract-metrics.sh script for chat export analysis ([b8c9559](https://github.com/oocx/tfplan2md/commit/b8c9559868278cbed0736bc30be2b11ce176e604))
* **skill:** add JSON output to extract-metrics.sh for cross-feature analysis ([fe4e2a1](https://github.com/oocx/tfplan2md/commit/fe4e2a15dda6353420d066d27e9100c854cceb27))
* **skills:** add analyze-chat-export skill for retrospective metrics ([00cfb41](https://github.com/oocx/tfplan2md/commit/00cfb4138790be09dd3540070acb167903fdd639))
* **uat:** add background agent for autonomous UAT execution ([0176192](https://github.com/oocx/tfplan2md/commit/0176192bd586a407200323aab04415deead95b59))
* **uat:** configure GitHub UAT to use dedicated tfplan2md-uat repository ([6e882cc](https://github.com/oocx/tfplan2md/commit/6e882cc894a01a0ed8fad3a0b0e6fab3a25f1c7c))
* **uat:** enforce simulation blocking and add smart platform-specific defaults ([4780e58](https://github.com/oocx/tfplan2md/commit/4780e580d3416f9c3d1febf77da96ea3d363f93c))
* **uat:** output PR URLs in UAT scripts ([8216229](https://github.com/oocx/tfplan2md/commit/82162299c65ffc725d0b9495225f235063b1f1ff))
* **workflow:** add agent skills and UAT skills ([693e51f](https://github.com/oocx/tfplan2md/commit/693e51f275c697263f991c9d1d7ebd591ce42adc))
* **workflow:** add azdo PR abandon wrapper ([ad05196](https://github.com/oocx/tfplan2md/commit/ad051963730d30ff6a9099dce214a99446c6e251))
* **workflow:** add comprehensive-demo-standard-diff artifact for GitHub UAT ([c05f367](https://github.com/oocx/tfplan2md/commit/c05f36717e2fa97af471d74a07c1575b5184fb36))
* **workflow:** add documentation alignment gate to Code Reviewer ([ac88d1a](https://github.com/oocx/tfplan2md/commit/ac88d1aaa40ab25dd7df20238fce6d807691bbf5))
* **workflow:** add one-command Azure DevOps PR script ([30d3864](https://github.com/oocx/tfplan2md/commit/30d38649a24a4ce2be54dcf126be106aca11ef6f))
* **workflow:** add one-command GitHub PR script ([8017fef](https://github.com/oocx/tfplan2md/commit/8017fef6adbfdf82af1a8665f9e578510957eade))
* **workflow:** add PR creation skills ([321ffab](https://github.com/oocx/tfplan2md/commit/321ffab4e07027e50c2da0012dfaa5675deec83d))
* **workflow:** add PR preview commands ([44349ba](https://github.com/oocx/tfplan2md/commit/44349ba81d01d85b5e1f20454574f1af4d80546a))
* **workflow:** add Release Manager → Retrospective handoff ([6869a63](https://github.com/oocx/tfplan2md/commit/6869a63c9685f0af221d18d05d8e33eb1578854a))
* **workflow:** add role boundaries and handoff/status templates ([94e2b55](https://github.com/oocx/tfplan2md/commit/94e2b55f12209f6e3484c7412dea5ac50b40bbc2))
* **workflow:** add stable scripts for demo generation and snapshot updates ([57b0cf2](https://github.com/oocx/tfplan2md/commit/57b0cf22117d65fb0bc7aa079daa6219823a98c7))
* **workflow:** add UAT PR watch skills ([9cb7b9c](https://github.com/oocx/tfplan2md/commit/9cb7b9cfd9fc0d651faf5fa2461dc65e005df78b))
* **workflow:** add uat-run wrapper and artifact guardrails ([e7dca79](https://github.com/oocx/tfplan2md/commit/e7dca79be2b55565f9a5372cecc8299c28a8829f))
* **workflow:** add view GitHub PR skill ([a79d500](https://github.com/oocx/tfplan2md/commit/a79d50081d1dabd7c12ad6a559543edc4d188b74))
* **workflow:** default UAT artifacts per platform ([94e1418](https://github.com/oocx/tfplan2md/commit/94e1418b2a4cb4ed2074d111c43bc78c6fefda72))
* **workflow:** update Developer agent to use stable demo and snapshot scripts ([8090d24](https://github.com/oocx/tfplan2md/commit/8090d24f1e72e4a008d68ec6faf789cb83201b5f))
* **workflow:** wire report style guide into agents ([8f50bc7](https://github.com/oocx/tfplan2md/commit/8f50bc7ee4a94e8a9b3f453ca2279b140da7f71d))

### 🐛 Bug Fixes

* add +/- markers to inline diffs for readability ([e86b7c5](https://github.com/oocx/tfplan2md/commit/e86b7c54e8a8377d40191563303949ca7a8b4793))
* add runCommands tool to Requirements Engineer agent ([02abe9a](https://github.com/oocx/tfplan2md/commit/02abe9a74bef3e1c3dfdc8ff99378a249021120a))
* add runInTerminal tool to Requirements Engineer and strengthen branch creation instructions ([2b9b2ec](https://github.com/oocx/tfplan2md/commit/2b9b2ec2c95c7bb234596ae50c63b7dbf30c4c10))
* address report-title review feedback ([21dd74c](https://github.com/oocx/tfplan2md/commit/21dd74c1b815477e6ed84a3994de441d5e095f7d))
* address UAT simulation findings ([a56a1ab](https://github.com/oocx/tfplan2md/commit/a56a1abc511071fedf817c59470b42edc4a3e598))
* apply whitespace control to fix table formatting ([d587f3c](https://github.com/oocx/tfplan2md/commit/d587f3c3952899ce165de6dd6f6d43279c219e55))
* avoid markdownlint errors for NSG empty descriptions ([5beeaff](https://github.com/oocx/tfplan2md/commit/5beeaffde75a0c6f63f537483ddfd67b6ea59496))
* correct MCP server name from microsoft-learn/* to microsoftdocs/mcp/* ([3b6fbcd](https://github.com/oocx/tfplan2md/commit/3b6fbcd3658d73f8bd9e45f213ac04558268101f))
* ensure gh cli calls are non-blocking in support engineer agent ([e561653](https://github.com/oocx/tfplan2md/commit/e561653750c24cc4fabae12d9af64100b81d0db0))
* exclude no-op resources from summary table Total count ([0fc5bdf](https://github.com/oocx/tfplan2md/commit/0fc5bdf3aac323f0261853d9aa6fd799296852af))
* fix agent handoffs ([4b76700](https://github.com/oocx/tfplan2md/commit/4b767000926532f1fdb96942e2a384fe2ef0df82))
* fix agent handoffs ([9dc407f](https://github.com/oocx/tfplan2md/commit/9dc407ff7bb9ddb0da5e8b2d2672b0bb04c6c5c8))
* handle empty before blocks and remove stray template separator ([e3fa75a](https://github.com/oocx/tfplan2md/commit/e3fa75adc454c1d605b622df64ab285b74f85b89))
* improve agent reliability and workflow consistency ([3726b10](https://github.com/oocx/tfplan2md/commit/3726b103448707f72288ba604c3500790507108e))
* improve agent workflow consistency and reliability ([f7e5ea9](https://github.com/oocx/tfplan2md/commit/f7e5ea9d052df3af81647ddebd49c4df34380351))
* improve Markdown rendering by enhancing template context handling and error reporting ([0c86c01](https://github.com/oocx/tfplan2md/commit/0c86c016f5b11d02bb9ce314ce683189996b6bad))
* improve UAT workflow reliability ([41bcb39](https://github.com/oocx/tfplan2md/commit/41bcb39b0ee74f644b08636c4130c83092669a3f))
* make changelog extraction POSIX AWK compatible ([b43573d](https://github.com/oocx/tfplan2md/commit/b43573dc262ddc18df34c6d817f2ecb95c2f2ef3))
* reduce UAT polling to 15s and restore branch after cleanup ([b10ec24](https://github.com/oocx/tfplan2md/commit/b10ec2423870620993d055a582e9ea1ba1cca7fb))
* remove extra newlines in attribute changes tables ([d6d185b](https://github.com/oocx/tfplan2md/commit/d6d185b9306ae4523cc20875e6d47d78512057bc))
* render line breaks in tables correctly by escaping values internally in format_diff ([4849a2e](https://github.com/oocx/tfplan2md/commit/4849a2eb012856c16371d9e8679ebeb6be3e4ddc))
* require Developer agent to handle skipped tests before marking work complete ([deee1b4](https://github.com/oocx/tfplan2md/commit/deee1b4036d5916fc26708ccd5234dfa92708e1d))
* require Requirements Engineer to use local git commands for branch creation ([27a3334](https://github.com/oocx/tfplan2md/commit/27a3334d6d38b85da30d9083fd1b898d42ace265))
* require Requirements Engineer to use local git commands for branch creation ([9a12a91](https://github.com/oocx/tfplan2md/commit/9a12a91e4393f278a92c94b3d51328d861ac60f4))
* resolve markdown rendering issues and enhance test coverage ([4bc947a](https://github.com/oocx/tfplan2md/commit/4bc947aa14f51b9400ebcc40b3ab44504a90cc58))
* strengthen agent boundaries and tool assignments ([c7cc9a0](https://github.com/oocx/tfplan2md/commit/c7cc9a0cf12dcadc1f63eecc7514c2ec43973755))
* strengthen UAT Tester autonomous execution instructions ([7568c27](https://github.com/oocx/tfplan2md/commit/7568c27459127cd57893655491c5239f955ce9be))
* strip trailing newlines in attribute changes table rows ([7120d2c](https://github.com/oocx/tfplan2md/commit/7120d2cebfecabf76472e6ae5a6cc5e5a2efc3f9))
* strip trailing newlines in attribute changes table rows ([a03e444](https://github.com/oocx/tfplan2md/commit/a03e444751dac2e08a800ccebb86de32ce76af57))
* tighten AzDO UAT approval and simplify simulation ([688c126](https://github.com/oocx/tfplan2md/commit/688c126a30d80220a5fdfcf19ac596e167d4958e))
* trigger release ([e01f730](https://github.com/oocx/tfplan2md/commit/e01f730541ad72db370b5c10f1b53965c9149904))
* update agent file links to use correct relative paths ([c3b26d2](https://github.com/oocx/tfplan2md/commit/c3b26d2a4b68a62c837aa192ee696826f1e19cc6))
* update agent models — Documentation Author, Quality Engineer, Support Engineer, Code Reviewer ([3e7a552](https://github.com/oocx/tfplan2md/commit/3e7a552f31fc29bb4d30e9f2a1e98ecadfb64051))
* update UAT Tester agent with GPT-5.2, correct tool names, and simulation instructions ([20f39fb](https://github.com/oocx/tfplan2md/commit/20f39fbeb97f2781bc9886f0e14fd1b06623706a))
* **agent:** simplify UAT Tester to run single command without monitoring ([892eb7e](https://github.com/oocx/tfplan2md/commit/892eb7efd910c5b3b57f7f2584d3808897ad9c60))
* **agent:** UAT Tester should run script in blocking mode, not background ([231ee5e](https://github.com/oocx/tfplan2md/commit/231ee5ee1a601a1522f299a6f713bb4add03edbf))
* **ci:** add .github/workflows/** to paths-ignore to skip CI for workflow-only changes ([ef186f0](https://github.com/oocx/tfplan2md/commit/ef186f031d5d427e392a6c4e30553eee905e9534))
* **ci:** add concurrency control and workflows permission to CI pipeline ([cb9816a](https://github.com/oocx/tfplan2md/commit/cb9816a503d534b81da466736b90390938e05975))
* **ci:** ensure workflows can push tags — persist-credentials and authenticated push; disable gh pager in release step ([03a8a5c](https://github.com/oocx/tfplan2md/commit/03a8a5c8db3616357804ab0183d667d915945e31))
* **ci:** remove invalid workflows permission ([bd96929](https://github.com/oocx/tfplan2md/commit/bd96929951c0968d7e083913c3213fb8fc56abc8))
* **renderer:** apply resource-specific templates automatically and add regression test ([92032f5](https://github.com/oocx/tfplan2md/commit/92032f51dcad9b521f4d0a76ae3234638ced465e))
* **uat:** block minimal artifacts and add validation helper + tests ([50a5ae3](https://github.com/oocx/tfplan2md/commit/50a5ae31f21bd510291acf80151d651efe0605ee))
* **uat:** correct background agent tool definitions ([b94fe59](https://github.com/oocx/tfplan2md/commit/b94fe59800ef28cca6c76db1faf6b4ff56adca29))
* **uat:** enable polling in simulation mode to allow approval testing ([3ccf23e](https://github.com/oocx/tfplan2md/commit/3ccf23eddb9e944a743a49bf0367ce43147af172))
* **uat:** keep validate_artifact stdout clean ([68d0c70](https://github.com/oocx/tfplan2md/commit/68d0c706b8b111bb3b3bef67e49644986381a684))
* **uat:** remove leftover simulation artifact template ([7982930](https://github.com/oocx/tfplan2md/commit/79829303ee039e87d5e4ae07616454406bcb4c94))
* **workflow:** add quotes to Retrospective agent handoff ([2e46ee0](https://github.com/oocx/tfplan2md/commit/2e46ee08d5b810879bf2a9731769db1048c82c4a))
* **workflow:** copy snapshots from bin output to source directory ([975b753](https://github.com/oocx/tfplan2md/commit/975b753b2056a4f3becd765d3b5af133d22273b3))
* **workflow:** correct CLI option to --large-value-format in demo generation script ([003b080](https://github.com/oocx/tfplan2md/commit/003b0802b362932ace6d8592a8533de7940c3a1f))
* **workflow:** fix Husky commit-msg args variable ([7156326](https://github.com/oocx/tfplan2md/commit/71563269d7f43e878ac7d1d998317b1d77022f5c))
* **workflow:** fix pr-github wrapper arg parsing ([878b6fe](https://github.com/oocx/tfplan2md/commit/878b6fec5ccc7b37ed64e1b0d2e5a002417df9c1))
* **workflow:** fix pr-github wrapper option parsing ([07bebbd](https://github.com/oocx/tfplan2md/commit/07bebbdaad7a1652831f50aba6c5e3ba8826f768))
* **workflow:** harden GitHub UAT polling ([ed28a6d](https://github.com/oocx/tfplan2md/commit/ed28a6de21fc0555515a6175a9e51934d85c7fbc))
* **workflow:** require explicit PR title and body ([1621124](https://github.com/oocx/tfplan2md/commit/1621124ee8f59793e46a5680534451d2d6866d9d))
* **workflow:** require PR description previews ([05af78e](https://github.com/oocx/tfplan2md/commit/05af78eb95c5cd0129af5f0f450e48a6f4986ecb))
* **workflow:** standardize response style and preview guidance ([4f5214f](https://github.com/oocx/tfplan2md/commit/4f5214f66acb46fb105d34f373ec28b32033790f))

### ♻️ Refactoring

* rename agents for clarity and consistency ([6b74f79](https://github.com/oocx/tfplan2md/commit/6b74f7917f0c07e17667c2e3cdc0fa44208bc2fa))
* require feature-specific test descriptions in UAT PRs ([a771882](https://github.com/oocx/tfplan2md/commit/a77188296885f994d835c00ab26b7709c2505f5d))
* simplify UAT to single agent calling uat-run.sh directly ([77bc2e3](https://github.com/oocx/tfplan2md/commit/77bc2e3f1535f9d97e9b492b65e1fc370dc04189))
* update assertions to use FluentAssertions for improved readability ([01c04c2](https://github.com/oocx/tfplan2md/commit/01c04c29d8080954a2925f3789c6580584c0756e))
* use GPT-5 mini for UAT Tester agent ([57103c5](https://github.com/oocx/tfplan2md/commit/57103c5c5ab55e9eae2f7637c9bcad0777582193))
* **scripts:** remove --body-file and --description options, enforce stdin-only ([4338625](https://github.com/oocx/tfplan2md/commit/4338625bf16197c1e8c34b58a7b354b7c6b170b9))
* **uat:** simplify simulation to use default artifacts ([50827df](https://github.com/oocx/tfplan2md/commit/50827dfec1c98c4d66cadfdd4b558d966b0656e0))

### 📚 Documentation

* add architecture and tasks for cumulative release notes ([08c516d](https://github.com/oocx/tfplan2md/commit/08c516d4db54be0b0240d974c610859c086739bb))
* add architecture and test plan for role assignment table format ([dd2804a](https://github.com/oocx/tfplan2md/commit/dd2804a23d403ecdcd43efdf86bfd621e278eb8a))
* add architecture for built-in templates ([7b7c812](https://github.com/oocx/tfplan2md/commit/7b7c812af2feb7d29f83e7c748bd995c1e48c9f1))
* add architecture for consistent-value-formatting ([adb8fdb](https://github.com/oocx/tfplan2md/commit/adb8fdbc4b8e47f5cd452b3eccc351219052de7b))
* add architecture for custom report title ([83fa608](https://github.com/oocx/tfplan2md/commit/83fa608bc15f6211fd19f231a0e99f94a2bf9f46))
* add architecture for large-attribute-value-display ([2709dca](https://github.com/oocx/tfplan2md/commit/2709dca05c6fdae3bdab29e5f1aa1b7920da7e50))
* add architecture for markdown quality validation ([b9857ab](https://github.com/oocx/tfplan2md/commit/b9857ab7e3b3bbb310d5e5a8f648b1b48781db9e))
* add architecture for NSG security rule template ([eb58b20](https://github.com/oocx/tfplan2md/commit/eb58b20c482a7139a9e0bd18181761c0fed521df))
* add architecture for replacement reasons and summaries ([2699a8f](https://github.com/oocx/tfplan2md/commit/2699a8f85dd6800ecb946ad0bc8e15658602caa2))
* add architecture for role-assignment-readable-display ([eb2dd44](https://github.com/oocx/tfplan2md/commit/eb2dd445ad42088759fc57db6fc933ee98ce44da))
* add architecture for summary resource type breakdown ([dd8457d](https://github.com/oocx/tfplan2md/commit/dd8457d31b2846fdcc7a3451b2377670d182387a))
* add architecture for unchanged values CLI option ([df95a47](https://github.com/oocx/tfplan2md/commit/df95a47c0fff869019671ce270f4e215a9038905))
* add architecture for universal Azure resource ID formatting ([e214ab3](https://github.com/oocx/tfplan2md/commit/e214ab36692d5e393478ae0c6aae4ac9b2096c63))
* add bug fixing guideline to documentation ([152accd](https://github.com/oocx/tfplan2md/commit/152accdb86221f5df78d8964dbd107d046ea557e))
* add detailed examples for feature-specific test descriptions ([7154403](https://github.com/oocx/tfplan2md/commit/71544034bff0e8702f8896eb7fa2c71f618e2d2f))
* add example project to generate a valid plan file ([39dffa8](https://github.com/oocx/tfplan2md/commit/39dffa84df1422fc32c14b28f977098cbc5a7bb4))
* add feature specification ([b66c25c](https://github.com/oocx/tfplan2md/commit/b66c25cffa38a6dde212d96d8908c8b1561ebdb6))
* add feature specification ([1cc5cc1](https://github.com/oocx/tfplan2md/commit/1cc5cc16a24d46896833e4bb6db0f033a958849d))
* add feature specification for built-in templates ([bc8af99](https://github.com/oocx/tfplan2md/commit/bc8af995ea6b40ca625012d6ace38582c4f1e076))
* add feature specification for consistent value formatting ([f00a05a](https://github.com/oocx/tfplan2md/commit/f00a05a3e170d82138a06eb2b50043b84b3c207a))
* add feature specification for cumulative release notes ([3a75a12](https://github.com/oocx/tfplan2md/commit/3a75a1273795ca21d1a15503f5d5d18724928638))
* add feature specification for custom report title ([37cfabf](https://github.com/oocx/tfplan2md/commit/37cfabfccd57f265b317e89c36b6ab9d02aadd4a))
* add feature specification for large attribute value display ([5a4cd2c](https://github.com/oocx/tfplan2md/commit/5a4cd2cf60e1cd8201f60ce876c4ce016ca249bd))
* add feature specification for markdown quality validation ([ff3b823](https://github.com/oocx/tfplan2md/commit/ff3b823179d9bf3b58e045af7834e6b714167d8e))
* add feature specification for NSG security rule template ([2c2082c](https://github.com/oocx/tfplan2md/commit/2c2082cc69b9ecd8e63983b3984e5ab9af2a0568))
* add feature specification for role assignment readable display ([825a1e5](https://github.com/oocx/tfplan2md/commit/825a1e5cf99a2665cd93c613e7ffd660d2d3ff87))
* add feature specification for role assignment table format ([ba7dfb4](https://github.com/oocx/tfplan2md/commit/ba7dfb49e74e42c2af061ca43322c9eb41e75e0a))
* add feature specification for summary resource type breakdown ([5883ac2](https://github.com/oocx/tfplan2md/commit/5883ac2e887213ae8ad19b33f785e95f9feb1f39))
* add feature specification for unchanged values CLI option ([ed47492](https://github.com/oocx/tfplan2md/commit/ed47492fb6cc6da02fcf3685cbcdba6c123c5fae))
* add feature specification for universal Azure resource ID formatting ([8804f2e](https://github.com/oocx/tfplan2md/commit/8804f2ea8be80caedb6fa051a7c93a446aebe295))
* add implementation tasks for custom report title ([3a7eef4](https://github.com/oocx/tfplan2md/commit/3a7eef4876ce31c5bd355e1e689bf87ed5b8657e))
* add independent LiveBench mappings for AI model recommendations ([005380d](https://github.com/oocx/tfplan2md/commit/005380dbf01c21248938cfd1614286bb37fe09df))
* add issue analysis for literal br tags in tables ([3636750](https://github.com/oocx/tfplan2md/commit/363675099f22ae4f68b994968a53499bb8ec4645))
* add issue analysis for markdown rendering errors in v0.26.0 ([6174b65](https://github.com/oocx/tfplan2md/commit/6174b65ac3d507418072372bece59fde7674c254))
* add issue analysis for summary table totals mismatch ([61b8252](https://github.com/oocx/tfplan2md/commit/61b8252d34ab8e768a836bd89ada57943f70b546))
* add mandatory artifact regeneration checklist for bug fixes ([fd54ab8](https://github.com/oocx/tfplan2md/commit/fd54ab88052cb9f83af3706d8c0c459a6904a3dc))
* add procedure for fixing agents during feature development ([188163d](https://github.com/oocx/tfplan2md/commit/188163d0521a4f1ea4cf14beee7e6f9f12d965c3))
* add retrospective for custom report title feature ([cf5c4e3](https://github.com/oocx/tfplan2md/commit/cf5c4e3cc20008027bad1e91e00193f04c0eeadc))
* add retrospective improvements tracker ([91ac9a5](https://github.com/oocx/tfplan2md/commit/91ac9a515f1d4af88573ff81b41533c42461cf27))
* add specification and architecture for comprehensive-demo ([0bbb619](https://github.com/oocx/tfplan2md/commit/0bbb61985be6ff0d4b4e1f72071620f3b66e048e))
* add tasks for built-in templates ([c21d7b8](https://github.com/oocx/tfplan2md/commit/c21d7b8fd4e50df1abc017d350149072b5d4bdb4))
* add tasks for comprehensive-demo ([a318082](https://github.com/oocx/tfplan2md/commit/a31808202746f43a2a56d0d2bd2ed9c47ea75bfb))
* add tasks for consistent-value-formatting ([aee0f34](https://github.com/oocx/tfplan2md/commit/aee0f348b3cb7c2fca77354a96065680cc16675e))
* add tasks for large-attribute-value-display ([74dc831](https://github.com/oocx/tfplan2md/commit/74dc8312b26a4a28e4c41bc60723577577d6fc0a))
* add tasks for markdown quality validation ([f92046b](https://github.com/oocx/tfplan2md/commit/f92046bbf33a9047f400a8e4f03f5b153f735c8d))
* add tasks for NSG security rule template ([4b043e0](https://github.com/oocx/tfplan2md/commit/4b043e0fbc3d1b1e88b87e62b1bc6855ae8cff77))
* add tasks for replacement reasons and summaries ([751879c](https://github.com/oocx/tfplan2md/commit/751879c9cd4b24ee0f0d356ae78eaeb38eba3825))
* add tasks for role assignment table format ([6e513bf](https://github.com/oocx/tfplan2md/commit/6e513bfbf6ddd7d4500e180418461d454f442605))
* add tasks for role-assignment-readable-display ([31c77d5](https://github.com/oocx/tfplan2md/commit/31c77d50265c513a3cd90a3df7d7040951e688b6))
* add tasks for summary resource type breakdown ([8ad1733](https://github.com/oocx/tfplan2md/commit/8ad17332c25d2beb082505294d23c484e73b2e31))
* add tasks for unchanged values CLI option ([b151f57](https://github.com/oocx/tfplan2md/commit/b151f570d5f2d854afa785e9682212288da22965))
* add tasks for universal Azure resource ID formatting ([8a0d8f0](https://github.com/oocx/tfplan2md/commit/8a0d8f0ac127e831535ddd1a96b48904ae0380a4))
* add test plan for built-in templates ([d40c327](https://github.com/oocx/tfplan2md/commit/d40c327593cd0e857c482a634c801fb38f95402a))
* add test plan for comprehensive-demo ([56a2ed0](https://github.com/oocx/tfplan2md/commit/56a2ed03706f6d86d7b5e18ce7b9b40d784476f9))
* add test plan for consistent-value-formatting ([23681c5](https://github.com/oocx/tfplan2md/commit/23681c5d13e203ef1885b697d718099fb89305b1))
* add test plan for cumulative release notes ([91aba8a](https://github.com/oocx/tfplan2md/commit/91aba8a190ae3f0d14e5a0e930a7ff66d398b75b))
* add test plan for custom report title ([ae2e922](https://github.com/oocx/tfplan2md/commit/ae2e9229092e7145e7efd72eb18a70a368f30f06))
* add test plan for large-attribute-value-display ([dd768f5](https://github.com/oocx/tfplan2md/commit/dd768f504453ee4ed251033d7178b5c3e7f77fac))
* add test plan for markdown quality validation ([9b4b04b](https://github.com/oocx/tfplan2md/commit/9b4b04b6610da178ee20fa12615ba81dc70b3b39))
* add test plan for NSG security rule template ([e30b553](https://github.com/oocx/tfplan2md/commit/e30b553084343e5631602778cfe6a94c7939fc57))
* add test plan for replacement reasons and summaries ([b04324f](https://github.com/oocx/tfplan2md/commit/b04324fb4a6e2c1e022519da9c07e9fb53f5c0fe))
* add test plan for role-assignment-readable-display ([23b7573](https://github.com/oocx/tfplan2md/commit/23b7573a8d6f417879c11322647af3304ce65f68))
* add test plan for summary resource type breakdown ([1b322de](https://github.com/oocx/tfplan2md/commit/1b322deed9e770962704bde56aaa3fa8c9d7903a))
* add test plan for unchanged values CLI option ([9697f51](https://github.com/oocx/tfplan2md/commit/9697f51ade134e3efc85bc470fd3f5b13c078186))
* add test plan for universal Azure resource ID formatting ([1536620](https://github.com/oocx/tfplan2md/commit/15366208fc3cf40608ae2e988c420479a23288c7))
* add text color for dark mode compatibility in example outputs ([43777e6](https://github.com/oocx/tfplan2md/commit/43777e67e4c85c687c88a9432dcc80890c95ddf4))
* add user note about automation rate to retrospective ([f7d844b](https://github.com/oocx/tfplan2md/commit/f7d844b85bfc5a8e0c1964538fcb7be95fcd6a4d))
* align Azure ID formatting examples and add review report ([b48e36f](https://github.com/oocx/tfplan2md/commit/b48e36f1bc0aec67423fb805a69f7d0a0c78bdb2))
* align release docs with tag-only triggers ([e77d817](https://github.com/oocx/tfplan2md/commit/e77d817e01f5c38d31ac514036d01683b72381e2))
* architecture, tasks and test plan for new feature ([bfc7218](https://github.com/oocx/tfplan2md/commit/bfc721877972d7e807ccd26b0f433835b3485f67))
* architecture, tasks and test plan for new feature ([7375a88](https://github.com/oocx/tfplan2md/commit/7375a88a9836c39ba653c263d2ae9e9af6fc5aaf))
* clarify that templates add the # character, not the tool ([2a0f496](https://github.com/oocx/tfplan2md/commit/2a0f496e57c04301997cef452f31b099293cb5b9))
* clarify that templates control their own default titles ([f444dd7](https://github.com/oocx/tfplan2md/commit/f444dd7e6498f56618e63fcef9a428843e99669c))
* complete Task 8 - documentation updates ([993ff09](https://github.com/oocx/tfplan2md/commit/993ff09a5d313e2a98d585054af446c8822c9e52))
* document prerelease + tag-only release rationale ([674ee00](https://github.com/oocx/tfplan2md/commit/674ee006632be231fbf6df26a3c874ef66774689))
* enforce artifact ownership boundaries across agents ([89a5e8b](https://github.com/oocx/tfplan2md/commit/89a5e8b002b5331ce5b4f94dfe9979dfd15f878b))
* enforce rebase-only release merges ([2295e7b](https://github.com/oocx/tfplan2md/commit/2295e7bc042015b7ad3ade8cbe2cd81fd6551857))
* finalize documentation and code review for large value display ([2525400](https://github.com/oocx/tfplan2md/commit/252540081f29619da3c73606d31ddba106420d50))
* finalize PR-based UAT workflow ([e3aaa46](https://github.com/oocx/tfplan2md/commit/e3aaa465cdb20deb2b116c245fd4e03f87bcd341))
* improve agent workflow consistency and visual appearance ([e88c5d8](https://github.com/oocx/tfplan2md/commit/e88c5d8bf81f4f2c7d57862d8581c81d665c98f7))
* improve Release Manager safety and efficiency ([142d939](https://github.com/oocx/tfplan2md/commit/142d9393721fa6112c7046de06c529ce168d3434))
* initial project specification ([6a0cc4e](https://github.com/oocx/tfplan2md/commit/6a0cc4e89dc7afbb77436292615bf4a9fbbf25a0))
* mark consistent value formatting review approved ([38c55f0](https://github.com/oocx/tfplan2md/commit/38c55f09d6d40be186840590fab63e32ee882228))
* mark inline diff tasks as done ([3a64660](https://github.com/oocx/tfplan2md/commit/3a646602916cef8cc200b76ff0fbb5c7d4072d56))
* mark issue [#6](https://github.com/oocx/tfplan2md/issues/6) as completed in retrospective tracker ([c4eeb1c](https://github.com/oocx/tfplan2md/commit/c4eeb1ce2d9f59b45922c1da7915aafa752873ae))
* mark issue [#7](https://github.com/oocx/tfplan2md/issues/7) as done - 12/13 completed (92%) ([4846cae](https://github.com/oocx/tfplan2md/commit/4846cae87fd62dc691b538a85bf3686e0c3f67e4))
* mark issues [#9](https://github.com/oocx/tfplan2md/issues/9) and [#10](https://github.com/oocx/tfplan2md/issues/10) as completed in retrospective tracker ([b23ce8c](https://github.com/oocx/tfplan2md/commit/b23ce8cb671dbf07a0b563463718a10aab9e22f8))
* mark q4 workflow roadmap complete ([91c8eb3](https://github.com/oocx/tfplan2md/commit/91c8eb3ecfe5503519483797f93039425bfe5342))
* mark task1 large-value-format cli as done ([e2a8358](https://github.com/oocx/tfplan2md/commit/e2a8358c560ce8b9f543f0ae7728487eb60b4202))
* mark task2 large value detection as done ([8efaa0f](https://github.com/oocx/tfplan2md/commit/8efaa0ffa5d7b192bfd5db5b106047ee96d8847b))
* mark task3 standard diff as done ([3e470cb](https://github.com/oocx/tfplan2md/commit/3e470cbf4e95f022e402a7733eaf28b70cf1bdd5))
* move UAT test plan responsibility to Quality Engineer ([8900515](https://github.com/oocx/tfplan2md/commit/8900515a59bd3edff6fe6242ed5c8bfed6160d12))
* move UAT test plan to feature folder and update agent instructions ([924c26f](https://github.com/oocx/tfplan2md/commit/924c26fd10fea05159799f30dd3ef8b62f787f52))
* prevent Task Planner from starting implementation ([388ce98](https://github.com/oocx/tfplan2md/commit/388ce985ac0631e384a86bbecae1bcf05657d82f))
* refine architecture to specify table-compatible diff formatting ([af43ae7](https://github.com/oocx/tfplan2md/commit/af43ae7130419d73b5abcded888f71bfcd188ebe))
* regenerate comprehensive demo ([88db09e](https://github.com/oocx/tfplan2md/commit/88db09ed09fc4567b4bbc37f021c7965968a1fdf))
* remove UAT Background agent references from agents.md ([c95489e](https://github.com/oocx/tfplan2md/commit/c95489e663d92649fe391569c6c622e1ad051c80))
* require full lifecycle analysis and mandatory metrics in Retrospective ([1a44576](https://github.com/oocx/tfplan2md/commit/1a44576c837838fc4b49820c318f46382d1db6d2))
* update AI model reference and agent configurations ([8dbabf0](https://github.com/oocx/tfplan2md/commit/8dbabf0b2091b53de008d643ddf02d263f9b349a))
* update AI model reference with GPT-5.2 benchmark data ([294d18e](https://github.com/oocx/tfplan2md/commit/294d18ea0127c8eac2a084dd95fa9454bed6b1a9))
* update architecture to use registered helper configuration ([e50c1fd](https://github.com/oocx/tfplan2md/commit/e50c1fd4014d71a0a13c87c1e11c215aa39222f4))
* update custom report title UAT report with results ([e3109ab](https://github.com/oocx/tfplan2md/commit/e3109abc682e7c7bbed8c432185fe5d296179cda))
* update documentation ([c0474ad](https://github.com/oocx/tfplan2md/commit/c0474adb14e048bfeab79fa7a9e89d6670cf15fa))
* update examples with proper rendering and refined role assignment formatting ([a0ae233](https://github.com/oocx/tfplan2md/commit/a0ae23304a3c67aa35d192b8ac86c37a060f1416))
* update large value feature docs ([d46a9b6](https://github.com/oocx/tfplan2md/commit/d46a9b64bd1774a7af52493c1d14fb8db78541f0))
* update remaining references to use new agent names ([d6baf4a](https://github.com/oocx/tfplan2md/commit/d6baf4a39c2fe3717b21b5f0940472a86068549a))
* update retrospective agent performance table ([a1ac4eb](https://github.com/oocx/tfplan2md/commit/a1ac4ebceb2355c55892701c4698217130ca39a6))
* update retrospective and roadmap with PR [#117](https://github.com/oocx/tfplan2md/issues/117) progress ([ecb7f96](https://github.com/oocx/tfplan2md/commit/ecb7f9621b068f6c188ba15a2b430231585e2581))
* update retrospective tracking with completed PRs [#111](https://github.com/oocx/tfplan2md/issues/111)-114 ([d9e6c7c](https://github.com/oocx/tfplan2md/commit/d9e6c7c38c4707ab13ad8e1b6f090c69941e905a))
* update retrospective with interactive feedback and agent improvements ([16e41b6](https://github.com/oocx/tfplan2md/commit/16e41b69050221d0ca15d8d5f220a298242cf620))
* update retrospective with more critical evaluation ([fee1860](https://github.com/oocx/tfplan2md/commit/fee186036834a64ae736fdd554e0fa7b96ac259a))
* update retrospective with user observations and new action items ([fe88c2d](https://github.com/oocx/tfplan2md/commit/fe88c2d86e67d18c7ee443547fa43069ecd69caf))
* update tasks for custom report title ([745b4b9](https://github.com/oocx/tfplan2md/commit/745b4b9e1d5ed3da92fed448e64bf7495c708b1b))
* update tasks for registered helper configuration ([24ee0c3](https://github.com/oocx/tfplan2md/commit/24ee0c3f0eae555bb525a5eea1ee479871e77c73))
* update tasks with actionable user stories ([ce79cdc](https://github.com/oocx/tfplan2md/commit/ce79cdc65880d8efd8bed9322ee4fe92a7d6f0ce))
* update terminal command guidelines ([69b52d9](https://github.com/oocx/tfplan2md/commit/69b52d9da308e122406cfc597b040454d996fb17))
* update test documentation ([f8fdb0f](https://github.com/oocx/tfplan2md/commit/f8fdb0fa049e9a1a152a24382334046c5c0d6ca9))
* update test plan with UAT artifact instructions ([43dcada](https://github.com/oocx/tfplan2md/commit/43dcada5d51151329f9de364d1d838b539fdb580))
* update UAT artifact instructions in QE and Tester agents ([8a2ecad](https://github.com/oocx/tfplan2md/commit/8a2ecad98fe2e462856ac740d9465b748ab598df))
* **agent:** instruct UAT Tester to run script in background and report PR links immediately ([afdaf6f](https://github.com/oocx/tfplan2md/commit/afdaf6f87b30b76a846b6101071d981f3934a205))
* **agents:** add GitHub tools to Retrospective ([0647648](https://github.com/oocx/tfplan2md/commit/0647648a29617579d1aeef02b815ae17dfdb9511))
* **agents:** prefer GitHub tools over gh ([efb0e07](https://github.com/oocx/tfplan2md/commit/efb0e0740b937bfe99cb27990453851301e06262))
* **agents:** require updating local main and creating feature branch before implementing features ([d8176ff](https://github.com/oocx/tfplan2md/commit/d8176ff5d82dcc186a2db9c14b5b5febb1171ae9))
* **gh:** prefer explicit PR body ([8f07fb2](https://github.com/oocx/tfplan2md/commit/8f07fb2a58e1cf2dd703dd4cf1b4faa2fff425bd))
* **release:** prefer explicit PR body ([6acee4a](https://github.com/oocx/tfplan2md/commit/6acee4aa76f5e61b7efc54e54e88eb9a36fa966b))
* **release:** prefer scripts/pr-github.sh for create+merge (rebase-and-merge) ([929f726](https://github.com/oocx/tfplan2md/commit/929f72690ca47bf6110d00cd2f7703cadfc6c98c))
* **retrospective:** add chat log ([41ea358](https://github.com/oocx/tfplan2md/commit/41ea358bbe9925b07036330bd6d6200c38326d48))
* **retrospective:** lead time + metrics guidance ([d5406e3](https://github.com/oocx/tfplan2md/commit/d5406e39db76d673c64c5edab2b3de269da4e8bc))
* **skill:** remove agent-dependent metrics from chat analysis ([137d0c5](https://github.com/oocx/tfplan2md/commit/137d0c59cebefc2ff493e3dcc9f25687fd8b47db))
* **skill:** remove misleading workaround for agent limitation ([c304731](https://github.com/oocx/tfplan2md/commit/c304731f3df8a65f9ddbdbd31ebefcaa11f6ab7a))
* **skills:** add VS Code source-based chat export format specification ([88beaf2](https://github.com/oocx/tfplan2md/commit/88beaf29238e3853ddc4a2f1177e22886049d083))
* **skills:** make explicit PR body default ([e745a9b](https://github.com/oocx/tfplan2md/commit/e745a9bb458cc2b485e2eba463fda17df9c247c9))
* **skills:** update create-pr-github skill to use stdin instead of --body-file ([2046e43](https://github.com/oocx/tfplan2md/commit/2046e43ff34f33921bf671a0622cc92ba8802869))
* **testing:** update UAT instructions to use scripts ([acc9a06](https://github.com/oocx/tfplan2md/commit/acc9a0688588451c1a00a616789005a055dfab71))
* **uat:** capture consistent value formatting findings ([30cb80a](https://github.com/oocx/tfplan2md/commit/30cb80acb7d146550fbe766509421d9487d99d04))
* **workflow:** add PR comment retrieval guidance ([133d840](https://github.com/oocx/tfplan2md/commit/133d840b5351ce37b24d4e9a198c73b0d396ddb2))
* **workflow:** add skill approval-minimization guidance ([b30cf52](https://github.com/oocx/tfplan2md/commit/b30cf525720b5d2490cc7e2ba2664a5c0b99818b))
* **workflow:** define when to use todo lists ([cb5ff60](https://github.com/oocx/tfplan2md/commit/cb5ff60120a36414d3a6606eedb463db117d1ee9))
* **workflow:** link latest retrospective follow-ups ([5903082](https://github.com/oocx/tfplan2md/commit/590308242087c9ca3a4df8dc99edb29433c8f74b))
* **workflow:** prefer explicit PR body ([c0a4ee7](https://github.com/oocx/tfplan2md/commit/c0a4ee705aad328ae34739724ea424bc4cb55080))
* **workflow:** prefer GitHub tools for PR inspection ([ba1f88f](https://github.com/oocx/tfplan2md/commit/ba1f88fe6e4e056195d7a2830313ac58c01e9d9c))
* **workflow:** prefer GitHub tools over gh ([88f697f](https://github.com/oocx/tfplan2md/commit/88f697f94c4f3360bea5387e47b6ad7b6c11675b))
* **workflow:** prefer PR wrapper scripts in gh instructions ([1362f99](https://github.com/oocx/tfplan2md/commit/1362f9941a422db426994c6265309cb3c01c0fa3))
* **workflow:** require PR preview in agent prompts ([82d84ce](https://github.com/oocx/tfplan2md/commit/82d84ce7fa5d77c6457cc39a69574450052e03a3))
* **workflow:** show PR title and summary before create ([d3c48f8](https://github.com/oocx/tfplan2md/commit/d3c48f80aa6adb3833a0e656ff1260cce3a6786e))
* **workflow:** track retrospective follow-up progress ([e31c619](https://github.com/oocx/tfplan2md/commit/e31c6190682a6c688e77cb01aa3c85128f61294f))

### Breaking Changes

* **scripts:** remove --body-file and --description options, enforce stdin-only ([4338625](https://github.com/oocx/tfplan2md/commit/4338625bf16197c1e8c34b58a7b354b7c6b170b9))

<a name="1.2.0"></a>
## [1.2.0](https://github.com/oocx/tfplan2md/compare/v1.1.0...v1.2.0) (2025-12-27)

### ✨ Features

* allow custom report title ([275d884](https://github.com/oocx/tfplan2md/commit/275d88404889510c6f0d28b27d7a683f7b3cf3a8))

### 🐛 Bug Fixes

* address report-title review feedback ([21dd74c](https://github.com/oocx/tfplan2md/commit/21dd74c1b815477e6ed84a3994de441d5e095f7d))

### 📚 Documentation

* add architecture for custom report title ([83fa608](https://github.com/oocx/tfplan2md/commit/83fa608bc15f6211fd19f231a0e99f94a2bf9f46))
* add feature specification for custom report title ([37cfabf](https://github.com/oocx/tfplan2md/commit/37cfabfccd57f265b317e89c36b6ab9d02aadd4a))
* add implementation tasks for custom report title ([3a7eef4](https://github.com/oocx/tfplan2md/commit/3a7eef4876ce31c5bd355e1e689bf87ed5b8657e))
* add test plan for custom report title ([ae2e922](https://github.com/oocx/tfplan2md/commit/ae2e9229092e7145e7efd72eb18a70a368f30f06))
* clarify that templates add the # character, not the tool ([2a0f496](https://github.com/oocx/tfplan2md/commit/2a0f496e57c04301997cef452f31b099293cb5b9))
* clarify that templates control their own default titles ([f444dd7](https://github.com/oocx/tfplan2md/commit/f444dd7e6498f56618e63fcef9a428843e99669c))
* move UAT test plan to feature folder and update agent instructions ([924c26f](https://github.com/oocx/tfplan2md/commit/924c26fd10fea05159799f30dd3ef8b62f787f52))
* update custom report title UAT report with results ([e3109ab](https://github.com/oocx/tfplan2md/commit/e3109abc682e7c7bbed8c432185fe5d296179cda))
* update tasks for custom report title ([745b4b9](https://github.com/oocx/tfplan2md/commit/745b4b9e1d5ed3da92fed448e64bf7495c708b1b))
* update test plan with UAT artifact instructions ([43dcada](https://github.com/oocx/tfplan2md/commit/43dcada5d51151329f9de364d1d838b539fdb580))

<a name="1.1.0"></a>
## [1.1.0](https://github.com/oocx/tfplan2md/compare/v1.0.0...v1.1.0) (2025-12-27)

### ✨ Features

* add simulation mode constraints to UAT agents ([f4881f1](https://github.com/oocx/tfplan2md/commit/f4881f1301205ebc14bcb6641476e81d6c46eadd))
* implement strict simulation mode for UAT workflow ([5779737](https://github.com/oocx/tfplan2md/commit/5779737cb20753ccd206b6a4a5e2e80e9b0a4c01))
* implement subagent pattern for autonomous UAT execution ([5b38711](https://github.com/oocx/tfplan2md/commit/5b38711fe2d8a9361309194610e5701b050632f6))
* **agent:** post PR overview links before running UAT script ([775f4a6](https://github.com/oocx/tfplan2md/commit/775f4a6e15d1ac1d4cfb2dda47d66a91c2033f3f))
* **uat:** add background agent for autonomous UAT execution ([0176192](https://github.com/oocx/tfplan2md/commit/0176192bd586a407200323aab04415deead95b59))
* **uat:** output PR URLs in UAT scripts ([8216229](https://github.com/oocx/tfplan2md/commit/82162299c65ffc725d0b9495225f235063b1f1ff))

### 🐛 Bug Fixes

* **agent:** simplify UAT Tester to run single command without monitoring ([892eb7e](https://github.com/oocx/tfplan2md/commit/892eb7efd910c5b3b57f7f2584d3808897ad9c60))
* **agent:** UAT Tester should run script in blocking mode, not background ([231ee5e](https://github.com/oocx/tfplan2md/commit/231ee5ee1a601a1522f299a6f713bb4add03edbf))
* **uat:** correct background agent tool definitions ([b94fe59](https://github.com/oocx/tfplan2md/commit/b94fe59800ef28cca6c76db1faf6b4ff56adca29))
* **uat:** enable polling in simulation mode to allow approval testing ([3ccf23e](https://github.com/oocx/tfplan2md/commit/3ccf23eddb9e944a743a49bf0367ce43147af172))
* **uat:** remove leftover simulation artifact template ([7982930](https://github.com/oocx/tfplan2md/commit/79829303ee039e87d5e4ae07616454406bcb4c94))

### ♻️ Refactoring

* simplify UAT to single agent calling uat-run.sh directly ([77bc2e3](https://github.com/oocx/tfplan2md/commit/77bc2e3f1535f9d97e9b492b65e1fc370dc04189))
* **uat:** simplify simulation to use default artifacts ([50827df](https://github.com/oocx/tfplan2md/commit/50827dfec1c98c4d66cadfdd4b558d966b0656e0))

### 📚 Documentation

* move UAT test plan responsibility to Quality Engineer ([8900515](https://github.com/oocx/tfplan2md/commit/8900515a59bd3edff6fe6242ed5c8bfed6160d12))
* remove UAT Background agent references from agents.md ([c95489e](https://github.com/oocx/tfplan2md/commit/c95489e663d92649fe391569c6c622e1ad051c80))
* update UAT artifact instructions in QE and Tester agents ([8a2ecad](https://github.com/oocx/tfplan2md/commit/8a2ecad98fe2e462856ac740d9465b748ab598df))
* **agent:** instruct UAT Tester to run script in background and report PR links immediately ([afdaf6f](https://github.com/oocx/tfplan2md/commit/afdaf6f87b30b76a846b6101071d981f3934a205))

<a name="1.0.0"></a>
## [1.0.0](https://github.com/oocx/tfplan2md/compare/v0.48.0...v1.0.0) (2025-12-27)

### ✨ Features

* add test instructions to UAT PR bodies ([cf1507b](https://github.com/oocx/tfplan2md/commit/cf1507b47f56bc06fcaf875cea7a2de3e1af3662))
* implement backtick formatting for Azure resource ID values ([ee95011](https://github.com/oocx/tfplan2md/commit/ee9501196e00f1f8481fae5b9f229c26b9e0064f))
* implement model-driven large value detection for Azure resource IDs ([2969f9b](https://github.com/oocx/tfplan2md/commit/2969f9bd5864fb2f6ea186a5b64149d7cfcb8d5b))
* **retrospective:** add agent-grouped analysis and automation insights ([fd203a0](https://github.com/oocx/tfplan2md/commit/fd203a014d84ccbd7c99703822ba40fa222c05fd))
* **retrospective:** add rejection analysis and time breakdown ([8d33fc5](https://github.com/oocx/tfplan2md/commit/8d33fc58646e94c4fbce5547b8b463eb1d165d9a))
* **scripts:** add stdin support to PR scripts to avoid temp files ([7677f99](https://github.com/oocx/tfplan2md/commit/7677f998d4744144ab7e5048a323de28773f6e46))
* **skill:** add extract-metrics.sh script for chat export analysis ([b8c9559](https://github.com/oocx/tfplan2md/commit/b8c9559868278cbed0736bc30be2b11ce176e604))
* **skill:** add JSON output to extract-metrics.sh for cross-feature analysis ([fe4e2a1](https://github.com/oocx/tfplan2md/commit/fe4e2a15dda6353420d066d27e9100c854cceb27))
* **skills:** add analyze-chat-export skill for retrospective metrics ([00cfb41](https://github.com/oocx/tfplan2md/commit/00cfb4138790be09dd3540070acb167903fdd639))
* **uat:** configure GitHub UAT to use dedicated tfplan2md-uat repository ([6e882cc](https://github.com/oocx/tfplan2md/commit/6e882cc894a01a0ed8fad3a0b0e6fab3a25f1c7c))

### 🐛 Bug Fixes

* **ci:** ensure workflows can push tags — persist-credentials and authenticated push; disable gh pager in release step ([03a8a5c](https://github.com/oocx/tfplan2md/commit/03a8a5c8db3616357804ab0183d667d915945e31))
* **uat:** block minimal artifacts and add validation helper + tests ([50a5ae3](https://github.com/oocx/tfplan2md/commit/50a5ae31f21bd510291acf80151d651efe0605ee))
* **uat:** keep validate_artifact stdout clean ([68d0c70](https://github.com/oocx/tfplan2md/commit/68d0c706b8b111bb3b3bef67e49644986381a684))

### ♻️ Refactoring

* require feature-specific test descriptions in UAT PRs ([a771882](https://github.com/oocx/tfplan2md/commit/a77188296885f994d835c00ab26b7709c2505f5d))
* **scripts:** remove --body-file and --description options, enforce stdin-only ([4338625](https://github.com/oocx/tfplan2md/commit/4338625bf16197c1e8c34b58a7b354b7c6b170b9))

### 📚 Documentation

* add architecture for universal Azure resource ID formatting ([e214ab3](https://github.com/oocx/tfplan2md/commit/e214ab36692d5e393478ae0c6aae4ac9b2096c63))
* add detailed examples for feature-specific test descriptions ([7154403](https://github.com/oocx/tfplan2md/commit/71544034bff0e8702f8896eb7fa2c71f618e2d2f))
* add feature specification for universal Azure resource ID formatting ([8804f2e](https://github.com/oocx/tfplan2md/commit/8804f2ea8be80caedb6fa051a7c93a446aebe295))
* add mandatory artifact regeneration checklist for bug fixes ([fd54ab8](https://github.com/oocx/tfplan2md/commit/fd54ab88052cb9f83af3706d8c0c459a6904a3dc))
* add retrospective improvements tracker ([91ac9a5](https://github.com/oocx/tfplan2md/commit/91ac9a515f1d4af88573ff81b41533c42461cf27))
* add tasks for universal Azure resource ID formatting ([8a0d8f0](https://github.com/oocx/tfplan2md/commit/8a0d8f0ac127e831535ddd1a96b48904ae0380a4))
* add test plan for universal Azure resource ID formatting ([1536620](https://github.com/oocx/tfplan2md/commit/15366208fc3cf40608ae2e988c420479a23288c7))
* align Azure ID formatting examples and add review report ([b48e36f](https://github.com/oocx/tfplan2md/commit/b48e36f1bc0aec67423fb805a69f7d0a0c78bdb2))
* enforce artifact ownership boundaries across agents ([89a5e8b](https://github.com/oocx/tfplan2md/commit/89a5e8b002b5331ce5b4f94dfe9979dfd15f878b))
* enforce rebase-only release merges ([2295e7b](https://github.com/oocx/tfplan2md/commit/2295e7bc042015b7ad3ade8cbe2cd81fd6551857))
* improve Release Manager safety and efficiency ([142d939](https://github.com/oocx/tfplan2md/commit/142d9393721fa6112c7046de06c529ce168d3434))
* mark issue [#6](https://github.com/oocx/tfplan2md/issues/6) as completed in retrospective tracker ([c4eeb1c](https://github.com/oocx/tfplan2md/commit/c4eeb1ce2d9f59b45922c1da7915aafa752873ae))
* mark issue [#7](https://github.com/oocx/tfplan2md/issues/7) as done - 12/13 completed (92%) ([4846cae](https://github.com/oocx/tfplan2md/commit/4846cae87fd62dc691b538a85bf3686e0c3f67e4))
* mark issues [#9](https://github.com/oocx/tfplan2md/issues/9) and [#10](https://github.com/oocx/tfplan2md/issues/10) as completed in retrospective tracker ([b23ce8c](https://github.com/oocx/tfplan2md/commit/b23ce8cb671dbf07a0b563463718a10aab9e22f8))
* mark q4 workflow roadmap complete ([91c8eb3](https://github.com/oocx/tfplan2md/commit/91c8eb3ecfe5503519483797f93039425bfe5342))
* prevent Task Planner from starting implementation ([388ce98](https://github.com/oocx/tfplan2md/commit/388ce985ac0631e384a86bbecae1bcf05657d82f))
* require full lifecycle analysis and mandatory metrics in Retrospective ([1a44576](https://github.com/oocx/tfplan2md/commit/1a44576c837838fc4b49820c318f46382d1db6d2))
* **release:** prefer scripts/pr-github.sh for create+merge (rebase-and-merge) ([929f726](https://github.com/oocx/tfplan2md/commit/929f72690ca47bf6110d00cd2f7703cadfc6c98c))
* **retrospective:** add chat log ([41ea358](https://github.com/oocx/tfplan2md/commit/41ea358bbe9925b07036330bd6d6200c38326d48))
* **skill:** remove agent-dependent metrics from chat analysis ([137d0c5](https://github.com/oocx/tfplan2md/commit/137d0c59cebefc2ff493e3dcc9f25687fd8b47db))
* **skill:** remove misleading workaround for agent limitation ([c304731](https://github.com/oocx/tfplan2md/commit/c304731f3df8a65f9ddbdbd31ebefcaa11f6ab7a))
* **skills:** add VS Code source-based chat export format specification ([88beaf2](https://github.com/oocx/tfplan2md/commit/88beaf29238e3853ddc4a2f1177e22886049d083))
* **skills:** update create-pr-github skill to use stdin instead of --body-file ([2046e43](https://github.com/oocx/tfplan2md/commit/2046e43ff34f33921bf671a0622cc92ba8802869))

### Breaking Changes

* **scripts:** remove --body-file and --description options, enforce stdin-only ([4338625](https://github.com/oocx/tfplan2md/commit/4338625bf16197c1e8c34b58a7b354b7c6b170b9))

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


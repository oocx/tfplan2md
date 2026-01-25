# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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


# Changelog

All notable changes to this project will be documented in this file.

## [2.5.0] - 2025-08-08

### ♻️ Refactor

- Simplify GetActualFieldType method using pattern matching ([0e6969d](https://github.com/phmatray/FormCraft/commit/0e6969d74c114c0e243b240e6d3fbb45193a2a77))
- Simplify code by removing unnecessary null checks and using range operators ([38bf521](https://github.com/phmatray/FormCraft/commit/38bf521f875e3417b67e4c2fed08f9598715317d))

### ✅ Testing

- Ensure custom renderer bypasses standard renderers ([4ea1b0d](https://github.com/phmatray/FormCraft/commit/4ea1b0d427516bd72cb66d1d545b267ce3bf0cf4))
- Add unit tests for RenderField method to validate type detection ([27bf6da](https://github.com/phmatray/FormCraft/commit/27bf6da810e5c06b1329d266f9f06ccd26cf03a8))

### ✨ Features

- Build forms from model attributes ([ca3fa29](https://github.com/phmatray/FormCraft/commit/ca3fa29ff826a64792cf6ea2535b3bfbdc779b9d))
- Add attribute-based form generation with model annotations ([e442790](https://github.com/phmatray/FormCraft/commit/e442790567ec7bdad0e25161bfd76b556f82ad88))
- Update README for v2.5.0 with attribute-based form generation details ([6ed0891](https://github.com/phmatray/FormCraft/commit/6ed0891f56b600f3a70a251551812f949174410a))

### 🐛 Bug Fixes

- Improve UI framework adapter check for field renderer registration ([fe48776](https://github.com/phmatray/FormCraft/commit/fe48776ebcb80481f5aae4a14d490c33946b9191))

### 🔧 Miscellaneous Tasks

- Update dependency dotnet-sdk to v9.0.302 ([6ea5dac](https://github.com/phmatray/FormCraft/commit/6ea5dac45b99f4d106625185b0031a6739595f04))
- Update dependency xunit.runner.visualstudio to 3.1.3 ([cf618c7](https://github.com/phmatray/FormCraft/commit/cf618c788d1bdeae2a78bd874c06e66a5cd935b6))
- Update dependency mudblazor to 8.11.0 ([7474066](https://github.com/phmatray/FormCraft/commit/74740660f91a8d7b0872315d5a2bcb2b43bb0dc4))
- Update Microsoft.AspNetCore.Components packages to latest versions ([9077e08](https://github.com/phmatray/FormCraft/commit/9077e08bf5083adb93a19c541d211fdf2635ce5a))
- Update Microsoft.Extensions.DependencyInjection to version 9.0.8 ([9a43ec9](https://github.com/phmatray/FormCraft/commit/9a43ec9f0da774eb31b480834df4ea025557744a))
- Update dependency dotnet-sdk to v9.0.304 ([9799def](https://github.com/phmatray/FormCraft/commit/9799def8bf2ca8b05b49f6a7dfef2c851741440e))

## [2.4.0] - 2025-07-16

### ✨ Features

- Add .NET 8 support alongside .NET 9 ([09eaf3a](https://github.com/phmatray/FormCraft/commit/09eaf3a128fd1600871edb6669f7630b0ce0a021))

### 🐛 Bug Fixes

- Integrate changelog generation into NUKE build system ([1928638](https://github.com/phmatray/FormCraft/commit/1928638126c50807e609856e03dc892b079e7900))
- Skip changelog generation in CI builds ([3763081](https://github.com/phmatray/FormCraft/commit/3763081165459f31fefbf567eb5ed11c04aea938))

### 🔧 Miscellaneous Tasks

- Update test and web packages ([d9b0a1e](https://github.com/phmatray/FormCraft/commit/d9b0a1e30353b0e40d9ef2097f52835f24533403))
- Update GitHub Actions workflow and add generated changelogs ([4a3abb8](https://github.com/phmatray/FormCraft/commit/4a3abb82ee6699bc7a28847780dad4af0f3d3f6b))

## [2.3.0] - 2025-07-08

### ✨ Features

- Add dialog integration support for FormCraft forms ([0ef83f7](https://github.com/phmatray/FormCraft/commit/0ef83f7532943dd0a028355550dca00be0781c04))

### 🐛 Bug Fixes

- Enable TextArea rendering with Lines attribute support ([bddde10](https://github.com/phmatray/FormCraft/commit/bddde10bf8ca3811b3596b4237ac0b6efc5b292b))
- Resolve race condition in AsyncValidator cancellation token test ([b9fd83e](https://github.com/phmatray/FormCraft/commit/b9fd83e48cf7ef8bea715b2840c6e0bd10a7a89a))

### 🔧 Miscellaneous Tasks

- Update dotnet monorepo to 9.0.7 ([59be2db](https://github.com/phmatray/FormCraft/commit/59be2db8513aabb81e50338578ec270f5a92f79a))

## [2.2.0] - 2025-07-02

### ✨ Features

- Add convenience methods for form field creation ([60a3421](https://github.com/phmatray/FormCraft/commit/60a3421ab8577e702d0cdd401fcd7033bd666799))

### 🐛 Bug Fixes

- Resolve nullable reference type warnings and platform compatibility issues ([f1ebc0e](https://github.com/phmatray/FormCraft/commit/f1ebc0e7d62755205ef2a945082b62dc7bd779d1))
- Resolve MudBlazor component warnings in ForMudBlazor project ([4e6de54](https://github.com/phmatray/FormCraft/commit/4e6de546772148da2186d2dd099f1b9d1cc5b256))
- Remove class constraint from extension methods to support nullable types ([287a33c](https://github.com/phmatray/FormCraft/commit/287a33c74b6f3a573cb6f306e8b5b41ee6c448f7))

### 📚 Documentation

- Update package installation instructions ([948be2d](https://github.com/phmatray/FormCraft/commit/948be2dd9e382be75ba4ed0ca8b134dcc84217c3))

### 🔧 Miscellaneous Tasks

- Update MudBlazor to 8.9.0 and Markdig to 0.41.3 ([f62014e](https://github.com/phmatray/FormCraft/commit/f62014ec3b5e2c265f68858556f9108c0b9db12b))

## [2.1.0] - 2025-06-19

### Ci

- Fix ci workflow ([e128e94](https://github.com/phmatray/FormCraft/commit/e128e943ed595ba7ba9febc12d12ed7839dfa462))
- Use nuke build ([38a41b7](https://github.com/phmatray/FormCraft/commit/38a41b7bdf6b25630a7473f93ec25a653bf310d4))
- Fix gitcliff installation ([ffeeac6](https://github.com/phmatray/FormCraft/commit/ffeeac6c62a502a33c5360c9016423b9c91409d0))
- Use official gitcliff integration ([19a1f45](https://github.com/phmatray/FormCraft/commit/19a1f458a72daf5908637ffc5c55535ee88add4c))
- Use dotnet 9 with nuke ([dd2732d](https://github.com/phmatray/FormCraft/commit/dd2732dc1d438f2e84d33be8c02187d9633ef87c))
- Another try to fix the gitcliff integration ([b732ae5](https://github.com/phmatray/FormCraft/commit/b732ae5a7c38ebe2f75061eb1cfd32f59ef98f81))
- Another try to fix the gitcliff integration ([8b0a655](https://github.com/phmatray/FormCraft/commit/8b0a655eaca814ca1692ee60d960d8f44d7949ee))
- Simplify gitcliff integration ([5e59df0](https://github.com/phmatray/FormCraft/commit/5e59df01ca87b11f99d020b88a08bbceb0f52cc9))
- Update the build script to publish release ([344f716](https://github.com/phmatray/FormCraft/commit/344f716a98f791a486f07c66edf4943480c46b95))
- Fix version detection ([6f5e295](https://github.com/phmatray/FormCraft/commit/6f5e2956684d12e7bfac418ddb56c501ddf3605b))
- Fix duplicate projects ids ([b1c797d](https://github.com/phmatray/FormCraft/commit/b1c797d92c420b197f11e6a66a0884f5526d6d08))
- Move Nuke dependencies to CPM ([f6f4263](https://github.com/phmatray/FormCraft/commit/f6f4263b27af1fdb326dbd81daeccef3da6dc99c))

### ♻️ Refactor

- Move rendering logic to FieldDefinition ([57d8ef0](https://github.com/phmatray/FormCraft/commit/57d8ef0f00895ee10f95e4999eb617433e4b1a6f))
- Improve rendering ([5caabf3](https://github.com/phmatray/FormCraft/commit/5caabf3d1f5166e80dc50bc3eede2c1d15c18656))
- Fix some warnings ([2cfccba](https://github.com/phmatray/FormCraft/commit/2cfccba16586e7f55541ce59d12d179f39f84e7e))
- Fix warnings ([553c239](https://github.com/phmatray/FormCraft/commit/553c2394ad0e98bcb16bbcc9d84819b8222ae21f))
- Create FormCraft RCL project ([baaa310](https://github.com/phmatray/FormCraft/commit/baaa3100766258e3fb47a3b230e2cfa75f51876e))
- Create Abstractions folder ([537a9e7](https://github.com/phmatray/FormCraft/commit/537a9e7b8a37f072d69f229c5313e1c303aa3690))
- Simplify namespace structure ([b9ba918](https://github.com/phmatray/FormCraft/commit/b9ba918916a6b03a4cf3f2c04c0c438bb2e47125))
- Fix some warnings ([8ad1127](https://github.com/phmatray/FormCraft/commit/8ad1127e8a36025ceca93953c1d367e57d043fc1))
- Rename demo website ([70ca396](https://github.com/phmatray/FormCraft/commit/70ca39617d7c129993c4a6922b80bab57c5cd800))
- Add FieldGroups demo page ([2f79f3b](https://github.com/phmatray/FormCraft/commit/2f79f3b7f252b978785b163d2a39528d2c0b97fc))
- Improve the demo website ([22348f0](https://github.com/phmatray/FormCraft/commit/22348f0fc620bcd6d5037426e9eea6d50a197849))
- Fix some warnings ([ad485db](https://github.com/phmatray/FormCraft/commit/ad485dbcecfee2b3bf760b7447efec1bdcbc546a))
- Fix some warnings ([0ab6bc3](https://github.com/phmatray/FormCraft/commit/0ab6bc32b4430b5218e5f8f7a5d8ff19f9e8fd35))
- Convert blazor server to blazor wasm ([24bfea6](https://github.com/phmatray/FormCraft/commit/24bfea66024a45b600280811dcad0816e67ab5e0))
- Split razor components and code-behind ([405aedd](https://github.com/phmatray/FormCraft/commit/405aeddf292f491553fb79fc80aa32dbd740b1b8))
- Fix some warnings ([bf2b3d8](https://github.com/phmatray/FormCraft/commit/bf2b3d86d3acee0e6b9ff58ef25791f74ef3a47c))
- Create some components for the demo pages ([c74dcd1](https://github.com/phmatray/FormCraft/commit/c74dcd12f6f82ef0c93468c11ee0e1286dc70d9c))
- Improve AddField method for enforce indentation ([649384c](https://github.com/phmatray/FormCraft/commit/649384c4e34040b84e9a0b6762adf6d4c50a66a2))
- Complex method split ([d65f04c](https://github.com/phmatray/FormCraft/commit/d65f04c1c4a36e17b17ce1425721469453534974))
- Refine SimplifiedForm demo page ([89f7508](https://github.com/phmatray/FormCraft/commit/89f750820eb08012806264fbdac14dbfc1cba667))
- Remove FormCodeGeneratorService ([cccf304](https://github.com/phmatray/FormCraft/commit/cccf30475bd5112631247fea6b328538eea1219d))
- Simplify multi-step form validation ([ad5aa5e](https://github.com/phmatray/FormCraft/commit/ad5aa5e3950b4af3070a3d0598d3d4e1abf90e28))

### ✅ Testing

- Add unit tests ([5650136](https://github.com/phmatray/FormCraft/commit/5650136d13ee7148de898cefc05db182a32df623))
- Replace FluentAssertions by Shouldly ([52cb498](https://github.com/phmatray/FormCraft/commit/52cb4982e4ad2200124409dcf0a9f84f83c2dbe6))
- Improve code coverage ([5bc1513](https://github.com/phmatray/FormCraft/commit/5bc1513ce207ed195cd1141d5ff20feb9d7993ed))
- Add tests for renderers ([9e5d24a](https://github.com/phmatray/FormCraft/commit/9e5d24a1ea39bb28384ada16a014904861d2c82a))
- Refactor FormCraftComponentTests ([fadbec3](https://github.com/phmatray/FormCraft/commit/fadbec3f6549f3a1bad1259317bdd3f54a7d1150))

### ✨ Features

- Add validation ([1f02319](https://github.com/phmatray/FormCraft/commit/1f0231996d06c98011f7f8d1606eabf7fc9898e6))
- Improve dynamic form generation ([028b0bc](https://github.com/phmatray/FormCraft/commit/028b0bccdcd0d5d189163f61d59446835b44dd50))
- Add form layout system ([519a398](https://github.com/phmatray/FormCraft/commit/519a3986fe878444b2cb7247b54c6ffff5e0f627))
- Add documentation pages and refine demo layout ([fef0430](https://github.com/phmatray/FormCraft/commit/fef043032b6267e9d3a4ff8476342428d56e3960))
- Enable prism for documentation pages ([d42a939](https://github.com/phmatray/FormCraft/commit/d42a939b4eacc0175d6e78194b0a90a3912300f0))
- Add decimal and double field renderers ([f09166b](https://github.com/phmatray/FormCraft/commit/f09166b7c4bba7f5f2e1a854bc19f8979e49bf87))
- Add FieldGroupBuilder ([d2e8c52](https://github.com/phmatray/FormCraft/commit/d2e8c52cfa64ef3a53521437131d44c6fb32ddb7))
- Add custom renderers ([4da5046](https://github.com/phmatray/FormCraft/commit/4da50468429cbccbc2ed85e4a40691a89c1b879a))
- Add FileUpload renderer ([4142397](https://github.com/phmatray/FormCraft/commit/4142397c5d686e0ffe337e58687cd69a9bd232b2))
- Add fileupload error handling ([f4a4ad8](https://github.com/phmatray/FormCraft/commit/f4a4ad8269a5de2adabf2af6b5aeb537a16519db))
- Create onboarding welcome page ([abfaad1](https://github.com/phmatray/FormCraft/commit/abfaad197b130feae5b952c83d8d109b8850e16c))
- Extract mudblazor components to their own library ([c9d48e5](https://github.com/phmatray/FormCraft/commit/c9d48e5c73de5596e368417cbcc08eedf36bf305))
- Add SubmitButtonClass property ([0a5a9f4](https://github.com/phmatray/FormCraft/commit/0a5a9f48768ead4433eb18cd7303aea38eb9df9c))
- Add form slots (before and after) ([1aba4bd](https://github.com/phmatray/FormCraft/commit/1aba4bdab8cf8caec25bf7eefbc78367aebb805b))
- Add StepperForm example ([1655cf6](https://github.com/phmatray/FormCraft/commit/1655cf65073a1755b9a862bc24d552fd2f4188ec))
- Create demo for TabbedForm ([7c01d5b](https://github.com/phmatray/FormCraft/commit/7c01d5bc9c6e711a352fb3e35cb2771d3f2ecd3f))
- Add header right content ([877c0de](https://github.com/phmatray/FormCraft/commit/877c0de69b215b1d95198c93419e9d648d46f929))
- Add MudSlider custom renderer ([baaf20c](https://github.com/phmatray/FormCraft/commit/baaf20c67d645776e9152979c0ab8ca3171c2aba))
- Add CSRF, AuditLog, RateLimit and Encryption ([052011d](https://github.com/phmatray/FormCraft/commit/052011dbf843c1db12719708bfc6d46880fb9c54))
- Add FluentValidation integration support ([5107899](https://github.com/phmatray/FormCraft/commit/51078993f5589a11b4696866ed8ab0b534909c24))

### 🐛 Bug Fixes

- Make validation works ([7dd3b84](https://github.com/phmatray/FormCraft/commit/7dd3b84b459b1c6397d0ea1940c563a35f446763))
- Validation message concatenation ([30a0f81](https://github.com/phmatray/FormCraft/commit/30a0f81cff82d05a130b62fafea30f15aaadc9f5))
- Reduce icon size ([aadb0bf](https://github.com/phmatray/FormCraft/commit/aadb0bfe56dd33e0127457e11998c464c4b62ee9))
- Broken link on github pages ([29af90d](https://github.com/phmatray/FormCraft/commit/29af90de1f11f8a566e8ea39618e05e88aa07988))
- Broken links ([83f4c09](https://github.com/phmatray/FormCraft/commit/83f4c09c68f0307666d7efaea01e8c531966395d))
- Urls in demo app ([85d5a9b](https://github.com/phmatray/FormCraft/commit/85d5a9b3571d405b31fd1126eca1406f9bf12d6f))
- Broken links in productions and some minor visual adjustments ([f1c4c35](https://github.com/phmatray/FormCraft/commit/f1c4c35e79fb13b25077e95d9d1678824e9753a1))
- Use required behavior from FluentValidation ([9458e12](https://github.com/phmatray/FormCraft/commit/9458e12ed6171ed0f35359282fa9b82c6d807b79))
- Validation messages are concatened without spacing ([7c5ee17](https://github.com/phmatray/FormCraft/commit/7c5ee17ea8bd7f5c96059ef5fa966ac0953a77cc))
- Update code examples ([37d71bb](https://github.com/phmatray/FormCraft/commit/37d71bb32601625be38663f14e9e414a47ab3590))
- Select field renders as a text field ([b41a138](https://github.com/phmatray/FormCraft/commit/b41a138d1e8ad92ed2256336e493bdda2b2c1ec2))
- Field groups rendering ([94bf53e](https://github.com/phmatray/FormCraft/commit/94bf53e0ce52ed41a05fcebcde7d813dc214aeaf))
- Custom renderers does not render correctly ([f9c7a23](https://github.com/phmatray/FormCraft/commit/f9c7a2375414249f4d0a9003283b3a033b99ca06))
- Use fluent validation in StepperForm ([fb50343](https://github.com/phmatray/FormCraft/commit/fb503435a4ee8649c2a8588e5683c22f4522c005))
- Help text positioning ([b069e1a](https://github.com/phmatray/FormCraft/commit/b069e1ac33ab2ff4932b4d9f28bc9f614899aea9))
- Improve file upload rendering ([ae122c5](https://github.com/phmatray/FormCraft/commit/ae122c537cd86e95c666cf8427e94f29424031e1))
- Code example on home page ([654e456](https://github.com/phmatray/FormCraft/commit/654e4562bdd0015b7e0005c6ca3d27c3272f7ee4))

### 📚 Documentation

- Update readme ([115988e](https://github.com/phmatray/FormCraft/commit/115988e3159c0a95d88ce06a0038ff1733724f7d))
- Update readme and create solution folders ([37dcb59](https://github.com/phmatray/FormCraft/commit/37dcb59212449531e2cdf1951877d747a0830b36))
- Improve the documentation ([3fdd591](https://github.com/phmatray/FormCraft/commit/3fdd591866f4dd90a1094d122d64c611218174cc))

### 🔧 Miscellaneous Tasks

- Remove pages not related to FormCraft ([1848bac](https://github.com/phmatray/FormCraft/commit/1848bac46fafc435a639bb3b1416882b003ac73a))
- Prepare NuGet package ([3b8b33e](https://github.com/phmatray/FormCraft/commit/3b8b33e936abb8c2a0daf052447fc49a70868a81))
- Fix icon format ([a24700b](https://github.com/phmatray/FormCraft/commit/a24700be32a1476fe83f564a4fbfd61a83cb44e6))
- Add changelog generation ([012a0a7](https://github.com/phmatray/FormCraft/commit/012a0a7e8b52e96871085a6c61bb9faf3f86cafd))
- Generate changelog before a commit with the setup-hooks script ([43990dd](https://github.com/phmatray/FormCraft/commit/43990ddc108b68737b62b05d43811b6aee4fa07d))
- Improve changelog generation ([a7faaf7](https://github.com/phmatray/FormCraft/commit/a7faaf734f6b29daf83817f27bacaca0a1fadd1b))
- Create a github action workflow to handle changelog ([5beeea7](https://github.com/phmatray/FormCraft/commit/5beeea7c93de7709a0a487e01d8b120242569ad1))
- Update changelog [skip ci] ([dcb5599](https://github.com/phmatray/FormCraft/commit/dcb55996ebc421d3e5c8443ebb957abfde65f170))
- Update changelog [skip ci] ([e662e0e](https://github.com/phmatray/FormCraft/commit/e662e0e896559c8a3e273c9aee92798fe533c82e))
- Update changelog [skip ci] ([9757738](https://github.com/phmatray/FormCraft/commit/97577383fd02244eae40a5c3486fcc96d478753b))
- Update orhun/git-cliff-action action to v4 ([b07c869](https://github.com/phmatray/FormCraft/commit/b07c86963d97f2e8a3a712993825c28df3888481))
- Update dependency octokit to v14 ([411b025](https://github.com/phmatray/FormCraft/commit/411b025afe5f2955e6317e40ccdda378db0164fb))
- Update dependency dotnet-sdk to v9.0.300 ([e3c6662](https://github.com/phmatray/FormCraft/commit/e3c6662f3f2c843f9e91c42b90f2aa268f438f29))
- Add CLAUDE.md file ([8ea08e6](https://github.com/phmatray/FormCraft/commit/8ea08e6af0686bd12529852cf6adf7e805899066))
- Prepare v2.0.0 ([64adc13](https://github.com/phmatray/FormCraft/commit/64adc13d40e7cdba504a1b2b11646435a526f3fe))
- Update dotnet monorepo to 9.0.6 ([ab0c61d](https://github.com/phmatray/FormCraft/commit/ab0c61d0f7025456b428cf2a6be2f928404aeb0c))
- Update dependency coverlet.collector to 6.0.4 ([7322a73](https://github.com/phmatray/FormCraft/commit/7322a73524e1f00e33e692f705cbabeaaab34d04))
- Update dependency xunit to 2.9.3 ([0b6ece4](https://github.com/phmatray/FormCraft/commit/0b6ece49b1596c781f3ffadc0db931a95fec265f))
- Update dependency dotnet-sdk to v9.0.301 ([74652b6](https://github.com/phmatray/FormCraft/commit/74652b63ab521baa38cdc5bcbc735112e60a73e6))
- Update dependency gitversion.msbuild to 6.3.0 ([e2c8e25](https://github.com/phmatray/FormCraft/commit/e2c8e257ca48d39f9d1ef584d88893226f590b17))
- Update dependency bunit to 1.40.0 ([a91c2ea](https://github.com/phmatray/FormCraft/commit/a91c2eaea88a81419022b96dd5853c4c35b04110))
- Update dependency mudblazor to 8.8.0 ([ef9130b](https://github.com/phmatray/FormCraft/commit/ef9130bbac5aba19507a84603bb1e8f9770fc149))

<!-- generated by git-cliff -->

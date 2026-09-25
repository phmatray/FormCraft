# Changelog

All notable changes to this project will be documented in this file.

<!--
  This file is generated and owned by release-please. Do not hand-edit it, and do not add an
  "Unreleased" section: your PR title is the changelog entry, and release-please writes the next
  release's section here when it opens the release PR.
-->

## [4.0.0](https://github.com/phmatray/FormCraft/compare/v3.1.0...v4.0.0) (2026-09-24)


### ⚠ BREAKING CHANGES

* **lov:** `ILovConfiguration<TItem, TValue>` gained a `GetByKeyProvider` property. A custom class implementing that interface must add it (returning `null` keeps the previous behaviour). Configurations built through `LovBuilder` are unaffected.
* **lov:** `MudBlazorLovFieldComponent` and `FluentUILovFieldComponent` gained a `TKey` type parameter (`<TModel, TValue, TKey, TItem>`), with `TValue` now the field's bound type. Code that renders these components directly by their old three-argument generic shape must add the key type; fields configured through `.AsLov(...)` / `.AsMultiSelectLov(...)` are unaffected.
* **lov:** `AsMultiSelectLov<TModel, TValue, TItem>`'s `configure` parameter changed from `Action<LovBuilder<TModel, IEnumerable<TValue>, TItem>>` to `Action<LovBuilder<TModel, TValue, TItem>>`, so `WithKey` now takes the scalar item key. Callers with an explicitly-typed configure lambda must update the type; inferred lambdas are unaffected.
* **core:** `IFormConfiguration<TModel>.ShowValidationSummary` now defaults to `false` (it was `true`, but had no effect). A form now renders a validation summary only after opting in with `.ShowValidationSummary()`. `IFormConfiguration<TModel>.ShowRequiredIndicator`, `.RequiredIndicator`, `FormBuilder<TModel>.ShowRequiredIndicator(bool, string)` and `FormLayout.Horizontal` are `[Obsolete]`. A consumer that builds with warnings as errors gets `CS0618` until it moves to `.WithNativeRequired()` / `FormLayout.Grid` or suppresses the warning locally.
* **core:** a nested field's `FieldName` is now its full dotted path (`Shipping.Value`), not its last member name (`Value`). This is visible through `GetRequiredFields`, `OnFieldChanged`, validation message keys (`FieldIdentifier`), `DependsOn`/group membership keys, and DOM ids built from the field name. Single-level fields are unchanged.
* **security:** `EncryptField` now fails closed. An expression that does not resolve to a string property, or a path that cannot be resolved, throws `ArgumentException` at configuration time instead of being silently skipped. Nested members (`x => x.Address.City`) are now actually encrypted and stored under their full dotted path.
* **core:** `FormDiagnosticScope` and `DiagnosticLog` moved from `FormCraft.ForMudBlazor` to `FormCraft.Diagnostics` (core). Code that references them by the old namespace must update its `using` directive. No forwarding shim is provided.
* **core:** move the UI-agnostic adapter machinery into core ([#279](https://github.com/phmatray/FormCraft/issues/279)) (#289)
* **core:** expose Validators as IReadOnlyList and require AddValidator ([#245](https://github.com/phmatray/FormCraft/issues/245))
* **lov:** WithDisplay(Expression<Func<TItem,string>>) is removed. Pass a lambda directly, or expr.Compile() if you hold an Expression.

### Features

* **core:** localize default validation messages ([#354](https://github.com/phmatray/FormCraft/issues/354)) ([#361](https://github.com/phmatray/FormCraft/issues/361)) ([6a9a2dc](https://github.com/phmatray/FormCraft/commit/6a9a2dc0637bc508d6abe3bd25902a4ff4545dc6))
* **demo:** redesign the demo site around code beside the live form ([#390](https://github.com/phmatray/FormCraft/issues/390)) ([60766d3](https://github.com/phmatray/FormCraft/commit/60766d309ca56c2f2a1fc58405b4f9fa1aa276e6))
* **demo:** redesign the demo site around the code-to-form binding ([#268](https://github.com/phmatray/FormCraft/issues/268)) ([679e0a2](https://github.com/phmatray/FormCraft/commit/679e0a2afb06111ec9ef4f222ec1cc1dd3851f8d))
* **fluentui:** add a FormCraft.ForFluentUI adapter for Fluent UI Blazor v5 ([#260](https://github.com/phmatray/FormCraft/issues/260)) ([#261](https://github.com/phmatray/FormCraft/issues/261)) ([713e358](https://github.com/phmatray/FormCraft/commit/713e358d10c4900190f6f4caf3d4b922b3126421))
* **fluentui:** bring the Fluent UI adapter to parity with MudBlazor ([#278](https://github.com/phmatray/FormCraft/issues/278)) ([#291](https://github.com/phmatray/FormCraft/issues/291)) ([6f6acaf](https://github.com/phmatray/FormCraft/commit/6f6acafdfbae0b4d8c0e7050918006d5534627c2))
* **mudblazor:** add a typed builder method for the native required opt-in ([#204](https://github.com/phmatray/FormCraft/issues/204)) ([#236](https://github.com/phmatray/FormCraft/issues/236)) ([4210be4](https://github.com/phmatray/FormCraft/commit/4210be4a260294d166fd76e4af2b013a29edc08e))
* **mudblazor:** add a typed WithMask builder for text field masks ([#265](https://github.com/phmatray/FormCraft/issues/265)) ([#273](https://github.com/phmatray/FormCraft/issues/273)) ([61e6445](https://github.com/phmatray/FormCraft/commit/61e6445621c727098a3c7d904e6c50a232b98ad8))
* **mudblazor:** announce required fields without the HTML5 required attribute ([#263](https://github.com/phmatray/FormCraft/issues/263)) ([#353](https://github.com/phmatray/FormCraft/issues/353)) ([3341705](https://github.com/phmatray/FormCraft/commit/33417058cd97838a090841696043f59f70442abb))
* **mudblazor:** announce required file-upload fields to assistive technology ([#262](https://github.com/phmatray/FormCraft/issues/262)) ([#270](https://github.com/phmatray/FormCraft/issues/270)) ([ba02aad](https://github.com/phmatray/FormCraft/commit/ba02aad2fd3e5661234de86f2b6b9edb804d0e75))
* **mudblazor:** give numeric adornments a typed click handler ([#215](https://github.com/phmatray/FormCraft/issues/215)) ([#244](https://github.com/phmatray/FormCraft/issues/244)) ([8a03598](https://github.com/phmatray/FormCraft/commit/8a0359809827bcb2613e3bbe3938e4aaf68257a3))
* **mudblazor:** make ShrinkLabel configurable per field and per form ([#177](https://github.com/phmatray/FormCraft/issues/177)) ([#178](https://github.com/phmatray/FormCraft/issues/178)) ([7a7dc30](https://github.com/phmatray/FormCraft/commit/7a7dc30d6822c55e6bedebef32826f94e0cd1f88))
* **mudblazor:** warn when a mask blanks a stored value ([#266](https://github.com/phmatray/FormCraft/issues/266)) ([#274](https://github.com/phmatray/FormCraft/issues/274)) ([1ea5425](https://github.com/phmatray/FormCraft/commit/1ea5425ef712cc13dc7992dc9e0ad6563fe1c797))
* **mudblazor:** warn when ShrinkLabel=false cannot be honoured ([#181](https://github.com/phmatray/FormCraft/issues/181)) ([#183](https://github.com/phmatray/FormCraft/issues/183)) ([b22d05d](https://github.com/phmatray/FormCraft/commit/b22d05d8f2dd19ee9fc79e02fba6f300b8cef465))
* **mudblazor:** warn when the password toggle displaces a configured adornment ([#219](https://github.com/phmatray/FormCraft/issues/219)) ([#243](https://github.com/phmatray/FormCraft/issues/243)) ([eccbf2d](https://github.com/phmatray/FormCraft/commit/eccbf2d67970d6fe5db26607966e5acb12dfe376))


### Bug Fixes

* **adapters:** custom template no longer renders nothing for a non-property field ([#330](https://github.com/phmatray/FormCraft/issues/330)) ([#387](https://github.com/phmatray/FormCraft/issues/387)) ([0d378af](https://github.com/phmatray/FormCraft/commit/0d378aff238699be9d0e2d05ab39c780fd228715))
* **adapters:** make Add/Remove inert and diagnosable on an unreadable collection binding ([#433](https://github.com/phmatray/FormCraft/issues/433)) ([#439](https://github.com/phmatray/FormCraft/issues/439)) ([a8adaf5](https://github.com/phmatray/FormCraft/commit/a8adaf568af8ad175c627c3e4fddbc13f8f7a8d2))
* **adapters:** stop a null-intermediate collection binding from crashing render ([#419](https://github.com/phmatray/FormCraft/issues/419)) ([#427](https://github.com/phmatray/FormCraft/issues/427)) ([1bd2dc3](https://github.com/phmatray/FormCraft/commit/1bd2dc319bff5a937376231234f4472cb3154e69))
* **adapters:** warn once on an unwritable nested collection item edit ([#450](https://github.com/phmatray/FormCraft/issues/450)) ([#451](https://github.com/phmatray/FormCraft/issues/451)) ([e774a46](https://github.com/phmatray/FormCraft/commit/e774a468dfe75d2678c3ffb8dfb02cab4d5064b9))
* **adapters:** write back through the field's compiled expression for custom templates ([#413](https://github.com/phmatray/FormCraft/issues/413)) ([20d80c3](https://github.com/phmatray/FormCraft/commit/20d80c3e5a733105d13b218e2da8277a8a8ac909))
* **build:** stop Clean deleting the build output it is running from ([#310](https://github.com/phmatray/FormCraft/issues/310)) ([#368](https://github.com/phmatray/FormCraft/issues/368)) ([7e3f9e7](https://github.com/phmatray/FormCraft/commit/7e3f9e7c22949618a9c4d57344f53b993127820f))
* **core:** build CollectionSetter from the full nested member chain ([#420](https://github.com/phmatray/FormCraft/issues/420)) ([#424](https://github.com/phmatray/FormCraft/issues/424)) ([9bac0ce](https://github.com/phmatray/FormCraft/commit/9bac0cec0835a25264db4d1550846b967f8e9ce6))
* **core:** evaluate DisabledCondition on render, mirroring VisibleWhen ([#476](https://github.com/phmatray/FormCraft/issues/476)) ([#479](https://github.com/phmatray/FormCraft/issues/479)) ([e51c9ef](https://github.com/phmatray/FormCraft/commit/e51c9ef8c92198e366654bb921453c954278fde0))
* **core:** honour or retire the inert form-level settings ([#457](https://github.com/phmatray/FormCraft/issues/457)) ([8d75085](https://github.com/phmatray/FormCraft/commit/8d7508573a1305a6373ff605dd83cb1b413bf904))
* **core:** keep collection validation messages consistent with the rendered form ([#342](https://github.com/phmatray/FormCraft/issues/342)) ([#379](https://github.com/phmatray/FormCraft/issues/379)) ([69b56c4](https://github.com/phmatray/FormCraft/commit/69b56c474627335a9bdbeb226b02ec599a9cbec3))
* **core:** qualify collection field identity by its full nested path ([#428](https://github.com/phmatray/FormCraft/issues/428)) ([#434](https://github.com/phmatray/FormCraft/issues/434)) ([e87756f](https://github.com/phmatray/FormCraft/commit/e87756f2ffc9609a3f91eafebc39879ce522c5a6))
* **core:** qualify scalar field identity by its full nested path ([#437](https://github.com/phmatray/FormCraft/issues/437)) ([37cb742](https://github.com/phmatray/FormCraft/commit/37cb7422e5f11b535db3e69045b54ba61f2a0345))
* **core:** resolve DisabledCondition in the core stub field renderers ([#483](https://github.com/phmatray/FormCraft/issues/483)) ([#489](https://github.com/phmatray/FormCraft/issues/489)) ([f9df972](https://github.com/phmatray/FormCraft/commit/f9df972d1af68ffca8761173c648d42d3c5aa781))
* **core:** share one security pipeline across both adapters ([#321](https://github.com/phmatray/FormCraft/issues/321)) ([#402](https://github.com/phmatray/FormCraft/issues/402)) ([e1488a1](https://github.com/phmatray/FormCraft/commit/e1488a107d0b03fb7a0db9153f3de49fb61fc2ed))
* **core:** stamp identifiers a validation pass writes fresh ([#445](https://github.com/phmatray/FormCraft/issues/445)) ([#446](https://github.com/phmatray/FormCraft/issues/446)) ([a7e74ec](https://github.com/phmatray/FormCraft/commit/a7e74ecb924dc3bc63d8ada173f4d800fc916246))
* **core:** stamp valid collection cells and skip hidden fields on write ([#447](https://github.com/phmatray/FormCraft/issues/447)) ([#452](https://github.com/phmatray/FormCraft/issues/452)) ([430c664](https://github.com/phmatray/FormCraft/commit/430c664b8e94b4ab3678c0fbb7475432bf5591c4))
* **core:** stop an unreadable nested binding from crashing render or validation ([#397](https://github.com/phmatray/FormCraft/issues/397)) ([#403](https://github.com/phmatray/FormCraft/issues/403)) ([45d7c0d](https://github.com/phmatray/FormCraft/commit/45d7c0d7dfb1028e23fe410bbf0929f840d69bf8))
* **core:** stop an unreadable nested collection binding from crashing validation ([#414](https://github.com/phmatray/FormCraft/issues/414)) ([f52faa0](https://github.com/phmatray/FormCraft/commit/f52faa079ebe4e5070015996e2903db4f97c76a6))
* **core:** stop FieldValueGetterCache swallowing genuine accessor faults and cancellation ([#425](https://github.com/phmatray/FormCraft/issues/425)) ([#438](https://github.com/phmatray/FormCraft/issues/438)) ([6f6aaee](https://github.com/phmatray/FormCraft/commit/6f6aaee1ac9859650ad9a666afff9399a1658fd9))
* **core:** validate each collection item field once per pass ([#329](https://github.com/phmatray/FormCraft/issues/329)) ([#331](https://github.com/phmatray/FormCraft/issues/331)) ([65944e5](https://github.com/phmatray/FormCraft/commit/65944e5adf37ee36d3e3705046a9ecf1391905ee))
* **demo:** allow pinch-zoom in the Fluent UI demo ([#341](https://github.com/phmatray/FormCraft/issues/341)) ([#360](https://github.com/phmatray/FormCraft/issues/360)) ([af2900e](https://github.com/phmatray/FormCraft/commit/af2900e809b07c8a747f3b6cd36b52a30be47045))
* **demo:** correct ARIA state, disposal and fade-in regressions ([#285](https://github.com/phmatray/FormCraft/issues/285)) ([#295](https://github.com/phmatray/FormCraft/issues/295)) ([3dd997c](https://github.com/phmatray/FormCraft/commit/3dd997c02455c2bd8b0b292424e45e35e3b35ba4))
* **demo:** guard every timed re-render against component disposal ([#315](https://github.com/phmatray/FormCraft/issues/315)) ([#323](https://github.com/phmatray/FormCraft/issues/323)) ([8557ce3](https://github.com/phmatray/FormCraft/commit/8557ce38e6ea65397f7c1696b10dc9acc14f69e2))
* **demo:** make every demo page readable and fill the gallery ([#405](https://github.com/phmatray/FormCraft/issues/405)) ([e5260fc](https://github.com/phmatray/FormCraft/commit/e5260fc29a2ce6b801323c51d142cff9bf64c3b3))
* **demo:** stop nesting cards in the form panel and give wide forms room ([#411](https://github.com/phmatray/FormCraft/issues/411)) ([36e18c3](https://github.com/phmatray/FormCraft/commit/36e18c3aba99cc6d457ed717aa4e8bb25d336f98))
* **fluentui:** bind numeric bounds explicitly so a dropped bound is dropped ([#348](https://github.com/phmatray/FormCraft/issues/348)) ([#382](https://github.com/phmatray/FormCraft/issues/382)) ([d4952cd](https://github.com/phmatray/FormCraft/commit/d4952cd1345cb27fbc4eb6e22409c30ccfc9a1cd))
* **fluentui:** clear TooManyFilesError on a multiple-file-upload config swap ([#416](https://github.com/phmatray/FormCraft/issues/416)) ([#421](https://github.com/phmatray/FormCraft/issues/421)) ([bd46189](https://github.com/phmatray/FormCraft/commit/bd46189d1fc09e83cd0ac44b6d793bfb53397fda))
* **fluentui:** focus the collection controls' own buttons, not their wrapper spans ([#383](https://github.com/phmatray/FormCraft/issues/383)) ([#399](https://github.com/phmatray/FormCraft/issues/399)) ([f073d88](https://github.com/phmatray/FormCraft/commit/f073d88e193c683cc228be09ffe6cf8c4eec06e4))
* **fluentui:** keep keyboard focus after collection controls unmount ([#337](https://github.com/phmatray/FormCraft/issues/337)) ([#377](https://github.com/phmatray/FormCraft/issues/377)) ([59ea837](https://github.com/phmatray/FormCraft/commit/59ea837b343e19a2811328dd4ef679d43d2cb0af))
* **fluentui:** key collection rows on a weak per-item token ([#401](https://github.com/phmatray/FormCraft/issues/401)) ([#410](https://github.com/phmatray/FormCraft/issues/410)) ([c73d088](https://github.com/phmatray/FormCraft/commit/c73d088189b174e914ef0b96e7d4108634571a55))
* **fluentui:** re-read a field's configuration when the rendered field changes ([#335](https://github.com/phmatray/FormCraft/issues/335)) ([#336](https://github.com/phmatray/FormCraft/issues/336)) ([d3af1ff](https://github.com/phmatray/FormCraft/commit/d3af1ff22017680b7da9ea44d9f772fbcb4be2df))
* **lov:** bind AsMultiSelectLov's WithKey to the scalar per-item key ([#467](https://github.com/phmatray/FormCraft/issues/467)) ([70fa17c](https://github.com/phmatray/FormCraft/commit/70fa17c5db21a26fcbe4f06e6b5ef0eef50b3c24))
* **lov:** let WithDisplay and WithKey accept a plain lambda ([#180](https://github.com/phmatray/FormCraft/issues/180)) ([#182](https://github.com/phmatray/FormCraft/issues/182)) ([fa88097](https://github.com/phmatray/FormCraft/commit/fa8809742fa7aa365fc319dd725783e3d1e1f1b0))
* **lov:** resolve multi-select LOV component TValue from the field's bound type ([#480](https://github.com/phmatray/FormCraft/issues/480)) ([0780e08](https://github.com/phmatray/FormCraft/commit/0780e088a90de05b6f054a5aea66b0ea683a443a))
* **lov:** WithDataSource(collection) honors search/sort/context filters ([#472](https://github.com/phmatray/FormCraft/issues/472)) ([a8f0561](https://github.com/phmatray/FormCraft/commit/a8f05613c3f93fa10cc4357bc460bf8df3f6ebe4))
* **mudblazor:** announce required fields to assistive technology ([#199](https://github.com/phmatray/FormCraft/issues/199)) ([#254](https://github.com/phmatray/FormCraft/issues/254)) ([e8e3586](https://github.com/phmatray/FormCraft/commit/e8e3586f2051100d78bce85d722d4e7ed3f2eced))
* **mudblazor:** bind a configured mask on both render paths ([#211](https://github.com/phmatray/FormCraft/issues/211)) ([#253](https://github.com/phmatray/FormCraft/issues/253)) ([8484dba](https://github.com/phmatray/FormCraft/commit/8484dba3006faacbb5aad629d5edbc6d841c2753))
* **mudblazor:** bind DisabledWhen to MudBlazor file upload components ([#487](https://github.com/phmatray/FormCraft/issues/487)) ([0343e59](https://github.com/phmatray/FormCraft/commit/0343e5940ca6826782baac95d3e0dc067b8acd85))
* **mudblazor:** bind factory masks whose pattern is computed lazily ([#317](https://github.com/phmatray/FormCraft/issues/317)) ([#366](https://github.com/phmatray/FormCraft/issues/366)) ([75ebb81](https://github.com/phmatray/FormCraft/commit/75ebb81d25c341b2b265d08d3ac3621a3e41199a))
* **mudblazor:** clear a multiple-file upload to an empty list, notifying once ([#319](https://github.com/phmatray/FormCraft/issues/319)) ([#376](https://github.com/phmatray/FormCraft/issues/376)) ([0828678](https://github.com/phmatray/FormCraft/commit/08286783fc44a1c8a31c3223c5aa1c859fd48b99))
* **mudblazor:** closing one LOV chip removes only that item instead of clearing the whole selection ([#475](https://github.com/phmatray/FormCraft/issues/475)) ([0780e08](https://github.com/phmatray/FormCraft/commit/0780e088a90de05b6f054a5aea66b0ea683a443a))
* **mudblazor:** dedupe field diagnostics across component re-mounts ([#304](https://github.com/phmatray/FormCraft/issues/304)) ([#309](https://github.com/phmatray/FormCraft/issues/309)) ([44d46ff](https://github.com/phmatray/FormCraft/commit/44d46ff9e8b67795ba119d4d48d0a305767dd3d2))
* **mudblazor:** drop the write-only _valueLookup cache from the autocomplete component ([#404](https://github.com/phmatray/FormCraft/issues/404)) ([#415](https://github.com/phmatray/FormCraft/issues/415)) ([769ec49](https://github.com/phmatray/FormCraft/commit/769ec4985f9581edb3097296026cbce7b7c08c46))
* **mudblazor:** focus Browse after clearing a file upload ([#281](https://github.com/phmatray/FormCraft/issues/281)) ([#292](https://github.com/phmatray/FormCraft/issues/292)) ([da7d1dc](https://github.com/phmatray/FormCraft/commit/da7d1dc7c9393e6164b98d7a7526db8d2a246823))
* **mudblazor:** forward a configured adornment to date item fields ([#217](https://github.com/phmatray/FormCraft/issues/217)) ([#248](https://github.com/phmatray/FormCraft/issues/248)) ([b4b649f](https://github.com/phmatray/FormCraft/commit/b4b649fc9e652bc6072691a6ddb5b54e043d9a6a))
* **mudblazor:** hide the upload's native file input and keep drops working ([#456](https://github.com/phmatray/FormCraft/issues/456)) ([6d0de99](https://github.com/phmatray/FormCraft/commit/6d0de99152b3c6db3d262e809768addf378e35fc))
* **mudblazor:** honour a configured Culture on numeric item fields ([#218](https://github.com/phmatray/FormCraft/issues/218)) ([#247](https://github.com/phmatray/FormCraft/issues/247)) ([e31a7e6](https://github.com/phmatray/FormCraft/commit/e31a7e62c31fe90ba14decbe3ef58b28872e9cb4))
* **mudblazor:** honour Format and ShowSpinButtons on numeric fields ([#208](https://github.com/phmatray/FormCraft/issues/208)) ([#240](https://github.com/phmatray/FormCraft/issues/240)) ([cefa8a8](https://github.com/phmatray/FormCraft/commit/cefa8a82fa8458d1ec39b07799f2eabc25d54620))
* **mudblazor:** invoke the adornment click handler passed to WithAdornment ([#192](https://github.com/phmatray/FormCraft/issues/192)) ([#194](https://github.com/phmatray/FormCraft/issues/194)) ([92add18](https://github.com/phmatray/FormCraft/commit/92add18dd0eeef327717031395e3c08d57c0288b))
* **mudblazor:** judge the ShrinkLabel diagnostic on the rendered adornment ([#212](https://github.com/phmatray/FormCraft/issues/212)) ([#241](https://github.com/phmatray/FormCraft/issues/241)) ([36dc2aa](https://github.com/phmatray/FormCraft/commit/36dc2aa9544b42142d84f41cffb7a8bc2f596ef2))
* **mudblazor:** keep a password field masked when it also configures multiple lines ([#207](https://github.com/phmatray/FormCraft/issues/207)) ([#232](https://github.com/phmatray/FormCraft/issues/232)) ([aea3e40](https://github.com/phmatray/FormCraft/commit/aea3e408b5725853ddf82f88173524889dff914a))
* **mudblazor:** key collection rows on a per-item weak token ([#334](https://github.com/phmatray/FormCraft/issues/334)) ([#400](https://github.com/phmatray/FormCraft/issues/400)) ([705a48b](https://github.com/phmatray/FormCraft/commit/705a48bc8d3da8d1172ad1cc24f0d60e6b5d1ccd))
* **mudblazor:** map number, date and time to their input types ([#210](https://github.com/phmatray/FormCraft/issues/210)) ([#239](https://github.com/phmatray/FormCraft/issues/239)) ([6dcab2d](https://github.com/phmatray/FormCraft/commit/6dcab2dc6d661b5d71b7e4d4fec08eec9c76ab7a))
* **mudblazor:** mask password fields inside collection item forms ([#189](https://github.com/phmatray/FormCraft/issues/189)) ([#196](https://github.com/phmatray/FormCraft/issues/196)) ([421eee3](https://github.com/phmatray/FormCraft/commit/421eee3f8e72658b31bb52974e338921c87988a0))
* **mudblazor:** move focus deliberately after a self-unmounting control ([#318](https://github.com/phmatray/FormCraft/issues/318)) ([#324](https://github.com/phmatray/FormCraft/issues/324)) ([33102ff](https://github.com/phmatray/FormCraft/commit/33102ffff4cf78598ee8c04c1695f03f306d6646))
* **mudblazor:** qualify the ShrinkLabel diagnostic key so scopes cannot collide ([#242](https://github.com/phmatray/FormCraft/issues/242)) ([2d591c3](https://github.com/phmatray/FormCraft/commit/2d591c3d72c394e7d8b21477b6eb1db8a6af9c8e))
* **mudblazor:** re-read a field's configuration when the rendered field changes ([#298](https://github.com/phmatray/FormCraft/issues/298)) ([#308](https://github.com/phmatray/FormCraft/issues/308)) ([947a3a5](https://github.com/phmatray/FormCraft/commit/947a3a527f45508806143cb44fa31e26c1324dbf))
* **mudblazor:** render a handler-less adornment as a plain icon, not a button ([#216](https://github.com/phmatray/FormCraft/issues/216)) ([#224](https://github.com/phmatray/FormCraft/issues/224)) ([789acae](https://github.com/phmatray/FormCraft/commit/789acae6bc8de806f8297086c033a62be45909aa))
* **mudblazor:** render adornments on collection item fields ([#184](https://github.com/phmatray/FormCraft/issues/184)) ([#188](https://github.com/phmatray/FormCraft/issues/188)) ([89f0dbc](https://github.com/phmatray/FormCraft/commit/89f0dbc90d0d381b4949b9189b6cbc34a7b4e973))
* **mudblazor:** render adornments on numeric fields ([#191](https://github.com/phmatray/FormCraft/issues/191)) ([#195](https://github.com/phmatray/FormCraft/issues/195)) ([7b3743f](https://github.com/phmatray/FormCraft/commit/7b3743f52fafc23611d2fa61dc3ffa1b4e9aeb24))
* **mudblazor:** render each uploaded file once ([#338](https://github.com/phmatray/FormCraft/issues/338)) ([#362](https://github.com/phmatray/FormCraft/issues/362)) ([e79abe4](https://github.com/phmatray/FormCraft/commit/e79abe47a6bbff64544e977533cf46896e5d0606))
* **mudblazor:** render long, float, short and byte collection item fields ([#209](https://github.com/phmatray/FormCraft/issues/209)) ([#246](https://github.com/phmatray/FormCraft/issues/246)) ([0c8be25](https://github.com/phmatray/FormCraft/commit/0c8be25b12239e58063f547625368c39c9f859d1))
* **mudblazor:** render novalidate on the form instead of applying it by script ([#206](https://github.com/phmatray/FormCraft/issues/206)) ([#235](https://github.com/phmatray/FormCraft/issues/235)) ([c9b5047](https://github.com/phmatray/FormCraft/commit/c9b5047c1e1f6c72ccb3ba97808049a11979bca5))
* **mudblazor:** report post-init and partially-discarded masked values ([#283](https://github.com/phmatray/FormCraft/issues/283)) ([#293](https://github.com/phmatray/FormCraft/issues/293)) ([af5c605](https://github.com/phmatray/FormCraft/commit/af5c605717dbc9fadb1da2ca76ed32debf53bef5))
* **mudblazor:** stop emitting the HTML5 Required attribute on collection item fields ([#190](https://github.com/phmatray/FormCraft/issues/190)) ([#193](https://github.com/phmatray/FormCraft/issues/193)) ([3bb3bde](https://github.com/phmatray/FormCraft/commit/3bb3bdecd6fd78b2253bb1c95b46f75d0958db20))
* sanitize FieldName for CSS-safe DOM ids on nested fields ([#449](https://github.com/phmatray/FormCraft/issues/449)) ([#453](https://github.com/phmatray/FormCraft/issues/453)) ([a47834f](https://github.com/phmatray/FormCraft/commit/a47834f560ae9c04cdddedd34764839c169d8165))
* **security:** encrypt nested EncryptField members through their full path ([#423](https://github.com/phmatray/FormCraft/issues/423)) ([5b9e5b3](https://github.com/phmatray/FormCraft/commit/5b9e5b330a100c9f0e93ff819b942455de6d0cbd))
* **security:** key audit log entries by full nested path, not last member ([#406](https://github.com/phmatray/FormCraft/issues/406)) ([#412](https://github.com/phmatray/FormCraft/issues/412)) ([2e4fcf8](https://github.com/phmatray/FormCraft/commit/2e4fcf89f872d0c444970b151ee5823da8011124))
* **security:** redact nested audit keys by full path or last segment ([#417](https://github.com/phmatray/FormCraft/issues/417)) ([#418](https://github.com/phmatray/FormCraft/issues/418)) ([bfc24df](https://github.com/phmatray/FormCraft/commit/bfc24df2f3dc5a94b2227d27d7472b7645801e69))
* **security:** resolve EncryptField paths through inherited interface properties ([#429](https://github.com/phmatray/FormCraft/issues/429)) ([#432](https://github.com/phmatray/FormCraft/issues/432)) ([864a071](https://github.com/phmatray/FormCraft/commit/864a07192e05f2ebd99d66277b84a3a5110d51fc))
* **upload:** resolve constraints through one path in every upload component ([#340](https://github.com/phmatray/FormCraft/issues/340)) ([#388](https://github.com/phmatray/FormCraft/issues/388)) ([c1606a0](https://github.com/phmatray/FormCraft/commit/c1606a089eeaffa50e8c03b300b92b36850db276))
* **validation:** keep prior messages intact when ValidateModelAsync throws mid-pass ([#440](https://github.com/phmatray/FormCraft/issues/440)) ([#441](https://github.com/phmatray/FormCraft/issues/441)) ([26ae1ff](https://github.com/phmatray/FormCraft/commit/26ae1ffb32c5da742522769200899a4623447f6b))
* **validation:** stop message-store races in DynamicFormValidator ([#443](https://github.com/phmatray/FormCraft/issues/443)) ([#444](https://github.com/phmatray/FormCraft/issues/444)) ([6052839](https://github.com/phmatray/FormCraft/commit/60528393c89188d6053d3208892d3147ffaa3d4e))


### Performance

* **core:** compile each field's value getter once ([#269](https://github.com/phmatray/FormCraft/issues/269)) ([#286](https://github.com/phmatray/FormCraft/issues/286)) ([cdc9ec4](https://github.com/phmatray/FormCraft/commit/cdc9ec4aa91cf73e7f681e43a1d5ad9e8e057a1a))
* **core:** compile each field's value getter once per validation ([#312](https://github.com/phmatray/FormCraft/issues/312)) ([#320](https://github.com/phmatray/FormCraft/issues/320)) ([320d689](https://github.com/phmatray/FormCraft/commit/320d689cebfac6079b38a53cbd5e97b1be21f61e))
* **core:** remove per-render reflection from the field render path ([#314](https://github.com/phmatray/FormCraft/issues/314)) ([#378](https://github.com/phmatray/FormCraft/issues/378)) ([1cae68e](https://github.com/phmatray/FormCraft/commit/1cae68e21b8158feca46cd37cec18977116f5446))


### Refactor

* **build:** give the results directory one definition ([#339](https://github.com/phmatray/FormCraft/issues/339)) ([#359](https://github.com/phmatray/FormCraft/issues/359)) ([3556327](https://github.com/phmatray/FormCraft/commit/3556327f635ff7bebfd1603356af0e93a55bf153))
* **core:** call the collection validator through a typed surface ([#344](https://github.com/phmatray/FormCraft/issues/344)) ([#381](https://github.com/phmatray/FormCraft/issues/381)) ([bd725ba](https://github.com/phmatray/FormCraft/commit/bd725bafd35f63796b5ad0f5e4c4de5224e571ee))
* **core:** expose Validators as IReadOnlyList and require AddValidator ([#245](https://github.com/phmatray/FormCraft/issues/245)) ([376866b](https://github.com/phmatray/FormCraft/commit/376866b9fd4793fae7ffcd53bf1204b32e9041c5))
* **core:** move the UI-agnostic adapter machinery into core ([#279](https://github.com/phmatray/FormCraft/issues/279)) ([#289](https://github.com/phmatray/FormCraft/issues/289)) ([e31cc39](https://github.com/phmatray/FormCraft/commit/e31cc3999b3aeb08aa9f40d915443548a5413267))
* **core:** share collection row identity tracking across adapters ([#422](https://github.com/phmatray/FormCraft/issues/422)) ([#442](https://github.com/phmatray/FormCraft/issues/442)) ([7b5111c](https://github.com/phmatray/FormCraft/commit/7b5111cfab28c9e7265aa6eb776f619f883aab57))
* **core:** share diagnostics warning log and per-form latch across adapters ([#398](https://github.com/phmatray/FormCraft/issues/398)) ([c0044cf](https://github.com/phmatray/FormCraft/commit/c0044cf6abd6917c5c0b6a6410ab08d6288b6c5e))
* **core:** share numeric type-default resolution between both adapters ([#389](https://github.com/phmatray/FormCraft/issues/389)) ([#393](https://github.com/phmatray/FormCraft/issues/393)) ([aaa27e9](https://github.com/phmatray/FormCraft/commit/aaa27e9571c68d4e6c8bd70d150ba327e73e1a75))
* **mudblazor:** express the two diagnostic idioms once each ([#284](https://github.com/phmatray/FormCraft/issues/284)) ([#294](https://github.com/phmatray/FormCraft/issues/294)) ([17e0dea](https://github.com/phmatray/FormCraft/commit/17e0dea4cec1e7d3f584570a844fbd23fc24a97c))
* **mudblazor:** render collection item fields through IFieldRendererService ([#203](https://github.com/phmatray/FormCraft/issues/203)) ([#250](https://github.com/phmatray/FormCraft/issues/250)) ([5c66e29](https://github.com/phmatray/FormCraft/commit/5c66e29ad87bace3f3a74665ffe4bde7081d25ba))

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

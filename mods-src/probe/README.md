# Probe 鈥?鑷畾涔夎鑹?Mod锛堟妧鏈獙璇佸垏鐗囷級

鍦ㄣ€婃潃鎴皷濉?銆媣0.111.0 涓婃柊澧炰竴涓?*鍙帺鐨勫師鍒涜鑹?*涓?*鍏ㄦ柊鍗＄墝**銆?鏈樁娈靛彧鍋氥€岀绾块獙璇併€嶏細瑙掕壊鑳借娉ㄥ唽銆佽兘琚紑灞€銆佸崱鐗岃兘鍑恒€佹暟鍊兼纭€傜編鏈殏鍊熷師鐗堣祫婧愩€?
## 褰撳墠鐘舵€侊細宸插疄娴嬮€氳繃 鉁?
| 楠岃瘉椤?| 缁撴灉 |
|---|---|
| Mod 鍔犺浇 | `Found mod manifest ... mods\probe\mod_manifest.json` 鈫?`Finished mod initialization for 'Probe Character' (probe)` |
| 瑙掕壊娉ㄥ唽 | `bridge_start_run(character="Probe")` 鎴愬姛 |
| 鍒濆鐘舵€?| HP 70/70銆侀噾甯?99銆侀仐鐗?BurningBlood銆佺墝缁?10 寮狅紙5脳ProbeStrike / 4脳ProbeDefend / 1脳ProbeBolt锛?|
| 鏀诲嚮鐗?| ProbeStrike 鎵撳嚭 6 浼ゅ锛涙寕涓婃槗浼ゅ悗鎵?9锛?脳1.5锛夆渽 |
| 鎶€鑳界墝 | ProbeDefend 鑾峰緱 5 鏍兼尅 鉁?|
| 鏂藉姞鐘舵€?| ProbeBolt 閫犳垚 5 浼ゅ骞舵柦鍔?1 灞?Vulnerable 鉁?|
| 鍥炲悎娴佽浆 | 鎶界墝 / 寮冪墝 / 缁撴潫鍥炲悎 / 鏁屼汉琛屽姩 / 绗?2 鍥炲悎姝ｅ父 鉁?|
| 鍗囩骇 | 鎺у埗鍙?`upgrade 0` 鍚?ProbeStrike 鎵?9锛?+3锛夆渽 |
| **閫変汉鐣岄潰** | 鑺傜偣 `CharSelectButtons/ButtonContainer/PROBE-PROBE_button`锛宍Lock` 涓嶅彲瑙侊紙鏈攣瀹氾級锛涚偣閫夊悗淇℃伅闈㈡澘鏄剧ず **The Probe** / 70/70 / 99 閲戝竵 / 鑷畾涔夋弿杩帮紙瑙?`.cache/probe_charselect2.png`锛?|
| 浠?UI 寮€灞€ | 鐐?Confirm 鍚庤繘鍏?Neow 浜嬩欢锛岃鑹?= Probe銆佺墝缁?10 寮犮€侀仐鐗?Burning Blood 鉁?|
| 鍗＄墝搴?| 渚ф爮鍑虹幇 `FILTER-CHARACTER_PROBE-PROBE` 鍗℃睜绛涢€夛紝鍗℃睜鍐呭崱鐗屽叏閮ㄥ彲瑙併€佹弿杩版纭紙瑙?`.cache/probe_library.png`锛?|
| 濂栧姳 / 鍟嗗簵 | 鍗＄墝濂栧姳鑳芥甯哥敓鎴愶紙鏃ュ織瀹炴祴鎶藉埌 `CARD.PROBE-PROBE_CHARGE`锛夛紝鍟嗗簵浜旀Ы涓嶅啀鎶涘紓甯?|
| 鍗囩骇 | 鎺у埗鍙?`upgrade 0` 鍚?ProbeStrike 鎵?9锛?+3锛夆渽 |
| **鏁村眬閫氬叧** | `bridge_autoslay_start(character="Probe", seed="PROBEWIN6")` 鈫?鎵撻€氫笁骞?鈫?`[probe] installing placeholder architect dialogue` 鈫?`Victory! Run completed`锛屽叏绋嬩粎 1 鏉?ERROR锛堟潵鑷埆鐨?mod 鐨勬棫寮忎緷璧栧０鏄庯級 |
| 鐢婚潰 | 浣跨敤 Ironclad 鐨?Spine 绔嬬粯鍗犱綅锛孶I 姝ｅ父锛堣 `.cache/probe_combat.png`锛?|

### 鍗℃睜锛?7 寮狅級

**浠?[design/cards.md](design/cards.md) 涓哄噯**锛堥偅鏄璁＄锛屼篃鏄垜鐢熸垚浠ｇ爜鐨勮緭鍏ワ級銆傚綋鍓嶉厤姣旓細

| 绋€鏈夊害 | 鏁伴噺 | 绫诲瀷 | 鏁伴噺 |
|---|---|---|---|
| 鍩虹 Basic | 4锛堟墦鍑幻?銆侀槻寰∶?銆佺‖鍖柮?銆佹縺鍙懨? 璧锋墜锛?| 鏀诲嚮 Attack | 7 |
| 鏅€?Common | 6 | 鎶€鑳?Skill | 9 |
| 缃曡 Uncommon | 1 | 鑳藉姏 Power | 1 |
| 绋€鏈?Rare | 6 | | |

> 鈿狅笍 缃曡妗ｅ彧鍓?**1** 寮犮€傚鍔卞崱鎸夌█鏈夊害鎶斤紝钖勬。浣嶆娊骞插悗鏈夋姏寮傚父鐨勯闄╋紝寤鸿琛ュ埌 鈮?銆?
鑷畾涔夌姸鎬侊細**閽欓摖**锛坄src/Powers/CalciumArmorPower.cs`锛屽洖鍚堝紑濮嬫寜灞傛暟缁欐牸鎸★紱浠庡崱鐗岃幏寰楁牸鎸℃椂鎸夋牸鎸″€间竴鍗婃帀灞傦級銆?
> **涓轰粈涔堝繀椤绘槸杩欎釜鍒嗗竷**锛氬鍔卞崱鎸夌█鏈夊害鎶姐€佸晢搴楁寜銆岀被鍨嬨€嶆娊銆?> 瑙佷笅闈€屽崱姹犺妯＄殑涓や釜纭€ц姹傘€嶃€?
## 鐩綍

```
probe/
  probe.csproj              net9.0锛屽紩鐢ㄦ父鎴忚嚜甯?sts2/GodotSharp/0Harmony + lib/BaseLib.dll
  mod_manifest.json         id=probe锛屼緷璧?BaseLib >= 3.4.5
  lib/BaseLib.dll           缂栬瘧鏈熷紩鐢紙鍙栬嚜宸ュ潑 v3.4.7锛屼笉闅?mod 鍒嗗彂锛?  design/cards.md           鈽?鍗＄墝璁捐琛紙浣犲～锛屾垜璇伙級
  design/powers.md          鈽?buff/debuff 瀹氫箟琛紙浼氭樉绀哄眰鏁扮殑鐘舵€侊級
  design/mechanics.md       鈽?鍗＄墝涓撳睘鏈哄埗 / 鍏抽敭瀛楄〃
  design/relics.md          鈽?閬楃墿璁捐琛?  tools/cardspec.py         涓夎〃瑙ｆ瀽 + 浜ゅ弶鏍￠獙鑴氭湰
  src/
    ProbeMain.cs            [ModInitializer("Init")] 鍏ュ彛
    Characters/Probe.cs     瑙掕壊妯″瀷锛堢户鎵?BaseLib 鐨?PlaceholderCharacterModel锛?    Pools/ProbePools.cs     鍗℃睜 / 閬楃墿姹?/ 鑽按姹?    Cards/*.cs              20 寮犲崱
    Relics/*.cs             6 寮犻仐鐗?    Patches/*.cs            TheArchitect 鑳滃埄淇
  tools/
    build.ps1               缂栬瘧锛堟妸 dotnet 鐘舵€佺暀鍦ㄥ伐浣滃尯鍐咃紝娌欑閲屼篃鑳借窇锛?    deploy.ps1              閮ㄧ讲鍒?<game>\mods\probe
```

## 鍏抽敭璁捐锛堜负浠€涔堣繖涔堝啓锛?
### 1. 瑙掕壊鑷姩娉ㄥ唽锛屼笉闇€瑕佹墜鍐欐敞鍐岃〃
BaseLib 鐨?`CustomCharacterModel` 鏋勯€犲嚱鏁颁細璋冪敤 `CustomContentDictionary.AddCharacter(this)`锛?娓告垙鑷繁鐨?`ModelDb.AllAbstractModelSubtypes` 浼氶€氳繃 `ReflectionHelper.GetSubtypesInMods<AbstractModel>()`
鎵弿鎵€鏈夊凡鍔犺浇 mod 绋嬪簭闆嗗苟瀹炰緥鍖栥€傛墍浠ワ細**绫诲啓鍑烘潵灏辫嚜鍔ㄨ繘姹?*銆?
### 2. 鍗＄墝蹇呴』甯?`[Pool]`
`CustomCardModel` 鏋勯€犲嚱鏁伴噷 `autoAdd: true` 鏃朵細璧?`CustomContentDictionary.AddModel`锛?娌℃湁 `[Pool(typeof(...))]` 浼氱洿鎺ユ姏寮傚父銆傚崱姹犲唴閮ㄥ啀鐢ㄥ畼鏂圭殑 `ModHelper.AddModelToPool` 鍚堝苟銆?
### 3. ID / 鏈湴鍖?key 閮芥槸澶у啓鐨?`ModelId.Entry = StringHelper.Slugify(type.Name)`锛岃€?`Slugify` 鍐呴儴浼?`ToUpperInvariant()`銆?鎵€浠?`class Probe` 鈫?entry `PROBE` 鈫?鏈湴鍖?key 鏄?`PROBE.title`銆乣PROBE.description`銆?鍗″悓鐞嗭細`ProbeStrike` 鈫?`PROBE_STRIKE.title`銆?
### 4. 鏈湴鍖栫洿鎺ュ啓鍦ㄤ唬鐮侀噷
`ILocalizationProvider.Localization` 杩斿洖 `(鍚庣紑, 鏂囨湰)` 鍒楄〃锛孊aseLib 浼氭寜妯″瀷绫诲埆鍐欒繘
`characters` / `cards` 琛紝key 鑷姩鎷兼垚 `Entry.鍚庣紑`銆傛兂鏀圭敤 JSON 涔熸敮鎸?锛堟斁 `<mod>/localization/zhs/*.json`锛岃蛋 PCK锛夛紝瑙?`.cache/pck_extract/localization/` 閲岀殑鍘熺増鏍蜂緥銆?
### 5. 缇庢湳鍗犱綅闈?`PlaceholderCharacterModel`
瀹冩妸**鎵€鏈?*瑙掕壊澶栬璧勬簮锛堟垬鏂楃珛缁樸€侀《鏍忓ご鍍忋€佽兘閲忚鏁板櫒銆佷紤鎭/鍟嗗簵鍔ㄧ敾銆佸崱鐗屾嫋灏俱€侀€変汉鑳屾櫙銆?闊虫晥浜嬩欢锛夐兘鎸囧悜 `PlaceholderID` 鎸囧畾鐨勫師鐗堣鑹诧紙榛樿 ironclad锛夈€傛敼 `PlaceholderID` 灏辨崲涓€濂楃毊銆?
鎹㈡垚鑷繁鐨勭編鏈椂锛?*涓€娆″彧瑕嗙洊涓€涓垚鍛?*锛岄€愰」鏇挎崲锛岄殢鏃跺彲鍥為€€锛?
```csharp
public override string? CustomVisualPath => "res://probe/scenes/probe_visuals.tscn"; // %Visuals/%Bounds/%IntentPos/%CenterPos
public override string? CustomIconTexturePath => "res://probe/images/char_icon_probe.png";
public override string? CustomCharacterSelectIconPath => "res://probe/images/char_select_probe.png";
public override string? CustomEnergyCounterPath => "res://probe/scenes/probe_energy_counter.tscn";
public override string? CustomRestSiteAnimPath => "res://probe/scenes/probe_rest_site.tscn";
```

> 鎴樻枟绔嬬粯鍙互鍋锋噿锛欱aseLib 鐨?`NodeFactory<NCreatureVisuals>`.CreateFromResource(Texture2D)
> 鑳界洿鎺ユ妸涓€寮?PNG 鍙樻垚鍚堟牸鐨?`NCreatureVisuals`锛堣嚜鍔ㄨˉ Bounds / Sprite2D / 鍚?Marker锛夛紝
> 鍗?`CreateCustomVisuals()` 閲岃繑鍥?`NodeFactory<NCreatureVisuals>.CreateFromResource("res://...png")`銆?
## 缂栬瘧 / 閮ㄧ讲 / 楠岃瘉

```powershell
# 缂栬瘧锛堝伐浣滃尯鍐咃紝涓嶉渶瑕佽仈缃戯級
powershell -ExecutionPolicy Bypass -File mods-src\probe\tools\build.ps1

# 閮ㄧ讲锛堥渶瑕佸啓娓告垙鐩綍锛屾矙绠卞鎵ц锛?powershell -ExecutionPolicy Bypass -File mods-src\probe\tools\deploy.ps1

# 鍚姩娓告垙鍚庯紝鐢?MCP bridge 楠岃瘉
#   bridge_ping
#   bridge_start_run(character="Probe", seed="PROBE1")
#   bridge_get_full_state
#   bridge_navigate_map(row=1, col=3)
#   bridge_get_combat_state 鈫?bridge_play_card(...)
```

缂栬瘧鐜瑕佺偣锛歚DOTNET_CLI_HOME=D:\STS2\.dotnet-home`銆乣NUGET_PACKAGES=D:\STS2\.nuget\packages`锛?鍚﹀垯 dotnet 棣栨杩愯浼氬幓鍐?`%USERPROFILE%\.dotnet` 鑰岃娌欑鎷掔粷銆?
## 鍗℃睜瑙勬ā鐨勪袱涓‖鎬ц姹傦紙瀹炴祴韪╁嚭鏉ョ殑锛?鑷畾涔夎鑹插崱姹犲お灏忎細璁?*鍒殑绯荤粺鐩存帴鎶涘紓甯?*锛岃€屼笖寮傚父鍙戠敓鍦?UI 鏋勫缓闃舵锛岃〃鐜版槸銆岀敾闈㈠崱浣?/ 娌℃湁濂栧姳灞忋€嶃€?
### 1. 鍗＄墝濂栧姳锛氭瘡涓█鏈夊害閮借鏈夌墝
`CardFactory.CreateForReward` 鍏堟寜鏉冮噸鎺蜂竴涓█鏈夊害锛屽啀鐢?`GetNextAllowedRarity` 鍚戜笂鎵?銆屾湰姹犲瓙閲岀湡鏈夌墝銆嶇殑绋€鏈夊害锛屾壘涓嶅埌灏辫繑鍥?`None` 骞舵姏锛?`Tried to create a card for a reward, but we couldn't generate a valid rarity!`

Basic 鐗屼笉鍙備笌濂栧姳锛坄Rarity != Basic` 杩囨护锛夛紝涓?*鐗岀粍閲屽凡鏈夌殑鐗屼細琚媺榛?*銆?鎵€浠ュ彧鏈?2 Basic + 1 Uncommon 鏃讹紝Uncommon 涓€鏃﹁鎷夐粦灏卞繀鐒跺け璐ャ€?
鈫?鑷冲皯瑕佹湁 Common / Uncommon / Rare 涓夋。鐨勭墝銆?
### 2. 鍟嗗簵锛欰ttack / Skill / Power 涓夌绫诲瀷閮借鏈夌墝
`MerchantInventory._coloredCardTypes` 鏄啓姝荤殑 `[Attack, Attack, Skill, Skill, Power]`锛?姣忎釜妲戒綅鎸夌被鍨嬩粠瑙掕壊鍗℃睜閲屾娊锛屽悓涓€娆″晢搴楀唴杩樹細鍘婚噸銆傚皯涓€绉嶇被鍨嬪氨鎶涳細
`Can't generate valid rarity for merchant card type Power with card options: ...`

鈫?鑷冲皯瑕佹湁 **2 寮犻潪 Basic 鏀诲嚮銆? 寮犻潪 Basic 鎶€鑳姐€? 寮犻潪 Basic 鑳藉姏鐗?*銆?
鑳藉姏鐗屼笉闇€瑕佽嚜瀹氫箟 PowerModel锛氱敤鍘熺増 `PowerCmd.Apply<StrengthPower>` 灏辫锛堣 `ProbeCharge`锛夈€?
## 鑷畾涔夎鑹查€氬叧浼氬穿锛歍heArchitect 绌哄彴璇?
鎵撹耽绗笁骞?Boss 鍚庤繘鍏?`THE_ARCHITECT` 浜嬩欢锛岀‘璁よ儨鍒╂椂锛?
```csharp
private async Task WinRun() {
    if (LocalContext.IsMe(Owner)) {
        await AnimPlayerAttackIfNecessary(Dialogue.EndAttackers);  // Dialogue 涓?null 鈫?NRE
        await RunManager.Instance.WinRun();                        // 姘歌繙鍒颁笉浜?    }
}
```

`LoadDialogue()` 鍙湪鍘熺増 5 涓鑹查噷鏌ュ彴璇嶏紝鑷畾涔夎鑹插彇鍒扮┖鍒楄〃 鈫?`Rng.NextItem` 杩斿洖 null銆?UI 鏄剧ず PROCEED 涓€鍒囨甯革紝鐩村埌鐐逛笅鍘绘墠 NRE 鈥斺€?琛ㄧ幇涓恒€屾墦瀹?Boss 鍗′綇/闂€€銆嶃€?
淇硶瑙?`src/Patches/ArchitectWinRunPatch.cs`锛氱粰 `WinRun` 鎵?Prefix锛?*鍙湪 `_dialogue` 涓?null 鏃?*
濉炰竴鏉￠潤榛樺崰浣嶅彴璇嶏紙`LineText = null`銆乣EndAttackers = None`锛夛紝鐒跺悗**鐓у父鎵ц鍘熸柟娉?*銆?鍘熸柟娉曟嬁鍒伴潪绌哄彴璇?鈫?璺宠繃寤虹瓚甯堟敾鍑诲姩鐢?鈫?璧板埌 `RunManager.Instance.WinRun()`銆?鍘熺増瑙掕壊涓嶄細鍛戒腑璇ュ垎鏀€?
涓夋潯韪╁潙璁板綍锛?
1. **Harmony 瀛楁娉ㄥ叆 `___field` 瀵瑰簲瀛楁鍚?`field`**銆傝繖閲岀殑瀛楁鍙?`_dialogue`锛岃鍐?`____dialogue`锛?   鎴戠涓€鐗堝啓鎴?`___dialogue`锛孒armony 闈欓粯璺宠繃锛堟棩蹇?`patch ... skipped`锛夛紝璺戝畬鏁村眬鎵嶅彂鐜版病鐢熸晥銆?   鐜板湪缁熶竴鐢?`AccessTools.Field(...)` 鏄惧紡鍙嶅皠 + 鎵句笉鍒板瓧娈垫墦 warn銆?2. 涓€寮€濮嬫兂鍦?`LoadDialogue` 涔嬪悗琛ュ彴璇嶏紝缁撴灉 `OnRoomEnter` 鍙戠幇銆屾湁鍙拌瘝銆嶄細 `ClearCurrentOptions()`锛?   鑰?`PlayCurrentLine` 閬囧埌绌?`LineText` 鎻愬墠 return锛岄€夐」鍐嶄篃娌¤鍥炲幓 鈫?鍗″湪鏃犻€夐」鐨勪簨浠堕噷
   锛圓utoSlay 鎶?`Event options not loaded`锛夈€?*鍒妸鍗犱綅鍙拌瘝鏀惧湪浜嬩欢鍒濆鍖栭樁娈?*銆?3. Prefix 閲?`return false` 璺宠繃 async 鍘熸柟娉曪紝浼氳瀹冭繑鍥?null Task锛?   璋冪敤鏂?`EventOption.Chosen()` 鍐嶈В寮曠敤 鈫?鍙堜竴涓?NRE銆?*瑕佽繑鍥炵湡姝ｇ殑 Task锛屽氨寰楁斁琛屽師鏂规硶**銆?
## 宸ヤ綔娴侊細鐘舵€佹爣璁?+ 姣忚疆鍙寰呭姙

姣忔潯璁捐锛堝崱鐗?/ 鐘舵€?/ 鏈哄埗 / 閬楃墿锛夌殑 `澶囨敞` 閲屽甫涓€涓爣绛撅細

| 鏍囩 | 鍚箟 | 鎴戝仛浠€涔?|
|---|---|---|
| `宸插疄瑁卄 | 浠ｇ爜宸插疄鐜?| **瀹屽叏涓嶈**锛堢渷 token锛?|
| `寰呭疄瑁卄 | 寰呭疄鐜?| 鍐欎唬鐮?+ 缂栬瘧 + 閮ㄧ讲 |
| `寰呬慨鏀筦 | 闇€瑕佹敼 | 鎸夊娉ㄩ噷鍐欑殑鏀?|
| `寰呭垹闄 | 瑕佸垹 | 鍒犱唬鐮侊紝**骞朵粠 md 閲屽垹鎺夎繖涓€鏉?* |

鏍囩璇嗗埆瀵?`宸?瀹炶`锛堝甫绌烘牸锛変篃鏈夋晥锛涘悓涓€鏉″啓浜嗗涓爣绛炬椂鎸?`寰呭垹闄?> 寰呬慨鏀?> 寰呭疄瑁?> 宸插疄瑁卄
鍙栨渶闇€瑕佸鐞嗙殑閭ｄ釜锛屾墍浠ャ€屽凡瀹炶锛涘緟淇敼銆嶄笉浼氳璇垽涓哄凡瀹屾垚銆?
### 姣忚疆鎴戝彧璺戣繖涓ゆ潯锛堜笉鏁寸瘒璇昏璁¤〃锛?
```powershell
$env:PYTHONUTF8='1'
$exe = 'D:\STS2\.python312\python.exe'
$tool = 'D:\STS2\mods-src\probe\tools\cardspec.py'

& $exe $tool --changed    # 鑷笂娆℃彁浜や互鏉ヤ綘鏀逛簡鍝簺鏉＄洰锛堟病鎵撴爣绛句篃鑳芥姄鍒帮級
& $exe $tool --pending    # 鏈疄瑁呮潯鐩殑瀹屾暣瑙勬牸锛屼竴琛屼竴鏉?```

`--changed` 鐨勮緭鍑哄舰濡傦細

```
cards.md
  鏂板锛?锛? 閽堝墏寮硅嵂銆侀儴缃?  淇敼锛?锛? 纭寲
```

`--pending` 鐨勮緭鍑哄舰濡傦紙姣忚鍖呭惈瀹炵幇鎵€闇€鐨勫叏閮ㄥ瓧娈碉紝鎴戜笉鐢ㄥ啀鎵撳紑 md锛夛細

```
[寰呬慨鏀筣 1 鏉?  - cards: 纭寲 | Basic | Skill | Self | 鍩虹: 璐圭敤 1 | 鑾峰緱 6 灞傞挋閾?| 鍗囩骇: 璐圭敤 1 | 鑾峰緱 8 灞傞挋閾?鈫?src\Cards\ProbeCalciumCards.cs
```

鏈哄櫒鍙锛歚cardspec.py --json --pending`銆?
涓€鑷存€ф鏌ワ紙`--todo` / 榛樿妯″紡锛変粛浼氭姤锛氭爣浜?`宸插疄瑁卄 浣嗕唬鐮侀噷娌℃湁銆佹爣浜?`寰呭疄瑁卄 浣嗕唬鐮佸凡鏈夈€?鏍囦簡 `寰呭垹闄 浣嗕唬鐮佸凡鏃犮€?
### 鎵归噺缁存姢

```powershell
& $exe D:\STS2\mods-src\probe\tools\tag_status.py --dry-run   # 缁欏凡瀹炵幇鐨勮ˉ 宸插疄瑁?& $exe D:\STS2\mods-src\probe\tools\tag_status.py
& $exe D:\STS2\mods-src\probe\tools\normalize_text.py       # 鍘绘帀鐗屾晥鏂囨湰閲岀殑绌烘牸锛堣〃鍜?C# 涓€璧锋敼锛?& $exe D:\STS2\mods-src\probe\tools\prune.py --dry-run        # 棰勮锛氬垹銆屽緟鍒犻櫎銆嶆潯鐩?+ 缈荤姸鎬?& $exe D:\STS2\mods-src\probe\tools\prune.py --retag
& $exe D:\STS2\mods-src\probe\tools\prune.py --clean-notes    # 鍘绘帀銆屽凡瀹炶锛堚€︼級銆嶉噷鐨勬畫鐣欒鏄?```

`prune.py --retag` **涓嶆槸鏃犺剳缈绘爣绛?*锛氬畠鍏堟瘮瀵瑰疄鐜颁笌鎻忚堪鏄惁涓€鑷达紙`<br>` 鏁伴噺銆乣璇嶆潯` 涓?`CanonicalKeywords`锛夛紝涓嶄竴鑷寸殑鏉＄洰浼氫繚鎸佸師鏍囩骞舵墦鍗?`瀹炵幇涓庢弿杩颁粛涓嶄竴鑷达紝淇濇寔鏍囩涓嶅姩`锛岄伩鍏嶆妸娌℃敼瀹岀殑鍗¤鏍囨垚宸插疄瑁咃紱缈绘爣绛炬椂浼氳繛甯﹀垹鎺夌揣璺熺殑
涓枃鎷彿璇存槑锛坄寰呬慨鏀癸紙鏀逛簡浠€涔堬級` 鈫?`宸插疄瑁卄锛夈€?
`normalize_text.py` 璁╃墝闈㈡枃鏈榻愬師鐗堜腑鏂囨帓鐗堬細`閫犳垚 6 鐐逛激瀹砢 鈫?`閫犳垚6鐐逛激瀹砢銆?瀹冨悓鏃舵敼鍐?`design/*.md` 鐨?`鍩虹/鍗囩骇/鎻忚堪/鏁堟灉` 鍜?`src/**/*.cs` 鐨?`("description", 鈥?` 瀛楃涓诧紝
姣忚疆鏀瑰畬鍗℃垜閮戒細璺戜竴娆°€?
鐗堟湰绠＄悊锛氬伐浣滃尯鏍圭洰褰曞凡 `git init`锛宍.gitignore` 鎺掗櫎浜?vendor/.cache/spine-1 绛夊ぇ鐩綍锛?鍙窡韪?`docs/` 涓?`mods-src/`銆傛瘡娆℃敼鍔ㄩ兘鎻愪氦涓€娆★紝`--changed` 灏辨槸鍩轰簬杩欐潯鎻愪氦绾垮仛瀵规瘮鐨勩€?
## 鍗＄墝璁捐娴佺▼锛堜綘 鈫?鎴戯級

1. 浣犲湪 [design/cards.md](D:\STS2\mods-src\probe\design\cards.md) 閲屾寜鏍煎紡鍐欏崱锛堟牸寮忚鏄庤
   [docs/card-spec-format.md](D:\STS2\docs\card-spec-format.md)锛夈€?   姣忓紶鍗″彧瑕佸啓锛?*鍚嶇О / 绋€鏈夊害 / 鍩虹锛堣垂鐢?鏁堟灉锛? 鍗囩骇锛堣垂鐢?鏁堟灉锛?*锛岀被鍨嬪拰鐩爣鍙互鐣欑┖璁╂垜鎺ㄦ柇銆?2. 浣犲厛鑷繁璺戜竴閬嶆牎楠岋紙鍙€夛紝浣嗗緢蹇級锛?
   ```powershell
   $env:PYTHONUTF8='1'
   D:\STS2\.python312\python.exe D:\STS2\mods-src\probe\tools\cardspec.py
   ```

   瀹冧細鎶ャ€岀己瀛楁 / 绋€鏈夊害鍐欓敊 / 鍗″悕閲嶅 / 鍗囩骇鍓嶅悗涓€鏍?/ 鍗℃睜閰嶆瘮涓嶅銆嶃€?3. 鎴戣璁捐琛?鈫?鎸夊悕绉板榻愶紙鏀瑰悕銆佹敼鏁板€笺€佹柊澧炪€乣# 鍋滅敤` 鍒犻櫎锛夆啋 鐢熸垚/淇敼 C# 绫?+ 鏈湴鍖?鈫?   缂栬瘧 鈫?鍗＄墝搴撻噷鏍稿 鈫?蹇呰鏃惰窇 AutoSlay 鍥炲綊銆?
璁捐琛ㄩ噷鐜板湪棰勫～浜嗗凡瀹炶鐨?20 寮犲崱锛屼綘鍙互鐩存帴鍦ㄤ笂闈㈡敼鍚嶆敼鏁板€笺€?
## 閬楃墿锛堝凡瀹炵幇锛屾睜瀛愬緟浣犻獙鏀讹級

6 寮犻仐鐗╁凡杩?`ProbeRelicPool`锛屽浘鏍囧€熺敤鍘熺増 atlas锛坄IconBaseName`锛夛紝璧锋墜閬楃墿鎹㈡垚浜?**ProbeCore**锛?
| 閬楃墿 | 绋€鏈夊害 | 鏁堟灉 | 鍥炬爣鍊熺敤 |
|---|---|---|---|
| Probe Core | 鍒濆 | 鎴樻枟鑳滃埄鍚庡洖澶?6 鐐圭敓鍛?| burning_blood |
| Anchor Chip | 鏅€?| 鎴樻枟寮€濮嬫椂鑾峰緱 8 鐐规牸鎸?| anchor |
| Smooth Memory | 鏅€?| 鎴樻枟鑳滃埄鍚庡洖澶?5 鐐圭敓鍛?| oddly_smooth_stone |
| Signal Amplifier | 缃曡 | 鎴樻枟寮€濮嬫椂鑾峰緱 1 鐐瑰姏閲?| vajra |
| Static Lantern | 缃曡 | 鎴樻枟寮€濮嬫椂鑾峰緱 1 鐐规晱鎹?| lantern |
| Recursion Engine | 绋€鏈?| 鎴樻枟寮€濮嬫椂鑾峰緱 2 鍔涢噺 + 2 鏁忔嵎 | bag_of_marbles |

宸插疄娴嬶細璧锋墜瑁呭 ProbeCore锛屾垬鏂楄儨鍒╁悗 HP 50 鈫?56锛?6锛夈€傚叾浣欓仐鐗╁皻鏈€愬紶楠岃瘉銆?
## 涓嬩竴姝?
1. **寮冪敤鍗犱綅缇庢湳**锛氱敤浣犺嚜宸辨彁渚涚殑绔嬬粯锛岄€愰」鏇挎崲 `Custom*Path`锛涘厛鍋氭垬鏂楃珛缁?+ 椤舵爮澶村儚 + 閫変汉澶村儚涓変欢濂椼€?2. **鎵╁崱姹?*锛氱洰鏍?20~30 寮犮€傜幇鏈?12 寮犳槸妯℃澘锛屽鍒舵敼鏁板€间笌 `OnPlay` 鍗冲彲銆?3. **瑙掕壊鏈哄埗**锛氱‘瀹氬叧閿瓧锛坄generate_mechanic` 鍙敓鎴?power + 绀轰緥鍗?+ 閬楃墿 + 鏈湴鍖栵級锛?   鍐嶈鍗℃睜鍥寸粫瀹冭璁°€?4. **閬楃墿/鑽按**锛氳嚜瀹氫箟閬楃墿瑕?`[Pool(typeof(ProbeRelicPool))]`锛涜嵂姘村悓鐞嗐€?
## 宸茬煡鐨勫潙锛堝疄娴嬭褰曪級

- **鎺у埗鍙?`kill all` / `damage` 缁撴潫鎴樻枟涓嶄細璧板鍔辩粨绠?*锛屾埧闂翠細鍋滃湪鍙湁瑙掕壊銆佹病鏈夐€€鍑烘寜閽殑鐘舵€侊紝
  鍙兘閲嶅紑涓€灞€锛坄room Monster` 鍙互璺冲嚭鍘伙級銆傚仛濂栧姳/鍗℃睜娴嬭瘯蹇呴』鐢ㄧ湡瀹炴墦娉曘€?- **`PunchOff` 浜嬩欢鎴块棿浼氳鏃ュ織姣忓抚鍒峰睆**锛氭煇娆℃暣灞€璺戝埌璇ヤ簨浠舵椂锛孏odot 鎸佺画鎶?  `Parameter "particles" is null` / `Parameter "mem" is null`锛屾爤椤舵槸
  `PackedScene.Instantiate_Patch1` 鈫?`PunchOff.PunchEachOther()`锛屼竴灞€鍐欏嚭 1.6GB 鏃ュ織銆?  杩欎釜琛ヤ竵鏉ヨ嚜鍒殑 mod锛堝満鏅疄渚嬪寲鎸傞挬锛夛紝涓嶆槸鏈?mod 鐨勪唬鐮佽矾寰勶紱
  鎹竴灞€娌￠亣鍒拌浜嬩欢鏃舵棩蹇楀彧鏈?~250KB銆?*璺戦暱鍥炲綊鍓嶈寰楃洴鐫€ `logs/godot.log` 浣撶Н**銆?- **AutoSlay 榛樿闅忔満閫夎鑹?*锛坄AutoSlayer` 閲?`_random.NextItem(鏈攣瀹氳鑹?`锛夛紝
  `bridge_autoslay_start(character=...)` 鍘熸湰瀹屽叏涓嶇敓鏁堛€傚凡缁?MCPTest 鍔犱簡
  `AutoSlayCharacterPatch`锛氭嫤鎴?`NCharacterSelectButton.Select`锛屾妸闅忔満閫変汉閲嶅畾鍚戝埌鎸囧畾瑙掕壊銆?  鍙﹀ `AutoSlayConfig` 閲岄兘鏄?`readonly` 闈欐€佸瓧娈碉紝`bridge_autoslay_configure` 鐨?  `max_floor` / `run_timeout` 瀹為檯鏀逛笉鍔紙鏃ュ織閲屼粛鏄剧ず 49 / 25min锛夈€?- **`bridge_click_node` 瀵?`NClickableControl` 绯绘寜閽父鏃犳晥**锛堝畠璋冪殑鏄?`ui_accept`锛夈€?  鍙潬鍋氭硶鏄敤 GodotExplorer 渚濇璋?`OnPressHandler` 鈫?`OnReleaseHandler`锛岃繖涓や釜鏄毚闇插嚭鏉ョ殑 Godot 鏂规硶銆?- **娓告垙缁撴潫鐣岄潰**鐨勩€岀户缁€嶆寜閽湪缁撶畻鍔ㄧ敾璺戝畬鍓嶆槸 disabled 鐨勶紱鍔ㄧ敾澶х害瑕佸崄鍑犵锛?  鏈熼棿 `IsEnabled=false`锛岀偣浜嗘病鍙嶅簲锛屽埆璇垽鎴愬崱姝汇€?- 浠?DSH 鍛戒护閲屽惎鍔ㄧ殑娓告垙杩涚▼浼氶殢鍛戒护缁撴潫琚潃銆傜粫杩囧姙娉曪細鎻愭潈鍚庣敤
  `Invoke-CimMethod Win32_Process Create` 璧?WMI 鍚姩锛堣繘绋嬩笉鍦ㄦ矙绠变綔涓氬璞￠噷锛岃兘瀛樻椿锛夈€?
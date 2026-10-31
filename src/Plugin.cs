using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using EpicLoot;
using EpicLoot.Adventure;
using EpicLoot.Adventure.Feature;
using EpicLoot.GatedItemType;
using HarmonyLib;
using UnityEngine;

namespace LocalEpicLootLuckyFinds
{


 public sealed partial class Plugin : MonoBehaviour
 {
  public const string Guid="local.epicloot.luckyfinds";
  internal static ConfigFile StartConfig;
  internal static BepInEx.Logging.ManualLogSource StartLogger;
  ConfigFile Config => StartConfig;
  BepInEx.Logging.ManualLogSource Logger => StartLogger;
  bool ready;
  static Plugin instance;
  ConfigEntry<bool> enabledSetting, logFinds;
  ConfigEntry<float> chance, bountyChance, vendorChance;
  static readonly System.Random random=new System.Random();
  static readonly FieldInfo Decisions=AccessTools.Field(typeof(CreatureSorterRunner),"LastDecisions");
  Harmony harmony;
  readonly HashSet<string> warnings=new HashSet<string>();
  [ThreadStatic] static RollContext context;
  [ThreadStatic] static string selected;
  [ThreadStatic] static BountyScope bounty;
  public sealed class BountyScope { public string Source; public Heightmap.Biome Biome; }
  public sealed class RollContext
  {
   public string Source;
   public BountyScope Bounty;
   public readonly Dictionary<LootDrop,string> Choices=new Dictionary<LootDrop,string>();
  }
  static bool Active => instance != null && instance.ready && instance.enabledSetting.Value && Player.m_localPlayer != null &&
   ZNet.instance != null && ZNet.instance.IsServer() && !ZNet.instance.IsDedicated() && ZoneSystem.instance != null;
  void Awake()
  {
   instance=this;
   enabledSetting=Config.Bind("General","Enabled",true,"Use native-creature equipment tiers capped by the first undefeated boss. Local host only.");
   chance=Config.Bind("Creatures","Lucky Gear Chance Percent",1f,new ConfigDescription("Per equipment roll chance to bypass the boss cap up to this creature's own native region; never above it.",new AcceptableValueRange<float>(0,100)));
   bountyChance=Config.Bind("Bounties","Lucky Gear Chance Percent",5f,new ConfigDescription("Per accepted main bounty equipment roll chance for the next region. Does not affect triple-coin bounties.",new AcceptableValueRange<float>(0,100)));
   vendorChance=Config.Bind("Vendor","Lucky Gear Chance Percent",5f,new ConfigDescription("Per newly generated equipment gamble offer chance for the same category exactly one tier above boss progression. No candidate means the original item.",new AcceptableValueRange<float>(0,100)));
   logFinds=Config.Bind("General","Log Lucky Finds",true,"Log regional equipment decisions and lucky upgrades for verification.");
   harmony=new Harmony(Guid);
   try {
    if(Decisions==null) throw new MissingFieldException("CreatureSorterRunner.LastDecisions");
    harmony.PatchAll(typeof(Plugin).Assembly);
    ready=true;
    Logger.LogInfo("Luck Finds 1.5.2 loaded. Lucky gear chances: creatures="+chance.Value+"%, bounties="+bountyChance.Value+"%, vendor="+vendorChance.Value+"%. Lucky bounty coins: 20% chance for 3x coins.");
   } catch(Exception ex) { ready=false; harmony.UnpatchSelf(); Logger.LogError("Regional loot inactive: "+ex); }
  }
  void OnDestroy() { harmony?.UnpatchSelf(); if(chimeSource)Destroy(chimeSource); if(chime)Destroy(chime); }
  static void Warn(string text) { if(instance.warnings.Add(text)) instance.Logger.LogWarning(text); }
  static List<Heightmap.Biome> Progression() => GatedItemTypeHelper.BiomesInOrder.Where(b=>b!=Heightmap.Biome.None && b!=Heightmap.Biome.Ocean).ToList();
  static int Ceiling(List<Heightmap.Biome> order)
  {
   return Policy.Ceiling(order.Select(b=>GatedItemTypeHelper.BiomesToBossKeys.TryGetValue(b,out var keys) && keys.Count>0 && keys.All(k=>ZoneSystem.instance.GetGlobalKey(k))).ToArray());
  }
  static int Tier(GatedItemDetails d,List<Heightmap.Biome> order)
  {
   if(d?.RequiredBosses==null || d.RequiredBosses.Count==0)return -1;
   int tier=-1;
   foreach(var boss in d.RequiredBosses) {
    int found=boss=="none"?0:-1;
    for(int i=0;i<order.Count;i++) if(GatedItemTypeHelper.BiomesToBossKeys.TryGetValue(order[i],out var keys) && keys.Contains(boss))found=i;
    if(found<0)return -1;
    tier=Math.Max(tier,found);
   }
   return tier;
  }
  static bool Equipment(ItemDrop.ItemData item)
  {
   if(item==null || !EpicLoot.EpicLoot.CanBeMagicItem(item))return false;
   switch(item.m_shared.m_itemType.ToString()) {
    case "OneHandedWeapon": case "TwoHandedWeapon": case "TwoHandedWeaponLeft": case "Bow": case "Shield":
    case "Helmet": case "Chest": case "Legs": case "Shoulder": case "Tool": return true;
    default:return false;
   }
  }
  static string Pick(LootDrop drop)
  {
   if(!Active || context==null || LootRoller.AnyItemSpawnCheatsActive() || drop==null)return null;
   if(context.Choices.TryGetValue(drop,out var cached))return cached;
   string result=null;
   try {
    var order=Progression();
    var decisions=Decisions.GetValue(null) as List<CreatureDecision>;
    var name=(context.Source??"").Replace("(Clone)","").Trim();
    var decision=decisions?.FirstOrDefault(d=>d.Name==name);
    // Never infer a creature's origin from the player's location or a loot-template tier.
    bool isBounty=context.Bounty!=null && context.Bounty.Source==name;
    int native=isBounty?order.IndexOf(context.Bounty.Biome):decision==null?-1:order.FindIndex(b=>string.Equals(b.ToString(),decision.Biome,StringComparison.OrdinalIgnoreCase));
    if(native<0)return null; // Non-creatures and unclassified creatures retain Epic Loot's boss-gated path.
    var db=ObjectDB.instance;
    var original=db?.GetItemPrefab(drop.Item)?.GetComponent<ItemDrop>();
    if(!Equipment(original?.m_itemData))return null;
    int cap=Ceiling(order);
    bool lucky=(isBounty ? native<order.Count-1 : native>cap) && Policy.Wins(random.NextDouble(),isBounty ? instance.bountyChance.Value : instance.chance.Value);
    int target=isBounty?Policy.BountyTarget(native,order.Count,lucky):Policy.Target(native,cap,lucky);
    GatedItemTypeHelper.AllItemsWithDetails.TryGetValue(drop.Item,out var originalDetails);
    var candidates=new List<string>();
    var slotFallback=new List<string>();
    var anyFallback=new List<string>();
    var craftable=new HashSet<string>(db.m_recipes.Where(r=>r!=null && r.m_enabled && !r.m_noCraftOnlyUpgrade && r.m_item!=null).Select(r=>r.m_item.gameObject.name));
    foreach(var entry in GatedItemTypeHelper.AllItemsWithDetails) {
     // Ungated neutral items have no native biome; unidentified conversion otherwise uses the kill location.
     if(entry.Value?.RequiredBosses==null || entry.Value.RequiredBosses.Contains("none") || Tier(entry.Value,order)!=target || LootDenyList.IsDenied(entry.Key) || !craftable.Contains(entry.Key))continue;
     var item=db.GetItemPrefab(entry.Key)?.GetComponent<ItemDrop>();
     if(!Equipment(item?.m_itemData))continue;
     anyFallback.Add(entry.Key);
     if(item.m_itemData.m_shared.m_itemType==original.m_itemData.m_shared.m_itemType)slotFallback.Add(entry.Key);
     if(originalDetails!=null && entry.Value.Type==originalDetails.Type)candidates.Add(entry.Key);
    }
    if(candidates.Count==0)candidates=slotFallback.Count>0?slotFallback:anyFallback;
    if(candidates.Count==0) {
     Warn("No equipment pool for "+order[target]+"; retaining boss-gated base roll."); return null;
    }
    candidates.Sort(StringComparer.Ordinal);
    result=candidates.Contains(drop.Item)?drop.Item:candidates[random.Next(candidates.Count)];
    if(instance.logFinds.Value)instance.Logger.LogInfo((isBounty?(lucky?"Lucky bounty":"Bounty drop"):(lucky?"Lucky Find":"Regional drop"))+": "+name+" native="+order[native]+" bossCap="+order[cap]+" target="+order[target]+" "+drop.Item+" -> "+result);
   } catch(Exception ex) { Warn("Regional selection failed; retaining boss-gated roll: "+ex.Message); }
   context.Choices[drop]=result;
   return result;
  }
  internal static string Resolve(string input,GatedItemTypeMode mode) => selected ?? GatedItemTypeHelper.GetGatedItemNameFromItemOrType(input,mode);

  const string RagdollBountyId="local-lucky-bounty-id";
  static BountyScope ReadBounty(ZDO zdo,string source,bool ragdoll=false)
  {
   if(!Active || zdo==null)return null;
   try {
    string id=zdo.GetString(ragdoll?RagdollBountyId:"BountyID","");
    if(string.IsNullOrEmpty(id))return null;
    var info=Player.m_localPlayer.GetAdventureSaveData().GetBountyInfoByID(id);
    if(!Policy.AcceptedBounty(true,!ragdoll && zdo.GetBool("IsAdd",false),
      info!=null && (info.State==BountyState.InProgress || info.State==BountyState.Complete)))return null;
    return new BountyScope { Source=source.Replace("(Clone)","").Trim(), Biome=info.Biome };
   } catch(Exception e) { Warn("Bounty classification unavailable; ordinary rules used: "+e.Message); return null; }
  }
  // Capture before Epic Loot's death callbacks can complete the quest. A boss flag alone never qualifies.
  [HarmonyPatch(typeof(Character),"OnDeath")]
  static class BountyDeathPatch {
   static void Prefix(Character __instance,out BountyScope __state) {
    __state=bounty;
    bounty=ReadBounty(__instance.GetComponent<ZNetView>()?.GetZDO(),__instance.gameObject.name);
   }
   static void Finalizer(BountyScope __state) { bounty=__state; }
  }
  // Corpse loot can run later, outside Character.OnDeath (including Instant Monster Drop).
  [HarmonyPatch(typeof(Ragdoll),"Setup")]
  static class BountyCorpsePatch {
   static void Postfix(Ragdoll __instance,CharacterDrop characterDrop) {
    try {
     var source=characterDrop ? characterDrop.GetComponent<ZNetView>()?.GetZDO() : null;
     var target=__instance.GetComponent<ZNetView>()?.GetZDO();
     if(characterDrop && target!=null && target.IsOwner() && ReadBounty(source,characterDrop.gameObject.name)!=null)
      target.Set(RagdollBountyId,source.GetString("BountyID",""));
    } catch(Exception e) { Warn("Could not retain bounty corpse context: "+e.Message); }
   }
  }
  [HarmonyPatch(typeof(Ragdoll),"SpawnLoot")]
  static class BountyCorpseLootPatch {
   static void Prefix(Ragdoll __instance,out BountyScope __state) {
    __state=bounty;
    var zdo=__instance.GetComponent<ZNetView>()?.GetZDO();
    bounty=ReadBounty(zdo,zdo?.GetString("characterName","")??"",true);
   }
   static void Finalizer(BountyScope __state) { bounty=__state; }
  }

  // Boss-based gating also governs non-creature loot and merchant stock. No configuration files are overwritten.
  [HarmonyPatch(typeof(EpicLoot.EpicLoot),"GetGatedItemTypeMode")]
  static class BossMode {
   static void Postfix(ref GatedItemTypeMode __result) { if(Active)__result=GatedItemTypeMode.BossKillUnlocksNextBiomeItems; }
  }
  [HarmonyPatch]
  static class ContextPatch {
   static MethodBase TargetMethod()=>typeof(LootRoller).GetMethods(BindingFlags.NonPublic|BindingFlags.Static).Single(m=>m.Name=="RollLootTableInternal" && m.GetParameters()[0].ParameterType==typeof(LootTable));
   static void Prefix(string objectName,out RollContext __state) { __state=context; context=new RollContext { Source=objectName, Bounty=bounty }; }
   static void Finalizer(RollContext __state) { context=__state; }
  }
  [HarmonyPatch]
  static class NormalPatch {
   static MethodBase TargetMethod()=>AccessTools.Method(typeof(LootRoller),"SpawnNormalItem");
   static void Prefix(LootDrop lootDrop,out string __state) { __state=selected; selected=Pick(lootDrop); }
   static void Finalizer(string __state) { selected=__state; }
   static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
    var original=AccessTools.Method(typeof(GatedItemTypeHelper),"GetGatedItemNameFromItemOrType",new[]{typeof(string),typeof(GatedItemTypeMode)});
    var replacement=AccessTools.Method(typeof(Plugin),nameof(Resolve));
    var code=instructions.ToList();
    int count=code.Count(i=>i.Calls(original));
    if(count!=2)throw new InvalidOperationException("Expected two normal gating calls, found "+count);
    foreach(var i in code) { if(i.Calls(original))i.operand=replacement; }
    return code;
   }
  }
  [HarmonyPatch]
  static class UnidentifiedPatch {
   static MethodBase TargetMethod()=>AccessTools.Method(typeof(LootRoller),"TrySpawnUnidentified");
   static void Prefix(LootDrop lootDrop,out string __state) { __state=lootDrop.Item; string choice=Pick(lootDrop); if(choice!=null)lootDrop.Item=choice; }
   static void Finalizer(LootDrop lootDrop,string __state) { lootDrop.Item=__state; }
  }
  [HarmonyPatch(typeof(GambleAdventureFeature),"GetGambleItems")]
  static class VendorPatch {
   static void Postfix(List<SecretStashItemInfo> __result) {
    if(!Active || __result==null)return;
    var order=Progression(); int cap=Ceiling(order);
    __result.RemoveAll(x=>Equipment(x.Item) && (!GatedItemTypeHelper.AllItemsWithDetails.TryGetValue(x.ItemID,out var d) || Tier(d,order)<0 || Tier(d,order)>cap));
    ApplyLuckyStock(__result,order,cap);
   }
  }
 }
}






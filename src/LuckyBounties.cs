using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using EpicLoot.Adventure;
using EpicLoot.Adventure.Feature;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace LocalEpicLootLuckyFinds {
 public sealed partial class Plugin {
  Dictionary<string,BountyRewardEntry> bountyRewards;
  string bountyRewardPath;
  string bountyRewardScope;
  bool bountyRewardsFailed;

  bool LoadBountyRewards() {
   string scope=Player.m_localPlayer.GetAdventureSaveData().WorldID.ToString(CultureInfo.InvariantCulture)+"-"+Player.m_localPlayer.GetPlayerID().ToString(CultureInfo.InvariantCulture);
   if(scope==bountyRewardScope)return !bountyRewardsFailed;
   bountyRewardScope=scope;
   bountyRewardPath=Path.Combine(Paths.ConfigPath,"LuckyFindsBounties",scope+".json");
   bountyRewardsFailed=false;
   try {
    bountyRewards=File.Exists(bountyRewardPath)?JsonConvert.DeserializeObject<Dictionary<string,BountyRewardEntry>>(File.ReadAllText(bountyRewardPath)):new Dictionary<string,BountyRewardEntry>();
    if(bountyRewards==null || bountyRewards.Values.Any(e=>!BountyRules.Valid(e)))throw new InvalidDataException("Invalid bounty history");
    return true;
   } catch(Exception ex) { bountyRewardsFailed=true; Warn("Lucky bounty coins disabled: cannot read saved history: "+ex.Message);return false; }
  }
  bool SaveBountyRewards() {
   try {
    Directory.CreateDirectory(Path.GetDirectoryName(bountyRewardPath));
    string temp=bountyRewardPath+".tmp";
    File.WriteAllText(temp,JsonConvert.SerializeObject(bountyRewards,Formatting.Indented));
    if(File.Exists(bountyRewardPath))File.Replace(temp,bountyRewardPath,null);else File.Move(temp,bountyRewardPath);
    return true;
   } catch(Exception ex) { bountyRewardsFailed=true;Warn("Lucky bounty coins disabled: cannot save history: "+ex.Message);return false; }
  }
  void ApplyBountyRewards(List<BountyInfo> offers) {
   if(!LoadBountyRewards())return;
   // Never alter accepted/completed contracts. Epic Loot saves the shown coin amount at acceptance.
   var available=offers.Where(b=>b!=null && b.State==BountyState.Available && b.PlayerID==Player.m_localPlayer.GetPlayerID()).ToList();
   bool changed=false;
   foreach(var offer in available) {
    string id=offer.ID;
    changed|=BountyRules.DecideOnce(bountyRewards,id,offer.RewardCoins,StockRules.Sample("lucky-bounty-v1|"+bountyRewardScope+"|"+id));
   }
   // Persist outcomes before displaying them. Never multiply an already modified object again.
   if(changed && !SaveBountyRewards())return;
   foreach(var offer in available)offer.RewardCoins=bountyRewards[offer.ID].Coins;
  }
  [HarmonyPatch]
  static class LuckyBountyOffersPatch {
   static MethodBase TargetMethod()=>typeof(BountiesAdventureFeature).GetMethods().Single(m=>m.Name=="GetAvailableBounties" && m.GetParameters().Length>0 && m.GetParameters()[0].ParameterType==typeof(int));
   static void Postfix(List<BountyInfo> __result) {
    if(!Active || __result==null)return;
    try { instance.ApplyBountyRewards(__result); }
    catch(Exception ex) { Warn("Lucky bounty offers unavailable: "+ex.Message); }
   }
  }
  [HarmonyPatch(typeof(BountyListElement),"SetItem")]
  static class LuckyBountyLabelPatch {
   static void Postfix(BountyListElement __instance,BountyInfo info) {
    if(!Active || info==null)return;
    try {
     if(!instance.LoadBountyRewards() || !instance.bountyRewards.TryGetValue(info.ID,out var entry) || !entry.Lucky || info.RewardCoins!=entry.Coins)return;
     if(__instance.NameText) {
      __instance.NameText.text="Lucky Bounty: "+__instance.NameText.text;
      __instance.NameText.color=new Color(1f,0.78f,0.15f);
     }
     if(__instance.Tooltip) {
      __instance.Tooltip.m_topic="Lucky Bounty: "+__instance.Tooltip.m_topic;
      __instance.Tooltip.m_text+="\n\nLucky Bounty: 3x coins. All other rewards unchanged.";
     }
    } catch(Exception ex) { Warn("Lucky bounty label unavailable: "+ex.Message); }
   }
  }
 }
}

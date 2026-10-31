using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using BepInEx;
using EpicLoot;
using EpicLoot.Adventure;
using EpicLoot.Adventure.Feature;
using EpicLoot.GatedItemType;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace LocalEpicLootLuckyFinds
{
 public sealed partial class Plugin
 {
  sealed class OfferTag { public string OriginalID; public string Key; }
  static readonly ConditionalWeakTable<SecretStashItemInfo,OfferTag> Tags=new ConditionalWeakTable<SecretStashItemInfo,OfferTag>();
  Dictionary<string,StockEntry> stock;
  string stockPath;
  List<string> visibleLucky=new List<string>();
  MerchantPanel panel;
  AudioSource chimeSource;
  AudioClip chime;
  bool stockFailed;

  bool LoadStock()
  {
   string scope=Player.m_localPlayer.GetAdventureSaveData().WorldID+"-"+Player.m_localPlayer.GetPlayerID();
   string path=Path.Combine(Paths.ConfigPath,"LuckyFindsStock",scope+".json");
   if(path==stockPath)return !stockFailed;
   stockPath=path; visibleLucky.Clear(); stockFailed=false;
   try { stock=File.Exists(path)?JsonConvert.DeserializeObject<Dictionary<string,StockEntry>>(File.ReadAllText(path)):new Dictionary<string,StockEntry>();
    if(stock==null || stock.Values.Any(x=>x==null))throw new InvalidDataException("Invalid stock history");
    return true;
   } catch(Exception ex) { stockFailed=true; Warn("Lucky vendor stock disabled for this session: cannot read saved history: "+ex.Message); return false; }
  }
  bool SaveStock()
  {
   try {
    Directory.CreateDirectory(Path.GetDirectoryName(stockPath));
    string temp=stockPath+".tmp";
    File.WriteAllText(temp,JsonConvert.SerializeObject(stock,Formatting.Indented));
    if(File.Exists(stockPath))File.Replace(temp,stockPath,null); else File.Move(temp,stockPath);
    return true;
   } catch(Exception ex) { Warn("Could not save vendor luck history; no new announcement: "+ex.Message); return false; }
  }
  static string UpgradeFor(SecretStashItemInfo offer,List<Heightmap.Biome> order,int cap,string key)
  {
   int target=Policy.VendorTarget(cap,order.Count);
   if(target<0 || !offer.IsGamble || !Equipment(offer.Item) || !GatedItemTypeHelper.AllItemsWithDetails.TryGetValue(offer.ItemID,out var original))return null;
   if(!Policy.Wins(StockRules.Sample(key+"|roll"),instance.vendorChance.Value))return null;
   var db=ObjectDB.instance;
   var craftable=new HashSet<string>(db.m_recipes.Where(r=>r!=null && r.m_enabled && !r.m_noCraftOnlyUpgrade && r.m_item!=null).Select(r=>r.m_item.gameObject.name));
   var candidates=GatedItemTypeHelper.AllItemsWithDetails.Where(e=>Policy.VendorCandidate(target,Tier(e.Value,order),original.Type,e.Value.Type,
    craftable.Contains(e.Key) && !LootDenyList.IsDenied(e.Key)) && Equipment(db.GetItemPrefab(e.Key)?.GetComponent<ItemDrop>()?.m_itemData)).Select(e=>e.Key).OrderBy(x=>x,StringComparer.Ordinal).ToList();
   return candidates.Count==0?null:candidates[(int)(StockRules.Sample(key+"|item")*candidates.Count)];
  }
  static void ApplyLuckyStock(List<SecretStashItemInfo> offers,List<Heightmap.Biome> order,int cap)
  {
   instance.visibleLucky.Clear();
   if(!instance.LoadStock())return;
   try {
    int interval=AdventureDataManager.Gamble.GetCurrentInterval();
    var keys=new Dictionary<SecretStashItemInfo,string>();
    bool changed=false;
    foreach(var offer in offers) {
     if(!offer.IsGamble || !Equipment(offer.Item))continue;
     string original=GambleAdventureFeature.GetGambleID(offer);
     string key=interval+"|"+original;
     keys[offer]=key;
     changed |= StockRules.DecideOnce(instance.stock,key,()=>UpgradeFor(offer,order,cap,instance.stockPath+"|"+key));
    }
    // Persist wins AND misses before exposing offers. Reopening cannot roll again.
    if(changed && !instance.SaveStock()) { instance.stockFailed=true; return; }
    for(int i=0;i<offers.Count;i++) {
     var offer=offers[i];
     if(!keys.TryGetValue(offer,out var key))continue;
     string upgrade=instance.stock[key].Upgrade;
     if(string.IsNullOrEmpty(upgrade))continue;
     var prefab=ObjectDB.instance.GetItemPrefab(upgrade);
     if(!prefab || LootDenyList.IsDenied(upgrade))continue;
     var item=prefab.GetComponent<ItemDrop>();
     if(!item || !Equipment(item.m_itemData))continue;
     var data=item.m_itemData.Clone(); data.m_dropPrefab=prefab; data.m_worldLevel=Game.m_worldLevel;
     data.m_stack=1; data.m_quality=1; data.m_durability=data.GetMaxDurability();
     var replacement=new SecretStashItemInfo(upgrade,data,offer.Cost.Clone(),true) { GuaranteedRarity=offer.GuaranteedRarity,Rarity=offer.Rarity };
     Tags.Add(replacement,new OfferTag { OriginalID=GambleAdventureFeature.GetGambleID(offer),Key=key });
     offers[i]=replacement;
     instance.visibleLucky.Add(key);
    }
   } catch(Exception ex) { Warn("Lucky stock unavailable; remaining offers unchanged: "+ex.Message); }
  }
  void Update()
  {
   if(!Active || panel==null || !panel.isActiveAndEnabled || visibleLucky.Count==0 || stockFailed || stock==null)return;
   try {
    // One chime for all new lucky offers currently shown, not one overlapping sound per row.
    var unseen=visibleLucky.Distinct().Where(k=>stock.TryGetValue(k,out var e) && StockRules.NeedsAnnouncement(e)).ToList();
    if(unseen.Count==0)return;
    EnsureChime();
    foreach(var key in unseen)stock[key].Announced=true;
    if(!SaveStock()) { foreach(var key in unseen)stock[key].Announced=false; return; }
    chimeSource.PlayOneShot(chime,0.65f);
    Player.m_localPlayer.Message(MessageHud.MessageType.Center,"Lucky Find! The vendor has special equipment.");
    if(logFinds.Value)Logger.LogInfo("Vendor discovery chime: "+unseen.Count+" new lucky offer(s); announcement saved.");
   } catch(Exception ex) { Warn("Vendor discovery sound unavailable: "+ex.Message); }
  }
  void EnsureChime()
  {
   if(chimeSource!=null && chime!=null)return;
   const int rate=44100; var samples=new float[(int)(rate*1.3)];
   double[] notes={659.255,830.609,987.767,1318.51};
   for(int n=0;n<notes.Length;n++)for(int i=0;i<(int)(rate*0.6);i++) {
    int index=(int)(n*0.18*rate)+i; if(index>=samples.Length)break;
    double t=i/(double)rate;
    double envelope=Math.Min(1,t/0.012)*Math.Exp(-7*t)*Math.Min(1,(0.6-t)/0.06);
    samples[index]+=(float)(0.18*Math.Sin(2*Math.PI*notes[n]*t)*envelope);
   }
   chime=AudioClip.Create("Lucky vendor discovery",samples.Length,1,rate,false);
   chime.SetData(samples,0);
   chimeSource=gameObject.AddComponent<AudioSource>(); chimeSource.playOnAwake=false; chimeSource.spatialBlend=0;
  }
  [HarmonyPatch(typeof(MerchantPanel),"OnEnable")]
  static class VendorOpenPatch { static void Postfix(MerchantPanel __instance) { instance.panel=__instance; } }
  // Keep the ORIGINAL offer ID for Epic Loot's once-per-refresh purchase tracking.
  [HarmonyPatch(typeof(GambleAdventureFeature),"GetGambleID")]
  static class OfferIdPatch {
   static void Postfix(SecretStashItemInfo itemInfo,ref string __result) { if(itemInfo!=null && Tags.TryGetValue(itemInfo,out var tag))__result=tag.OriginalID; }
  }
 }
}

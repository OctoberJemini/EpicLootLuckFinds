using System;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
namespace LocalEpicLootLuckyFinds
{
 [BepInPlugin("local.epicloot.luckyfinds", "Epic Loot: Luck Finds", "1.5.2")]
 [BepInDependency("randyknapp.mods.epicloot")]
 public sealed class Bootstrap : BaseUnityPlugin
 {
  void Awake() {
   try {
    if(!Chainloader.PluginInfos.TryGetValue("randyknapp.mods.epicloot",out var epic)) { Logger.LogError("Luck Finds inactive: Epic Loot is missing."); return; }
    var failures=Compatibility.Check(epic.Instance.GetType().Assembly,ApiContract.Entries);
    if(failures.Count>0) { Logger.LogWarning("Luck Finds inactive on Epic Loot "+epic.Metadata.Version+"; no patches applied. "+string.Join("; ",failures.ToArray())); return; }
    Logger.LogInfo("Epic Loot "+epic.Metadata.Version+" passed Luck Finds API checks. This checks API compatibility, not all in-game behavior.");
    StartEngine();
   } catch(Exception ex) { Logger.LogError("Luck Finds inactive: compatibility/startup check failed: "+ex); }
  }
  [MethodImpl(MethodImplOptions.NoInlining)]
  void StartEngine() {
   Plugin.StartConfig=Config; Plugin.StartLogger=Logger;
   gameObject.AddComponent<Plugin>();
  }
 }
}




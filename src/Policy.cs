using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
namespace LocalEpicLootLuckyFinds
{
 public sealed class StockEntry { public string Upgrade; public bool Announced; }
 internal static class StockRules
 {
  internal static double Sample(string key) {
   using(var sha=SHA256.Create()) {
    var b=sha.ComputeHash(Encoding.UTF8.GetBytes(key));
    uint value=((uint)b[0]<<24)|((uint)b[1]<<16)|((uint)b[2]<<8)|b[3];
    return value/4294967296.0;
   }
  }
  internal static bool DecideOnce(Dictionary<string,StockEntry> history,string key,Func<string> roll) {
   if(history.ContainsKey(key))return false;
   history.Add(key,new StockEntry { Upgrade=roll() }); return true;
  }
  internal static bool NeedsAnnouncement(StockEntry e) => e!=null && !string.IsNullOrEmpty(e.Upgrade) && !e.Announced;
 }
 internal static class Policy
 {
  internal static int Ceiling(bool[] defeated) {
   if(defeated==null || defeated.Length==0)return -1;
   for(int i=0;i<defeated.Length;i++)if(!defeated[i])return i;
   return defeated.Length-1;
  }
  internal static int Target(int native,int cap,bool lucky) => native<0 || cap<0 ? -1 : lucky ? native : Math.Min(native,cap);
  internal static int BountyTarget(int region,int count,bool lucky) => region<0 || region>=count ? -1 : Math.Min(region+(lucky?1:0),count-1);
  internal static bool AcceptedBounty(bool hasId,bool isAdd,bool acceptedOrComplete) => hasId && !isAdd && acceptedOrComplete;
  internal static int VendorTarget(int cap,int count) => cap>=0 && cap<count-1 ? cap+1 : -1;
  internal static bool VendorCandidate(int target,int candidate,string originalType,string candidateType,bool eligible)
   => eligible && target>=0 && candidate==target && !string.IsNullOrEmpty(originalType) && originalType==candidateType;
  internal static bool Wins(double sample,double percent) => !double.IsNaN(percent) && sample>=0 && sample<1 && sample<Math.Max(0,Math.Min(100,percent))/100.0;
 }
}

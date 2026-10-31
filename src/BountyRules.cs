using System;
using System.Collections.Generic;
namespace LocalEpicLootLuckyFinds {
 public sealed class BountyRewardEntry { public int BaseCoins; public int Coins; public bool Lucky; }
 internal static class BountyRules {
  internal static BountyRewardEntry Decide(int coins,double sample) {
   // Refuse an unrepresentable triple rather than wrapping into a negative payout.
   bool lucky=coins>0 && coins<=int.MaxValue/3 && Policy.Wins(sample,20);
   return new BountyRewardEntry { BaseCoins=coins, Coins=lucky?coins*3:coins, Lucky=lucky };
  }
  internal static bool DecideOnce(Dictionary<string,BountyRewardEntry> history,string key,int coins,double sample) {
   if(history.ContainsKey(key))return false;
   history.Add(key,Decide(coins,sample));return true;
  }
  internal static bool Valid(BountyRewardEntry e) => e!=null && (e.Lucky ? e.BaseCoins>0 && e.BaseCoins<=int.MaxValue/3 && e.Coins==e.BaseCoins*3 : e.Coins==e.BaseCoins);
 }
}

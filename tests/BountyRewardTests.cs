using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using LocalEpicLootLuckyFinds;
class BountyRewardTests {
 static int checks;
 static void Check(bool condition,string description){checks++;if(!condition)throw new Exception(description);}
 static void Main() {
  Check(BountyRules.Decide(300,0).Coins==900,"300 -> 900");
  Check(BountyRules.Decide(300,0.199999).Lucky,"Below 20% wins");
  Check(!BountyRules.Decide(300,0.2).Lucky,"Exact 20% loses");
  Check(BountyRules.Decide(300,0.9).Coins==300,"Normal reward unchanged");
  Check(!BountyRules.Decide(0,0).Lucky && BountyRules.Decide(0,0).Coins==0,"Zero remains zero");
  Check(!BountyRules.Decide(-10,0).Lucky,"Negative rewards not promoted");
  Check(BountyRules.Decide(int.MaxValue,0).Coins==int.MaxValue,"Overflow does not corrupt reward");
  Check(BountyRules.Decide(int.MaxValue/3,0).Coins==(int.MaxValue/3)*3,"Largest representable triple");
  var history=new Dictionary<string,BountyRewardEntry>();
  Check(BountyRules.DecideOnce(history,"world/player/interval/bounty",300,0.1),"First offer recorded");
  for(int i=0;i<100;i++) {
   Check(!BountyRules.DecideOnce(history,"world/player/interval/bounty",900,0.99),"Reopen cannot reroll");
   Check(history["world/player/interval/bounty"].Coins==900,"Reopen cannot multiply again");
  }
  history=JsonConvert.DeserializeObject<Dictionary<string,BountyRewardEntry>>(JsonConvert.SerializeObject(history));
  Check(history["world/player/interval/bounty"].Lucky && history["world/player/interval/bounty"].Coins==900,"Relog keeps win and amount");
  Check(!BountyRules.DecideOnce(history,"world/player/interval/bounty",300,0.99),"Reload does not reroll");
  Check(BountyRules.DecideOnce(history,"new-interval",300,0.9),"New offer can lose");
  Check(!BountyRules.DecideOnce(history,"new-interval",300,0),"Losing offers cannot reroll");
  Check(BountyRules.Valid(history["new-interval"]),"Normal history valid");
  Check(!BountyRules.Valid(new BountyRewardEntry {BaseCoins=300,Coins=2700,Lucky=true}),"Corrupt compounded reward rejected");
  Check(!BountyRules.Valid(null),"Null history rejected");
  int wins=0;
  for(int i=0;i<100000;i++) {
   string key="lucky-bounty-v1|world-player|Bounty."+i;
   double sample=StockRules.Sample(key);
   var first=BountyRules.Decide(300,sample);
   var again=BountyRules.Decide(300,StockRules.Sample(key));
   Check(first.Coins==again.Coins && first.Lucky==again.Lucky,"Stable per-bounty decision");
   if(first.Lucky)wins++;
  }
  Check(wins>19000 && wins<21000,"20% sample distribution");
  Console.WriteLine("PASS: "+checks+" bounty reward checks; "+wins+" lucky bounties in 100000 samples.");
 }
}

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using LocalEpicLootLuckyFinds;
class StockTests {
 static int checks;
 static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
 static void Main(){
  var history=new Dictionary<string,StockEntry>();int rolls=0;
  Check(StockRules.DecideOnce(history,"12|Sword",()=>{rolls++;return "SwordSilver";}),"First offer rolls");
  Check(StockRules.NeedsAnnouncement(history["12|Sword"]),"New winning offer announces");
  for(int i=0;i<100;i++)Check(!StockRules.DecideOnce(history,"12|Sword",()=>{rolls++;return "SwordMistwalker";}),"Reopen cannot reroll");
  Check(rolls==1 && history["12|Sword"].Upgrade=="SwordSilver","Selection fixed");
  history["12|Sword"].Announced=true;
  string saved=JsonConvert.SerializeObject(history);
  var restored=JsonConvert.DeserializeObject<Dictionary<string,StockEntry>>(saved);
  Check(restored["12|Sword"].Upgrade=="SwordSilver","Upgrade survives actual JSON roundtrip");
  Check(!StockRules.NeedsAnnouncement(restored["12|Sword"]),"Relog does not repeat chime");
  Check(!StockRules.DecideOnce(restored,"12|Sword",()=>{throw new Exception("Rerolled after load");}),"Relog keeps roll");
  Check(StockRules.DecideOnce(restored,"13|Sword",()=>"SwordSilver"),"New interval is new offer");
  Check(StockRules.NeedsAnnouncement(restored["13|Sword"]),"Same item new refresh announces again");
  StockRules.DecideOnce(restored,"13|Bow",()=>null);
  Check(!StockRules.NeedsAnnouncement(restored["13|Bow"]),"Losing offer silent");
  Check(!StockRules.DecideOnce(restored,"13|Bow",()=>"BowDraugrFang"),"Miss saved; cannot reroll into win");
  Check(StockRules.Sample("world1-player1|13|Sword|roll")==StockRules.Sample("world1-player1|13|Sword|roll"),"Stable roll across calls");
  Check(StockRules.Sample("world1-player1|13|Sword|roll")!=StockRules.Sample("world2-player1|13|Sword|roll"),"World-separated roll");
  int wins=0;for(int i=0;i<100000;i++)if(Policy.Wins(StockRules.Sample("offer-"+i),0.5))wins++;
  Check(wins>400 && wins<600,"Stable hash distribution near 0.5 percent: "+wins);
  Console.WriteLine("PASS: "+checks+" stock persistence/announcement checks; "+wins+" wins in 100000 stock rolls.");
 }
}

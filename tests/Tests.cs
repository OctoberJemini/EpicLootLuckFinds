using System;
using LocalEpicLootLuckyFinds;
class Tests
{
 static int checks;
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static void Main(){
  // Every possible boss state, including out-of-order kills, must stop at the first undefeated boss.
  for(int mask=0;mask<256;mask++){
   var bosses=new bool[8];int expected=7;
   for(int i=0;i<8;i++)bosses[i]=(mask&(1<<i))!=0;
   for(int i=0;i<8;i++)if(!bosses[i]){expected=i;break;}
   int cap=Policy.Ceiling(bosses);Check(cap==expected,"Contiguous progression "+mask);
   for(int native=0;native<8;native++){
    Check(Policy.Target(native,cap,false)==Math.Min(native,expected),"Normal exact region/cap");
    Check(Policy.Target(native,cap,true)==native,"Lucky never above native region");
   }
  }
  Check(Policy.Ceiling(new[]{true,true,false,false,false,false,false,false})==2,"Before Bonemass is Swamp");
  Check(Policy.Ceiling(new[]{true,true,true,true,false,false,false,false})==4,"After Moder is Plains");
  Check(Policy.Target(4,2,false)==2,"Deathsquito before Bonemass normal Swamp");
  Check(Policy.Target(4,2,true)==4,"Deathsquito lucky Plains, never Mistlands");
  Check(Policy.Target(3,2,true)==3,"Wolf lucky Mountains");
  Check(Policy.Target(1,4,false)==1,"Lured Greydwarf remains Black Forest");
  Check(Policy.Target(5,4,true)==5,"Mistlands creature after Moder lucky Mistlands");
  Check(Policy.Target(6,4,true)==6,"Ashlands creature after Moder lucky Ashlands");
  Check(Policy.Target(-1,2,true)==-1,"Unknown native region not guessed");
  Check(Policy.Target(2,-1,false)==-1,"Unknown progression not guessed");
  Check(Policy.Ceiling(new bool[0])==-1,"Empty biome definitions");
  Check(!Policy.Wins(0,0),"Zero chance");Check(!Policy.Wins(0.005,0.5),"Threshold excluded");
  Check(Policy.Wins(0.004999,0.5),"Below threshold");Check(!Policy.Wins(0,double.NaN),"NaN");
  int wins=0;for(int i=0;i<100000;i++)if(Policy.Wins(i/100000.0,0.5))wins++;
  Check(wins==500,"0.5 percent across 100000 samples");
  for(int count=1;count<=8;count++)for(int region=0;region<count;region++) {
   Check(Policy.BountyTarget(region,count,false)==region,"Bounty normal ignores progression cap");
   Check(Policy.BountyTarget(region,count,true)==Math.Min(region+1,count-1),"Bounty exactly one region higher, final region capped");
  }
  Check(Policy.BountyTarget(-1,8,true)==-1,"Unknown bounty region rejected");
  Check(Policy.BountyTarget(8,8,true)==-1,"Out of bounds bounty region rejected");
  for(int mask=0;mask<8;mask++)Check(Policy.AcceptedBounty((mask&1)!=0,(mask&2)!=0,(mask&4)!=0)==(mask==5),"Only accepted main target qualifies");
  for(int count=1;count<=8;count++)for(int cap=0;cap<count;cap++) {
   int target=Policy.VendorTarget(cap,count);
   Check(target==(cap==count-1?-1:cap+1),"Vendor exactly one above progression; no final-tier upgrade");
   for(int candidate=0;candidate<count;candidate++) {
    Check(Policy.VendorCandidate(target,candidate,"Swords","Swords",true)==(target>=0 && candidate==target),"Vendor cannot skip tiers");
    Check(!Policy.VendorCandidate(target,candidate,"Swords","Bows",true),"Vendor category must match");
    Check(!Policy.VendorCandidate(target,candidate,"Swords","Swords",false),"Unavailable vendor candidate excluded");
   }
  }
  Check(Policy.VendorTarget(2,8)==3,"Before Bonemass vendor jackpot Mountains");
  Check(Policy.VendorTarget(4,8)==5,"After Moder vendor jackpot Mistlands");
  Check(Policy.VendorTarget(-1,8)==-1,"No guessed vendor progression");
  Check(!Policy.VendorCandidate(3,3,null,null,true),"No guessed vendor category");
  Console.WriteLine("PASS: "+checks+" regional progression checks, including 100,000 probability samples.");
 }
}

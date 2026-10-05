using System;
using System.Collections.Generic;
using Tossup;
using Tossup.UI;

static class LatestTests
{
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("latest rules: "+message);}
    static bool Near(double a,double b)=>Math.Abs(a-b)<1e-9;

    public static void Run()
    {
        Check(Content.CoinOrder.Count==57&&Content.Coins.Count==57,"all 57 coins are registered");
        Check(Content.ItemOrder.Count==11&&Content.Items.Count==11,"all 11 chips are registered");
        Check(Game.Route.Count==8&&Game.Route[7].Boss,"eight-stage route ends at The House");
        Check(Game.Stakes.Count==8&&Game.Modifiers.Count==8,"stakes and modifiers are complete");
        Check(Game.Contracts.Count==5&&Game.Encounters.Count==5&&Game.AugmentDefs.Count==8,"run systems are complete");

        var slots=Game.New(101,"blade",null,null,false);
        slots.Phase=Phase.Shop;slots.Player.Gold=100;
        Check(slots.Slots==5&&Game.SlotPrice(slots)==5&&Game.BuySlot(slots)&&slots.Slots==6,"deck slot purchase");
        Check(slots.Player.Gold==95,"slot price is charged");

        var stake=Game.New(102,"blade",null,null,false,8);
        Check(Near(Game.Rule(stake,"quota_mult",1),1.8)&&Near(Game.Rule(stake,"exchange_max",3),2),"cumulative stake rules");

        var tied=Game.New(103,"trader",null,null,false);
        var loaded=tied.Coins.Find(c=>c.Id=="loaded");
        Check(loaded!=null&&Near(Content.Coins["loaded"].TieProbability,.23)&&Game.TieEffects("loaded").Count==2,"Edge outcome data");

        var upgraded=Game.New(104,"blade",null,null,false);
        var normal=upgraded.Coins.Find(c=>c.Id=="normal");
        normal.Upgrade="mathematician";
        Check(Near(Game.Probability(upgraded,normal),.75),"coin upgrade changes Heads odds");

        var contract=Game.New(105,"blade",null,null,true);
        Check(Game.OfferContract(contract)&&contract.Phase==Phase.Contract,"contract offer phase");
        string contractId=contract.Encounter.ContractOptions[0];
        Check(Game.ChooseContract(contract,contractId)&&contract.Encounter.Contract.Id==contractId,"contract selection");

        var bet=Game.New(106,"blade",null,null,false);bet.Player.Gold=50;
        var quote=Game.SideBetQuote(bet,Side.Heads);
        Check(quote!=null&&Game.PlaceSideBet(bet,Side.Heads)&&bet.Player.Gold==50-quote.Stake,"side bet placement");

        var augment=Game.New(107,"blade",null,null,false);augment.Phase=Phase.Shop;augment.EncounterIndex=2;
        Check(Game.OfferAugment(augment,3)&&augment.Phase==Phase.Augment&&augment.AugmentOptions.Count==3,"augment offer phase");
        string augmentId=augment.AugmentOptions[0];Check(Game.ChooseAugment(augment,augmentId),"augment selection");
        if(augment.AugmentPending!=null){var choices=Game.AugmentChoices(augment);Check(choices.Count>0&&Game.ChooseAugmentOption(augment,choices[0].Key),"augment follow-up choice");}
        Check(augment.Augments.Contains(augmentId),"augment is owned");

        var flow=Game.New(108,"blade",null,null,false);
        while(flow.Phase!=Phase.Victory)
        {
            Check(flow.Phase==Phase.Encounter,"route enters an encounter");flow.Encounter.Cleared=true;flow.Pending=null;flow.Mulligan=null;
            Check(Game.EndLevel(flow),"cleared level closes");if(flow.Phase==Phase.Victory)break;
            Check(Game.NextEncounter(flow),"route advances");
            if(flow.Phase==Phase.Augment){string id=flow.AugmentOptions[0];Check(Game.ChooseAugment(flow,id),"route augment chosen");if(flow.AugmentPending!=null){var choices=Game.AugmentChoices(flow);Check(choices.Count>0&&Game.ChooseAugmentOption(flow,choices[0].Key),"route augment option chosen");}}
        }
        Check(flow.EncounterIndex==8,"route reaches the eighth-stage boss");

        var contractView=Game.New(109,"blade",null,null,true);Game.OfferContract(contractView);Ui.Game=contractView;AppCore.Draw();
        var augmentView=Game.New(110,"blade",null,null,false);augmentView.Phase=Phase.Shop;augmentView.EncounterIndex=2;Game.OfferAugment(augmentView,3);Ui.Game=augmentView;AppCore.Draw();Ui.Game=null;

        var profile=Profile.New();Check(Profile.MaxStake(profile,"blade")==1,"new profile starts on stage 1");
        Check(Profile.RecordStakeWin(profile,"blade",1)==2&&Profile.MaxStake(profile,"blade")==2,"winning unlocks next stake");
        string json=Profile.Encode(profile);var restored=Profile.Decode(json);
        Check(Profile.MaxStake(restored,"blade")==2,"stake progression survives JSON");
        FeatureParityTests.Run();
        Console.WriteLine("latest: focused gameplay checks passed");
    }
}

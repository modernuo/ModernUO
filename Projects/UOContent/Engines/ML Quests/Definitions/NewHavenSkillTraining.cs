using System.Collections.Generic;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions
{
    public class CleansingOldHaven : MLQuest
    {
        public CleansingOldHaven()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077719; // Cleansing Old Haven
            Description =
                1077722; // Head East out of town to Old Haven. Consecrate your weapon, cast Divine Fury, and battle monsters there until you have raised your Chivalry skill to 50.<br><center>------</center><br>Hail, friend. The life of a Paladin is a life of much sacrifice, humility, bravery, and righteousness. If you wish to pursue such a life, I have an assignment for you. Adventure east to Old Haven, consecrate your weapon, and lay to rest the undead that inhabit there.<br><br>Each ability a Paladin wishes to invoke will require a certain amount of "tithing points" to use. A Paladin can earn these tithing points by donating gold at a shrine or holy place. You may tithe at this shrine.<br><br>Return to me once you feel that you are worthy of the rank of Apprentice Paladin.
            RefusalMessage = 1077723; // Farewell to you my friend. Return to me if you wish to live the life of a Paladin.
            InProgressMessage =
                1077724; // There are still more undead to lay to rest. You still have more to learn. Return to me once you have done so.
            CompletionMessage =
                1077726; // Well done, friend. While I know you understand Chivalry is its own reward, I would like to reward you with something that will protect you in battle. It was passed down to me when I was a lad. Now, I am passing it on you. It is called the Bulwark Leggings. Thank you for your service.
            CompletionNotice =
                1077725; // You have achieved the rank of Apprentice Paladin. Return to Aelorn in New Haven to report your progress.

            Objectives.Add(new GainSkillObjective(SkillName.Chivalry, 500, true, true));

            Rewards.Add(new ItemReward(1077727, typeof(BulwarkLeggings))); // Bulwark Leggings
        }
    }

    public class TheRudimentsOfSelfDefense : MLQuest
    {
        public TheRudimentsOfSelfDefense()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077609; // The Rudiments of Self Defense
            Description =
                1077610; // Head East out of town and go to Old Haven. Battle monster there until you have raised your Wrestling skill to 50.Listen up! If you want to learn the rudiments of self-defense, you need toughening up, and there's no better way to toughen up than engaging in combat. Head East out of town to Old Haven and battle the undead there in hand to hand combat. Afraid of dying, you say? Well, you should be! Being an adventurer isn't a bed of posies, or roses, or however that saying goes. If you take a dirt nap, go to one of the nearby wandering healers and they'll get you back on your feet.Come back to me once you feel that you are worthy of the rank Apprentice Wrestler and i will reward you wit a prize.
            RefusalMessage =
                1077611; // Ok, featherweight. come back to me if you want to learn the rudiments of self-defense.
            InProgressMessage =
                1077630; // You have not achived the rank of Apprentice Wrestler. Come back to me once you feel that you are worthy of the rank Apprentice Wrestler and i will reward you with something useful.
            CompletionMessage =
                1077613; // It's about time! Looks like you managed to make it through your self-defense training. As i promised, here's a little something for you. When worn, these Gloves of Safeguarding will increase your awareness and resistances to most elements except poison. Oh yeah, they also increase your natural health regeneration aswell. Pretty handy gloves, indeed. Oh, if you are wondering if your meditation will be hinered while wearing these gloves, it won't be. Mages can wear cloth and leather items without needing to worry about that. Now get out of here and make something of yourself.
            CompletionNotice =
                1077612; // You have achieved the rank of Apprentice Wrestler. Return to Dimethro in New Haven to receive your prize.

            Objectives.Add(new GainSkillObjective(SkillName.Wrestling, 500, true, true));

            Rewards.Add(new ItemReward(1077614, typeof(GlovesOfSafeguarding))); // Gloves Of Safeguarding
        }
    }

    public class CrushingBonesAndTakingNames : MLQuest
    {
        public CrushingBonesAndTakingNames()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1078070; // Crushing Bones and Taking Names
            Description =
                1078065; // Head East out of town and go to Old Haven. While wielding your mace,battle monster there until you have raised your Mace Fighting skill to 50. I see you want to learn a real weapon skill and not that toothpick training Jockles hasto offer. Real warriors are called Armsmen, and they wield mace weapons. No doubt about it. Nothing is more satisfying than knocking the wind out of your enemies, smashing there armor, crushing their bones, and taking there names. Want to learn how to wield a mace? Well i have an assignment for you. Head East out of town and go to Old Haven. Undead have plagued the town, so there are plenty of bones for you to smash there. Come back to me after you have ahcived the rank of Apprentice Armsman, and i will reward you with a real weapon.
            RefusalMessage =
                1078068; // I thought you wanted to be an Armsman and really make something of yourself. You have potential, kid, but if you want to play with toothpicks, run to Jockles and he will teach you how to clean your teeth with a sword. If you change your mind, come back to me, and i will show you how to wield a real weapon.
            InProgressMessage =
                1078067; // Listen kid. There are a lot of undead in Old Haven, and you haven't smashed enough of them yet. So get back there and do some more cleansing.
            CompletionMessage =
                1078069; // Now that's what I'm talking about! Well done! Don't you like crushing bones and taking names? As i promised, here is a war mace for you. It hits hard. It swings fast. It hits often. What more do you need? Now get out of here and crush some more enemies!
            CompletionNotice =
                1078068; // You have achieved the rank of Apprentice Armsman. Return to Churchill in New Haven to claim your reward.

            Objectives.Add(new GainSkillObjective(SkillName.Macing, 500, true, true));

            Rewards.Add(new ItemReward(1078062, typeof(ChurchillsWarMace))); // Churchill's War Mace
        }
    }

    public class SwiftAsAnArrow : MLQuest
    {
        public SwiftAsAnArrow()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1078201; // Swift as an Arrow
            Description =
                1078205; // Head East out of town and go to Old Haven. While wielding your bow or crossbow, battle monster there until you have raised your Archery skill to 50. Well met, friend. Imagine yourself in a distant grove of trees, You raise your bow, take slow, careful aim, and with the twitch of a finger, you impale your prey with a deadly arrow. You look like you would make a excellent archer, but you will need practice. There is no better way to practice Archery than when you life is on the line. I have a challenge for you. Head East out of town and go to Old Haven. While wielding your bow or crossbow, battle the undead that reside there. Make sure you bring a healthy supply of arrows (or bolts if you prefer a crossbow). If you wish to purchase a bow, crossbow, arrows, or bolts, you can purchase them from me or the Archery shop in town. You can also make your own arrows with the Bowcraft/Fletching skill. You will need fletcher's tools, wood to turn into sharft's, and feathers to make arrows or bolts. Come back to me after you have achived the rank of Apprentice Archer, and i will reward you with a fine Archery weapon.
            RefusalMessage =
                1078206; // I understand that Archery may not be for you. Feel free to visit me in the future if you change your mind.
            InProgressMessage = 1078207; // You're doing great as an Archer! however, you need more practice.
            CompletionMessage =
                1078209; // Congratulation! I want to reward you for your accomplishment. Take this composite bow. It is called " Heartseeker". With it, you will shoot with swiftness, precision, and power. I hope "Heartseeker" serves you well.
            CompletionNotice =
                1078208; // You have achieved the rank of Apprentice Archer. Return to Robyn in New Haven to claim your reward.

            Objectives.Add(new GainSkillObjective(SkillName.Archery, 500, true, true));

            Rewards.Add(new ItemReward(1078210, typeof(Heartseeker))); // Heartseeker
        }
    }

    public class EnGuarde : MLQuest
    {
        public EnGuarde()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1078186; // En Guarde!
            Description =
                1078190; // Head East out of town to Old Haven. Battle monsters there until you have raised your Fencing skill to 50.<br><center>------</center><br>Well hello there, lad. Fighting with elegance and precision is far more enriching than slugging an enemy with a club or butchering an enemy with a sword. Learn the art of Fencing if you want to master combat and look good doing it!<br><br>The key to being a successful fencer is to be the complement and not the opposition to your opponent's strength. Watch for your opponent to become off balance. Then finish him off with finesse and flair.<br><br>There are some undead that need cleansing out in Old Haven towards the East. Head over there and slay them, but remember, do it with style!<br><br>Come back to me once you have achieved the rank of Apprentice Fencer, and I will reward you with a prize.
            RefusalMessage =
                1078191; // I understand, lad. Being a hero isn't for everyone. Run along, then. Come back to me if you change your mind.
            InProgressMessage =
                1078192; // You're doing well so far, but you're not quite ready yet. Head back to Old Haven, to the East, and kill some more undead.
            CompletionMessage =
                1078194; // Excellent! You are beginning to appreciate the art of Fencing. I told you fighting with elegance and precision is more enriching than fighting like an ogre.<br><br>Since you have returned victorious, please take this war fork and use it well. The war fork is a finesse weapon, and this one is magical! I call it "Recaro's Riposte". With it, you will be able to parry and counterstrike with ease! Your enemies will bask in your greatness and glory! Good luck to you, lad, and keep practicing!
            CompletionNotice =
                1078193; // You have achieved the rank of Apprentice Fencer. Return to Recaro in New Haven to claim your reward.

            Objectives.Add(new GainSkillObjective(SkillName.Fencing, 500, true, true));

            Rewards.Add(new ItemReward(1078195, typeof(RecarosRiposte))); // Recaro's Riposte
        }
    }

    public class TheArtOfWar : MLQuest
    {
        public TheArtOfWar()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077667; // The Art of War
            Description =
                1077670; // Head East out of town to Old Haven. Battle monsters there until you have raised your Tactics skill to 50.<br><center>------</center><br>Knowing how to hold a weapon is only half of the battle. The other half is knowing how to use it against an opponent. It's one thing to kill a few bunnies now and then for fun, but a true warrior knows that the right moves to use against a lich will pretty much get your arse fried by a dragon.<br><br>I'll help teach you how to fight so that when you do come up against that dragon, maybe you won't have to walk out of there "OooOOooOOOooOO'ing" and looking for a healer.<br><br>There are some undead that need cleaning out in Old Haven towards the east. Why don't you head on over there and practice killing things?<br><br>When you feel like you've got the basics down, come back to me and I'll see if I can scrounge up an item to help you in your adventures later on.
            RefusalMessage =
                1077671; // That's too bad. I really thought you had it in you. Well, I'm sure those undead will still be there later, so if you change your mind, feel free to stop on by and I'll help you the best I can.
            InProgressMessage =
                1077672; // You're making some progress, that i can tell, but you're not quite good enough to last for very long out there by yourself. Head back to Old Haven, to the east, and kill some more undead.
            CompletionMessage =
                1077674; // Hey, good job killing those undead! Hopefully someone will come along and clean up the mess. All that blood and guts tends to stink after a few days, and when the wind blows in from the east, it can raise a mighty stink!<br><br>Since you performed valiantly, please take these arms and use them well. I've seen a few too many harvests to be running around out there myself, so you might as well take it.<br><br>There is a lot left for you to learn, but I think you'll do fine. Remember to keep your elbows in and stick'em where it hurts the most!
            CompletionNotice =
                1077673; // You have achieved the rank of Apprentice Warrior. Return to Alden Armstrong in New Haven to claim your reward.

            Objectives.Add(new GainSkillObjective(SkillName.Tactics, 500, true, true));

            Rewards.Add(new ItemReward(1077675, typeof(ArmsOfArmstrong))); // Arms of Armstrong
        }
    }

    public class TheWayOfTheBlade : MLQuest
    {
        public TheWayOfTheBlade()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077658; // The way of The Blade
            Description =
                1077661; // Head East out of town and go to Old Haven. While wielding your sword, battle monster there until you have raised your Swordsmanship skill to 50. *as you approach, you notice Jockles sizing you up with a skeptical look on his face* i can see you want to learn how to handle a blade. It's a lot harder than it looks, and you're going to have to put alot of time and effort if you ever want to be half as good as i am. I'll tell you what, kid, I'll help you get started, but you're going to have to do all the work if you want to learn something. East of here, outside of town, is Old Haven. It's been overrun with the nastiest of undead you've seen, which makes it a perfect place for you to turn that sloppy grin on your face into actual skill at handling a sword. Make sure you have a sturdy Swordsmanship weapon in good repair before you leave. 'tis no fun to travel all the way down there just to find out you forgot your blade! When you feel that you've cut down enough of those foul smelling things to learn how to handle a blade without hurting yourself, come back to me. If i think you've improved enough, I'll give you something suited for a real warrior.
            RefusalMessage =
                1077662; // Ha! I had a feeling you were a lily-livered pansy. You might have potential, but you're scared by a few smelly undead, maybe it's better that you stay away from sharp objects. After all, you wouldn't want to hurt yourself swinging a sword. If you change your mind, I might give you another chance...maybe.
            InProgressMessage =
                1077663; // *Jockles looks you up and down* Come on! You've got to work harder than that to get better. Now get out of here, go kill some more of those undead to the east in Old Haven, and don't come back till you've got real skill.
            CompletionMessage =
                1077665; // Well, well, look at what we have here! You managed to do it after all. I have to say, I'm a little surprised that you came back in one piece, but since you did. I've got a little something for you. This is a fine blade that served me well in my younger days. Of course I've got much better swords at my disposal now, so I'll let you go ahead and use it under one condition. Take goodcare of it and treat it with the respect that a fine sword deserves. You're one of the quickers learners I've seen, but you still have a long way to go. Keep at it, and you'll get there someday. Happy hunting, kid.
            CompletionNotice =
                1077664; // You have achieved the rank of Apprentice Swordsman. Return to Jockles in New Haven to see what kind of reward he has waiting for you. Hopefully he'll be a little nicer this time!

            Objectives.Add(new GainSkillObjective(SkillName.Swords, 500, true, true));

            Rewards.Add(new ItemReward(1077666, typeof(JocklesQuicksword))); // Jockles' Quicksword
        }
    }

    public class ThouAndThineShield : MLQuest
    {
        public ThouAndThineShield()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077704; // Thou and Thine Shield
            Description =
                1077707; // Head East out of town and go to Old Haven. Battle monsters, or simply let them hit you, while holding a shield or a weapon until you have raised your Parrying skill to 50. Oh, hello. You probably want me to teach you how to parry, don't you? Very Well. First, you'll need a weapon or a shield. Obviously shields work best of all, but you can parry with a 2-handed weapon. Or if you're feeling particularly brave, a 1-handed weapon will do in a pinch, I'd advise you to go to Old Haven, which you'll find to the East, and practice blocking incoming blows from the undead there. You'll learn quickly if you have more than one opponent attacking you at the same time to practice parrying lots of blows at once. That's the quickest way to master the art of parrying. If you manage to improve your skill enough, i have a shield that you might find useful. Come back to me when you've trained to an apprentice level.
            RefusalMessage =
                1077708; // It's your choice, obviously, but I'd highly suggest that you learn to parry before adventuring out into the world. Come talk to me again when you get tired of being beat on by your opponents
            InProgressMessage =
                1077709; // You're doing well, but in my opinion, I Don't think you really want to continue on without improving your parrying skill a bit more. Go to Old Haven, to the East, and practice blocking blows with a shield.
            CompletionMessage =
                1077711; // Well done! You're much better at parrying blows than you were when we first met. You should be proud of your new ability and I bet your body is greatful to you aswell. *Tyl Ariadne laughs loudly at his ownn (mostly lame) joke*	Oh yes, I did promise you a shield if I thought you were worthy of having it, so here you go. My father made these shields for the guards who served my father faithfully for many years, and I just happen to have obe that i can part with. You should find it useful as you explore the lands.Good luck, and may the Virtues be your guide.
            CompletionNotice =
                1077710; // You have achieved the rank of Apprentice Warrior (for Parrying). Return to Tyl Ariadne in New Haven as soon as you can to claim your reward.

            Objectives.Add(new GainSkillObjective(SkillName.Parry, 500, true, true));

            Rewards.Add(new ItemReward(1077694, typeof(EscutcheonDeAriadne))); // Escutcheon de Ariadne
        }
    }

    public class DefyingTheArcane : MLQuest
    {
        public DefyingTheArcane()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077621; // Defying the Arcane
            Description =
                1077623; // Head East out of town and go to Old Haven. Battle spell casting monsters there until you have raised your Resisting Spells skill to 50.<br><center>------</center><br>Hail and well met! To become a true master of the arcane art of Magery, I suggest learning the complementary skill known as Resisting Spells. While the name of this skill may suggest that it helps with resisting all spells, this is not the case. This skill helps you lessen the severity of spells that lower your stats or ones that last for a specific duration of time. It does not lessen damage from spells such as Energy Bolt or Flamestrike.<BR><BR>The Magery spells that can be resisted are Clumsy, Curse, Feeblemind, Mana Drain, Mana Vampire, Paralyze, Paralyze Field, Poison, Poison Field, and Weaken.<BR><BR>The Necromancy spells that can be resisted are Blood Oath, Corpse Skin, Mind Rot, and Pain Spike.<BR><BR>At higher ranks, the Resisting Spells skill also benefits you by adding a bonus to your minimum elemental resists. This bonus is only applied after all other resist modifications - such as from equipment - has been calculated. It's also not cumulative. It compares the number of your minimum resists to the calculated value of your modifications and uses the higher of the two values.<BR><BR>As you can see, Resisting Spells is a difficult skill to understand, and even more difficult to master. This is because in order to improve it, you will have to put yourself in harm's way - as in the path of one of the above spells.<BR><BR>Undead have plagued the town of Old Haven. We need your assistance in cleansing the town of this evil influence. Old Haven is located east of here. Battle the undead spell casters that inhabit there.<BR><BR>Comeback to me once you feel that you are worthy of the rank of Apprentice Mage and I will reward you with an arcane prize.
            RefusalMessage =
                1077624; // The ability to resist powerful spells is a taxing experience. I understand your resistance in wanting to pursue it. If you wish to reconsider, feel free to return to me for Resisting Spells training. Good journey to you!
            InProgressMessage =
                1077632; // You have not achieved the rank of Apprentice Mage. Come back to me once you feel that you are worthy of the rank of Apprentice Mage and I will reward you with an arcane prize.
            CompletionMessage =
                1077626; // You have successfully begun your journey in becoming a true master of Magery. On behalf of the New Haven Mage Council I wish to present you with this bracelet. When worn, the Bracelet of Resilience will enhance your resistances vs. the elements, physical, and poison harm. The Bracelet of Resilience also magically enhances your ability fend off ranged and melee attacks. I hope it serves you well.
            CompletionNotice =
                1077625; // You have achieved the rank of Apprentice Mage (for Resisting Spells). Return to Alefian in New Haven to receive your arcane prize.

            Objectives.Add(new GainSkillObjective(SkillName.MagicResist, 500, true, true));

            Rewards.Add(new ItemReward(1077627, typeof(BraceletOfResilience))); // Bracelet of Resilience
        }
    }

    public class StoppingTheWorld : MLQuest
    {
        public StoppingTheWorld()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077597; // Stopping the World
            Description =
                1077598; // Head East out of town and go to Old Haven. Use spells and abilities to deplete your mana and meditate there until you have raised your Meditation skill to 50.	Well met! I can teach you how to 'Stop the World' around you and focus your inner energies on replenishing you mana. What is mana? Mana is the life force for everyone who practices arcane arts. When a practitioner of magic invokes a spell or scribes a scroll. It consumes mana. Having a abundant supply of mana is vital to excelling as a practitioner of the arcane. Those of us who study the art of Meditation are also known as stotics. The Meditation skill allows stoics to increase the rate at which they regenerate mana A Stoic needs to perform abilities or cast spells to deplete mana before he can meditate to replenish it. Meditation can occur passively or actively. Actively Meditation is more difficult to master but allows for the stoic to replenish mana at a significantly faster rate. Metal armor inerferes with the regenerative properties of Meditation. It is wise to wear leather or cloth protection when meditating. Head east out of town and go to Old Haven. Use spells and abilities to deplete your mana and actively meditate to replenish it.	Come back once you feel you are at the worthy rank of Apprentice Stoic and i will reward you with a arcane prize.
            RefusalMessage = 1077599; // Seek me out if you ever wish to study the art of Meditation. Good journey.
            InProgressMessage =
                1077628; // You have not achieved the rank of Apprentice Stoic. Come back to me once you feel that you are worthy of the rank Apprentice Stoic and i will reward you with a arcane prize.
            CompletionMessage =
                1077626; // You have successfully begun your journey in becoming a true master of Magery. On behalf of the New Haven Mage Council I wish to present you with this bracelet. When worn, the Bracelet of Resilience will enhance your resistances vs. the elements, physical, and poison harm. The Bracelet of Resilience also magically enhances your ability fend off ranged and melee attacks. I hope it serves you well.
            CompletionNotice =
                1077600; // You have achieved the rank of Apprentice Stoic (for Meditation). Return to Gustar in New Haven to receive your arcane prize.

            Objectives.Add(new GainSkillObjective(SkillName.Meditation, 500, true, true));

            Rewards.Add(new ItemReward(1077602, typeof(PhilosophersHat))); // Philosopher's Hat
        }
    }

    public class ScribingArcaneKnowledge : MLQuest
    {
        public ScribingArcaneKnowledge()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077615; // Scribing Arcane Knowledge
            Description =
                1077616; // While here at the New Haven Magery Library, use a scribe's pen and scribe and 3rd and 4th circle Magery scrolls that you have in your spellbook. Remember, you will need blank scrolls as well. Do this until you have raised your Inscription skill to 50.<br><center>------</center><br>Greetings and welcome to the New Haven Magery Library! You wish to learn how to scribe spell scrolls? You have come to the right place! Inscribed in a steady hand and imbued with the power of reagents, a scroll can mean the difference between life and death in a perilous situation. Those knowledgeable in Inscription may transcribe spells to create useful and valuable magical scrolls.<BR><BR>Before you inscribe a spell, you must first be able to cast the spell without the aid of a scroll. This means that you need the appropriate level of proficiency as a mage, the required mana, and the required reagents. Second, you will need a blank scroll to write on and a scribe's pen. Then, you will need to decide which particular spell you wish to scribe. It may sound easy, but there is a bit more to it. As with the development of all skills, you need to practice Inscription of lower level spells before you can move onto the more difficult ones.<BR><BR>The most important aspect of Inscription is mana. Inscribing a scroll with a magic spell drains your mana. When inscribing 3rd circle or lower spells this will not be much of a problem for these spells consume a small amount of mana. However, when you are inscribing higher circle spells, you may see your mana drain rapidly. When this happens, pause or meditate before continuing.<BR><BR>I suggest you begin scribing any 3rd and 4th circle spells that you know. If you don't possess any, you can always barter with one of the local mage merchants or a fellow adventurer that is a seasoned Scribe.<BR><BR>Come back to me once you feel that you are worthy of the rank of Apprentice Scribe and I will reward you with an arcane prize.
            RefusalMessage =
                1077617; // I understand. When you are ready, feel free to return to me for Inscription training. Thanks for stopping by!
            InProgressMessage =
                1077631; // You have not achieved the rank of Apprentice Scribe. Come back to me once you feel that you are worthy of the rank Apprentice Scribe and i will reward you with a arcane prize.
            CompletionMessage =
                1077619; // Scribing is a very fulfilling pursuit. I am pleased to see you embark on this journey. You sling a pen well! On behalf of the New Haven Mage Council I wish to present you with this spellbook. When equipped, the Hallowed Spellbook greatly enhances the potency of your offensive spells when used against Undead. Be mindful, though. While this book is equipped, when you invoke your powerful spells and abilities vs. Humanoids such as other humans, orcs, ettins, trolls, and the like, your offensive spells will diminish in effectiveness. I suggest unequipping the Hallowed Spellbook when battling Humanoids. I hope this spellbook serves you well.
            CompletionNotice =
                1077618; // You have achieved the rank of Apprentice Scribe. Return to Jillian in New Haven to receive your arcane prize.

            Objectives.Add(new GainSkillObjective(SkillName.Inscribe, 500, true, true));

            Rewards.Add(new ItemReward(1077620, typeof(HallowedSpellbook))); // Hallowed Spellbook
        }
    }

    public class TheMagesApprentice : MLQuest
    {
        public TheMagesApprentice()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077576; // The Mage's Apprentice
            Description =
                1077577; // Head East out of town and go to Old Haven. Cast fireballs and lightning bolts against monsters there until you have raised your Magery skill to 50. Greetings. You seek to unlock the secrets of the arcane art of Magery. The New Haven Mage Council has an assignment for you. Undead have plagued the town of Old Haven. We need your assistance in cleansing the town of this evil influence. Old Haven is located east of here. I suggest using your offensive Magery spells such as Fireball and Lightning Bolt against the Undead that inhabit there. Make sure you have plenty of reagents before embarking on your journey. Reagents are required to cast Magery spells. You can purchase extra reagents at the nearby Reagent shop, or you can find reagents growing in the nearby wooded areas. You can see which reagents are required for each spell by looking in your spellbook. Come back to me once you feel that you are worthy of the rank of Apprentice Mage and I will reward you with an arcane prize.
            RefusalMessage =
                1077578; // Very well, come back to me when you are ready to practice Magery. You have so much arcane potential. 'Tis a shame to see it go to waste. The New Haven Mage Council could really use your help.
            InProgressMessage =
                1077579; // You have not achieved the rank of Apprentice Mage. Come back to me once you feel that you are worthy of the rank of Apprentice Mage and I will reward you with an arcane prize.
            CompletionMessage =
                1077581; // Well done! On behalf of the New Haven Mage Council I wish to present you with this staff. Normally a mage must unequip weapons before spell casting. While wielding your new Ember Staff, however, you will be able to invoke your Magery spells. Even if you do not currently possess skill in Mace Fighting, the Ember Staff will allow you to fight as if you do. However, your Magery skill will be temporarily reduced while doing so. Finally, the Ember Staff occasionally smites a foe with a Fireball while wielding it in melee combat. I hope the Ember Staff serves you well.
            CompletionNotice =
                1077580; // You have achieved the rank of Apprentice Mage. Return to Kaelynna in New Haven to receive your arcane prize.

            Objectives.Add(new GainSkillObjective(SkillName.Magery, 500, true, true));

            Rewards.Add(new ItemReward(1077582, typeof(EmberStaff))); // Ember Staff
        }
    }

    public class ScholarlyTask : MLQuest
    {
        public ScholarlyTask()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077603; // A Scholarly Task
            Description =
                1077604; // Head East out of town and go to Old Haven. Use Evaluating Intelligence on all creatures you see there. You can also cast Magery spells as well to raise Evaluating Intelligence. Do these activities until you have raised your Evaluating Intelligence skill to 50.<br><center>------</center><br>Hello. Truly knowing your opponent is essential for landing your offensive spells with precision. I can teach you how to enhance the effectiveness of your offensive spells, but first you must learn how to size up your opponents intellectually. I have a scholarly task for you. Head East out of town and go to Old Haven. Use Evaluating Intelligence on all creatures you see there. You can also cast Magery spells as well to raise Evaluating Intelligence.<BR><BR>Come back to me once you feel that you are worthy of the rank of Apprentice Scholar and I will reward you with an arcane prize.
            RefusalMessage = 1077605; // Return to me if you reconsider and wish to become an Apprentice Scholar.
            InProgressMessage =
                1077629; // You have not achieved the rank of Apprentice Scholar. Come back to me once you feel that you are worthy of the rank of Apprentice Scholar and I will reward you with an arcane prize.
            CompletionMessage =
                1077607; // You have completed the task. Well done. On behalf of the New Haven Mage Council I wish to present you with this ring. When worn, the Ring of the Savant enhances your intellectual aptitude and increases your mana pool. Your spell casting abilities will take less time to invoke and recovering from such spell casting will be hastened. I hope the Ring of the Savant serves you well.
            CompletionNotice =
                1077606; // You have achieved the rank of Apprentice Scholar. Return to Mithneral in New Haven to receive your arcane prize.

            Objectives.Add(new GainSkillObjective(SkillName.EvalInt, 500, true, true));

            Rewards.Add(new ItemReward(1077608, typeof(RingOfTheSavant))); // Ring of the Savant
        }
    }

    public class TheRightToolForTheJob : MLQuest
    {
        public TheRightToolForTheJob()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077741; // The Right Tool for the Job
            Description =
                1077744; // Create new scissors and hammers while inside Amelia's workshop. Try making scissors up to 45 skill, the switch to making hammers until 50 skill.<br><center>-----</center><br>Hello! I guess you're here to learn something about Tinkering, eh? You've come to the right place, as Tinkering is what I've dedicated my life to. <br><br>You'll need two things to get started: a supply of ingots and the right tools for the job. You can either buy ingots from the market, or go mine them yourself. As for tools, you can try making your own set of Tinker's Tools, or if you'd prefer to buy them, I have some for sale.<br><br>Working here in my shop will let me give you pointers as you go, so you'll be able to learn faster than anywhere else. Start off making scissors until you reach 45 tinkering skill, then switch to hammers until you've achieved 50. Once you've done that, come talk to me and I'll give you something for your hard work.
            RefusalMessage =
                1077745; // I’m disappointed that you aren’t interested in learning more about Tinkering. It’s really such a useful skill!<br><br>*Amelia smiles*<br><br>At least you know where to find me if you change your mind, since I rarely spend time outside of this shop.
            InProgressMessage =
                1077746; // Nice going! You're not quite at Apprentice Tinkering yet, though, so you better get back to work. Remember that the quickest way to learn is to make scissors up until 45 skill, and then switch to hammers. Also, don't forget that working here in my shop will let me give you tips so you can learn faster.
            CompletionMessage =
                1077748; // You've done it! Look at our brand new Apprentice Tinker! You've still got quite a lot to learn if you want to be a Grandmaster Tinker, but I believe you can do it! Just keep in mind that if you're tinkering just to practice and improve your skill, make items that are moderately difficult (60-80% success chance), and try to stick to ones that use less ingots.  <br><br>Come here, my brand new Apprentice Tinker, I want to give you something special. I created this just for you, so I hope you like it. It's a set of Tinker's Tools that contains a bit of magic. These tools have more charges than any Tinker's Tools a Tinker can make. You can even use them to make a normal set of tools, so that way you won't ever find yourself stuck somewhere with no tools!
            CompletionNotice =
                1077747; // You have achieved the rank of Apprentice Tinker. Talk to Amelia Youngstone in New Haven to see what kind of reward she has waiting for you.

            Objectives.Add(new GainSkillObjective(SkillName.Tinkering, 500, true, true));

            Rewards.Add(new ItemReward(1077749, typeof(AmeliasToolbox))); // Amelia’s Toolbox
        }
    }

    public class KnowThineEnemy : MLQuest
    {
        public KnowThineEnemy()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077685; // Know Thine Enemy
            Description =
                1077688; // Head East out of town to Old Haven. Battle monsters there, or heal yourself and other players, until you have raised your Anatomy skill to 50.<br><center>------</center><br>Hail and well met. You must be here to improve your knowledge of Anatomy. Well, you've come to the right place because I can teach you what you need to know. At least all you'll need to know for now. Haha!<br><br>Knowing about how living things work inside can be a very useful skill. Not only can you learn where to strike an opponent to hurt him the most, but you can use what you learn to heal wounds better as well. Just walking around town, you can even tell if someone is strong or weak or if they happen to be particularly dexterous or not.<BR><BR>If you're interested in learning more, I'd advise you to head out to Old Haven, just to the east, and jump into the fray. You'll learn best by engaging in combat while keeping you and your fellow adventurers healed, or you can even try sizing up your opponents.<br><br>While you're gone, I'll dig up something you may find useful.
            RefusalMessage =
                1077689; // It's your choice, but I wouldn't head out there without knowing what makes those things tick inside! If you change your mind, you can find me right here dissecting frogs, cats or even the occasional unlucky adventurer.
            InProgressMessage =
                1077690; // I'm surprised to see you back so soon. You've still got a ways to go if you want to really understand the science of Anatomy. Head out to Old Haven and practice combat and healing yourself or other adventurers.
            CompletionMessage =
                1077692; // By the Virtues, you've done it! Congratulations mate! You still have quite a ways to go if you want to perfect your knowledge of Anatomy, but I know you'll get there someday. Just keep at it.<br><br>In the meantime, here's a piece of armor that you might find useful. It's not fancy, but it'll serve you well if you choose to wear it.<br><br>Happy adventuring, and remember to keep your cranium separate from your clavicle!
            CompletionNotice =
                1077691; // You have achieved the rank of Apprentice Healer (for Anatomy). Return to Andreas Vesalius in New Haven as soon as you can to claim your reward.

            Objectives.Add(new GainSkillObjective(SkillName.Anatomy, 500, true, true));

            Rewards.Add(new ItemReward(1077693, typeof(TunicOfGuarding))); // Tunic of Guarding
        }
    }

    public class BruisesBandagesAndBlood : MLQuest
    {
        public BruisesBandagesAndBlood()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077676; // Bruises, Bandages and Blood
            Description =
                1077679; // Head East out of town and go to Old Haven. Heal yourself and other players until you have raised your Healing skill to 50.<br><center>------</center><br>Ah, welcome to my humble practice. I am Avicenna, New Haven's resident Healer. A lot of adventurers head out into the wild from here, so I keep rather busy when they come back bruised, bleeding, or worse.<br><br>I can teach you how to bandage a wound, sure, but it's not a job for the queasy! For some folks, the mere sight of blood is too much for them, but it's something you'll get used to over time. It is one thing to cut open a living thing, but it's quite another to sew it back up and save it from sure death. 'Tis noble work, healing.<br><br>Best way for you to practice fixing up wounds is to head east out to Old Haven and either practice binding up your own wounds, or practice on someone else. Surely they'll be grateful for the assistance.<br><br>Make sure to take enough bandages with you! You don't want to run out in the middle of a tough fight.
            RefusalMessage =
                1077680; // No? Are you sure? Well, when you feel that you're ready to practice your healing, come back to me. I'll be right here, fixing up adventurers and curing the occasional cold!
            InProgressMessage =
                1077681; // Hail! 'Tis good to see you again. Unfortunately, you're not quite ready to call yourself an Apprentice Healer quite yet. Head back out to Old Haven, due east from here, and bandage up some wounds. Yours or someone else's, it doesn't much matter.
            CompletionMessage =
                1077683; // Hello there, friend. I see you've returned in one piece, and you're an Apprentice Healer to boot! You should be proud of your accomplishment, as not everyone has "the touch" when it comes to healing.<br><br>I can't stand to see such good work go unrewarded, so I have something I'd like you to have. It's not much, but it'll help you heal just a little faster, and maybe keep you alive.<br><br>Good luck out there, friend, and don't forget to help your fellow adventurer whenever possible!
            CompletionNotice =
                1077682; // You have achieved the rank of Apprentice Healer. Return to Avicenna in New Haven as soon as you can to claim your reward.

            Objectives.Add(new GainSkillObjective(SkillName.Healing, 500, true, true));

            Rewards.Add(new ItemReward(1077684, typeof(HealersTouch))); // Healer's Touch
        }
    }

    public class TheInnerWarrior : MLQuest
    {
        public TheInnerWarrior()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077696; // The Inner Warrior
            Description =
                1077699; // Head East out of town to Old Haven. Expend stamina and mana until you have raised your Focus skill to 50.<br><center>------</center><br>Well, hello there. Don't you look like quite the adventurer!<br><br>You want to learn more about Focus, do you? I can teach you something about that, but first you should know that not everyone can be disciplined enough to excel at it. Focus is the ability to achieve inner balance in both body and spirit, so that you recover from physical and mental exertion faster than you otherwise would.<br><br>If you want to practice Focus, the best place to do that is east of here, in Old Haven, where you'll find an undead infestation. Exert yourself physically by engaging in combat and moving quickly. For testing your mental balance, expend mana in whatever way you find most suitable to your abilities. Casting spells and using abilities work well for consuming your mana.<br><br>Go. Train hard, and you will find that your concentration will improve naturally. When you've improved your ability to focus yourself at an Apprentice level, come back to me and I shall give you something worthy of your new ability.
            RefusalMessage =
                1077700; // I'm disappointed. You have a lot of inner potential, and it would pain me greatly to see you waste that. Oh well. If you change your mind, I'll be right here.
            InProgressMessage =
                1077701; // Hello again. I see you've returned, but it seems that your Focus skill hasn't improved as much as it could have. Just head east, to Old Haven, and exert yourself physically and mentally as much as possible. To do this physically, engage in combat and move as quickly as you can. For exerting yourself mentally, expend mana in whatever way you find most suitable to your abilities. Casting spells and using abilities work well for consuming your mana.<br><br>Return to me when you have gained enough Focus skill to be considered an Apprentice Stoic.
            CompletionMessage =
                1077703; // Look who it is! I knew you could do it if you just had the discipline to apply yourself. It feels good to recover from battle so quickly, doesn't it? Just wait until you become a Grandmaster, it's amazing!<br><br>Please take this gift, as you've more than earned it with your hard work. It will help you recover even faster during battle, and provides a bit of protection as well.<br><br>You have so much more potential, so don't stop trying to improve your Focus now! Safe travels!
            CompletionNotice =
                1077702; // You have achieved the rank of Apprentice Stoic (for Focus). Return to Sarsmea Smythe in New Haven to see what kind of reward she has waiting for you.

            Objectives.Add(new GainSkillObjective(SkillName.Focus, 500, true, true));

            Rewards.Add(new ItemReward(1077695, typeof(ClaspOfConcentration))); // Clasp of Concentration
        }
    }

    public class TheArtOfStealth : MLQuest
    {
        public TheArtOfStealth()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1078154; // The Art of Stealth
            Description =
                1078158; // Head East out of town and go to Old Haven. While wielding your fencing weapon, battle monsters with focus attack and summon mirror images up to 40 Ninjitsu skill, and continue practicing focus attack on monsters until 50 Ninjitsu skill.<br><center>------</center><br>Welcome, young one. You seek to learn Ninjitsu. With it, and the book of Ninjitsu, a Ninja can evoke a number of special abilities including transforming into a variety of creatures that give unique bonuses, using stealth to attack unsuspecting opponents or just plain disappear into thin air! If you do not have a book of Ninjitsu, you can purchase one from me.<br><br>I have an assignment for you. Head East out of town and go to Old Haven. While wielding your fencing weapon, battle monsters with focus attack and summon mirror images up to Novice rank, and continue focusing your attacks for greater damage on monsters until you become an Apprentice Ninja. Each image will absorb one attack. The art of deception is a strong defense. Use it wisely.<br><br>Come back to me once you have achieved the rank of Apprentice Ninja, and I shall reward you with something useful.
            RefusalMessage = 1078159; // Come back to me if you with to learn Ninjitsu in the future.
            InProgressMessage =
                1078160; // You have not achieved the rank of Apprentice Ninja. Come back to me once you have done so.
            CompletionMessage =
                1078162; // You have done well, young one. Please accept this kryss as a gift. It is called the "Silver Serpent Blade". With it, you will strike with precision and power. This should aid you in your journey as a Ninja. Farewell.
            CompletionNotice =
                1078161; // You have achieved the rank of Apprentice Ninja. Return to Ryuichi in New Haven to see what kind of reward he has waiting for you.

            Objectives.Add(new GainSkillObjective(SkillName.Ninjitsu, 500, true, true));

            Rewards.Add(new ItemReward(1078163, typeof(SilverSerpentBlade))); // Silver Serpent Blade
        }
    }

    public class BecomingOneWithTheShadows : MLQuest
    {
        public BecomingOneWithTheShadows()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1078164; // Becoming One with the Shadows
            Description =
                1078168; // Practice hiding in the Ninja Dojo until you reach 50 Hiding skill.<br><center>------</center><br>Come closer. Don't be afraid. The shadows will not harm you. To be a successful Ninja, you must learn to become one with the shadows. The Ninja Dojo is the ideal place to learn the art of concealment. Practice hiding here.<br><br>Talk to me once you have achieved the rank of Apprentice Rogue (for Hiding), and I shall reward you.
            RefusalMessage = 1078169; // If you wish to become one with the shadows, come back and talk to me.
            InProgressMessage =
                1078170; // You have not achieved the rank of Apprentice Rogue (for Hiding). Talk to me when you feel you have accomplished this.
            CompletionMessage =
                1078172; // Not bad at all. You have learned to control your fear of the dark and you are becoming one with the shadows. If you haven't already talked to Jun, I advise you do so. Jun can teach you how to stealth undetected. Hiding and Stealth are essential skills to master when becoming a Ninja.<br><br>As promised, I have a reward for you. Here are some smokebombs. As long as you are an Apprentice Ninja and have mana available you will be able to use them. They will allow you to hide while in the middle of combat. I hope these serve you well.
            CompletionNotice =
                1078171; // You have achieved the rank of Apprentice Rogue (for Hiding). Return to Chiyo in New Haven to claim your reward.

            Objectives.Add(new GainSkillObjective(SkillName.Hiding, 500, true, true));

            Rewards.Add(new ItemReward(1078173, typeof(BagOfSmokeBombs))); // Bag of Smoke Bombs
        }
    }

    public class WalkingSilently : MLQuest
    {
        public WalkingSilently()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1078174; // Walking Silently
            Description =
                1078178; // Head East out of town and go to Old Haven. While wearing normal clothes, practice Stealth there until you reach 50 Stealth skill.<br><center>------</center><br>You there. You're not very quiet in your movements. I can help you with that. Not only must you must learn to become one with the shadows, but also you must learn to quiet your movements. Old Haven is the ideal place to learn how to Stealth.<br><br>Head East out of town and go to Old Haven. While wearing normal clothes, practice Stealth there. Stealth becomes more difficult as you wear heavier pieces of armor, so for now, only wear clothes while practicing Stealth.<br><br>You can only Stealth once you are hidden.  If you become visible, use your Hiding skill, and begin slowing walking.<br><br>Come back to me once you have achieved the rank of Apprentice Rogue (for Stealth), and I will reward you with something useful.
            RefusalMessage = 1078179; // If you want to learn to quiet your movements, talk to me, and I will help you.
            InProgressMessage =
                1078180; // You have not achieved the rank of Apprentice Rogue (for Stealth). Come back to me when you feel you have accomplished this.
            CompletionMessage =
                1078182; // Good. You have learned to quiet your movements. If you haven't already talked to Chiyo, I advise you do so. Chiyo can teach you how to become one with the shadows. Hiding and Stealth are essential skills to master when becoming a Ninja.<br><br>Here is your reward. This leather Ninja jacket is called "Twilight Jacket". It will offer greater protection to you. I hope this serve you well.
            CompletionNotice =
                1078181; // You have achieved the rank of Apprentice Rogue (for Stealth). Return to Jun in New Haven to claim your reward.

            Objectives.Add(new GainSkillObjective(SkillName.Stealth, 500, true, true));

            Rewards.Add(new ItemReward(1078183, typeof(TwilightJacket))); // Twilight Jacket
        }
    }

    public class EyesOfARanger : MLQuest
    {
        public EyesOfARanger()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1078211; // Eyes of a Ranger
            Description =
                1078217; // Track animals, monsters, and people on Haven Island until you have raised your Tracking skill to 50.<br><center>------</center><br>Hello friend. I am Walker, Grandmaster Ranger. An adventurer needs to keep alive in the wilderness. Being able to track those around you is essential to surviving in dangerous places. Certain Ninja abilities are more potent when the Ninja possesses Tracking knowledge. If you want to be a Ninja, or if you simply want to get a leg up on the creatures that habit these parts, I advise you learn how to track them.<br><br>You can track any animals, monsters, or people on Haven Island. Clear your mind, focus, and note any tracks in the ground or sounds in the air that can help you find your mark. You can do it, friend. I have faith in you.<br><br>Come back to me once you have achieved the rank of Apprentice Ranger (for Tracking), and I will give you something that may help you in your travels. Take care, friend.
            RefusalMessage =
                1078218; // Farewell, friend. Be careful out here. If you change your mind and want to learn Tracking, come back and talk to me.
            InProgressMessage =
                1078219; // So far so good, kid. You are still alive, and you are getting the hang of Tracking. There are many more animals, monsters, and people to track. Come back to me once you have tracked them.
            CompletionMessage =
                1078221; // I knew you could do it! You have become a fine Ranger. Just keep practicing, and one day you will become a Grandmaster Ranger. Just like me.<br><br>I have a little something for you that will hopefully aid you in your journeys. These leggings offer some resistances that will hopefully protect you from harm. I hope these serve you well. Farewell, friend.
            CompletionNotice =
                1078220; // You have achieved the rank of Apprentice Ranger (for Tracking). Return to Walker in New Haven to claim your reward.

            Objectives.Add(new GainSkillObjective(SkillName.Tracking, 500, true, true));

            Rewards.Add(new ItemReward(1078222, typeof(WalkersLeggings))); // Walker's Leggings
        }
    }

    public class TheWayOfTheSamurai : MLQuest
    {
        public TheWayOfTheSamurai()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1078007; // The Way of the Samurai
            Description =
                1078010; // Head East out of town and go to Old Haven. use the Confidence defensive stance and attempt to honorably execute monsters there until you have raised your Bushido skill to 50.<br><center>------</center><br>Greetings. I see you wish to learn the Way of the Samurai. Wielding a blade is easy. Anyone can grasp a sword's hilt. Learning how to fight properly and skillfully is to become an Armsman. Learning how to master weapons, and even more importantly when not to use them, is the Way of the Warrior. The Way of the Samurai. The Code of the Bushido. That is why you are here.<br><br>Adventure East to Old Haven. Use the Confidence defensive stance and attempt to honorably execute the undead that inhabit there. You will need a book of Bushido to perform these abilities. If you do not possess a book of Bushido, you can purchase one from me. <br><br>If you fail to honorably execute the undead, your defenses will be greatly weakened: Resistances will suffer and Resisting Spells will suffer. A successful parry instantly ends the weakness. If you succeed, however, you will be infused with strength and healing. Your swing speed will also be boosted for a short duration. With practice, you will learn how to master your Bushido abilities.<br><br>Return to me once you feel that you have become an Apprentice Samurai.
            RefusalMessage = 1078011; // Good journey to you. Return to me if you wish to live the life of a Samurai.
            InProgressMessage =
                1078012; // You are not ready to become an Apprentice Samurai. There are still more undead to lay to rest. Return to me once you have done so.
            CompletionMessage =
                1078014; // You have proven yourself young one. You will continue to improve as your skills are honed with age. You are an honorable warrior, worthy of the rank of Apprentice Samurai.  Please accept this no-dachi as a gift. It is called "The Dragon's Tail". Upon a successful strike in combat, there is a chance this mighty weapon will replenish your stamina equal to the damage of your attack. I hope "The Dragon's Tail" serves you well. You have earned it. Farewell for now.
            CompletionNotice =
                1078013; // You have achieved the rank of Apprentice Samurai. Return to Hamato in New Haven to report your progress.

            Objectives.Add(new GainSkillObjective(SkillName.Bushido, 500, true, true));

            Rewards.Add(new ItemReward(1078015, typeof(TheDragonsTail))); // The Dragon's Tail
        }
    }

    public class TheAllureOfDarkMagic : MLQuest
    {
        public TheAllureOfDarkMagic()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1078036; // The Allure of Dark Magic
            Description =
                1078039; // Head East out of town and go to Old Haven. Cast Evil Omen and Pain Spike against monsters there until you have raised your Necromancy skill to 50.<br><center>------</center><br>Welcome! I see you are allured by the dark magic of Necromancy. First, you must prove yourself worthy of such knowledge. Undead currently occupy the town of Old Haven. Practice your harmful Necromancy spells on them such as Evil Omen and Pain Spike.<br><br>Make sure you have plenty of reagents before embarking on your journey. Reagents are required to cast Necromancy spells. You can purchase extra reagents from me, or you can find reagents growing in the nearby wooded areas. You can see which reagents are required for each spell by looking in your spellbook.<br><br>Come back to me once you feel that you are worthy of the rank of Apprentice Necromancer and I will reward you with the knowledge you desire.
            RefusalMessage = 1078040; // You are weak after all. Come back to me when you are ready to practice Necromancy.
            InProgressMessage =
                1078041; // You have not achieved the rank of Apprentice Necromancer. Come back to me once you feel that you are worthy of the rank of Apprentice Necromancer and I will reward you with the knowledge you desire.
            CompletionMessage =
                1078043; // You have done well, my young apprentice. Behold! I now present to you the knowledge you desire. This spellbook contains all the Necromancer spells. The power is intoxicating, isn't it?
            CompletionNotice =
                1078042; // You have achieved the rank of Apprentice Necromancer. Return to Mulcivikh in New Haven to receive the knowledge you desire.

            Objectives.Add(new GainSkillObjective(SkillName.Necromancy, 500, true, true));

            Rewards.Add(new InternalReward());
        }

        private class InternalReward : ItemReward
        {
            public InternalReward()
                : base(1078052, typeof(NecromancerSpellbook)) // Complete Necromancer Spellbook
            {
            }

            public override Item CreateItem()
            {
                var item = base.CreateItem();

                if (item is Spellbook book)
                {
                    book.Content = (1ul << book.BookCount) - 1;
                }

                return item;
            }
        }
    }

    public class ChannelingTheSupernatural : MLQuest
    {
        public ChannelingTheSupernatural()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1078044; // Channeling the Supernatural
            Description =
                1078047; // Head East out of town and go to Old Haven. Use Spirit Speak and channel energy from either yourself or nearby corpses there. You can also cast Necromancy spells as well to raise Spirit Speak. Do these activities until you have raised your Spirit Speak skill to 50.<br><center>------</center><br>How do you do? Channeling the supernatural through Spirit Speak allows you heal your wounds. Such channeling expends your mana, so be mindful of this. Spirit Speak enhances the potency of your Necromancy spells. The channeling powers of a Medium are quite useful when practicing the dark magic of Necromancy.<br><br>It is best to practice Spirit Speak where there are a lot of corpses. Head East out of town and go to Old Haven. Undead currently reside there. Use Spirit Speak and channel energy from either yourself or nearby corpses. You can also cast Necromancy spells as well to raise Spirit Speak.<br><br>Come back to me once you feel that you are worthy of the rank of Apprentice Medium and I will reward you with something useful.
            RefusalMessage =
                1078048; // Channeling the supernatural isn't for everyone. It is a dark art. See me if you ever wish to pursue the life of a Medium.
            InProgressMessage =
                1078049; // Back so soon? You have not achieved the rank of Apprentice Medium. Come back to me once you feel that you are worthy of the rank of Apprentice Medium and I will reward you with something useful.
            CompletionMessage =
                1078051; // Well done! Channeling the supernatural is taxing, indeed. As promised, I will reward you with this bag of Necromancer reagents. You will need these if you wish to also pursue the dark magic of Necromancy. Good journey to you.
            CompletionNotice =
                1078050; // You have achieved the rank of Apprentice Medium. Return to Morganna in New Haven to receive your reward.

            Objectives.Add(new GainSkillObjective(SkillName.SpiritSpeak, 500, true, true));

            Rewards.Add(new ItemReward(1078053, typeof(BagOfNecromancerReagents))); // Bag of Necromancer Reagents
        }
    }

    public class TheDeluciansLostMine : MLQuest
    {
        public TheDeluciansLostMine()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077750; // The Delucian’s Lost Mine
            Description =
                1077753; // Find Jacob's Lost Mine and mine iron ore there, using a pickaxe or shovel. Bring it back to Jacob's forge and smelt the ore into ingots, until you have raised your Mining skill to 50. You may find a packhorse useful for hauling the ore around. The animal trainer in New Haven has packhorses for sale.<br><center>-----</center><br>Howdy! Welcome to my camp. It's not much, I know, but it's all I'll be needin' up here. I don't need them fancy things those townspeople have down there in New Haven. Nope, not one bit. Just me, Bessie, my pick and a thick vein 'o valorite.<br><br>Anyhows, I'm guessin' that you're up here to ask me about minin', aren't ya? Well, don't be expectin' me to tell you where the valorite's at, cause I ain't gonna tell the King of Britannia, much less the likes of you. But I will show ya how to mine and smelt iron, cause there certainly is a 'nough of up in these hills.<br><br>*Jacob looks around, with a perplexed look on his face*<br><br>Problem is, I can't remember where my iron mine's at, so you'll have to find it yourself. Once you're there, have at it with a pickaxe or shovel, then haul it back to camp and I'll show ya how to smelt it. Ya look a bit wimpy, so you might wanna go buy yourself a packhorse in town from the animal trainer to help you haul around all that ore.<br><br>When you're an Apprentice Miner, talk to me and I'll give ya a little somethin' I've got layin' around here... somewhere.
            RefusalMessage =
                1077754; // Couldn’t find my iron mine, could ya? Well, neither can I!<br><br>*Jacob laughs*<br><br>Oh, ya don’t wanna find it? Well, allrighty then, ya might as well head on back down to town then and stop cluttering up my camp. Come back and talk to me if you’re interested in learnin’ ‘bout minin’.
            InProgressMessage =
                1077755; // Where ya been off a gallivantin’ all day, pilgrim? You ain’t seen no hard work yet! Get yer arse back out there to my mine and dig up some more iron. Don’t forget to take a pickaxe or shovel, and if you’re so inclined, a packhorse too.
            CompletionMessage =
                1077757; // Dang gun it! If that don't beat all! Ya went and did it, didn’t ya? What we got ourselves here is a mighty fine brand spankin’ new Apprentice Miner!<br><br>I can see ya put some meat on them bones too while you were at it!<br><br>Here’s that little somethin’ I told ya I had for ya. It’s a pickaxe with some high falutin’ magic inside that’ll help you find the good stuff when you’re off minin’. It wears out fast, though, so you can only use it a few times a day.<br><br>Welp, I’ve got some smeltin’ to do, so off with ya. Good luck, pilgrim!
            CompletionNotice =
                1077756; // You have achieved the rank of Apprentice Miner. Return to Jacob Waltz in at his camp in the hills above New Haven as soon as you can to claim your reward.

            Objectives.Add(new GainSkillObjective(SkillName.Mining, 500, true, true));

            Rewards.Add(new ItemReward(1077758, typeof(JacobsPickaxe))); // Jacob's Pickaxe
        }
    }

    public class ItsHammerTime : MLQuest
    {
        public ItsHammerTime()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1077732; // It’s Hammer Time!
            Description =
                1077735; // Create new daggers and maces using the forge and anvil in George's shop. Try making daggers up to 45 skill, the switch to making maces until 50 skill.<br><center>-----</center><br>Hail, and welcome to my humble shop. I'm George Hephaestus, New Haven's blacksmith. I assume that you're here to ask me to train you to be an Apprentice Blacksmith. I certainly can do that, but you're going to have to supply your own ingots.<br><br>You can always buy them at the market, but I highly suggest that you mine your own. That way, any items you sell will be pure profit!<br><br>So, once you have a supply of ingots, use my forge and anvil here to create items. You'll also need a supply of the proper tools; you can use a smith's hammer, a sledgehammer or tongs. You can either make them yourself if you have the tinkering skill, or buy them from a tinker at the market.<br><br>Since I'll be around to give you advice, you'll learn faster here than anywhere else. Start off making daggers until you reach 45 blacksmithing skill, then switch to maces until you've achieved 50. Once you've done that, come talk to me and I'll give you something for your hard work.
            RefusalMessage =
                1077736; // You're not interested in learning to be a smith, eh? I thought for sure that's why you were here. Oh well, if you change your mind, you can always come back and talk to me.
            InProgressMessage =
                1077737; // You’re doing well, but you’re not quite there yet. Remember that the quickest way to learn is to make daggers up until 45 skill, and then switch to maces. Also, don’t forget that using my forge and anvil will help you learn faster.
            CompletionMessage =
                1077739; // I've been watching you get better and better as you've been smithing, and I have to say, you're a natural! It's a long road to being a Grandmaster Blacksmith, but I have no doubt that if you put your mind to it you'll get there someday. Let me give you one final piece of advice. If you're smithing just to practice and improve your skill, make items that are moderately difficult (60-80% success chance), and try to stick to ones that use less ingots.<br><br>Now that you're an Apprentice Blacksmith, I have something for you. While you were busy practicing, I was crafting this hammer for you. It's finely balanced, and has a bit of magic imbued within that will help you craft better items. However, that magic needs to restore itself over time, so you can only use it so many times per day. I hope you find it useful!
            CompletionNotice =
                1077738; // You have achieved the rank of Apprentice Blacksmith. Return to George Hephaestus in New Haven to see what kind of reward he has waiting for you.

            Objectives.Add(new GainSkillObjective(SkillName.Blacksmith, 500, true, true));

            Rewards.Add(new ItemReward(1077740, typeof(HammerOfHephaestus))); // Hammer of Hephaestus
        }
    }

    public class Aelorn : KeeperOfChivalry
    {
        [Constructible]
        public Aelorn()
        {
            Title = "the Chivalry Instructor";
            Body = 0x190;
            Hue = 0x83EA;
            HairItemID = 0x203C;
            HairHue = 0x47D;
            FacialHairItemID = 0x204D;
            FacialHairHue = 0x47D;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.Anatomy, 120.0);
            SetSkill(SkillName.MagicResist, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Swords, 120.0);
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.Focus, 120.0);
            SetSkill(SkillName.Chivalry, 120.0);
        }

        public Aelorn(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Aelorn";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078133); // Hail, friend. Want to live the life of a paladin?
        }

        public override void InitOutfit()
        {
            AddItem(new Backpack());
            AddItem(new VikingSword());
            AddItem(new PlateChest());
            AddItem(new PlateLegs());
            AddItem(new PlateGloves());
            AddItem(new PlateArms());
            AddItem(new PlateGorget());
            AddItem(new OrderShield());
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Dimethro : BaseCreature
    {
        [Constructible]
        public Dimethro() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Wrestling Instructor";
            Body = 0x190;
            Hue = 0x83EA;
            HairItemID = 0x203D;
            HairHue = 0x455;
            FacialHairItemID = 0x204D;
            FacialHairHue = 0x455;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.EvalInt, 120.0);
            SetSkill(SkillName.Inscribe, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.MagicResist, 120.0);
            SetSkill(SkillName.Wrestling, 120.0);
            SetSkill(SkillName.Meditation, 120.0);

            AddItem(new Backpack());
            AddItem(new Sandals(0x455));
            AddItem(new BodySash(0x455));
            AddItem(new LongPants(0x455));
        }

        public Dimethro(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Dimethro";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078128); // You there! Wanna master hand to hand defense? Of course you do!
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Churchill : BaseCreature
    {
        [Constructible]
        public Churchill() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Mace Fighting Instructor";
            Body = 0x190;
            Hue = 0x83EA;
            HairItemID = 0x203C;
            HairHue = 0x455;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.Anatomy, 120.0);
            SetSkill(SkillName.Parry, 120.0);
            SetSkill(SkillName.Healing, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Macing, 120.0);
            SetSkill(SkillName.Focus, 120.0);

            AddItem(new Backpack());
            AddItem(new OrderShield());
            AddItem(new WarMace());

            Item item;

            item = new PlateLegs();
            item.Hue = 0x966;
            AddItem(item);

            item = new PlateGloves();
            item.Hue = 0x966;
            AddItem(item);

            item = new PlateGorget();
            item.Hue = 0x966;
            AddItem(item);

            item = new PlateChest();
            item.Hue = 0x966;
            AddItem(item);

            item = new PlateArms();
            item.Hue = 0x966;
            AddItem(item);
        }

        public Churchill(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Churchill";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078141); // Don't listen to Jockles. Real warriors wield mace weapons!
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Robyn : Bowyer
    {
        [Constructible]
        public Robyn()
        {
            Title = "the Archery Instructor";
            Body = 0x191;
            Hue = 0x83EA;
            HairItemID = 0x203C;
            HairHue = 0x47D;
            Female = true;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.Anatomy, 120.0);
            SetSkill(SkillName.Parry, 120.0);
            SetSkill(SkillName.Fletching, 120.0);
            SetSkill(SkillName.Healing, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Archery, 120.0);
            SetSkill(SkillName.Focus, 120.0);
        }

        public Robyn(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Robyn";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078202); // Archery requires a steady aim and dexterous fingers.
        }

        public override void InitOutfit()
        {
            AddItem(new Backpack());
            AddItem(new Boots(0x592));
            AddItem(new Cloak(0x592));
            AddItem(new Bandana(0x592));
            AddItem(new CompositeBow());

            Item item;

            item = new StuddedLegs();
            item.Hue = 0x592;
            AddItem(item);

            item = new StuddedGloves();
            item.Hue = 0x592;
            AddItem(item);

            item = new StuddedGorget();
            item.Hue = 0x592;
            AddItem(item);

            item = new StuddedChest();
            item.Hue = 0x592;
            AddItem(item);

            item = new StuddedArms();
            item.Hue = 0x592;
            AddItem(item);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Recaro : BaseCreature
    {
        [Constructible]
        public Recaro() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Fencer Instructor";
            Body = 0x190;
            Hue = 0x83EA;
            HairItemID = 0x203C;
            HairHue = 0x455;
            FacialHairItemID = 0x204D;
            FacialHairHue = 0x455;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.Anatomy, 120.0);
            SetSkill(SkillName.Parry, 120.0);
            SetSkill(SkillName.Healing, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Fencing, 120.0);
            SetSkill(SkillName.Focus, 120.0);

            AddItem(new Backpack());
            AddItem(new Shoes(0x455));
            AddItem(new WarFork());

            Item item;

            item = new StuddedLegs();
            item.Hue = 0x455;
            AddItem(item);

            item = new StuddedGloves();
            item.Hue = 0x455;
            AddItem(item);

            item = new StuddedGorget();
            item.Hue = 0x455;
            AddItem(item);

            item = new StuddedChest();
            item.Hue = 0x455;
            AddItem(item);

            item = new StuddedArms();
            item.Hue = 0x455;
            AddItem(item);
        }

        public Recaro(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Recaro";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            // The art of fencing requires a dexterous hand, a quick wit and fleet feet.
            MLQuestSystem.Tell(this, pm, 1078187);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class AldenArmstrong : BaseCreature
    {
        [Constructible]
        public AldenArmstrong() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Tactics Instructor";
            Body = 0x190;
            Hue = 0x83EA;
            HairItemID = 0x203B;
            HairHue = 0x44E;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.Anatomy, 120.0);
            SetSkill(SkillName.Parry, 120.0);
            SetSkill(SkillName.Healing, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Swords, 120.0);
            SetSkill(SkillName.Focus, 120.0);

            AddItem(new Backpack());
            AddItem(new Shoes());
            AddItem(new StuddedLegs());
            AddItem(new StuddedGloves());
            AddItem(new StuddedGorget());
            AddItem(new StuddedChest());
            AddItem(new StuddedArms());
            AddItem(new Katana());
        }

        public AldenArmstrong(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Alden Armstrong";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            // There is an art to slaying your enemies swiftly. It's called tactics, and I can teach it to you.
            MLQuestSystem.Tell(this, pm, 1078136);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Jockles : BaseCreature
    {
        [Constructible]
        public Jockles() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Swordsmanship Instructor";
            Body = 0x190;
            Hue = 0x83FA;
            HairItemID = 0x203C;
            HairHue = 0x8A7;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.Anatomy, 120.0);
            SetSkill(SkillName.Parry, 120.0);
            SetSkill(SkillName.Healing, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Swords, 120.0);
            SetSkill(SkillName.Focus, 120.0);

            AddItem(new Backpack());
            AddItem(new Broadsword());
            AddItem(new PlateChest());
            AddItem(new PlateLegs());
            AddItem(new PlateGloves());
            AddItem(new PlateArms());
            AddItem(new PlateGorget());
            AddItem(new OrderShield());
        }

        public Jockles(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Jockles";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078135); // Talk to me to learn the way of the blade.
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class TylAriadne : BaseCreature
    {
        [Constructible]
        public TylAriadne() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Parrying Instructor";
            Body = 0x190;
            Hue = 0x8374;
            HairItemID = 0;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.Anatomy, 120.0);
            SetSkill(SkillName.Parry, 120.0);
            SetSkill(SkillName.Healing, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Swords, 120.0);
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.Focus, 120.0);

            AddItem(new Backpack());
            AddItem(new ElvenBoots(0x96D));

            Item item;

            item = new StuddedLegs();
            item.Hue = 0x96D;
            AddItem(item);

            item = new StuddedGloves();
            item.Hue = 0x96D;
            AddItem(item);

            item = new StuddedGorget();
            item.Hue = 0x96D;
            AddItem(item);

            item = new StuddedChest();
            item.Hue = 0x96D;
            AddItem(item);

            item = new StuddedArms();
            item.Hue = 0x96D;
            AddItem(item);

            item = new DiamondMace();
            item.Hue = 0x96D;
            AddItem(item);
        }

        public TylAriadne(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Tyl Ariadne";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078140); // Want to learn how to parry blows?
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Alefian : BaseCreature
    {
        [Constructible]
        public Alefian() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Resisting Spells Instructor";
            Body = 0x190;
            Hue = 0x83EA;
            HairItemID = 0x203D;
            HairHue = 0x457;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.EvalInt, 120.0);
            SetSkill(SkillName.Inscribe, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.MagicResist, 120.0);
            SetSkill(SkillName.Wrestling, 120.0);
            SetSkill(SkillName.Meditation, 120.0);

            AddItem(new Backpack());
            AddItem(new Robe());
            AddItem(new Sandals());
        }

        public Alefian(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Alefian";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078130); // A mage should learn how to resist spells.
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Gustar : BaseCreature
    {
        [Constructible]
        public Gustar() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Meditation Instructor";
            Body = 0x190;
            Hue = 0x83F5;
            HairItemID = 0x203B;
            HairHue = 0x455;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.EvalInt, 120.0);
            SetSkill(SkillName.Inscribe, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.MagicResist, 120.0);
            SetSkill(SkillName.Wrestling, 120.0);
            SetSkill(SkillName.Meditation, 120.0);

            AddItem(new Backpack());
            AddItem(new GustarShroud());
            AddItem(new Sandals());
        }

        public Gustar(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Gustar";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078126); // Meditation allows a mage to replenish mana quickly. I can teach you.
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class GustarShroud : BaseOuterTorso
    {
        [Constructible]
        public GustarShroud() : base(0x2684) => Hue = 0x479;

        public GustarShroud(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => " ";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // Version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Jillian : BaseCreature
    {
        [Constructible]
        public Jillian() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Inscription Instructor";
            Body = 0x191;
            Female = true;
            Hue = 0x83EA;
            HairItemID = 0x203D;
            HairHue = 0x455;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.EvalInt, 120.0);
            SetSkill(SkillName.Inscribe, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.MagicResist, 120.0);
            SetSkill(SkillName.Wrestling, 120.0);
            SetSkill(SkillName.Meditation, 120.0);

            AddItem(new Backpack());
            AddItem(new Robe(0x479));
            AddItem(new Sandals());
        }

        public Jillian(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Jillian";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078129); // I can teach you how to scribe magic scrolls.
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Kaelynna : BaseCreature
    {
        [Constructible]
        public Kaelynna() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Magery Instructor";
            Body = 0x191;
            Female = true;
            Hue = 0x83EA;
            HairItemID = 0x203C;
            HairHue = 0x47D;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.EvalInt, 120.0);
            SetSkill(SkillName.Inscribe, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.MagicResist, 120.0);
            SetSkill(SkillName.Wrestling, 120.0);
            SetSkill(SkillName.Meditation, 120.0);

            AddItem(new Backpack());
            AddItem(new Robe(0x592));
            AddItem(new Sandals());
        }

        public Kaelynna(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Kaelynna";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078125); // Want to unlock the secrets of magery?
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Mithneral : BaseCreature
    {
        [Constructible]
        public Mithneral() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Evaluating Intelligence Instructor";
            Body = 0x190;
            Hue = 0x83EA;
            HairItemID = 0x203C;
            HairHue = 0x455;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.EvalInt, 120.0);
            SetSkill(SkillName.Inscribe, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.MagicResist, 120.0);
            SetSkill(SkillName.Wrestling, 120.0);
            SetSkill(SkillName.Meditation, 120.0);

            AddItem(new Backpack());
            AddItem(new Sandals());

            Item item;

            item = new GustarShroud();
            item.Hue = 0x51C;
            AddItem(item);
        }

        public Mithneral(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Mithneral";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078127); // Want to maximize your spell damage? I have a scholarly task for you!
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class AmeliaYoungstone : Tinker
    {
        [Constructible]
        public AmeliaYoungstone()
        {
            Title = "the Tinkering Instructor";
            Body = 0x191;
            Female = true;
            Hue = 0x83EA;
            HairItemID = 0x203D;
            HairHue = 0x46C;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.ArmsLore, 120.0);
            SetSkill(SkillName.Blacksmith, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Swords, 120.0);
            SetSkill(SkillName.Tinkering, 120.0);
            SetSkill(SkillName.Mining, 120.0);
        }

        public AmeliaYoungstone(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Amelia Youngstone";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078123); // Tinkering is very useful for a blacksmith. You can make your own tools.
        }

        public override void InitOutfit()
        {
            AddItem(new Backpack());
            AddItem(new Sandals());
            AddItem(new Doublet());
            AddItem(new ShortPants());
            AddItem(new HalfApron(0x8AB));
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class AndreasVesalius : BaseCreature
    {
        [Constructible]
        public AndreasVesalius() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Anatomy Instructor";
            Body = 0x190;
            Hue = 0x83EC;
            HairItemID = 0x203C;
            HairHue = 0x477;
            FacialHairItemID = 0x203E;
            FacialHairHue = 0x477;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.Anatomy, 120.0);
            SetSkill(SkillName.Parry, 120.0);
            SetSkill(SkillName.Healing, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Swords, 120.0);
            SetSkill(SkillName.Focus, 120.0);

            AddItem(new Backpack());
            AddItem(new Boots());
            AddItem(new BlackStaff());
            AddItem(new LongPants());
            AddItem(new Tunic(0x66D));
        }

        public AndreasVesalius(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Andreas Vesalius";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078138); // Learning of the body will allow you to excel in combat.
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Avicenna : BaseCreature
    {
        [Constructible]
        public Avicenna() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Healing Instructor";
            Body = 0x190;
            Hue = 0x83EA;
            HairItemID = 0x203B;
            HairHue = 0x477;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.Anatomy, 120.0);
            SetSkill(SkillName.Parry, 120.0);
            SetSkill(SkillName.Healing, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Swords, 120.0);
            SetSkill(SkillName.Focus, 120.0);

            AddItem(new Backpack());
            AddItem(new Robe(0x66D));
            AddItem(new Boots());
            AddItem(new GnarledStaff());
        }

        public Avicenna(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Avicenna";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078137); // A warrior needs to learn how to apply bandages to wounds.
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class SarsmeaSmythe : BaseCreature
    {
        [Constructible]
        public SarsmeaSmythe() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Focus Instructor";
            Body = 0x191;
            Female = true;
            Hue = 0x83EA;
            HairItemID = 0x203C;
            HairHue = 0x456;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.Anatomy, 120.0);
            SetSkill(SkillName.Parry, 120.0);
            SetSkill(SkillName.Healing, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Swords, 120.0);
            SetSkill(SkillName.Focus, 120.0);

            AddItem(new Backpack());
            AddItem(new ThighBoots());
            AddItem(new StuddedGorget());
            AddItem(new LeatherLegs());
            AddItem(new FemaleLeatherChest());
            AddItem(new StuddedGloves());
            AddItem(new LeatherNinjaBelt());
            AddItem(new LightPlateJingasa());
        }

        public SarsmeaSmythe(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Sarsmea Smythe";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078139); // Know yourself, and you will become a true warrior.
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Ryuichi : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Ryuichi()
            : base("the Ninjitsu Instructor")
        {
            Hue = 0x8403;

            SetSkill(SkillName.Hiding, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Tracking, 120.0);
            SetSkill(SkillName.Fencing, 120.0);
            SetSkill(SkillName.Stealth, 120.0);
            SetSkill(SkillName.Ninjitsu, 120.0);
        }

        public Ryuichi(Serial serial)
            : base(serial)
        {
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override string DefaultName => "Ryuichi";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078155); // I can teach you Ninjitsu. The Art of Stealth.
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBNinja());
        }

        public override bool GetGender() => false;

        public override void InitOutfit()
        {
            HairItemID = 0x203B;
            HairHue = 0x455;

            AddItem(new SamuraiTabi());
            AddItem(new LeatherNinjaPants());
            AddItem(new LeatherNinjaMitts());
            AddItem(new LeatherNinjaHood());
            AddItem(new LeatherNinjaJacket());
            AddItem(new LeatherNinjaBelt());

            PackGold(100, 200);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Chiyo : BaseCreature
    {
        [Constructible]
        public Chiyo() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Hiding Instructor";
            Body = 0xF7;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.Hiding, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Tracking, 120.0);
            SetSkill(SkillName.Fencing, 120.0);
            SetSkill(SkillName.Stealth, 120.0);
            SetSkill(SkillName.Ninjitsu, 120.0);
        }

        public Chiyo(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Chiyo";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078165); // To be undetected means you cannot be harmed.
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Jun : BaseCreature
    {
        [Constructible]
        public Jun() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Stealth Instructor";
            Body = 0x190;
            Hue = 0x8403;
            HairItemID = 0x203B;
            HairHue = 0x455;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.Hiding, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Tracking, 120.0);
            SetSkill(SkillName.Fencing, 120.0);
            SetSkill(SkillName.Stealth, 120.0);
            SetSkill(SkillName.Ninjitsu, 120.0);

            AddItem(new Backpack());
            AddItem(new SamuraiTabi());
            AddItem(new LeatherNinjaPants());
            AddItem(new LeatherNinjaMitts());
            AddItem(new LeatherNinjaHood());
            AddItem(new LeatherNinjaJacket());
            AddItem(new LeatherNinjaBelt());
        }

        public Jun(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Jun";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078175); // Walk Silently. Remain unseen. I can teach you.
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Walker : BaseCreature
    {
        [Constructible]
        public Walker() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Tracking Instructor";
            Body = 0x190;
            Hue = 0x83EA;
            HairItemID = 0x203B;
            HairHue = 0x47D;
            FacialHairItemID = 0x204B;
            FacialHairHue = 0x47D;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.Hiding, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Tracking, 120.0);
            SetSkill(SkillName.Fencing, 120.0);
            SetSkill(SkillName.Wrestling, 120.0);
            SetSkill(SkillName.Stealth, 120.0);
            SetSkill(SkillName.Ninjitsu, 120.0);

            AddItem(new Backpack());
            AddItem(new Boots(0x455));
            AddItem(new LongPants(0x455));
            AddItem(new FancyShirt(0x47D));
            AddItem(new FloppyHat(0x455));
        }

        public Walker(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Walker";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(
                this,
                pm,
                Utility.RandomList(
                    1078213, // I don't sleep. I wait.
                    1078212, // There is no theory of evolution. Just a list of creatures I allow to live.
                    1078214  // I can lead a horse to water and make it drink.
                )
            );
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Hamato : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public Hamato()
            : base("the Bushido Instructor")
        {
            Hue = 0x8403;

            SetSkill(SkillName.Anatomy, 120.0);
            SetSkill(SkillName.Parry, 120.0);
            SetSkill(SkillName.Healing, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Swords, 120.0);
            SetSkill(SkillName.Bushido, 120.0);
        }

        public Hamato(Serial serial)
            : base(serial)
        {
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override string DefaultName => "Hamato";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078134); // Seek me to learn the way of the samurai.
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBSamurai());
        }

        public override bool GetGender() => false;

        public override void InitOutfit()
        {
            HairItemID = 0x203D;
            HairHue = 0x497;

            AddItem(new Backpack());
            AddItem(new NoDachi());
            AddItem(new NinjaTabi());
            AddItem(new PlateSuneate());
            AddItem(new LightPlateJingasa());
            AddItem(new LeatherDo());
            AddItem(new LeatherHiroSode());

            PackGold(100, 200);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Mulcivikh : Mage
    {
        [Constructible]
        public Mulcivikh()
        {
            Title = "the Necromancy Instructor";
            Body = 0x190;
            Hue = 0x83EA;
            HairItemID = 0x203D;
            HairHue = 0x457;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.MagicResist, 120.0);
            SetSkill(SkillName.SpiritSpeak, 120.0);
            SetSkill(SkillName.Swords, 120.0);
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.Necromancy, 120.0);
        }

        public Mulcivikh(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Mulcivikh";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078131); // Allured by dark magic, aren't you?
        }

        public override void InitOutfit()
        {
            AddItem(new Backpack());
            AddItem(new Sandals(0x8FD));
            AddItem(new BoneHelm());

            Item item;

            item = new LeatherLegs();
            item.Hue = 0x2C3;
            AddItem(item);

            item = new LeatherGloves();
            item.Hue = 0x2C3;
            AddItem(item);

            item = new LeatherGorget();
            item.Hue = 0x2C3;
            AddItem(item);

            item = new LeatherChest();
            item.Hue = 0x2C3;
            AddItem(item);

            item = new LeatherArms();
            item.Hue = 0x2C3;
            AddItem(item);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Morganna : Mage
    {
        [Constructible]
        public Morganna()
        {
            Title = "the Spirit Speak Instructor";
            Body = 0x191;
            Female = true;
            Hue = 0x83EA;
            HairItemID = 0x203C;
            HairHue = 0x455;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.MagicResist, 120.0);
            SetSkill(SkillName.SpiritSpeak, 120.0);
            SetSkill(SkillName.Swords, 120.0);
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.Necromancy, 120.0);
        }

        public Morganna(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Morganna";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078132); // Want to learn how to channel the supernatural?
        }

        public override void InitOutfit()
        {
            AddItem(new Backpack());
            AddItem(new Sandals());
            AddItem(new Robe(0x47D));
            AddItem(new SkullCap(0x455));
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class JacobWaltz : BaseCreature
    {
        [Constructible]
        public JacobWaltz() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Miner Instructor";
            Body = 0x190;
            Hue = 0x83EA;
            HairItemID = 0x2048;
            HairHue = 0x44E;
            FacialHairItemID = 0x204D;
            FacialHairHue = 0x44E;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.ArmsLore, 120.0);
            SetSkill(SkillName.Blacksmith, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Tinkering, 120.0);
            SetSkill(SkillName.Swords, 120.0);
            SetSkill(SkillName.Mining, 120.0);

            AddItem(new Backpack());
            AddItem(new Pickaxe());
            AddItem(new Boots());
            AddItem(new WideBrimHat(0x966));
            AddItem(new ShortPants(0x370));
            AddItem(new Shirt(0x966));
            AddItem(new HalfApron(0x1BB));
        }

        public JacobWaltz(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;
        public override bool CanTeach => true;
        public override string DefaultName => "Jacob Waltz";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078124); // You there! I can use some help mining these rocks!
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class GeorgeHephaestus : Blacksmith
    {
        [Constructible]
        public GeorgeHephaestus()
        {
            Title = "the Blacksmith Instructor";
            Body = 0x190;
            Hue = 0x83EA;
            HairItemID = 0x203B;
            HairHue = 0x47B;

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            SetSkill(SkillName.ArmsLore, 120.0);
            SetSkill(SkillName.Blacksmith, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.Tactics, 120.0);
            SetSkill(SkillName.Tinkering, 120.0);
            SetSkill(SkillName.Swords, 120.0);
            SetSkill(SkillName.Mining, 120.0);
        }

        public GeorgeHephaestus(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "George Hephaestus";
        public override bool CanShout => true;

        public override void Shout(PlayerMobile pm)
        {
            MLQuestSystem.Tell(this, pm, 1078122); // Wanna learn how to make powerful weapons and armor? Talk to me.
        }

        public override void InitOutfit()
        {
            AddItem(new Backpack());
            AddItem(new Boots(0x973));
            AddItem(new LongPants());
            AddItem(new Bascinet());
            AddItem(new FullApron(0x8AB));

            Item item;

            item = new SmithHammer();
            item.Hue = 0x8AB;
            AddItem(item);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}

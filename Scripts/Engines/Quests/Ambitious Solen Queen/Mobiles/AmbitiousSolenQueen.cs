using Server.Mobiles;
using Server.Items;

namespace Server.Engines.Quests.Ambitious
{
	public abstract class BaseAmbitiousSolenQueen : BaseQuester
	{
		public abstract bool RedSolen{ get; }
		public override string DefaultName => "an ambitious solen queen";
		public override bool DisallowAllMoves => false;

		public BaseAmbitiousSolenQueen()
		{
		}

		public override void InitBody()
		{
			Body = 0x30F;

			if ( !RedSolen )
				Hue = 0x453;

			SpeechHue = 0;
		}

		public override int GetIdleSound()
		{
			return 0x10D;
		}

		public override void OnTalk( PlayerMobile player, bool contextMenu )
		{
			this.Direction = GetDirectionTo( player );

			if ( player.Quest is AmbitiousQueenQuest qs && qs.RedSolen == this.RedSolen )
			{
				if ( qs.IsObjectiveInProgress( typeof( KillQueensObjective ) ) )
				{
					qs.AddConversation( new DuringKillQueensConversation() );
				}
				else
				{
					QuestObjective obj = qs.FindObjective( typeof( ReturnAfterKillsObjective ) );

					if ( obj != null && !obj.Completed )
					{
						obj.Complete();
					}
					else if ( qs.IsObjectiveInProgress( typeof( GatherFungiObjective ) ) )
					{
						qs.AddConversation( new DuringFungiGatheringConversation() );
					}
					else
					{
						if ( qs.FindObjective( typeof( GetRewardObjective ) ) is GetRewardObjective lastObj && !lastObj.Completed )
						{
							bool bagOfSending = lastObj.BagOfSending;
							bool powderOfTranslocation = lastObj.PowderOfTranslocation;
							bool gold = lastObj.Gold;

							AmbitiousQueenQuest.GiveRewardTo( player, ref bagOfSending, ref powderOfTranslocation, ref gold );

							lastObj.BagOfSending = bagOfSending;
							lastObj.PowderOfTranslocation = powderOfTranslocation;
							lastObj.Gold = gold;

							if ( !bagOfSending && !powderOfTranslocation && !gold )
							{
								lastObj.Complete();
							}
							else
							{
								qs.AddConversation( new FullBackpackConversation( false, lastObj.BagOfSending, lastObj.PowderOfTranslocation, lastObj.Gold ) );
							}
						}
					}
				}
			}
			else
			{
				QuestSystem newQuest = new AmbitiousQueenQuest( player, this.RedSolen );

				if ( player.Quest == null && QuestSystem.CanOfferQuest( player, typeof( AmbitiousQueenQuest ) ) )
				{
					newQuest.SendOffer();
				}
				else
				{
					newQuest.AddConversation( new DontOfferConversation() );
				}
			}
		}

		public override bool OnDragDrop( Mobile from, Item dropped )
		{
			this.Direction = GetDirectionTo( from );

			if ( from is PlayerMobile player )
			{
				if ( player.Quest is AmbitiousQueenQuest qs && qs.RedSolen == this.RedSolen )
				{
					QuestObjective obj = qs.FindObjective( typeof( GatherFungiObjective ) );

					if ( obj != null && !obj.Completed )
					{
						if ( dropped is ZoogiFungus fungi )
						{
							if ( fungi.Amount >= 50 )
							{
								obj.Complete();

								fungi.Amount -= 50;

								if ( fungi.Amount == 0 )
								{
									fungi.Delete();
									return true;
								}

								return false;
							}

							SayTo( player, 1054072 ); // Our arrangement was for 50 of the zoogi fungus. Please return to me when you have that amount.
							return false;
						}
					}
				}
			}

			return base.OnDragDrop( from, dropped );
		}

		public BaseAmbitiousSolenQueen( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}

	public class RedAmbitiousSolenQueen : BaseAmbitiousSolenQueen
	{
		public override bool RedSolen => true;

		[Constructible]
		public RedAmbitiousSolenQueen()
		{
		}

		public RedAmbitiousSolenQueen( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}

	public class BlackAmbitiousSolenQueen : BaseAmbitiousSolenQueen
	{
		public override bool RedSolen => false;

		[Constructible]
		public BlackAmbitiousSolenQueen()
		{
		}

		public BlackAmbitiousSolenQueen( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}

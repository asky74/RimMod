/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 20.08.2017
 * Time: 02:26
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Harmony;
using Verse.AI;

namespace RIMMSqol
{
	/// <summary>
	/// Description of Building_CommsConsolePlus.
	/// </summary>
	public class Building_CommsConsolePlus : Building_CommsConsole
	{
		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
		{
			IEnumerable<FloatMenuOption> __result = base.GetFloatMenuOptions(myPawn);
			
			//lazy implementation simply checks if more than one entry exists in the returned list if so then add the new entries otherwise it wasnt possible to communicate and dont add anything.
			if ( __result != null && __result.Count() > 1 ) {
				List<FloatMenuOption> result = new List<FloatMenuOption>(__result);
				foreach ( WorldObject wo in Find.World.worldObjects.AllWorldObjects ) {
					processCommunicable(wo as ICommunicable, myPawn, result);
				}
				foreach ( GameComponent gc in Current.Game.components ) {
					processCommunicable(gc as ICommunicable, myPawn, result);
				}
				return result;
			}
			
			return __result;
		}
		
		protected void processCommunicable(ICommunicable comm, Pawn myPawn, List<FloatMenuOption> result) {
			if ( comm != null ) {
				string text = "CallOnRadio".Translate(comm.GetCallLabel());
				Action action = delegate {
					ICommunicable localCommTarget = comm;
					if (!Building_OrbitalTradeBeacon.AllPowered(this.Map).Any<Building_OrbitalTradeBeacon>()) {
						Messages.Message("MessageNeedBeaconToTradeWithShip".Translate(), this, MessageTypeDefOf.RejectInput);
						return;
					}
					if ( comm is ILoadReferenceable ) {
						//if we communicate with an object that can be saved we create a job.
						Job job = new Job(JobDefOf.UseCommsConsole, this);
						job.commTarget = localCommTarget;
						myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					} else {
						//if we cannot save the com target we avoid creating a job and start the communication immediately.
						localCommTarget.TryOpenComms(myPawn);
					}
					PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.OpeningComms, KnowledgeAmount.Total);
				};
				result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, action, MenuOptionPriority.InitiateSocial, null, null, 0f, null, null), myPawn, this));
			}
		}
	}
}

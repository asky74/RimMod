/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 18.08.2017
 * Time: 20:03
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Harmony;
using Verse.AI;

namespace RIMMSqol
{
	/// <summary>
	/// Description of SpawnRemnantColony.
	/// </summary>
	[HarmonyPatch(typeof(SettlementAbandonUtility))]
	[HarmonyPatch("Abandon")]
	[HarmonyPatchNamespace("RemnantColony")]
	static public class SpawnRemnantColony
	{
		static bool Prefix(Settlement settlement) {
			//TODO altering the abandon string for the dialog and a check if the abandoned settlement meets the requirements for becoming an abandoned functional colony. enough beds? a rec room. joy stuff. everyone clothed. etc..
			//SettlementAbandonUtility.TryAbandonViaInterface spawns the confirmation dialog with the text that needs to be altered if the settlment meets the requirements for a remnant colony.
			
			//In case we abandon a player base we instead spawn a remnant colony.
			if (settlement.Faction.IsPlayer && settlement.Map.mapPawns.FreeColonistsSpawned.Any() )
			{
				WorldObject worldObject = WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("WorldObjectRemnantColony"));
				worldObject.Tile = settlement.Tile;
				worldObject.SetFaction(settlement.Faction);
				Find.WorldObjects.Add(worldObject);
				
				//MapDeiniter.PassPawnsToWorld takes care of banishing. If we spawn a remnant colony we remove the pawns that we harbor from the map so they wont get banished.
				//Only the free colonists spawned are accounted for this everybody else can be banished. Animals that were bonded will still cause the banished penalty.
				List<Pawn> pawns = settlement.Map.mapPawns.FreeColonistsSpawned.ToList();
				foreach (Pawn p in pawns) {
					try {
						if (p.Spawned) {
							p.DeSpawn();
						}
						if (p.ownership != null) {
							p.ownership.UnclaimAll();
						}
						if (p.guest != null) {
							p.guest.SetGuestStatus(null, false);
						}
						p.inventory.UnloadEverything = false;
						//Pawn.Notify_PassedToWorld will set them to a random faction if kept. Fully discarding them would clear relations.
						//TODO We simply reset them into the player faction once they have been booted into the world.
						Faction fac = p.Faction;
						Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.Decide);
						p.SetFaction(fac);
					} catch (Exception ex) {
						Log.Error(string.Concat(new object[] {
							"Could not despawn and pass to world ",
							p,
							": ",
							ex
						}));
					}
				}
				
				Find.WorldObjects.Remove(settlement);
				Find.GameEnder.CheckOrUpdateGameOver();
				return false;
			}
			return true;
		}
	}
	
	[HarmonyPatch(typeof(SettlementAbandonUtility))]
	[HarmonyPatch("TryAbandonViaInterface")]
	[HarmonyPatchNamespace("RemnantColony")]
	static public class DisplayConfirmationDialog {
		static bool Prefix(MapParent settlement) {
			Settlement factionBase = settlement as Settlement;
			if ( WorldObjectRemnantColony.CanCreateRemnantColony(factionBase) )
			{
				//custom text for remnant colony
				Map map = settlement.Map;
				StringBuilder stringBuilder = new StringBuilder();
				IEnumerable<Pawn> source = map.mapPawns.PawnsInFaction(Faction.OfPlayer);
				if (source.Any()) {
					StringBuilder stringBuilder2 = new StringBuilder();
					foreach (Pawn current in from x in source
					orderby x.IsColonist descending
					select x) {
						if (stringBuilder2.Length > 0) {
							stringBuilder2.AppendLine();
						}
						stringBuilder2.Append("    " + current.LabelCap);
					}
					stringBuilder.Append("ConfirmAbandonHomeWithColonyPawns".Translate(stringBuilder2.ToString()));
				}
				
				//any pawns we wont use in the remnant colony will be banished(e.g. animals and prisoner)
				PawnDiedOrDownedThoughtsUtility.BuildMoodThoughtsListString(
					map.mapPawns.AllPawns.Except(WorldObjectRemnantColony.GetPawnsFromSettlementForRemnantColony(factionBase)), 
					PawnDiedOrDownedThoughtsKind.Banished, stringBuilder, null, "\n\n" + "ConfirmAbandonHomeNegativeThoughts_Everyone".Translate(), "ConfirmAbandonHomeNegativeThoughts");
				
				Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(stringBuilder.ToString(), delegate {
                  	typeof(SettlementAbandonUtility).GetMethod("Abandon", BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new Object[]{settlement});
				}, false, null));
				return false;
			}
			return true;
		}
	}
}

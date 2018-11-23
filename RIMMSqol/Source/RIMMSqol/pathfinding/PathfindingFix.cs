/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 01.03.2018
 * Time: 15:37
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Runtime.InteropServices;
using Harmony;
using RIMMSqol.pathfinding;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RIMMSqol.pathfinding
{
	/// <summary>
	/// Description of PathfindingFix.
	/// </summary>
	[HarmonyPatch(typeof(PathFinder))]
	[HarmonyPatch("FindPath")]
	[HarmonyPatch(new[]{typeof(IntVec3),typeof(LocalTargetInfo),typeof(TraverseParms),typeof(PathEndMode)})]
	[HarmonyPatchNamespace("Pathfinding")]
	static public class PathfindingFix
	{
		//static Stopwatch swVanilla;
		private static FieldInfo mapField = typeof(PathFinder).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic);
		
		static bool Prefix(PathFinder __instance, ref PawnPath __result, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode = PathEndMode.OnCell) {
			//swVanilla = null;
			Pawn pawn = traverseParms.pawn;
			if ( pawn != null ) {
				int algId;
				if ( pawn.IsColonist ) {
					algId = 1;
				} else if ( pawn.RaceProps.Animal ) {
					if ( pawn.Faction != null && pawn.Faction.IsPlayer ) algId = 2;
					else algId = 3;
				} else {
					algId = 4;
				}
				
				if ( QOLMod.hasPFAlgorithm(algId) ) {
					Map map = (Map)mapField.GetValue(__instance);
					if ( !preSearchChecks(map,start,dest,traverseParms,peMode) ) {
						__result = PawnPath.NotFound;
						return false;
					}
					
					ByteGrid avoidGrid = (pawn == null) ? null : pawn.GetAvoidGrid();
					Area allowedArea = GetAllowedArea(pawn);
					int ticksPerMoveCardinal;
					int ticksPerMoveDiagonal;
					if (pawn != null) {
						ticksPerMoveCardinal = pawn.TicksPerMoveCardinal;
						ticksPerMoveDiagonal = pawn.TicksPerMoveDiagonal;
						//Log.Message("PawnTicksToMove: "+ticksPerMoveCardinal+"/"+ticksPerMoveDiagonal);
					} else {
						//we currently do not pathfind for non pawn(e.g. generating maps) but best to leave it as a reminder
						ticksPerMoveCardinal = 13;
						ticksPerMoveDiagonal = 18;
					}
					
					__result = QOLMod.doPFAlgorithm(algId, map,start,dest,traverseParms,peMode,avoidGrid,allowedArea,ticksPerMoveCardinal,ticksPerMoveDiagonal);
					if ( __result == null ) return true;
					if ( DebugViewSettings.drawPaths ) {
						foreach ( IntVec3 pos in __result.NodesReversed ) {
							map.debugDrawer.FlashCell(pos, 0.7f, "finalPath", 100);
						}
					}
					return false;
				}
			}
			/*if ( pawn != null && pawn.IsColonist ) {
				Log.Message("Pathfinding for distance: "+Math.Max(Math.Abs(start.x-dest.Cell.x),Math.Abs(start.z-dest.Cell.z)));
				swVanilla = Stopwatch.StartNew();
			}*/
			return true;
		}
		
		/*static void Postfix() {
			if ( swVanilla != null ) {
				swVanilla.Stop();
				Log.Message("time taken vanilla pf "+swVanilla.ElapsedTicks+"ticks");
			}
		}*/
		
		static Area GetAllowedArea(Pawn pawn) {
			if (pawn != null && pawn.playerSettings != null && !pawn.Drafted && ForbidUtility.CaresAboutForbidden(pawn, true))
			{
				Area area = pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap;
				if (area != null && area.TrueCount <= 0)
				{
					area = null;
				}
				return area;
			}
			return null;
		}
		
		static bool preSearchChecks(Map map, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode) {
			if (DebugSettings.pathThroughWalls) {
				traverseParms.mode = TraverseMode.PassAllDestroyableThings;
			}
			Pawn pawn = traverseParms.pawn;
			if (pawn != null && pawn.Map != map) {
				Log.Error(string.Concat(new object[]
				{
					"Tried to FindPath for pawn which is spawned in another map. His map PathFinder should have been used, not this one. pawn=",
					pawn,
					" pawn.Map=",
					pawn.Map,
					" map=",
					map
				}));
				return false;
			}
			if (!start.IsValid) {
				Log.Error(string.Concat(new object[]
				{
					"Tried to FindPath with invalid start ",
					start,
					", pawn= ",
					pawn
				}));
				return false;
			}
			if (!dest.IsValid) {
				Log.Error(string.Concat(new object[]
				{
					"Tried to FindPath with invalid dest ",
					dest,
					", pawn= ",
					pawn
				}));
				return false;
			}
			if (traverseParms.mode == TraverseMode.ByPawn) {
				if (!pawn.CanReach(dest, peMode, Danger.Deadly, traverseParms.canBash, traverseParms.mode)) {
					return false;
				}
			} else if (!map.reachability.CanReach(start, dest, peMode, traverseParms)) {
				return false;
			}
			return true;
		}
	}
}

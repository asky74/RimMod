/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 12.07.2018
 * Time: 04:21
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RIMMSqol.performance.worldpawngc
{
	/// <summary>
	/// Description of WorldPawnGCOptimized.
	/// </summary>
	static class WorldPawnGCOptimized
	{
		static Func<WorldPawnGC,int> lastSuccessfulGCTickGet = UtilReflection.CreateGetter<WorldPawnGC,int>(typeof(WorldPawnGC).GetField("lastSuccessfulGCTick", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
		static Action<WorldPawnGC,int> lastSuccessfulGCTickSet = UtilReflection.CreateSetter<WorldPawnGC,int>(typeof(WorldPawnGC).GetField("lastSuccessfulGCTick", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
		
		static Func<WorldPawnGC,IEnumerator> activeGCProcessGet = UtilReflection.CreateGetter<WorldPawnGC,IEnumerator>(typeof(WorldPawnGC).GetField("activeGCProcess", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
		static Action<WorldPawnGC,IEnumerator> activeGCProcessSet = UtilReflection.CreateSetter<WorldPawnGC,IEnumerator>(typeof(WorldPawnGC).GetField("activeGCProcess", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
				
		static Func<WorldPawnGC,int> currentGCRateGet = UtilReflection.CreateGetter<WorldPawnGC,int>(typeof(WorldPawnGC).GetField("currentGCRate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
		static Action<WorldPawnGC,int> currentGCRateSet = UtilReflection.CreateSetter<WorldPawnGC,int>(typeof(WorldPawnGC).GetField("currentGCRate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
		
		static StringBuilder logDotgraph = null;
		static HashSet<string> logDotgraphUniqueLinks = null;
		
		delegate string GetCriticalPawnReasonType(Pawn pawn);
		
		static List<PawnRelationDef> AllAllowedRelationDefs = DefDatabase<PawnRelationDef>.AllDefsListForReading.Where(def=>def != PawnRelationDefOf.Kin).Add(PawnRelationDefOf.Kin).ToList();
		
		public static bool WorldPawnGCTickPrefix(WorldPawnGC __instance)
		{
			if (lastSuccessfulGCTickGet(__instance) < Find.TickManager.TicksGame / 15000 * 15000)
			{
				if (activeGCProcessGet(__instance) == null)
				{
					activeGCProcessSet(__instance, PawnGCPass(__instance).GetEnumerator());
					//since we are faster we could amp up the speed, reducing the chance of an interrupt and thus overhead
					//currentGCRateSet(__instance,8);
					if (DebugViewSettings.logWorldPawnGC)
					{
						Log.Message(string.Format("World pawn GC started at rate {0}", currentGCRateGet(__instance)));
					}
				}
			}
			return true;
		}
		
		static IEnumerable PawnGCPass(WorldPawnGC __instance)
		{
			Dictionary<Pawn, string> keptPawns = new Dictionary<Pawn, string>();
			foreach (object _ in AccumulatePawnGCData(__instance, keptPawns))
			{
				yield return null;
			}
			//since resets happen if pawns are added/removed we do not need to create the copy before we accumulate the data 
			Pawn[] AllPawnsAliveOrDeadBuffered = Find.WorldPawns.AllPawnsAliveOrDead.ToArray<Pawn>();
			foreach ( Pawn pawn in AllPawnsAliveOrDeadBuffered ) {
				if (!keptPawns.ContainsKey(pawn)) {
					Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
				}
			}
		}
		
		static private IEnumerable AccumulatePawnGCData(WorldPawnGC __instance, Dictionary<Pawn, string> keptPawns)
		{
			GetCriticalPawnReasonType GetCriticalPawnReason = (GetCriticalPawnReasonType)Delegate.CreateDelegate(typeof(GetCriticalPawnReasonType), __instance, 
                   typeof(WorldPawnGC).GetMethod("GetCriticalPawnReason", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
			foreach (Pawn current in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead)
			{
				string criticalPawnReason = GetCriticalPawnReason(current);
				if (!criticalPawnReason.NullOrEmpty())
				{
					keptPawns[current] = criticalPawnReason;
					if (logDotgraph != null)
					{
						logDotgraph.AppendLine(string.Format("{0} [label=<{0}<br/><font point-size=\"10\">{1}</font>> color=\"{2}\" shape=\"{3}\"];", new object[]
						{
							WorldPawnGC.DotgraphIdentifier(current),
							criticalPawnReason,
							(current.relations == null || !current.relations.everSeenByPlayer) ? "grey" : "black",
							(!current.RaceProps.Humanlike) ? "box" : "oval"
						}));
					}
				}
				else if (logDotgraph != null)
				{
					logDotgraph.AppendLine(string.Format("{0} [color=\"{1}\" shape=\"{2}\"];", WorldPawnGC.DotgraphIdentifier(current), (current.relations == null || !current.relations.everSeenByPlayer) ? "grey" : "black", (!current.RaceProps.Humanlike) ? "box" : "oval"));
				}
			}
			//This is for finding non spawned but alive raiders/visitors not tremendously usefull
			foreach (Pawn current2 in (from pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive
				where pawn.RaceProps.Humanlike && !keptPawns.ContainsKey(pawn) 
				orderby pawn.records.StoryRelevance descending select pawn).Take(20)) {
				keptPawns[current2] = "StoryRelevant";
			}
			//We now have lets say 1000 keepers. Over the generations family trees grow wider and wider. 
			//Not only do the trees grow wider over generations, male animals will breed with multiple females and as such multiple family trees get merged amplyfiing the problem.
			Pawn[] storyRelevantPawns = new Pawn[keptPawns.Count];
			keptPawns.Keys.CopyTo(storyRelevantPawns, 0);
			foreach ( Pawn pawn in storyRelevantPawns ) {
				AddAllRelationships(__instance,pawn,keptPawns);
				yield return null;
			}
			foreach ( Pawn pawn in storyRelevantPawns ) {
				AddAllMemories(pawn,keptPawns);
			}
		}
		
		static private void AddAllRelationships(WorldPawnGC __instance, Pawn pawn, Dictionary<Pawn, string> keptPawns)
		{
			if ( pawn.relations == null || !pawn.relations.RelatedToAnyoneOrAnyoneRelatedToMe || !pawn.RaceProps.IsFlesh ) {
				return;
			}
			Stack<Pawn> stack = null;
			HashSet<Pawn> visited = null;
			try {
				stack = SimplePool<Stack<Pawn>>.Get();
				visited = SimplePool<HashSet<Pawn>>.Get();
				stack.Push(pawn);
				visited.Add(pawn);
				while ( stack.Count > 0 ) {
					Pawn p = stack.Pop();
					foreach (Pawn otherPawn in p.relations.DirectRelations.Select(r=>r.otherPawn).Concat<Pawn>(
						(HashSet<Pawn>)typeof(Pawn_RelationsTracker).GetField("pawnsWithDirectRelationsWithMe",
						                                                      BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(p.relations)) ) {
						if (!visited.Contains(otherPawn)) {
							if ( otherPawn.RaceProps.IsFlesh ) {
								PawnRelationDef relation = AllAllowedRelationDefs.FirstOrDefault(def => def.Worker.InRelation(pawn, otherPawn));
								if ( relation != default(PawnRelationDef) ) {
									if (logDotgraph != null) {
										string text = string.Format("{0}->{1} [label=<{2}> color=\"purple\"];", WorldPawnGC.DotgraphIdentifier(pawn), 
										                            WorldPawnGC.DotgraphIdentifier(otherPawn), relation.ToString());
										if (!logDotgraphUniqueLinks.Contains(text)) {
											logDotgraphUniqueLinks.Add(text);
											logDotgraph.AppendLine(text);
										}
									}
									if (!keptPawns.ContainsKey(otherPawn)) {
										keptPawns[otherPawn] = "Relationship";
										//Not correct to have it here if the pairing between two pawns is filtered but if we allow all relations this is correct.
										//The pursuit of animal family trees if a human is bonded with an animal is pointless.
										if ( relation != PawnRelationDefOf.Bond ) {
											stack.Push(otherPawn);
										}
									}
								}
							}
							visited.Add(otherPawn);
						}
					}
				}
			} finally {
				if ( stack != null ) {
					stack.Clear();
					stack.TrimExcess();
					SimplePool<Stack<Pawn>>.Return(stack);
				}
				if ( visited != null ) {
					visited.Clear();
					stack.TrimExcess();
					SimplePool<HashSet<Pawn>>.Return(visited);
				}
			}
		}
		
		static private void AddAllMemories(Pawn pawn, Dictionary<Pawn, string> keptPawns)
		{
			if (pawn.needs == null || pawn.needs.mood == null || pawn.needs.mood.thoughts == null || pawn.needs.mood.thoughts.memories == null) {
				return;
			}
			foreach (Thought_Memory current in pawn.needs.mood.thoughts.memories.Memories) {
				if (current.otherPawn != null) {
					if (logDotgraph != null) {
						string text = string.Format("{0}->{1} [label=<{2}> color=\"orange\"];", WorldPawnGC.DotgraphIdentifier(pawn), WorldPawnGC.DotgraphIdentifier(current.otherPawn), current.def);
						if (!logDotgraphUniqueLinks.Contains(text)) {
							logDotgraphUniqueLinks.Add(text);
							logDotgraph.AppendLine(text);
						}
					}
					if (!keptPawns.ContainsKey(current.otherPawn)) {
						keptPawns[current.otherPawn] = "Memory";
					}
				}
			}
		}
		
		static public void RunGC(WorldPawnGC __instance)
		{
			__instance.CancelGCPass();
			PerfLogger.Reset();
			PawnGCPass(__instance).ExecuteEnumerable();
			float num = PerfLogger.Duration() * 1000f;
			PerfLogger.Flush();
			Log.Message(string.Format("World pawn GC run complete in {0} ms", num));
		}
		
		static public void LogDotgraph(WorldPawnGC __instance)
		{
			logDotgraph = new StringBuilder();
			logDotgraphUniqueLinks = new HashSet<string>();
			GUIUtility.systemCopyBuffer = "building log dot graph...";
			logDotgraph.AppendLine("digraph { rankdir=LR;");
			AccumulatePawnGCData(__instance,new Dictionary<Pawn, string>()).ExecuteEnumerable();
			logDotgraph.AppendLine("}");
			GUIUtility.systemCopyBuffer = logDotgraph.ToString();
			Log.Message("Dotgraph copied to clipboard");
			logDotgraph = null;
			logDotgraphUniqueLinks = null;
		}
		
		static public string PawnGCDebugResults(WorldPawnGC __instance)
		{
			Dictionary<Pawn, string> dictionary = new Dictionary<Pawn, string>();
			AccumulatePawnGCData(__instance,dictionary).ExecuteEnumerable();
			Dictionary<string, int> countReasons = new Dictionary<string, int>();
			foreach (Pawn current in Find.WorldPawns.AllPawnsAlive) {
				string reason;
				if ( !dictionary.TryGetValue(current, out reason) ) {
					reason = "Discarded";
				}
				int counter;
				if ( !countReasons.TryGetValue(reason, out counter) ) {
					counter = 0;
				}
				countReasons[reason] = ++counter;
			}
			return GenText.ToLineList(from kvp in countReasons
			orderby kvp.Value descending
			select string.Format("{0}: {1}", kvp.Value, kvp.Key));
		}
	}
	
	[HarmonyPatch(typeof(WorldPawnGC))]
	[HarmonyPatch("WorldPawnGCTick")]
	[HarmonyPatchNamespace("WorldPawnGC")]
	static class WorldPawnGC_WorldPawnGCTick {
		static bool Prefix(WorldPawnGC __instance)
		{
			return WorldPawnGCOptimized.WorldPawnGCTickPrefix(__instance);
		}
	}
	
	[HarmonyPatch(typeof(WorldPawnGC))]
	[HarmonyPatch("RunGC")]
	[HarmonyPatchNamespace("WorldPawnGC")]
	static class WorldPawnGC_RunGC {
		static bool Prefix(WorldPawnGC __instance)
		{
			WorldPawnGCOptimized.RunGC(__instance);
			return false;
		}
	}
	
	[HarmonyPatch(typeof(WorldPawnGC))]
	[HarmonyPatch("LogDotgraph")]
	[HarmonyPatchNamespace("WorldPawnGC")]
	static class WorldPawnGC_LogDotgraph {
		static bool Prefix(WorldPawnGC __instance)
		{
			WorldPawnGCOptimized.LogDotgraph(__instance);
			return false;
		}
	}
	
	[HarmonyPatch(typeof(WorldPawnGC))]
	[HarmonyPatch("PawnGCDebugResults")]
	[HarmonyPatchNamespace("WorldPawnGC")]
	static class WorldPawnGC_PawnGCDebugResults {
		static bool Prefix(WorldPawnGC __instance, out string __result)
		{
			__result = WorldPawnGCOptimized.PawnGCDebugResults(__instance);
			return false;
		}
	}
}

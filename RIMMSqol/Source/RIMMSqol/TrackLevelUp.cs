/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 29.05.2017
 * Time: 01:19
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Harmony;

namespace RIMMSqol
{	
	[HarmonyPatch(typeof(SkillRecord))]
	[HarmonyPatch("Learn")]
	[HarmonyPatchNamespace("TrackLevelUp")]
	static public class TrackLevelUp
	{
		static bool Prefix(SkillRecord __instance, out int __state) {
			__state = __instance.levelInt;
			return true;
		}
		
		static void Postfix(SkillRecord __instance, int __state) {
			//if we detect that the level changed we add a symbol for levelUp or levelDown to the pawn whose skill changed.
			if ( __state != __instance.levelInt ) {
				foreach (Pawn p in Find.CurrentMap.mapPawns.FreeColonistsSpawned) {
					if ( p.skills.GetSkill(__instance.def) == __instance ) {
						ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamed("QOLLeveled"+__instance.def.defName,false);
						if ( thought == null ) thought = DefDatabase<ThoughtDef>.GetNamed("QOLLeveled");
						Thought_Memory newThought = ThoughtMaker.MakeThought(thought,__state < __instance.levelInt ? 1 : 0);
						p.needs.mood.thoughts.memories.TryGainMemory(newThought, null);
						break;
					}
				}
			}
		}
	}
	
	[HarmonyPatch(typeof(SkillRecord))]
	[HarmonyPatch("Interval")]
	[HarmonyPatchNamespace("StopSkillDecay")]
	static public class StopSkillDecay
	{
		private static bool stopSkillDecay;
		
		public static bool Prepare() {
			QOLMod.addApplySettingsListener(mod=>stopSkillDecay = QOLMod.stopSkillDecay());
			stopSkillDecay = QOLMod.stopSkillDecay();
			return true;
		}
		
		static bool Prefix() {
			return !stopSkillDecay;
		}
	}
}

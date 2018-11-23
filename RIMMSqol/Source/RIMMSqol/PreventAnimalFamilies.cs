/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 31.05.2018
 * Time: 04:14
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using Harmony;
using RimWorld;
using Verse;

namespace RIMMSqol
{
	[HarmonyPatch(typeof(Pawn_RelationsTracker))]
	[HarmonyPatch("AddDirectRelation")]
	[HarmonyPatchNamespace("PreventAnimalFamilies")]
	static class PreventAnimalFamilies_Pawn_RelationsTracker_AddDirectRelation {
		static bool Prefix(PawnRelationDef def, Pawn otherPawn)
		{
			//Prevent the association of an animal parent to prevent the sprawling animal family trees.
			return !QOLMod.preventAnimalFamilies() || def != PawnRelationDefOf.Parent || otherPawn.RaceProps == null || !otherPawn.RaceProps.Animal;
		}
	}
	
	[HarmonyPatch(typeof(TaleRecorder))]
	[HarmonyPatch("RecordTale")]
	[HarmonyPatchNamespace("PreventAnimalFamilies")]
	static class PreventAnimalFamilies_TaleRecorder_RecordTale {
		static bool Prefix(TaleDef def, params object[] args)
		{
			//Prevent the recording of birth if both parents are animals to prevent unnecessary tales that keep global pawns from being garbage collected if they are used as art.
			return !QOLMod.preventAnimalFamilies() || def != TaleDefOf.GaveBirth || args == null || args.Length < 1 || !IsAnimal(args[0] as Pawn) || args.Length < 2 || !IsAnimal(args[1] as Pawn);
		}
		
		static bool IsAnimal(Pawn p) {
			return p != null && p.RaceProps != null && p.RaceProps.Animal;
		}
	}
}

/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 10.09.2018
 * Time: 10:51
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using Harmony;
using RimWorld;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Harmony fails to patch this method for some reason...
	/// </summary>
	/*[HarmonyPatch(typeof(TrainableUtility))]
	[HarmonyPatch("TamenessCanDecay")]
	[HarmonyPatch(new[]{typeof(ThingDef)})]
	[HarmonyPatchNamespace("StopTamenessDecay")]
	static public class PreventTamenessDecay_TrainableUtility_TamenessCanDecay
	{
		//private static bool stopTamenessDecay = true;
		
		//public static bool Prepare() {
		//	QOLMod.addApplySettingsListener(mod=>stopTamenessDecay = QOLMod.stopTamenessDecay());
		//	stopTamenessDecay = QOLMod.stopTamenessDecay();
		//	return true;
		//}
		
		static bool Prefix() {
			Log.Message("Prefix");
			return true;
		}
		
		static void Postfix() {
			Log.Message("Postfix");
			//if ( stopTamenessDecay ) {
			//	__result = false;
			//	Log.Message("stopping tameness decay");
			//} else {
			//	Log.Message("default tameness decay");
			//}
		}
	}*/
	
	[HarmonyPatch(typeof(Pawn_TrainingTracker))]
	[HarmonyPatch("TrainingTrackerTickRare")]
	[HarmonyPatchNamespace("StopTamenessDecay")]
	static public class PreventTamenessDecay_Pawn_TrainingTracker_Pawn_TrainingTracker
	{
		static bool stopTamenessDecay = true;
		
		static bool Prepare() {
			QOLMod.addApplySettingsListener(mod=>stopTamenessDecay = QOLMod.stopTamenessDecay());
			stopTamenessDecay = QOLMod.stopTamenessDecay();
			return true;
		}
		
		static void Prefix(DefMap<TrainableDef, int> ___steps, out DefMap<TrainableDef, int> __state) {
			if ( stopTamenessDecay ) {
				__state = new DefMap<TrainableDef, int>();
				foreach ( KeyValuePair<TrainableDef, int> kvp in ___steps ) {
					__state[kvp.Key] = kvp.Value;
				}
			} else {
				__state = null;
			}
		}
		
		static void Postfix(DefMap<TrainableDef, int> ___steps, DefMap<TrainableDef, int> __state) {
			if ( stopTamenessDecay ) {
				foreach ( KeyValuePair<TrainableDef, int> kvp in ___steps ) {
					___steps[kvp.Key] = __state[kvp.Key];
				}
			}
		}
	}
}

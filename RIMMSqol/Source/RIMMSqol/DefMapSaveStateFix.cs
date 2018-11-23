/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 16.07.2017
 * Time: 05:32
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using Verse;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Harmony;
using UnityEngine;

namespace RIMMSqol
{
	/// <summary>
	/// In the vanilla game DefMaps are saving their content as a list of values. 
	/// If mods add new definitions loading an old save will not create default values for the new entries. This crashes things like the colonists record page.
	/// If the order of definitions changes, then the values are assigned to the wrong fields.
	/// This patch will fill DefMaps with default values for missing entries after it was loaded and it will save the definitions key to ensure that values are always
	/// assigned to the correct definition.
	/// 
	/// Patch needs to exist for each concrete defmap implementation that uses the defmap to save its state(on other implementations it simply wont change anything).
	/// A18 uses defmaps with these defs to save data: JoyKindDef(is joy coming from social, work etc. sources),RecordDef,TrainableDef(e.g. train "release" or "obedience"),WorkTypeDef(mining, crafting, ... but not virtual categories like smithing)
	/// JoyKindDef would crash if a new category like fetish is added and a pawn with masochist trait gains joy from his fetish source when being wounded.
	/// RecordDef: Add a new RecordDef, load a save game, open the records page of a pawn and the page comes up empty due to an error in defmap.
	/// WorkTypeDef: Add a new worktype, load a save game, open work tab. Again an exception in DefMap prevents the window from rendering.
	/// 
	/// Tested WorkTypeDef and RecordDef both work fine if the DefMap is patched.
	/// </summary>
	static public class DefMapSaveStateFix
	{
		static public void Postfix<T,K>(DefMap<T, K> __instance) where T : Def, new() where K : new() {
			if (Scribe.mode == LoadSaveMode.Saving) {
				List<string> keys = new List<string>();
				foreach ( T def in DefDatabase<T>.AllDefsListForReading.OrderBy(e=>e.index) ) {
					keys.Add(def.defName);
				}
				Scribe_Collections.Look<string>(ref keys, "keys", LookMode.Undefined, new object[0]);
			} else {
				//grabbing the values via reflection. The llokup can be cached per load for performance.
				System.Reflection.FieldInfo fi = __instance.GetType().GetField("values", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				List<K> values = (List<K>)fi.GetValue(__instance);
				List<string> keys = new List<string>();
				Scribe_Collections.Look<string>(ref keys, "keys", LookMode.Undefined, new object[0]);
				//we only need to save and calculate the keys once per save. To keep the patch simple we save it multiple times
				List<int> valueMapping = new List<int>(values.Count);
				if ( keys.NullOrEmpty() ) { //old save without keys, we must assume the order is matching the database ordering
					for ( int index = 0; index < values.Count; index++ ) {
						valueMapping.Add(index);
					}
				} else {
					foreach ( string defName in keys ) {
						T def = DefDatabase<T>.GetNamed(defName, false);
						if ( def == null ) {
							//we no longer use the value specified at this index.
							valueMapping.Add(-1);
						} else {
							valueMapping.Add(def.index);
						}
					}
				}
				
				List<K> oldValues = new List<K>(values.Count);
				oldValues.AddRange(values);
				values.Clear();
				//fill the list until it has enough entries
				foreach ( T def in DefDatabase<T>.AllDefsListForReading.OrderBy(e=>e.index) ) {
					values.Add(default(K));
				}
				//assign the saved values to the right spot
				for ( int index = 0; index < valueMapping.Count; index++ ) {
					if ( valueMapping[index] >= 0 ) {
						values[valueMapping[index]] = oldValues[index];
					}
				}
			}
		}
	}
	
	[HarmonyPatch(typeof(DefMap<RecordDef, float>))]
	[HarmonyPatch("ExposeData")]
	[HarmonyPatchNamespace("DefMap")]
	static public class DefMapSaveStateFixRecordDef
	{
		static void Postfix(DefMap<RecordDef, float> __instance) {
			DefMapSaveStateFix.Postfix<RecordDef,float>(__instance);
		}
	}
	
	[HarmonyPatch(typeof(DefMap<WorkTypeDef, int>))]
	[HarmonyPatch("ExposeData")]
	[HarmonyPatchNamespace("DefMap")]
	static public class DefMapSaveStateFixWorkTypeDef
	{
		static void Postfix(DefMap<WorkTypeDef, int> __instance) {
			DefMapSaveStateFix.Postfix<WorkTypeDef,int>(__instance);
		}
	}
}

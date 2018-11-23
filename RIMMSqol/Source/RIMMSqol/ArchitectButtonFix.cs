/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 18.04.2018
 * Time: 05:03
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of ArchitectButtonFix.
	/// </summary> 
	[HarmonyPatch(typeof(MainTabWindow_Architect))]
	[HarmonyPatch("CacheDesPanels")]
	[HarmonyPatchNamespace("ArchitectButtonVisibility")]
	public static class ArchitectButtonFix
	{
		static bool Prefix(MainTabWindow_Architect __instance) {
			try {
				List<ArchitectCategoryTab> desPanelsCached = new List<ArchitectCategoryTab>();
				foreach (DesignationCategoryDef current in from dc in DefDatabase<DesignationCategoryDef>.AllDefs 
				         where dc.GetModExtension<QOLDefModExtension_DesignationCategoryDef>() == null || dc.GetModExtension<QOLDefModExtension_DesignationCategoryDef>().visible
					orderby dc.order descending select dc) {
					desPanelsCached.Add(new ArchitectCategoryTab(current));
				}
				typeof(MainTabWindow_Architect).GetField("desPanelsCached", System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(__instance,desPanelsCached);
				return false;
			} catch {
				Log.Error("Failed to run architect buttons patch!");
			}
			return true;
		}
	}
}

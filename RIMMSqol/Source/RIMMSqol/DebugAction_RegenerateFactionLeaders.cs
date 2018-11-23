/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 26.09.2018
 * Time: 13:22
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Reflection;
using Harmony;
using RimWorld;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of DebugAction_RegenerateFactionLeaders.
	/// </summary>
	[HarmonyPatch(typeof(Dialog_DebugActionsMenu))]
	[HarmonyPatch("DoListingItems_AllModePlayActions")]
	[HarmonyPatchNamespace("DebugMenuOptions")]
	static class DebugAction_RegenerateFactionLeaders
	{
		static void Postfix(Dialog_DebugActionsMenu __instance) {
			MethodInfo mi = typeof(Dialog_DebugOptionLister).GetMethod("DebugAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			mi.Invoke(__instance,new Object[]{"Restore faction leaders",(Action)delegate {
	          		foreach ( Faction f in Find.FactionManager.AllFactions ) {
	          			if ( f.leader == null ) {
	          				f.GenerateNewLeader();
	          				if ( f.leader == null ) {
	          					Log.Warning("No leader could be generated for faction "+f.GetCallLabel()+" - if that is unexpected fix xml definition for faction def "+f.def.defName+" and try again.");
	          				}
	          			}
	          		}
	          	}});
		}
	}
}

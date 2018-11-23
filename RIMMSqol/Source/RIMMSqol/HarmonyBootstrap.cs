/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 29.05.2017
 * Time: 01:30
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Harmony;
using Verse;
using System.Reflection;
using System.Linq;

namespace RIMMSqol
{
	/// <summary>
	/// Description of Class1.
	/// </summary>
	[StaticConstructorOnStartup]
	public static class HarmonyBootstrap
	{
		static HarmonyBootstrap()
		{
			var harmony = HarmonyInstance.Create("RIMMSqol");
			//harmony.PatchAll(Assembly.GetExecutingAssembly());
			QOLMod.ApplySettings();
			
			HarmonyPatchNamespace.registerAll();
			Dictionary<string,HarmonyPatchNamespaceProperties> namespaces = HarmonyPatchNamespace.namespaces;
			
			StringBuilder sb = new StringBuilder();
          	try {
				Assembly.GetExecutingAssembly().GetTypes().Do(delegate(Type type) {
		              	List<HarmonyMethod> harmonyMethods = type.GetHarmonyMethods();
						if (harmonyMethods != null && harmonyMethods.Count<HarmonyMethod>() > 0) {
							bool allowPatch;
							HarmonyPatchNamespace nsAttr;
							if ( type.TryGetAttribute(out nsAttr) ) {
								HarmonyPatchNamespaceProperties props;
								if ( namespaces.TryGetValue(nsAttr.id, out props) ) {
									allowPatch = props.active;
								} else {
									Log.Error("Found no configuration for existing patch namespace \""+nsAttr.id+"\". Ensure the namespaces are initialized!");
									allowPatch = false;
								}
							} else {
								allowPatch = true;
								nsAttr = null;
							}
							if ( allowPatch ) {
								sb.AppendLine("Allow patch: "+type.Namespace+":"+type.Name+(nsAttr != null ? "["+nsAttr.id+"]" : ""));
								HarmonyMethod attributes = HarmonyMethod.Merge(harmonyMethods);
								new PatchProcessor(harmony, type, attributes).Patch();
							} else {
								sb.AppendLine("Deny patch: "+type.Namespace+":"+type.Name+(nsAttr != null ? "["+nsAttr.id+"]" : ""));
							}
						}
				});
			} finally {
              		Log.Message(sb.ToString());
          	}
		}
	}
}

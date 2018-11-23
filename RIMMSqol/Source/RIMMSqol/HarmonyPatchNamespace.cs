/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.07.2018
 * Time: 20:04
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of HarmonyPatchNamespace.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class HarmonyPatchNamespace : Attribute
	{
		public static Dictionary<string,HarmonyPatchNamespaceProperties> namespaces = new Dictionary<string, HarmonyPatchNamespaceProperties>();
		public readonly string id;
		
		public HarmonyPatchNamespace(string id) {
			this.id = id;
		}
		
		public void register() {
			if ( !namespaces.ContainsKey(id) ) namespaces.Add(id,new HarmonyPatchNamespaceProperties(id));
		}
		
		static public void registerAll() {
			Assembly.GetExecutingAssembly().GetTypes().Do(delegate(Type type) {
				List<HarmonyMethod> harmonyMethods = type.GetHarmonyMethods();
				if (harmonyMethods != null && harmonyMethods.Count<HarmonyMethod>() > 0) {
					HarmonyPatchNamespace nsAttr;
					if ( type.TryGetAttribute(out nsAttr) ) {
						nsAttr.register();
					}
				}});
			QOLModSettings settings = QOLMod.getSettings();
			if ( settings.forbiddenPatchNamespaces.NullOrEmpty() ) {
				settings.forbiddenPatchNamespaces = new List<string>();
			}
			foreach ( KeyValuePair<string,HarmonyPatchNamespaceProperties> p in namespaces ) {
				if ( settings.forbiddenPatchNamespaces.Contains(p.Key) ) {
					p.Value.active = false;
				}
			}
		}
	}
	
	public class HarmonyPatchNamespaceProperties {
		public readonly string id, label, description;
		public bool active;
		
		public HarmonyPatchNamespaceProperties(string id) {
			label = id.Translate();
			description = (id+"Description").Translate();
			active = true;
		}
	}
}

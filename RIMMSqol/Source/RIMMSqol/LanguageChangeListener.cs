/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 24.07.2018
 * Time: 06:39
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using Harmony;
using RIMMSqol.genericSettings;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of LanguageChangeListener.
	/// </summary>
	[HarmonyPatch(typeof(LanguageDatabase))]
	[HarmonyPatch("LoadAllMetadata")]
	[HarmonyPatchNamespace("LanguageChangeListener")]
	static public class LanguageChangeListener
	{
		static public List<Action> translationActions = new List<Action>();
		
		static bool Prepare() {
			translate();
			return true;
		}
		
		static void Postfix() {
			translate();
		}
		
		static void translate() {
			if (LanguageDatabase.activeLanguage != null) {
				foreach ( Action action in translationActions ) {
					action();
				}
			}
		}
	}
}

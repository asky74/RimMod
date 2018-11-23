/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 14.02.2018
 * Time: 20:43
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Reflection;
using Harmony;
using UnityEngine;
using Verse;

namespace RIMMSqol
{
	[HarmonyPatch]
	[HarmonyPatchNamespace("NumericTextboxes")]
	public static class ReplaceNumericTextboxesInt {
		private static bool useFixedNumericTextfields;
		
		public static bool Prepare() {
			QOLMod.addApplySettingsListener(mod=>useFixedNumericTextfields = QOLMod.useFixedNumericTextfields());
			useFixedNumericTextfields = QOLMod.useFixedNumericTextfields();
			return true;
		}
		
    	static MethodInfo TargetMethod() {
    		return typeof(Widgets).GetMethod("TextFieldNumeric").MakeGenericMethod(typeof(int));
    	}
    	
		static bool Prefix(Rect rect, ref int val, ref string buffer, float min = 0f, float max = 1E+09f) {
			if ( useFixedNumericTextfields ) {
				WidgetsFix.TextFieldNumeric<int>(rect,ref val,ref buffer,min,max);
				return false;
			}
			return true;
		}
	}
	[HarmonyPatch]
	[HarmonyPatchNamespace("NumericTextboxes")]
	public static class ReplaceNumericTextboxesFloat {
		private static bool useFixedNumericTextfields;
		
		public static bool Prepare() {
			QOLMod.addApplySettingsListener(mod=>useFixedNumericTextfields = QOLMod.useFixedNumericTextfields());
			useFixedNumericTextfields = QOLMod.useFixedNumericTextfields();
			return true;
		}
		
    	static MethodInfo TargetMethod() {
    		return typeof(Widgets).GetMethod("TextFieldNumeric").MakeGenericMethod(typeof(float));
    	}
    	
		static bool Prefix(Rect rect, ref float val, ref string buffer, float min = 0f, float max = 1E+09f) {
			if ( useFixedNumericTextfields ) {
				WidgetsFix.TextFieldNumeric<float>(rect,ref val,ref buffer,min,max);
				return false;
			}
			return true;
		}
	}
}

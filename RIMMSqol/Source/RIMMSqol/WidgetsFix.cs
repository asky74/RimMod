/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 11.02.2018
 * Time: 03:17
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of WidgetsFix.
	/// </summary>
	public static class WidgetsFix
	{
		static string lastControlFailedValidation;
		
		public static void TextFieldNumeric<T>(Rect rect, ref T val, ref string buffer, float min = 0f, float max = 1E+09f) where T : struct
		{
			if (buffer == null) {
				buffer = ToStringTypedIn<T>(val);
			}
			
			Vector2 screenPos = GUIUtility.GUIToScreenPoint(new Vector2(rect.x,rect.y));
			string text = "TextField" + screenPos.y.ToString("F0") + screenPos.x.ToString("F0") + GUI.depth;
			GUI.SetNextControlName(text);
			Color oldColor = GUI.color;
			if ( lastControlFailedValidation == text ) {
				TooltipHandler.TipRegion(rect, () => "ValueBetween".Translate(typeof(T)==typeof(int)?"whole":"decimal",min,max), 1);
				GUI.color = Color.red;
			}
			string text2 = GUI.TextField(rect, buffer, Text.CurTextFieldStyle);
			GUI.color = oldColor;
			if (GUI.GetNameOfFocusedControl() != text) {
				ResolveParseNow<T>(buffer, ref val, ref buffer, min, max);
				if ( lastControlFailedValidation == text ) {
					lastControlFailedValidation = null;
				}
			} else {
				if (text2 != buffer) {
					buffer = text2;
					string bufferDiscard = "";
					if ( ResolveParseNow<T>(text2, ref val, ref bufferDiscard, min, max) ) {
						lastControlFailedValidation = text;
					} else {
						lastControlFailedValidation = null;
					}
				}
			}
		}
		private static string ToStringTypedIn<T>(T val)
		{
			if (typeof(T) == typeof(float)) {
				return ((float)((object)val)).ToString("g");
			} else if (typeof(T) == typeof(int)) {
				return ((int)((object)val)).ToString("g");
			}
			return val.ToString();
		}
		/// <summary>
		/// Added return value.
		/// </summary>
		/// <param name="edited"></param>
		/// <param name="val"></param>
		/// <param name="buffer"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns>True if the value has been set to a value other than the edited string. This happens if min, max restrictions trigger or the value is reset.</returns>
		private static bool ResolveParseNow<T>(string edited, ref T val, ref string buffer, float min, float max)
		{
			if (typeof(T) == typeof(int)) {
				int num;
				bool triggeredRestriction;
				if (int.TryParse(edited, out num)) {
					if ((float)num < min) {
						val = (T)((object)Mathf.CeilToInt(min));
						triggeredRestriction = true;
					} else if ((float)num > max) {
						val = (T)((object)Mathf.FloorToInt(max));
						triggeredRestriction = true;
					} else {
						val = (T)((object)num);
						triggeredRestriction = false;
					}
					buffer = ToStringTypedIn<T>(val);
				} else {
					//is 0 if the parsing failed but 0 may be outside the restrictions.
					num = default(int);
					if ( (float)num < min ) {
						val = (T)((object)Mathf.CeilToInt(min));
						triggeredRestriction = true;
					} else if ( (float)num > max ) {
						val = (T)((object)Mathf.FloorToInt(max));
						triggeredRestriction = true;
					} else {
						val = (T)((object)num);
						triggeredRestriction = true;
					}
					buffer = ToStringTypedIn<T>(val);
				}
				return triggeredRestriction;
			}
			else {
				if (typeof(T) == typeof(float)) {
					float value;
					bool triggeredRestriction;
					if (float.TryParse(edited, out value)) {
						if (value < min) {
							val = (T)((object)min);
							triggeredRestriction = true;
						} else if (value > max) {
							val = (T)((object)max);
							triggeredRestriction = true;
						} else {
							val = (T)((object)value);
							triggeredRestriction = false;
						}
						buffer = ToStringTypedIn<T>(val);
					} else {
						//is 0 if the parsing failed but 0 may be outside the restrictions.
						value = default(float);
						if ( value < min ) {
							val = (T)((object)min);
							triggeredRestriction = true;
						} else if ( value > max ) {
							val = (T)((object)max);
							triggeredRestriction = true;
						} else {
							val = (T)((object)value);
							triggeredRestriction = true;
						}
						buffer = ToStringTypedIn<T>(val);
					}
					return triggeredRestriction;
				}
				else {
					Log.Error("TextField<T> does not support " + typeof(T));
					return false;
				}
			}
		}
	}
}

/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 29.09.2017
 * Time: 22:32
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using UnityEngine;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of Listing_StandardExtensions.
	/// </summary>
	public static class Listing_StandardExtensions
	{
		public static bool ButtonImageText(this Listing_Standard ls, string label, Texture2D tex, float iconWidth, float iconHeight)
		{
			float maxHeight = Math.Max(30f,iconHeight);
			Rect rect = ls.GetRect(maxHeight);
			bool result = Widgets.ButtonImage(new Rect(rect.x, rect.y, iconWidth, iconHeight), tex);
			rect.x += iconWidth; rect.width -= iconWidth;
			result = result | Widgets.ButtonText(rect, label, true, false, true);
			ls.Gap(ls.verticalSpacing);
			return result;
		}
	}
}

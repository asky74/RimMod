/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.07.2018
 * Time: 05:12
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using UnityEngine;
using Verse;

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Description of ScrollViewRenderer.
	/// </summary>
	public class VerticalScrollViewRenderer : ParentRenderer
	{
		protected Vector2 scrollPosition;
		public override float getHeight() {
			float totalHeight = 0;
			foreach ( BaseRenderer cr in childs ) {
				totalHeight += cr.getHeight();
			}
			return totalHeight;
		}
		public override float getWidth() { return -1; }
		
		public override void DoComponentContents(Rect inRect, Flow flow) {
			float height = getHeight();
			
			Rect viewRect = new Rect(0,0,inRect.width,Mathf.Max(height,inRect.height));
			if ( height > inRect.height ) {
				viewRect.width -= QOLMod.VerticalScrollbarWidth();
			}
			
			Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
			Rect row = new Rect(0,0,viewRect.width,0);
			foreach ( BaseRenderer cr in childs ) {
				row.height = cr.getHeight();
				cr.DoComponentContents(row, flow);
				row.y += row.height;
			}
			Widgets.EndScrollView();
		}
	}
}

/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 04.11.2017
 * Time: 22:55
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RIMMSqol.renderers
{
	public class ListRenderer : ParentRendererWithIteration {
		protected IList items;
		protected Func<Flow,IList> exprItems;
		protected float width, height, lastMeasuredHeight;
		protected Vector2 scrollPosition;
		public override float getHeight() {
			return height;
		}
		public override float getWidth() { 
			return width;
		}
		public ListRenderer(IList items, float width = 200f, float height = 100f) {
			this.exprItems = null;
			this.items = items;
			this.width = width;
			this.height = height;
			this.lastMeasuredHeight = -1;
		}
		public ListRenderer(Func<Flow,IList> exprItems, float width = 200f, float height = 100f) {
			this.exprItems = exprItems;
			this.items = null;
			this.width = width;
			this.height = height;
			this.lastMeasuredHeight = -1;
		}
		public override void DoComponentContents(Rect inRect, Flow flow) {
			if ( exprItems != null ) {
				items = exprItems(flow);
			}
			if ( items == null || items.Count == 0 ) return;
			
			Rect viewRect = new Rect(inRect);
			viewRect.height = Mathf.Max(lastMeasuredHeight,inRect.height);
			viewRect.width -= QOLMod.VerticalScrollbarWidth();
			Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
			
			beginIteration(flow);
			
			lastMeasuredHeight = 0;
			Rect cell = new Rect(viewRect);
			foreach ( object curItem in items ) {
				advanceIteration(curItem);
				
				float rowheight = 0;
				foreach ( BaseRenderer cr in childs ) {
					rowheight = Math.Max(rowheight, cr.getHeight());
				}
				cell.height = rowheight;
				if ( cell.y-scrollPosition.y < inRect.y+inRect.height && cell.y+cell.height-scrollPosition.y > inRect.y ) {
					float x = cell.x;
					foreach ( BaseRenderer cr in childs ) {
						cell.width = cr.getWidth();
						cr.DoComponentContents(cell, flow);
						cell.x += cell.width;
					}
					cell.x = x;
				}
				cell.y += cell.height;
				lastMeasuredHeight += cell.height;
			}
			
			endIteration(flow);
			
			Widgets.EndScrollView();
		}
	}
}

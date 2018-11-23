/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 16.10.2017
 * Time: 02:53
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Look And Feel from a select dialog as a renderer.
	/// </summary>
	public class FilteredGridListRenderer<T> : ParentRendererWithIteration
	{
		protected IOrderedEnumerable<T> items;
		protected Func<string,T,bool> filter;
		protected Func<IOrderedEnumerable<T>,IOrderedEnumerable<T>> dynamicSorter;
		protected float totalOptionsWidth = -1, height, width;
		protected string filterExpr;
		protected Vector2 scrollPosition;
		protected float horizontalGap = 17f, verticalGap = 2f, childGap = 2f;
		
		public FilteredGridListRenderer(IEnumerable<T> items, float width, float height, Func<string,T,bool> filter = null, Func<IOrderedEnumerable<T>,IOrderedEnumerable<T>> dynamicSorter = null)
		{
			if ( items is IOrderedEnumerable<T> ) this.items = (IOrderedEnumerable<T>)items;
			else this.items = items.OrderBy(d=>0);
			this.width = width;
			this.height = height;
			this.filter = filter;
			this.dynamicSorter = dynamicSorter;
		}
		
		public override float getHeight() {
			return height;
		}
		public override float getWidth() {
			return width;
		}
		
		public override void DoComponentContents(Rect inRect, Flow flow) {
			Rect viewRect = new Rect(inRect);
			
			if ( filter != null ) {
				this.filterExpr = Widgets.TextField(new Rect(inRect.x, inRect.y, 200f, 30f), this.filterExpr);
				viewRect.yMin += 35f;
			}
			
			if ( dynamicSorter != null ) items = dynamicSorter(items);
			
			Rect contentRect = new Rect(0f, 0f, totalOptionsWidth > 0 ? totalOptionsWidth : viewRect.width, viewRect.height - QOLMod.HorizontalScrollbarHeight());
			Widgets.BeginScrollView(viewRect, ref this.scrollPosition, contentRect, true);
			
			beginIteration(flow);
			
			Rect r = new Rect(contentRect.x, contentRect.y, 0, 0);
			float columnStart = r.x, maxColumnWidth = 0;
			foreach ( T d in items ) {
				if ( filter == null || filter(filterExpr, d) ) {
					advanceIteration(d);
					
					r.height = 0;
					foreach ( BaseRenderer cr in childs ) {
						r.height = Mathf.Max(r.height, cr.getHeight());
					}
					
					if ( r.yMax > contentRect.height ) {
						r.y = contentRect.yMin;
						r.x += maxColumnWidth + horizontalGap;
						columnStart = r.x;
						maxColumnWidth = 0;
					}
					
					foreach ( BaseRenderer cr in childs ) {
						r.width = cr.getWidth();
						cr.DoComponentContents(r, flow);
						r.x += r.width + childGap;
					}
					maxColumnWidth = Mathf.Max(maxColumnWidth, r.x - columnStart - childGap);
					r.x = columnStart;
					r.y += r.height + verticalGap;
				}
			}
			totalOptionsWidth = columnStart + maxColumnWidth + horizontalGap - contentRect.x;
			
			endIteration(flow);
			
			Widgets.EndScrollView();
		}
	}
}

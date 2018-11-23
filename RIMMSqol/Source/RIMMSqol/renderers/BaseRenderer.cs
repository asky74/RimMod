/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 16.10.2017
 * Time: 02:38
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using UnityEngine;
using Verse;

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Description of Renderer.
	/// </summary>
	public abstract class BaseRenderer {
		public Func<Flow,string> tooltipExpr;
		public BaseRenderer attachTooltip(string tooltip) {
			if ( !tooltip.NullOrEmpty() ) this.tooltipExpr = (f => tooltip);
			else this.tooltipExpr = null;
			return this;
		}
		public BaseRenderer attachTooltip(Func<Flow,string> tooltipExpr) {
			this.tooltipExpr = tooltipExpr;			
			return this;
		}
		public void displayToolTipIfNecessary(Rect inRect, Flow flow, string prefix = null) {
			if ((tooltipExpr != null||prefix != null) && Mouse.IsOver(inRect)) {
				string str;
				if ( tooltipExpr != null ) str = tooltipExpr(flow);
				else str = null;
				if ( !prefix.NullOrEmpty() ) {
					if ( str.NullOrEmpty() ) str = prefix;
					else str = prefix+" | "+str;
				}
				if ( str != null ) TooltipHandler.TipRegion(inRect, () => str, 1);
			}
		}
		public abstract float getHeight();
		public abstract float getWidth();
		public abstract void DoComponentContents(Rect inRect, Flow flow);
		public virtual void PostNavigation() {}
	}
}

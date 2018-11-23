/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.07.2018
 * Time: 02:07
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using UnityEngine;

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Description of HideRenderer.
	/// </summary>
	public class HideRenderer : BaseRenderer {
		protected Func<Flow,bool> hiddenOn;
		public BaseRenderer child;
		public override float getHeight() {
			return child.getHeight();
		}
		public override float getWidth() {
			return child.getWidth();
		}
		public HideRenderer(Func<Flow,bool> hiddenOn, BaseRenderer child) {
			this.hiddenOn = hiddenOn;
			this.child = child;
		}
		public override void DoComponentContents(Rect inRect, Flow flow) {
			if ( hiddenOn != null && hiddenOn(flow) ) {
				return;
			}
			child.DoComponentContents(inRect,flow);
		}
	}
}

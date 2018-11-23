/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.07.2018
 * Time: 02:10
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using UnityEngine;
using Verse;

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Description of CheckboxRenderer.
	/// </summary>
	public class CheckboxRenderer : BaseRenderer {
		protected Func<Flow,bool> valueExpr;
		protected Action<Flow,bool> onChange;
		protected float size;
		public override float getHeight() {
			return size;
		}
		public override float getWidth() {
			return size;
		}
		public CheckboxRenderer(Func<Flow,bool> valueExpr, Action<Flow,bool> onChange, float size = 24f) {
			this.valueExpr = valueExpr;
			this.onChange = onChange;
			this.size = size;
		}
		public override void DoComponentContents(Rect inRect, Flow flow) {
			bool value = valueExpr(flow);
			bool valueOriginal = value;
			displayToolTipIfNecessary(inRect, flow);
			Widgets.Checkbox(new Vector2(inRect.x,inRect.y), ref value, size);
			if ( valueOriginal != value ) onChange(flow,value);
		}
	}
}

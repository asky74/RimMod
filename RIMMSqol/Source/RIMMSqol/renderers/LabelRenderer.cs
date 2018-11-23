/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 05.12.2017
 * Time: 04:11
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using UnityEngine;
using Verse;

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Description of LabelRenderer.
	/// </summary>
	public class LabelRenderer : BaseRenderer {
		protected Func<Flow,string> labelExpr;
		protected string label;
		protected GameFont font;
		protected float width;
		protected TextAnchor anchor;
		public override float getHeight() {
			return QOLMod.LineHeight(font);
		}
		public override float getWidth() {
			return width;
		}
		public LabelRenderer(string label, float width = -1f, GameFont font = GameFont.Small, TextAnchor anchor = TextAnchor.MiddleLeft) {
			this.label = label;
			this.font = font;
			this.width = width;
			this.anchor = anchor;
			if ( this.width < 0 ) {
				this.width = QOLMod.CalcSize(this.label,font,this.anchor,false).x;
			}
			this.labelExpr = null;
		}
		public LabelRenderer(Func<Flow,string> labelExpr, float width, GameFont font = GameFont.Small, TextAnchor anchor = TextAnchor.MiddleLeft) {
			this.labelExpr = labelExpr;
			this.font = font;
			this.label = null;
			this.width = width;
			this.anchor = anchor;
		}
		public override void DoComponentContents(Rect inRect, Flow flow) {
			Text.Font = font;
			Text.Anchor = anchor;
			bool wasCapped;
			string labelToRender = label!=null?label:labelExpr(flow);
			string labelCapped = QOLMod.CapString(labelToRender,inRect.width,"...",out wasCapped,font,anchor);
			displayToolTipIfNecessary(inRect, flow, wasCapped ? labelToRender : null);
			Widgets.Label(inRect, labelCapped);
		}
	}
}

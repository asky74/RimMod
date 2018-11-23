/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 05.12.2017
 * Time: 14:27
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using UnityEngine;
using Verse;

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Description of ButtonTextRenderer.
	/// </summary>
	public class ButtonTextRenderer : BaseRenderer {
		public string label;
		protected Func<Flow,string> exprLabel;
		public Action<Flow> onClick;
		public GameFont font;
		protected float width;
		public ButtonTextRenderer(string label, Action<Flow> onClick, GameFont font = GameFont.Small, float width = -1) {
			this.exprLabel = null;
			this.label = label;
			this.onClick = onClick;
			this.font = font;
			this.width = width;
		}
		public ButtonTextRenderer(Func<Flow,string> exprLabel, Action<Flow> onClick, GameFont font = GameFont.Small, float width = -1) {
			this.exprLabel = exprLabel;
			this.label = null;
			this.onClick = onClick;
			this.font = font;
			this.width = width;
		}
		public override float getHeight() {
			return QOLMod.LineHeight(font) + 4f;
		}
		public override float getWidth() {
			if ( width >= 0 ) return width;
			return QOLMod.CalcSize(label, font, TextAnchor.MiddleCenter, false).x + 8f;
		}
		public override void DoComponentContents(Rect inRect, Flow flow) {
			Text.Font = font;
			bool wasCapped;
			string labelToRender = label!=null?label:exprLabel(flow);
			string labelCapped = QOLMod.CapString(labelToRender,inRect.width,"...",out wasCapped,font);
			displayToolTipIfNecessary(inRect, flow, wasCapped ? labelToRender : null);
			if ( Widgets.ButtonText(inRect, labelCapped) ) {
				onClick(flow);
			}
		}
	}
}

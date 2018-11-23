/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 05.12.2017
 * Time: 14:26
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using UnityEngine;
using Verse;

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Description of ButtonRenderer.
	/// </summary>
	public class ButtonRenderer : BaseRenderer {
		protected Texture2D iconTexture;
		protected Action<Flow> onClick;
		protected float height = -1, width = -1;
		protected Func<Flow,bool> hiddenOn;
		public override float getHeight() {
			if ( height >= 0 ) return height;
			return iconTexture.height;
		}
		public override float getWidth() {
			if ( width >= 0 ) return width;
			return iconTexture.width;
		}
		public ButtonRenderer hidden(Func<Flow,bool> hiddenOn) {
			this.hiddenOn = hiddenOn;
			return this;
		}
		public ButtonRenderer(Texture2D iconTexture, Action<Flow> onClick, float width = -1, float height = -1) {
			this.iconTexture = iconTexture;
			this.onClick = onClick;
			this.width = width;
			this.height = height;
		}
		public override void DoComponentContents(Rect inRect, Flow flow) {
			if ( hiddenOn != null && hiddenOn(flow) ) {
				return;
			}
			displayToolTipIfNecessary(inRect, flow);
			if ( Widgets.ButtonImage(inRect, iconTexture) ) {
				onClick(flow);
			}
		}
	}
}

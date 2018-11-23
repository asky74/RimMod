/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 29.09.2018
 * Time: 14:38
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using UnityEngine;
using Verse;

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Description of ColorEditRenderer.
	/// </summary>
	public class EditColorRenderer : BaseRenderer {
		protected Func<Flow,Color> exprColor;
		protected float width, height;
		protected Action<Flow,Color> onChange;
		public EditColorRenderer(Func<Flow,Color> exprColor, Action<Flow,Color> onChange, float width) {
			this.width = width;
			this.exprColor = exprColor;
			this.onChange = onChange;
		}
		public override float getHeight() {
			return QOLMod.LineHeight(GameFont.Tiny) * 4;
		}
		public override float getWidth() {
			return width;
		}
		public override void DoComponentContents(Rect inRect, Flow flow) {
			float lineHeight = QOLMod.LineHeight(GameFont.Tiny);
			float tripleLineHeight = lineHeight * 4;
			Rect nextSquare = new Rect(inRect.x, inRect.y, width, tripleLineHeight);
			
			Color originalColor = exprColor(flow);
			Color currentColor = originalColor;
			GUI.DrawTexture(nextSquare, QOLMod.getSolidColorTexture(currentColor));
			Rect rgbLine = new Rect(nextSquare); rgbLine.height = lineHeight;
			currentColor.r = Widgets.HorizontalSlider(rgbLine, currentColor.r, 0, 1f); 
			rgbLine.y += lineHeight;
			currentColor.g = Widgets.HorizontalSlider(rgbLine, currentColor.g, 0, 1f); 
			rgbLine.y += lineHeight;
			currentColor.b = Widgets.HorizontalSlider(rgbLine, currentColor.b, 0, 1f);
			rgbLine.y += lineHeight;
			currentColor.a = Widgets.HorizontalSlider(rgbLine, currentColor.a, 0, 1f);
			
			if ( !originalColor.IndistinguishableFrom(currentColor) ) {
				onChange(flow,currentColor);
		    }
		}
	}
}

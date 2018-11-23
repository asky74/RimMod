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

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Description of RowLayoutRenderer.
	/// </summary>
	public class RowLayoutRenderer : ParentRenderer {
		public float paddingBetweenRows = 4f;
		public override float getHeight() {
			float totalHeight = 0;
			foreach ( BaseRenderer cr in childs ) {
				totalHeight += cr.getHeight() + paddingBetweenRows;
			}
			return totalHeight;
		}
		public override float getWidth() { return -1; }
		
		public override void DoComponentContents(Rect inRect, Flow flow) {
			Rect row = new Rect(inRect.x,inRect.y,inRect.width,0);
			foreach ( BaseRenderer cr in childs ) {
				row.height = cr.getHeight();
				cr.DoComponentContents(row, flow);
				row.y += row.height + paddingBetweenRows;
			}
		}
	}
}

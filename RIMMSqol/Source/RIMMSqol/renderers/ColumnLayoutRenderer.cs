/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.07.2018
 * Time: 02:11
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using UnityEngine;

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Description of ColumnLayoutRenderer.
	/// </summary>
	public class ColumnLayoutRenderer : ParentRenderer {
		public float paddingBetweenColumns = 4f;
		public override float getHeight() {
			float maxHeight = 0;
			foreach ( BaseRenderer cr in childs ) {
				maxHeight = Math.Max(maxHeight,cr.getHeight());
			}
			return maxHeight;
		}
		public override float getWidth() { 
			float totalWidth = 0;
			foreach ( BaseRenderer cr in childs ) {
				totalWidth += cr.getWidth();
			}
			totalWidth += (float)childs.Count * paddingBetweenColumns;
			return totalWidth;
		}
		public override void DoComponentContents(Rect inRect, Flow flow) {
			Rect column = new Rect(inRect.x,inRect.y,0,inRect.height);
			foreach ( BaseRenderer cr in childs ) {
				column.width = cr.getWidth();
				cr.DoComponentContents(column, flow);
				column.x += column.width + paddingBetweenColumns;
			}
		}
	}
}

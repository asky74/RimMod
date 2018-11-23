/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.07.2018
 * Time: 02:12
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Description of IterationItem.
	/// </summary>
	public class IterationItem {
		public object parentItem;
		public int index;
		public object curItem;
		public IterationItem(object parentItem, int index = -1) {
			this.parentItem = parentItem;
			this.index = index;
		}
		public IterationItem(object curItem, object parentItem, int index = 0) {
			this.curItem = curItem;
			this.parentItem = parentItem;
			this.index = index;
		}
		public void advance(object nextItem) {
			curItem = nextItem;
			index++;
		}
	}
}

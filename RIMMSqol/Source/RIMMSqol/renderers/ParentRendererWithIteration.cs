/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 17.10.2017
 * Time: 18:25
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Description of ParentRendererWithIteration.
	/// </summary>
	public abstract class ParentRendererWithIteration : ParentRenderer
	{
		protected IterationItem itit;
		protected void beginIteration(Flow flow) {
			object parentItem;
			if ( !flow.pageScope.TryGetValue("curItem", out parentItem) ) {
				parentItem = null;
			}
			itit = new IterationItem(parentItem);
			flow.pageScope["curItem"] = itit;
		}
		protected void endIteration(Flow flow) {
			if ( itit.parentItem != null ) flow.pageScope["curItem"] = itit.parentItem;
			else flow.pageScope.Remove("curItem");
		}
		protected void advanceIteration(object obj) {
			itit.advance(obj);
		}
	}
}

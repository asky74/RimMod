/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 16.10.2017
 * Time: 02:37
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Description of ParentRenderer.
	/// </summary>
	public abstract class ParentRenderer : BaseRenderer {
		public List<BaseRenderer> childs;
		protected ParentRenderer() {
			childs = new List<BaseRenderer>();
		}
		public ParentRenderer AddChild(BaseRenderer child) {
			if ( child != null ) childs.Add(child);
			return this;
		}
		public virtual new void PostNavigation() {
			foreach ( BaseRenderer cr in childs ) {
				cr.PostNavigation();
			}
		}
	}
}

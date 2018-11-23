/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 22.08.2017
 * Time: 22:13
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using RIMMSqol.renderers;
using Verse;
using UnityEngine;

namespace RIMMSqol
{
	/// <summary>
	/// Description of Dialog_Flow.
	/// </summary>
	public class Dialog_Flow : Window
	{
		protected Flow flow;
		
		public Dialog_Flow(Flow flow) {
			this.flow = flow;
			this.closeOnCancel = true;
			this.doCloseX = true;
			this.onlyOneOfTypeAllowed = true;
			this.absorbInputAroundWindow = true;
			this.forcePause = true;
		}
		
		public override void DoWindowContents(Rect inRect)
		{
			if ( flow != null ) flow.DoFlowContents(inRect);
		}
		
		public override void PostClose()
		{
			base.PostClose();
			if ( flow != null ) flow.destroy();
			flow = null;
		}
	}
}

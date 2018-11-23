/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 16.11.2017
 * Time: 03:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using RimWorld;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of QOLRecordFactorDef.
	/// </summary>
	public class QOLRecordFactorDef : Def
	{
		public RecordDef recordDef;
		public float factor;
		public override void ResolveReferences()
		{
			base.ResolveReferences();
			label = recordDef.label;
			if ( label.NullOrEmpty() ) {
				Log.Message("Missing label for: "+recordDef.defName);
			}
		}
	}
}

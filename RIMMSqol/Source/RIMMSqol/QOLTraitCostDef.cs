/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 15.11.2017
 * Time: 08:26
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using RimWorld;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of QOLTraitCostDef.
	/// </summary>
	public class QOLTraitCostDef : Def
	{
		public TraitDef traitDef;
		public int traitDegree;
		public float cost;
		[DefaultValue(0.0), Description("Seperate cost for removing the trait. Zero means identical to normal cost.")]
		public float costRemoval;
		public override void ResolveReferences()
		{
			base.ResolveReferences();
			label = traitDef.DataAtDegree(traitDegree).label;
			if ( label.NullOrEmpty() ) {
				Log.Message("Missing label for: "+traitDef.defName+"/"+traitDegree);
			}
		}
	}
}

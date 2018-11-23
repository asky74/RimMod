/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.10.2018
 * Time: 15:41
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RIMMSqol.remnantcolony
{
	/// <summary>
	/// Description of QOLSocietyIndicatorDef.
	/// </summary>
	public class QOLSocietyIndicatorDef : Def, IComparable<QOLSocietyIndicatorDef>
	{
		/*[Unsaved]
		private bool unsaved;
		
		[Description("The name of this Def. It is used as an identifier by the game code."), NoTranslate]
		public string defName = "UnnamedDef";

		[DefaultValue(null), Description("A human-readable label used to identify this in game."), MustTranslate]
		public string label;*/
		
		[DefaultValue(null), Description("A described and labeled representation for the intensity of the indicator.")]
		public List<QOLSocietyIndicatorStage> stages;
		
		[DefaultValue(int.MaxValue), Description("Used to sort this def in relation to other instances of this def. Default order is from low to high.")]
		public int order;
		
		public QOLSocietyIndicatorStage getStage(float thresholdPercentile) {
			foreach ( QOLSocietyIndicatorStage stage in stages ) {
				if ( stage.thresholdMin <= thresholdPercentile && stage.thresholdMax >= thresholdPercentile ) {
					return stage;
				}
			}
			return null;
		}
		
		public int CompareTo(QOLSocietyIndicatorDef def) {
			return order.CompareTo(def.order);
		}
		
		public override void PostLoad() {
			base.PostLoad();
			if ( stages == null ) stages = new List<QOLSocietyIndicatorStage>(0);
		}
	}
	
	public class QOLSocietyIndicatorStage {
		[MustTranslate]
		public string label;
		public float thresholdMin;
		public float thresholdMax;

		public void CityContribution(Dictionary<QOLCityNeedDef, NumericAdjustment> needContribution, Dictionary<QOLCityNeedDef, NumericAdjustment> needRequirements) {
			
		}
	}
}

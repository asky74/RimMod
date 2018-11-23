/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 27.08.2017
 * Time: 18:28
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of CuMExtensions.
	/// </summary>
	public static class CuMExtensions
	{
		public static float getCuMWorth(this Backstory bs, bool includeSkillGainWorth = true) {
			if ( bs == null ) return 0;
			float total = 0, BC = QOLMod.getBaseCost();
			
			if ( includeSkillGainWorth ) {
				foreach(KeyValuePair<SkillDef,int> p in bs.skillGainsResolved) {
					total += BC * (float)p.Value;
				}
			}
			WorkTags disabledWorkTags = bs.workDisables;
			List<WorkTypeDef> allDefsListForReading = DefDatabase<WorkTypeDef>.AllDefsListForReading;
			foreach(WorkTypeDef wtd in allDefsListForReading) {
				if ( (wtd.workTags & disabledWorkTags) != WorkTags.None ) total += -2 * BC;
			}
			return total;
		}
		
		public static float getCuMWorth(this Trait t, bool removal = false) {
			if ( t == null ) return 0;
			return QOLMod.getTraitCost(t,removal) * QOLMod.getBaseCost();
		}
		
		public static float getCuMWorth(this SkillRecord sr) {
			if ( sr == null || sr.TotallyDisabled ) return 0;
			return sr.Level * QOLMod.getBaseCost() + sr.passion.getCuMWorth();
		}
		
		public static float getCuMWorth(this Passion p) {
			switch(p) {
				case Passion.None: return 0;
				case Passion.Minor: return QOLMod.getBaseCost() * 2;
				case Passion.Major: return QOLMod.getBaseCost() * 3;
			}
			Log.Error("Unknown Passion type while calculation upgrade cost!");
			return 0;
		}
		
		public static float getCuMWorth(this Pawn_RecordsTracker tracker) {
			if ( tracker == null ) return 0;
			float points = 0;
			foreach ( RecordDef d in DefDatabase<RecordDef>.AllDefsListForReading ) {
				points += tracker.GetValue(d) * QOLMod.getCuMRecordFactor(d);
			}
			return points;
		}
		
		public static float getCuMWorth(this Pawn p) {
			if ( p == null ) return 0;
			float points = 0, BC = QOLMod.getBaseCost();;
			points += getCuMWorth(p.records);
			foreach ( Trait t in p.story.traits.allTraits ) {
				points += t.getCuMWorth();
			}
			foreach ( Backstory bs in p.story.AllBackstories ) {
				points += bs.getCuMWorth(false);
			}
			foreach ( SkillRecord sr in p.skills.skills ) {
				points += sr.getCuMWorth();
			}
			return points;
		}
	}
}

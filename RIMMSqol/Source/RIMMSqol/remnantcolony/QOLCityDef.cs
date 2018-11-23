/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 10.10.2018
 * Time: 14:55
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using Verse;

namespace RIMMSqol.remnantcolony
{
	/// <summary>
	/// Description of QOLCityDef.
	/// </summary>
	public class QOLCityDef : Def {
		public List<QOLCityProjectDef> projects;
		public List<QOLCityNeedDef> needs;
		public List<QOLSocietyIndicatorDef> indicators;
		public List<QOLCityResourceDef> resources;
		public QOLCityPopulationProperties population;
	}
	
	public class QOLCityPopulationProperties {
		public List<QOLCitySocialStandingDef> socialStandings;
		public List<QOLCityPopulationImmigrationLaw> immigrationLaws;
		public List<QOLCityPopulationContribution> contributions;
		public bool IsImmigrationAllowed(QOLCity city, Pawn p) {
			return !immigrationLaws.Any(il=>!il.IsAllowed(city,p));
		}
	}
	
	abstract public class QOLCityPopulationImmigrationLaw {
		abstract public bool IsAllowed(QOLCity city, Pawn p);
	}
	
	public class QOLCityPopulationImmigrationLaw_AtLeastHumanlikeIntelligence : QOLCityPopulationImmigrationLaw {
		override public bool IsAllowed(QOLCity city, Pawn p) {
			return p != null && p.RaceProps != null && p.RaceProps.Humanlike;
		}
	}
	
	public class QOLCityPopulationImmigrationLaw_Animal : QOLCityPopulationImmigrationLaw {
		override public bool IsAllowed(QOLCity city, Pawn p) {
			return p != null && p.RaceProps != null && p.RaceProps.Animal;
		}
	}
	
	abstract public class QOLCityPopulationContribution {
		public List<QOLCityPopulationPawnFilter> pawnFilters;
		protected int RelevantPopulationCount(QOLCity city) {
			int relevantPopulationCount = city.population.citizens.Count;
			if ( !pawnFilters.NullOrEmpty() ) {
				foreach ( Pawn p in city.population.citizens.Keys ) {
					if ( pawnFilters.Any(filter => !filter.IsAllowed(city, p)) ) {
						relevantPopulationCount--;
					}
				}
			}
			return relevantPopulationCount;
		}
		abstract public void CityContribution(QOLCity city, Dictionary<QOLCityNeedDef, NumericAdjustment> needContribution, Dictionary<QOLCityNeedDef, NumericAdjustment> needRequirements, Dictionary<QOLSocietyIndicatorDef, NumericAdjustment> indicatorContribution);
	}
	
	abstract public class QOLCityPopulationPawnFilter {
		abstract public bool IsAllowed(QOLCity city, Pawn p);
	}
	
	public class QOLCityPopulationPawnFilter_Flesh : QOLCityPopulationPawnFilter {
		public override bool IsAllowed(QOLCity city, Pawn p) {
			return p.RaceProps.IsFlesh;
		}
	}
	
	public class QOLCityPopulationPawnFilter_Not : QOLCityPopulationPawnFilter {
		public QOLCityPopulationPawnFilter filter;
		public override bool IsAllowed(QOLCity city, Pawn p) {
			return !filter.IsAllowed(city,p);
		}
	}
	
	public class QOLCityPopulationContribution_Needs : QOLCityPopulationContribution {
		public QOLCityNeedDef need;
		public float multiplierPopulationCount;
		public bool isRequirement;
		
		override public void CityContribution(QOLCity city, Dictionary<QOLCityNeedDef, NumericAdjustment> needContribution, Dictionary<QOLCityNeedDef, NumericAdjustment> needRequirements, Dictionary<QOLSocietyIndicatorDef, NumericAdjustment> indicatorContribution) {
			Dictionary<QOLCityNeedDef, NumericAdjustment> dic;
			if ( isRequirement ) {
				dic = needRequirements;
			} else {
				dic = needContribution;
			}
			if ( dic.ContainsKey(need) ) {
				needRequirements[need].flat += RelevantPopulationCount(city) * multiplierPopulationCount;
			}
		}
	}

	/*
	 * Population should cover only things that happen due to adding/removing pawns or changing a pawns social standing. Therefore all contributions should be based on
	 * those events.
     * Project needs to report next relevant tick.    
 	 */
	public class QOLCityPopulationContribution_Indicator : QOLCityPopulationContribution {
		public QOLSocietyIndicatorDef indicator;
		public float multiplierPopulationCount;
		
		override public void CityContribution(QOLCity city, Dictionary<QOLCityNeedDef, NumericAdjustment> needContribution, Dictionary<QOLCityNeedDef, NumericAdjustment> needRequirements, Dictionary<QOLSocietyIndicatorDef, NumericAdjustment> indicatorContribution) {
			if ( indicatorContribution.ContainsKey(indicator) ) {
				indicatorContribution[indicator].flat += RelevantPopulationCount(city) * multiplierPopulationCount;
			}
		}
	}
}

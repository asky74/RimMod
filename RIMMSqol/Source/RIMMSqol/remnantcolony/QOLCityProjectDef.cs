/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.10.2018
 * Time: 15:55
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
	/// Description of QOLCityProject.
	/// </summary>
	public class QOLCityProjectDef : Def
	{
		[DefaultValue(null), Description("World object def that is used as a template to construct a world object representing a completed project in a city.")]
		public WorldObjectDef worldObject;
		[DefaultValue(null), Description("Defines the modifications to indicators that occur due to the completion of this project.")]
		public List<QOLCityProjectIndicator> indicators;
		[DefaultValue(null), Description("Defines the modifications to needs that occur due to the completion of this project.")]
		public List<QOLCityProjectNeed> needs;
		[DefaultValue(int.MaxValue), Description("Used to sort this def in relation to other instances of this def. Default order is from low to high.")]
		public int order;
		[DefaultValue(0), Description("A positive number above zero indicates that this project last for that number of turns before becoming useless.")]
		public int lifetime;
		[DefaultValue(0), Description("A positive number indicates that this project is started after that number of turns. A negative number means it is started periodically after that number of turns until maxInstances are reached. Zero means it is build manually.")]
		public int autospawnTimer;
		[DefaultValue(0), Description("How much labour is required to finish this project.")]
		public int labour;
		[DefaultValue(1), Description("How often can this project be repeated in one city.")]
		public int maximumInstances;
		[DefaultValue(null), Description("Conditions that must be met before the project can be started.")]
		public List<QOLCityProjectRequirement> requirements;
		
		public override void PostLoad() {
			base.PostLoad();
			if ( indicators == null ) indicators = new List<QOLCityProjectIndicator>(0);
			if ( needs == null ) needs = new List<QOLCityProjectNeed>(0);
			if ( requirements == null ) requirements = new List<QOLCityProjectRequirement>(0);
		}
	}
	
	public class QOLCityProjectIndicator {
		public QOLSocietyIndicatorDef indicator;
		public float flat;
		public float factor;
	}
	
	public class QOLCityProjectNeed {
		public QOLCityNeedDef need;
		public float requirementFlat;
		public float requirementFactor;
		public float contributionFlat;
		public float contributionFactor;
	}
	
	public abstract class QOLCityProjectRequirement {
		//min and max validation for population count, indicators, need requirements, need contributions
		//exist and not exist validation for indicator stages, projects, matching citizen(s), later stories maybee.
        abstract public bool IsValid(QOLCity city);
	}
	
	public class QOLCityProjectRequirement_PopulationCount : QOLCityProjectRequirement {
		[DefaultValue(0), Description("The population count must be at least this high.")]
		public int min;
		[DefaultValue(int.MaxValue), Description("The population count must be at most this high.")]
		public int max;
		
		override public bool IsValid(QOLCity city) {
			return min <= city.population.citizens.Count && city.population.citizens.Count <= max;
		}
	}
}

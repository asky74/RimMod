/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.10.2018
 * Time: 17:37
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RIMMSqol.remnantcolony
{
	/// <summary>
	/// Description of QOLCity.
	/// </summary>
	public class QOLCity : WorldObject, ICommunicable {
		public QOLCityDef defCity;
		
		//save state
		public string cityName = "unnamed";
		public List<QOLCityProject> projects = new List<QOLCityProject>();
		public QOLCityPopulation population = new QOLCityPopulation();
		public int lastSimulatedTick = 0;
		
		//unsaved for visualization etc.
		public List<QOLSocietyIndicatorStage> currentStages;
		public List<QOLCityNeed> currentNeeds;
		public SortedDictionary<QOLSocietyIndicatorDef,float> currentIndicators;
		
		override public void ExposeData() {
			base.ExposeData();
			Scribe_Deep.Look<QOLCityPopulation>(ref population, "population", new object[0]);
			Scribe_Values.Look<string>(ref cityName, "cityName", "unnamed");
			Scribe_Values.Look<int>(ref lastSimulatedTick, "lastSimulatedTick", 0);
			Scribe_Collections.Look<QOLCityProject>(ref projects, "projects");
		}
		
		public bool AddCitizen(Pawn p) {
			if ( defCity.population.IsImmigrationAllowed(this,p) ) {
				//population.citizens.Add(p,defaultSocialStanding);
				return true;
			}
			return false;
		}
		
		public void RemoveCitizen(Pawn p) {
			population.citizens.Remove(p);
		}
		
		public void FinishBuildingProject(QOLCityProject proj) {
			
		}
		
		public QOLCityProject StartBuildingProject(QOLCityProjectDef def) {
			if ( def.requirements.Any(r=>!r.IsValid(this)) ) {
		    	return null;
		    }
			QOLCityProject proj = new QOLCityProject(def);
			if ( def.worldObject != null ) {
				//search a free spot in the surroundings of the city and create a world object according to the def there and link it to the project.
			}
			return proj;
		}
		
		public void RemoveProject(QOLCityProject proj) {
			
		}
		
		public void Refresh() {
			Dictionary<QOLCityNeedDef,NumericAdjustment> needContribution = new Dictionary<QOLCityNeedDef, NumericAdjustment>();
			Dictionary<QOLCityNeedDef,NumericAdjustment> needRequirements = new Dictionary<QOLCityNeedDef, NumericAdjustment>();
			//only the needs and indicators defined for the city are used
			foreach ( QOLCityNeedDef need in defCity.needs ) {
				needContribution.Add(need,new NumericAdjustment());
				needRequirements.Add(need,new NumericAdjustment());
			}
			Dictionary<QOLSocietyIndicatorDef,NumericAdjustment> indicatorContribution = new Dictionary<QOLSocietyIndicatorDef, NumericAdjustment>();
			foreach ( QOLSocietyIndicatorDef indicator in defCity.indicators ) {
				indicatorContribution.Add(indicator,new NumericAdjustment());
			}
			//projects fill these
			foreach ( QOLCityProject project in projects ) {
				project.CityContribution(needContribution, needRequirements, indicatorContribution);
			}
			//population fills these
			//population.CityContribution(needContribution, needRequirements, indicatorContribution);
			
			Dictionary<QOLSocietyIndicatorDef,QOLSocietyIndicatorStage> activeStages = new Dictionary<QOLSocietyIndicatorDef, QOLSocietyIndicatorStage>();
			//based on indicatorContribution, adds to need requirements
			foreach ( KeyValuePair<QOLSocietyIndicatorDef,NumericAdjustment> p in indicatorContribution ) {
				activeStages.Add(p.Key,p.Key.getStage(p.Value.flat*p.Value.factor));
				activeStages[p.Key].CityContribution(needContribution, needRequirements);
			}
			
			//a feedback loop can exist through afflictions that are created if needs aren't met
		}

		public string GetCallLabel() {
			return cityName;
		}
		
		public string GetInfoText() {
			return defCity.description;
		}
		
		public void TryOpenComms(Pawn negotiator) {
			Find.WindowStack.Add(new Dialog_MessageBox("tbd"));
		}
		
		public Faction GetFaction() {
			return Faction;
		}
		
		public FloatMenuOption CommFloatMenuOption(Building_CommsConsole console, Pawn negotiator) {
			return new FloatMenuOption(Label, ()=>console.GiveUseCommsJob(negotiator,this));
		}

		override public void Tick() {
			base.Tick();
			foreach ( QOLCityProject proj in projects ) {
				proj.Tick();
			}
		}
	}
}

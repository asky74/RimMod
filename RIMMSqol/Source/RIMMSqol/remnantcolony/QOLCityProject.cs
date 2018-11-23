/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.10.2018
 * Time: 17:02
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace RIMMSqol.remnantcolony
{
	/// <summary>
	/// Description of QOLCityProject.
	/// </summary>
	public class QOLCityProject : IExposable
	{
		public QOLCityProjectDef def;
		public WorldObject worldObject;
		public int timeAdded, timeBuilt;
		
		public QOLCityProject(QOLCityProjectDef def) {
			this.def = def;
		}

		public void CityContribution(Dictionary<QOLCityNeedDef, NumericAdjustment> needContribution, Dictionary<QOLCityNeedDef, NumericAdjustment> needRequirements, Dictionary<QOLSocietyIndicatorDef, NumericAdjustment> indicatorContribution) {
			
		}
		
		public virtual void Tick() {
			if ( worldObject != null ) worldObject.Tick();
		}
		
		public virtual void ExposeData() {
			Scribe_Defs.Look<QOLCityProjectDef>(ref this.def, "def");
			Scribe_References.Look<WorldObject>(ref this.worldObject, "worldObject");
			Scribe_Values.Look<int>(ref this.timeAdded, "timeAdded");
			Scribe_Values.Look<int>(ref this.timeBuilt, "timeBuilt");
		}
	}
}

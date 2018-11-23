/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 10.10.2018
 * Time: 13:07
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using Verse;

namespace RIMMSqol.remnantcolony
{
	/// <summary>
	/// Description of QOLCityPopulation.
	/// </summary>
	public class QOLCityPopulation : IExposable
	{
		public Dictionary<Pawn,QOLCitySocialStandingDef> citizens;
		protected List<Pawn> tmpCitizensKeys;
		protected List<QOLCitySocialStandingDef> tmpCitizensValues;

		public void ExposeData() {
			Scribe_Collections.Look<Pawn,QOLCitySocialStandingDef>(ref citizens, "citizens", LookMode.Reference, LookMode.Def, ref tmpCitizensKeys, ref tmpCitizensValues);
		}
	}
}

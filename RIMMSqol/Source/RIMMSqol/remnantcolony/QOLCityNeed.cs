/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 09.10.2018
 * Time: 14:12
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using Verse;

namespace RIMMSqol.remnantcolony
{
	/// <summary>
	/// Description of QOLCityNeeds.
	/// </summary>
	public class QOLCityNeed : IExposable
	{
		public QOLCityNeedDef def;
		public float required;
		public float satisfied;
		
		public void ExposeData() {
			Scribe_Defs.Look<QOLCityNeedDef>(ref def, "def");
			Scribe_Values.Look<float>(ref required, "required");
			Scribe_Values.Look<float>(ref satisfied, "satisfied");
		}
	}
}

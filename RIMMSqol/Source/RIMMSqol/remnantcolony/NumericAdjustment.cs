/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 10.10.2018
 * Time: 05:13
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using Verse;

namespace RIMMSqol.remnantcolony
{
	/// <summary>
	/// Description of NumericAdjustment.
	/// </summary>
	public class NumericAdjustment : IExposable
	{
		public float flat;
		public float factor;

		public void ExposeData()
		{
			Scribe_Values.Look<float>(ref flat,"flat");
			Scribe_Values.Look<float>(ref factor,"factor",1.0f);
		}
	}
}

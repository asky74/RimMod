/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 31.05.2017
 * Time: 15:42
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using RIMMSqol.genericSettings;
using Verse;
using UnityEngine;

namespace RIMMSqol
{
	/// <summary>
	/// Description of QOLModSettings.
	/// </summary>
	public class QOLModSettings : ModSettings
	{
		public bool stopSkillDecay = false, useFixedNumericTextfields = false, preventAnimalFamilies = false, stopTamenessDecay = false;
		public float cumBaseCost = 20f, cumPointPawnToPoolConversionFactor = 0f, cumPointRemnantsToPoolConversionFactor = 0.25f, remnantOrderPriceFactor = 0.5f;
		public List<string> cumTraitCosts;
		public List<string> cumRecordsFactor;
		public List<string> pfRestrictionExcemptions = null;
		public string pfColonist = null, pfAnimalTame = null, pfAnimalWild = null, pfOther = null;
		public string pfColonistConf = null, pfAnimalTameConf = null, pfAnimalWildConf = null, pfOtherConf = null;
		public List<string> forbiddenPatchNamespaces = null;
		
		public override void ExposeData()
		{
			base.ExposeData();
			SettingsInit.init();
			SettingsStorage.ExposeData();
		}
	}
}

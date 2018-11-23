/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.06.2018
 * Time: 15:18
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using RimWorld;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of MainButtonWorker_TabMenu.
	/// </summary>
	public class MainButtonWorker_TabMenu : MainButtonWorker
	{
		public bool resetViewNextTime = true;

		public override void Activate()
		{
			if ( !Find.WindowStack.IsOpen<Dialog_ModSettings>() ) {
				Find.WindowStack.Add(new Dialog_ModSettings());
				Verse.Sound.SoundStarter.PlayOneShotOnCamera(SoundDefOf.DialogBoxAppear,null);
			}
		}
	}
}

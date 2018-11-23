/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 19.10.2018
 * Time: 23:44
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace RIMMSqol.remnantcolony
{
	/// <summary>
	/// Description of QOLCityResourceDef.
	/// </summary>
	public class QOLCityResourceDef : Def {
		[NoTranslate]
		private string icon;
		private Texture2D iconInt;
		public Texture2D Icon {
			get {
				if (this.iconInt == null) {
					if (this.icon == null) {
						return null;
					}
					this.iconInt = ContentFinder<Texture2D>.Get(this.icon, true);
				}
				return this.iconInt;
			}
		}
		
		[DefaultValue(int.MaxValue), Description("Used to sort this def in relation to other instances of this def. Default order is from low to high.")]
		public int order;
	}
}

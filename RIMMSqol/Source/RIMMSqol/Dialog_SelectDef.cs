/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 26.08.2017
 * Time: 23:16
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using RIMMSqol.genericSettings;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of Dialog_SelectDef.
	/// </summary>
	public class Dialog_SelectDef<T> : Dialog_Select<T> where T : Def, new()
	{
		/*protected Func<T,bool> onSelect;
		protected IEnumerable<T> items;*/
		
		public Dialog_SelectDef(Func<T,bool> onSelect) : this(onSelect,DefDatabase<T>.AllDefsListForReading)
		{
		}

		public Dialog_SelectDef(Func<T,bool> onSelect, IEnumerable<T> items) : base(onSelect,items, d=>SettingsFieldPropertiesSelectable.DefLabelProducer(d))
		{
			/*this.onSelect = onSelect;
			this.items = items;*/
		}
		
		/*protected override void DoListingItems() {
			foreach ( T d in items ) {
				if ( FilterAllows(d.label) ) {
					if (this.listing.ButtonText(d.label, null))
					{
						if ( onSelect(d) ) {
							Find.WindowStack.TryRemove(this, true);
						}
					}
				}
			}
		}*/
	}
}

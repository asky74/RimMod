/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 30.08.2017
 * Time: 00:18
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of Dialog_SelectEnum.
	/// </summary>
	public class Dialog_SelectEnum<T> : Dialog_OptionLister
	{
		protected Func<T,bool> onSelect;
		protected T[] items;
		
		public Dialog_SelectEnum(Func<T,bool> onSelect) : this(onSelect, null) {}
		
		public Dialog_SelectEnum(Func<T,bool> onSelect, Predicate<T> restrictItems)
		{
			this.onSelect = onSelect;
			this.items = (T[])Enum.GetValues(typeof(T));
			if ( restrictItems != null ) this.items = Array.FindAll<T>(this.items,restrictItems);
		}
		
		protected override void DoListingItems() {
			foreach ( T d in items ) {
				string label = Enum.GetName(typeof(T),d);
				label.TryTranslate(out label);
				if ( FilterAllows(label) ) {
					if (this.listing.ButtonText(label, null))
					{
						if ( onSelect(d) ) {
							Find.WindowStack.TryRemove(this, true);
						}
					}
				}
			}
		}
	}
}

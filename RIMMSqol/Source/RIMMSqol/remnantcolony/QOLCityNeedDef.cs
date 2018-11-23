/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 09.10.2018
 * Time: 14:11
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
	public class QOLCityNeedDef : Def, IComparable<QOLCityNeedDef>
	{
		[DefaultValue(int.MaxValue), Description("Used to sort this def in relation to other instances of this def. Default order is from low to high.")]
		public int order;
		
		public int CompareTo(QOLCityNeedDef def) {
			return order.CompareTo(def.order);
		}
	}
}

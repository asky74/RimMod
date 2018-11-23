/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 30.08.2017
 * Time: 00:12
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of Dialog_Select.
	/// </summary>
	public class Dialog_Select<T> : Dialog_OptionLister
	{
		protected Func<T,bool> onSelect;
		protected IOrderedEnumerable<T> items;
		protected Func<T,Texture2D> iconResolver;
		protected Func<T,string> labelResolver;
		protected IDialog_Filter<T> filterModel;
		protected Func<T,string> tooltipProducer;
		protected float totalOptionsWidth;
		public int numOfColumns = 4;
		
		public override Vector2 InitialSize {
			get {
				return new Vector2((float)Math.Min(UI.screenWidth,1024), (float)Math.Min(UI.screenHeight,768));
			}
		}
		
		public Dialog_Select(Func<T,bool> onSelect, IEnumerable<T> items, Func<T,string> labelResolver, Func<T,Texture2D> iconResolver, IDialog_Filter<T> filter = null, Func<T,string> tooltipProducer = null) : this(onSelect,items,filter,tooltipProducer,labelResolver,iconResolver)
		{}
		
		public Dialog_Select(Func<T,bool> onSelect, IEnumerable<T> items, Func<T,string> labelResolver, IDialog_Filter<T> filter = null, Func<T,string> tooltipProducer = null) : this(onSelect,items,filter,tooltipProducer,labelResolver,null)
		{}
		
		protected Dialog_Select(Func<T,bool> onSelect, IEnumerable<T> items, IDialog_Filter<T> filter, Func<T,string> tooltipProducer, Func<T,string> labelResolver, Func<T,Texture2D> iconResolver)
		{
			this.onSelect = onSelect;
			if ( items is IOrderedEnumerable<T> ) this.items = (IOrderedEnumerable<T>)items;
			else this.items = items.OrderBy(d=>0);
			this.labelResolver = labelResolver;
			this.iconResolver = iconResolver;
			this.filterModel = filter == null ? new Dialog_FilterLabel<T>() : filter;
			this.tooltipProducer = tooltipProducer;
			totalOptionsWidth = -1;
		}
		
		public override void DoWindowContents(UnityEngine.Rect inRect)
		{
			Rect outRect = filterModel.DoFilterWindowContent(inRect);
			
			items = filterModel.Sort(items);
			Rect rect = new Rect(0f, 0f, totalOptionsWidth > 0 ? totalOptionsWidth : outRect.width - 16f, outRect.height - QOLMod.HorizontalScrollbarHeight());
			Widgets.BeginScrollView(outRect, ref this.scrollPosition, rect, true);
			this.listing = new Listing_Standard();
			this.listing.ColumnWidth = (outRect.width - 16f - 51f) / ((float)numOfColumns);
			this.listing.Begin(rect);
			this.DoListingItems();
			//we determine the correct width after rendering, basically dragging one frame behind in favor of rendering a layout frame.
			Rect rectFinal = this.listing.GetRect(0);
			this.listing.End();
			totalOptionsWidth = rectFinal.x + rectFinal.width + Listing.ColumnSpacing;
			Widgets.EndScrollView();
		}
		
		protected override void DoListingItems() {
			foreach ( T d in items ) {
				try {
				string label = labelResolver(d);
				if ( FilterAllows(label,d) ) {
					Rect r = this.listing.GetRect(0);
					bool wasCapped;
					string labelToRender = label;
					string labelCapped;
					if ( iconResolver != null ) {
						labelCapped = QOLMod.CapString(labelToRender,r.width-32f,"...",out wasCapped,Text.Font);
						if (this.listing.ButtonImageText(labelCapped, iconResolver(d), 32f, 32f)) {
							if ( onSelect(d) ) {
								Find.WindowStack.TryRemove(this, true);
							}
						}
					} else {
						labelCapped = QOLMod.CapString(labelToRender,r.width,"...",out wasCapped,Text.Font);
						if (this.listing.ButtonText(labelCapped, null))
						{
							if ( onSelect(d) ) {
								Find.WindowStack.TryRemove(this, true);
							}
						}
					}
					Rect rShifted = this.listing.GetRect(0);
					if (Math.Abs(rShifted.x - r.x) > 0.0001f) {
						//new column so we need to shift the rect from bottom to next columns top position.
						r.x = rShifted.x;
						r.y = 0;
						r.height = rShifted.y;
					} else r.height = rShifted.y - r.y;
					
					if ( (tooltipProducer != null || wasCapped) && Mouse.IsOver(r) ) {
						if ( tooltipProducer == null ) TooltipHandler.TipRegion(r, () => labelToRender, 1);
						else {
							string tooltip = tooltipProducer(d);
							if (wasCapped) {
								if ( !tooltip.NullOrEmpty() ) 
									TooltipHandler.TipRegion(r, () => labelToRender+" | "+tooltip, 1);
								else TooltipHandler.TipRegion(r, () => labelToRender, 1);
							} else if ( !tooltip.NullOrEmpty() ) TooltipHandler.TipRegion(r, () => tooltip, 1);
						}
					}
				}
				} catch (Exception ex) {
					Log.Message("Exception while rendering item: "+d+" "+ex);
				}
			}
		}
		
		protected bool FilterAllows(string label, T value)
		{
			return filterModel.FilterAllows(label,value);
		}
	}
	
	public interface IDialog_Filter<T> {
		Rect DoFilterWindowContent(Rect rect);
		bool FilterAllows(string label, T value);
		IOrderedEnumerable<T> Sort(IOrderedEnumerable<T> items);
	}
	
	public class Dialog_FilterLabel<T> : IDialog_Filter<T> {
		protected string filter;
		#region IDialog_Filter implementation
		public Rect DoFilterWindowContent(Rect rect)
		{
			this.filter = Widgets.TextField(new Rect(0f, 0f, 200f, 30f), this.filter);
			Rect r = new Rect(rect); r.yMin += 35f;
			return r;
		}
		public bool FilterAllows(string label, T value)
		{
			return this.filter.NullOrEmpty() || label.NullOrEmpty() || label.IndexOf(this.filter, StringComparison.OrdinalIgnoreCase) >= 0;
		}
		public IOrderedEnumerable<T> Sort(IOrderedEnumerable<T> items) {
			return items;
		}
		#endregion
	}
}

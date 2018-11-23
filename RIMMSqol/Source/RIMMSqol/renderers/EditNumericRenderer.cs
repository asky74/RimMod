/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.07.2018
 * Time: 02:09
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Description of EditNumericRenderer.
	/// </summary>
	public class EditNumericRenderer<T> : BaseRenderer where T : struct {
		protected Func<Flow,T> valueExpr;
		protected Action<Flow,T> onChange;
		protected GameFont font;
		protected float width, min, max;
		protected string buffer;
		protected T lastValue;
		protected Dictionary<int,string> buffers;
		protected Dictionary<int,T> lastValues;
		public override float getHeight() {
			return QOLMod.LineHeight(font);
		}
		public override float getWidth() {
			if ( width >= 0 ) return width;
			return 40f;
		}
		public EditNumericRenderer(Func<Flow,T> valueExpr, Action<Flow,T> onChange, float width = 40f, float min = float.MinValue, float max = float.MaxValue) {
			this.valueExpr = valueExpr;
			this.onChange = onChange;
			this.width = width;
			this.min = min;
			this.max = max;
		}
		public override void DoComponentContents(Rect inRect, Flow flow) {
			T value = valueExpr(flow);
			
			object itit;
			if ( flow.pageScope.TryGetValue("curItem",out itit) ) {
				//If we are in an iteration the buffer must be tied to the iteration item otherwise all textfields in the list will display the same buffer
				if ( buffers == null ) buffers = new Dictionary<int, string>();
				buffers.TryGetValue(((IterationItem)itit).index, out buffer);
				if ( lastValues == null ) lastValues = new Dictionary<int, T>();
				lastValues.TryGetValue(((IterationItem)itit).index, out lastValue);
			} else itit = null;
			
			displayToolTipIfNecessary(inRect, flow);
			T originalValue = value;
			//reset buffer if the value was changed through an external action
			if ( !lastValue.Equals(value) ) {
				buffer = value.ToString();
			}
			//Widgets.TextFieldNumeric<T>(inRect, ref value, ref buffer, min, max);
			WidgetsFix.TextFieldNumeric<T>(inRect, ref value, ref buffer, min, max);
			if ( !originalValue.Equals(value) ) {
				//Log.Message(originalValue+" value: "+value+" buffer: "+buffer+" min: "+min+" max: "+max+" index: "+(itit!=null?((IterationItem)itit).index:0));
				onChange(flow,value);
			}
			lastValue = value;
			
			if ( itit != null ) {
				buffers[((IterationItem)itit).index] = buffer;
				lastValues[((IterationItem)itit).index] = lastValue;
			}
		}
		public override void PostNavigation() {
			buffer = null;
			base.PostNavigation();
		}
	}
}

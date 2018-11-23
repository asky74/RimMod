/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.07.2018
 * Time: 02:08
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace RIMMSqol.renderers
{
	/// <summary>
	/// Description of EditTextRenderer.
	/// </summary>
	public class EditTextRenderer : BaseRenderer {
		protected Func<Flow,string> valueExpr;
		protected Action<Flow,string> onChange;
		protected GameFont font;
		protected float width; 
		protected int maxNumOfChars;
		protected Regex inputValidator;
		public override float getHeight() {
			return QOLMod.LineHeight(font);
		}
		public override float getWidth() {
			if ( width >= 0 ) return width;
			return 40f;
		}
		public EditTextRenderer(Func<Flow,string> valueExpr, Action<Flow,string> onChange, float width = 40f, int maxNumOfChars = int.MaxValue, Regex inputValidator = null) {
			if ( inputValidator == null ) inputValidator = new Regex(@".*");
			this.valueExpr = valueExpr;
			this.onChange = onChange;
			this.width = width;
			this.maxNumOfChars = maxNumOfChars;
			this.inputValidator = inputValidator;
		}
		public override void DoComponentContents(Rect inRect, Flow flow) {
			string value = valueExpr(flow);
			
			displayToolTipIfNecessary(inRect, flow);
			string originalValue = value;

			value = Widgets.TextField(inRect, value, maxNumOfChars, inputValidator);
			
			if ( ( originalValue == null && value != null ) || ( originalValue != null && !originalValue.Equals(value) ) ) {
				onChange(flow,value);
			}
		}
	}
}

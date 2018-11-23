/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.07.2018
 * Time: 02:13
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
	/// Description of PageRenderer.
	/// </summary>
	public class PageRenderer {
		public Func<Flow,String> title;
		public Func<String> onClose, onBack; //return the name of the new page
		public Action<Flow> onPostNavigationHandler; //if a navigation takes place this is called by the navigating flow.
		public List<BaseRenderer> components;
		public PageRenderer(Func<Flow,String> title, Func<String> onClose, Func<string> onBack, Action<Flow> onPostNavigationHandler = null) {
			this.title = title;
			this.onClose = onClose;
			this.onBack = onBack;
			this.components = new List<BaseRenderer>();
			this.onPostNavigationHandler = onPostNavigationHandler;
		}
		public void DoPageContents(Rect inRect, Flow flow) {
			if ( onClose != null && Widgets.CloseButtonFor(inRect) ) {
				flow.navigate(onClose.Invoke());
			}
			if ( onBack != null && Widgets.ButtonImage(new Rect(inRect.x + 4f, inRect.y + 4f, 18f, 18f), Materials.backButtonSmall, Color.white, Widgets.MouseoverOptionColor) ) {
				flow.navigate(onBack.Invoke());
			}
			Text.Font = GameFont.Medium;
			TextAnchor anchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleCenter;
			float titleBarHeight = Math.Max(Text.LineHeight+8f,26f);
			Widgets.Label(new Rect(inRect.x + 26f, inRect.y + 4f, inRect.width - 52f, titleBarHeight-4f),title(flow));
			inRect = new Rect(inRect.x, inRect.y + titleBarHeight, inRect.width, inRect.height - titleBarHeight);
			
			Rect contentRect = inRect.ContractedBy(4f);
			foreach ( BaseRenderer cr in components ) {
				cr.DoComponentContents(inRect, flow);
			}
			Text.Anchor = anchor;
		}
		public void PostNavigation() {
			foreach ( BaseRenderer cr in components ) {
				cr.PostNavigation();
			}
		}
		public PageRenderer AddChild(BaseRenderer child) {
			if ( child != null ) components.Add(child);
			return this;
		}
	}
}

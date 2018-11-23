/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.07.2018
 * Time: 02:14
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
	/// Description of Flow.
	/// </summary>
	public class Flow {
		private PageRenderer currentPage, nextPage;
		private readonly Dictionary<string,PageRenderer> pages; //all pages plus their names
		public readonly Dictionary<string,object> pageScope;
		public readonly Dictionary<string,object> flowScope;
		protected List<Action<Flow>> postRenderCallbacks = new List<Action<Flow>>();
		public void addPostRenderCallback(Action<Flow> callback) {
			postRenderCallbacks.Add(callback);
		}
		public Flow(Dictionary<string,PageRenderer> pages, string startPage) {
			this.pages = new Dictionary<string, PageRenderer>(pages);
			flowScope = new Dictionary<string, object>();
			pageScope = new Dictionary<string, object>();
			navigate(startPage);
		}
		
		public void navigate(string pageName) {
			//Queuing up navigation to take place between renderings so that the pageScope is clean on each pass.
			//Simply set a nextPage variable if the page was found and then actually switch pages during a defined part of the rendering step.
			if ( pageName == null ) return;
			PageRenderer renderer;
			if ( pages.TryGetValue(pageName, out renderer) ) {
				nextPage = renderer;
			} else {
				Log.Error("unknown page name \""+pageName+"\" in flow navigation!");
			}
		}
		
		public void DoFlowContents(Rect inRect) {
			if ( nextPage != null ) {
				GUIUtility.keyboardControl = 0;
				if ( pageScope != null ) pageScope.Clear();
				if ( currentPage != null && currentPage.onPostNavigationHandler != null ) currentPage.onPostNavigationHandler(this);
				nextPage.PostNavigation();
				currentPage = nextPage;
				nextPage = null;
			}
			if ( currentPage != null ) {
				currentPage.DoPageContents(inRect, this);
			} else {
				Log.Error("Flow has no page to display!");
			}
			if ( !postRenderCallbacks.NullOrEmpty() ) {
				foreach(Action<Flow> a in postRenderCallbacks ) {
					a(this);
				}
				postRenderCallbacks.Clear();
			}
		}
		
		public void destroy() {
			currentPage = nextPage = null;
			if ( pages != null ) pages.Clear();
			if ( pageScope != null ) pageScope.Clear();
			if ( flowScope != null ) flowScope.Clear();
			if ( postRenderCallbacks != null ) postRenderCallbacks.Clear(); postRenderCallbacks = null;
		}
	}
}

/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 17.11.2017
 * Time: 03:55
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using System.Collections.Generic;
using RIMMSqol.renderers;

namespace RIMMSqol.genericSettings
{
	public interface ISettingsVisualization {
		bool displayInMenu{get;}
		bool displaySelection{get;}
	}
	
	public interface ISettingsVisualizationMenu {
		string mainMenuButtonLabel{get;}
	}
	
	public interface ISettingsVisualizationSelection {
		string selectPageTitle{get;}
		Func<List<object>> getBaseObjectsForSelection{get;}
		Func<object,string> getKeyFromBaseObject{get;}
		Func<object,string> getLabelFromBaseObject{get;}
		Func<string,object> getBaseObjectFromKey{get;}
	}
	
	public interface ISettingsVisualizationSingletonSelection {
		Func<object> getBaseObject{get;}
	}
	
	public interface ISettingsVisualizationEdit {
		Func<Flow,string> getEditPageTitle{get;}
	}
	
	public class SettingsVisualizationMenuSelectEdit<T> : ISettingsVisualization, ISettingsVisualizationMenu, ISettingsVisualizationSelection, ISettingsVisualizationEdit {
		public bool displayInMenu {get { return true; }}
		public bool displaySelection {get { return true; }}
		public string mainMenuButtonLabel {get; protected set;}
		public string selectPageTitle {get; protected set;}
		public Func<List<object>> getBaseObjectsForSelection {get; protected set;}
		public Func<object, string> getKeyFromBaseObject {get; protected set;}
		public Func<object, string> getLabelFromBaseObject {get; protected set;}
		public Func<string, object> getBaseObjectFromKey {get; protected set;}
		public Func<Flow, string> getEditPageTitle {get; protected set;}
		public SettingsVisualizationMenuSelectEdit(string mainMenuButtonLabel, string selectPageTitle, Func<List<T>> getBaseObjectsForSelection, Func<T, string> getKeyFromBaseObject, 
		                                           Func<T, string> getLabelFromBaseObject, Func<string, T> getBaseObjectFromKey, Func<Flow, string> getEditPageTitle) {
			this.mainMenuButtonLabel = mainMenuButtonLabel;
			this.selectPageTitle = selectPageTitle;
			this.getBaseObjectsForSelection = ()=>getBaseObjectsForSelection().Cast<object>().ToList();
			this.getKeyFromBaseObject = o=>getKeyFromBaseObject((T)o);
			this.getLabelFromBaseObject = o=>getLabelFromBaseObject((T)o);
			this.getBaseObjectFromKey = s=>(object)getBaseObjectFromKey(s);
			this.getEditPageTitle = getEditPageTitle;
		}
	}
	
	public class SettingsVisualizationMenuEdit<T> : ISettingsVisualization, ISettingsVisualizationMenu, ISettingsVisualizationSingletonSelection, ISettingsVisualizationEdit {
		public bool displayInMenu {get { return true; }}
		public bool displaySelection {get { return false; }}
		public string mainMenuButtonLabel {get; protected set;}
		public Func<object> getBaseObject {get; protected set;}
		public Func<Flow, string> getEditPageTitle {get; protected set;}
		public SettingsVisualizationMenuEdit(string mainMenuButtonLabel, Func<T> getBaseObject, string getEditPageTitle) {
			this.mainMenuButtonLabel = mainMenuButtonLabel;
			this.getBaseObject = ()=>(object)getBaseObject();
			this.getEditPageTitle = f=>getEditPageTitle;
		}
	}
	
	public class SettingsVisualizationEdit : ISettingsVisualization, ISettingsVisualizationEdit {
		public bool displayInMenu {get { return false; }}
		public bool displaySelection {get { return false; }}
		public Func<Flow, string> getEditPageTitle {get; protected set;}
		public SettingsVisualizationEdit(Func<Flow, string> getEditPageTitle) {
			this.getEditPageTitle = getEditPageTitle;
		}
	}
}

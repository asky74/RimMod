/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 30.10.2017
 * Time: 06:17
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;

namespace RIMMSqol.genericSettings
{
	/// <summary>
	/// Description of SettingsProperties.
	/// </summary>
	public class SettingsProperties {
		public int order = 0;
		public string id;
		public Dictionary<string,ISettingsInstance> overriddenSettings = new Dictionary<string,ISettingsInstance>();
		public List<ISettingsFieldProperties> fields = new List<ISettingsFieldProperties>();
		public Func<ISettingsInstance,string> dynamicLabel;
		public List<Action<ISettingsInstance>> mergers = new List<Action<ISettingsInstance>>();
		public List<Func<ISettingsInstance,bool>> validators = new List<Func<ISettingsInstance,bool>>();
		
		public ISettingsVisualization visualization;

		public SettingsProperties(string id, int order)
		{
			this.id = id;
			this.order = order;
		}
	}
}

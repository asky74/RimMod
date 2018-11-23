/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 31.10.2017
 * Time: 00:12
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RIMMSqol.genericSettings
{
	/// <summary>
	/// Description of SettingsStorage.
	/// </summary>
	public static class SettingsStorage
	{
		private static Dictionary<string,SettingsProperties> settingsProperties = new Dictionary<string, SettingsProperties>();
		private static Dictionary<string,List<SettingsInstance>> settingsInstances = new Dictionary<string, List<SettingsInstance>>();
		
		public static SettingsProperties getSettingsProperties(string id) {
			SettingsProperties settings;
			if ( settingsProperties.TryGetValue(id,out settings) ) return settings;
			return null;
		}
		
		public static SettingsProperties getOrInitSettingsProperties(string id) {
			SettingsProperties settings;
			if ( !settingsProperties.TryGetValue(id,out settings) ) {
				settings = new SettingsProperties(id,settingsProperties.Count);
				settingsProperties[id] = settings;
			}
			return settings;
		}
		
		public static List<SettingsProperties> getAllSettingsProperties() {
			return settingsProperties.Values.OrderBy(p=>p.order).ToList();
		}
		
		public static void ExposeData() {
			foreach ( SettingsProperties props in settingsProperties.Values ) {
				if ( !props.mergers.NullOrEmpty() ) {
					List<SettingsInstance> lst;
					if ( !settingsInstances.TryGetValue(props.id, out lst) ) {
						lst = null;
					}
					if (Scribe.mode == LoadSaveMode.Saving) {
						lst.RemoveAll(si=>!si.getActive());
					}
					
					try {
						Scribe_Collections.Look<SettingsInstance>(ref lst,props.id);
					} catch (Exception ex) {
						Log.Error(ex.Message+" : Failed to load RimMsQol data for "+props.id+" entries. Using defaults.");
					}
					
					if (Scribe.mode == LoadSaveMode.PostLoadInit) { 
						if ( lst == null ) lst = new List<SettingsInstance>(); 
					}
					settingsInstances[props.id] = lst;
				}
			}
		}
		
		public static void initializeSettingsInstances() {
			foreach ( SettingsProperties props in settingsProperties.Values ) {
				if ( !props.mergers.NullOrEmpty() ) {
					if ( !settingsInstances.ContainsKey(props.id) ) {
						settingsInstances[props.id] = new List<SettingsInstance>();
					}
				}
			}
		}
		
		public static void ApplySettings() {
			foreach ( SettingsProperties props in settingsProperties.Values ) {
				if ( !props.mergers.NullOrEmpty() ) {
					List<SettingsInstance> lstOriginal;
					if ( !settingsInstances.TryGetValue(props.id,out lstOriginal) ) {
						lstOriginal = new List<SettingsInstance>();
					}
					Dictionary<string,ISettingsInstance> overriddenSettings = props.overriddenSettings;
					
					List<SettingsInstance> lst = new List<SettingsInstance>(lstOriginal);
					//all overridden settings must be present so we find the ones that we have to reset, not the case if the list got purged from non active settings.
					foreach ( ISettingsInstance setting in overriddenSettings.Values ) {
						if ( lst.Find(s=>s.getKey().Equals(setting.getKey())) == null ) {
							lst.Add((SettingsInstance)setting);
						}
					}
					
					foreach ( SettingsInstance setting in lst ) {
						if ( setting.getActive() && setting.getDirty() ) {
							if ( !overriddenSettings.ContainsKey(setting.getKey()) ) {
								//before we apply a new value for a def for the first time we save the original data
								try {
									if ( props.visualization.displaySelection ) {
										overriddenSettings.Add(setting.getKey(),new SettingsInstance(props.id, ((ISettingsVisualizationSelection)props.visualization).getBaseObjectFromKey(setting.getKey())));
									} else {
										overriddenSettings.Add(setting.getKey(),new SettingsInstance(props.id, ((ISettingsVisualizationSingletonSelection)props.visualization).getBaseObject()));
									}
								} catch {
									Log.Warning("RIMMSQoL setting \""+props.id+"\".\""+setting.getKey()+"\" failed to find overriden object. Deactivating entry.");
									setting.setActive(false);
									lstOriginal.Remove(setting);
									continue;
								}
							}
						} else if ( !setting.getActive() && overriddenSettings.ContainsKey(setting.getKey()) ) {
							//no longer active but previously overridden
							setting.adopt(overriddenSettings[setting.getKey()]);
							setting.setDirty(true);
							overriddenSettings.Remove(setting.getKey());
						} else {
							//not an active setting that needs to be merged, nor a reset setting
							continue;
						}
						
						bool isValid = true;
						foreach ( Func<ISettingsInstance, bool> validator in props.validators ) {
							if ( validator != null ) isValid = isValid && validator(setting);
						} 
						if ( isValid ) {
							foreach ( Action<ISettingsInstance> merger in props.mergers ) {
								merger(setting);
							}
						} else {
							setting.setActive(false);
							lstOriginal.Remove(setting);
							Log.Message("RIMMSQoL setting \""+props.id+"\".\""+setting.getKey()+"\" was marked as invalid. Deactivating entry.");
						}
						setting.setDirty(false);
					}
				}
			}
		}
		
		public static List<ISettingsInstance> generateListForSelection(SettingsProperties props) {
			if ( props.visualization.displaySelection ) {
				List<SettingsInstance> lst;
				if ( settingsInstances.TryGetValue(props.id,out lst) ) {
					ISettingsVisualizationSelection visualProps = ((ISettingsVisualizationSelection)props.visualization);
					List<object> allBaseObjects = visualProps.getBaseObjectsForSelection();
					foreach ( object baseObject in allBaseObjects.Where(o=>lst.Find(si=>si.getKey().Equals(visualProps.getKeyFromBaseObject(o)))==null) ) {
						string key;
						try {
							key = visualProps.getKeyFromBaseObject(baseObject);
						} catch {
							Log.Warning("Failed to load key from database object \""+baseObject+"\".");
							continue;
						}
						if ( !lst.Any(si=>si.getKey().Equals(key)) ) {
							try {
								lst.Add(new SettingsInstance(props.id,baseObject));
							} catch {
								Log.Warning("Failed to load database object for key \""+key+"\" into setting \""+props.id+"\".");
							}
						}
					}
					//lst.AddRange(allBaseObjects.Where(o=>lst.Find(si=>si.getKey().Equals(visualProps.getKeyFromBaseObject(o)))==null).Select(o=>new SettingsInstance(props.id,o)));
					
					lst = lst.OrderByDescending(si=>si.getActive()).ThenBy(si=>si.getLabel()).ToList();
					return lst.Cast<ISettingsInstance>().ToList();
				}
			}
			return null;
		}
		
		public static ISettingsInstance generateSingletonObject(SettingsProperties props) {
			ISettingsVisualizationSingletonSelection visuals = props.visualization as ISettingsVisualizationSingletonSelection;
			if ( visuals != null ) {
				List<SettingsInstance> lst;
				if ( settingsInstances.TryGetValue(props.id,out lst) ) {
					//at some point it was possible to have multiple copies in the list this just corrects that state.
					while ( lst.Count > 1 ) { lst.RemoveAt(0); }
					if ( lst.Any() ) {
						return lst.Last();
					} else {
						try {
							SettingsInstance instance = new SettingsInstance(props.id,visuals.getBaseObject());
							lst.Add(instance);
							return instance;
						} catch {
							Log.Warning("Failed to load singleton object \""+visuals.getBaseObject()+"\" into setting \""+props.id+"\"");
						}
					}
				}
			}
			return null;
		}
	}
}

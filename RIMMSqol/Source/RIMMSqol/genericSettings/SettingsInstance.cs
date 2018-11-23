/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 30.10.2017
 * Time: 06:19
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace RIMMSqol.genericSettings
{
	/// <summary>
	/// Description of SettingsInstance.
	/// </summary>
	public interface ISettingsInstance
	{
		string getId();
		string getKey();
		bool getActive();
		void setActive(bool newValue);
		bool getDirty();
		void setDirty(bool newValue);
		string getLabel();
		V get<V>(string fieldId);
		void set<V>(string fieldId, V value);
		void markAsChanged();
		void reset();
		void attach(ISettingsInstance parentObject);
		ISettingsInstance getParentObject();
	}
	
	public class SettingsInstance : ISettingsInstance, IExposable {
		protected bool active, dirty;
		protected Dictionary<string,ISettingsField> _fields;
		protected ISettingsInstance parentObject;
		protected string id, baseObjectKey, label;
		protected Func<ISettingsInstance,string> dynamicLabel;
		public bool getActive() { if ( parentObject == null ) return active; else return parentObject.getActive(); }
		public void setActive(bool newValue) { if ( parentObject == null ) active = newValue; else parentObject.setActive(newValue); }
		public bool getDirty() { if ( parentObject == null ) return dirty; else return parentObject.getDirty(); }
		public void setDirty(bool newValue) { if ( parentObject == null ) dirty = newValue; else parentObject.setDirty(newValue); }
		public string getId() { return id; }
		public string getKey() { return baseObjectKey; }
		public string getLabel() { return label; }
		public void attach(ISettingsInstance parentObject) { this.parentObject = parentObject; }
		public ISettingsInstance getParentObject() { return parentObject; }
		
		public SettingsInstance() {}
		
		public SettingsInstance(string id, object obj) : this(id,obj,null,null) {
			SettingsProperties properties = SettingsStorage.getSettingsProperties(getId());
			ISettingsVisualizationSelection selection = properties.visualization as ISettingsVisualizationSelection;
			if ( selection != null ) {
				this.baseObjectKey = selection.getKeyFromBaseObject(obj);
				this.label = selection.getLabelFromBaseObject(obj);
			} else {
				this.baseObjectKey = "singleton";
				this.label = "singleton";
			}
		}
		
		public SettingsInstance(string id, object obj, string baseObjectKey, string label) {
			this.id = id;
			this.baseObjectKey = baseObjectKey;
			this.label = label;
			this.active = false;
			this.dirty = false;
			SettingsProperties properties = SettingsStorage.getSettingsProperties(getId());
			foreach ( SettingsFieldProperties p in properties.fields ) {
				fields[p.id].set(p.getFromBaseObject(obj));
				attachFieldIfPossible(p.id);
			}
			updateDynamicLabel();
		}
		
		protected void updateDynamicLabel() {
			SettingsProperties properties = SettingsStorage.getSettingsProperties(getId());
			if ( properties.dynamicLabel != null ) label = properties.dynamicLabel(this);
		}
		
		protected Dictionary<string,ISettingsField> fields {
			get { 
				if ( _fields == null || _fields.Count == 0 ) {
					SettingsProperties settings = SettingsStorage.getSettingsProperties(getId());
					_fields = new Dictionary<string, ISettingsField>();
					foreach ( SettingsFieldProperties p in settings.fields ) {
						ISettingsField value;
						if ( p.type == typeof(int) | p.type == typeof(float) | p.type == typeof(string) | p.type == typeof(bool) | p.type == typeof(UnityEngine.Color) ) {
							value = new PrimitiveSettingsField();
						} else if ( p.type.IsSubclassOf(typeof(Def)) ) {
							value = new DefSettingsField();
						} else if ( typeof(IList).IsAssignableFrom(p.type) ) {
							value = new ListSettingsField();
						} else {
							value = null;
							Log.Error("unknown type for settings: "+p.type);
						}
						if ( value != null ) {
							value.set(p.defaultValue);
							_fields.Add(p.id,value);
						}
					}
				}
				return _fields;
			}
		}
		
		public V get<V>(string fieldId) {
			ISettingsField field;
			if ( fields.TryGetValue(fieldId, out field) ) {
				return (V)field.get();
			}
			return default(V);
		}
		
		protected void attachFieldIfPossible(string fieldId) {
			//if a field that contains another ISettingsInstane object or a container that contains more of those is changed we have to attach those objects to this one
			ISettingsField field;
			if ( fields.TryGetValue(fieldId, out field) ) {
				object value = field.get();
				if ( value != null ) {
					SettingsProperties settings = SettingsStorage.getSettingsProperties(getId());
					ISettingsFieldProperties fieldSettings = settings.fields.Find(f=>f.id.Equals(fieldId));
					if ( fieldSettings.isSettings ) {
						((ISettingsInstance)value).attach(this);
					} else if ( fieldSettings.isList && ((ISettingsFieldPropertiesList)fieldSettings).isListSettings ) {
						foreach ( ISettingsInstance exposable in (IList)value ) {
							exposable.attach(this);
						}
					}
				}
			}
		}
		
		public void set<V>(string fieldId, V value) {
			ISettingsField field;
			if ( fields.TryGetValue(fieldId, out field) ) {
				if ( field.set(value) ) {
					attachFieldIfPossible(fieldId);
					markAsChanged();
				}
			}
		}
		
		public virtual void adopt(ISettingsInstance source) {
			if ( this == source ) return;
			id = source.getId();
			baseObjectKey = source.getKey();
			label = source.getLabel();
			fields.Clear();
			SettingsProperties settings = SettingsStorage.getSettingsProperties(getId());
			MethodInfo miGetNonGeneric = this.GetType().GetMethod("get", BindingFlags.Instance|BindingFlags.Public);
			MethodInfo miSetNonGeneric = this.GetType().GetMethod("set", BindingFlags.Instance|BindingFlags.Public);
			foreach ( SettingsFieldProperties props in settings.fields ) {
				miSetNonGeneric.MakeGenericMethod(props.type).Invoke(this,new Object[]{props.id,miGetNonGeneric.MakeGenericMethod(props.type).Invoke(source,new Object[]{props.id})});
			}
			active = source.getActive();
			dirty = source.getDirty();
		}
		
		public virtual void markAsChanged() {
			if ( parentObject == null ) {
				active = true;
				dirty = true;
			} else {
				parentObject.markAsChanged();
			}
			updateDynamicLabel();
		}
		
		public virtual void reset() {
			SettingsProperties properties = SettingsStorage.getSettingsProperties(getId());
			ISettingsInstance originalSettings;
			if ( properties.overriddenSettings.TryGetValue(getKey(), out originalSettings) ) {
				adopt(originalSettings);
			} else {
				adopt(new SettingsInstance(id,((ISettingsVisualizationSelection)properties.visualization).getBaseObjectFromKey(getKey())));
			}
			active = false;
			dirty = false;
		}
		
		protected virtual void OnSerialize() {
			Scribe_Values.Look<string>(ref baseObjectKey, "baseObjectKey", null);
			Scribe_Values.Look<string>(ref label, "label", null);
		}
		
		protected virtual void OnBeforeSerialize() {}
		
		protected virtual void OnAfterDeserialize() {
			foreach ( string fieldId in fields.Keys ) {
				attachFieldIfPossible(fieldId);
			}
			active = true;
			dirty = true;
		}
		
		public void ExposeData()
		{
			if ( Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.Saving ) {
				Scribe_Values.Look<string>(ref id, "id", null);
				SettingsProperties properties = SettingsStorage.getSettingsProperties(getId());
				if (Scribe.mode == LoadSaveMode.Saving) {
					OnBeforeSerialize();
					foreach ( SettingsFieldProperties p in properties.fields ) {
						ISettingsField field; if ( fields.TryGetValue(p.id,out field) ) field.OnBeforeSerialize(p);
					}
				}
				OnSerialize();
				foreach ( SettingsFieldProperties p in properties.fields ) {
					ISettingsField field; if ( fields.TryGetValue(p.id,out field) ) field.ExposeData(p);
				}
				if (Scribe.mode != LoadSaveMode.Saving) {
					OnAfterDeserialize();
					foreach ( SettingsFieldProperties p in properties.fields ) {
						ISettingsField field; if ( fields.TryGetValue(p.id,out field) ) field.OnAfterDeserialize(p);
					}
				}
			}
		}
	}
}

/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 30.10.2017
 * Time: 06:19
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using RIMMSqol.renderers;
using UnityEngine;
using Verse;

namespace RIMMSqol.genericSettings
{
	public interface ISettingsFieldProperties {
		string id{get;}
		string label{get;set;}
		string labelTooltip{get;set;}
		int order{get;}
		Func<object,object> getFromBaseObject{get;}
		object defaultValue{get;}
		Type type{get;}
		bool isSettings{get;}
		bool isList{get;}
		bool isSelectable{get;}
	}
	
	public interface ISettingsFieldPropertiesSelectSingle : ISettingsFieldProperties {
		Func<Flow,Func<object,bool>> onSelectItem{get;}
		Func<Flow,IEnumerable<object>> selectableItems{get;}
		Func<object, String> labelProducer {get;}
		bool isNullAllowed{get;}
	}
	
	public interface ISettingsFieldPropertiesList : ISettingsFieldProperties {
		Func<object> createNewEntry{get;}
		bool isRemovalAllowed{get;}
		bool isAddingAllowed{get;}
		bool isListSettings{get;}
		bool isListPrimitive{get;}
	}
	
	public abstract class SettingsFieldProperties : ISettingsFieldProperties {
		virtual public bool isSettings { get { throw new NotImplementedException(); } }
		virtual public bool isList { get { throw new NotImplementedException(); } }
		virtual public bool isSelectable { get { throw new NotImplementedException(); } }
		public string id { get; protected set; }
		public string label { get; set; }
		public string labelTooltip { get; set; }
		public int order { get; protected set; }
		public Func<object, object> getFromBaseObject { get; protected set; }
		public object defaultValue { get; protected set; }
		public Type type { get; protected set; }
		
		protected SettingsFieldProperties(string id, string label, int order, Type type, object defaultValue, Func<object, object> getFromBaseObject)
		{
			if (id == null)
				throw new ArgumentNullException("id");
			if (order < 0 )
				throw new ArgumentOutOfRangeException("order", order, "Value must be greater than 0");
			if (type == null)
				throw new ArgumentNullException("type");
			if (getFromBaseObject == null)
				throw new ArgumentNullException("getFromBaseObject");
			if (defaultValue != null && !type.IsInstanceOfType(defaultValue) )
				throw new ArgumentException("defaultValue \""+defaultValue+"\" of type \""+defaultValue.GetType()+"\" is incompatible with provided field type \""+type+"\".");
			this.id = id;
			this.label = label;
			this.order = order;
			this.type = type;
			this.defaultValue = defaultValue;
			this.getFromBaseObject = getFromBaseObject;
		}
	}
	
	public abstract class SettingsFieldPropertiesPrimitive : SettingsFieldProperties {
		override public bool isSettings { get { return false; } }
		override public bool isList { get { return false; } }
		override public bool isSelectable { get { return false; } }
		
		protected SettingsFieldPropertiesPrimitive(string id, string label, int order, Type type, object defaultValue, Func<object, object> getFromBaseObject) 
			: base(id,label,order,type,defaultValue,getFromBaseObject)
		{
			if (!(type == typeof(float) || type == typeof(int) || type == typeof(string) || type == typeof(bool) || type == typeof(Color)))
				throw new ArgumentException("type for a primitive field must be either float, int, string, bool or Color.");
		}
	}
	
	public interface ISettingsFieldPropertiesPrimitiveNumeric<T> {
		T maxValue {get;}
		T minValue {get;}
	}
	
	public interface ISettingsFieldPropertiesPrimitiveInt : ISettingsFieldPropertiesPrimitiveNumeric<int> {}
	
	public class SettingsFieldPropertiesPrimitiveInt<T> : SettingsFieldPropertiesPrimitive, ISettingsFieldPropertiesPrimitiveInt {
		public int maxValue { get; protected set; }
		public int minValue { get; protected set; }
		public SettingsFieldPropertiesPrimitiveInt(string id, string label, int order, int defaultValue, Func<T, int> getFromBaseObject, int minValue = int.MinValue, int maxValue = int.MaxValue) 
			: base(id,label,order,typeof(int),defaultValue,o=>getFromBaseObject((T)o)) {
			this.minValue = minValue;
			this.maxValue = maxValue;
		}
	}
	
	public interface ISettingsFieldPropertiesPrimitiveFloat : ISettingsFieldPropertiesPrimitiveNumeric<float> {}
	
	public class SettingsFieldPropertiesPrimitiveFloat<T> : SettingsFieldPropertiesPrimitive, ISettingsFieldPropertiesPrimitiveFloat {
		public float maxValue { get; protected set; }
		public float minValue { get; protected set; }
		public SettingsFieldPropertiesPrimitiveFloat(string id, string label, int order, float defaultValue, Func<T, float> getFromBaseObject, float minValue = float.MinValue, float maxValue = float.MaxValue) 
			: base(id,label,order,typeof(float),defaultValue,o=>getFromBaseObject((T)o)) {
			this.minValue = minValue;
			this.maxValue = maxValue;
		}
	}
	
	public interface ISettingsFieldPropertiesPrimitiveString {
		int maxNumOfChars {get;}
		Regex inputValidator {get;}
	}
	
	public class SettingsFieldPropertiesPrimitiveString<T> : SettingsFieldPropertiesPrimitive, ISettingsFieldPropertiesPrimitiveString {
		public int maxNumOfChars { get; protected set; }
		public Regex inputValidator { get; protected set; }
		public SettingsFieldPropertiesPrimitiveString(string id, string label, int order, string defaultValue, Func<T, string> getFromBaseObject, int maxNumOfChars = int.MaxValue, Regex inputValidator = null) 
			: base(id,label,order,typeof(string),defaultValue,o=>getFromBaseObject((T)o)) {
			this.maxNumOfChars = maxNumOfChars;
			this.inputValidator = inputValidator;
		}
	}
	
	public class SettingsFieldPropertiesPrimitiveBool<T> : SettingsFieldPropertiesPrimitive {
		public SettingsFieldPropertiesPrimitiveBool(string id, string label, int order, bool defaultValue, Func<T, bool> getFromBaseObject) 
			: base(id,label,order,typeof(bool),defaultValue,o=>getFromBaseObject((T)o)) {}
	}
	
	public class SettingsFieldPropertiesPrimitiveColor<T> : SettingsFieldPropertiesPrimitive {
		public SettingsFieldPropertiesPrimitiveColor(string id, string label, int order, Color defaultValue, Func<T, Color> getFromBaseObject) 
			: base(id,label,order,typeof(Color),defaultValue,o=>getFromBaseObject((T)o)) {}
	}
	
	public class SettingsFieldPropertiesSettings : SettingsFieldProperties {
		override public bool isSettings { get { return true; } }
		override public bool isList { get { return false; } }
		override public bool isSelectable { get { return false; } }
		
		public SettingsFieldPropertiesSettings(string id, string label, int order, Type type, object defaultValue, Func<object, object> getFromBaseObject) 
			: base(id,label,order,type,defaultValue,getFromBaseObject)
		{
			if (!typeof(ISettingsInstance).IsAssignableFrom(type))
				throw new ArgumentException("type for settings field must implement the interface \""+typeof(ISettingsInstance)+"\".");
		}
	}
	
	public class SettingsFieldPropertiesSelectable : SettingsFieldProperties, ISettingsFieldPropertiesSelectSingle {
		static public Func<object, String> DefLabelProducer = o=>o != null ? ((Def)o).label.NullOrEmpty() ? ((Def)o).defName : ((Def)o).label : "NONE";
		public Func<Flow, Func<object, bool>> onSelectItem { get; protected set; }
		public Func<Flow, IEnumerable<object>> selectableItems { get; protected set; }
		public Func<object, String> labelProducer { get; protected set; }
		public bool isNullAllowed { get; protected set; }
		override public bool isSettings { get { return false; } }
		override public bool isList { get { return false; } }
		override public bool isSelectable { get { return true; } }
		
		public SettingsFieldPropertiesSelectable(string id, string label, int order, Type type, object defaultValue, Func<object, object> getFromBaseObject, Func<Flow, IEnumerable<object>> selectableItems, Func<Flow, Func<object, bool>> onSelectItem, Func<object, String> labelProducer, bool isNullAllowed) 
			: base(id,label,order,type,defaultValue,getFromBaseObject)
		{
			this.selectableItems = selectableItems;
			this.onSelectItem = onSelectItem;
			this.isNullAllowed = isNullAllowed;
			this.labelProducer = labelProducer;
			if ( isNullAllowed ) {
				this.selectableItems = flow => { IEnumerable<object> lst = selectableItems(flow); if ( !lst.Contains(null) ) { lst = new object[]{null}.Concat(lst); } return lst; };
			}
		}
	}
	
	
	public abstract class SettingsFieldPropertiesList : SettingsFieldProperties, ISettingsFieldPropertiesList {
		virtual public bool isListSettings { get { throw new NotImplementedException(); } }
		virtual public bool isListPrimitive { get { throw new NotImplementedException(); } }
		public Func<object> createNewEntry { get; protected set; }
		override public bool isSettings { get { return false; } }
		override public bool isList { get { return true; } }
		override public bool isSelectable { get { return false; } }
		public bool isRemovalAllowed { get; protected set; }
		public bool isAddingAllowed { get; protected set; }
		
		protected SettingsFieldPropertiesList(string id, string label, int order, Type type, object defaultValue, Func<object, object> getFromBaseObject, Func<object> createNewEntry = null, bool isRemovalAllowed = false) 
			: base(id,label,order,type,defaultValue,getFromBaseObject)
		{
			this.createNewEntry = createNewEntry;
			this.isAddingAllowed = createNewEntry != null;
			this.isRemovalAllowed = isRemovalAllowed;
		}
	}
	
	public class SettingsFieldPropertiesListPrimitive : SettingsFieldPropertiesList {
		override public bool isListSettings { get { return false; } }
		override public bool isListPrimitive { get { return true; } }
		override public bool isSelectable { get { return false; } }
		public Func<ISettingsInstance,Flow,string> listLabelProducer {get; protected set;}
		
		public SettingsFieldPropertiesListPrimitive(string id, string label, int order, Type type, object defaultValue, Func<object, object> getFromBaseObject, Func<ISettingsInstance,Flow,string> listLabelProducer, Func<object> createNewEntry = null, bool isRemovalAllowed = false)
			: base(id,label,order,type,defaultValue,getFromBaseObject,createNewEntry,isRemovalAllowed)
		{
			this.listLabelProducer = listLabelProducer;
		}
	}
	
	public class SettingsFieldPropertiesListSettings : SettingsFieldPropertiesList {
		override public bool isListSettings { get { return true; } }
		override public bool isListPrimitive { get { return false; } }
		override public bool isSelectable { get { return false; } }
		public string idEnclosedSettings {get; protected set;}
		
		public SettingsFieldPropertiesListSettings(string id, string label, int order, Type type, object defaultValue, Func<object, object> getFromBaseObject, string idEnclosedSettings, Func<object> createNewEntry = null, bool isRemovalAllowed = false)
			: base(id,label,order,type,defaultValue,getFromBaseObject,createNewEntry,isRemovalAllowed)
		{
			if (idEnclosedSettings == null)
				throw new ArgumentNullException("idEnclosedSettings");
			this.idEnclosedSettings = idEnclosedSettings;
		}
	}
	
	//TODO: SettingsField can be a selectable primitive and a selectable setting
	public class SettingsFieldPropertiesListSelectable : SettingsFieldPropertiesList {
		override public bool isListSettings { get { return false; } }
		override public bool isListPrimitive { get { return false; } }
		override public bool isSelectable { get { return true; } }
		public Func<Flow, Func<object, bool>> onSelectItem { get; protected set; }
		public Func<Flow, IEnumerable<object>> selectableItems { get; protected set; }
		public Func<object,string> labelProducer {get; protected set;}
		public Func<object,string> tooltipProducer {get; protected set;}
		
		public SettingsFieldPropertiesListSelectable(string id, string label, int order, Type type, object defaultValue, Func<object, object> getFromBaseObject, Func<Flow, IEnumerable<object>> selectableItems, Func<Flow, Func<object, bool>> onSelectItem, Func<object,string> labelProducer, Func<object> createNewEntry = null, bool isRemovalAllowed = false, Func<object,string> tooltipProducer = null)
			: base(id,label,order,type,defaultValue,getFromBaseObject,createNewEntry,isRemovalAllowed)
		{
			this.selectableItems = selectableItems;
			this.onSelectItem = onSelectItem;
			this.labelProducer = labelProducer;
			this.tooltipProducer = tooltipProducer;
		}
	}
}

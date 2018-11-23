/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 30.10.2017
 * Time: 06:21
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
	/// Description of SettingsField.
	/// </summary>
	public interface ISettingsField
	{
		bool set(object value);
		object get();
		void OnBeforeSerialize(SettingsFieldProperties p);
		void OnAfterDeserialize(SettingsFieldProperties p);
		void ExposeData(SettingsFieldProperties p);
	}
	
	public class PrimitiveSettingsField : ISettingsField {
		protected object _value;
		public bool set(object value) {
			if ( (value == null && _value != null) || (value != null && !value.Equals(_value)) ) {
				_value = value;
				return true;
			}
			return false;
		}
		public object get() {
			return _value;
		}
		public void OnBeforeSerialize(SettingsFieldProperties p) {}
		public void OnAfterDeserialize(SettingsFieldProperties p) {}
		public void ExposeData(SettingsFieldProperties p) {
			MethodInfo mi = typeof(Scribe_Values).GetMethod("Look", BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(p.type);
			object[] arguments = new object[]{_value,p.id,null,false};
			mi.Invoke(null,arguments);
			_value = arguments[0];
		}
	}
	
	
	
	public class DefSettingsField : ISettingsField {
		public static Type GetTypeFromAnyAssemblyVersion(string typeName)
		{
			Type t = Type.GetType(typeName);
			if ( t == null ) {
				AssemblyName an = new AssemblyName(typeName.Substring(typeName.IndexOf(',')+1));
				an.Version = null;
				return Assembly.Load(an).GetType(typeName.Substring(0,typeName.IndexOf(',')));
			}
	        return t;
		}
		
		protected string _defName, _typeName;
		protected Def _def;
		public bool set(object value)
		{
			if ((value != null && _defName != ((Def)value).defName) || (value == null && _defName != null)) { 
				_def = (Def)value;
				_typeName = value != null ? value.GetType().AssemblyQualifiedName : null;
				_defName = value != null ? ((Def)value).defName : null; 
				return true; 
			}
			return false;
		}
		public object get()
		{
			if ( _def == null && _defName != null ) {
				MethodInfo mi = typeof(DefDatabase<>).MakeGenericType(GetTypeFromAnyAssemblyVersion(_typeName)).GetMethod("GetNamed", BindingFlags.Public | BindingFlags.Static);
				_def = (Def)mi.Invoke(null,new object[]{_defName,false});
			}
			return _def;
		}
		public void OnBeforeSerialize(SettingsFieldProperties p) {}
		public void OnAfterDeserialize(SettingsFieldProperties p) {}
		public void ExposeData(SettingsFieldProperties p) {
			Scribe_Values.Look<string>(ref _defName, p.id, null);
			Scribe_Values.Look<string>(ref _typeName, p.id+"_type", null);
		}
	}
	
	public class ListSettingsField : ISettingsField {
		protected IList _value;
		public bool set(object value)
		{
			if ( !listEquals(_value,(IList)value) ) {
				if ( _value != null ) {
					_value.Clear();
				} else if (value != null) {
					_value = (IList)Activator.CreateInstance(value.GetType());
				}
				if ( value != null ) {
					foreach ( object o in (IList)value ) {
						_value.Add(o);
					}
				} else {
					_value = null;
				}
				return true;
			}
			return false;
		}
		public object get()
		{
			return _value;
		}
		protected bool listEquals(IList current, IList value) {
			if ( current == value ) return true;
			if ( current == null || value == null ) return false;
			if ( current.Count != value.Count ) return false;
			IEnumerator enumerator1 = current.GetEnumerator();
			IEnumerator enumerator2 = value.GetEnumerator();
			while ( enumerator1.MoveNext() ) {
				if ( !enumerator2.MoveNext() ) return false;
				if ( object.ReferenceEquals(enumerator1.Current, enumerator2.Current) ) continue;
				if ( object.ReferenceEquals(null,enumerator1.Current) || object.ReferenceEquals(null,enumerator2.Current) ) return false;
				if ( !enumerator1.Current.Equals(enumerator2.Current) ) return false;
			}
			return true;
		}
		public void OnBeforeSerialize(SettingsFieldProperties p){}
		public void OnAfterDeserialize(SettingsFieldProperties p){}
		public void ExposeData(SettingsFieldProperties p)
		{
			MethodInfo miLook = null;
			string[] parameterNames = new string[]{"list","label","lookMode","ctorArgs"};
			foreach ( MethodInfo mi in typeof(Scribe_Collections).GetMember("Look") ) {
				if ( mi.GetParameters().Length == parameterNames.Length ) {
					bool mismatch = false; int i = 0;
					foreach ( ParameterInfo pi in mi.GetParameters() ) {
						if ( !pi.Name.Equals(parameterNames[i]) ) {
							mismatch = true; break;
						} else {
							i++;
						}
					}
					if ( !mismatch ) {
						miLook = mi;
						break;
					}
				}
			}
			if ( miLook != null ) {
				miLook = miLook.MakeGenericMethod(p.type.GetGenericArguments()[0]);
				object[] arguments = new object[]{_value,p.id,LookMode.Undefined,null};
				miLook.Invoke(null,arguments);
				_value = (IList)arguments[0];
			} else {
				Log.Error("Failed to build generic look method for list field!");
			}
		}
	}
}

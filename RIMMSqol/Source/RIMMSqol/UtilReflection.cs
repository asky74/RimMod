/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 29.05.2018
 * Time: 07:31
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace RIMMSqol
{
	/// <summary>
	/// Description of UtilReflection.
	/// </summary>
	static public class UtilReflection
	{
		public static Func<S, T> CreateGetter<S, T>(FieldInfo field)
		{
			string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
			DynamicMethod setterMethod = new DynamicMethod(methodName, typeof(T), new Type[1] { typeof(S) }, field.Module, true);
			ILGenerator gen = setterMethod.GetILGenerator();
			if (field.IsStatic)
			{
				gen.Emit(OpCodes.Ldsfld, field);
			}
			else
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, field);
			}
			gen.Emit(OpCodes.Ret);
			return (Func<S, T>)setterMethod.CreateDelegate(typeof(Func<S, T>));
		}
		
		public static Action<T,S> CreateSetter<T,S>(FieldInfo field)
	    {
			string methodName = field.ReflectedType.FullName + ".set_" + field.Name;
			DynamicMethod setterMethod = new DynamicMethod(methodName, null, new []{ typeof(T), typeof(S) }, field.Module, true);
	        ILGenerator gen = setterMethod.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stfld, field);
	        gen.Emit(OpCodes.Ret);
	        
	        return (Action<T,S>)setterMethod.CreateDelegate(typeof(Action<T,S>));
	    }
	}
}

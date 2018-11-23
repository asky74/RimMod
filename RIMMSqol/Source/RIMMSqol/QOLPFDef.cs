/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 09.03.2018
 * Time: 21:12
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Reflection;
using RimWorld;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of QOLPFDef.
	/// </summary>
	public class QOLPFDef : Def
	{
		public Type algorithmClass;
		
		[Unsaved]
		private MethodInfo findPath;
		[Unsaved]
		private ConstructorInfo constructor;
		
		public override void ResolveReferences()
		{
			base.ResolveReferences();
			constructor = algorithmClass.GetConstructor(new Type[]{typeof(string)});
			findPath = algorithmClass.GetMethod("findPath");
		}
		
		public MethodInfo GetFindPathMethod() {
			return findPath;
		}
		
		public ConstructorInfo GetConstructor() {
			return constructor;
		}
	}
}

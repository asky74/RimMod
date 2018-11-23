/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 10.03.2018
 * Time: 10:16
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.Reflection;
using Verse;
using Verse.AI;

namespace RIMMSqol.pathfinding
{
	/// <summary>
	/// Description of ReflectionMethodInvoker.
	/// </summary>
	public class ReflectionMethodInvoker
	{
		public object instance; 
		public MethodInfo mi;
		public delegate PawnPath findPathType(Map map, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode, ByteGrid avoidGrid, Area allowedArea,int costsCardinal, int costsDiagonal);
		public findPathType findPath;
		
		public ReflectionMethodInvoker(object instance, MethodInfo mi) {
			this.instance = instance; 
			this.mi = mi;
			this.findPath = (findPathType)Delegate.CreateDelegate(typeof(findPathType), instance, mi);
		}
		public object invoke(object[] parameters) {
			return mi.Invoke(instance, parameters);
		}
	}
}

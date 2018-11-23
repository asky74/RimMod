/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 06.03.2018
 * Time: 02:57
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using Verse;
using Verse.AI;

namespace RIMMSqol.pathfinding
{
	/// <summary>
	/// Description of PathfinderAlgorithm.
	/// </summary>
	public interface PathfinderAlgorithm
	{
		PawnPath findPath(Map map, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode, ByteGrid avoidGrid, Area allowedArea,
		                        int costsCardinal, int costsDiagonal);
	}
}

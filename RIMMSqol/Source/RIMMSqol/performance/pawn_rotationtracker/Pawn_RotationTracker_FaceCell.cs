/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.07.2018
 * Time: 04:21
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Reflection;
using Harmony;
using Verse;

namespace RIMMSqol.performance.pawn_rotationtracker
{
	/// <summary>
	/// Description of Pawn_RotationTracker_FaceCell.
	/// </summary>
	[HarmonyPatch(typeof(Pawn_RotationTracker))]
	[HarmonyPatch("FaceCell")]
	[HarmonyPatchNamespace("PawnRotation")]
	static class Pawn_RotationTracker_FaceCell
	{
		static Func<Pawn_RotationTracker,Pawn> getter = UtilReflection.CreateGetter<Pawn_RotationTracker,Pawn>(typeof(Pawn_RotationTracker).GetField("pawn",BindingFlags.Instance|BindingFlags.NonPublic));
		static Rot4 constRot4North = Rot4.North;
		static Rot4 constRot4South = Rot4.South;
		static Rot4 constRot4East = Rot4.East;
		static Rot4 constRot4West = Rot4.West;
		
		static bool Prefix(Pawn_RotationTracker __instance, IntVec3 c) {
			Pawn p = getter(__instance);
			IntVec3 pos = p.Position;
			
			int dx = pos.x-c.x;
			int dz = pos.z-c.z;
			if ( dx != 0 || dz != 0 ) {
				if ( Math.Abs(dx) > Math.Abs(dz) ) {
					if ( dx > 0 ) {
						p.Rotation = constRot4West;
					} else {
						p.Rotation = constRot4East;
					}
				} else {
					if ( dz > 0 ) {
						p.Rotation = constRot4South;
					} else {
						p.Rotation = constRot4North;
					}
				}
			}
			
			return false;
		}
	}
}

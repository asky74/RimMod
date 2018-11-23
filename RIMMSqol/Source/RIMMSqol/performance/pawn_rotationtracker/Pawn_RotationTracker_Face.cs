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
using UnityEngine;
using Verse;

namespace RIMMSqol.performance.pawn_rotationtracker
{
	/// <summary>
	/// Description of Pawn_RotationTracker_Face.
	/// </summary>
	[HarmonyPatch(typeof(Pawn_RotationTracker))]
	[HarmonyPatch("Face")]
	[HarmonyPatchNamespace("PawnRotation")]
	static class Pawn_RotationTracker_Face
	{
		static Func<Pawn_RotationTracker,Pawn> getter = UtilReflection.CreateGetter<Pawn_RotationTracker,Pawn>(typeof(Pawn_RotationTracker).GetField("pawn",BindingFlags.Instance|BindingFlags.NonPublic));
		static Rot4 constRot4North = Rot4.North;
		static Rot4 constRot4South = Rot4.South;
		static Rot4 constRot4East = Rot4.East;
		static Rot4 constRot4West = Rot4.West;

		static bool Prefix(Pawn_RotationTracker __instance, Vector3 p) {
			Pawn pawn = getter(__instance);
			Vector3 pos = pawn.DrawPos;
			
			float dx = pos.x-p.x;
			float dz = pos.z-p.z;
			if (Math.Abs(dx) > 9.99999944E-11f || Math.Abs(dz) > 9.99999944E-11f ) {
				if ( Math.Abs(dx) > Math.Abs(dz) ) {
					if ( dx > 0 ) {
						pawn.Rotation = constRot4West;
					} else {
						pawn.Rotation = constRot4East;
					}
				} else {
					if ( dz > 0 ) {
						pawn.Rotation = constRot4South;
					} else {
						pawn.Rotation = constRot4North;
					}
				}
			}
			
			return false;
		}
	}
}

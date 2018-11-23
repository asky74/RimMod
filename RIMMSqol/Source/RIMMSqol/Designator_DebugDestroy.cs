/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 05.06.2018
 * Time: 03:29
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of Designator_DebugDestroy.
	/// </summary>
	public class Designator_DebugDestroy : Designator
	{
		public override int DraggableDimensions
		{
			get
			{
				return 2;
			}
		}

		public Designator_DebugDestroy()
		{
			this.defaultLabel = "DesignatorDebugDestroy".Translate();
			this.defaultDesc = "DesignatorDebugDestroyDesc".Translate();
			this.icon = ContentFinder<Texture2D>.Get("UI/Designators/Deconstruct", true);
			this.soundDragSustain = SoundDefOf.Designate_DragStandard;
			this.soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			this.useMouseIcon = true;
			this.soundSucceeded = SoundDefOf.Designate_Deconstruct;
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 c)
		{
			foreach (Thing t in base.Map.thingGrid.ThingsAt(c) ) {
				if (CanDesignateThing(t).Accepted) return AcceptanceReport.WasAccepted;
			}
			return AcceptanceReport.WasRejected;
		}

		public override void DesignateSingleCell(IntVec3 c)
		{
			foreach (Thing t in base.Map.thingGrid.ThingsAt(c) ) {
				if (CanDesignateThing(t).Accepted) {
					DesignateThing(t);
				}
			}
		}
		
		public override void DesignateThing(Thing t)
		{
			if ( !t.Destroyed && (Thing.allowDestroyNonDestroyable || t.def.destroyable) ) t.Destroy(DestroyMode.Vanish);
		}

		public override AcceptanceReport CanDesignateThing(Thing t)
		{
			if ( t == null ) return AcceptanceReport.WasRejected;
			return DebugSettings.godMode && !t.Destroyed && (Thing.allowDestroyNonDestroyable || t.def.destroyable);
		}
		
		public override bool CanRemainSelected()
		{
			return CanDesignateThing(Find.Selector.SingleSelectedThing).Accepted;
		}
	}
}

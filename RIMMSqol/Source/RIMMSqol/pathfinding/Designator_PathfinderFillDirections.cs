/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 11.04.2018
 * Time: 01:25
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RIMMSqol.pathfinding
{
	/// <summary>
	/// Description of Designator_PathfinderFillDirections.
	/// </summary>
	public class Designator_PathfinderFillDirections : Designator
	{
		public override int DraggableDimensions
		{
			get
			{
				return 2;
			}
		}

		public override bool DragDrawMeasurements
		{
			get
			{
				return true;
			}
		}
		
		public Designator_PathfinderFillDirections()
		{
			this.soundDragSustain = SoundDefOf.Designate_DragStandard;
			this.soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			this.useMouseIcon = true;
			
			this.defaultLabel = "DesignatorPathfinderFillDirections".Translate();
			this.defaultDesc = "DesignatorPathfinderFillDirectionsDesc".Translate();
			this.icon = Materials.designatorPathfinderFillDirections;
			this.soundSucceeded = SoundDefOf.Designate_AreaAdd;
			this.hotKey = KeyBindingDefOf.Misc10;
		}
		
		public override void SelectedUpdate() {
			GenUI.RenderMouseoverBracket();
			//if ( Find.WindowStack.WindowOfType<Window_SelectPathfinderFillDirections>() == null ) {
	            MapComponent_PathfinderDirections grid = base.Map.GetComponent<MapComponent_PathfinderDirections>();
	            if (grid == null)
	            {
	                grid = new MapComponent_PathfinderDirections(base.Map);
	                base.Map.components.Add(grid);
	            }
				grid.MarkForDraw();
			//}
		}
		
		public override AcceptanceReport CanDesignateCell(IntVec3 loc) {
			return loc.InBounds(base.Map);
		}
		
		public override void DesignateSingleCell(IntVec3 c)
        {
            MapComponent_PathfinderDirections grid = base.Map.GetComponent<MapComponent_PathfinderDirections>();
            if (grid == null) {
                grid = new MapComponent_PathfinderDirections(base.Map);
                base.Map.components.Add(grid);
            }
            grid.SetDirections(c, Window_SelectPathfinderFillDirections.selection);
		}

		public override void ProcessInput(Event ev)
		{
			if (!base.CheckCanInteract())
			{
				return;
			}
			if ( Find.WindowStack.WindowOfType<Window_SelectPathfinderFillDirections>() == null ) Find.WindowStack.Add(new Window_SelectPathfinderFillDirections());
			base.ProcessInput(ev);
		}
	}
}

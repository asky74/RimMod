/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.04.2018
 * Time: 22:50
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace RIMMSqol.pathfinding
{
	/// <summary>
	/// Description of Designator_PathfinderDirections.
	/// </summary>
	public class Designator_PathfinderDirections : Designator
	{
		private static IEnumerable<Direction8Way> directions;

		public override int DraggableDimensions
		{
			get
			{
				return 1;
			}
		}

		public override bool DragDrawMeasurements
		{
			get
			{
				return true;
			}
		}

		public Designator_PathfinderDirections()
		{
			this.soundDragSustain = SoundDefOf.Designate_DragStandard;
			this.soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			this.useMouseIcon = true;
			Designator_PathfinderDirections.directions = new []{Direction8Way.North};
			
			this.defaultLabel = "DesignatorPathfinderDirections".Translate();
			this.defaultDesc = "DesignatorPathfinderDirectionsDesc".Translate();
			this.icon = Materials.designatorPathfinderDirections;
			this.soundSucceeded = SoundDefOf.Designate_AreaAdd;
			this.hotKey = KeyBindingDefOf.Misc9;
		}

		public override void SelectedUpdate() {
			GenUI.RenderMouseoverBracket();
            MapComponent_PathfinderDirections grid = base.Map.GetComponent<MapComponent_PathfinderDirections>();
            if (grid == null)
            {
                grid = new MapComponent_PathfinderDirections(base.Map);
                base.Map.components.Add(grid);
            }
			grid.MarkForDraw();
		}
		
		public override AcceptanceReport CanDesignateCell(IntVec3 loc) {
			return loc.InBounds(base.Map);
		}

		public override void DesignateMultiCell(IEnumerable<IntVec3> cells) {
			if ( cells != null ) {
				DesignationDragger dragger = Find.DesignatorManager.Dragger;
				FieldInfo fi = typeof(DesignationDragger).GetField("startDragCell", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
				IntVec3 startDragCell = (IntVec3)fi.GetValue(dragger);
				int minX = int.MaxValue, minZ = int.MaxValue, maxX = int.MinValue, maxZ = int.MinValue;
				foreach ( IntVec3 v in cells ) {
					minX = Math.Min(minX,v.x);
					minZ = Math.Min(minZ,v.z);
					maxX = Math.Max(maxX,v.x);
					maxZ = Math.Max(maxZ,v.z);
				}
				//whichever abs delta is larger determines if it is horizontal or vertical movement.
				bool horizontalMovement = (maxX-minX)>(maxZ-minZ);
				//based on which end the start cell matches we know the direction of the movement. we also dont want the last cell in the drag since it is used only as an indicator for the direction.
				List<IntVec3> cellsCopy = new List<IntVec3>(cells);
				if ( horizontalMovement ) {
					if ( startDragCell.x == minX ) {
						directions = new []{Direction8Way.East};
						cellsCopy.RemoveAll(v=>v.x == maxX);
					} else {
						directions = new []{Direction8Way.West};
						cellsCopy.RemoveAll(v=>v.x == minX);
					}
				} else {
					if ( startDragCell.z == minZ ) {
						directions = new []{Direction8Way.North};
						cellsCopy.RemoveAll(v=>v.z == maxZ);
					} else {
						directions = new []{Direction8Way.South};
						cellsCopy.RemoveAll(v=>v.z == minZ);
					}
				}
				cells = cellsCopy;
			}
			
			base.DesignateMultiCell(cells);
		}
		
		public override void DesignateSingleCell(IntVec3 c)
        {
            MapComponent_PathfinderDirections grid = base.Map.GetComponent<MapComponent_PathfinderDirections>();
            if (grid == null) {
                grid = new MapComponent_PathfinderDirections(base.Map);
                base.Map.components.Add(grid);
            }
            grid.ToggleDirections(c, directions, false);
		}
	}
}

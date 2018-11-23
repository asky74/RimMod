/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 19.05.2017
 * Time: 10:31
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// A window that displays items on the current map and allows filtering.
	/// </summary>
	public class WTFWindow :  RimWorld.MainTabWindow
	{
		private Vector2 scrollPosition;
		private List<List<Thing>> thingGroups, filteredThingGroups;
		private List<Thing> currentGroup;
		private int currentGroupIndex;
		private string filter = "", lastFilter = null;
		private bool showOnlyForbidden = false, lastShowOnlyForbidden = true;
		
		public WTFWindow() {
			forcePause = true;
		}
		
		public override void PreOpen() {
			base.PreOpen();
			//switch to the map view if the world view is active
			Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.None;
			clearState();
			FogGrid fogGrid = Find.CurrentMap.fogGrid;
			List<Thing> allThings = new List<Thing>();
			//all items, buildings, animals and structures on the map but not the undiscovered terrain.
			allThings.AddRange(Find.CurrentMap.listerThings.AllThings.Where(t => /*!t.def.mineable || */!fogGrid.IsFogged(t.Position)));
			//all the stuff that the colonists on the map have(not the stuff from prisoners, allies or enemies).
			foreach ( Pawn p in Find.CurrentMap.mapPawns.FreeColonistsSpawned ) {
				if ( !p.Dead ) {
					allThings.AddRange(ThingOwnerUtility.GetAllThingsRecursively(p, false));
				}
			}
			//corpses for dead people
			foreach ( Pawn p in Find.CurrentMap.mapPawns.AllPawnsSpawned ) {
				if ( p.Dead ) {
					allThings.Add(p.Corpse);
				}
			}
			
			//grouping the things based on their archetype(e.g. all cowboy hats regardless of the material used)
			thingGroups = (from Thing t in allThings group t by t.def into newGroup orderby newGroup.Key.label select newGroup.ToList()).ToList();
			
			//calculating the widest label and sizing the window to fit that label plus a scrollbar.
			float width = 0;
			foreach ( List<Thing> g in thingGroups ) {
				float x = Text.CalcSize(g.Count() + " " + g[0].def.label).x;
				if (x > width) {
					width = x;
				}
			}
			windowRect.width = width + 32;
			
			windowRect.height = 270;
			windowRect.y = (float)(UI.screenHeight - 35) - windowRect.height;
		}
		
		protected void clearState() {
			thingGroups = null;
			filteredThingGroups = null;
			lastFilter = null;
			lastShowOnlyForbidden = !showOnlyForbidden;
			currentGroup = null;
			currentGroupIndex = 0;
			scrollPosition = Vector2.zero;
		}
		
		public override void PostClose() {
			base.PostClose();
			clearState();
		}
		
		protected void filterDBIfNecessary() {
			if ( filteredThingGroups == null || !filter.EqualsIgnoreCase(lastFilter) || showOnlyForbidden != lastShowOnlyForbidden ) {
				//refining the results with the filters
				filteredThingGroups = new List<List<Thing>>();
				//filteredThingGroups.AddRange(thingGroups.Where(g=>g[0].def.label.ToLower().Contains(filter.ToLower())));
		        foreach(List<Thing> g in thingGroups) {
					if ( g[0].def.label.ToLower().Contains(filter.ToLower()) ) {
						if ( showOnlyForbidden ) {
							IEnumerable<Thing> filteredThings = g.Where(t => t.IsForbidden(Faction.OfPlayer));
							if (filteredThings.Any()) {
								filteredThingGroups.Add(filteredThings.ToList());
							}
						} else filteredThingGroups.Add(g);
					}
		        }
				lastFilter = filter;
				lastShowOnlyForbidden = showOnlyForbidden;
			}
		}
		
		static Designator_DebugDestroy designatorDebugDestroy = new Designator_DebugDestroy();
		
		protected void displayInspectActions() {
			//Since InspectGizmoGrid is internal(you gotta love that vanilla codebase...) lets redo the entire code for the inspect toolbar
			List<Gizmo> gizmoList = new List<Gizmo>();
			List<object> selectedObjects = Find.Selector.SelectedObjectsListForReading;
			if ( selectedObjects.NullOrEmpty() ) return;
			for (int i = 0; i < selectedObjects.Count; i++)
			{
				ISelectable selectable = selectedObjects[i] as ISelectable;
				if (selectable != null)
				{
					foreach (Gizmo current in selectable.GetGizmos())
					{
						gizmoList.Add(current);
					}
				}
			}
			for (int j = 0; j < selectedObjects.Count; j++)
			{
				Thing t = selectedObjects[j] as Thing;
				if (t != null)
				{
					IEnumerable<Designator> allDesignators = Find.ReverseDesignatorDatabase.AllDesignators.Concat(designatorDebugDestroy);
					foreach (Designator des in allDesignators)
					{
						if (des.CanDesignateThing(t).Accepted)
						{
							Command_Action command_Action = new Command_Action();
							command_Action.defaultLabel = des.LabelCapReverseDesignating(t);
							float iconAngle;
							Vector2 iconOffset;
							command_Action.icon = des.IconReverseDesignating(t, out iconAngle, out iconOffset);
							command_Action.iconAngle = iconAngle;
							command_Action.iconOffset = iconOffset;
							command_Action.defaultDesc = des.DescReverseDesignating(t);
							command_Action.action = delegate
							{
								if (!TutorSystem.AllowAction(des.TutorTagDesignate))
								{
									return;
								}
								des.DesignateThing(t);
								des.Finalize(true);
							};
							command_Action.hotKey = des.hotKey;
							command_Action.groupKey = des.groupKey;
							gizmoList.Add(command_Action);
						}
					}
				}
			}
			
			Gizmo mouseoverGizmo;
			GizmoGridDrawer.DrawGizmoGrid(gizmoList, 20f + windowRect.width, out mouseoverGizmo);
		}
		
		public override void DoWindowContents(Rect inRect)
		{
			//can happen when the window gets closed
			if ( thingGroups == null ) return;
			Text.Font = GameFont.Tiny;
			
			//a text field that the user can enter a value for filtering the results at the top of the window
			filter = Widgets.TextField(new Rect(0f, 0f, inRect.width, 23f), filter);
			Widgets.CheckboxLabeled(new Rect(0f, 23f, inRect.width, 23f), "Only Forbidden", ref showOnlyForbidden, false); 
			
			filterDBIfNecessary();
			
			//the scrollviews content area is narrower than the full window but possibly taller thus showing the vertical scrollbar if necessary
			Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, (float)filteredThingGroups.Count() * 23f);
			//the scrollview is positioned below the text field and shows no more content than the window can display.
			Widgets.BeginScrollView(new Rect(inRect.x,inRect.y+50f,inRect.width, inRect.height-50f), ref this.scrollPosition, viewRect, true);
			
			//drawing a label for each entry inside the scrollview content area. If a group is clicked we cycle through the things in the clicked group.
			int indx = 0;
			Rect rect = new Rect(0, 0, viewRect.width, 23f);
			foreach ( List<Thing> g in filteredThingGroups ) {
				if (Mouse.IsOver(rect))
				{
					Widgets.DrawHighlight(rect);
					/**
					 * Everytime the window gets drawn this method is called. Depending on framerate it can happen multiple times for a single mouse state change.
					 * Checking the mouses button state would yield multiple passes counting a single click multiple times. Thats why the mouse click is
					 * handled via the event instead of the mouse state.
					 */
					if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
						//left click cycles through the things in the group selecting and zooming to each thing in sequence.
						//consuming the event
						Event.current.Use();
						//removing focus
						GUIUtility.keyboardControl = 0;
						//start cycling through a new group if it changed between clicks. 
						if ( currentGroup == null || currentGroup[0].def != g[0].def ) {
							currentGroup = g;
							currentGroupIndex = 0;
						}
						Thing thing = currentGroup[currentGroupIndex];
						currentGroupIndex = (currentGroupIndex+1)%currentGroup.Count();
						Find.CameraDriver.JumpToCurrentMapLoc(thing.PositionHeld);
						Find.Selector.ClearSelection();
						
						//either we select the thing directly if it is contained directly by the map or we cycle through its parent container till we find the one that is contained on the map
						if ( thing.ParentHolder is Map ) {
							if ( thing.ParentHolder == Find.CurrentMap )
								Find.Selector.Select(thing, true, true);
						} else {
							//e.g.: thing -> apparel -> pawn -> map
							IThingHolder holder = thing.ParentHolder;
							while ( holder != null && !(holder.ParentHolder is Map) ) {
								holder = holder.ParentHolder;
							}
							if ( holder != null && holder.ParentHolder == Find.CurrentMap ) 
								Find.Selector.Select(holder, true, true);
						}
					} else if (Event.current.type == EventType.MouseDown && Event.current.button == 1) {
						//right click selects all things in the group and zooms to the first thing. Things that are not directly on the map are not selected since they can not be treated equally.
						//consuming the event
						Event.current.Use();
						//removing focus
						GUIUtility.keyboardControl = 0;
						Find.Selector.ClearSelection();
						//the game wont select more than 80 things at once, that restrictions stays in place.
						int groupIndex = 0;
						foreach ( Thing t in g ) {
							if ( t.ParentHolder is Map && t.ParentHolder == Find.CurrentMap ) {
								//jump to the first valid thing in the group
								if ( groupIndex == 0 ) {
									Find.CameraDriver.JumpToCurrentMapLoc(t.PositionHeld);
								}
								Find.Selector.Select(t, groupIndex == 0, true);
								groupIndex++;
								if ( groupIndex > 80 ) break;
							}
						}
						//switch over to the main selection interaction menu and close this window - nah
						//Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Inspect, false);
					}
				}
				Widgets.Label(rect, g.Count() + " " + g[0].def.label);
				rect.y += 23f;
				indx++;
			}
			Widgets.EndScrollView();
			
			//removing the focus from the text field to allow keyboard input once the user no longer wants to type into the text field.
			if ( ( Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(inRect)) ) {
				GUIUtility.keyboardControl = 0;
			}
		}
		
		public override void ExtraOnGUI() {
			displayInspectActions();
		}
	}
}
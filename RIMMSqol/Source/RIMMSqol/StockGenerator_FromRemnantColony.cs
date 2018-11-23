/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 17.08.2017
 * Time: 02:36
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of StockGenerator_FromRemnantColony.
	/// </summary>
	public class StockGenerator_FromRemnantColony : StockGenerator
	{
		public StockGenerator_FromRemnantColony()
		{
		}
		
		public override IEnumerable<Thing> GenerateThings(int forTile) {
			Settlement settlement = Find.World.worldObjects.WorldObjectAt<Settlement>(forTile);
			if ( settlement != null ) {
				Map map = settlement.Map;
				//Only things that are in storage that accepts the thing will become tradebale. Zhis quickly removes the need to filter out walls, blueprints, dirt and such.
				List<Thing> allThings = map.listerThings.AllThings.Where(StoreUtility.IsInValidStorage).ToList();
				
				//generating things with the correct stack count for minable resources on the surface.
				foreach ( Thing t in map.listerThings.AllThings.Where(t=>t.def.mineable && t.def.building.isResourceRock) ) {
					Thing minedThing = ThingMaker.MakeThing(t.def.building.mineableThing, null);
					minedThing.stackCount = t.def.building.mineableYield;
					allThings.Add(minedThing);
				}
				
				//generating things with the correct stack count for minable resources under the surface.
				foreach ( IntVec3 cell in  map.AllCells ) {
					ThingDef td = map.deepResourceGrid.ThingDefAt(cell);
					if ( td != null ) {
						int count = map.deepResourceGrid.CountAt(cell);
						if ( count > 0 ) {
							Thing minedThing = ThingMaker.MakeThing(td, null);
							minedThing.stackCount = count;
							allThings.Add(minedThing);
						}
					}
				}
				
				//TODO: stone walls and slate pieces should be converted to bricks.
				
				return allThings;
			} else {
				return new List<Thing>();
			}
		}

		public override bool HandlesThingDef(ThingDef thingDef) {
			return thingDef.techLevel <= this.maxTechLevelBuy;
		}
	}
}

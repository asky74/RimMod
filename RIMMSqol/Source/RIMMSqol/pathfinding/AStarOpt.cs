/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.03.2018
 * Time: 10:26
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Priority_Queue;
using RimWorld;
using Verse;
using Verse.AI;

namespace RIMMSqol.pathfinding
{
	/// <summary>
	/// Description of AStarOpt.
	/// </summary>
	public class AStarOpt : PathfinderAlgorithm
	{
		protected class Step : FastPriorityQueueNode {
		    public IntVec3 current;
		    public int currentIndx;
		    public Step predecessor;
		    public int estimatedCostsToDestination;
		    public int costToReachThis;
		    public int runId = 0;
		    public bool closed;
		
		    public Step() { }
		    
		    public Step(IntVec3 current, int currentIndx, LocalTargetInfo destination, int costPerMoveDiagonal, int costPerMoveCardinal, int runId) {
		    	Reclaim(current,currentIndx,destination,costPerMoveDiagonal,costPerMoveCardinal,runId);
		    }
		    
		    public void Reclaim(IntVec3 current, int currentIndx, LocalTargetInfo destination, int costPerMoveDiagonal, int costPerMoveCardinal, int runId) {
		    	this.current = current;
		    	this.currentIndx = currentIndx;
		        this.closed = false;
		        this.runId = runId;
		        this.costToReachThis = 0;
		        this.predecessor = null;
		        estimatedCostsToDestination = GenMath.OctileDistance(Math.Abs(destination.Cell.x-current.x), Math.Abs(destination.Cell.z-current.z), costPerMoveCardinal, costPerMoveDiagonal);
		    }
		}
		protected class StepFactory {
			protected static Step[] usedStepsShared = new Step[0];
			protected Step[] usedSteps;
			protected static int runIdShared = int.MinValue;
			protected int runId;
			protected LocalTargetInfo destination;
			protected int costPerMoveDiagonal, costPerMoveCardinal, count;
			protected bool cheat;
			public void Reset(LocalTargetInfo destination, int costPerMoveCardinal, int costPerMoveDiagonal, bool cheat, int mapWidth, int mapHeight) {
				usedSteps = usedStepsShared;
				if ( usedSteps.Length < mapWidth * mapHeight ) {
					int oldSize = usedSteps.Length;
					Array.Resize<Step>(ref usedSteps, mapWidth * mapHeight);
					for ( int i = oldSize; i < usedSteps.Length; i++ ) {
						usedSteps[i] = new Step();
					}
					usedStepsShared = usedSteps;
				}
				runId = runIdShared;
				if ( runId++ == int.MinValue ) {
					for ( int i = 0; i < usedSteps.Length; i++ ) {
						usedSteps[i].runId = int.MinValue;
					}
				}
				runIdShared = runId;
				count = 0;
				this.destination = destination;
				this.costPerMoveCardinal = costPerMoveCardinal;
				this.costPerMoveDiagonal = costPerMoveDiagonal;
				this.cheat = cheat;
			}
			public Step getCachedOrNewStep(IntVec3 node, int nodeIndx) {
				Step s = usedSteps[nodeIndx];
				if ( s.runId != runId ) {
					s.Reclaim(node,nodeIndx,destination,costPerMoveDiagonal, costPerMoveCardinal, runId);
		            if ( cheat ) s.estimatedCostsToDestination *= 2;
		            count++;
		        }
		        return s;
		    }
			public int Count {
				get { return count; }
			}
		}
		/*protected class StepFactory {
			protected Dictionary<int,Step> usedSteps;
			protected LocalTargetInfo destination; 
			protected int costPerMoveDiagonal, costPerMoveCardinal, count, instancesUsed;
			protected bool cheat;
			protected Step[] instances;
			public StepFactory() {
				usedSteps = new Dictionary<int, Step>();
				instances = new Step[2000];
				for ( int i = 0; i < instances.Length; i++ ) {
					instances[i] = new Step();
				}
			}
			public void Reset(LocalTargetInfo destination, int costPerMoveCardinal, int costPerMoveDiagonal, bool cheat) {
				usedSteps.Clear();
				instancesUsed = 0;
				count = 0;
				this.destination = destination;
				this.costPerMoveCardinal = costPerMoveCardinal;
				this.costPerMoveDiagonal = costPerMoveDiagonal;
				this.cheat = cheat;
			}
			public Step getCachedOrNewStep(IntVec3 node, int nodeIndx) {
				Step s;
				if ( !usedSteps.TryGetValue(nodeIndx,out s) ) {
					if ( instancesUsed < instances.Length ) {
						s = instances[instancesUsed++];
						s.Reclaim(node,nodeIndx,destination,costPerMoveDiagonal, costPerMoveCardinal);
					} else s = new Step(node,nodeIndx,destination,costPerMoveDiagonal, costPerMoveCardinal);
		            if ( cheat ) s.estimatedCostsToDestination *= 2;
		            usedSteps.Add(nodeIndx, s);
		            count++;
		        }
		        return s;
		    }
			public int Count {
				get { return count; }
			}
		}*/
		protected const int limitForSearch = 160000;
		static protected int[] DirectionsShared = { 0, 1, 0, -1, 1, 1, -1, -1, -1, 0, 1, 0, -1, 1, 1, -1 };
		static protected IntVec3 invalidIntVec3Shared = new IntVec3(-1,-1,-1);
		static protected Priority_Queue.FastPriorityQueue<Step> openlistShared;
		
		protected StepFactory stepCache;
		protected Priority_Queue.FastPriorityQueue<Step> openlist;
		protected IntVec3[] neighbors = {invalidIntVec3Shared,invalidIntVec3Shared,invalidIntVec3Shared,invalidIntVec3Shared,invalidIntVec3Shared,invalidIntVec3Shared,invalidIntVec3Shared,invalidIntVec3Shared};
		protected Map map;
		protected CellIndices cellIndices;
		protected EdificeGrid edificeGrid;
		protected TraverseParms traverseParms;
		protected LocalTargetInfo dest;
		protected PathEndMode peMode;
		protected int costPerMoveDiagonal, costPerMoveCardinal;
		protected int[] pathGridArray;
		protected TerrainDef[] topGrid;
		protected List<Blueprint>[] blueprintGrid;
		protected PathGrid pathGrid;
		protected Pawn pawn;
		protected ByteGrid avoidGrid;
		protected Area allowedArea;
		protected bool drawPath, drafted;
		protected int maxDistanceBeforeCheating;
		protected int mapSizeX, mapSizeZ;
		protected bool dontPassWater, collidesWithPawns;
		protected int minTerrainCost;
		protected MapComponent_PathfinderDirections pathfinderDirections;
		
		public AStarOpt(string conf) {
			if ( conf != null ) {
				if ( !int.TryParse(conf.Trim(), out maxDistanceBeforeCheating) ) {
					maxDistanceBeforeCheating = int.MaxValue;
				}
			}
			Reload();
		}
		
		protected Step astar(IntVec3 start) {
			if ( stepCache == null ) stepCache = new StepFactory();
			stepCache.Reset(dest, costPerMoveCardinal, costPerMoveDiagonal, Math.Max(Math.Abs(start.x-dest.Cell.x),Math.Abs(start.z-dest.Cell.z)) > maxDistanceBeforeCheating, mapSizeX, mapSizeZ);
			openlist = openlistShared;
			if ( openlist == null ) {
				openlist = new Priority_Queue.FastPriorityQueue<Step>(limitForSearch+10);
				openlistShared = openlist;
			} else openlist.Clear();
			List<int> destCells = CalculateAllowedDestCells(map, dest, peMode, traverseParms);
	        
	        // Initialisierung der Open List, die Closed List ist noch leer
	        // (die Priorität bzw. der f Wert des Startknotens ist unerheblich)
	        Step firstStep = stepCache.getCachedOrNewStep(start,cellIndices.CellToIndex(start));
	        openlist.Enqueue(firstStep,0);
	        // diese Schleife wird durchlaufen bis entweder
	        // - die optimale Lösung gefunden wurde oder
	        // - feststeht, dass keine Lösung existiert
	        while ( openlist.Count != 0 ) {
	            // Knoten mit dem geringsten f Wert aus der Open List entfernen
	            Step currentStep = openlist.Dequeue();
	            // Wurde das Ziel gefunden?
	            if ( destCells.Contains(currentStep.currentIndx) )
	                return currentStep;
	            if ( stepCache.Count >= limitForSearch ) return null;
	            // Wenn das Ziel noch nicht gefunden wurde: Nachfolgeknoten
	            // des aktuellen Knotens auf die Open List setzen
	            expandNode(currentStep);
	            // der aktuelle Knoten ist nun abschließend untersucht
	            currentStep.closed = true;
	            //if ( drawPath ) map.debugDrawer.FlashCell(currentStep.current, 0.9f, "closed", 100);
	        }
	        // die Open List ist leer, es existiert kein Pfad zum Ziel
	        return null;
	    }
		
		protected bool BlocksDiagonalMovement(int index) {
			return !pathGrid.WalkableFast(index) || edificeGrid[index] is Building_Door;
		}
		
		static protected List<int> CalculateAllowedDestCells(Map map, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParms) {
			CellIndices cellIndices = map.cellIndices;
			List<int> allowedDestCells = new List<int>();
			CellRect targetArea;
			if (!dest.HasThing || peMode == PathEndMode.OnCell) {
				targetArea = CellRect.SingleCell(dest.Cell);
			} else {
				targetArea = dest.Thing.OccupiedRect();
			}
			if (peMode == PathEndMode.Touch) {
				targetArea = targetArea.ExpandedBy(1);
				allowedDestCells.AddRange(from c in targetArea.Cells select cellIndices.CellToIndex(c.x, c.z));
				int minX = targetArea.minX;
				int minZ = targetArea.minZ;
				int maxX = targetArea.maxX;
				int maxZ = targetArea.maxZ;
				if (!TouchPathEndModeUtility.IsCornerTouchAllowed(minX + 1, minZ + 1, minX + 1, minZ, minX, minZ + 1, map)) {
					allowedDestCells.Remove(cellIndices.CellToIndex(minX, minZ));
				}
				if (!TouchPathEndModeUtility.IsCornerTouchAllowed(minX + 1, maxZ - 1, minX + 1, maxZ, minX, maxZ - 1, map)) {
					allowedDestCells.Remove(cellIndices.CellToIndex(minX, maxZ));
				}
				if (!TouchPathEndModeUtility.IsCornerTouchAllowed(maxX - 1, maxZ - 1, maxX - 1, maxZ, maxX, maxZ - 1, map)) {
					allowedDestCells.Remove(cellIndices.CellToIndex(maxX, maxZ));
				}
				if (!TouchPathEndModeUtility.IsCornerTouchAllowed(maxX - 1, minZ + 1, maxX - 1, minZ, maxX, minZ + 1, map)) {
					allowedDestCells.Remove(cellIndices.CellToIndex(maxX, minZ));
				}
			} else {
				allowedDestCells.AddRange(from c in targetArea.Cells select cellIndices.CellToIndex(c.x, c.z));
			}
			
			return allowedDestCells;
		}
		
	    private void expandNode(Step currentNode) {
			bool previousNodeHasDoor, previousNodeIsPathCostIgnoreRepeater;
			Building previousBuilding = edificeGrid[currentNode.currentIndx];
        	if ( previousBuilding is Building_Door ) previousNodeHasDoor = true;
        	else previousNodeHasDoor = false;
        	
        	previousNodeIsPathCostIgnoreRepeater = false;
        	foreach ( ThingDef def in map.thingGrid.ThingsListAtFast(currentNode.currentIndx).Select(t=>t.def) ) {
        		if ( def.pathCostIgnoreRepeat ) {
        			previousNodeIsPathCostIgnoreRepeater = true;
        			break;
        		}
    		}
			
			//add all neighboring tiles but diagonal only if it is allowed, fixed size where we null out the after last entry if it is not full
			int currentIndice = currentNode.currentIndx;
			int currentX = currentNode.current.x;
			int currentZ = currentNode.current.z;
			int[] Directions = DirectionsShared;
			IntVec3 invalidIntVec3 = invalidIntVec3Shared;
			
			byte byteDirections;
			if ( pathfinderDirections != null ) {
				byteDirections = pathfinderDirections.GetDirections(currentNode.current);
			} else byteDirections = 0xFF;
			for ( int i = 0; i < 8; i++ ) {
				if ( byteDirections != 0xFF ) {
					//{ 0, 1, 0, -1,  1, 1, -1, -1, 
					// -1, 0, 1,  0, -1, 1,  1, -1 }
					switch (i) {
						case 2: if ( (byteDirections & 1) != 1 ) { neighbors[i] = invalidIntVec3; continue; } break;
						case 5: if ( (byteDirections & 2) != 2 ) { neighbors[i] = invalidIntVec3; continue; } break;
						case 1: if ( (byteDirections & 4) != 4 ) { neighbors[i] = invalidIntVec3; continue; } break;
						case 4: if ( (byteDirections & 8) != 8 ) { neighbors[i] = invalidIntVec3; continue; } break;
						case 0: if ( (byteDirections & 16) != 16 ) { neighbors[i] = invalidIntVec3; continue; } break;
						case 8: if ( (byteDirections & 32) != 32 ) { neighbors[i] = invalidIntVec3; continue; } break;
						case 3: if ( (byteDirections & 64) != 64 ) { neighbors[i] = invalidIntVec3; continue; } break;
						case 7: if ( (byteDirections & 128) != 128 ) { neighbors[i] = invalidIntVec3; continue; } break;
						default: neighbors[i] = invalidIntVec3; continue;
					}
				}
				int neighborX = currentX + Directions[i];
				int neighborZ = currentZ + Directions[i + 8];
				//check if we are within map bounds
				if ( neighborX >= mapSizeX || neighborZ >= mapSizeZ || neighborX < 0 || neighborZ < 0 ) {
					neighbors[i] = invalidIntVec3; continue;
				}
				//check if we have to skip diagonal movement due to adjacent tiles.
				if ( i > 3 ) {
					if ( Directions[i] == 1 ) {
						if ( BlocksDiagonalMovement(currentIndice + 1) ) {
							neighbors[i] = invalidIntVec3; continue;
						}
					} else {
						if ( BlocksDiagonalMovement(currentIndice - 1) ) {
							neighbors[i] = invalidIntVec3; continue;
						}
					}
					if ( Directions[i+8] == 1 ) {
						if ( BlocksDiagonalMovement(currentIndice + mapSizeX) ) {
							neighbors[i] = invalidIntVec3; continue;
						}
					} else {
						if ( BlocksDiagonalMovement(currentIndice - mapSizeX) ) {
							neighbors[i] = invalidIntVec3; continue;
						}
					}
				}
				//check if we can cross water tiles
				IntVec3 ngb = new IntVec3(neighborX,0,neighborZ);
				if (dontPassWater && ngb.GetTerrain(map).HasTag("Water")) {
					neighbors[i] = invalidIntVec3; continue;
				}
				neighbors[i] = ngb;
			}
			for ( int i = 0; i < neighbors.Count(); i++ ) {
				IntVec3 neighbor = neighbors[i];
				if ( neighbor == invalidIntVec3 ) continue;
				int neighborIndice = cellIndices.CellToIndex(neighbor);
				Step successor = stepCache.getCachedOrNewStep(neighbor,neighborIndice);
	            
				// wenn der Nachfolgeknoten bereits geschlossen wurde - tue nichts
	            if ( successor.closed ) continue;
	            
	            //can the tile be walked across?
				bool useTerrainCost = true;
				int costToDestroyInsteadOfTerrainCost = 0;
				if ( !pathGrid.WalkableFast(neighborIndice) ) {
					if (traverseParms.mode != TraverseMode.PassAllDestroyableThings) {
						continue;
					}
					useTerrainCost = false;
					costToDestroyInsteadOfTerrainCost += 70;
					Building building = edificeGrid[neighborIndice];
					if (building == null) {
						continue;
					}
					if (!PathFinder.IsDestroyable(building)) {
						continue;
					}
					costToDestroyInsteadOfTerrainCost += (int)((float)building.HitPoints * 0.11f);
				}
				
	            bool isUpdate = successor.costToReachThis > 0;
	            
	            int stepCosts = (i <= 3) ? costPerMoveCardinal : costPerMoveDiagonal;
	            Building building2 = edificeGrid[neighborIndice];
				if (building2 != null) {
					int buildingCost = PathFinder.GetBuildingCost(building2, traverseParms, pawn);
					if (buildingCost == Int32.MaxValue) {
						continue;
					}
					stepCosts += buildingCost;
				}
	            List<Blueprint> blueprints = this.blueprintGrid[neighborIndice];
				if (blueprints != null) {
	            	int maxBluprintCost = 0;
	            	blueprints.ForEach(bp=>maxBluprintCost = Math.Max(maxBluprintCost, PathFinder.GetBlueprintCost(bp, pawn)));
					if (maxBluprintCost == Int32.MaxValue) {
	            		continue;
					}
					stepCosts += maxBluprintCost;
				}
				if (useTerrainCost) {
	            	stepCosts += pathGridArray[neighborIndice] - minTerrainCost;
	            	//PathGrid uses cached static cost and does not account for directional costs
	            	//1. moving from a thing with pathCostIgnoreRepeat and a cost of at least 25 to a thing with pathCostIgnoreRepeat reduces the cost by the pathCost of the first thing
	            	//2. moving from door to door increases the cost by 45
	            	if ( previousNodeHasDoor && building2 != null && building2 is Building_Door ) {
	            		stepCosts += 45;
	            	}
	            	if ( previousNodeIsPathCostIgnoreRepeater ) {
	            		foreach ( ThingDef def in map.thingGrid.ThingsListAtFast(neighborIndice).Select(t=>t.def) ) {
	            			if ( def.pathCostIgnoreRepeat && def.pathCost >= 25 ) stepCosts -= def.pathCost; 
	            		}
	            	}
	            	//new in 1.0
	            	if (drafted) {
						stepCosts += topGrid[neighborIndice].extraDraftedPerceivedPathCost;
					} else {
						stepCosts += topGrid[neighborIndice].extraNonDraftedPerceivedPathCost;
					}
				} else {
					stepCosts += costToDestroyInsteadOfTerrainCost;
				}
				if (avoidGrid != null) {
					stepCosts += (int)(avoidGrid[neighborIndice] * 8);
				}
	            //Variation to default behaviour:
	            //When pathfinding from outside the allowed area back into the allowed area we ignore the malus for forbidden area to avoid ruining the performance for no good reason.
            	//We check the current position and if it is forbidden we do not apply a malus if it is allowed the current neighbor is tested.
	            if ( allowedArea != null && allowedArea[currentIndice] && !allowedArea[neighborIndice] ) {
            		stepCosts += 600;
	            }
				if (collidesWithPawns && PawnUtility.AnyPawnBlockingPathAt(neighbor, pawn, false, false, true)) {
					stepCosts += 175;
				}
	            
	            // g Wert für den neuen Weg berechnen: g Wert des Vorgängers plus
	            // die Kosten der gerade benutzten Kante
				int costToReachThisNeighbor = currentNode.costToReachThis + stepCosts;
	            // wenn der Nachfolgeknoten bereits auf der Open List ist,
	            // aber der neue Weg nicht besser ist als der alte - tue nichts
	            if ( isUpdate && successor.costToReachThis <= costToReachThisNeighbor ) continue;
	            successor.costToReachThis = costToReachThisNeighbor;
	            // Vorgängerzeiger setzen und g Wert merken
	            successor.predecessor = currentNode;
	            // f Wert des Knotens in der Open List aktualisieren
	            // bzw. Knoten mit f Wert in die Open List einfügen
	            if ( isUpdate ) {
	                openlist.UpdatePriority(successor,successor.costToReachThis + successor.estimatedCostsToDestination);
	                //if ( drawPath ) map.debugDrawer.FlashCell(successor.current, 0.1f, "updated", 100);
	            } else {
	            	openlist.Enqueue(successor,successor.costToReachThis + successor.estimatedCostsToDestination);
	            	if ( drawPath ) map.debugDrawer.FlashCell(successor.current, 0.22f, "opened", 100);
	            }
	        }
	    }
		
		public void Reload() {
			minTerrainCost = DefDatabase<TerrainDef>.AllDefsListForReading.Min(t=>t.passability != Traversability.Impassable ? t.pathCost : int.MaxValue);
		}
		
		public PawnPath findPath(Map map, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode, ByteGrid avoidGrid, Area allowedArea,
		                        int costsCardinal, int costsDiagonal) {
			this.map = map;
			this.cellIndices = this.map.cellIndices;
			this.edificeGrid = map.edificeGrid;
			this.traverseParms = traverseParms;
			this.dest = dest;
			this.peMode = peMode;
			this.costPerMoveCardinal = costsCardinal;
			this.costPerMoveDiagonal = costsDiagonal;
			this.avoidGrid = avoidGrid;
			this.allowedArea = allowedArea;
			this.pathGridArray = map.pathGrid.pathGrid;
			this.pathGrid = map.pathGrid;
			this.topGrid = map.terrainGrid.topGrid;
			this.pawn = traverseParms.pawn;
			this.drafted = pawn != null && pawn.Drafted;
			this.blueprintGrid = map.blueprintGrid.InnerArray;
			
			//Only colonists and tamed animals should respect restrictions.
			//Drafted pawns move unrestricted.
			//Some job types like firefighting should exclude the restrictions.
			List<string> exceptions = QOLMod.getSettings().pfRestrictionExcemptions;
			if ( this.pawn.Faction != null && this.pawn.Faction.IsPlayer && !this.pawn.Drafted && (exceptions == null || pawn.jobs.curJob == null || !exceptions.Contains(pawn.jobs.curJob.def.defName)) ) {
				this.pathfinderDirections = map.GetComponent<MapComponent_PathfinderDirections>();
			} else this.pathfinderDirections = null;
			this.drawPath = DebugViewSettings.drawPaths;
			this.mapSizeX = map.Size.x;
			this.mapSizeZ = map.Size.z;
			this.dontPassWater = traverseParms.mode == TraverseMode.NoPassClosedDoorsOrWater || traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater;
			this.collidesWithPawns = pawn != null && PawnUtility.ShouldCollideWithPawns(pawn);
			
			Step step = astar(start);
			if ( step == null ) return PawnPath.NotFound;
			
			PawnPath emptyPawnPath = map.pawnPathPool.GetEmptyPawnPath();
			int costs = step.costToReachThis;
			while ( step != null )
			{
				emptyPawnPath.AddNode(step.current);
				step = step.predecessor;
			}
			emptyPawnPath.SetupFound((float)costs, false);
			return emptyPawnPath;
		}
	}
}

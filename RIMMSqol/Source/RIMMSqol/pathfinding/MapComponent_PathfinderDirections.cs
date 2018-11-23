/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 08.04.2018
 * Time: 23:02
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
	/// Description of MapComponent_PathfinderDirections.
	/// </summary>
	public class MapComponent_PathfinderDirections : MapComponent
	{
		protected byte[] grid;
		protected bool redraw = true;
		protected Material material;
		protected Mesh[] meshes;
		
		public static Direction8Way ByteToDirection(byte b) {
			switch(b) {
 				case 1: return Direction8Way.North;
 				case 2: return Direction8Way.NorthEast;
 				case 4: return Direction8Way.East;
 				case 8: return Direction8Way.SouthEast;
 				case 16: return Direction8Way.South;
 				case 32: return Direction8Way.SouthWest;
 				case 64: return Direction8Way.West;
 				case 128: return Direction8Way.NorthWest;
         	}
			throw new Exception("no single direction found for byte: "+b);
		}
		
		public MapComponent_PathfinderDirections(Map map) : base(map)
		{
			
		}
		
		public void ToggleDirections(IntVec3 loc, IEnumerable<Direction8Way> directions, bool allowNoPermissionCell = true) {
			if ( !loc.InBounds(map) ) return;
			InitGrid();
			if ( directions == null ) { 
				grid[map.cellIndices.CellToIndex(loc)] = 0xFF;
			} else {
				byte b = 0;
				foreach ( Direction8Way dir in directions ) {
					switch(dir) {
						case Direction8Way.North: b |= 1; break;
						case Direction8Way.NorthEast: b |= 2; break;
						case Direction8Way.East: b |= 4; break;
						case Direction8Way.SouthEast: b |= 8; break;
						case Direction8Way.South: b |= 16; break;
						case Direction8Way.SouthWest: b |= 32; break;
						case Direction8Way.West: b |= 64; break;
						case Direction8Way.NorthWest: b |= 128; break;	
					}
				}
				int cellIndex = map.cellIndices.CellToIndex(loc);
				if ( grid[cellIndex] == 0xFF ) grid[cellIndex] = b; //when adding a direction to an unrestricted cell we dont merge we use only the new directions.
				else grid[cellIndex] ^= b;//XOR if previously set and submitted again it gets deleted, if previously not set and submitted it gets set, if not submitted no change.
				if ( !allowNoPermissionCell	&& grid[cellIndex] == 0 ) grid[cellIndex] = 0xFF;
			}
		}
		
		public void SetDirections(IntVec3 loc, byte directions) {
			if ( !loc.InBounds(map) ) return;
			InitGrid();
			grid[map.cellIndices.CellToIndex(loc)] = directions;
		}
		
		protected void InitGrid() {
			if ( grid == null ) grid = new byte[0];
			if ( grid.Length < map.Size.x * map.Size.z ) {
				int oldSize = grid.Length;
				Array.Resize<byte>(ref grid, map.Size.x * map.Size.z);
				for ( int i = oldSize; i < grid.Length; i++ ) {
					grid[i] = 0xFF;
				}
			}
		}
		
		public bool AllowsDirection(IntVec3 loc, Direction8Way direction) {
			if ( !loc.InBounds(map) ) return false;
			if ( grid == null ) return true;
			byte b = grid[map.cellIndices.CellToIndex(loc)];
			switch(direction) {
				case Direction8Way.North: return (b & 1) == 1;
				case Direction8Way.NorthEast: return (b & 2) == 2;
				case Direction8Way.East: return (b & 4) == 4;
				case Direction8Way.SouthEast: return (b & 8) == 8;
				case Direction8Way.South: return (b & 16) == 16;
				case Direction8Way.SouthWest: return (b & 32) == 32;
				case Direction8Way.West: return (b & 64) == 64;
				case Direction8Way.NorthWest: return (b & 128) == 128;
			}
			return false;
		}
		
		public byte GetDirections(IntVec3 loc) {
			if ( !loc.InBounds(map) ) return 0;
			if ( grid == null ) return 0xFF;
			return grid[map.cellIndices.CellToIndex(loc)];
		}
		
		public void MarkForDraw() {
            this.redraw = true;
		}
		
		protected Mesh createArrow(Direction8Way direction) {
			Mesh m = new Mesh();
			float y = Altitudes.AltitudeFor(AltitudeLayer.MapDataOverlay);
			const float xPixelSize = 1f / 15f;
			const float zPixelSize = 1f / 15f;
			
			List<Vector3> vertices = new List<Vector3>(7);
			List<Color> colors = new List<Color>(7);
			int[] triangles = new int[9];
			switch ( direction ) {
				case Direction8Way.South:
					m.name = "ArrowSouth";
					//stem
					vertices.Add(new Vector3(xPixelSize*7f,y,zPixelSize*5f));
					vertices.Add(new Vector3(xPixelSize*8f,y,zPixelSize*5f));
					vertices.Add(new Vector3(xPixelSize*8f,y,zPixelSize*3f));
					vertices.Add(new Vector3(xPixelSize*7f,y,zPixelSize*3f));
					//head
					vertices.Add(new Vector3(xPixelSize*5f,y,zPixelSize*3f));
					vertices.Add(new Vector3(xPixelSize*10f,y,zPixelSize*3f));
					vertices.Add(new Vector3(xPixelSize*7.5f,y,zPixelSize*0f));
					break;
				case Direction8Way.North:
					m.name = "ArrowNorth";
					//stem
					vertices.Add(new Vector3(xPixelSize*8f,y,zPixelSize*12f));
					vertices.Add(new Vector3(xPixelSize*8f,y,zPixelSize*10f));
					vertices.Add(new Vector3(xPixelSize*7f,y,zPixelSize*10f));
					vertices.Add(new Vector3(xPixelSize*7f,y,zPixelSize*12f));
					//head
					vertices.Add(new Vector3(xPixelSize*7.5f,y,zPixelSize*15f));
					vertices.Add(new Vector3(xPixelSize*10f,y,zPixelSize*12f));
					vertices.Add(new Vector3(xPixelSize*5f,y,zPixelSize*12f));
					break;
				case Direction8Way.East:
					m.name = "ArrowEast";
					//stem
					vertices.Add(new Vector3(xPixelSize*10f,y,zPixelSize*8f));
					vertices.Add(new Vector3(xPixelSize*12f,y,zPixelSize*8f));
					vertices.Add(new Vector3(xPixelSize*12f,y,zPixelSize*7f));
					vertices.Add(new Vector3(xPixelSize*10f,y,zPixelSize*7f));
					//head
					vertices.Add(new Vector3(xPixelSize*12f,y,zPixelSize*5f));
					vertices.Add(new Vector3(xPixelSize*12f,y,zPixelSize*10f));
					vertices.Add(new Vector3(xPixelSize*15f,y,zPixelSize*7.5f));
					break;
				case Direction8Way.West:
					m.name = "ArrowWest";
					//stem
					vertices.Add(new Vector3(xPixelSize*5f,y,zPixelSize*7f));
					vertices.Add(new Vector3(xPixelSize*3f,y,zPixelSize*7f));
					vertices.Add(new Vector3(xPixelSize*3f,y,zPixelSize*8f));
					vertices.Add(new Vector3(xPixelSize*5f,y,zPixelSize*8f));
					//head
					vertices.Add(new Vector3(xPixelSize*3f,y,zPixelSize*5f));
					vertices.Add(new Vector3(xPixelSize*0f,y,zPixelSize*7.5f));
					vertices.Add(new Vector3(xPixelSize*3f,y,zPixelSize*10f));
					break;
				
				case Direction8Way.NorthEast:
					m.name = "NorthEast";
					//stem
					vertices.Add(new Vector3(xPixelSize*10f,y,zPixelSize*11f));
					vertices.Add(new Vector3(xPixelSize*12f,y,zPixelSize*13f));
					vertices.Add(new Vector3(xPixelSize*13f,y,zPixelSize*12f));
					vertices.Add(new Vector3(xPixelSize*11f,y,zPixelSize*10f));
					//head
					vertices.Add(new Vector3(xPixelSize*11f,y,zPixelSize*14f));
					vertices.Add(new Vector3(xPixelSize*14f,y,zPixelSize*14f));
					vertices.Add(new Vector3(xPixelSize*14f,y,zPixelSize*11f));
					break;
				case Direction8Way.NorthWest:
					m.name = "NorthWest";
					//stem
					vertices.Add(new Vector3(xPixelSize*5f,y,zPixelSize*11f));
					vertices.Add(new Vector3(xPixelSize*4f,y,zPixelSize*10f));
					vertices.Add(new Vector3(xPixelSize*2f,y,zPixelSize*12f));
					vertices.Add(new Vector3(xPixelSize*3f,y,zPixelSize*13f));
					//head
					vertices.Add(new Vector3(xPixelSize*1f,y,zPixelSize*11f));
					vertices.Add(new Vector3(xPixelSize*1f,y,zPixelSize*14f));
					vertices.Add(new Vector3(xPixelSize*4f,y,zPixelSize*14f));
					break;
				case Direction8Way.SouthEast:
					m.name = "SouthEast";
					//stem
					vertices.Add(new Vector3(xPixelSize*10f,y,zPixelSize*4f));
					vertices.Add(new Vector3(xPixelSize*11f,y,zPixelSize*5f));
					vertices.Add(new Vector3(xPixelSize*13f,y,zPixelSize*3f));
					vertices.Add(new Vector3(xPixelSize*12f,y,zPixelSize*2f));
					//head
					vertices.Add(new Vector3(xPixelSize*11f,y,zPixelSize*1f));
					vertices.Add(new Vector3(xPixelSize*14f,y,zPixelSize*4f));
					vertices.Add(new Vector3(xPixelSize*14f,y,zPixelSize*1f));
					break;
				case Direction8Way.SouthWest:
					m.name = "SouthWest";
					//stem
					vertices.Add(new Vector3(xPixelSize*4f,y,zPixelSize*5f));
					vertices.Add(new Vector3(xPixelSize*5f,y,zPixelSize*4f));
					vertices.Add(new Vector3(xPixelSize*3f,y,zPixelSize*2f));
					vertices.Add(new Vector3(xPixelSize*2f,y,zPixelSize*3f));
					//head
					vertices.Add(new Vector3(xPixelSize*4f,y,zPixelSize*1f));
					vertices.Add(new Vector3(xPixelSize*1f,y,zPixelSize*1f));
					vertices.Add(new Vector3(xPixelSize*1f,y,zPixelSize*4f));
					break;
				default:
					m.name = "ArrowNorth";
					//stem
					vertices.Add(new Vector3(xPixelSize*7f,y,zPixelSize*10f));
					vertices.Add(new Vector3(xPixelSize*8f,y,zPixelSize*10f));
					vertices.Add(new Vector3(xPixelSize*8f,y,zPixelSize*12f));
					vertices.Add(new Vector3(xPixelSize*7f,y,zPixelSize*12f));
					//head
					vertices.Add(new Vector3(xPixelSize*5f,y,zPixelSize*12f));
					vertices.Add(new Vector3(xPixelSize*10f,y,zPixelSize*12f));
					vertices.Add(new Vector3(xPixelSize*7.5f,y,zPixelSize*15f));
					break;
			}
			
			triangles[0] = 0; triangles[1] = 1; triangles[2] = 2;
			triangles[3] = 0; triangles[4] = 2; triangles[5] = 3;
			triangles[6] = vertices.Count-3; triangles[7] = vertices.Count-2; triangles[8] = vertices.Count-1;					
			
			for ( int i = 0; i < vertices.Count; i++ ) {
				colors.Add(Color.black);
			}
			
			m.SetVertices(vertices);
			m.SetColors(colors);
			m.SetTriangles(triangles, 0);
			return m;
		}
		
		protected Mesh createCrossOut() {
			Mesh m = new Mesh();
			float y = Altitudes.AltitudeFor(AltitudeLayer.MapDataOverlay);
			const float xPixelSize = 1f / 15f;
			const float zPixelSize = 1f / 15f;
			
			m.name = "CrossOut";
			List<Vector3> vertices = new List<Vector3>(8);
			List<Color> colors = new List<Color>(8);
			int[] triangles = new int[12];
			
			vertices.Add(new Vector3(xPixelSize*3f,y,zPixelSize*1f));
			vertices.Add(new Vector3(xPixelSize*14f,y,zPixelSize*12f));
			vertices.Add(new Vector3(xPixelSize*12f,y,zPixelSize*14f));
			vertices.Add(new Vector3(xPixelSize*1f,y,zPixelSize*3f));
			triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
			triangles[3] = 0; triangles[4] = 3; triangles[5] = 2;
			
			vertices.Add(new Vector3(xPixelSize*1f,y,zPixelSize*12f));
			vertices.Add(new Vector3(xPixelSize*3f,y,zPixelSize*14f));
			vertices.Add(new Vector3(xPixelSize*14f,y,zPixelSize*3f));
			vertices.Add(new Vector3(xPixelSize*12f,y,zPixelSize*1f));
			triangles[6] = 4; triangles[7] = 5; triangles[8] = 6;
			triangles[9] = 4; triangles[10] = 6; triangles[11] = 7;
			
			for ( int i = 0; i < vertices.Count; i++ ) {
				colors.Add(Color.black);
			}
			
			m.SetVertices(vertices);
			m.SetColors(colors);
			m.SetTriangles(triangles, 0);
			return m;
		}
		
		public override void MapComponentUpdate()
        {
            if (this.redraw)
            {
            	if ( material == null ) {
	            	material = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0, 0, 0.5f, 1f), false);
	            	meshes = new Mesh[9];
	            	meshes[0] = createArrow(Direction8Way.North);
	            	meshes[1] = createArrow(Direction8Way.NorthEast);
	            	meshes[2] = createArrow(Direction8Way.East);
	            	meshes[3] = createArrow(Direction8Way.SouthEast);
	            	meshes[4] = createArrow(Direction8Way.South);
	            	meshes[5] = createArrow(Direction8Way.SouthWest);
	            	meshes[6] = createArrow(Direction8Way.West);
	            	meshes[7] = createArrow(Direction8Way.NorthWest);
	            	meshes[8] = createCrossOut();
            	}
            	if ( grid != null ) {
	            	//TODO skip to visible cell in the upper left and then go along the row until no longer visible, then next row and reset x and end if not visible
	            	Vector3 pos = new Vector3(-1,0,0);
	            	int index = -1;
	            	foreach(byte b in grid) {
	            		index++;
	            		pos.x += 1;
	            		if ( pos.x >= map.Size.x ) {
	            			pos.x = 0;
	            			pos.z += 1;
	            		}
	            		if ( b == 0xFF ) continue;
	            		if ( b == 0 ) {
	            			Graphics.DrawMesh(this.meshes[8], pos, Quaternion.identity, this.material, 0);
	            			continue;
	            		}
	            		if ( (b & 1) == 1 ) {
	            			Graphics.DrawMesh(this.meshes[0], pos, Quaternion.identity, this.material, 0);
	            		}
	            		if ( (b & 2) == 2 ) {
	            			Graphics.DrawMesh(this.meshes[1], pos, Quaternion.identity, this.material, 0);
	            		}
	            		if ( (b & 4) == 4 ) {
	            			Graphics.DrawMesh(this.meshes[2], pos, Quaternion.identity, this.material, 0);
	            		}
	            		if ( (b & 8) == 8 ) {
	            			Graphics.DrawMesh(this.meshes[3], pos, Quaternion.identity, this.material, 0);
	            		}
	            		if ( (b & 16) == 16 ) {
	            			Graphics.DrawMesh(this.meshes[4], pos, Quaternion.identity, this.material, 0);
	            		}
	            		if ( (b & 32) == 32 ) {
	            			Graphics.DrawMesh(this.meshes[5], pos, Quaternion.identity, this.material, 0);
	            		}
	            		if ( (b & 64) == 64 ) {
	            			Graphics.DrawMesh(this.meshes[6], pos, Quaternion.identity, this.material, 0);
	            		}
	            		if ( (b & 128) == 128 ) {
	            			Graphics.DrawMesh(this.meshes[7], pos, Quaternion.identity, this.material, 0);
	            		}
	            	}
            	}
                this.redraw = false;
            }
		}
		
		public override void ExposeData() {
			DataExposeUtility.ByteArray(ref this.grid, "grid");
		}

		/*
		public virtual void MapComponentTick()
		{
		}

		public virtual void MapComponentOnGUI()
		{
		}

		public virtual void FinalizeInit()
		{
		}

		public virtual void MapGenerated()
		{
		}

		public virtual void MapRemoved()
		{
		}*/
	}
}

/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 11.06.2017
 * Time: 04:07
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using Harmony;
using Harmony.ILCopying;
using RIMMSqol.genericSettings;
using RIMMSqol.remnantcolony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RIMMSqol
{
	/// <summary>
	/// Description of RPGWindow.
	/// </summary>
	public class RPGWindow : RimWorld.MainTabWindow
	{
		private Vector2 scrollPosition,scrollPositionModel, scrollPositionRelations;
		private float lastModelHeight = -1;
		private List<Pawn> pawns;
		private List<Trait> traits;
		private RPGModel model;
		private Dictionary<HairDef,Texture2D> hairCache;
		
		public RPGWindow()
		{
			forcePause = true;
		}
		
		public override void PreOpen() {
			base.PreOpen();
			//find all colonists and store them for drawing the window content
			pawns = new List<Pawn>();
			foreach ( Map m in Find.Maps ) {
				pawns.AddRange(m.mapPawns.FreeColonistsSpawned);
			}
			traits = null;
			model = null;
			hairCache = null;
			scrollPosition = new Vector2();
			scrollPositionModel = new Vector2();
			scrollPositionRelations = new Vector2();
			
			/*int currentTileID = Find.CurrentMap.Tile;
			World world = Find.World;
			WorldGrid grid = world.grid;
			List<int> neighborTiles = new List<int>();
			int neighborTile = -1;
			grid.GetTileNeighbors(currentTileID, neighborTiles);
			for (int i = 0; i < neighborTiles.Count; i++)
			{
				int num = neighborTiles[i];
				if ( !Find.WorldObjects.AnyWorldObjectAt(num) ) {
					neighborTile = num;
					break;
				}
			}
			if ( neighborTile != -1 ) {
				QOLCity worldObject = (QOLCity)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("WorldObjectQOLCity"));
				worldObject.defCity = DefDatabase<QOLCityDef>.GetNamed("QOLDefaultCity");
				worldObject.Tile = neighborTile;
				worldObject.SetFaction(Find.FactionManager.OfPlayer);
				Find.WorldObjects.Add(worldObject);
			}*/
		}
		
		public override void PostClose() {
			base.PostClose();
			pawns = null;
			model = null;
			hairCache = null;
			traits = null;
		}
		
		private void updateTraits(Pawn p) {
			//follows the code in: TraitSet.GainTrait(trait)
			if (p.workSettings != null) {
				p.workSettings.Notify_GainedTrait();
			}
			//p.story.Notify_TraitChanged();
			p.story.GetType().GetMethod("Notify_TraitChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(p.story,null);
			
			if (p.skills != null) {
				p.skills.Notify_SkillDisablesChanged();
			}
			if (!p.Dead && p.RaceProps.Humanlike) {
				p.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
			}
		}
		
		public override void DoWindowContents(Rect inRect)
		{
			if ( pawns == null ) return;
			
			Text.Font = GameFont.Medium;
			Rect rectTopBarResources = new Rect(inRect.x,inRect.y,inRect.width,Text.LineHeight);
			Rect rectLeftSideBarPortraits = new Rect(rectTopBarResources.x, rectTopBarResources.y + rectTopBarResources.height, 100, inRect.height-rectTopBarResources.height);
			Rect rectContentSelection = new Rect(rectLeftSideBarPortraits.x+rectLeftSideBarPortraits.width, rectTopBarResources.y + rectTopBarResources.height, 
			                                    inRect.width - rectLeftSideBarPortraits.width - 200, inRect.height - rectTopBarResources.height);
			Rect rectRightSideBarOptions = new Rect(rectContentSelection.x+rectContentSelection.width, rectTopBarResources.y + rectTopBarResources.height, 
			                                    200, inRect.height - rectTopBarResources.height);
			
			float portraitLineHeight = QOLMod.LineHeight(GameFont.Tiny)+70f+16f;
			Vector2 portraitSize = new Vector2(70f,70f);
			
			QOLModGameComponent gcPool = Current.Game.GetComponent<QOLModGameComponent>();
			Widgets.Label(rectTopBarResources, (model!=null?"Resources for minion: "+model.resources+" Resources in pool: "+model.resourcesPooled:"Select colonist"));
			if ( model != null && QOLMod.getCumPointPawnToPoolConversionFactor() > 0 ) {
				string text = "Transfer from minion to pool";
				Vector2 dim = Text.CalcSize(text);
				Rect r = new Rect(rectTopBarResources);
				r.width = dim.x + 20f; r.x = rectTopBarResources.x+rectTopBarResources.width-r.width;
				if ( Widgets.ButtonText(r, text) ) {
					Find.WindowStack.Add(new Dialog_MessageBox("Transfer all points from the current minion to the pool? "+Mathf.Floor(100f*QOLMod.getCumPointPawnToPoolConversionFactor())+"% will be retained!", "Transfer", delegate{
					                                           	if ( model != null ) {
						                                           	float points = 0;
																	RecordDef spentPointsDef = null;
																	foreach ( RecordDef def in DefDatabase<RecordDef>.AllDefsListForReading ) {
																		points += QOLMod.getCuMRecordPoints(model.pawn,def);
																		if ( def.defName.Equals("CuMSpent") ) {
																			spentPointsDef = def;
																		}
																	}
																	QOLModGameComponent pool = Current.Game.GetComponent<QOLModGameComponent>();
																	pool.pooledPoints += points * QOLMod.getCumPointPawnToPoolConversionFactor();
																	model.pawn.records.AddTo(spentPointsDef, points);
																	model = null;
					                                           	}
					                                           }, "Cancel", null, "Transfer points", false));
				}
			}
			
			Widgets.BeginScrollView(rectLeftSideBarPortraits, ref this.scrollPosition, new Rect(0, 0, rectLeftSideBarPortraits.width-GUI.skin.verticalScrollbar.fixedWidth - (float)GUI.skin.verticalScrollbar.margin.left, portraitLineHeight*pawns.Count), true);
			Rect portraitRect = new Rect(0,0,portraitSize.x,portraitSize.y);
			Rect labelRect = new Rect(0,portraitSize.y,portraitSize.x,portraitLineHeight - portraitSize.y);
			Text.Font = GameFont.Tiny;
			TextAnchor oldAnchor = Text.Anchor;
			Text.Anchor = TextAnchor.UpperCenter;
			foreach ( Pawn p in pawns ) {
				if (Mouse.IsOver(portraitRect)||Mouse.IsOver(labelRect)||(model != null && model.pawn == p))
				{
					Widgets.DrawHighlight(portraitRect);
					Widgets.DrawHighlight(labelRect);
				}
				if (Mouse.IsOver(portraitRect)||Mouse.IsOver(labelRect))
				{
					if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
						Event.current.Use();
						GUIUtility.keyboardControl = 0;
						model = new RPGModel(p);
						scrollPositionModel = new Vector2();
						scrollPositionRelations = new Vector2();
						lastModelHeight = -1;
					}
				}
				RenderTexture tx = PortraitsCache.Get(p, portraitSize);
				GUI.DrawTexture(portraitRect, tx);
				Widgets.Label(labelRect, p.LabelShort);
				portraitRect.y += portraitLineHeight;
				labelRect.y += portraitLineHeight;
			}
			Text.Anchor = oldAnchor;
			Widgets.EndScrollView();
			
			if ( model != null ) {
				float modelHeight = Mathf.Max(lastModelHeight,rectContentSelection.height);
				Rect modelRect = new Rect(rectContentSelection.x,rectContentSelection.y,rectContentSelection.width,rectContentSelection.height);
				float modelWidth = modelHeight > rectContentSelection.height ? modelRect.width-QOLMod.VerticalScrollbarWidth() : modelRect.width;
				Widgets.BeginScrollView(modelRect, ref scrollPositionModel, new Rect(0, 0, modelWidth, modelHeight), true);
				
				float y = DoBackstories(model,0,0);
				y = DoTraits(model,0,y);
				float y2 = DoGender(model,300.0f+QOLMod.LineHeight(GameFont.Tiny)+32.0f,0.0f);
				DoName(model,300.0f+QOLMod.LineHeight(GameFont.Tiny)+32.0f,y2);
				y = DoSkills(model,0,y);
				lastModelHeight = y;
				
				Text.Font = GameFont.Small;
				Rect rect = new Rect(16, y, modelWidth-16, Text.LineHeight);
				foreach ( string s in model.messages ) {
					Widgets.Label(rect, s);
					rect.y += rect.height;
					lastModelHeight += rect.height;
				}
				
				bool upgrade = Widgets.ButtonText(rect, "Upgrade (cost "+model.totalCost+")");
				if ( upgrade ) {
					if ( model.acceptChanges() ) {
						model = null;
					}
				}
				lastModelHeight += Text.LineHeight;
				
				Widgets.EndScrollView();
				
				if ( model != null ) {
					y = DoCosmetics(model,rectRightSideBarOptions);
					y = DoRelations(model,y,rectRightSideBarOptions);
				}
			}
		}
		
		private float DoGender(RPGModel m, float x, float y) {
			GameFont oldFont = Text.Font;
			Color oldColor = GUI.color;
			
			Text.Font = GameFont.Medium;
			Rect r = new Rect(x+16,y,300f-32.0f,Text.LineHeight);
			Widgets.Label(r,"Gender");
			r.y += Text.LineHeight;
			r.width = Materials.iconGenderMale.width;
			r.height = Materials.iconGenderMale.height;
			if ( Widgets.ButtonImage(r, Materials.iconGenderMale, m.gender == Gender.Male ? GenUI.MouseoverColor : Color.white, m.gender == Gender.Male ? GenUI.MouseoverColor : Color.white) ) {
				m.gender = Gender.Male;
			}
			r.x += r.width + 16f;
			if ( Widgets.ButtonImage(r, Materials.iconGenderFemale, m.gender == Gender.Female ? GenUI.MouseoverColor : Color.white, m.gender == Gender.Female ? GenUI.MouseoverColor : Color.white) ) {
				m.gender = Gender.Female;
			}
			r.x += r.width + 16f;
			if ( Widgets.ButtonImage(r, Materials.iconGenderGenderless, m.gender == Gender.None ? GenUI.MouseoverColor : Color.white, m.gender == Gender.None ? GenUI.MouseoverColor : Color.white) ) {
				m.gender = Gender.None;
			}
			r.y += r.height + 8f;
			
			GUI.color = oldColor;
			Text.Font = oldFont;
			return r.y;
		}
		
		private void DoName(RPGModel m, float x, float y) {
			const float widthLabel = 100.0f, widthInput = 200.0f;
			Text.Font = GameFont.Medium;
			Rect r = new Rect(x+16,y,widthLabel+widthInput-32.0f,Text.LineHeight);
			Widgets.Label(r,"Name");
			r.y += Text.LineHeight;
			
			Text.Font = GameFont.Tiny;
			r.height = Text.LineHeight;
			r.width = widthLabel - 16.0f;
			Widgets.Label(r,"Firstname");
			r.x += widthLabel; r.width = widthInput - 16.0f;
			m.firstname = Widgets.TextField(r,m.firstname);
			r.y += Text.LineHeight; r.x = x + 16.0f; r.width = widthLabel - 16.0f;
			Widgets.Label(r,"Nickname");
			r.x += widthLabel; r.width = widthInput - 16.0f;
			m.nickname = Widgets.TextField(r,m.nickname);
			r.y += Text.LineHeight; r.x = x + 16.0f; r.width = widthLabel - 16.0f;
			Widgets.Label(r,"Lastname");
			r.x += widthLabel; r.width = widthInput - 16.0f;
			m.lastname = Widgets.TextField(r,m.lastname);
		}
		
		private float DoCosmetics(RPGModel m, Rect inRect) {
			float y = inRect.y;
			
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(inRect.x+16,y,inRect.width-16,Text.LineHeight),"Cosmetics");
			y += Text.LineHeight;
			
			Text.Font = GameFont.Tiny;
			float halfHeight = Mathf.Ceil(Text.LineHeight / 2.0f);
			
			if ( Widgets.ButtonText(new Rect(inRect.x+16, y, inRect.width-16, Text.LineHeight),"Body type: "+m.bodyType) ) {
				Find.WindowStack.Add(new Dialog_SelectDef<BodyTypeDef>(bt=>{m.bodyType = bt; return bt != null;}, m.bodyTypes));
			}
			y += Text.LineHeight;
			
			if ( m.hasHair ) {
				y += halfHeight;
				/*GUI.DrawTexture(new Rect(inRect.x+16, y+halfHeight, inRect.width-16, Text.LineHeight*3-halfHeight), QOLMod.getSolidColorTexture(m.CurrentHairColor));
				m.CurrentHairColor.r = Widgets.HorizontalSlider(new Rect(inRect.x+32, y, inRect.width-48, Text.LineHeight), m.CurrentHairColor.r, 0, 1f, false, "Hair Color"); 
				y += Text.LineHeight;
				m.CurrentHairColor.g = Widgets.HorizontalSlider(new Rect(inRect.x+32, y, inRect.width-48, Text.LineHeight), m.CurrentHairColor.g, 0, 1f); 
				y += Text.LineHeight;
				m.CurrentHairColor.b = Widgets.HorizontalSlider(new Rect(inRect.x+32, y, inRect.width-48, Text.LineHeight), m.CurrentHairColor.b, 0, 1f); 
				y += Text.LineHeight;*/
				y = DoColorSelectionRandomPick(inRect,y,"Hair color","Randomize race appropiate colors or pick unrestricted.",m.CurrentHairColor, c=>m.CurrentHairColor = c,()=>{ m.GenerateCurrentHairColor(); return m.CurrentHairColor; });
				y += halfHeight;
				if ( m.hasSecondaryHairColorGenerator ) {
					y = DoColorSelectionRandomPick(inRect,y,"2nd Hair color","Randomize race appropiate colors or pick unrestricted.",m.CurrentSecondaryHairColor, c=>m.CurrentSecondaryHairColor = c,()=>{ m.GenerateCurrentSecondaryHairColor(); return m.CurrentSecondaryHairColor; });
					y += halfHeight;
				}
				
				if ( Widgets.ButtonText(new Rect(inRect.x+16, y, inRect.width-16, Text.LineHeight),"Hair width: "+m.crownType) ) {
					//TODO: possible alien crown types are defined as strings in AlienRace.AlienPartGenerator.aliencrowntypes. The AlienComp.crowntype holds the current value.
					Find.WindowStack.Add(new Dialog_Select<string>(ct=>{m.crownType = ct; return true;}, m.crownTypes, ct => ct));
				}
				y += Text.LineHeight;
			
				if ( Widgets.ButtonText(new Rect(inRect.x+16, y, inRect.width-16, Text.LineHeight),"Hairstyle: "+m.hairDef.label) ) {
					if ( hairCache == null ) {
						hairCache = new Dictionary<HairDef, Texture2D>();
						foreach ( HairDef hairDef in DefDatabase<HairDef>.AllDefsListForReading ) {
							hairCache.Add(hairDef,ContentFinder<Texture2D>.Get(hairDef.texPath + "_south", false));
						}
					}
					Find.WindowStack.Add(new Dialog_Select<HairDef>(def=>{m.hairDef = def; return true;},DefDatabase<HairDef>.AllDefsListForReading,def=>def.label,def=>hairCache[def],
					                                                new Dialog_FilterSelectHaircut(m.hairTraits)));
				}
				y += Text.LineHeight;
			}
			
			y += halfHeight;
			
			if( m.isAlienRace ) {
				if ( m.hasSkinColorGenerator ) {
					/*if ( Widgets.ButtonImage(new Rect(inRect.x+16, y+halfHeight, inRect.width-16, Text.LineHeight), QOLMod.getSolidColorTexture(m.CurrentSkinColor), Color.white, Color.white) ) {
						m.GenerateCurrentSkinColor();
					}
					y += Text.LineHeight+halfHeight;*/
					y = DoColorSelectionRandomPick(inRect,y,"Skin color","Randomize race appropiate colors or pick unrestricted.",m.CurrentSkinColor, c=>m.CurrentSkinColor = c,()=>{ m.GenerateCurrentSkinColor(); return m.CurrentSkinColor; });
					y += halfHeight;
				} else {
					/*GUI.DrawTexture(new Rect(inRect.x+16, y+halfHeight, inRect.width-16, Text.LineHeight), QOLMod.getSolidColorTexture(m.CurrentSkinColor));
					m.melanin = Widgets.HorizontalSlider(new Rect(inRect.x+32, y, inRect.width-48, Text.LineHeight), model.melanin, 0, 1f, false, "Skin Color");
					y += Text.LineHeight;*/
					y = DoColorSelectionRandomPick(inRect,y,"Skin color",null,m.CurrentSkinColor, c=>m.CurrentSkinColor = c,null, false, f=> { m.melanin = f; return m.CurrentSkinColor; }, m.melanin);
					y += halfHeight;
				}
				if ( m.hasSecondarySkinColorGenerator ) {
					/*if ( Widgets.ButtonImage(new Rect(inRect.x+16, y+halfHeight, inRect.width-16, Text.LineHeight), QOLMod.getSolidColorTexture(m.CurrentSecondarySkinColor), Color.white, Color.white) ) {
						m.GenerateCurrentSecondarySkinColor();
					}*/
					y = DoColorSelectionRandomPick(inRect,y,"2nd Skin color","Randomize race appropiate colors or pick unrestricted.",m.CurrentSecondarySkinColor, c=>m.CurrentSecondarySkinColor = c,()=>{ m.GenerateCurrentSecondarySkinColor(); return m.CurrentSecondarySkinColor; });
					y += halfHeight;
				}
				//y += Text.LineHeight;
			} else {
				/*GUI.DrawTexture(new Rect(inRect.x+16, y+halfHeight, inRect.width-16, Text.LineHeight), QOLMod.getSolidColorTexture(m.CurrentSkinColor));
				m.melanin = Widgets.HorizontalSlider(new Rect(inRect.x+32, y, inRect.width-48, Text.LineHeight), model.melanin, 0, 1f, false, "Skin Color");
				y += Text.LineHeight;*/
				y = DoColorSelectionRandomPick(inRect,y,"Skin color",null,m.CurrentSkinColor, c=>m.CurrentSkinColor = c,null, false, f=> { m.melanin = f; return m.CurrentSkinColor; }, m.melanin);
				y += halfHeight;
			}
			y += halfHeight;
			//PawnGraphicSet.headGraphic = GraphicDatabaseHeadRecords.GetHeadNamed(this.pawn.story.HeadGraphicPath, this.pawn.story.SkinColor);
			//PawnRenderer.RenderPawnInternal(pos, angle, true, Rot4.South, Rot4.South, this.CurRotDrawMode, true, false);
			
			if ( !m.isAlienRace ) {
				if ( Widgets.ButtonText(new Rect(inRect.x+16, y, inRect.width-16, Text.LineHeight),"Change Head") ) {
					Find.WindowStack.Add(new Dialog_Select<string>(headGraphicPath=>{
					                                               	if ( headGraphicPath != null ) m.headGraphicPath = headGraphicPath.Substring(0,headGraphicPath.Length-"_south".Length); 
					                                               	return headGraphicPath != null;
					                                               },
					                                               m.headGraphicPaths, headGraphicPath=>headGraphicPath, headGraphicPath=>ContentFinder<Texture2D>.Get(headGraphicPath, false)));
				}
				y += Text.LineHeight;
			}
			
			return y;
		}
		
		private float DoColorSelectionRandomPick(Rect inRect, float y, string label, string tooltip, Color currentColor, Action<Color> setColor, Func<Color> randomizeColor, bool allowRGB = true, Func<float,Color> linearMapping = null, float currentLinearValue = -1) {
			Rect buttonRect = new Rect(inRect.x+16, y, inRect.width-16, Text.LineHeight);
			if ( Widgets.ButtonImage(buttonRect, QOLMod.getSolidColorTexture(currentColor), Color.white, Color.white) ) {
				Find.WindowStack.Add(new Dialog_ColorSelectionRandomPick(currentColor,setColor,randomizeColor,allowRGB,linearMapping,currentLinearValue));
			}
			if ( !label.NullOrEmpty() ) {
				TextAnchor oldAnchor = Text.Anchor;
				Text.Anchor = TextAnchor.MiddleCenter;
				Color oldColor = GUI.color;
				GUI.color = ( currentColor.r + currentColor.g + currentColor.b ) / 3.0f > 0.5f ? Color.black : Color.white;
				
				Widgets.Label(buttonRect,label);
				Text.Anchor = oldAnchor;
				GUI.color = oldColor;
			}
			if ( !tooltip.NullOrEmpty() && Mouse.IsOver(buttonRect) ) {
				TooltipHandler.TipRegion(buttonRect, () => tooltip, 1);
			}
			
			y += Text.LineHeight;
			
			return y;
		}
		
		private class Dialog_ColorSelectionRandomPick : Window {
			protected Color currentColor;
			protected Func<Color> randomizeColor;
			protected Action<Color> setColor;
			protected float lineHeight, gapSize, tripleLineHeight, linearValue;
			protected bool allowRGB;
			protected Func<float,Color> linearMapping;
			
			public Dialog_ColorSelectionRandomPick(Color currentColor, Action<Color> setColor, Func<Color> randomizeColor, bool allowRGB = true, Func<float,Color> linearMapping = null, float currentLinearValue = -1) {
				this.randomizeColor = randomizeColor;
				this.setColor = setColor;
				this.currentColor = currentColor;
				this.linearValue = currentLinearValue;
				this.linearMapping = linearMapping;
				this.allowRGB = allowRGB;
				
				GameFont oldFont = Text.Font;
				Text.Font = GameFont.Small;
				lineHeight = Text.LineHeight; 
				gapSize = lineHeight / 8.0f; 
				tripleLineHeight = lineHeight * 3;
				Text.Font = oldFont;
				
				this.doCloseX = true;
				this.onlyOneOfTypeAllowed = true;
				this.absorbInputAroundWindow = true;
			}
			
			public override Vector2 InitialSize {
				get {
					//the number of visual squares is 1->3. Random, RGB, Linear mapping. Each block adds a gap plus tripleLineHeight.
					int squares = 0;
					if ( randomizeColor != null ) squares++;
					if ( allowRGB ) squares++;
					if ( linearMapping != null ) squares++;
					
					return new Vector2((float)Math.Min(UI.screenWidth,gapSize+(tripleLineHeight+gapSize)*squares+Margin*2), (float)Math.Min(UI.screenHeight,gapSize*2.0f+tripleLineHeight+Margin*2));
				}
			}
			
			public override void DoWindowContents(Rect inRect) {
				GameFont oldFont = Text.Font;
				Text.Font = GameFont.Small;
				
				Rect nextSquare = new Rect(inRect.x+gapSize, gapSize, tripleLineHeight, tripleLineHeight);
				if ( randomizeColor != null ) {
					if ( Widgets.ButtonImage(nextSquare, QOLMod.getSolidColorTexture(currentColor), Color.white, Color.white) ) {
						currentColor = randomizeColor();
					}
					GUI.DrawTexture(nextSquare.ContractedBy(gapSize),Materials.iconDice, ScaleMode.ScaleToFit);
					nextSquare = new Rect(nextSquare.x+nextSquare.width+gapSize, nextSquare.y, tripleLineHeight, tripleLineHeight);
				}
				
				if ( allowRGB ) {
					GUI.DrawTexture(nextSquare, QOLMod.getSolidColorTexture(currentColor));
					Rect rgbLine = new Rect(nextSquare); rgbLine.height = lineHeight;
					currentColor.r = Widgets.HorizontalSlider(rgbLine, currentColor.r, 0, 1f); 
					rgbLine.y += lineHeight;
					currentColor.g = Widgets.HorizontalSlider(rgbLine, currentColor.g, 0, 1f); 
					rgbLine.y += lineHeight;
					currentColor.b = Widgets.HorizontalSlider(rgbLine, currentColor.b, 0, 1f);
					nextSquare = new Rect(nextSquare.x+nextSquare.width+gapSize, nextSquare.y, tripleLineHeight, tripleLineHeight);
				}
				
				if ( linearMapping != null ) {
					GUI.DrawTexture(nextSquare, QOLMod.getSolidColorTexture(currentColor));
					Rect middleLine = new Rect(nextSquare); middleLine.height = lineHeight;
					middleLine.y += lineHeight;
					float linearValueNew = Widgets.HorizontalSlider(middleLine, linearValue, 0, 1f);
					if (Math.Abs(linearValueNew - linearValue) > float.Epsilon) {
						currentColor = linearMapping(linearValue);
						linearValue = linearValueNew;
					}
					nextSquare = new Rect(nextSquare.x+nextSquare.width+gapSize, nextSquare.y, tripleLineHeight, tripleLineHeight);
				}
				
				Text.Font = oldFont;
			}
			
			public override void PostClose() {
				base.PostClose();
				setColor(currentColor);
			}
		}
		
		private float DoRelations(RPGModel m, float y, Rect inRect) {
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(inRect.x+16,y,inRect.width-16,Text.LineHeight),"Relations");
			y += Text.LineHeight;
			
			//If we have more than 10 entries we need to use a scrollview with a vertical scrollbar
			Text.Font = GameFont.Tiny;
			float modelHeight = m.pawn.relations.DirectRelations.Count * Text.LineHeight;
			Rect scrollViewRect = new Rect(inRect.x+16,y,inRect.width-16,((float)Math.Min(m.pawn.relations.DirectRelations.Count,10)) * Text.LineHeight);
			float modelWidth = modelHeight > scrollViewRect.height ? scrollViewRect.width-QOLMod.VerticalScrollbarWidth() : scrollViewRect.width;
			Widgets.BeginScrollView(scrollViewRect, ref scrollPositionRelations, new Rect(0, 0, modelWidth, modelHeight), true);
			DirectPawnRelation relationToRemove = null;
			float yScrollView = 0;
			foreach ( DirectPawnRelation r in m.pawn.relations.DirectRelations ) {
				if ( !r.def.implied ) {
					Rect rect = new Rect(0,yScrollView,modelWidth - 16f,Text.LineHeight);
					Widgets.Label(rect,r.def.GetGenderSpecificLabel(m.pawn)+" - "+r.otherPawn.LabelShort);
					rect.x += rect.width; rect.width = 16;
					GUI.DrawTexture(rect,Widgets.CheckboxOffTex, ScaleMode.ScaleToFit);
					if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(rect)) {
						relationToRemove = r;
					}
					yScrollView += Text.LineHeight;
				}
			}
			if ( relationToRemove != null ) {
				RemoveRelation(m.pawn,relationToRemove);
			}
			Widgets.EndScrollView();
			
			if ( Widgets.ButtonText(new Rect(inRect.x+16,scrollViewRect.yMax,inRect.width-16,Text.LineHeight),"Add Relation") ) {
				AddRelation(m.pawn);
			}
			
			return scrollViewRect.yMax + Text.LineHeight;
		}
		
		protected class Dialog_FilterSelectHaircut : IDialog_Filter<HairDef> {
			protected bool allowAllHaircuts = false;
			protected string filter;
			protected List<string> allowedHairTags;
			
			public Dialog_FilterSelectHaircut(List<string> allowedHairTags) {
				this.allowedHairTags = allowedHairTags;
			}
			
			public Rect DoFilterWindowContent(Rect rect) {
				const float lineHeight = 30f, horizontalGap = 8f;
				
				this.filter = Widgets.TextField(new Rect(0f, 0f, 200f, lineHeight), this.filter);
				
				Rect symbolRect = new Rect(200f + horizontalGap,0f,lineHeight,lineHeight);
				if ( Widgets.ButtonImage(symbolRect,Materials.iconWarning,!allowAllHaircuts ? Color.red : Color.white,!allowAllHaircuts ? Color.red : Color.white) ) {
					allowAllHaircuts = !allowAllHaircuts;
				}
				
				Rect r = new Rect(rect); r.yMin += 35f;
				return r;
			}
			
			public bool FilterAllows(string label, HairDef value) {
				if ( !allowAllHaircuts && !allowedHairTags.SharesElementWith(value.hairTags) ) {
					return false;
				}
				return this.filter.NullOrEmpty() || label.NullOrEmpty() || label.IndexOf(this.filter, StringComparison.OrdinalIgnoreCase) >= 0;
			}
			
			public IOrderedEnumerable<HairDef> Sort(IOrderedEnumerable<HairDef> items) {
				return items;
			}
		}
		
		protected class Dialog_FilterSelectPawnForRelation : IDialog_Filter<Pawn> {
			protected bool reorder = true, filterInterspecies, filterAnimals;
			protected Faction filterFaction = null;
			protected string filter, title;
			protected PawnRelationDef relation;
			protected Pawn forPawn;
			public Dialog_FilterSelectPawnForRelation(Pawn forPawn, PawnRelationDef relation) {
				this.forPawn = forPawn;
				this.relation = relation;
				this.title = forPawn.LabelShort+(forPawn.def != null ? "("+forPawn.def.label+")" : "")+(forPawn.ageTracker != null ? " "+forPawn.ageTracker.AgeNumberString : "")+(forPawn.Faction != null ? " "+forPawn.Faction.Name : "");
			}
			public Rect DoFilterWindowContent(Rect rect) {
				GameFont oldFont = Text.Font;
				Text.Font = GameFont.Small;
				const float verticalGap = 5f, lineHeight = 30f, horizontalGap = 8f;
				Rect r = new Rect(rect);
				
				Widgets.Label(new Rect(r.x, r.y, 400f, lineHeight),title);
				r.yMin += lineHeight + verticalGap;
				
				this.filter = Widgets.TextField(new Rect(r.x, r.y, 200f, lineHeight), this.filter);
				
				Color oldColor = GUI.color;
				Rect symbolRect = new Rect(200f+horizontalGap,r.y,lineHeight,lineHeight);
				
				if ( Widgets.ButtonImage(symbolRect,Materials.iconAlien,filterInterspecies ? Color.red : Color.white,filterInterspecies ? Color.red : Color.white) ) {
					filterInterspecies = !filterInterspecies;
					reorder = true;
				}
				if ( Mouse.IsOver(symbolRect) ) {
					TooltipHandler.TipRegion(symbolRect, () => "Filter alien species, including animals.", 1);
				}
				symbolRect.x += symbolRect.width + horizontalGap;
				
				if ( Widgets.ButtonImage(symbolRect,Materials.skillAnimals,filterAnimals ? Color.red : Color.white,filterAnimals ? Color.red : Color.white) ) {
					filterAnimals = !filterAnimals;
					reorder = true;
				}
				if ( Mouse.IsOver(symbolRect) ) {
					TooltipHandler.TipRegion(symbolRect, () => "Filter animals.", 1);
				}
				symbolRect.x += symbolRect.width + horizontalGap;
				
				symbolRect.width = 300f;
				GUI.color = filterFaction == null || !filterFaction.HostileTo(Faction.OfPlayer) ? oldColor : Color.red;
				if ( Widgets.ButtonText(symbolRect,filterFaction == null ? "All Factions" : filterFaction.IsPlayer ? "Player Faction" : filterFaction.Name) ) {
					foreach ( Faction f in Find.FactionManager.AllFactionsInViewOrder ) {
						if ( f == filterFaction ) {
							filterFaction = null;
						} else if ( filterFaction == null ) {
							filterFaction = f;
						}
					}
				}
				symbolRect.x += symbolRect.width + horizontalGap;
				r.yMin += lineHeight + verticalGap;
				
				GUI.color = oldColor;
				Text.Font = oldFont;
				return r;
			}
			public bool FilterAllows(string label, Pawn value)
			{
				//No relation to itself
				if (forPawn == value) {
					return false;
				}
				//No relation if it already exists
				if (forPawn.relations.DirectRelationExists(relation, value)) {
					return false;
				}
				//check faction matches filter
				if (filterFaction != null && value.Faction != filterFaction ) {
					return false;
				}
				//check if animals are allowed
				if (filterAnimals && value.RaceProps.Animal ) {
					return false;
				}
				//check if interspecies is allowed
				if (filterInterspecies && value.def != forPawn.def) {
					return false;
				}
				
				//special callback filter that modders can register for their relations. For example the parent relation requires the parent to be born before the child.
				//children with the same mother must be 9 month apart or have the same birthday(maybe gastation period but then they might use eggs).
				
				return this.filter.NullOrEmpty() || label.NullOrEmpty() || label.IndexOf(this.filter, StringComparison.OrdinalIgnoreCase) >= 0;
			}
			public IOrderedEnumerable<Pawn> Sort(IOrderedEnumerable<Pawn> items)
			{
				if ( !reorder ) return items;
				reorder = false;
				return items.OrderByDescending(p=>p.def==forPawn.def).ThenBy(p=>p.IsWorldPawn());
			}
		}
		
		protected void AddRelation(Pawn p) {
			Func<PawnRelationDef,bool> callbackSelectPawnRelationDef = selectedRelation => {
				Func<Pawn,bool> callbackSelectPawn = selectedPawn => {
					p.relations.AddDirectRelation(selectedRelation, selectedPawn);
					return true;
				};
				IEnumerable<Pawn> validPawns = from x in PawnsFinder.AllMapsWorldAndTemporary_Alive where x.RaceProps.IsFlesh orderby x.def == p.def descending, x.IsWorldPawn() select x;
				//label should display name, race, faction, age biological and chronological, opinion
				Func<Pawn,Texture2D> iconProducer = cp => { 
					int opinion = p.relations.OpinionOf(cp);
					if ( opinion < -50 ) return Materials.iconOpinionBad;
					if ( opinion > 50 ) return Materials.iconOpinionGood;
					return Materials.iconOpinionNeutral;
				};
				Func<Pawn,string> labelProducer = cp => cp.LabelShort+(cp.def != null ? "("+cp.def.label+")" : "")+(cp.ageTracker != null ? " "+cp.ageTracker.AgeNumberString : "")+(cp.Faction != null ? " "+cp.Faction.Name : "");
				Func<Pawn,string> tooltipProducer = cp => 
					p.LabelShort + " opinion of " + cp.LabelShort + ": " + p.relations.OpinionOf(cp) + System.Environment.NewLine +
					cp.LabelShort + " opinion of " + p.LabelShort + ": " + cp.relations.OpinionOf(p) + System.Environment.NewLine +
					"Biological age: " + cp.ageTracker.AgeBiologicalYears + " Chronological age: " + cp.ageTracker.AgeChronologicalYears + System.Environment.NewLine +
					"Race: " + cp.def.label + " Faction: " + (cp.Faction != null && cp.Faction.Name != null ? cp.Faction.Name : "none");
				Find.WindowStack.Add(new Dialog_Select<Pawn>(callbackSelectPawn,validPawns,
				                                             labelProducer,iconProducer,new Dialog_FilterSelectPawnForRelation(p,selectedRelation),tooltipProducer){numOfColumns=3});
				return true;
			};
			Find.WindowStack.Add(new Dialog_SelectDef<PawnRelationDef>(callbackSelectPawnRelationDef,DefDatabase<PawnRelationDef>.AllDefs.Where(r=>!r.implied)));
			
			
			/*List<DebugMenuOption> list2 = new List<DebugMenuOption>();
			foreach (PawnRelationDef current in DefDatabase<PawnRelationDef>.AllDefs) {
				if (!current.implied) {
					PawnRelationDef defLocal = current;
					list2.Add(new DebugMenuOption(defLocal.defName, DebugMenuOptionMode.Action, delegate {
						List<DebugMenuOption> list4 = new List<DebugMenuOption>();
						IOrderedEnumerable<Pawn> relationTargets = from x in PawnsFinder.AllMapsWorldAndTemporary_Alive where x.RaceProps.IsFlesh orderby x.def == p.def descending, x.IsWorldPawn() select x;
						foreach (Pawn current2 in relationTargets) {
							if (p != current2) {
								if (!defLocal.familyByBloodRelation || current2.def == p.def) {
									if (!p.relations.DirectRelationExists(defLocal, current2)) {
										Pawn otherLocal = current2;
										list4.Add(new DebugMenuOption(otherLocal.LabelShort + " (" + otherLocal.KindLabel + ")", DebugMenuOptionMode.Action, delegate {
											p.relations.AddDirectRelation(defLocal, otherLocal);
										}));
									}
								}
							}
						}
						Find.WindowStack.Add(new Dialog_DebugOptionListLister(list4));
					}));
				}
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list2));*/
		}
		
		private void RemoveRelation(Pawn p, DirectPawnRelation r) {
			if ( p != null && r != null && p.relations != null ) p.relations.RemoveDirectRelation(r);
		}
		
		private float DoBackstories(RPGModel m, float x, float y) {
			Text.Font = GameFont.Medium;
			string title = "Backstory";
			Vector2 titleSize = Text.CalcSize(title);
			Widgets.Label(new Rect(x+16,y,titleSize.x,titleSize.y),title);
			y += titleSize.y;
			Text.Font = GameFont.Tiny;
			DoBackstory(m,x,y,BackstorySlot.Childhood);
			DoBackstory(m,x,y+=Text.LineHeight,BackstorySlot.Adulthood);
			return y+Text.LineHeight;
		}
		
		private void DoBackstory(RPGModel m, float x, float y, BackstorySlot bss) {
			Rect rectBackgroundLabel = new Rect(x+16,y,100,Text.LineHeight);
			Rect rectBackgroundEntry = new Rect(rectBackgroundLabel.x+rectBackgroundLabel.width,rectBackgroundLabel.y,200,rectBackgroundLabel.height);
			Rect rectBackgroundRemove = new Rect(rectBackgroundEntry.x+rectBackgroundEntry.width,rectBackgroundEntry.y,rectBackgroundEntry.height,rectBackgroundEntry.height);
			Rect rectBackgroundAdd = new Rect(rectBackgroundEntry.x,rectBackgroundEntry.y,rectBackgroundEntry.width+rectBackgroundRemove.width,rectBackgroundEntry.height);
			
			Widgets.Label(rectBackgroundLabel, bss.ToString());
			Backstory bs = m.backstories[bss];
			if ( bs != null ) {
				Widgets.Label(rectBackgroundEntry,bs.title);
				bool remove = Widgets.ButtonText(rectBackgroundRemove, "-");
				if (Mouse.IsOver(rectBackgroundEntry)) {
					TooltipHandler.TipRegion(rectBackgroundEntry, () => bs.FullDescriptionFor(m.pawn), 1);
				} else if (remove) {
					m.apply(new RPGUpgradeBackstory(bss,null));
				}
			} else {
				//if the backstory slot is adulthood the pawn must be  >=20f years old.
				if (bss == BackstorySlot.Childhood || m.pawn.ageTracker.AgeBiologicalYearsFloat>=20f) {
					if (Widgets.ButtonText(rectBackgroundAdd, "+")) {
						Find.WindowStack.Add(new Dialog_Select<Backstory>(
							selectedBackstory => { m.apply(new RPGUpgradeBackstory(bss,selectedBackstory)); return true; },
							BackstoryDatabase.allBackstories.Select(p=>p.Value).Where(bsfilter=>bsfilter.slot == bss),
                          	currentBackstory => currentBackstory.title,
                          	new Dialog_FilterBackstories(m.pawn),currentBackstory => currentBackstory.FullDescriptionFor(m.pawn)));
					}
				} else {
					Widgets.Label(rectBackgroundAdd, "Too young");
				}
			}
		}
		
		private float DoTraits(RPGModel m, float x, float y) {
			Text.Font = GameFont.Medium;
			string title = "Traits";
			Vector2 titleSize = Text.CalcSize(title);
			Widgets.Label(new Rect(x+16,y,titleSize.x,titleSize.y),title);
			y += titleSize.y;
			Text.Font = GameFont.Tiny;
			foreach(Trait t in m.traits.ToList()) {//iterate over copy since we can modify the list during DoTrait
				DoTrait(m,t,x,y);
				y += Text.LineHeight;
			}
			DoTrait(m,null,x,y);
			return y + Text.LineHeight;
		}
		
		private void DoTrait(RPGModel m, Trait t, float x, float y) {
			Rect rectBackgroundEntry = new Rect(x+16,y,300,Text.LineHeight);
			Rect rectBackgroundRemove = new Rect(rectBackgroundEntry.x+rectBackgroundEntry.width,rectBackgroundEntry.y,rectBackgroundEntry.height,rectBackgroundEntry.height);
			Rect rectBackgroundAdd = new Rect(rectBackgroundEntry.x,rectBackgroundEntry.y,rectBackgroundEntry.width+rectBackgroundRemove.width,rectBackgroundEntry.height);
			
			if ( t != null ) {
				Widgets.Label(rectBackgroundEntry,t.Label);
				bool remove = Widgets.ButtonText(rectBackgroundRemove, "-");
				if (Mouse.IsOver(rectBackgroundEntry)) {
					TooltipHandler.TipRegion(rectBackgroundEntry, () => t.TipString(m.pawn), 1);
				} else if (remove) {
					m.apply(new RPGUpgradeTrait(null,t));
				}
			} else {
				bool add = Widgets.ButtonText(rectBackgroundAdd, "+", true, false, true);
				if (add) {
					//bring up a list from which to select
					if ( traits == null ) {
						traits = (from TraitDef tDef in DefDatabase<TraitDef>.AllDefsListForReading from TraitDegreeData degree in tDef.degreeDatas.DefaultIfEmpty(new TraitDegreeData()) 
						          select new Trait(tDef,degree.degree,false)).OrderBy(e=>e.Label).ToList();
					}
					Find.WindowStack.Add(new Dialog_Select<Trait>(trait=>{m.apply(new RPGUpgradeTrait(trait,null)); return true;}, traits, trait=>trait.Label, new TraitSelectionFilter(m), trait=>trait.TipString(m.pawn)));
				}
			}
		}
		
		private class TraitSelectionFilter : IDialog_Filter<Trait> {
			protected string filter;
			protected bool allowConflictingTraits = true;
			protected IEnumerable<TraitDef> existingTraits;
			protected IEnumerable<TraitDef> conflictingTraits;
			public TraitSelectionFilter(RPGModel m) {
				existingTraits = (from Trait t in m.traits group t by t.def into something select something.First().def);
				
				List<WorkTypeDef> disabledWorkTypes = new List<WorkTypeDef>();
				WorkTags disabledWorkTags = WorkTags.None;
				foreach(Backstory bs in m.backstories.Values) {
					if ( bs != null ) {
						disabledWorkTags |= bs.workDisables;
						disabledWorkTypes.AddRange(from WorkTypeDef wtd in bs.DisabledWorkTypes where !disabledWorkTypes.Contains(wtd) select wtd);
					}
				}
				foreach(Trait t in m.traits) {
					disabledWorkTags |= t.def.disabledWorkTags;
					disabledWorkTypes.AddRange(from WorkTypeDef wtd in t.GetDisabledWorkTypes() where !disabledWorkTypes.Contains(wtd) select wtd);
				}
				conflictingTraits = (from t in DefDatabase<TraitDef>.AllDefsListForReading where (disabledWorkTags & t.requiredWorkTags) != WorkTags.None ||
				                         t.requiredWorkTypes.Any(disabledWorkTypes.Contains) select t);
			}

			public Rect DoFilterWindowContent(Rect rect)
			{
				GameFont oldFont = Text.Font;
				Color oldColor = GUI.color;
				Text.Font = GameFont.Small;
				const float lineHeight = 30f;
				
				this.filter = Widgets.TextField(new Rect(0f, 0f, 200f, lineHeight), this.filter);
				Color c = allowConflictingTraits ? Color.yellow : Color.green;
				Rect symbolRect = new Rect(216f,0f,lineHeight,lineHeight);
				if ( Widgets.ButtonImage(symbolRect, Materials.iconWarning, c, c) )
					allowConflictingTraits = !allowConflictingTraits;
				if ( Mouse.IsOver(symbolRect) ) {
					TooltipHandler.TipRegion(symbolRect, () => "Filter traits that generate warnings due to conflicts.", 1);
				}
				
				GUI.color = oldColor;
				Text.Font = oldFont;
				Rect r = new Rect(rect); r.yMin += lineHeight+5f;
				return r;
			}
			
			public bool FilterAllows(string label, Trait value)
			{
				if ( existingTraits.Contains(value.def) ) return false;
				if  ( !allowConflictingTraits && conflictingTraits.Contains(value.def) ) return false;
				
				return this.filter.NullOrEmpty() || label.NullOrEmpty() || label.IndexOf(this.filter, StringComparison.OrdinalIgnoreCase) >= 0;
			}
			
			public IOrderedEnumerable<Trait> Sort(IOrderedEnumerable<Trait> items) {
				return items;
			}
		}
		
		private float DoSkills(RPGModel m, float x, float y) {
			Text.Font = GameFont.Medium;
			string title = "Skills";
			Vector2 titleSize = Text.CalcSize(title);
			Widgets.Label(new Rect(x+16,y,titleSize.x,titleSize.y),title);
			y += titleSize.y;
			Text.Font = GameFont.Tiny;
			
			foreach(RPGModelSkill s in m.skills) { 
				DoSkill(m, s, x, y);
				y += Text.LineHeight;
			}
			return y + Text.LineHeight;
		}
		
		private void DoSkill(RPGModel m, RPGModelSkill s, float x, float y) {
			Rect rectBackgroundEntry = new Rect(x+16,y,300-Text.LineHeight*2,Text.LineHeight);
			Rect rectBackgroundLvl = new Rect(rectBackgroundEntry.x+rectBackgroundEntry.width,rectBackgroundEntry.y,300-rectBackgroundEntry.width,rectBackgroundEntry.height);
			Rect rectBackgroundPassion = new Rect(rectBackgroundLvl.x+rectBackgroundLvl.width,rectBackgroundEntry.y,rectBackgroundEntry.height,rectBackgroundEntry.height);
			
			Color origColor = GUI.contentColor;
			if ( m.isDisabled(s.def) ) GUI.contentColor = Color.red;
			Widgets.Label(rectBackgroundEntry,s.def.label);
			Widgets.Label(rectBackgroundLvl,s.level.ToString());
			GUI.contentColor = origColor;
			
			//displaying it as a three button choice group is an option
			bool upgradePassion = false;
			switch (s.passion) {
				case Passion.Major: upgradePassion = Widgets.ButtonImage(rectBackgroundPassion,Materials.iconPassionMajor); break;
				case Passion.Minor: upgradePassion = Widgets.ButtonImage(rectBackgroundPassion,Materials.iconPassionMinor); break;
				case Passion.None: upgradePassion = Widgets.ButtonImage(rectBackgroundPassion,Materials.iconPassionNone); break;
			}
			
			if (upgradePassion) {
				switch (s.passion) {
					case Passion.Major: model.apply(new RPGUpgradePassion(s.def, Passion.None)); break;
					case Passion.Minor: model.apply(new RPGUpgradePassion(s.def, Passion.Major)); break;
					case Passion.None: model.apply(new RPGUpgradePassion(s.def, Passion.Minor)); break;
				}
			}
			
			y += Text.LineHeight;
		}
		
		private interface IRPGUpgrade {}
		
		private class RPGUpgradeBackstory : IRPGUpgrade {
			public BackstorySlot slot;
			public Backstory value;
			public RPGUpgradeBackstory(BackstorySlot slot, Backstory value) {
				this.slot = slot;
				this.value = value;
			}
		}
		
		private class RPGUpgradeTrait : IRPGUpgrade {
			public Trait add;
			public Trait remove;
			public RPGUpgradeTrait(Trait add, Trait remove) {
				this.add = add;
				this.remove = remove;
			}
		}
		
		private class RPGUpgradePassion : IRPGUpgrade {
			public SkillDef def;
			public Passion passion;
			public RPGUpgradePassion(SkillDef def, Passion passion) {
				this.def = def;
				this.passion = passion;
			}
		}
		
		private class AlienPartGeneratorConnector {
			public List<BodyTypeDef> alienbodytypes;
			public List<string> aliencrowntypes;
			public ColorGenerator alienskincolorgen, alienskinsecondcolorgen, alienhaircolorgen, alienhairsecondcolorgen;
			public bool useSkincolorForHair, useGenderedHeads;
			
			public AlienPartGeneratorConnector(object o) {
				alienbodytypes = (List<BodyTypeDef>)o.GetType().GetField("alienbodytypes").GetValue(o);
				alienskincolorgen = (ColorGenerator)o.GetType().GetField("alienskincolorgen").GetValue(o);
				alienskinsecondcolorgen = (ColorGenerator)o.GetType().GetField("alienskinsecondcolorgen").GetValue(o);
				alienhaircolorgen = (ColorGenerator)o.GetType().GetField("alienhaircolorgen").GetValue(o);
				alienhairsecondcolorgen = (ColorGenerator)o.GetType().GetField("alienhairsecondcolorgen").GetValue(o);
				useSkincolorForHair = (bool)o.GetType().GetField("useSkincolorForHair").GetValue(o);
				aliencrowntypes = (List<string>)o.GetType().GetField("aliencrowntypes").GetValue(o);
				useGenderedHeads = (bool)o.GetType().GetField("useGenderedHeads").GetValue(o);
			}
		}
		private class GeneralSettingsConnector {
			public AlienPartGeneratorConnector alienPartGenerator;
			
			public GeneralSettingsConnector(object o) {
				alienPartGenerator = new AlienPartGeneratorConnector(o.GetType().GetField("alienPartGenerator").GetValue(o));
			}
		}
		private class HairSettingsConnector {
			public bool hasHair = true;
			public List<string> hairTags;
			public int getsGreyAt = 40;
			
			public HairSettingsConnector(object o) {
				hairTags = (List<string>)o.GetType().GetField("hairTags").GetValue(o);
				hasHair = (bool)o.GetType().GetField("hasHair").GetValue(o);
				getsGreyAt = (int)o.GetType().GetField("getsGreyAt").GetValue(o);
			}
		}
		private class AlienRaceConnector {
			public GeneralSettingsConnector generalSettings;
			public HairSettingsConnector hairSettings;
			public GraphicsPathConnectors graphicPaths;
			
			public AlienRaceConnector(object o) {
				generalSettings = new GeneralSettingsConnector(o.GetType().GetField("generalSettings").GetValue(o));
				hairSettings = new HairSettingsConnector(o.GetType().GetField("hairSettings").GetValue(o));
				graphicPaths = new GraphicsPathConnectors((IList)o.GetType().GetField("graphicPaths").GetValue(o));
			}
		}
		private class GraphicsPathConnector {
			public string head;
			public List<LifeStageDef> lifeStageDefs;
			public GraphicsPathConnector(object o) {
				head = (string)o.GetType().GetField("head").GetValue(o);
				lifeStageDefs = (List<LifeStageDef>)o.GetType().GetField("lifeStageDefs").GetValue(o);
			}
		}
		private class GraphicsPathConnectors {
			public List<GraphicsPathConnector> listOfGraphicsParts;
			public GraphicsPathConnectors(IList os) {
				listOfGraphicsParts = new List<GraphicsPathConnector>();
				foreach ( object o in os ) {
					listOfGraphicsParts.Add(new GraphicsPathConnector(o));
				}
			}
			public GraphicsPathConnector GetCurrentGraphicPath(LifeStageDef lifeStageDef) {
				Type alienType = DefSettingsField.GetTypeFromAnyAssemblyVersion("AlienRace.GraphicPathsExtension, AlienRace, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
				
				return listOfGraphicsParts.FirstOrDefault((GraphicsPathConnector gp) => gp.lifeStageDefs != null && gp.lifeStageDefs.Contains(lifeStageDef)) ?? 
					listOfGraphicsParts.First<GraphicsPathConnector>();
			}
		}
		private class ThingDef_AlienRaceConnector {
			public AlienRaceConnector alienRace;
			
			public ThingDef_AlienRaceConnector(ThingDef d) {
				alienRace = new AlienRaceConnector(d.GetType().GetField("alienRace").GetValue(d));
			}
		}
		private class AlienCompConnector {
			static protected Func<object,Color> getterSkinColor, getterSkinColorSecond, getterHairColorSecond;
			static protected Action<object,Color> setterSkinColor, setterSkinColorSecond, setterHairColorSecond;
			static protected Func<object,string> getterCrownType;
			static protected Action<object,string> setterCrownType;
			public Color skinColor {
				get { return getterSkinColor(alienComp); }
				set { setterSkinColor(alienComp,value); }
			}
			public Color skinColorSecond {
				get { return getterSkinColorSecond(alienComp); }
				set { setterSkinColorSecond(alienComp,value); }
			}
			public Color hairColorSecond {
				get { return getterHairColorSecond(alienComp); }
				set { setterHairColorSecond(alienComp,value); }
			}
			public string crownType {
				get { return getterCrownType(alienComp); }
				set { setterCrownType(alienComp,value); }
			}
			protected object alienComp;
			
			public AlienCompConnector(Pawn p) {
				Type alienCompType = DefSettingsField.GetTypeFromAnyAssemblyVersion("AlienRace.AlienPartGenerator+AlienComp, AlienRace, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
				alienComp = p.AllComps.FirstOrDefault(alienCompType.IsInstanceOfType);
				if ( getterSkinColor == null ) getterSkinColor = UtilReflection.CreateGetter<object,Color>(alienCompType.GetField("skinColor"));
				if ( setterSkinColor == null ) setterSkinColor = UtilReflection.CreateSetter<object,Color>(alienCompType.GetField("skinColor"));
				if ( getterSkinColorSecond == null ) getterSkinColorSecond = UtilReflection.CreateGetter<object,Color>(alienCompType.GetField("skinColorSecond"));
				if ( setterSkinColorSecond == null ) setterSkinColorSecond = UtilReflection.CreateSetter<object,Color>(alienCompType.GetField("skinColorSecond"));
				if ( getterHairColorSecond == null ) getterHairColorSecond = UtilReflection.CreateGetter<object,Color>(alienCompType.GetField("hairColorSecond"));
				if ( setterHairColorSecond == null ) setterHairColorSecond = UtilReflection.CreateSetter<object,Color>(alienCompType.GetField("hairColorSecond"));
				if ( getterCrownType == null ) getterCrownType = UtilReflection.CreateGetter<object,string>(alienCompType.GetField("crownType"));
				if ( setterCrownType == null ) setterCrownType = UtilReflection.CreateSetter<object,string>(alienCompType.GetField("crownType"));
			}
		}
		private class AlienBackstoryDefConnector {
			static protected Func<object,string> getterLinkedBackstory;
			static protected Func<object,IntRange> getterBioAgeRange, getterChronoAgeRange;
			static protected Func<object,List<string>> getterForcedHediffs;
			public IntRange bioAgeRange {
				get { if ( alienBs == null ) return default(IntRange); return getterBioAgeRange(alienBs); }
			}
			public IntRange chronoAgeRange {
				get { if ( alienBs == null ) return default(IntRange); return getterChronoAgeRange(alienBs); }
			}
			public List<string> forcedHediffs {
				get { if ( alienBs == null ) return null; return getterForcedHediffs(alienBs); }
			}
			public string linkedBackstory {
				get { if ( alienBs == null ) return null; return getterLinkedBackstory(alienBs); }
			}
			protected object alienBs;
			public AlienBackstoryDefConnector(Backstory bs) {
				Type alienBsType = DefSettingsField.GetTypeFromAnyAssemblyVersion("AlienRace.BackstoryDef, AlienRace, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
				if ( alienBsType == null ) return;
				if ( getterLinkedBackstory == null ) getterLinkedBackstory = UtilReflection.CreateGetter<object,string>(alienBsType.GetField("linkedBackstory"));
				if ( getterBioAgeRange == null ) getterBioAgeRange = UtilReflection.CreateGetter<object,IntRange>(alienBsType.GetField("bioAgeRange"));
				if ( getterChronoAgeRange == null ) getterChronoAgeRange = UtilReflection.CreateGetter<object,IntRange>(alienBsType.GetField("chronoAgeRange"));
				if ( getterForcedHediffs == null ) getterForcedHediffs = UtilReflection.CreateGetter<object,List<string>>(alienBsType.GetField("forcedHediffs"));
				alienBs = typeof(DefDatabase<>).MakeGenericType(alienBsType).GetMethod("GetNamed").Invoke(null,new object[]{bs.identifier,false});
			}
		}
		
		private class RPGModel
		{
			public bool isAlienRace = false, allowAllHaircuts = false;
			protected ThingDef_AlienRaceConnector alienRaceConnector;
			protected AlienCompConnector alienCompConnector;
			protected List<BodyTypeDef> bodyTypesInt;
			protected List<string> headGraphicPathsInt;
			protected List<string> hairTraitsInt, crownTypesInt;
			protected Color currentSkinColorInt = Color.clear, currentSecondarySkinColorInt = Color.clear, currentHairColorInt = Color.clear, currentSecondaryHairColorInt = Color.clear;
			public List<BodyTypeDef> bodyTypes {
				get {
					if ( bodyTypesInt == null ) {
						if ( !isAlienRace ) {
							bodyTypesInt = new List<BodyTypeDef>(DefDatabase<BodyTypeDef>.AllDefsListForReading);
						} else {
							bodyTypesInt = new List<BodyTypeDef>(alienRaceConnector.alienRace.generalSettings.alienPartGenerator.alienbodytypes);
						}
					}
					return bodyTypesInt;
				}
			}
			public List<string> headGraphicPaths {
				get {
					if ( headGraphicPathsInt == null ) {
						headGraphicPathsInt = new List<string>();
						if ( isAlienRace ) {
							GraphicsPathConnector gpc = alienRaceConnector.alienRace.graphicPaths.GetCurrentGraphicPath(pawn.ageTracker.CurLifeStage);
							string folder = gpc.head;
							if ( folder.EndsWith("/") ) folder = folder.Substring(0,folder.Length-1);
							foreach (Texture2D currentTexture in ContentFinder<Texture2D>.GetAllInFolder(folder)) {
								string current = currentTexture.name;
								if (current.EndsWith("_south")) {
									headGraphicPathsInt.Add(folder + "/" + current);
								}
							}
							foreach ( string s in headGraphicPathsInt ) Log.Message("found graphic: "+s);
						} else {
							//vanilla: "Things/Pawn/Humanlike/Heads/Male","Things/Pawn/Humanlike/Heads/Female"
							string[] headsFolderPaths = (string[])typeof(GraphicDatabaseHeadRecords).GetField("HeadsFolderPaths", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
							for (int i = 0; i < headsFolderPaths.Length; i++)
							{
								string text = headsFolderPaths[i];
								foreach (string current in GraphicDatabaseUtility.GraphicNamesInFolder(text)) {
									headGraphicPathsInt.Add(text + "/" + current + "_south");
								}
							}
						}
					}
					return headGraphicPathsInt;
				}
			}
			public bool hasHair {
				get { return !isAlienRace || alienRaceConnector.alienRace.hairSettings.hasHair; }
			}
			public List<string> hairTraits {
				get {
					if ( hairTraitsInt == null ) {
						if ( !isAlienRace || alienRaceConnector.alienRace.hairSettings.hairTags.NullOrEmpty() ) {
							hairTraitsInt = new List<string>(pawn.Faction.def.hairTags);
						} else {
							hairTraitsInt = new List<string>(alienRaceConnector.alienRace.hairSettings.hairTags);
						}
					}
					return hairTraitsInt;
				}
			}
			public List<string> crownTypes {
				get {
					if ( crownTypesInt == null ) {
						if ( !isAlienRace ) {
							crownTypesInt = new List<string>(new string[]{CrownType.Average.ToString(),CrownType.Narrow.ToString()});
						} else {
							crownTypesInt = alienRaceConnector.alienRace.generalSettings.alienPartGenerator.aliencrowntypes;
						}
					}
					return crownTypesInt;
				}
			}
			public Color CurrentSkinColor {
				get { return currentSkinColorInt; }
				set { 
					currentSkinColorInt = value; 
					if ( isAlienRace && alienRaceConnector.alienRace.generalSettings.alienPartGenerator.useSkincolorForHair ) {
						CurrentHairColor = currentSkinColorInt;
					}
					if ( isAlienRace && !hasSecondarySkinColorGenerator ) {
						CurrentSecondarySkinColor = currentSkinColorInt;
					}
				}
			}
			public Color CurrentHairColor {
				get { return currentHairColorInt; }
				set {
					currentHairColorInt = value;
					if ( isAlienRace && !hasSecondaryHairColorGenerator ) {
						CurrentSecondaryHairColor = currentHairColorInt;
					}
				}
			}
			public Color CurrentSecondarySkinColor {
				get { return currentSecondarySkinColorInt; }
				set { currentSecondarySkinColorInt = value; }
			}
			public Color CurrentSecondaryHairColor {
				get { return currentSecondaryHairColorInt; }
				set { currentSecondaryHairColorInt = value; }
			}
			public void GenerateCurrentSkinColor() {
				if (!isAlienRace || alienRaceConnector.alienRace.generalSettings.alienPartGenerator.alienskincolorgen == null) {
					currentSkinColorInt = PawnSkinColors.GetSkinColor(melaninInt);
					hasSkinColorGenerator = false;
				} else {
					Color oldSkinColor = currentSkinColorInt;
					for ( int i = 0; currentSkinColorInt.Equals(oldSkinColor) && i < 10; i++ )
						currentSkinColorInt = alienRaceConnector.alienRace.generalSettings.alienPartGenerator.alienskincolorgen.NewRandomizedColor();
					hasSkinColorGenerator = true;
				}
			}
			public void GenerateCurrentSecondarySkinColor() {
				if ( !isAlienRace || alienRaceConnector.alienRace.generalSettings.alienPartGenerator.alienskinsecondcolorgen == null ) {
					currentSecondarySkinColorInt = currentSkinColorInt;
					hasSecondarySkinColorGenerator = false;
				} else {
					Color oldSkinColor = currentSecondarySkinColorInt;
					for ( int i = 0; currentSecondarySkinColorInt.Equals(oldSkinColor) && i < 10; i++ )
						currentSecondarySkinColorInt = alienRaceConnector.alienRace.generalSettings.alienPartGenerator.alienskinsecondcolorgen.NewRandomizedColor();
					hasSecondarySkinColorGenerator = true;
				}
			}
			public void GenerateCurrentSecondaryHairColor() {
				if ( !isAlienRace || alienRaceConnector.alienRace.generalSettings.alienPartGenerator.alienhairsecondcolorgen == null ) {
					currentSecondaryHairColorInt = currentHairColorInt;
					hasSecondaryHairColorGenerator = false;
				} else {
					Color oldSkinColor = currentSecondaryHairColorInt;
					for ( int i = 0; currentSecondaryHairColorInt.Equals(oldSkinColor) && i < 10; i++ )
						currentSecondaryHairColorInt = alienRaceConnector.alienRace.generalSettings.alienPartGenerator.alienhairsecondcolorgen.NewRandomizedColor();
					hasSecondaryHairColorGenerator = true;
				}
			}
			public void GenerateCurrentHairColor() {
				if (!isAlienRace || alienRaceConnector.alienRace.generalSettings.alienPartGenerator.alienhaircolorgen == null) {
					if ( isAlienRace && alienRaceConnector.alienRace.generalSettings.alienPartGenerator.useSkincolorForHair ) {
						currentHairColorInt = currentSkinColorInt;
					} else {
						currentHairColorInt = PawnHairColors.RandomHairColor(currentSkinColorInt, pawn.ageTracker.AgeBiologicalYears);
					}
					hasHairColorGenerator = false;
				} else {
					Color oldSkinColor = currentHairColorInt;
					for ( int i = 0; currentHairColorInt.Equals(oldSkinColor) && i < 10; i++ )
						currentHairColorInt = alienRaceConnector.alienRace.generalSettings.alienPartGenerator.alienhaircolorgen.NewRandomizedColor();
					hasHairColorGenerator = true;
				}
				if ( isAlienRace && alienRaceConnector.alienRace.hairSettings.getsGreyAt <= pawn.ageTracker.AgeBiologicalYears ) {
					float num = Rand.Range(0.65f, 0.85f);
					currentHairColorInt = new Color(num, num, num);
				}
			}
			public bool hasSecondarySkinColorGenerator = false, hasSecondaryHairColorGenerator = false;
			public bool hasSkinColorGenerator = false, hasHairColorGenerator = false;
			
			public float totalCost, resources, resourcesPooled;
			public Pawn pawn;
			public Dictionary<BackstorySlot,Backstory> backstories;
			public List<Trait> traits;
			public List<RPGModelSkill> skills;
			
			public BodyTypeDef bodyType = null;
			public string headGraphicPath = null;
			public string crownType = CrownType.Undefined.ToString();
			public HairDef hairDef = null;
			protected float melaninInt = -1; //0<->1
			public float melanin {
				get { return melaninInt; }
				set {
					if (Math.Abs(melaninInt - value) > float.Epsilon) {					
						melaninInt = value;
						if ( !hasSkinColorGenerator ) {
							GenerateCurrentSkinColor();
							if ( !hasSecondarySkinColorGenerator ) GenerateCurrentSecondarySkinColor();
						}
					}
				}
			}
			private Gender _gender;
			public Gender gender {
				set{_gender = value; calculateMessages();}
				get{return _gender;}
			}
			
			public string firstname, lastname, nickname;
			
			protected Dictionary<SkillDef,bool> cachedDisabled;
			public List<string> messages;
			protected float BC;
			
			public RPGModel(Pawn pawn) {
				this.pawn = pawn;
				this.isAlienRace = pawn != null && pawn.def.GetType() == DefSettingsField.GetTypeFromAnyAssemblyVersion("AlienRace.ThingDef_AlienRace, AlienRace, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
				if ( isAlienRace ) {
					alienRaceConnector = new ThingDef_AlienRaceConnector(pawn.def);
					alienCompConnector = new AlienCompConnector(pawn);
				}
				this.BC = QOLMod.getBaseCost();
				backstories = new Dictionary<BackstorySlot, Backstory>();
				backstories.Add(BackstorySlot.Childhood, pawn.story.childhood);
				backstories.Add(BackstorySlot.Adulthood, pawn.story.adulthood);
				
				traits = new List<Trait>();
				traits.AddRange(pawn.story.traits.allTraits);
				
				skills = new List<RPGModelSkill>();
				foreach(SkillRecord sr in pawn.skills.skills) {
					skills.Add(new RPGModelSkill(sr));
				}
				
				Pawn_StoryTracker story = pawn.story;
				bodyType = story.bodyType;
				headGraphicPath = story.HeadGraphicPath;
				if ( isAlienRace ) {
					crownType = alienCompConnector.crownType;
				} else {
					crownType = story.crownType.ToString();
				}
				hairDef = story.hairDef;
				currentHairColorInt = story.hairColor;
				melanin = story.melanin;
				if ( isAlienRace ) {
					currentSkinColorInt = alienCompConnector.skinColor;
					currentSecondarySkinColorInt = alienCompConnector.skinColorSecond;
					currentSecondaryHairColorInt = alienCompConnector.hairColorSecond;
				}
				_gender = pawn.gender;
				
				NameTriple n = pawn.Name as NameTriple;
				if ( n != null ) {
					firstname = n.First;
					nickname = n.Nick;
					lastname = n.Last;
				} else {
					NameSingle ns = pawn.Name as NameSingle;
					firstname = ns.Name;
					nickname = ns.Name;
					lastname = ns.Number.ToString();
				}
				
				cachedDisabled = new Dictionary<SkillDef, bool>();
				messages = new List<string>();
				
				calculateSkillLevels();
				calculateMessages();
				
				resources = 0;
				foreach ( RecordDef def in DefDatabase<RecordDef>.AllDefsListForReading ) {
					resources += QOLMod.getCuMRecordPoints(pawn,def);
				}
				resources = Mathf.Floor(resources);
				
				resourcesPooled = Current.Game.GetComponent<QOLModGameComponent>().pooledPoints;
				resourcesPooled = Mathf.Floor(resourcesPooled);
			}
			
			public bool isDisabled(SkillDef def) {
				bool disabled;
				if ( !cachedDisabled.TryGetValue(def,out disabled) ) {
					WorkTags workTags = WorkTags.None;
					List<WorkTypeDef> disabledWorkTypes = new List<WorkTypeDef>();
					foreach(Backstory bs in backstories.Values) {
						if ( bs != null ) {
							workTags |= bs.workDisables;
							disabledWorkTypes.AddRange(from WorkTypeDef wtd in bs.DisabledWorkTypes where !disabledWorkTypes.Contains(wtd) select wtd);
						}
					}
					foreach(Trait t in traits) {
						workTags |= t.def.disabledWorkTags;
						disabledWorkTypes.AddRange(from WorkTypeDef wtd in t.GetDisabledWorkTypes() where !disabledWorkTypes.Contains(wtd) select wtd);
					}
					disabled = def.IsDisabled(workTags,disabledWorkTypes);
					cachedDisabled.Add(def,disabled);
				}
				return disabled;
			}
			
			public bool acceptChanges() {
				//consume resources according to totalCosts if possible otherwise return false
				float points = 0;
				RecordDef spentPointsDef = DefDatabase<RecordDef>.GetNamed("CuMSpent");
				points += pawn.records.getCuMWorth();
				QOLModGameComponent pool = Current.Game.GetComponent<QOLModGameComponent>();
				float poolPoints = pool.pooledPoints;
				if ( points >= totalCost && spentPointsDef != null ) {
					pawn.records.AddTo(spentPointsDef, totalCost);
				} else if ( points + poolPoints >= totalCost && spentPointsDef != null ) {
					pawn.records.AddTo(spentPointsDef, points);
					pool.pooledPoints -= totalCost-points;
				} else {
					return false;
				}
				
				pawn.gender = gender;
				pawn.story.childhood = backstories[BackstorySlot.Childhood];
				pawn.story.adulthood = backstories[BackstorySlot.Adulthood];
				pawn.story.traits.allTraits.Clear();
				pawn.story.traits.allTraits.AddRange(traits);
				foreach( SkillRecord sr in pawn.skills.skills ) {
					RPGModelSkill skill = skills.Find(s=>s.def == sr.def);
					//LevelDescriptor can be used to determine the maximum and minimum level. if description is unknown we lower positive values or raise negative values.
					//exp needed (levelInt+1)*1000
					sr.Level = Math.Max(0, Math.Min(20,skill.level));
					sr.passion = skill.passion;
				}
				
				pawn.story.bodyType = bodyType;
				if ( !isAlienRace )
					pawn.story.GetType().GetField("headGraphicPath", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pawn.story, headGraphicPath);
				if ( isAlienRace ) {
					alienCompConnector.crownType = crownType;
				} else {
					pawn.story.crownType = (CrownType)Enum.Parse(typeof(CrownType),crownType);
				}
				pawn.story.hairDef = hairDef;
				pawn.story.hairColor = currentHairColorInt;
				pawn.story.melanin = melanin;
				if ( isAlienRace ) {
					alienCompConnector.skinColor = currentSkinColorInt;
					alienCompConnector.skinColorSecond = currentSecondarySkinColorInt;
				}
				
				NameTriple n = pawn.Name as NameTriple;
				if ( n == null || !n.First.Equals(firstname) || (n.Nick != null && !n.Nick.Equals(nickname)) || (n.Nick == null && !nickname.NullOrEmpty()) || !n.Last.Equals(lastname) )
					pawn.Name = new NameTriple(firstname,nickname,lastname);
				
				//applies the forced hediffs
				if ( isAlienRace ) {
					List<string> forcedHediffs = new List<string>();
					if ( pawn.story.childhood != null ) {
						AlienBackstoryDefConnector alienBs = new AlienBackstoryDefConnector(pawn.story.childhood);
						if ( !alienBs.forcedHediffs.NullOrEmpty() ) forcedHediffs.AddRange(alienBs.forcedHediffs);
					}
					if ( pawn.story.adulthood != null ) {
						AlienBackstoryDefConnector alienBs = new AlienBackstoryDefConnector(pawn.story.adulthood);
						if ( !alienBs.forcedHediffs.NullOrEmpty() ) forcedHediffs.AddRange(alienBs.forcedHediffs);
					}
					
					foreach ( string hediff in forcedHediffs ) {
						HediffDef hd = DefDatabase<HediffDef>.GetNamed(hediff,false);
						if ( hd != null ) {
							BodyPartRecord bodyPartRecord = null;
							RecipeDef recipeThatAddsHediff = DefDatabase<RecipeDef>.AllDefsListForReading.FirstOrDefault((RecipeDef rd) => rd.addsHediff == hd);
							if (recipeThatAddsHediff != null)
							{
								recipeThatAddsHediff.appliedOnFixedBodyParts.SelectMany((BodyPartDef bpd) => 
									from bpr in pawn.health.hediffSet.GetNotMissingParts(0, 0, null, null)
									where bpr.def == bpd && !pawn.health.hediffSet.hediffs.Any((Hediff h) => h.def == hd && h.Part == bpr)
									select bpr
								).TryRandomElement(out bodyPartRecord);
							}
							pawn.health.AddHediff(hd,bodyPartRecord);
						}
					}
				}
				
				updatePawn();
				return true;
			}
			
			private void updatePawn() {
				//follows the code in: TraitSet.GainTrait(trait)
				if (pawn.workSettings != null) {
					pawn.workSettings.Notify_GainedTrait();
				}
				//p.story.Notify_TraitChanged();
				pawn.story.GetType().GetMethod("Notify_TraitChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(pawn.story,null);
				
				if (pawn.skills != null) {
					//This does only reset the cached value wheter or not a skill is completely disabled. It does not drop weapons if the pawn becomes unable to use violence etc.
					pawn.skills.Notify_SkillDisablesChanged();
				}
				if (!pawn.Dead && pawn.RaceProps.Humanlike) {
					pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
				}
				
				//In case a pawn is unable to use specific equipment we can try to drop it
				if ( pawn.skills.GetSkill(SkillDefOf.Shooting).TotallyDisabled && pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsWithinCategory(DefDatabase<ThingCategoryDef>.GetNamed("WeaponsRanged")) ) {
					ThingWithComps thingWithComps;
					if ( !pawn.equipment.TryDropEquipment(pawn.equipment.Primary,out thingWithComps,pawn.Position, false) ) {
						Log.Message("Pawn with disabled shooting skill has a ranged weapon equipped that could not be dropped!");
					}
				}
				if ( pawn.skills.GetSkill(SkillDefOf.Melee).TotallyDisabled && pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsWithinCategory(DefDatabase<ThingCategoryDef>.GetNamed("WeaponsMelee")) ) {
					ThingWithComps thingWithComps;
					if ( !pawn.equipment.TryDropEquipment(pawn.equipment.Primary,out thingWithComps,pawn.Position, false) ) {
						Log.Message("Pawn with disabled melee skill has a melee weapon equipped that could not be dropped!");
					}
				}
				
				/*System.Reflection.FieldInfo fi = pawn.story.GetType().GetField("headGraphicPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				fi.SetValue(pawn.story,null);*/
				pawn.Drawer.renderer.graphics.ResolveAllGraphics();
				PortraitsCache.SetDirty(pawn);
			}
			
			public void apply(IRPGUpgrade upgrade) {
				float upgradeCost = 0;
				
				RPGUpgradeBackstory bsUpgrade = upgrade as RPGUpgradeBackstory;
				if ( bsUpgrade != null ) {
					float costOld = backstories[bsUpgrade.slot].getCuMWorth();
					float costNew = bsUpgrade.value.getCuMWorth();
					upgradeCost = costNew - costOld;
					
					backstories[bsUpgrade.slot] = bsUpgrade.value;
				}
				
				RPGUpgradePassion passionUpgrade = upgrade as RPGUpgradePassion;
				if ( passionUpgrade != null ) {
					RPGModelSkill skill = skills.Find(s=>s.def == passionUpgrade.def);
					float costOld = skill.passion.getCuMWorth();
					float costNew = passionUpgrade.passion.getCuMWorth();
					upgradeCost = costNew - costOld;
					
					skill.passion = passionUpgrade.passion;
				}
				
				RPGUpgradeTrait traitUpgrade = upgrade as RPGUpgradeTrait;
				if ( traitUpgrade != null ) {
					//different removal cost than add cost. Is only used if the pawn currently has the trait otherwise add cost - to allow switching the traits around.
					bool useRemovalCost = traitUpgrade.remove != null && pawn.story.traits.HasTrait(traitUpgrade.remove.def) && 
						pawn.story.traits.DegreeOfTrait(traitUpgrade.remove.def) == traitUpgrade.remove.Degree;
					float costOld = traitUpgrade.remove.getCuMWorth(useRemovalCost);
					if ( traitUpgrade.remove != null && !traits.Contains(traitUpgrade.remove) ) costOld = 0;
					float costNew = traitUpgrade.add.getCuMWorth();
					bool notADuplicate = traitUpgrade.add == null || !(from Trait t in traits where t.def == traitUpgrade.add.def select t.def).Any();
					if ( traitUpgrade.add != null && !notADuplicate ) costNew = 0;
					upgradeCost = costNew - costOld;
					
					if ( traitUpgrade.remove != null ) traits.Remove(traitUpgrade.remove);
					if ( traitUpgrade.add != null && notADuplicate ) traits.Add(traitUpgrade.add);
				}
				
				totalCost += upgradeCost;
				
				//clearing state
				cachedDisabled.Clear();
				
				calculateSkillLevels();
				calculateMessages();
			}
			
			protected void calculateSkillLevels() {
				Dictionary<RPGModelSkill,int> newBaseLevels = new Dictionary<RPGModelSkill, int>();
				foreach(RPGModelSkill skill in skills) {
					newBaseLevels[skill] = 0;
				}
				foreach(Backstory bs in backstories.Values) {
					if ( bs != null ) {
						foreach(KeyValuePair<SkillDef,int> p in bs.skillGainsResolved) {
							RPGModelSkill skill = skills.Find(s=>s.def == p.Key);
							newBaseLevels[skill] += p.Value;
						}
					}
				}
				foreach(Trait t in traits) {
					foreach(KeyValuePair<SkillDef,int> p in t.CurrentData.skillGains) {
						RPGModelSkill skill = skills.Find(s=>s.def == p.Key);
						newBaseLevels[skill] += p.Value;
					}
				}
				foreach(RPGModelSkill skill in skills) {
					skill.levelBase = newBaseLevels[skill];
				}
			}
			
			protected void calculateMessages() {
				messages.Clear();
				foreach(KeyValuePair<BackstorySlot,Backstory> p in backstories) {
					Backstory bs = p.Value;
					if ( bs == null ) continue;
					AlienBackstoryDefConnector alienBs;
					if ( isAlienRace )
						alienBs = new AlienBackstoryDefConnector(bs);
					else alienBs = null;
					if ( bs.slot != p.Key ) {
						messages.Add("Wrong backstory slot for story \""+bs.titleShort+"\"!");
					}
					foreach ( Trait t in traits ) {
						if ( bs.DisallowsTrait(t.def,t.Degree) ) {
							messages.Add("Backstory \""+bs.titleShort+"\" disallows trait \""+t.Label+"\"!");
						}
					}
					if( bs.forcedTraits != null ) {
						foreach ( TraitEntry te in bs.forcedTraits ) {
							Trait trait = traits.Find(t=>t.def == te.def && t.Degree == te.degree);
							if ( trait != null ) {
								messages.Add("Backstory \""+bs.titleShort+"\" is requiring trait \""+trait.Label+"\"!");
							}
						}
					}
					if ( pawn.story.bodyType != bs.BodyTypeFor(gender) && bs.BodyTypeFor(gender) != null ) {
						messages.Add("Backstory \""+bs.titleShort+"\" requires body type \""+bs.BodyTypeFor(gender)+"\"!");
					}
					if ( bs.slot == BackstorySlot.Childhood ) {
						if ( alienBs != null ) {
							string linkedBackstory = alienBs.linkedBackstory;
							if ( !linkedBackstory.NullOrEmpty() && backstories[BackstorySlot.Adulthood] != null && backstories[BackstorySlot.Adulthood].identifier != linkedBackstory ) {
								messages.Add("Backstory \""+bs.titleShort+"\" requires adult backstory \""+linkedBackstory+"\"!");
							}
						}
					}
					if ( alienBs != null ) {
						IntRange bioAgeRange = alienBs.bioAgeRange;
						IntRange chronoAgeRange = alienBs.chronoAgeRange;
						if ( bioAgeRange != default(IntRange) ) {
							if ( bioAgeRange.min >= pawn.ageTracker.AgeBiologicalYears || pawn.ageTracker.AgeBiologicalYears >= bioAgeRange.max ) {
								messages.Add("Backstory \""+bs.titleShort+"\" requires biological age from "+bioAgeRange.min+" to "+bioAgeRange.max+"!");
							}
						}
						if ( chronoAgeRange != default(IntRange) ) {
							if ( chronoAgeRange.min >= pawn.ageTracker.AgeChronologicalYears || pawn.ageTracker.AgeChronologicalYears >= chronoAgeRange.max ) {
								messages.Add("Backstory \""+bs.titleShort+"\" requires chronological age from "+chronoAgeRange.min+" to "+chronoAgeRange.max+"!");
							}
						}
						List<string> forcedHediffs = alienBs.forcedHediffs;
						if ( !forcedHediffs.NullOrEmpty() ) {
							foreach ( string hediff in forcedHediffs ) {
								HediffDef hd = DefDatabase<HediffDef>.GetNamed(hediff,false);
								if ( hd != null ) {
									if ( !pawn.health.hediffSet.HasHediff(hd) )
										messages.Add("Backstory \""+bs.titleShort+"\" requires health condition "+SettingsFieldPropertiesSelectable.DefLabelProducer(hd)+"!");
								}
							}
						}
					}
					
					if ( !bs.spawnCategories.NullOrEmpty() && pawn.Faction != null && !pawn.Faction.def.backstoryCategories.NullOrEmpty() && !bs.spawnCategories.Intersect(pawn.Faction.def.backstoryCategories).Any() ) {
						messages.Add("Backstory \""+bs.titleShort+"\" allows categories \""+String.Join("\",\"",bs.spawnCategories.ToArray())+"\" but the pawns faction belongs to \""+String.Join("\",\"",pawn.Faction.def.backstoryCategories.ToArray())+"\"!");
					}
					
					//pawn.kindDef.backstoryCategory//unused so we can ignore it
				}
				
				/*
			    hediffGraphics
				 */
				
				List<WorkTypeDef> disabledWorkTypes = new List<WorkTypeDef>();
				WorkTags disabledWorkTags = WorkTags.None;
				foreach(Backstory bs in backstories.Values) {
					if ( bs != null ) {
						disabledWorkTags |= bs.workDisables;
						disabledWorkTypes.AddRange(from WorkTypeDef wtd in bs.DisabledWorkTypes where !disabledWorkTypes.Contains(wtd) select wtd);
					}
				}
				foreach(Trait t in traits) {
					disabledWorkTags |= t.def.disabledWorkTags;
					disabledWorkTypes.AddRange(from WorkTypeDef wtd in t.GetDisabledWorkTypes() where !disabledWorkTypes.Contains(wtd) select wtd);
				}
				
				foreach ( Trait t in traits ) {
					foreach ( Trait t2 in traits ) {
						if ( t2 != t ) {
							if ( t.def.ConflictsWith(t2) ) {
								messages.Add("Trait \""+t.Label+"\" conflicts with trait \""+t2.Label+"\"!");
							}
						}
					}
					
					List<WorkTypeDef> allDefsListForReading = DefDatabase<WorkTypeDef>.AllDefsListForReading;
					string[] labels = (from WorkTypeDef workTypeDef in allDefsListForReading where 
					          (workTypeDef.workTags & disabledWorkTags) != WorkTags.None && (workTypeDef.workTags & t.def.requiredWorkTags) != WorkTags.None 
					          select workTypeDef.workTags.LabelTranslated()).ToArray<string>();
					if (!labels.NullOrEmpty()) {
						messages.Add("Trait \""+t.Label+"\" requires colonist to be capable of "+string.Join(", ",labels)+"!");
					}
					labels = (from WorkTypeDef wtd in disabledWorkTypes.Intersect(t.def.requiredWorkTypes) select wtd.label).ToArray();
					if (!labels.NullOrEmpty()) {
						messages.Add("Trait \""+t.Label+"\" requires colonist to be capable of "+string.Join(", ",labels)+"!");
					}
				}
				
				
				
				//show a warning if we have to clip skills
				bool skillClipped = false;
				foreach ( RPGModelSkill skill in skills ) {
					if ( skill.level > 20 || skill.level < 0 ) {
						skillClipped = true;
						break;
					}
				}
				if ( skillClipped ) messages.Add("Levels above 20 or below 0 will be lost.");
				
				//determine if a backstory is valid for the pawns gender
				foreach(KeyValuePair<BackstorySlot,Backstory> bs in backstories) {
					if ( bs.Value != null && !(from bio in SolidBioDatabase.allBios
					       where ((bs.Key == BackstorySlot.Childhood && bio.childhood == bs.Value) || (bs.Key == BackstorySlot.Adulthood && bio.adulthood == bs.Value)) && 
					       (bio.gender == GenderPossibility.Either || gender == Gender.None || 
					        (gender == Gender.Male && bio.gender == GenderPossibility.Male) || 
					        (gender == Gender.Female && bio.gender == GenderPossibility.Female))
					       select bio).Any() ) {
						PawnBio bio = SolidBioDatabase.allBios.FirstOrDefault(b => (bs.Key == BackstorySlot.Childhood && b.childhood == bs.Value) || (bs.Key == BackstorySlot.Adulthood && b.adulthood == bs.Value));
						//some backstory are not in "rotation" for those the gender can not be determined.
						if ( bio != null ) messages.Add("Backstory \""+bs.Value.titleShort+"\" conflicts with pawns gender!");
					}
				}
			}
		}
		
		private class RPGModelSkill
		{
			public SkillDef def;
			private int levelBaseInt;
			public int levelBase {
				get {return levelBaseInt;}
				set {if ( levelBaseInt != -1 ) level += value - levelBaseInt; levelBaseInt = value;}
			}
			public int level;
			public Passion passion;
			public RPGModelSkill(SkillRecord record) : this(record.def, record.levelInt, record.passion) {
			}
			
			public RPGModelSkill(SkillDef def, int level, Passion passion) {
				this.def = def;
				this.level = level;
				this.passion = passion;
				this.levelBaseInt = -1;
			}
		}
	}
	
	public class Dialog_FilterBackstories : IDialog_Filter<Backstory> {
		protected string filter;
		protected List<string> skillGainsToOrderBy = new List<string>();
		protected bool reorder = true;
		protected Pawn p;
		public Dialog_FilterBackstories(Pawn p) {
			this.p = p;
		}
		
		#region IDialog_Filter implementation
		public Rect DoFilterWindowContent(Rect rect)
		{
			this.filter = Widgets.TextField(new Rect(0f, 0f, 200f, 30f), this.filter);
			
			Color oldColor = GUI.color;
			Rect symbolRect = new Rect(200f+8f,0f,30f,30f);
			foreach ( SkillDef d in DefDatabase<SkillDef>.AllDefsListForReading ) {
				Texture2D tex; 
				if ( !Materials.IconForSkill.TryGetValue(d.defName, out tex) ) {
					tex = Materials.iconEdit;
				}
				if ( Widgets.ButtonImage(symbolRect,tex,skillGainsToOrderBy.Contains(d.defName) ? Color.green : Color.white) ) {
					if ( !skillGainsToOrderBy.Remove(d.defName) ) 
						skillGainsToOrderBy.Add(d.defName);
					reorder = true;
				}
				symbolRect.x += symbolRect.width + 8f;
			}
			GUI.color = oldColor;
			//TODO maybe a checkbox to filter backstories that prevent actions?
			Rect r = new Rect(rect); r.yMin += 35f;
			return r;
		}
		public bool FilterAllows(string label, Backstory value)
		{
			return this.filter.NullOrEmpty() || label.NullOrEmpty() || label.IndexOf(this.filter, StringComparison.OrdinalIgnoreCase) >= 0 || value.FullDescriptionFor(p).IndexOf(this.filter, StringComparison.OrdinalIgnoreCase) >= 0;
		}
		public IOrderedEnumerable<Backstory> Sort(IOrderedEnumerable<Backstory> items) {
			if ( !reorder ) return items;
			reorder = false;
			return items.OrderByDescending(bs=>{
			                     	if ( !bs.skillGainsResolved.Any() ) return 0;
			                     	int gains = 0; 
			                     	foreach(KeyValuePair<SkillDef,int> p in bs.skillGainsResolved) {
			                     		if ( skillGainsToOrderBy.Contains(p.Key.defName) ) gains += p.Value;
			                     	}
			                     	return gains; 
			                     }).ThenBy(bs=>bs.title);
		}
		#endregion
	}
}

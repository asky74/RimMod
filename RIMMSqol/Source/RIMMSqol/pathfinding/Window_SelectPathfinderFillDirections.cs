/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 11.04.2018
 * Time: 03:40
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using UnityEngine;
using Verse;

namespace RIMMSqol.pathfinding
{
	/// <summary>
	/// Description of Window_SelectPathfinderFillDirections.
	/// </summary>
	public class Window_SelectPathfinderFillDirections : Window
	{
		const float btnWidth = 21.0f, btnHeight = 21.0f, iconSize = 16.0f, gapInBetweenBtns = 2.0f;
		static protected Color colorSelected = Color.green, colorUnselected = Color.red;
		
		static public byte selection = 0xFF;
		
		protected override float Margin {
			get {
				return 0f;
			}
		}

		public override Vector2 InitialSize {
			get {
				return new Vector2(btnWidth*3f+gapInBetweenBtns*4f, btnHeight*3f+iconSize+gapInBetweenBtns*4f);
			}
		}
		
		public Window_SelectPathfinderFillDirections()
		{
		}
		
		protected override void SetInitialSizeAndPosition()
		{
			//Mouse position
			Vector2 vector = UI.MousePositionOnUIInverted;
			if (vector.x + this.InitialSize.x > (float)UI.screenWidth) {
				vector.x = (float)UI.screenWidth - this.InitialSize.x;
			}
			if (vector.y + this.InitialSize.y > (float)UI.screenHeight) {
				vector.y = (float)UI.screenHeight - this.InitialSize.y;
			}
			this.windowRect = new Rect(vector.x, vector.y, this.InitialSize.x, this.InitialSize.y);
		}
		
		protected void renderButton(Rect btnRect, Texture2D tex, byte value, ref byte btnPressed) {
			Color color = (value != 0 && (selection & value) == value) || (value == 0 && 0 == selection) ? colorSelected : colorUnselected;
			if ( Widgets.ButtonImage(btnRect, tex, color, color) ) {
				btnPressed = value;
			}
		}
		
		public override void DoWindowContents(Rect inRect) {
			//Closing window if designator is closed. Sadly manager does not escalate deselect to the designator.
			if ( Find.DesignatorManager.SelectedDesignator as Designator_PathfinderFillDirections == null ) {
				Close(false);
			}
			Rect iconRect = new Rect(inRect.x+inRect.width-iconSize,inRect.y,iconSize,iconSize);
			Widgets.DrawTextureFitted(iconRect,Widgets.CheckboxOffTex,1.0f);
			if (Mouse.IsOver(iconRect)) {
				if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
					Event.current.Use();
					GUIUtility.keyboardControl = 0;
					Close(false);
				}
			}
			
			Rect btnRect = new Rect(inRect.x + gapInBetweenBtns,inRect.y+iconSize + gapInBetweenBtns,btnWidth,btnHeight);
			byte btnPressed = 0xFF; 
			
			renderButton(btnRect, Materials.designatorPathfinderFillDirectionsNW, 128, ref btnPressed); btnRect.x += btnWidth + gapInBetweenBtns;
			renderButton(btnRect, Materials.designatorPathfinderFillDirectionsN, 1, ref btnPressed); btnRect.x += btnWidth + gapInBetweenBtns;
			renderButton(btnRect, Materials.designatorPathfinderFillDirectionsNE, 2, ref btnPressed); btnRect.x = inRect.x; btnRect.y += btnHeight + gapInBetweenBtns;
			
			renderButton(btnRect, Materials.designatorPathfinderFillDirectionsW, 64, ref btnPressed); btnRect.x += btnWidth + gapInBetweenBtns;
			renderButton(btnRect, Materials.designatorPathfinderFillDirectionsCenter, 0, ref btnPressed); btnRect.x += btnWidth + gapInBetweenBtns;
			renderButton(btnRect, Materials.designatorPathfinderFillDirectionsE, 4, ref btnPressed); btnRect.x = inRect.x; btnRect.y += btnHeight + gapInBetweenBtns;
			
			renderButton(btnRect, Materials.designatorPathfinderFillDirectionsSW, 32, ref btnPressed); btnRect.x += btnWidth + gapInBetweenBtns;
			renderButton(btnRect, Materials.designatorPathfinderFillDirectionsS, 16, ref btnPressed); btnRect.x += btnWidth + gapInBetweenBtns;
			renderButton(btnRect, Materials.designatorPathfinderFillDirectionsSE, 8, ref btnPressed); btnRect.x = inRect.x; btnRect.y += btnHeight;
			
			if ( btnPressed != 0xFF ) {
				if ( btnPressed == 0 ) {
					if ( selection == 0 ) selection = 0xFF;
					else selection = 0;
				} else {
					selection ^= btnPressed;
				}
			}
		}
	}
}

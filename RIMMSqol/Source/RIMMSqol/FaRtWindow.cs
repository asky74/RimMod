/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 22.04.2018
 * Time: 13:25
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
	/// Description of FaRtWindow.
	/// </summary>
	public class FaRtWindow : Window
	{
		protected bool 
			restrictEatingIngredients,
			restrictEatingPreservedFood, 
			restrictMoodBoostFood, 
			restrictAscetics, 
			restrictAnimals, 
			restrictPrisoners, 
			restrictPrisonerRecruits;
			
		protected Vector2 initialSize = new Vector2(900f, 700f);
		protected Vector2[] rowSizes = null;
		protected float lastCachedSize = -1f;
		
		public FaRtWindow()
		{
			forcePause = true;
			this.doCloseButton = true;
		}
		
		public override Vector2 InitialSize { get { return initialSize; } }
		
		public override void PreOpen() {
			base.PreOpen();
			
			QOLModGameComponent pool = Current.Game.GetComponent<QOLModGameComponent>();
			restrictEatingIngredients = pool.restrictEatingIngredients;
			restrictEatingPreservedFood = pool.restrictEatingPreservedFood; 
			restrictMoodBoostFood = pool.restrictMoodBoostFood;
			restrictAscetics = pool.restrictAscetics;
			restrictAnimals = pool.restrictAnimals;
			restrictPrisoners = pool.restrictPrisoners;
			restrictPrisonerRecruits = pool.restrictPrisonerRecruits;
		}
		
		public override void PostClose() {
			base.PostClose();
			
			QOLModGameComponent pool = Current.Game.GetComponent<QOLModGameComponent>();
			pool.restrictEatingIngredients = restrictEatingIngredients;
			pool.restrictEatingPreservedFood = restrictEatingPreservedFood; 
			pool.restrictMoodBoostFood = restrictMoodBoostFood;
			pool.restrictAscetics = restrictAscetics;
			pool.restrictAnimals = restrictAnimals;
			pool.restrictPrisoners = restrictPrisoners;
			pool.restrictPrisonerRecruits = restrictPrisonerRecruits;
			
			pool.SynchronizeRules();
			
			rowSizes = null;
		}
		
		public override void DoWindowContents(Rect inRect) {
			initRowSizes(inRect);
			
			Rect rectRow = new Rect(inRect.xMin, inRect.yMin, inRect.width, rowSizes[0].y);
			/*
			<NoIngredients>Restrict faction members from eating ingredients. Animals are allowed to eat human corpses.</NoIngredients>
			<NoPreserved>Restrict non starving faction members from feeding or eating preserved food.</NoPreserved>
			<NoFineOrLavish>Restrict non starving colonists with mood above minor break threshold from eating meals better than simple.</NoFineOrLavish>
			<NoFineOrLavishForAscetic>Restrict non starving colonists with ascetics trait from eating better than simple meals.</NoFineOrLavishForAscetic>
			<NoMealsForAnimals>Restrict non starving faction animals from eating meals.</NoMealsForAnimals>
			<NoFineOrLavishForPrisoners>Restrict non starving non colonist prisoners not marked for recruiting from eating meals better than simple.</NoFineOrLavishForPrisoners>
			<NoFineOrLavishForRecruits>Restrict non starving non colonist prisoners marked for recruiting with mood above 80% from eating meals better than simple.</NoFineOrLavishForRecruits>
			 */
			Widgets.CheckboxLabeled(rectRow, "NoIngredients".Translate(), ref restrictEatingIngredients); rectRow.y += rowSizes[0].y; rectRow.height = rowSizes[1].y;
			Widgets.CheckboxLabeled(rectRow, "NoPreserved".Translate(), ref restrictEatingPreservedFood); rectRow.y += rowSizes[1].y; rectRow.height = rowSizes[2].y;
			Widgets.CheckboxLabeled(rectRow, "NoFineOrLavish".Translate(), ref restrictMoodBoostFood); rectRow.y += rowSizes[2].y; rectRow.height = rowSizes[3].y;
			Widgets.CheckboxLabeled(rectRow, "NoFineOrLavishForAscetic".Translate(), ref restrictAscetics); rectRow.y += rowSizes[3].y; rectRow.height = rowSizes[4].y;
			Widgets.CheckboxLabeled(rectRow, "NoMealsForAnimals".Translate(), ref restrictAnimals); rectRow.y += rowSizes[4].y; rectRow.height = rowSizes[5].y;
			Widgets.CheckboxLabeled(rectRow, "NoFineOrLavishForPrisoners".Translate(), ref restrictPrisoners); rectRow.y += rowSizes[5].y; rectRow.height = rowSizes[6].y;
			Widgets.CheckboxLabeled(rectRow, "NoFineOrLavishForRecruits".Translate(), ref restrictPrisonerRecruits); rectRow.y += rowSizes[6].y;
		}
		
		protected void initRowSizes(Rect inRect) {
			if(rowSizes != null && Math.Abs(lastCachedSize - inRect.width) < float.Epsilon) {
				return;
			}
			
			TextAnchor anchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleLeft;
            
			rowSizes = new Vector2[7];
			rowSizes[0] = Text.CalcSize("NoIngredients".Translate());
			rowSizes[1] = Text.CalcSize("NoPreserved".Translate());
			rowSizes[2] = Text.CalcSize("NoFineOrLavish".Translate());
			rowSizes[3] = Text.CalcSize("NoFineOrLavishForAscetic".Translate());
			rowSizes[4] = Text.CalcSize("NoMealsForAnimals".Translate());
			rowSizes[5] = Text.CalcSize("NoFineOrLavishForPrisoners".Translate());
			rowSizes[6] = Text.CalcSize("NoFineOrLavishForRecruits".Translate());
			for ( int i = 0; i < rowSizes.Length; i++ ) {
				rowSizes[i].y *= Mathf.Ceil(rowSizes[i].x / inRect.width);
			}
			lastCachedSize = inRect.width;
			
			Text.Anchor = anchor;
		}
	}
	
	public class PawnColumnWorker_RulesetColumn : PawnColumnWorker {
		private const int TopAreaHeight = 65;
		private const float ManageRulesetButtonHeight = 32f;
		private const int ManageRulesetButtonWidth = 32;
		static protected bool alternateIcon = false;
		static int nextAnimationToggle = 0, animationDelay = 300;
		
		public override void DoHeader(Rect rect, PawnTable table) {
			//base.DoHeader(rect, table);
			if ( Environment.TickCount > nextAnimationToggle ) {
				alternateIcon = !alternateIcon;
				nextAnimationToggle = Environment.TickCount + animationDelay;
			}
			Rect rect2 = new Rect(rect.x, rect.y + (rect.height - TopAreaHeight), ManageRulesetButtonWidth, ManageRulesetButtonHeight);
			if (Widgets.ButtonImage(rect2, alternateIcon ? Materials.iconForbid : Materials.iconUnforbid))
			{
				Find.WindowStack.Add(new FaRtWindow());
			}
			Log.Message("DoHeader "+rect+" "+rect2);
		}
		
		public override int GetMinHeaderHeight(PawnTable table)
		{
			return TopAreaHeight;
		}
		
		public override void DoCell(Rect rect, Pawn pawn, PawnTable table) {
		}

		public override int GetMinWidth(PawnTable table) {
			return ManageRulesetButtonWidth;
		}

		public override int GetMaxWidth(PawnTable table) {
			return ManageRulesetButtonWidth;
		}

		public override int GetOptimalWidth(PawnTable table) {
			return ManageRulesetButtonWidth;
		}

		public override int GetMinCellHeight(Pawn pawn) {
			return 0;
		}
	}
}

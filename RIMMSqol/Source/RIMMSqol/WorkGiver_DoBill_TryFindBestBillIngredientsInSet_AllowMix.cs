/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 12.07.2018
 * Time: 05:25
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of WorkGiver_DoBill_TryFindBestBillIngredientsInSet_AllowMix.
	/// </summary>
	[HarmonyPatch(typeof(WorkGiver_DoBill))]
	[HarmonyPatch("TryFindBestBillIngredientsInSet_AllowMix")]
	[HarmonyPatchNamespace("DoBillIngredientOverflow")]
	static class WorkGiver_DoBill_TryFindBestBillIngredientsInSet_AllowMix
	{
		static bool Prefix(List<Thing> availableThings, Bill bill, List<ThingCount> chosen, ref bool __result) 
		{
			__result = TryFindBestBillIngredientsInSet_AllowMix(availableThings,bill,chosen);
			return false;
		}
		
		static bool TryFindBestBillIngredientsInSet_AllowMix(List<Thing> availableThings, Bill bill, List<ThingCount> chosen)
		{
			chosen.Clear();
			List<ThingCount> chosenInThisStep = new List<ThingCount>();
			for (int i = 0; i < bill.recipe.ingredients.Count; i++) {
				IngredientCount ingredientCount = bill.recipe.ingredients[i];
				float num = ingredientCount.GetBaseCount();
				chosenInThisStep.Clear();
				for (int j = 0; j < availableThings.Count; j++) {
					Thing thing = availableThings[j];
					if (ingredientCount.filter.Allows(thing)) {
						if (ingredientCount.IsFixedIngredient || bill.ingredientFilter.Allows(thing)) {
							float num2 = bill.recipe.IngredientValueGetter.ValuePerUnitOf(thing.def);
							int num3 = Mathf.Min(Mathf.CeilToInt(num / num2), thing.stackCount);
							ThingCountUtility.AddToList(chosen, thing, num3);
							ThingCountUtility.AddToList(chosenInThisStep, thing, num3);
							num -= (float)num3 * num2;
							if (num <= 0.0001f) {
								break;
							}
						}
					}
				}
				if (num > 0.0001f) {
					return false;
				}
				//patch: do another pass eliminating as many resources as possible. starting with the biggest resources, 
				//have we allocated more than we may need? And at least 2 different things.
				if ( num < -0.0001f && chosenInThisStep.Count > 1 ) {
					num = -num;
					chosenInThisStep.SortByDescending(ta=>bill.recipe.IngredientValueGetter.ValuePerUnitOf(ta.Thing.def));
					for ( int j = 0; j < chosenInThisStep.Count && num > 0.0001f; j++ ) {
						ThingCount ta = chosenInThisStep[j];
						float valuePerUnit = bill.recipe.IngredientValueGetter.ValuePerUnitOf(ta.Thing.def);
						//can we remove at least one instance of the current ingredient?
						if ( valuePerUnit < num ) {
							//determine how many instances we can remove
							int instancesToRemove = 0;
							while ( valuePerUnit < num && instancesToRemove < ta.Count ) {
								num -= valuePerUnit - 0.0001f;//adding a small amount since the precision loss of float causes problems where subtraction leads to 0.04999 instead of 0.05
								instancesToRemove++;
							}
							if ( instancesToRemove != 0 ) ThingCountUtility.AddToList(chosen, ta.Thing, -instancesToRemove);
						}
					}
				}
				chosen.RemoveAll(ta=>ta.Count<=0);
			}
			return true;
		}
	}
}

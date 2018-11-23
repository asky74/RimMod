/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 12.07.2018
 * Time: 04:37
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RIMMSqol.performance.workgiver_dobill
{
	/// <summary>
	/// Description of WorkGiver_DoBill_TryFindBestBillIngredients.
	/// </summary>
	[HarmonyPatch(typeof(WorkGiver_DoBill))]
	[HarmonyPatch("TryFindBestBillIngredients")]
	[HarmonyPatchNamespace("DoBillIngredientSearchRadius")]
	public static class WorkGiver_DoBill_TryFindBestBillIngredients
	{
		delegate bool TryFindBestBillIngredientsInSetType(List<Thing> availableThings, Bill bill, List<ThingCount> chosen);
		static TryFindBestBillIngredientsInSetType TryFindBestBillIngredientsInSet;
		
		delegate IntVec3 GetBillGiverRootCellType(Thing billGiver, Pawn forPawn);
		static GetBillGiverRootCellType GetBillGiverRootCell;
		
		delegate void MakeIngredientsListInProcessingOrderType(List<IngredientCount> ingredientsOrdered, Bill bill);
		static MakeIngredientsListInProcessingOrderType MakeIngredientsListInProcessingOrder;
		
		delegate void AddEveryMedicineToRelevantThingsType(Pawn pawn, Thing billGiver, List<Thing> relevantThings, Predicate<Thing> baseValidator, Map map);
		static AddEveryMedicineToRelevantThingsType AddEveryMedicineToRelevantThings;
		
		//instances come from the original class to make it more vanilla friendly at least ingredientsOrdered is being used outside the methods context
		static List<IngredientCount> ingredientsOrdered;
		static List<Thing> newRelevantThings, relevantThings;
		static HashSet<Thing> processedThings;
		
		static bool Prefix(Bill bill, Pawn pawn, Thing billGiver, List<ThingCount> chosen, ref bool __result)
		{
			if ( ingredientsOrdered == null ) ingredientsOrdered = (List<IngredientCount>)typeof(WorkGiver_DoBill).GetField("ingredientsOrdered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
			if ( newRelevantThings == null ) newRelevantThings = (List<Thing>)typeof(WorkGiver_DoBill).GetField("newRelevantThings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
			if ( relevantThings == null ) relevantThings = (List<Thing>)typeof(WorkGiver_DoBill).GetField("relevantThings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
			if ( processedThings == null ) processedThings = (HashSet<Thing>)typeof(WorkGiver_DoBill).GetField("processedThings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
			
			if ( TryFindBestBillIngredientsInSet == null ) 
				TryFindBestBillIngredientsInSet = (TryFindBestBillIngredientsInSetType)Delegate.CreateDelegate(typeof(TryFindBestBillIngredientsInSetType), null, typeof(WorkGiver_DoBill).GetMethod("TryFindBestBillIngredientsInSet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));
			if ( GetBillGiverRootCell == null )
				GetBillGiverRootCell = (GetBillGiverRootCellType)Delegate.CreateDelegate(typeof(GetBillGiverRootCellType), null, typeof(WorkGiver_DoBill).GetMethod("GetBillGiverRootCell", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));
			if ( MakeIngredientsListInProcessingOrder == null )
				MakeIngredientsListInProcessingOrder = (MakeIngredientsListInProcessingOrderType)Delegate.CreateDelegate(typeof(MakeIngredientsListInProcessingOrderType), null, typeof(WorkGiver_DoBill).GetMethod("MakeIngredientsListInProcessingOrder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));
			if ( AddEveryMedicineToRelevantThings == null )
				AddEveryMedicineToRelevantThings = (AddEveryMedicineToRelevantThingsType)Delegate.CreateDelegate(typeof(AddEveryMedicineToRelevantThingsType), null, typeof(WorkGiver_DoBill).GetMethod("AddEveryMedicineToRelevantThings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)); 
			
			chosen.Clear();
			newRelevantThings.Clear();
			if (bill.recipe.ingredients.Count == 0)
			{
				__result = true;
				return false;
			}
			IntVec3 rootCell = GetBillGiverRootCell(billGiver, pawn);
			Region rootReg = rootCell.GetRegion(pawn.Map, RegionType.Set_Passable);
			if (rootReg == null)
			{
				__result = false;
				return false;
			}
			
			//TODO Ingredient list is completely based on RecipeDef and can be cached however ingredientsOrdered is used to transfer data so the static pointer must be set it is not modified outside the MakeIng... call
			MakeIngredientsListInProcessingOrder(ingredientsOrdered, bill);
			relevantThings.Clear();
			processedThings.Clear();
			bool foundAll = false;
			Predicate<Thing> baseValidator = (Thing t) => t.Spawned && !t.IsForbidden(pawn) && (float)(t.Position - billGiver.Position).LengthHorizontalSquared < bill.ingredientSearchRadius * bill.ingredientSearchRadius && bill.IsFixedOrAllowedIngredient(t) && bill.recipe.ingredients.Any((IngredientCount ingNeed) => ingNeed.filter.Allows(t)) && pawn.CanReserve(t, 1, -1, null, false);
			bool billGiverIsPawn = billGiver is Pawn;
			if (billGiverIsPawn)
			{
				AddEveryMedicineToRelevantThings(pawn, billGiver, relevantThings, baseValidator, pawn.Map);
				if (TryFindBestBillIngredientsInSet(relevantThings, bill, chosen))
				{
					__result = true;
					return false;
				}
			}
			TraverseParms traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false);
			RegionEntryPredicate entryCondition;
			//set to maximum so no restriction
			if ( Math.Abs(Bill.MaxIngredientSearchRadius - bill.ingredientSearchRadius) < 1f ) entryCondition = (Region from, Region r) => r.Allows(traverseParams, false);
			else {
				int posX = billGiver.Position.x, posY = billGiver.Position.z;
				float ingredientSearchRadius = bill.ingredientSearchRadius;
				float ingredientSearchRadiusSquared = ingredientSearchRadius * ingredientSearchRadius;
				entryCondition = (Region from, Region r) => {
					if ( !r.Allows(traverseParams, false) ) return false;
					//check if the distance restriction touches the regions rect.
					CellRect rect = r.extentsClose;
				    //integer only calculation
				    int DeltaX = Math.Abs(posX - Math.Max(rect.minX, Math.Min(posX, rect.maxX)));
					if ( DeltaX > ingredientSearchRadius ) return false;
					int DeltaY = Math.Abs(posY - Math.Max(rect.minZ, Math.Min(posY, rect.maxZ)));
					if ( DeltaY > ingredientSearchRadius ) return false;
					return (DeltaX * DeltaX + DeltaY * DeltaY) <= (ingredientSearchRadiusSquared);
				};
			}
			int adjacentRegionsAvailable = rootReg.Neighbors.Count((Region region) => entryCondition(rootReg, region));
			int regionsProcessed = 0;
			processedThings.AddRange(relevantThings);
			RegionProcessor regionProcessor = delegate(Region r)
			{
				List<Thing> list = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
				for (int i = 0; i < list.Count; i++)
				{
					Thing thing = list[i];
					if (!processedThings.Contains(thing))
					{
						//This is just retarded by the defs, they check if its touch if so ok, if its oncell they check if a cell of the 
						//thing is in the region which was used to retrieve the thing in the first place the only time this could do something is
						//if the haulable thing had an interaction cell which none have.
						/*if (ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, PathEndMode.ClosestTouch, pawn))
						{*/
							if (baseValidator(thing) && (!thing.def.IsMedicine || !billGiverIsPawn)) {
								newRelevantThings.Add(thing);
								processedThings.Add(thing);
							}
						//}
					}
				}
				regionsProcessed++;
				if (newRelevantThings.Count > 0 && regionsProcessed > adjacentRegionsAvailable)
				{
					Comparison<Thing> comparison = delegate(Thing t1, Thing t2)
					{
						float num = (float)(t1.Position - rootCell).LengthHorizontalSquared;
						float value = (float)(t2.Position - rootCell).LengthHorizontalSquared;
						return num.CompareTo(value);
					};
					//doesnt work since the region traversal is not taking place ordered by distance. Thats why its waiting for all immediately adjacent regions but past that it wont work.
					newRelevantThings.Sort(comparison);
					relevantThings.AddRange(newRelevantThings);
					newRelevantThings.Clear();
					//TODO all things are processed if something new was found, coding it as an incremental algorithm could help. Also the filter for ingredients should no longer collect ingredients whose quota has been reached
					if (TryFindBestBillIngredientsInSet(relevantThings, bill, chosen))
					{
						foundAll = true;
						return true;
					}
				}
				return false;
			};
			RegionTraverser.BreadthFirstTraverse(rootReg, entryCondition, regionProcessor, 99999, RegionType.Set_Passable);
			relevantThings.Clear();
			newRelevantThings.Clear();
			__result = foundAll;
			return false;
		}
	}
}

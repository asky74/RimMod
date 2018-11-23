/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 25.08.2017
 * Time: 10:37
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RIMMSqol
{
	/// <summary>
	/// Description of QOLModGameComponent.
	/// </summary>
	public class QOLModGameComponent : GameComponent
	{
		public float pooledPoints;
		public bool 
			restrictEatingIngredients,
			restrictEatingPreservedFood, 
			restrictMoodBoostFood, 
			restrictAscetics, 
			restrictAnimals, 
			restrictPrisoners, 
			restrictPrisonerRecruits;
		
		public QOLModGameComponent() {
		}
		
		public QOLModGameComponent(Game game)
		{
		}
		
		public override void ExposeData() {
			Scribe_Values.Look<float>(ref this.pooledPoints, "GCCuMPoints", 0f);
			
			Scribe_Values.Look<bool>(ref this.restrictEatingIngredients, "restrictEatingIngredients", false);
			Scribe_Values.Look<bool>(ref this.restrictEatingPreservedFood, "restrictEatingPreservedFood", false);
			Scribe_Values.Look<bool>(ref this.restrictMoodBoostFood, "restrictMoodBoostFood", false);
			Scribe_Values.Look<bool>(ref this.restrictAscetics, "restrictAscetics", false);
			Scribe_Values.Look<bool>(ref this.restrictAnimals, "restrictAnimals", false);
			Scribe_Values.Look<bool>(ref this.restrictPrisoners, "restrictPrisoners", false);
			Scribe_Values.Look<bool>(ref this.restrictPrisonerRecruits, "restrictPrisonerRecruits", false);
			
			if (Scribe.mode == LoadSaveMode.PostLoadInit) {
				SynchronizeRules();
			}
		}
		
		protected bool HasThingCategory(ThingDef t, ThingCategoryDef c) {
			return t.thingCategories != null && t.thingCategories.Any(tc=>tc.Equals(c) || tc.Parents.Contains(c));
		}
		
		public void SynchronizeRules() {
			/*
			 * 2. Restrict non starving faction members from eating preserved food to allow stockpiling travel rations.
			 * 3. Restrict non starving colonists with mood above x from eating meals better than simple.
			 * 4. Restrict non starving colonists with ascetics trait from eating better than simple.
			 * 5. Restrict non starving faction animals from eating meals.
			 * 6. Restrict non starving non colonist prisoners not marked for recruiting from eating meals better than simple.
			 * 7. Restrict non starving non colonist prisoners marked for recruiting with mood above 0.8f from eating meals better than simple. (mood 0.5,1.0f -> 1.0,1.5f)
			 * 
			 * taming only uses raw tasty
			 */
			if ( restrictEatingIngredients ) {
				ThingForbiddenContext.SetRule("QOL.restrictEatingIngredients", new []{"QOL.food"},(con)=> {
				                              	IntentContext_Food icf = con as IntentContext_Food;
				                              	if ( icf != null ) return icf.eater.IsColonist;
				                              	return false;
				                              },
				                              DefDatabase<ThingDef>.AllDefsListForReading.Where(t=>t.IsNutritionGivingIngestible && !t.ingestible.IsMeal && 
				                                                                                (!HasThingCategory(t,ThingCategoryDefOf.Foods)||HasThingCategory(t,DefDatabase<ThingCategoryDef>.GetNamed("FoodRaw"))) &&
				                                                								(t.plant == null || t.plant.Harvestable)));
				ThingForbiddenContext.SetRule("QOL.restrictEatingIngredientsNonHumanlike", new []{"QOL.food"},(con)=> {
				                              	IntentContext_Food icf = con as IntentContext_Food;
				                              	if ( icf != null ) return icf.eater.Faction != null && icf.eater.Faction.IsPlayer && icf.eater.RaceProps.Animal;
				                              	return false;
				                              },
				                              DefDatabase<ThingDef>.AllDefsListForReading.Where(t=>t.IsNutritionGivingIngestible && !t.ingestible.IsMeal && 
				                                                                                (!HasThingCategory(t,ThingCategoryDefOf.Foods)||HasThingCategory(t,DefDatabase<ThingCategoryDef>.GetNamed("FoodRaw"))) &&
				                                                                                !(t.IsCorpse && t.race != null && t.race.Humanlike) && 
				                                                                                (t.plant == null || t.plant.Harvestable)));
				
			} else {
				ThingForbiddenContext.RemoveRule("QOL.restrictEatingIngredients");
				ThingForbiddenContext.RemoveRule("QOL.restrictEatingIngredientsNonHumanlike");
			}
			if ( restrictEatingPreservedFood ) {
				ThingForbiddenContext.SetRule("QOL.restrictEatingPreservedFood", new []{"QOL.food"}, (con)=> {
				                              	IntentContext_Food icf = con as IntentContext_Food;
				                              	if ( icf != null ) return icf.eater.needs.food.CurLevelPercentage > icf.eater.needs.food.PercentageThreshUrgentlyHungry && 
				                              		icf.getter.Faction != null && icf.getter.Faction.IsPlayer;
				                              	return false;
				                              },
				                             DefDatabase<ThingDef>.AllDefsListForReading.Where(t=>t.IsNutritionGivingIngestible && !t.HasComp(typeof(CompRottable))));
			} else ThingForbiddenContext.RemoveRule("QOL.restrictEatingPreservedFood");
			if ( restrictMoodBoostFood ) {
				ThingForbiddenContext.SetRule("QOL.restrictMoodBoostFood", new []{"QOL.food"}, (con)=> {
				                              	IntentContext_Food icf = con as IntentContext_Food;
				                              	if ( icf != null ) return icf.eater.IsColonist && icf.eater.needs.food.CurLevelPercentage > icf.eater.needs.food.PercentageThreshUrgentlyHungry &&
				                              		icf.eater.needs.mood.CurLevel > icf.eater.mindState.mentalBreaker.BreakThresholdMinor;
				                              	return false;
				                              },
				                             DefDatabase<ThingDef>.AllDefsListForReading.Where(t=>t.IsNutritionGivingIngestible && t.ingestible.preferability > FoodPreferability.MealSimple));
			} else ThingForbiddenContext.RemoveRule("QOL.restrictMoodBoostFood");
			if ( restrictAscetics ) {
				ThingForbiddenContext.SetRule("QOL.restrictAscetics", new []{"QOL.food"}, (con)=> {
				                              	IntentContext_Food icf = con as IntentContext_Food;
				                              	if ( icf != null ) return icf.eater.IsColonist && icf.eater.needs.food.CurLevelPercentage > icf.eater.needs.food.PercentageThreshUrgentlyHungry &&
				                              		icf.eater.story != null && icf.eater.story.traits.HasTrait(TraitDefOf.Ascetic);
				                              	return false;
				                              },
				                             DefDatabase<ThingDef>.AllDefsListForReading.Where(t=>t.IsNutritionGivingIngestible && t.ingestible.preferability > FoodPreferability.MealSimple));
			} else ThingForbiddenContext.RemoveRule("QOL.restrictAscetics");
			if ( restrictAnimals ) {
				ThingForbiddenContext.SetRule("QOL.restrictAnimals", new []{"QOL.food"}, (con)=> {
				                              	IntentContext_Food icf = con as IntentContext_Food;
				                              	if ( icf != null ) return icf.eater.Faction != null && icf.eater.Faction.IsPlayer && icf.eater.RaceProps.Animal && 
				                              		icf.eater.needs.food.CurLevelPercentage > icf.eater.needs.food.PercentageThreshUrgentlyHungry;
				                              	return false;
				                              },
				                             DefDatabase<ThingDef>.AllDefsListForReading.Where(t=>t.IsNutritionGivingIngestible && t.ingestible.IsMeal));
			} else ThingForbiddenContext.RemoveRule("QOL.restrictAnimals");
			if ( restrictPrisoners ) {
				ThingForbiddenContext.SetRule("QOL.restrictPrisoners", new []{"QOL.food"}, (con)=> {
				                              	IntentContext_Food icf = con as IntentContext_Food;
				                              	if ( icf != null ) return icf.eater.IsPrisoner && !icf.eater.IsColonist && icf.eater.guest.interactionMode != PrisonerInteractionModeDefOf.AttemptRecruit && 
				                              		icf.eater.needs.food.CurLevelPercentage > icf.eater.needs.food.PercentageThreshUrgentlyHungry;
				                              	return false;
				                              },
				                             DefDatabase<ThingDef>.AllDefsListForReading.Where(t=>t.IsNutritionGivingIngestible && t.ingestible.preferability > FoodPreferability.MealSimple));
			} else ThingForbiddenContext.RemoveRule("QOL.restrictPrisoners");
			if ( restrictPrisonerRecruits ) {
				ThingForbiddenContext.SetRule("QOL.restrictPrisonerRecruits", new []{"QOL.food"}, (con)=> {
				                              	IntentContext_Food icf = con as IntentContext_Food;
				                              	if ( icf != null ) return icf.eater.IsPrisoner && !icf.eater.IsColonist && icf.eater.guest.interactionMode == PrisonerInteractionModeDefOf.AttemptRecruit && 
				                              		icf.eater.needs.food.CurLevelPercentage > icf.eater.needs.food.PercentageThreshUrgentlyHungry && icf.eater.needs.mood.CurLevel > 0.8f;
				                              	return false;
				                              },
				                              DefDatabase<ThingDef>.AllDefsListForReading.Where(t=>t.IsNutritionGivingIngestible && t.ingestible.preferability > FoodPreferability.MealSimple));
			} else ThingForbiddenContext.RemoveRule("QOL.restrictPrisonerRecruits");
		}
	}
}

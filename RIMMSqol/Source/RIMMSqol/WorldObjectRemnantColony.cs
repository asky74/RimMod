/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 17.08.2017
 * Time: 00:42
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RIMMSqol.renderers;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace RIMMSqol
{
	/// <summary>
	/// Description of WorldObjectRemnantColony.
	/// </summary>
	public class WorldObjectRemnantColony : WorldObject, ITrader, IThingHolder, ICommunicable
	{
		public Faction GetFaction()
		{
			return Faction;
		}
		
		protected ThingOwner<Thing> stock;
		protected string settlementName;
		protected List<SkillLevel> maxSkillLevels;
		protected List<string> pawnNames;
		protected List<Action<Settlement>> onSettlementChanged;
		
		public static IEnumerable<Pawn> GetPawnsFromSettlementForRemnantColony(Settlement settlement) {
			if ( settlement == null || settlement.Map == null ) {
				return Enumerable.Empty<Pawn>();
			}
			return settlement.Map.mapPawns.FreeColonistsSpawned;
		}
		
		public static bool CanCreateRemnantColony(Settlement settlement) {
			if ( settlement == null || settlement.Map == null || !settlement.Faction.IsPlayer ) {
				return false;
			}
			
			return GetPawnsFromSettlementForRemnantColony(settlement).Any();
		}
		
		public WorldObjectRemnantColony()
		{
		}
		
		public override void PostMake() {
			onSettlementChanged = new List<Action<Settlement>>();
			base.PostMake();
		}
		
		protected override void PositionChanged()
		{
			base.PositionChanged();
			Settlement settlement = Find.World.worldObjects.WorldObjectAt<Settlement>(Tile);
			settlementChanged(settlement);
		}
		
		protected void settlementChanged(Settlement settlement) {
			if ( settlement == null ) return;
			
			//grab items for trade stock from settlement
			stock = new ThingOwner<Thing>(this);
			foreach ( StockGenerator generator in TraderKind.stockGenerators ) {
				IEnumerable<Thing> things = generator.GenerateThings(settlement.Tile);
				foreach (Thing t in things ) {
					GiveSoldThingToTrader(t,t.stackCount,null);
				}
			}
			//grab name from settlement
			if ( settlement != null ) {
				this.settlementName = settlement.TraderName;
			} else {
				this.settlementName = "unknown";
			}
			//A non null list of pawns from the settlement that we evaluate as workforce
			List<Pawn> pawnsToInspect = new List<Pawn>();
			pawnsToInspect.AddRange(settlement.Map.mapPawns.FreeColonistsSpawned);
			
			//grab the highest skill levels from the pawns in the settlement for each skill.
			maxSkillLevels = new List<SkillLevel>();
			foreach ( SkillDef skillDef in DefDatabase<SkillDef>.AllDefsListForReading ) {
				SkillLevel skr = buildDefaultMaxSkillLevel(skillDef);
				maxSkillLevels.Add(skr);
				foreach ( Pawn p in pawnsToInspect ) {
					skr.level = Math.Max(skr.level,p.skills.GetSkill(skillDef).Level);
				}
			}
			
			//Names from the workers
			this.pawnNames = (from Pawn p in pawnsToInspect select p.Name.ToStringFull).ToList();
			
			//collect the CuM points for the pawns and add them to the pool
			float BC = QOLMod.getBaseCost();
			float points = 0;
			foreach ( Pawn p in pawnsToInspect ) {
				points += p.getCuMWorth();
			}
			Current.Game.GetComponent<QOLModGameComponent>().pooledPoints += points * QOLMod.getCumPointRemnantsToPoolConversionFactor();
			
			foreach ( Action<Settlement> a in onSettlementChanged ) {
				a(settlement);
			}
		}
		
		public override void ExposeData()
		{
			onSettlementChanged = new List<Action<Settlement>>();
			base.ExposeData();
			//saving information about the colony
			Scribe_Deep.Look<ThingOwner<Thing>>(ref this.stock, "stock", new object[]{this});
			Scribe_Values.Look<string>(ref this.settlementName, "settlementName", "unknown");
			Scribe_Collections.Look<SkillLevel>(ref this.maxSkillLevels, "maxSkillLevels", LookMode.Deep);
			Scribe_Collections.Look<string>(ref this.pawnNames, "pawnNames");
			
			//assuring we have a valid and complete state
			if ( Scribe.mode != LoadSaveMode.Saving ) {
				if ( this.pawnNames == null ) pawnNames = new List<string>();
				if ( this.maxSkillLevels == null ) this.maxSkillLevels = new List<SkillLevel>();
				foreach ( SkillDef skillDef in DefDatabase<SkillDef>.AllDefsListForReading ) {
					bool found = false;
					foreach ( SkillLevel skr in maxSkillLevels ) {
						if ( skr.def == skillDef ) {
							found = true;
							break;
						}
					}
					if ( !found ) {
						maxSkillLevels.Add(buildDefaultMaxSkillLevel(skillDef));
					}
				}
			}
		}
		
		protected SkillLevel buildDefaultMaxSkillLevel(SkillDef skill) {
			SkillLevel skr = new SkillLevel();
			skr.def = skill;
			skr.level = 10;
			return skr;
		}

		#region ICommunicable
		public string GetCallLabel()
		{
			return TraderName;
		}
		public string GetInfoText()
		{
			return GetDescription();
		}
		public void TryOpenComms(Pawn negotiator)
		{
			if (!this.CanTradeNow)
			{
				return;
			}
			ModelPlaceOrder modelPlaceOrder = new ModelPlaceOrder(this);
			
			//maybe a grid or a dialog featuring my own flow components.
			Dictionary<string,PageRenderer> pages = new Dictionary<string, PageRenderer>();
			pages.Add("menu",new PageRenderer((f=>GetCallLabel()),null,null)
			          .AddChild(new RowLayoutRenderer()
		                    .AddChild(new ColumnLayoutRenderer()
	                              .AddChild(new ButtonTextRenderer("Trade", (f => Find.WindowStack.Add(new Dialog_Trade(negotiator, this)))))
	                              //Placing an order requires selecting stuff for the item to make(could be a stuff category or fixed ingredients) more complicated things have filters
	                              //Maybe using the workbench interface to define the possible materials? But then we need to pick from storage etc.. simulating the whole chain.
	                              //Only allow recipes with category item.
	                              .AddChild(new ButtonTextRenderer("Place order", (f => { modelPlaceOrder.resources = ColonyThingsWillingToBuy(negotiator); f.navigate("placeOrder"); })))
	                              //.AddChild(new ButtonTextRenderer("Claim order", (f => f.navigate("statistics"))))
	                              .AddChild(new ButtonTextRenderer("Statistics", (f => f.navigate("statistics"))))
	                              .AddChild(new ButtonTextRenderer("Rename colony", (f => f.navigate("renameColony"))))
	                              .AddChild(new ButtonTextRenderer("Remove colony", (f => Find.WindowStack.Add(new Dialog_MessageBox("This is irreversible! Are you sure?","Remove",delegate{
                                     	Find.WorldObjects.Remove(this);
                                     	//TODO When removing a colony we have the option to remove the housed pawns for good(including their relations) or we keep them around.
                                 	},"Cancel",null,"Remove Remnant Colony", true)))))
	                             )));
			
			pages.Add("statistics", new PageRenderer((f=>"Statistics"), null, ()=>"menu")
					.AddChild(new RowLayoutRenderer()
                    	.AddChild(new ColumnLayoutRenderer()
                        	.AddChild(new LabelRenderer("Inhabitants")))
	                    .AddChild(new ColumnLayoutRenderer()
				        	.AddChild(new ListRenderer(pawnNames, 200f+QOLMod.VerticalScrollbarWidth(),QOLMod.LineHeight(GameFont.Small)*5)
                            	.AddChild(new LabelRenderer((f => ((string)((IterationItem)f.pageScope["curItem"]).curItem)), 200f))))
	                    .AddChild(new ColumnLayoutRenderer()
                        	.AddChild(new LabelRenderer("Max skill levels")))
	                    .AddChild(new ColumnLayoutRenderer()
				        	.AddChild(new ListRenderer(maxSkillLevels, 400f+QOLMod.VerticalScrollbarWidth(),QOLMod.LineHeight(GameFont.Small)*5)
                            	.AddChild(new LabelRenderer((f => ((SkillLevel)((IterationItem)f.pageScope["curItem"]).curItem).def.label), 200f))
                            	.AddChild(new LabelRenderer((f => ((SkillLevel)((IterationItem)f.pageScope["curItem"]).curItem).level.ToString()), 200f))))));
			
			pages.Add("renameColony", new PageRenderer((f => "Rename colony"), null, ()=>"menu")
				          .AddChild(new RowLayoutRenderer()
				                    .AddChild(new ColumnLayoutRenderer()
					                    .AddChild(new LabelRenderer("Name", 200f))
					                    .AddChild(new EditTextRenderer((f => settlementName), (f,v) => settlementName = v, 200f)))
				                   	));
			
			pages.Add("placeOrder", new PageRenderer((f => "Place order"), null, ()=>"menu")
				          .AddChild(new RowLayoutRenderer()
				                    .AddChild(new ColumnLayoutRenderer()
					                    .AddChild(new LabelRenderer("Recipe", 200f))
					                    .AddChild(new ButtonTextRenderer(f=>modelPlaceOrder.recipe == null ? "Choose" : modelPlaceOrder.recipe.label, 
					                                                     f=>Find.WindowStack.Add(
					                                                     	new Dialog_Select<RecipeDef>(def=>{modelPlaceOrder.recipe = def; return true;},
					                                                     	                             DefDatabase<RecipeDef>.AllDefsListForReading
					                                                     	                             //only recipes that produce something and only recipes that can be crafted with the tradeable resources(e.g. chunks arent launchable and cannot be used)
					                                                     	                             .Where(d=>!d.products.NullOrEmpty()/*&&modelPlaceOrder.HasIngredients(d)*/)
					                                                     	                             	.OrderBy(d=>d.label),
					                                                     	                             d=>d.label)
					                                                     ), GameFont.Small, 200f)))
			                    	.AddChild(new ColumnLayoutRenderer()
			                              .AddChild(new LabelRenderer("Ingredient", 200f, GameFont.Small, UnityEngine.TextAnchor.MiddleCenter))
			                              .AddChild(new LabelRenderer("Amount", 100f, GameFont.Small, UnityEngine.TextAnchor.MiddleCenter))
			                              .AddChild(new LabelRenderer("In Stock", 100f, GameFont.Small, UnityEngine.TextAnchor.MiddleCenter)))
			                    	.AddChild(new ColumnLayoutRenderer()
							        	.AddChild(new ListRenderer(modelPlaceOrder.ingredients, 400f+QOLMod.VerticalScrollbarWidth(),QOLMod.LineHeight(GameFont.Small)*5)
			                                        .AddChild(new ButtonTextRenderer(f => ((Pair<ThingDef, IngredientCount>)((IterationItem)f.pageScope["curItem"]).curItem).Key.label,
			                                                                         (f=>{f.pageScope["selectItem"] = ((IterationItem)f.pageScope["curItem"]).curItem; Find.WindowStack.Add(new Dialog_SelectDef<ThingDef>(def=>{
                                                                                        	((Pair<ThingDef,IngredientCount>)f.pageScope["selectItem"]).Key = def; modelPlaceOrder.updateStuff(); return true;
                                                    	},((Pair<ThingDef,IngredientCount>)f.pageScope["selectItem"]).Value.filter.AllowedThingDefs));}), GameFont.Small, 200f))
			                            	.AddChild(new LabelRenderer((f => {
            	                             	Pair<ThingDef,IngredientCount> p = ((Pair<ThingDef,IngredientCount>)((IterationItem)f.pageScope["curItem"]).curItem);
            	                             	return p.Value.CountRequiredOfFor(p.Key, modelPlaceOrder.recipe).ToString();
            	                             }), 100f, GameFont.Small, UnityEngine.TextAnchor.MiddleRight))
		                                    .AddChild(new LabelRenderer((f => modelPlaceOrder.stockCount(((Pair<ThingDef,IngredientCount>)((IterationItem)f.pageScope["curItem"]).curItem).Key).ToString()), 100f, GameFont.Small, UnityEngine.TextAnchor.MiddleRight))))
			                    	.AddChild(new ColumnLayoutRenderer()
			                              .AddChild(new LabelRenderer("Quality", 100f, GameFont.Small, UnityEngine.TextAnchor.MiddleCenter))
			                              .AddChild(new LabelRenderer("Material", 150f, GameFont.Small, UnityEngine.TextAnchor.MiddleCenter))
			                              .AddChild(new LabelRenderer("Product", 150f, GameFont.Small, UnityEngine.TextAnchor.MiddleCenter)))
			                    	.AddChild(new ColumnLayoutRenderer()
							        	.AddChild(new ListRenderer(modelPlaceOrder.products, 400f+QOLMod.VerticalScrollbarWidth(),QOLMod.LineHeight(GameFont.Small)*5)
			                                        .AddChild(new HideRenderer(f=>!((ProductConfig)((IterationItem)f.pageScope["curItem"]).curItem).HasQuality(),
	                                                                   new ButtonTextRenderer(f => ((ProductConfig)((IterationItem)f.pageScope["curItem"]).curItem).quality.GetLabel(),
                                                 				(f=>{f.pageScope["selectItem"] = ((IterationItem)f.pageScope["curItem"]).curItem; Find.WindowStack.Add(new Dialog_Select<QualityCategory>(q=>{
                                                         			((ProductConfig)f.pageScope["selectItem"]).quality = q; modelPlaceOrder.updateStuff(); return true;
                                                 				}, modelPlaceOrder.allowedQualityCategories, d=>Enum.GetName(typeof(QualityCategory),d)));}), GameFont.Small, 100f)))
			                                        
			                                        .AddChild(new LabelRenderer(f=>{ThingDef stuff = ((ProductConfig)((IterationItem)f.pageScope["curItem"]).curItem).stuff; return stuff != null ? stuff.label : "";}, 150f, GameFont.Small, UnityEngine.TextAnchor.MiddleCenter))
			                                        
					                            	.AddChild(new LabelRenderer((f => {
		            	                             	ProductConfig p = ((ProductConfig)((IterationItem)f.pageScope["curItem"]).curItem);
		            	                             	return p.countClass.thingDef.label + (p.countClass.count > 1 ? "(" + p.countClass.count.ToString() + ")" : "");
		            	                             }), 150f, GameFont.Small, UnityEngine.TextAnchor.MiddleCenter))))
			                    //Display the currency count from the stock 
			                    	.AddChild(new ColumnLayoutRenderer()
			                    	        .AddChild(new LabelRenderer("Cost", 200f))
			                    	        .AddChild(new LabelRenderer(f=>((ModelPlaceOrder)f.flowScope["model"]).cost().ToString()+" ("+modelPlaceOrder.stockCount(ThingDefOf.Silver)+")", 200f)))
			                    	.AddChild(new ColumnLayoutRenderer()
			                              .AddChild(new ButtonTextRenderer("Buy",f=>modelPlaceOrder.buy(negotiator),GameFont.Small, 400f)))
				                   	));
			//A Button to place the order
			
			
			Flow flow = new Flow(pages,"menu"); Enum.GetValues(typeof(QualityCategory));
			flow.flowScope["model"] = modelPlaceOrder;
			
			Find.WindowStack.Add(new Dialog_Flow(flow));
			LessonAutoActivator.TeachOpportunity(ConceptDefOf.BuildOrbitalTradeBeacon, OpportunityType.Critical);
			TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.TradeGoodsMustBeNearBeacon);
		}
		
		public FloatMenuOption CommFloatMenuOption(Building_CommsConsole console, Pawn negotiator)
		{
			return new FloatMenuOption(Label, ()=>console.GiveUseCommsJob(negotiator,this));
		}
		
		protected class ProductConfig {
			public ThingDefCountClass countClass;
			//TODO: stuff and quality should be global for the recipe
			public ThingDef stuff;
			public QualityCategory quality;
			public bool HasQuality() {
				return countClass.thingDef.comps.Any(c=>c.compClass == typeof(CompQuality));
			}
		}
		
		protected class Pair<K,V> {
			public K Key;
			public V Value;
			public Pair(K key, V value) { this.Key = key; this.Value = value; }
		}
		
		protected class ModelPlaceOrder {
			protected WorldObjectRemnantColony colony;
			protected RecipeDef _recipe;
			protected float cachedCost = -1;
			protected int cachedSilver = -1;
			public List<QualityCategory> allowedQualityCategories = new List<QualityCategory>();
			public List<Pair<ThingDef,IngredientCount>> ingredients = new List<Pair<ThingDef, IngredientCount>>();
			public List<ProductConfig> products = new List<ProductConfig>();
			public IEnumerable<Thing> resources;
			public ModelPlaceOrder(WorldObjectRemnantColony colony) {
				this.colony = colony;
			}
			public RecipeDef recipe {get{return _recipe;}set{if ( !value.Equals(_recipe) ) {
						_recipe = value;
						ingredients.Clear();
						products.Clear();
						if ( _recipe != null ) {
							foreach ( IngredientCount ic in recipe.ingredients ) {
								ingredients.Add(new Pair<ThingDef, IngredientCount>(ic.filter.AllowedThingDefs.First(),ic));
							}
							foreach ( ThingDefCountClass product in recipe.products ) {
								ProductConfig pc = new ProductConfig();
								pc.countClass = product;
								pc.stuff = null;
								pc.quality = QualityCategory.Normal;
								products.Add(pc);
							}
							updateStuff();
						}
					}}}
			public int stockCount(ThingDef thing) {
				try {
					return this.resources.Where(t=>t.def.defName == thing.defName).Select(t=>t.stackCount).Aggregate((c1,c2) => c1+c2);
				} catch {
					return 0;
				}
			}
			public bool HasIngredients(RecipeDef d) {
				if ( d == null || d.ingredients == null ) return true;
				foreach ( IngredientCount ic in d.ingredients ) {
					bool hasAtLeastOneIngredient = false;
					foreach ( ThingDef t in ic.filter.AllowedThingDefs ) {
						hasAtLeastOneIngredient = stockCount(t)>=ic.CountRequiredOfFor(t,d);
						if ( hasAtLeastOneIngredient ) break;
					}
					if ( !hasAtLeastOneIngredient && ic.filter.AllowedThingDefs.Any() ) return false;
				}
				return true;
			}
			public bool canAfford() {
				int silverUsedInCrafting = 0;
				foreach ( Pair<ThingDef,IngredientCount> p in ingredients ) {
					if ( stockCount(p.Key) < p.Value.CountRequiredOfFor(p.Key,recipe)) {
						return false;
					} else {
						if ( p.Key == ThingDefOf.Silver ) silverUsedInCrafting += p.Value.CountRequiredOfFor(p.Key,recipe);
					}
				}
				return stockCount(ThingDefOf.Silver) >= cost() + silverUsedInCrafting;
			}
			public void buy(Pawn negotiator) {
				if ( canAfford() ) {
					IEnumerable<ThingDef> thingsToPay = ingredients.Select(p=>p.Key).Concat(new ThingDef[]{ThingDefOf.Silver});
					foreach ( Pair<ThingDef,IngredientCount> p in ingredients ) {
						IEnumerable<Thing> thingsInStock = resources.Where(thingInStock=>thingInStock.def == p.Key).OrderBy(thingInStock=>thingInStock.stackCount);
						int countToGive = p.Value.CountRequiredOfFor(p.Key,recipe);
						foreach ( Thing t in thingsInStock ) {
							int countForStack = Math.Min(t.stackCount,countToGive);
							colony.GiveSoldThingToTrader(t,countForStack,negotiator);
							if ( countToGive <= countForStack ) break;
							countToGive -= countForStack;
						}
					}
					
					IEnumerable<Thing> silverInStock = resources.Where(thingInStock=>thingInStock.def == ThingDefOf.Silver).OrderBy(thingInStock=>thingInStock.stackCount);
					int silverToGive = cost();
					foreach ( Thing t in silverInStock ) {
						int countForStack = Math.Min(t.stackCount,silverToGive);
						colony.GiveSoldThingToTrader(t,countForStack,negotiator);
						if ( silverToGive == countForStack ) break;
						else silverToGive -= countForStack;
					}
					
					foreach ( Thing t in createProducts() ) {
						colony.GiveSoldThingToPlayer(t,t.stackCount,negotiator);
					}
				} else {
					Find.WindowStack.Add(new Dialog_MessageBox("You cannot afford the recipe, ensure all materials and silver are placed under a trade bacon in valid stockpiles.","Ok"));
				}
			}
			public void updateStuff() {
				cachedCost = -1;
				foreach ( ProductConfig product in products ) {
					if ( product.countClass.thingDef.MadeFromStuff ) {
						product.stuff = ingredients.First(p=>p.Key.IsStuff).Key;
					} else {
						product.stuff = null;
					}
				}
				allowedQualityCategories.Clear();
				int workSkillLevel = 10;
				if ( colony.maxSkillLevels != null ) {
					SkillLevel maxSkillLevel = colony.maxSkillLevels.FirstOrDefault(sl=>sl.def == recipe.workSkill);
					if ( maxSkillLevel != null ) workSkillLevel = maxSkillLevel.level;
				}
				if ( workSkillLevel >= 0 ) allowedQualityCategories.Add(QualityCategory.Awful);
				//if ( workSkillLevel >= 5 ) allowedQualityCategories.Add(QualityCategory.Shoddy);
				if ( workSkillLevel >= 8 ) allowedQualityCategories.Add(QualityCategory.Poor);
				if ( workSkillLevel >= 10 ) allowedQualityCategories.Add(QualityCategory.Normal);
				if ( workSkillLevel >= 12 ) allowedQualityCategories.Add(QualityCategory.Good);
				//if ( workSkillLevel >= 14 ) allowedQualityCategories.Add(QualityCategory.Superior);
				if ( workSkillLevel >= 16 ) allowedQualityCategories.Add(QualityCategory.Excellent);
				if ( workSkillLevel >= 18 ) allowedQualityCategories.Add(QualityCategory.Masterwork);
				if ( workSkillLevel >= 20 ) allowedQualityCategories.Add(QualityCategory.Legendary);
			}
			public List<Thing> createProducts() {
				List<Thing> generatedProducts = new List<Thing>();
				foreach ( ProductConfig pc in products ) {
					Thing t;
					if ( pc.countClass.thingDef.MadeFromStuff ) {
						t = ThingMaker.MakeThing(pc.countClass.thingDef, pc.stuff);
					} else {
						t = ThingMaker.MakeThing(pc.countClass.thingDef, null);
					}
					t.stackCount = pc.countClass.count;
					MinifiedThing minifiedThing = t as MinifiedThing;
					CompQuality compQuality = (minifiedThing == null) ? t.TryGetComp<CompQuality>() : minifiedThing.InnerThing.TryGetComp<CompQuality>();
					if ( compQuality != null ) compQuality.SetQuality(pc.quality, ArtGenerationContext.Outsider);
					generatedProducts.Add(t);
				}
				return generatedProducts;
			}
			public int cost() {
				if ( cachedCost < 0 ) {
					cachedCost = 0;
					List<Thing> l = createProducts();
					foreach ( Thing t in l ) {
						cachedCost += t.MarketValue * colony.TraderKind.PriceTypeFor(t.def, TradeAction.PlayerBuys).PriceMultiplier();
					}
				}
				return Mathf.FloorToInt(cachedCost * QOLMod.getRemnantOrderPriceFactor());
			}
		}
		#endregion
		
		#region IThingHolder
		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
		}
		
		public ThingOwner GetDirectlyHeldThings()
		{
			return stock;
		}
		#endregion
		
		#region ITrader
		public IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
		{
			//The result is a list of things that the pawn HAS, that are in a sellable position and that the trader accepts.
			if ( playerNegotiator != null ) {
				if ( playerNegotiator.Map != null ) {
					return TradeUtility.AllLaunchableThingsForTrade(playerNegotiator.Map).Where(t=>TraderKind.WillTrade(t.def));
				} else if ( playerNegotiator.GetCaravan() != null ) {
					return playerNegotiator.GetCaravan().trader.Goods.Where(t=>TraderKind.WillTrade(t.def));
				}
			}
			return new List<Thing>();
		}

		public void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			if ( toGive as Pawn != null ) {
				Log.Error("Slave trade with a remnant colony is not supported!");
				return;
			}
			Thing thing = toGive.SplitOff(countToGive);
			thing.PreTraded(TradeAction.PlayerSells, playerNegotiator, this);
			if (!stock.TryAdd(thing, false)) {
				thing.Destroy(DestroyMode.Vanish);
				Log.Error("Failed to add bought thing to trader stock in remnant colony!");
			}
		}

		public void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			if ( toGive as Pawn != null ) {
				Log.Error("Slave trade with a remnant colony is not supported!");
				return;
			}
			if ( playerNegotiator.Map == null ) {
				Log.Error("Trade with a remnant colony is expected to take place via a com console and not via caravans!");
				return;
			}
			Thing thing = toGive.SplitOff(countToGive);
			thing.PreTraded(TradeAction.PlayerBuys, playerNegotiator, this);
			TradeUtility.SpawnDropPod(DropCellFinder.TradeDropSpot(playerNegotiator.Map), playerNegotiator.Map, thing);
		}

		public TraderKindDef TraderKind {
			get {
				return DefDatabase<TraderKindDef>.GetNamed("RemnantColony");
			}
		}

		public IEnumerable<Thing> Goods {
			get {
				return this.stock.InnerListForReading;
			}
		}

		public int RandomPriceFactorSeed {
			get {
				return 1;
			}
		}

		public string TraderName {
			get {
				return settlementName;
			}
		}

		public bool CanTradeNow {
			get {
				return true;
			}
		}

		public float TradePriceImprovementOffsetForPlayer {
			get {
				return 0f;
			}
		}
		#endregion
	}
	
	public class SkillLevel : IExposable {
		public SkillDef def;
		public int level;
		
		#region IExposable implementation
		public void ExposeData()
		{
			Scribe_Defs.Look<SkillDef>(ref this.def, "def");
			Scribe_Values.Look<int>(ref this.level, "level", 10, false);
		}
		#endregion
	}
}

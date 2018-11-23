/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 31.10.2017
 * Time: 00:06
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Harmony;
using RIMMSqol.genericSettings;
using RIMMSqol.pathfinding;
using RIMMSqol.renderers;
using RimWorld;
using Verse;
using Verse.AI;

namespace RIMMSqol
{
	/// <summary>
	/// Description of SettingsInit.
	/// </summary>
	public static class SettingsInit
	{
		private static bool initialized = false;
		
		public static void init() {
			if ( !initialized ) {	
				generalSettings();
				traitCosts();
				recordFactors();
				powerBuildings();
				mainButtons();
				architectButtons();
				recipes();
				floorsAndTerrain();
				thoughts();
				apparel();
				weapons();
				projectiles();
				plants();
				materials();
				foods();
				
				//materials are split under: ressources raw,textiles, isStuff
				
				//common flushing of caches that gets always applied regardless of changes in settings
				QOLMod.addApplySettingsListener(mod=> {
                	QOLMod.reloadPFAlgorithms();
                	if ( Current.Game != null ) {
						foreach ( Map m in Find.Maps ) {
							//recalculates e.g. beauty ratings of rooms
							//this also resets the temperature. In cases where volcanic winter or other extremes influence the heat this can result in an immediate heating/cooling failure
							//m.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
							//In order to avoid loosing the game state for temperature we trigger recalculation of room stats.
							foreach (Room room in m.regionGrid.allRooms) {
								room.Notify_TerrainChanged();
							}
							//removing cached information about tile-speed for all map cells
							//m.pathGrid.ResetPathGrid(); doing this will reset temperature in conjunction with recalculating path costs
							m.pathGrid.RecalculateAllPerceivedPathCosts();
						}
						//applies a new ordering to the main menu while a game is running
						System.Reflection.FieldInfo fi = Find.MainButtonsRoot.GetType().GetField("allButtonsInOrder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
						fi.SetValue(Find.MainButtonsRoot, (from x in DefDatabase<MainButtonDef>.AllDefs orderby x.order select x).ToList<MainButtonDef>());
                	}});
				
				SettingsStorage.initializeSettingsInstances();
				
				foreach ( SettingsProperties props in SettingsStorage.getAllSettingsProperties() ) {
					props.fields.Sort((a,b)=>a.order.CompareTo(b.order));
				}
				
				//create language file output in the console
				/*
				StringBuilder sb = new StringBuilder();
				foreach ( SettingsProperties props in SettingsStorage.getAllSettingsProperties() ) {
					foreach ( ISettingsFieldProperties field in props.fields ) {
						string tagName = props.id+"."+field.id;
						sb.Append("<"+tagName+">"+field.label+"</"+tagName+">\n");
						tagName = tagName+".tooltip";
						sb.Append("<"+tagName+">"+field.labelTooltip+"</"+tagName+">\n\n");
					}
				}
				Log.Message(sb.ToString());
				*/
				
				LanguageChangeListener.translationActions.Add(()=>{
                  	foreach ( SettingsProperties props in SettingsStorage.getAllSettingsProperties() ) {
						foreach ( ISettingsFieldProperties field in props.fields ) {
							string translation;
							string tagName = props.id+"."+field.id;
							if ( tagName.TryTranslate(out translation) && !translation.NullOrEmpty() ) {
								field.label = translation;
							}
							tagName = tagName+".tooltip";
							if ( tagName.TryTranslate(out translation) && !translation.NullOrEmpty() ) {
								field.labelTooltip = translation;
							}
						}
					}});
				
				initialized = true;
			}
		}
		
		private static void generalSettings() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("settings");
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<QOLModSettings>("StopSkillDecay","Stop skill decay",10,default(bool),t=>t.stopSkillDecay));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<QOLModSettings>("StopTamenessDecay","Stop tameness decay",11,default(bool),t=>t.stopTamenessDecay));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<QOLModSettings>("PreventAnimalFamilies","No animal families",12,default(bool),t=>t.preventAnimalFamilies));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<QOLModSettings>("UseFixedNumericTextfields","Fixed Numberinput",15,default(bool),t=>t.useFixedNumericTextfields));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<QOLModSettings>("CuMBaseCost","CuM base cost",20,20f,t=>t.cumBaseCost));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<QOLModSettings>("CuMConversionPawn","CuM point conversion from pawns",30,0f,t=>t.cumPointPawnToPoolConversionFactor, 0f, 1f));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<QOLModSettings>("CuMConversionRemnants","CuM point conversion from remnants",40,0.25f,t=>t.cumPointRemnantsToPoolConversionFactor, 0f));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<QOLModSettings>("RemnantsOrderPriceFactor","Remnants-Order price factor",50,0.5f,t=>t.remnantOrderPriceFactor));
			
			/*
			 * We want to configure 4 groups for pathfinding:
			 * - colonists
			 * - tame animals
			 * - wild animals
			 * - other
			 * That way colonists can gain vip pathfinding and the rest gets aproximations to save time.
			 * We select an algorithm(null means vanilla) and we define a text property that can be used to configure the pf algorithm instance(ignored for vanilla).
			 * The algorithms declare a class to instantiate for the algorithm. That class must use a constructor that accepts a string argument and it must implement
			 * the findpath method. Reflexion is used to grab the method. That way other mods implementing pathfinding algorithms do not need to link my assembly for
			 * their compilation.
			 */
			props.fields.Add(new SettingsFieldPropertiesSelectable("ColonistPF","Colonist PF",60,typeof(QOLPFDef),default(QOLPFDef),
			                                                       t=> {
			                                                       	string defName = ((QOLModSettings)t).pfColonist;
			                                                       	if ( defName == null ) return null;
			                                                       	return DefDatabase<QOLPFDef>.GetNamed(defName,false);
			                                                       },
				                                             f=>from object d in DefDatabase<QOLPFDef>.AllDefsListForReading select d,
				                                             f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<QOLPFDef>("ColonistPF",(QOLPFDef)def); return true; },
				                                             SettingsFieldPropertiesSelectable.DefLabelProducer,true));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveString<QOLModSettings>("ColonistPFConf","Configuration",70,default(string),t=>t.pfColonistConf));
			
			props.fields.Add(new SettingsFieldPropertiesSelectable("AnimalTamePF","Tame Animal PF",80,typeof(QOLPFDef),default(QOLPFDef),
			                                                       t=> {
			                                                       	string defName = ((QOLModSettings)t).pfAnimalTame;
			                                                       	if ( defName == null ) return null;
			                                                       	return DefDatabase<QOLPFDef>.GetNamed(defName,false);
			                                                       },
				                                             f=>from object d in DefDatabase<QOLPFDef>.AllDefsListForReading select d,
				                                             f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<QOLPFDef>("AnimalTamePF",(QOLPFDef)def); return true; },
				                                             SettingsFieldPropertiesSelectable.DefLabelProducer,true));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveString<QOLModSettings>("AnimalTamePFConf","Configuration",90,default(string),t=>t.pfAnimalTameConf));
			
			props.fields.Add(new SettingsFieldPropertiesSelectable("AnimalWildPF","Wild Animal PF",100,typeof(QOLPFDef),default(QOLPFDef),
			                                                       t=> {
			                                                       	string defName = ((QOLModSettings)t).pfAnimalWild;
			                                                       	if ( defName == null ) return null;
			                                                       	return DefDatabase<QOLPFDef>.GetNamed(defName,false);
			                                                       },
				                                             f=>from object d in DefDatabase<QOLPFDef>.AllDefsListForReading select d,
				                                             f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<QOLPFDef>("AnimalWildPF",(QOLPFDef)def); return true; },
				                                             SettingsFieldPropertiesSelectable.DefLabelProducer,true));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveString<QOLModSettings>("AnimalWildPFConf","Configuration",110,default(string),t=>t.pfAnimalWildConf));
			
			props.fields.Add(new SettingsFieldPropertiesSelectable("OtherPF","Other PF",120,typeof(QOLPFDef),default(QOLPFDef),
			                                                       t=> {
			                                                       	string defName = ((QOLModSettings)t).pfOther;
			                                                       	if ( defName == null ) return null;
			                                                       	return DefDatabase<QOLPFDef>.GetNamed(defName,false);
			                                                       },
				                                             f=>from object d in DefDatabase<QOLPFDef>.AllDefsListForReading select d,
				                                             f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<QOLPFDef>("OtherPF",(QOLPFDef)def); return true; },
				                                             SettingsFieldPropertiesSelectable.DefLabelProducer,true));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveString<QOLModSettings>("OtherPFConf","Configuration",130,default(string),t=>t.pfOtherConf));
			
			props.fields.Add(new SettingsFieldPropertiesListSelectable("PFRestrictionExcemption","Free Moving Jobs",140,typeof(List<string>),new List<string>(),
			                                                           t=> ((QOLModSettings)t).pfRestrictionExcemptions,
			                                                           f=>DefDatabase<JobDef>.AllDefsListForReading.Select(d=>d.defName).Cast<Object>(),
			                                                          flow=>{
			                                                          	ISettingsInstance setting = (ISettingsInstance)flow.flowScope["edit"];
                                                                   		int listIndex = ((IterationItem)flow.pageScope["curItem"]).index;
			                                                          	return listEntry=>{
	                                                                   	flow.addPostRenderCallback(fl=>{setting.get<List<string>>("PFRestrictionExcemption")[listIndex] = (string)listEntry; setting.markAsChanged();});
			                                                          	return true;
                                                                   		};},
			                                                           entry=>{
			                                                           	JobDef jd = DefDatabase<JobDef>.GetNamed((string)entry,false);
			                                                           	if ( jd != null ) return SettingsFieldPropertiesSelectable.DefLabelProducer(jd);
			                                                           	return "Outdated";
			                                                           },()=>DefDatabase<JobDef>.AllDefsListForReading.First().defName,true));

			props.fields.Add(new SettingsFieldPropertiesListSelectable("ForbiddenPatches","Forbidden patches",150,typeof(List<string>),new List<string>(),
			                                                           t=> ((QOLModSettings)t).forbiddenPatchNamespaces,
			                                                           f=>HarmonyPatchNamespace.namespaces.Keys.Cast<Object>().ToList(),
			                                                          flow=>{
			                                                          	ISettingsInstance setting = (ISettingsInstance)flow.flowScope["edit"];
                                                                   		int listIndex = ((IterationItem)flow.pageScope["curItem"]).index;
			                                                          	return listEntry=>{
	                                                                   	flow.addPostRenderCallback(fl=>{setting.get<List<string>>("ForbiddenPatches")[listIndex] = (string)listEntry; setting.markAsChanged();});
			                                                          	return true;
                                                                   		};},
			                                                           entry=>{
			                                                           	HarmonyPatchNamespaceProperties nsprops;
			                                                           	if ( entry != null && HarmonyPatchNamespace.namespaces.TryGetValue((string)entry, out nsprops) ) 
			                                                           		return nsprops.label;
			                                                           	return "Select";
			                                                           },()=>"Select",true,
			                                                          entry=>{
			                                                           	HarmonyPatchNamespaceProperties nsprops;
			                                                           	if ( entry != null && HarmonyPatchNamespace.namespaces.TryGetValue((string)entry, out nsprops) ) 
			                                                           		return nsprops.description;
			                                                           	return null;
			                                                           }));
			
			props.mergers.Add(setting => {
                	QOLModSettings settings = QOLMod.getSettings();
                	settings.stopSkillDecay = setting.get<bool>("StopSkillDecay");
                	settings.stopTamenessDecay = setting.get<bool>("StopTamenessDecay");
                	settings.preventAnimalFamilies = setting.get<bool>("PreventAnimalFamilies");
                	settings.useFixedNumericTextfields = setting.get<bool>("UseFixedNumericTextfields");
                	settings.cumBaseCost = setting.get<float>("CuMBaseCost");
                	settings.cumPointPawnToPoolConversionFactor = setting.get<float>("CuMConversionPawn");
                	settings.cumPointRemnantsToPoolConversionFactor = setting.get<float>("CuMConversionRemnants");
                	settings.remnantOrderPriceFactor = setting.get<float>("RemnantsOrderPriceFactor");
                	
                	settings.pfColonist = setting.get<QOLPFDef>("ColonistPF") != null ? setting.get<QOLPFDef>("ColonistPF").defName : null;
                	settings.pfAnimalTame = setting.get<QOLPFDef>("AnimalTamePF") != null ? setting.get<QOLPFDef>("AnimalTamePF").defName : null;
                	settings.pfAnimalWild = setting.get<QOLPFDef>("AnimalWildPF") != null ? setting.get<QOLPFDef>("AnimalWildPF").defName : null;
                	settings.pfOther = setting.get<QOLPFDef>("OtherPF") != null ? setting.get<QOLPFDef>("OtherPF").defName : null;
                	settings.pfColonistConf = setting.get<string>("ColonistPFConf");
                	settings.pfAnimalTameConf = setting.get<string>("AnimalTamePFConf");
                	settings.pfAnimalWildConf = setting.get<string>("AnimalWildPFConf");
                	settings.pfOtherConf = setting.get<string>("OtherPFConf");
                	
                	Dictionary<int,ReflectionMethodInvoker> pfAlgorithms = new Dictionary<int,ReflectionMethodInvoker>();
                	AddPFAlgorithm(settings.pfColonist,settings.pfColonistConf,1,pfAlgorithms);
                	AddPFAlgorithm(settings.pfAnimalTame,settings.pfAnimalTameConf,2,pfAlgorithms);
                	AddPFAlgorithm(settings.pfAnimalWild,settings.pfAnimalWildConf,3,pfAlgorithms);
                	AddPFAlgorithm(settings.pfOther,settings.pfOtherConf,4,pfAlgorithms);
                	QOLMod.setPFAlgorithms(pfAlgorithms);
                	
                	bool changesOccurred = false;
                	settings.forbiddenPatchNamespaces = setting.get<List<string>>("ForbiddenPatches");
                	if ( settings.forbiddenPatchNamespaces == null ) settings.forbiddenPatchNamespaces = new List<string>();
                	foreach ( KeyValuePair<string,HarmonyPatchNamespaceProperties> p in HarmonyPatchNamespace.namespaces ) {
                		if ( !p.Value.active && !settings.forbiddenPatchNamespaces.Contains(p.Key) ) {
                			p.Value.active = true;
                			changesOccurred = true;
                		}
                	}
                	foreach ( string id in settings.forbiddenPatchNamespaces ) {
                		HarmonyPatchNamespaceProperties nsprops;
                		if ( HarmonyPatchNamespace.namespaces.TryGetValue(id,out nsprops) ) {
                			if ( nsprops.active ) {
                				nsprops.active = false;
                				changesOccurred = true;
                			}
                		}
                	}
                	if ( changesOccurred ) {
                		Find.WindowStack.Add(new Dialog_MessageBox("Changing the forbidden patches requires a restart to take effect.", null, null, null, null, "Restart required", true, null, null));
                	}
                });
			
			props.visualization = new SettingsVisualizationMenuEdit<QOLModSettings>("General Settings", QOLMod.getSettings, "Change General Settings for QOL Mod");
		}
		
		private static void AddPFAlgorithm(string defName, string conf, int algId, Dictionary<int,ReflectionMethodInvoker> pfAlgorithms) {
			if ( defName != null ) {
        		ReflectionMethodInvoker rmi = GetPFAlgorithm(defName,conf);
        		if ( rmi != null ) pfAlgorithms.Add(algId,rmi);
        	}
		}
		
		private static ReflectionMethodInvoker GetPFAlgorithm(string defName, string conf) {
			QOLPFDef def = DefDatabase<QOLPFDef>.GetNamed(defName,false);
    		return new ReflectionMethodInvoker(def.GetConstructor().Invoke(null,new Object[]{conf}),def.GetFindPathMethod());
		}
		
		private static QOLTraitCostDef getDefinedOrDefaultQOLTraitCostDef(TraitDef trait, TraitDegreeData degree) {
			if ( trait == null || degree == null ) return null;
			QOLTraitCostDef def = DefDatabase<QOLTraitCostDef>.GetNamed("QOLTraitCost"+trait.defName+degree.degree,false);
			if ( def == null ) {
				//Log.Message("Trait "+trait.defName+"/"+degree+" is missing CuM cost definitions.");
				def = new QOLTraitCostDef(){defName="QOLTraitCost"+trait.defName+degree.degree,traitDef=trait,traitDegree=degree.degree,cost=2};
				def.ResolveReferences();
				DefDatabase<QOLTraitCostDef>.Add(def);
			}
			return def;
		}
		
		private static QOLTraitCostDef getDefinedOrDefaultQOLTraitCostDef(string key) {
			Regex regexpr = new Regex(@"QOLTraitCost(.*)([0123456789]+)");
			Match m = regexpr.Match(key);
			int degreeInt = int.Parse(m.Groups[2].Value);
			TraitDef trait = DefDatabase<TraitDef>.GetNamed(m.Groups[1].Value,false);
			TraitDegreeData degree = trait.DataAtDegree(degreeInt);
			return getDefinedOrDefaultQOLTraitCostDef(trait,degree);
		}
		
		private static List<QOLTraitCostDef> getDefinedOrDefaultQOLTraitCostDef() {
			foreach ( TraitDef trait in DefDatabase<TraitDef>.AllDefsListForReading ) {
				foreach ( TraitDegreeData degree in trait.degreeDatas ) {
					getDefinedOrDefaultQOLTraitCostDef(trait,degree);
				}
			}
			return DefDatabase<QOLTraitCostDef>.AllDefsListForReading;
		}
		
		private static void traitCosts() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("traitCosts");
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<QOLTraitCostDef>("Cost","Cost",10,default(float),t=>t.cost));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<QOLTraitCostDef>("RemovalCost","Removal Cost",20,default(float),t=>t.costRemoval));
			
			props.mergers.Add(setting => {
	        	QOLTraitCostDef traitCostDef = getDefinedOrDefaultQOLTraitCostDef(setting.getKey());
	        	traitCostDef.cost = setting.get<float>("Cost");
	        	traitCostDef.costRemoval = setting.get<float>("RemovalCost");
	        	});
			
			props.visualization = new SettingsVisualizationMenuSelectEdit<QOLTraitCostDef>("CuM Trait Costs","Select Trait",getDefinedOrDefaultQOLTraitCostDef, 
                   def => def.defName, DefLabelProducerGeneric<QOLTraitCostDef>.LabelProducerNotNull, getDefinedOrDefaultQOLTraitCostDef,
				f=>"Set Cost for Trait \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
		}
		
		private static QOLRecordFactorDef getDefinedOrDefaultQOLRecordFactorDef(RecordDef record) {
			if ( record == null ) return null;
			QOLRecordFactorDef def = DefDatabase<QOLRecordFactorDef>.GetNamed("QOLRecordFactor"+record.defName,false);
			if ( def == null ) {
				def = new QOLRecordFactorDef(){defName="QOLRecordFactor"+record.defName,recordDef=record,factor=0};
				def.ResolveReferences();
				DefDatabase<QOLRecordFactorDef>.Add(def);
			}
			return def;
		}
		
		private static QOLRecordFactorDef getDefinedOrDefaultQOLRecordFactorDef(string key) {
			return getDefinedOrDefaultQOLRecordFactorDef(DefDatabase<RecordDef>.GetNamed(key.Substring("QOLRecordFactor".Length),false));
		}
		
		private static List<QOLRecordFactorDef> getDefinedOrDefaultQOLRecordFactorDef() {
			foreach ( RecordDef record in DefDatabase<RecordDef>.AllDefsListForReading ) {
				getDefinedOrDefaultQOLRecordFactorDef(record);
			}
			return DefDatabase<QOLRecordFactorDef>.AllDefsListForReading;
		}
		
		private static void recordFactors() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("recordFactors");
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<QOLRecordFactorDef>("Factor","Factor",10,default(float),t=>t.factor));
			
			props.mergers.Add(setting => {
              	QOLRecordFactorDef recordFactorDef = getDefinedOrDefaultQOLRecordFactorDef(setting.getKey());
        		recordFactorDef.factor = setting.get<float>("Factor");
              	});
			
			props.visualization = new SettingsVisualizationMenuSelectEdit<QOLRecordFactorDef>("CuM Record Points", "Select Record", getDefinedOrDefaultQOLRecordFactorDef, 
                  def => def.defName, DefLabelProducerGeneric<QOLRecordFactorDef>.LabelProducerNotNull, getDefinedOrDefaultQOLRecordFactorDef,
				f=>"Set Point-Gain-Factor for record \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
		}
		
		private static void powerBuildings() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("powerBuildings");
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("BasePower","Base Power",10,default(float),t=>t.GetCompProperties<CompProperties_Power>().basePowerConsumption));
			
			props.mergers.Add(setting => {
              	ThingDef thing = DefDatabase<ThingDef>.GetNamed(setting.getKey());
				CompProperties_Power cpp = thing.GetCompProperties<CompProperties_Power>();
				cpp.basePowerConsumption = setting.get<float>("BasePower");
				if ( Current.Game != null ) {
					foreach ( Map m in Find.Maps ) {
						m.listerThings.ThingsOfDef(thing).ForEach(t=>{CompPowerTrader pt = t.TryGetComp<CompPowerTrader>(); if ( pt != null ) {
							pt.SetUpPowerVars();}});
					}
				}});
			
			props.visualization = new SettingsVisualizationMenuSelectEdit<ThingDef>("Power Buildings","Select Power Building",()=>DefDatabase<ThingDef>.AllDefsListForReading.Where(d=>d.GetCompProperties<CompProperties_Power>() != null).ToList(),
               def => def.defName, DefLabelProducerGeneric<ThingDef>.LabelProducerNotNull,key => DefDatabase<ThingDef>.GetNamed(key),
               f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
		}
		
		private static void mainButtons() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("mainButtons");
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<MainButtonDef>("Visible","Visible",10,default(bool),t=>t.buttonVisible));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<MainButtonDef>("Order","Order",20,default(int),t=>t.order));
			
			props.mergers.Add(setting => {
			                  	MainButtonDef btn = DefDatabase<MainButtonDef>.GetNamed(setting.getKey());
				btn.buttonVisible = setting.get<bool>("Visible");
				btn.order = setting.get<int>("Order");
				});
			
			props.visualization = new SettingsVisualizationMenuSelectEdit<MainButtonDef>("Main Buttons","Select Main Button",()=>DefDatabase<MainButtonDef>.AllDefsListForReading,
               def => def.defName, DefLabelProducerGeneric<MainButtonDef>.LabelProducerNotNull,key => DefDatabase<MainButtonDef>.GetNamed(key),
               f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
		}
		
		private static void architectButtons() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("architectButtons");
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<DesignationCategoryDef>("Visible","Visible",10,default(bool),t=> {
                 	QOLDefModExtension_DesignationCategoryDef modExtension = t.GetModExtension<QOLDefModExtension_DesignationCategoryDef>();
                 	if ( modExtension != null ) return modExtension.visible;
                 	return true;
                 }));
			                                                                         
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<DesignationCategoryDef>("Order","Order",20,default(int),t=>t.order));
			
			props.mergers.Add(setting => {
              	DesignationCategoryDef btn = null;
              	btn = DefDatabase<DesignationCategoryDef>.GetNamed(setting.getKey());
            					
      			QOLDefModExtension_DesignationCategoryDef modExtension = btn.GetModExtension<QOLDefModExtension_DesignationCategoryDef>();
      			if ( modExtension == null ) {
      				if ( btn.modExtensions == null ) btn.modExtensions = new List<DefModExtension>();
      				modExtension = new QOLDefModExtension_DesignationCategoryDef();
      				btn.modExtensions.Add(modExtension);
      			}
  				modExtension.visible = setting.get<bool>("Visible");
      			btn.order = setting.get<int>("Order");
      			});
			
			QOLMod.addApplySettingsListener(mod=> {
                	if ( MainButtonDefOf.Architect.TabWindow != null ) 
                		MainButtonDefOf.Architect.tabWindowClass.GetMethod("CacheDesPanels", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(MainButtonDefOf.Architect.TabWindow, null);
                });
			
			props.visualization = new SettingsVisualizationMenuSelectEdit<DesignationCategoryDef>("Architect Buttons","Select Architect Button",()=>DefDatabase<DesignationCategoryDef>.AllDefsListForReading,
               def => def.defName, DefLabelProducerGeneric<DesignationCategoryDef>.LabelProducerNotNull,key => DefDatabase<DesignationCategoryDef>.GetNamed(key, false),
               f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
		}
		
		private static void ThingDefCountClass() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("thingDefCountClass");
			if ( props.fields.NullOrEmpty() ) {
				props.fields.Add(new SettingsFieldPropertiesSelectable("Thing","Thing",10,typeof(ThingDef),default(ThingDef),t=>((ThingDefCountClass)t).thingDef,
				                                             f=>from object d in DefDatabase<ThingDef>.AllDefsListForReading where ((ThingDef)d).category == ThingCategory.Item select d,
				                                             f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<ThingDef>("Thing",(ThingDef)def); return true; },
				                                             SettingsFieldPropertiesSelectable.DefLabelProducer,false));
				props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDefCountClass>("Count","Count",20,default(int),t=>t.count));

				props.dynamicLabel = settings => settings.get<int>("Count")+"x"+settings.get<ThingDef>("Thing").label;
				
				props.visualization = new SettingsVisualizationEdit(f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
			}
		}
		
		private static void SkillRequirement() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("skillRequirement");
			if ( props.fields.NullOrEmpty() ) {
				props.fields.Add(new SettingsFieldPropertiesSelectable("Skill","Skill",10,typeof(SkillDef),default(SkillDef),t=>((SkillRequirement)t).skill,
				                                             f=>from object d in DefDatabase<SkillDef>.AllDefsListForReading select d,
				                                            f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<SkillDef>("Skill",(SkillDef)def); return true; },
				                                           SettingsFieldPropertiesSelectable.DefLabelProducer,false));
				props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<SkillRequirement>("MinLevel","Min Level",20,default(int),t=>t.minLevel));

				props.dynamicLabel = settings => settings.get<SkillDef>("Skill").label+" lvl "+settings.get<int>("MinLevel");
				
				props.visualization = new SettingsVisualizationEdit(f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
			}
		}
		
		private static void recipes() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("recipes");
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<RecipeDef>("WorkAmount","Work Amount",10,default(float),t=>t.workAmount));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<RecipeDef>("LearnFactor","Learn Factor",20,default(float),t=>t.workSkillLearnFactor));
			props.fields.Add(new SettingsFieldPropertiesSelectable("WorkSkill","Work Skill",30,typeof(SkillDef),default(SkillDef),t=>((RecipeDef)t).workSkill,
			                                             f=>from object d in DefDatabase<SkillDef>.AllDefsListForReading select d,
			                                            f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<SkillDef>("WorkSkill",(SkillDef)def); return true; },
			                                            SettingsFieldPropertiesSelectable.DefLabelProducer,true));
			props.fields.Add(new SettingsFieldPropertiesSelectable("WorkSpeedStat","Work Speed",40,typeof(StatDef),default(StatDef),t=>((RecipeDef)t).workSpeedStat,
			                                             f=>from StatDef d in DefDatabase<StatDef>.AllDefsListForReading where d.category == StatCategoryDefOf.PawnWork select (object)d,
			                                            f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<StatDef>("WorkSpeedStat",(StatDef)def); return true; },
			                                           SettingsFieldPropertiesSelectable.DefLabelProducer,true));
			ThingDefCountClass();
			props.fields.Add(new SettingsFieldPropertiesListSettings("Products","Products",50,typeof(List<SettingsInstance>),null,
                                     t=> ((RecipeDef)t).products == null ? new List<SettingsInstance>() : ((RecipeDef)t).products.Select(tc=>new SettingsInstance("thingDefCountClass",tc,null,null)).ToList(),
                                     "thingDefCountClass", ()=>new SettingsInstance("thingDefCountClass",new ThingDefCountClass(DefDatabase<ThingDef>.AllDefsListForReading.Find(d=>d.category == ThingCategory.Item),1),null,null),true));
			SkillRequirement();
			props.fields.Add(new SettingsFieldPropertiesListSettings("SkillRequirements","Skill Req.",60,typeof(List<SettingsInstance>),null,
                                 t=> ((RecipeDef)t).skillRequirements == null ? new List<SettingsInstance>() : ((RecipeDef)t).skillRequirements.Select(tc=>new SettingsInstance("skillRequirement",tc,null,null)).ToList(),
                                 "skillRequirement",()=>new SettingsInstance("skillRequirement",new SkillRequirement() { skill = DefDatabase<SkillDef>.AllDefsListForReading[0], minLevel = 1 },null,null),true));
			props.fields.Add(new SettingsFieldPropertiesListPrimitive("Ingredients","Ingredients",70,typeof(List<float>),null,
                                      t=> ((RecipeDef)t).ingredients == null ? new List<float>() : ((RecipeDef)t).ingredients.Select(tc=>tc.GetBaseCount()).ToList(),
                              			(settings,f)=>{
					                 		RecipeDef def = DefDatabase<RecipeDef>.GetNamed(settings.getKey(),false);
					                 		int indx = ((IterationItem)f.pageScope["curItem"]).index;
					                 		if ( def != null && def.ingredients != null && def.ingredients.Count > indx ) return def.ingredients[indx].filter.Summary; return "undefined";
                                          },null,false));
			
			props.mergers.Add(setting => {
              	RecipeDef def = DefDatabase<RecipeDef>.GetNamed(setting.getKey());
         		def.workAmount = setting.get<float>("WorkAmount");
         		def.workSkill = setting.get<SkillDef>("WorkSkill");
         		def.workSkillLearnFactor = setting.get<float>("LearnFactor");
         		def.workSpeedStat = setting.get<StatDef>("WorkSpeedStat");
				
				if ( def.products != null ) def.products.Clear();
         		List<SettingsInstance> products = setting.get<List<SettingsInstance>>("Products");
				if ( !products.NullOrEmpty() ) {
					if ( def.products == null ) def.products = new List<ThingDefCountClass>(products.Count);
					foreach ( ISettingsInstance e in products ) {
						if ( e.get<ThingDef>("Thing") != null ) {
							def.products.Add(new ThingDefCountClass(e.get<ThingDef>("Thing"),e.get<int>("Count")));
						}
					}
				}
         		
				if ( def.skillRequirements != null ) def.skillRequirements.Clear();
				List<SettingsInstance> skillRequirements = setting.get<List<SettingsInstance>>("SkillRequirements");
				if ( !skillRequirements.NullOrEmpty() ) {
					if ( def.skillRequirements == null ) def.skillRequirements = new List<SkillRequirement>(skillRequirements.Count);
					foreach ( ISettingsInstance e in skillRequirements ) {
						if ( e.get<SkillDef>("Skill") != null ) {
							SkillRequirement skrq = new SkillRequirement();
							skrq.skill = e.get<SkillDef>("Skill");
							skrq.minLevel = e.get<int>("MinLevel");
							def.skillRequirements.Add(skrq);
						}
					}
				}
				
				List<float> ingredients = setting.get<List<float>>("Ingredients");
				if ( !ingredients.NullOrEmpty() && !def.ingredients.NullOrEmpty() ) {
					for ( int i = 0; i < def.ingredients.Count && i < ingredients.Count; i++ ) {
						def.ingredients[i].SetBaseCount(ingredients[i]);
					}
				}});
			
			props.visualization = new SettingsVisualizationMenuSelectEdit<RecipeDef>("Recipes","Select Recipe",()=>DefDatabase<RecipeDef>.AllDefsListForReading,
				def => def.defName, DefLabelProducerGeneric<RecipeDef>.LabelProducerNotNull,key => DefDatabase<RecipeDef>.GetNamed(key),
				f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
		}
		
		private static void floorsAndTerrain() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("floorsAndTerrain");
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<TerrainDef>("Beauty","Beauty",10,default(float),t=>t.statBases.GetStatValueFromList(StatDef.Named("Beauty"), 0)));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<TerrainDef>("WorkToBuild","Work",20,default(float),t=>t.statBases.GetStatValueFromList(StatDef.Named("WorkToBuild"), 0)));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<TerrainDef>("Flammability","Flammability",30,default(float),t=>t.statBases.GetStatValueFromList(StatDef.Named("Flammability"), 0)));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<TerrainDef>("Cleanliness","Cleanliness",40,default(float),t=>t.statBases.GetStatValueFromList(StatDef.Named("Cleanliness"), 0)));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<TerrainDef>("PathCost","Path-Cost",50,default(int),t=>t.pathCost));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<TerrainDef>("Fertility","Fertility",60,default(float),t=>t.fertility));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<TerrainDef>("FilthTerrain","Filth-Terrain",70,default(bool),t=>t.acceptTerrainSourceFilth));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<TerrainDef>("Filth","Filth",80,default(bool),t=>t.acceptFilth));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<TerrainDef>("ExtraDraftedPerceivedPathCost","Extra Path-Cost Drafted",51,default(int),t=>t.extraDraftedPerceivedPathCost));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<TerrainDef>("ExtraNonDraftedPerceivedPathCost","Extra Path-Cost Undrafted",52,default(int),t=>t.extraNonDraftedPerceivedPathCost));
			
			props.mergers.Add(setting => {
              	TerrainDef t = DefDatabase<TerrainDef>.GetNamed(setting.getKey());
            	t.SetStatBaseValue(StatDef.Named("Beauty"), setting.get<float>("Beauty"));
				t.SetStatBaseValue(StatDef.Named("WorkToBuild"), setting.get<float>("WorkToBuild"));
				t.SetStatBaseValue(StatDef.Named("Flammability"), setting.get<float>("Flammability"));
				t.SetStatBaseValue(StatDef.Named("Cleanliness"), setting.get<float>("Cleanliness"));
				t.pathCost = setting.get<int>("PathCost");
				t.fertility = setting.get<float>("Fertility");
				t.acceptTerrainSourceFilth = setting.get<bool>("FilthTerrain");
				t.acceptFilth = setting.get<bool>("Filth");
				t.extraDraftedPerceivedPathCost = setting.get<int>("ExtraDraftedPerceivedPathCost");
				t.extraNonDraftedPerceivedPathCost = setting.get<int>("ExtraNonDraftedPerceivedPathCost");
				});
			
			props.visualization = new SettingsVisualizationMenuSelectEdit<TerrainDef>("Floors and Terrain","Select Floor or Terrain",()=>DefDatabase<TerrainDef>.AllDefsListForReading,
				def => def.defName, DefLabelProducerGeneric<TerrainDef>.LabelProducerNotNull,key => DefDatabase<TerrainDef>.GetNamed(key),
				f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
		}
		
		private static bool HasVerbs(this ThingDef td) {
			return td.Verbs != null && td.Verbs.Count > 0;
		}
		
		private static void projectiles() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("projectiles");
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThingDef>("FlyOverhead","Fly overhead",10,default(bool),t=>t.projectile.flyOverhead));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("DamageAmountBase","Damage amount base",20,-1,
                           t=>(int)t.projectile.GetType().GetField("damageAmountBase",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(t.projectile)));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("StoppingPower","Stopping power",30,0f,t=>t.projectile.stoppingPower));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("ArmorPenetrationBase","Armor penetration base",40,-1f,
                           t=>(float)t.projectile.GetType().GetField("armorPenetrationBase",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(t.projectile)));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("Speed","Speed",50,4f,t=>t.projectile.speed));
			props.fields.Add(new SettingsFieldPropertiesSelectable("DamageDef","Damage type",60,typeof(DamageDef),null,
                                                       t=>((ThingDef)t).projectile.damageDef,
			                                           f=>DefDatabase<DamageDef>.AllDefsListForReading.OrderBy(d=>SettingsFieldPropertiesSelectable.DefLabelProducer(d)).Cast<object>(),
			                                           f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<DamageDef>("DamageDef",(DamageDef)def); return true; },
			                                          SettingsFieldPropertiesSelectable.DefLabelProducer,true));
			
			props.mergers.Add(setting => {
              	ThingDef t = DefDatabase<ThingDef>.GetNamed(setting.getKey());
              	t.projectile.flyOverhead = setting.get<bool>("FlyOverhead");
              	t.projectile.stoppingPower = setting.get<float>("StoppingPower");
              	t.projectile.speed = setting.get<float>("Speed");
              	t.projectile.damageDef = setting.get<DamageDef>("DamageDef");
              	
              	t.projectile.GetType().GetField("damageAmountBase",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)
              		.SetValue(t.projectile,setting.get<int>("DamageAmountBase"));
              	t.projectile.GetType().GetField("armorPenetrationBase",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)
              		.SetValue(t.projectile,setting.get<float>("ArmorPenetrationBase"));
              });
			props.validators.Add(setting => {
                 	ThingDef t = DefDatabase<ThingDef>.GetNamed(setting.getKey(), false);
                 	return t != null && t.projectile != null;
                 });
			
			props.visualization = new SettingsVisualizationMenuSelectEdit<ThingDef>("Projectiles","Select Projectile",()=>
                DefDatabase<ThingDef>.AllDefsListForReading.Where(d=>d.category == ThingCategory.Projectile).ToList(),
                def => def.defName, DefLabelProducerGeneric<ThingDef>.LabelProducerNotNull,key => DefDatabase<ThingDef>.GetNamed(key),
				f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
		}
		
		private static void ToolSettings() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("toolSettings");
			if ( props.fields.NullOrEmpty() ) {
				props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<Tool>("Power","Power",10,default(float),t=>t.power));
				props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<Tool>("Cooldown","Cooldown",20,default(float),t=>t.cooldownTime));
				props.fields.Add(new SettingsFieldPropertiesListSelectable("Capacities","Capacities",20,typeof(List<ToolCapacityDef>),new List<ToolCapacityDef>(),
			                                                          tool=>((Tool)tool).capacities.ToList(),
			                                                          f=>DefDatabase<ToolCapacityDef>.AllDefsListForReading.Cast<object>(),
			                                                          flow=>{
			                                                          	ISettingsInstance setting = (ISettingsInstance)flow.flowScope["edit"];
                                                                   		int listIndex = ((IterationItem)flow.pageScope["curItem"]).index;
			                                                          	return listEntry=>{
	                                                                   	flow.addPostRenderCallback(fl=>{setting.get<List<ToolCapacityDef>>("Capacities")[listIndex] = (ToolCapacityDef)listEntry; setting.markAsChanged();});
			                                                          	return true;
                                                                   		};},
			                                                          SettingsFieldPropertiesSelectable.DefLabelProducer,()=>DefDatabase<ToolCapacityDef>.AllDefsListForReading.First(),
			                                                          true));
				
				props.visualization = new SettingsVisualizationEdit(f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
			}
		}
		
		private static void weapons() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("weapons");
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThingDef>("Smeltable","Smeltable",10,default(bool),t=>t.smeltable));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("WarmupTime","Warmup time",20,default(float),t=>t.HasVerbs() ? t.Verbs[0].warmupTime : 0f));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("MinRange","Min range",30,default(float),t=>t.HasVerbs() ? t.Verbs[0].minRange : 0f));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("Range","Range",40,default(float),t=>t.HasVerbs() ? t.Verbs[0].range : 0f));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("ForcedMissRadius","Forced miss radius",50,default(float),t=>t.HasVerbs() ? t.Verbs[0].forcedMissRadius : 0f));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("BurstShotCount","Burst shots",60,default(int),t=>t.HasVerbs() ? t.Verbs[0].burstShotCount : 0));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("TicksBetweenBurstShots","Ticks between burst shots",61,15,t=>t.HasVerbs() ? t.Verbs[0].ticksBetweenBurstShots : 0));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("MuzzleFlashScale","Muzzle flash scale",70,default(float),t=>t.HasVerbs() ? t.Verbs[0].muzzleFlashScale : 0f));
			
			props.fields.Add(new SettingsFieldPropertiesSelectable("DefaultProjectile","Projectile",80,typeof(ThingDef),default(ThingDef),
                                                       t=>((ThingDef)t).HasVerbs() ? ((ThingDef)t).Verbs[0].defaultProjectile : null,
			                                           f=>DefDatabase<ThingDef>.AllDefsListForReading.Where(d=>d.category == ThingCategory.Projectile)
			                                            	.OrderBy(d=>SettingsFieldPropertiesSelectable.DefLabelProducer(d)).Cast<object>(),
			                                           f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<ThingDef>("DefaultProjectile",(ThingDef)def); return true; },
			                                          SettingsFieldPropertiesSelectable.DefLabelProducer,false));
			
			StatModifier();
			props.fields.Add(new SettingsFieldPropertiesListSettings("StatBases","Item Stats",90,typeof(List<SettingsInstance>),null,
                        t=> ((ThingDef)t).statBases.NullOrEmpty() ? new List<SettingsInstance>() : ((ThingDef)t).statBases.Where(tc => tc != null).Select(tc=>new SettingsInstance("statModifier",tc,null,null)).ToList(),
                        "statModifier",()=>new SettingsInstance("statModifier",new StatModifier(){stat=DefDatabase<StatDef>.AllDefsListForReading.First(),value=0},null,null),true));
			
			//TODO editing a fixed list of tools, inside a tool we can change power cooldown and capacitie
			ToolSettings();
			props.fields.Add(new SettingsFieldPropertiesListSettings("Tools","Tools",100,typeof(List<SettingsInstance>),null,
                        t=> ((ThingDef)t).tools.NullOrEmpty() ? new List<SettingsInstance>() : ((ThingDef)t).tools.Where(tc => tc != null).Select(tc=>new SettingsInstance("toolSettings",tc,null,tc.label)).ToList(),
						"toolSettings",null,false));
			
			props.mergers.Add(setting => {
			                  	ThingDef t = DefDatabase<ThingDef>.GetNamed(setting.getKey());
	                  	t.smeltable = setting.get<bool>("Smeltable");
	                  	
	                  	if ( t.HasVerbs() ) {
		                  	VerbProperties vp = t.Verbs[0];
		                  	
		                  	vp.warmupTime = setting.get<float>("WarmupTime");
		                  	vp.minRange = setting.get<float>("MinRange");
		                  	vp.range = setting.get<float>("Range");
		                  	vp.forcedMissRadius = setting.get<float>("ForcedMissRadius");
		                  	vp.burstShotCount = setting.get<int>("BurstShotCount");
		                  	vp.ticksBetweenBurstShots = setting.get<int>("TicksBetweenBurstShots");
		                  	vp.muzzleFlashScale = setting.get<float>("MuzzleFlashScale");
		                  	
		                  	vp.defaultProjectile = setting.get<ThingDef>("DefaultProjectile");
	                  	}
	                  	
	                  	List<SettingsInstance> statBases = setting.get<List<SettingsInstance>>("StatBases");
		            	if ( statBases != null ) {
		            		if ( t.statBases == null ) t.statBases = new List<StatModifier>();
		            		t.statBases.Clear();
		            		foreach ( ISettingsInstance s in statBases ) {
		            			t.statBases.Add(new StatModifier() {stat = s.get<StatDef>("Stat"), value = s.get<float>("Value")});
		            		}
		            	}
	                  	
	                  	List<SettingsInstance> tools = setting.get<List<SettingsInstance>>("Tools");
	                  	if ( tools != null && t.tools != null ) {
	                  		for ( int i = 0; i < t.tools.Count && i < tools.Count; i++ ) {
	                  			SettingsInstance tool = tools[i];
	                  			t.tools[i].power = tool.get<float>("Power");
	                  			t.tools[i].cooldownTime = tool.get<float>("Cooldown");
	                  			List<ToolCapacityDef> capacities = tool.get<List<ToolCapacityDef>>("Capacities");
	                  			if ( capacities != null ) {
	                  				if ( t.tools[i].capacities.NullOrEmpty() ) t.tools[i].capacities = new List<ToolCapacityDef>();
	                  				t.tools[i].capacities.Clear();
	                  				t.tools[i].capacities.AddRange(capacities);
	                  			}
	                  		}
	                  	}});
			
			props.visualization = new SettingsVisualizationMenuSelectEdit<ThingDef>("Weapons","Select Weapon",()=>
                DefDatabase<ThingDef>.AllDefsListForReading.Where(t=>t.IsWithinCategory(ThingCategoryDefOf.Weapons)).ToList(),
				def => def.defName, DefLabelProducerGeneric<ThingDef>.LabelProducerNotNull,key => DefDatabase<ThingDef>.GetNamed(key),
				f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
		}
		
		private static void ThoughtStage() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("thoughtStage");
			if ( props.fields.NullOrEmpty() ) {
				props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThoughtStage>("BaseMoodEffect","Base Mood Effect",10,default(float),t=>t.baseMoodEffect));
				props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThoughtStage>("BaseOpinionOffset","Base Opinion Offset",20,default(float),t=>t.baseOpinionOffset));
				props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThoughtStage>("Visible","Visible",30,default(bool),t=>t.visible));
				
				props.visualization = new SettingsVisualizationEdit(f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
			}
		}
		
		private static void thoughts() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("thoughts");
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThoughtDef>("Duration","Duration(Days)",10,default(float),t=>t.durationDays));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThoughtDef>("OnlyForColonists","Only For Colonists",20,default(bool),t=>t.nullifiedIfNotColonist));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThoughtDef>("ShowBubble","Show Bubble",30,default(bool),t=>t.showBubble));
			props.fields.Add(new SettingsFieldPropertiesSelectable("MultiplyingStat","Multiplying Stat",40,typeof(StatDef),default(StatDef),t=>((ThoughtDef)t).effectMultiplyingStat,
                                                       f=>DefDatabase<StatDef>.AllDefsListForReading.OrderBy(d=>SettingsFieldPropertiesSelectable.DefLabelProducer(d)).Cast<object>(),
			                                            f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<StatDef>("MultiplyingStat",(StatDef)def); return true; },
			                                           SettingsFieldPropertiesSelectable.DefLabelProducer,true));
			props.fields.Add(new SettingsFieldPropertiesSelectable("NextThought","Next Thought",50,typeof(ThoughtDef),default(ThoughtDef),t=>((ThoughtDef)t).nextThought,
			                                            f=>DefDatabase<ThoughtDef>.AllDefsListForReading.OrderBy(d=>SettingsFieldPropertiesSelectable.DefLabelProducer(d)).Cast<object>(),
			                                           f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<ThoughtDef>("NextThought",(ThoughtDef)def); return true; },
			                                          SettingsFieldPropertiesSelectable.DefLabelProducer,true));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThoughtDef>("StackLimit","Stack Limit",60,1,t=>t.stackLimit));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThoughtDef>("StackLimitPerPawn","Stack Limit Per Pawn",70,-1,t=>t.stackLimitForSameOtherPawn));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThoughtDef>("StackedEffectMultiplier","Stacked Effect Multiplier",80,0.75f,t=>t.stackedEffectMultiplier));
			//The default points to float max which is a value that can not be parsed with "float.parse" which is used by the numeric-text widget. 
			//Therefore the value would trigger a change whenever it is displayed. We convert the value to string and parse it to ensure that this wont happen.
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThoughtDef>("MaximumCumulatedOpinion","Maximum Cumulated Opinion",90,float.MaxValue,t=>float.Parse(t.maxCumulatedOpinionOffset.ToString())));
			
			//TODO maybe lerpOpinionToZero as "Age Factor" property. Regulates the decay based on age and Duration.
			//TODO a help page for each settings page that can be configured.
			
			ThoughtStage();
			props.fields.Add(new SettingsFieldPropertiesListSettings("Stages","Stages",100,typeof(List<SettingsInstance>),null,
                        t=> ((ThoughtDef)t).stages.NullOrEmpty() ? new List<SettingsInstance>() : ((ThoughtDef)t).stages.Where(tc => tc != null).Select(tc=>new SettingsInstance("thoughtStage",tc,null,tc.label)).ToList(),
						"thoughtStage",null,false));
			
			props.mergers.Add(setting => {
			                  	ThoughtDef t = DefDatabase<ThoughtDef>.GetNamed(setting.getKey());
            	t.durationDays = setting.get<float>("Duration");
            	t.nullifiedIfNotColonist = setting.get<bool>("OnlyForColonists");
            	t.showBubble = setting.get<bool>("ShowBubble");
            	t.effectMultiplyingStat = setting.get<StatDef>("MultiplyingStat");
            	t.nextThought = setting.get<ThoughtDef>("NextThought");
            	t.stackLimit = setting.get<int>("StackLimit");
            	t.stackLimitForSameOtherPawn = setting.get<int>("StackLimitPerPawn");
            	t.stackedEffectMultiplier = setting.get<float>("StackedEffectMultiplier");
            	t.maxCumulatedOpinionOffset = setting.get<float>("MaximumCumulatedOpinion");
            	List<SettingsInstance> stages = setting.get<List<SettingsInstance>>("Stages");
            	if ( t.stages != null && stages != null ) {
            		for ( int i = 0; i < t.stages.Count && i < stages.Count; i++ ) {
            			t.stages[i].baseMoodEffect = stages[i].get<float>("BaseMoodEffect");
            			t.stages[i].baseOpinionOffset = stages[i].get<float>("BaseOpinionOffset");
            			t.stages[i].visible = stages[i].get<bool>("Visible");
            		}
            	}});
			
			props.visualization = new SettingsVisualizationMenuSelectEdit<ThoughtDef>("Thoughts","Select Thought (group)",()=>DefDatabase<ThoughtDef>.AllDefsListForReading,
				def => def.defName, DefLabelProducerGeneric<ThoughtDef>.LabelProducerNotNull,key => DefDatabase<ThoughtDef>.GetNamed(key),
				f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
		}
		
		private static void StatModifier() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("statModifier");
			if ( props.fields.NullOrEmpty() ) {
				props.fields.Add(new SettingsFieldPropertiesSelectable("Stat","Stat",10,typeof(StatDef),default(StatDef),t=>((StatModifier)t).stat,
				                                             f=>from object d in DefDatabase<StatDef>.AllDefsListForReading select d,
				                                             f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<StatDef>("Stat",(StatDef)def); return true; },
				                                             SettingsFieldPropertiesSelectable.DefLabelProducer,false));
				props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<StatModifier>("Value","Value",20,default(float),t=>t.value));

				props.dynamicLabel = settings => settings.get<float>("Value")+"x"+settings.get<StatDef>("Stat").label;
				
				props.visualization = new SettingsVisualizationEdit(f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
			}
		}
		
		private static void apparel() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("apparel");
			
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("wearPerDay","WearPerDay",10,default(float),t=>t.apparel.wearPerDay));
			//SelectMany field is needed for these
			
			props.fields.Add(new SettingsFieldPropertiesListSelectable("layers","Layers",20,typeof(List<string>),new List<string>(),
			                                                          apparel=>((ThingDef)apparel).apparel.layers.Select(al=>al.defName).ToList(),
			                                                          f=>DefDatabase<ApparelLayerDef>.AllDefsListForReading.Select(d=>(object)d.defName),
			                                                          flow=>{
			                                                          	ISettingsInstance setting = (ISettingsInstance)flow.flowScope["edit"];
                                                                   		int listIndex = ((IterationItem)flow.pageScope["curItem"]).index;
			                                                          	return listEntry=>{
	                                                                   	flow.addPostRenderCallback(fl=>{setting.get<List<string>>("layers")[listIndex] = (string)listEntry; setting.markAsChanged();});
			                                                          	return true;
                                                                   		};},
			                                                          entry=>(string)entry,()=>ApparelLayerDefOf.Belt.defName,
			                                                          true));
			props.fields.Add(new SettingsFieldPropertiesListSelectable("bodyPartGroups","Body Parts Covered",30,typeof(List<string>),new List<string>(),
			                                                          apparel=>((ThingDef)apparel).apparel.bodyPartGroups.Select(bpg=>bpg.defName).ToList(),
			                                                          f=>DefDatabase<BodyPartGroupDef>.AllDefsListForReading.Select(bpg=>(object)bpg.defName),
			                                                          flow=>{
			                                                          	ISettingsInstance setting = (ISettingsInstance)flow.flowScope["edit"];
                                                                   		int listIndex = ((IterationItem)flow.pageScope["curItem"]).index;
			                                                          	return listEntry=>{
	                                                                   	flow.addPostRenderCallback(fl=>{setting.get<List<string>>("bodyPartGroups")[listIndex] = (string)listEntry; setting.markAsChanged();});
			                                                          	return true;
                                                                   		};},
			                                                          entry=>DefDatabase<BodyPartGroupDef>.GetNamed((string)entry).label,()=>DefDatabase<BodyPartGroupDef>.AllDefsListForReading.First().defName,
			                                                          true));
			StatModifier();
			props.fields.Add(new SettingsFieldPropertiesListSettings("equippedStatOffsets","Wearer Stats",100,typeof(List<SettingsInstance>),null,
                        t=> ((ThingDef)t).equippedStatOffsets.NullOrEmpty() ? new List<SettingsInstance>() : ((ThingDef)t).equippedStatOffsets.Where(tc => tc != null).Select(tc=>new SettingsInstance("statModifier",tc,null,null)).ToList(),
						"statModifier",()=>new SettingsInstance("statModifier",new StatModifier(){stat=DefDatabase<StatDef>.AllDefsListForReading.First(),value=0},null,null),true));
			props.fields.Add(new SettingsFieldPropertiesListSettings("statBases","Item Stats",110,typeof(List<SettingsInstance>),null,
                        t=> ((ThingDef)t).statBases.NullOrEmpty() ? new List<SettingsInstance>() : ((ThingDef)t).statBases.Where(tc => tc != null).Select(tc=>new SettingsInstance("statModifier",tc,null,null)).ToList(),
                        "statModifier",()=>new SettingsInstance("statModifier",new StatModifier(){stat=DefDatabase<StatDef>.AllDefsListForReading.First(),value=0},null,null),true));
			
			props.mergers.Add(setting => {
              	ThingDef t = DefDatabase<ThingDef>.GetNamed(setting.getKey());
            	
            	t.apparel.wearPerDay = setting.get<float>("wearPerDay");
            	List<string> lsDefNames = setting.get<List<string>>("layers");
            	IEnumerable<ApparelLayerDef> layers = DefDatabase<ApparelLayerDef>.AllDefsListForReading.Where(l=>lsDefNames.Contains(l.defName));
            	if ( layers.Any() ) {
            		if ( t.apparel.layers == null ) t.apparel.layers = new List<ApparelLayerDef>();
        			t.apparel.layers.Clear();
            		t.apparel.layers.AddRange(layers);
            	}
            	List<string> bpgsDefNames = setting.get<List<string>>("bodyPartGroups");
            	IEnumerable<BodyPartGroupDef> bpgs = DefDatabase<BodyPartGroupDef>.AllDefsListForReading.Where(bpg=>bpgsDefNames.Contains(bpg.defName));
            	if ( bpgs != null ) {
            		if ( t.apparel.bodyPartGroups == null ) t.apparel.bodyPartGroups = new List<BodyPartGroupDef>();
            		t.apparel.bodyPartGroups.Clear();
            		t.apparel.bodyPartGroups.AddRange(bpgs);
            	}
            	List<SettingsInstance> equippedStatOffsets = setting.get<List<SettingsInstance>>("equippedStatOffsets");
            	if ( equippedStatOffsets != null ) {
            		if ( t.equippedStatOffsets == null ) t.equippedStatOffsets = new List<StatModifier>();
            		t.equippedStatOffsets.Clear();
            		foreach ( ISettingsInstance s in equippedStatOffsets ) {
            			t.equippedStatOffsets.Add(new StatModifier() {stat = s.get<StatDef>("Stat"), value = s.get<float>("Value")});
            		}
            	}
            	List<SettingsInstance> statBases = setting.get<List<SettingsInstance>>("statBases");
            	if ( statBases != null ) {
            		if ( t.statBases == null ) t.statBases = new List<StatModifier>();
            		t.statBases.Clear();
            		foreach ( ISettingsInstance s in statBases ) {
            			t.statBases.Add(new StatModifier() {stat = s.get<StatDef>("Stat"), value = s.get<float>("Value")});
            		}
            	}});
			
			props.visualization = new SettingsVisualizationMenuSelectEdit<ThingDef>("Apparel","Select Apparel",()=>DefDatabase<ThingDef>.AllDefsListForReading.Where(t=>t.IsApparel).ToList(),
				def => def.defName, DefLabelProducerGeneric<ThingDef>.LabelProducerNotNull,key => DefDatabase<ThingDef>.GetNamed(key),
				f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
		}
		
		private static void materials() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("material");
			
			StatModifier();
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("StackLimit","Stack Limit",10,1,t=>t.stackLimit));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("PathCost","Path Cost",20,0,t=>t.pathCost));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThingDef>("UseHitPoints","Use Hit Points",30,default(bool),t=>t.useHitPoints));
			
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("DeepCommonality","Deep Commonality",40,default(float),t=>t.deepCommonality));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("DeepCountPerPortion","Deep Count Per Portion",50,-1,t=>t.deepCountPerPortion));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("DeepCountPerCell","Deep Count Per Cell",60,300,t=>t.deepCountPerCell,0,65535));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("DeepLumpSizeMin","Deep Lump Size Min",70,default(int),t=>t.deepLumpSizeRange.min));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("DeepLumpSizeMax","Deep Lump Size Max",80,default(int),t=>t.deepLumpSizeRange.max));
			
			props.fields.Add(new SettingsFieldPropertiesListSettings("statBases","Item Stats",90,typeof(List<SettingsInstance>),null,
                        t=> ((ThingDef)t).statBases.NullOrEmpty() ? new List<SettingsInstance>() : ((ThingDef)t).statBases.Where(tc => tc != null).Select(tc=>new SettingsInstance("statModifier",tc,null,null)).ToList(),
                        "statModifier",()=>new SettingsInstance("statModifier",new StatModifier(){stat=DefDatabase<StatDef>.AllDefsListForReading.First(),value=0},null,null),true));
			
			//stuff props
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThingDef>("Smeltable","Smeltable",100,default(bool),t=>t.stuffProps.smeltable));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("Commonality","Commonality",110,default(float),t=>t.stuffProps.commonality));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveColor<ThingDef>("Color","Color",120,new UnityEngine.Color(0.8f,0.8f,0.8f),t=>t.stuffProps.color));
			props.fields.Add(new SettingsFieldPropertiesListSettings("StatFactors","Material Stat Factors",130,typeof(List<SettingsInstance>),null,
                        t=> ((ThingDef)t).stuffProps.statFactors.NullOrEmpty() ? new List<SettingsInstance>() : ((ThingDef)t).stuffProps.statFactors.Where(tc => tc != null).Select(tc=>new SettingsInstance("statModifier",tc,null,null)).ToList(),
                        "statModifier",()=>new SettingsInstance("statModifier",new StatModifier(){stat=DefDatabase<StatDef>.AllDefsListForReading.First(),value=0},null,null),true));
			props.fields.Add(new SettingsFieldPropertiesListSettings("StatOffsets","Material Stat Offsets",140,typeof(List<SettingsInstance>),null,
                        t=> ((ThingDef)t).stuffProps.statOffsets.NullOrEmpty() ? new List<SettingsInstance>() : ((ThingDef)t).stuffProps.statOffsets.Where(tc => tc != null).Select(tc=>new SettingsInstance("statModifier",tc,null,null)).ToList(),
                        "statModifier",()=>new SettingsInstance("statModifier",new StatModifier(){stat=DefDatabase<StatDef>.AllDefsListForReading.First(),value=0},null,null),true));
			
			props.mergers.Add(setting => {
              	ThingDef t = DefDatabase<ThingDef>.GetNamed(setting.getKey());
            	
              	t.stackLimit = setting.get<int>("StackLimit");
              	t.pathCost = setting.get<int>("PathCost");
              	t.useHitPoints = setting.get<bool>("UseHitPoints");
              	t.deepCommonality = setting.get<float>("DeepCommonality");
              	t.deepCountPerPortion = setting.get<int>("DeepCountPerPortion");
              	t.deepCountPerCell = setting.get<int>("DeepCountPerCell");
              	t.deepLumpSizeRange.min = setting.get<int>("DeepLumpSizeMin");
              	t.deepLumpSizeRange.max = setting.get<int>("DeepLumpSizeMax");
              	t.stuffProps.smeltable = setting.get<bool>("Smeltable");
              	t.stuffProps.commonality = setting.get<float>("Commonality");
              	t.stuffProps.color = setting.get<UnityEngine.Color>("Color");
              	
              	List<SettingsInstance> stats = setting.get<List<SettingsInstance>>("StatFactors");
            	if ( stats != null ) {
            		if ( t.stuffProps.statFactors == null ) t.stuffProps.statFactors = new List<StatModifier>();
            		t.stuffProps.statFactors.Clear();
            		foreach ( ISettingsInstance s in stats ) {
            			t.stuffProps.statFactors.Add(new StatModifier() {stat = s.get<StatDef>("Stat"), value = s.get<float>("Value")});
            		}
            	}
              	stats = setting.get<List<SettingsInstance>>("StatOffsets");
            	if ( stats != null ) {
            		if ( t.stuffProps.statOffsets == null ) t.stuffProps.statOffsets = new List<StatModifier>();
            		t.stuffProps.statOffsets.Clear();
            		foreach ( ISettingsInstance s in stats ) {
            			t.stuffProps.statOffsets.Add(new StatModifier() {stat = s.get<StatDef>("Stat"), value = s.get<float>("Value")});
            		}
            	}
              	
              	
            	List<SettingsInstance> statBases = setting.get<List<SettingsInstance>>("statBases");
            	if ( statBases != null ) {
            		if ( t.statBases == null ) t.statBases = new List<StatModifier>();
            		t.statBases.Clear();
            		foreach ( ISettingsInstance s in statBases ) {
            			t.statBases.Add(new StatModifier() {stat = s.get<StatDef>("Stat"), value = s.get<float>("Value")});
            		}
            	}});
			
			props.visualization = new SettingsVisualizationMenuSelectEdit<ThingDef>("Materials","Select Material",()=>DefDatabase<ThingDef>.AllDefsListForReading.Where(t=>t.IsStuff).ToList(),
				def => def.defName, DefLabelProducerGeneric<ThingDef>.LabelProducerNotNull,key => DefDatabase<ThingDef>.GetNamed(key),
				f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
		}
		
		private static void foods() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("food");
			
			StatModifier();
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("StackLimit","Stack Limit",10,1,t=>t.stackLimit));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("PathCost","Path Cost",20,0,t=>t.pathCost));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThingDef>("UseHitPoints","Use Hit Points",30,default(bool),t=>t.useHitPoints));
			
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("DeepCommonality","Deep Commonality",40,default(float),t=>t.deepCommonality));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("DeepCountPerPortion","Deep Count Per Portion",50,-1,t=>t.deepCountPerPortion));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("DeepCountPerCell","Deep Count Per Cell",60,300,t=>t.deepCountPerCell,0,65535));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("DeepLumpSizeMin","Deep Lump Size Min",70,default(int),t=>t.deepLumpSizeRange.min));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("DeepLumpSizeMax","Deep Lump Size Max",80,default(int),t=>t.deepLumpSizeRange.max));
			
			props.fields.Add(new SettingsFieldPropertiesListSettings("statBases","Item Stats",90,typeof(List<SettingsInstance>),null,
                        t=> ((ThingDef)t).statBases.NullOrEmpty() ? new List<SettingsInstance>() : ((ThingDef)t).statBases.Where(tc => tc != null).Select(tc=>new SettingsInstance("statModifier",tc,null,null)).ToList(),
                        "statModifier",()=>new SettingsInstance("statModifier",new StatModifier(){stat=DefDatabase<StatDef>.AllDefsListForReading.First(),value=0},null,null),true));
			
			props.fields.Add(new SettingsFieldPropertiesSelectable("FoodType","Food-Type",100,typeof(string),Enum.GetName(typeof(FoodTypeFlags),FoodTypeFlags.None),
			                                                       t=>Enum.GetName(typeof(FoodTypeFlags),((ThingDef)t).ingestible.foodType),
	                                                       f=>from FoodTypeFlags d in Enum.GetValues(typeof(FoodTypeFlags)) orderby Enum.GetName(typeof(FoodTypeFlags),d) select (object)Enum.GetName(typeof(FoodTypeFlags),d),
				                                             f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<string>("FoodType",(string)def); return true; },
				                                             o=>(string)o,false));
			props.fields.Add(new SettingsFieldPropertiesSelectable("FoodPreferability","Food-Preferability",110,typeof(string),FoodPreferability.Undefined.ToString(),
			                                                       t=>Enum.GetName(typeof(FoodPreferability),((ThingDef)t).ingestible.preferability),
			                                                       f=>from FoodPreferability d in Enum.GetValues(typeof(FoodPreferability)) orderby d.ToString() select (object)d.ToString(),
				                                             f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<string>("FoodPreferability",(string)def); return true; },
				                                             o=>(string)o,false));
			
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("MaxNumToIngestAtOnce","maxNumToIngestAtOnce",120,20,t=>t.ingestible.maxNumToIngestAtOnce,1));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("BaseIngestTicks","baseIngestTicks",130,500,t=>t.ingestible.baseIngestTicks,0));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("ChairSearchRadius","chairSearchRadius",140,32f,t=>t.ingestible.chairSearchRadius,0));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThingDef>("UseEatingSpeedStat","useEatingSpeedStat",150,true,t=>t.ingestible.useEatingSpeedStat));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThingDef>("IngestHoldUsesTable","ingestHoldUsesTable",160,true,t=>t.ingestible.ingestHoldUsesTable));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("Joy","joy",170,default(float),t=>t.ingestible.joy));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThingDef>("Nurseable","nurseable",180,default(bool),t=>t.ingestible.nurseable));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("OptimalityOffsetHumanlikes","optimalityOffsetHumanlikes",190,default(float),t=>t.ingestible.optimalityOffsetHumanlikes));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("OptimalityOffsetFeedingAnimals","optimalityOffsetFeedingAnimals",200,default(float),t=>t.ingestible.optimalityOffsetFeedingAnimals));
			props.fields.Add(new SettingsFieldPropertiesSelectable("TasteThought","tasteThought",210,typeof(ThoughtDef),default(ThoughtDef),t=>((ThingDef)t).ingestible.tasteThought,
			                                            f=>DefDatabase<ThoughtDef>.AllDefsListForReading.OrderBy(d=>SettingsFieldPropertiesSelectable.DefLabelProducer(d)).Cast<object>(),
			                                           f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<ThoughtDef>("TasteThought",(ThoughtDef)def); return true; },
			                                          SettingsFieldPropertiesSelectable.DefLabelProducer,true));
			props.fields.Add(new SettingsFieldPropertiesSelectable("SpecialThoughtDirect","specialThoughtDirect",220,typeof(ThoughtDef),default(ThoughtDef),t=>((ThingDef)t).ingestible.specialThoughtDirect,
			                                            f=>DefDatabase<ThoughtDef>.AllDefsListForReading.OrderBy(d=>SettingsFieldPropertiesSelectable.DefLabelProducer(d)).Cast<object>(),
			                                           f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<ThoughtDef>("SpecialThoughtDirect",(ThoughtDef)def); return true; },
			                                          SettingsFieldPropertiesSelectable.DefLabelProducer,true));
			props.fields.Add(new SettingsFieldPropertiesSelectable("SpecialThoughtAsIngredient","specialThoughtAsIngredient",230,typeof(ThoughtDef),default(ThoughtDef),t=>((ThingDef)t).ingestible.specialThoughtAsIngredient,
			                                            f=>DefDatabase<ThoughtDef>.AllDefsListForReading.OrderBy(d=>SettingsFieldPropertiesSelectable.DefLabelProducer(d)).Cast<object>(),
			                                           f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<ThoughtDef>("SpecialThoughtAsIngredient",(ThoughtDef)def); return true; },
			                                          SettingsFieldPropertiesSelectable.DefLabelProducer,true));
			props.fields.Add(new SettingsFieldPropertiesSelectable("JoyKind","joyKind",240,typeof(JoyKindDef),default(JoyKindDef),t=>((ThingDef)t).ingestible.specialThoughtAsIngredient,
			                                            f=>DefDatabase<JoyKindDef>.AllDefsListForReading.OrderBy(d=>SettingsFieldPropertiesSelectable.DefLabelProducer(d)).Cast<object>(),
			                                           f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<JoyKindDef>("JoyKind",(JoyKindDef)def); return true; },
			                                          SettingsFieldPropertiesSelectable.DefLabelProducer,true));
			props.fields.Add(new SettingsFieldPropertiesSelectable("DrugCategory","drugCategory",250,typeof(string),Enum.GetName(typeof(DrugCategory),DrugCategory.None),
			                                                       t=>Enum.GetName(typeof(DrugCategory),((ThingDef)t).ingestible.drugCategory),
	                                                       f=>from DrugCategory d in Enum.GetValues(typeof(DrugCategory)) orderby Enum.GetName(typeof(DrugCategory),d) select (object)Enum.GetName(typeof(DrugCategory),d),
				                                             f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<string>("DrugCategory",(string)def); return true; },
				                                             o=>(string)o,false));
			
			props.mergers.Add(setting => {
              	ThingDef t = DefDatabase<ThingDef>.GetNamed(setting.getKey());
            	
              	t.stackLimit = setting.get<int>("StackLimit");
              	t.pathCost = setting.get<int>("PathCost");
              	t.useHitPoints = setting.get<bool>("UseHitPoints");
              	t.deepCommonality = setting.get<float>("DeepCommonality");
              	t.deepCountPerPortion = setting.get<int>("DeepCountPerPortion");
              	t.deepCountPerCell = setting.get<int>("DeepCountPerCell");
              	t.deepLumpSizeRange.min = setting.get<int>("DeepLumpSizeMin");
              	t.deepLumpSizeRange.max = setting.get<int>("DeepLumpSizeMax");
              	
              	FoodTypeFlags foodType;
            	FoodPreferability foodPreferability;
            	try {
	            	foodType = (FoodTypeFlags)Enum.Parse(typeof(FoodTypeFlags),setting.get<string>("FoodType"));
            	} catch ( ArgumentException ) {
            		foodType = FoodTypeFlags.None;
            	}
            	try {
	            	foodPreferability = (FoodPreferability)Enum.Parse(typeof(FoodPreferability),setting.get<string>("FoodPreferability"));
            	} catch ( ArgumentException ) {
            		foodPreferability = FoodPreferability.Undefined;
            	}
        		t.ingestible.foodType = foodType;
        		t.ingestible.preferability = foodPreferability;
        		
    			t.ingestible.maxNumToIngestAtOnce = setting.get<int>("MaxNumToIngestAtOnce");
        		t.ingestible.baseIngestTicks = setting.get<int>("BaseIngestTicks");
				t.ingestible.chairSearchRadius = setting.get<float>("ChairSearchRadius");
				t.ingestible.useEatingSpeedStat = setting.get<bool>("UseEatingSpeedStat");
				t.ingestible.ingestHoldUsesTable = setting.get<bool>("IngestHoldUsesTable");
				t.ingestible.joy = setting.get<float>("Joy");
				t.ingestible.nurseable = setting.get<bool>("Nurseable");
				t.ingestible.optimalityOffsetHumanlikes = setting.get<float>("OptimalityOffsetHumanlikes");
				t.ingestible.optimalityOffsetFeedingAnimals = setting.get<float>("OptimalityOffsetFeedingAnimals");
				t.ingestible.tasteThought = setting.get<ThoughtDef>("TasteThought");
				t.ingestible.specialThoughtDirect = setting.get<ThoughtDef>("SpecialThoughtDirect");
				t.ingestible.specialThoughtAsIngredient = setting.get<ThoughtDef>("SpecialThoughtAsIngredient");
				t.ingestible.joyKind = setting.get<JoyKindDef>("JoyKind");
				
				DrugCategory drugCategory;
            	try {
	            	drugCategory = (DrugCategory)Enum.Parse(typeof(DrugCategory),setting.get<string>("DrugCategory"));
            	} catch ( ArgumentException ) {
            		drugCategory = DrugCategory.None;
            	}
				t.ingestible.drugCategory = drugCategory;
              	
            	List<SettingsInstance> statBases = setting.get<List<SettingsInstance>>("statBases");
            	if ( statBases != null ) {
            		if ( t.statBases == null ) t.statBases = new List<StatModifier>();
            		t.statBases.Clear();
            		foreach ( ISettingsInstance s in statBases ) {
            			t.statBases.Add(new StatModifier() {stat = s.get<StatDef>("Stat"), value = s.get<float>("Value")});
            		}
            	}
            	typeof(IngestibleProperties).GetField("cachedNutrition", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(t.ingestible,-1f);
          	});
			
			props.visualization = new SettingsVisualizationMenuSelectEdit<ThingDef>("Foods","Select Food",()=>DefDatabase<ThingDef>.AllDefsListForReading.Where(t=>t.IsIngestible && t.plant == null).ToList(),
				def => def.defName, DefLabelProducerGeneric<ThingDef>.LabelProducerNotNull,key => DefDatabase<ThingDef>.GetNamed(key),
				f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
		}
		
		private static void plants() {
			SettingsProperties props = SettingsStorage.getOrInitSettingsProperties("plants");
			
			/*
			 * <sowResearchPrerequisites>
			        <li>TreeSowing</li>
			      </sowResearchPrerequisites>
			      <sowTags>
			        <li>Ground</li>
			      </sowTags>
			    </plant>
			 */
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("PathCost","Path-Cost",10,0,t=>t.pathCost));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThingDef>("PathCostIgnoreRepeat","Path-Cost ignore repeat",11,default(bool),t=>t.pathCostIgnoreRepeat));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThingDef>("BlockWind","Block wind",20,default(bool),t=>t.blockWind));
			
			props.fields.Add(new SettingsFieldPropertiesSelectable("FoodType","Food-Type",30,typeof(string),Enum.GetName(typeof(FoodTypeFlags),FoodTypeFlags.None),
			                                                       t=>((ThingDef)t).ingestible != null ? Enum.GetName(typeof(FoodTypeFlags),((ThingDef)t).ingestible.foodType) : Enum.GetName(typeof(FoodTypeFlags),FoodTypeFlags.None),
	                                                       f=>from FoodTypeFlags d in Enum.GetValues(typeof(FoodTypeFlags)) orderby Enum.GetName(typeof(FoodTypeFlags),d) select (object)Enum.GetName(typeof(FoodTypeFlags),d),
				                                             f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<string>("FoodType",(string)def); return true; },
				                                             o=>(string)o,false));
			props.fields.Add(new SettingsFieldPropertiesSelectable("FoodPreferability","Food-Preferability",40,typeof(string),FoodPreferability.Undefined.ToString(),
			                                                       t=>((ThingDef)t).ingestible != null ? Enum.GetName(typeof(FoodPreferability),((ThingDef)t).ingestible.preferability) : Enum.GetName(typeof(FoodPreferability),FoodPreferability.Undefined),
			                                                       f=>from FoodPreferability d in Enum.GetValues(typeof(FoodPreferability)) orderby d.ToString() select (object)d.ToString(),
				                                             f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<string>("FoodPreferability",(string)def); return true; },
				                                             o=>(string)o,false));
			
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("FertilityMin","Fertility-Min",49,0.9f,t=>t.plant.fertilityMin));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("FertilitySensitivity","Fertility-Sensitivity",50,0.5f,t=>t.plant.fertilitySensitivity));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("SowWork","Sowing-Work",60,default(float),t=>t.plant.sowWork, 0));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("SowMinSkill","Skill requirement",70,0,t=>t.plant.sowMinSkill, 0));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThingDef>("MustBeWildToSow","Restrict by Biome",80,default(bool),t=>t.blockWind));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("HarvestWork","Harvest-Work",90,default(float),t=>t.plant.harvestWork, 0));
			
			props.fields.Add(new SettingsFieldPropertiesSelectable("HarvestedThingDef","Harvest-Product",100,typeof(ThingDef),null,t=>((ThingDef)t).plant.harvestedThingDef,
	                                                         f=>from object d in DefDatabase<ThingDef>.AllDefsListForReading select d,
				                                             f=>def=>{ ((ISettingsInstance)f.flowScope["edit"]).set<ThingDef>("HarvestedThingDef",(ThingDef)def); return true; },
				                                             SettingsFieldPropertiesSelectable.DefLabelProducer,true));
			
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("HarvestYield","Yield",110,default(float),t=>t.plant.harvestYield));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("HarvestMinGrowth","Min-Growth",120,default(float),t=>t.plant.harvestMinGrowth));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThingDef>("HarvestFailable","Harvest can fail",130,default(bool),t=>t.plant.harvestFailable));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThingDef>("BlockAdjacentSow","Block adjacent sowing",140,default(bool),t=>t.plant.blockAdjacentSow));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("VisualSizeRangeMin","Min-Visual-Size",150,default(float),t=>t.plant.visualSizeRange.min));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("VisualSizeRangeMax","Max-Visual-Size",160,default(float),t=>t.plant.visualSizeRange.max));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("TopWindExposure","Top-Wind-Exposure",170,default(float),t=>t.plant.topWindExposure));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveInt<ThingDef>("WildClusterRadius","Wild growth radius",180,-1,t=>t.plant.wildClusterRadius, -1));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("WildClusterWeight","Wild growth weight",190,15f,t=>t.plant.wildClusterWeight, 0));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveFloat<ThingDef>("WildOrder","Wild order",200,2f,t=>t.plant.wildOrder, 0));
			props.fields.Add(new SettingsFieldPropertiesPrimitiveBool<ThingDef>("InterferesWithRoof","Blocks roof",210,default(bool),t=>t.plant.interferesWithRoof));
			
			
			StatModifier();
			props.fields.Add(new SettingsFieldPropertiesListSettings("statBases","Item Stats",220,typeof(List<SettingsInstance>),null,
                        t=> ((ThingDef)t).statBases.NullOrEmpty() ? new List<SettingsInstance>() : ((ThingDef)t).statBases.Where(tc => tc != null).Select(tc=>new SettingsInstance("statModifier",tc,null,null)).ToList(),
                        "statModifier",()=>new SettingsInstance("statModifier",new StatModifier(){stat=DefDatabase<StatDef>.AllDefsListForReading.First(),value=0},null,null),true));
			
			props.mergers.Add(setting => {
              	ThingDef t = DefDatabase<ThingDef>.GetNamed(setting.getKey());
            	
            	t.pathCost = setting.get<int>("PathCost");
            	t.pathCostIgnoreRepeat = setting.get<bool>("PathCostIgnoreRepeat");
            	t.blockWind = setting.get<bool>("BlockWind");
            	
            	FoodTypeFlags foodType;
            	FoodPreferability foodPreferability;
            	try {
	            	foodType = (FoodTypeFlags)Enum.Parse(typeof(FoodTypeFlags),setting.get<string>("FoodType"));
            	} catch ( ArgumentException ) {
            		foodType = FoodTypeFlags.None;
            	}
            	try {
	            	foodPreferability = (FoodPreferability)Enum.Parse(typeof(FoodPreferability),setting.get<string>("FoodPreferability"));
            	} catch ( ArgumentException ) {
            		foodPreferability = FoodPreferability.Undefined;
            	}
            	if ( t.ingestible != null ) {
            		t.ingestible.foodType = foodType;
            		t.ingestible.preferability = foodPreferability;
            	} else {
            		if ( foodType != FoodTypeFlags.None ) setting.set<string>("FoodType", Enum.GetName(typeof(FoodTypeFlags),FoodTypeFlags.None));
            		if ( foodPreferability != FoodPreferability.Undefined ) setting.set<string>("FoodPreferability", Enum.GetName(typeof(FoodPreferability),FoodPreferability.Undefined));
            	}
            	
        		t.plant.fertilityMin = setting.get<float>("FertilityMin");
            	t.plant.fertilitySensitivity = setting.get<float>("FertilitySensitivity");
            	t.plant.sowWork = setting.get<float>("SowWork");
            	t.plant.sowMinSkill = setting.get<int>("SowMinSkill");
            	t.plant.mustBeWildToSow = setting.get<bool>("MustBeWildToSow");
            	t.plant.harvestWork = setting.get<float>("HarvestWork");
            	t.plant.harvestedThingDef = setting.get<ThingDef>("HarvestedThingDef");
            	t.plant.harvestYield = setting.get<float>("HarvestYield");
            	t.plant.harvestMinGrowth = setting.get<float>("HarvestMinGrowth");
            	t.plant.harvestFailable = setting.get<bool>("HarvestFailable");
            	t.plant.blockAdjacentSow = setting.get<bool>("BlockAdjacentSow");
            	t.plant.visualSizeRange.min = setting.get<float>("VisualSizeRangeMin");
            	t.plant.visualSizeRange.max = setting.get<float>("VisualSizeRangeMax");
            	t.plant.topWindExposure = setting.get<float>("TopWindExposure");
            	t.plant.wildClusterRadius = setting.get<int>("WildClusterRadius");
            	t.plant.wildClusterWeight = setting.get<float>("WildClusterWeight");
            	t.plant.wildOrder = setting.get<float>("WildOrder");
            	t.plant.interferesWithRoof = setting.get<bool>("InterferesWithRoof");
            	
            	List<SettingsInstance> statBases = setting.get<List<SettingsInstance>>("statBases");
            	if ( statBases != null ) {
            		if ( t.statBases == null ) t.statBases = new List<StatModifier>();
            		t.statBases.Clear();
            		foreach ( ISettingsInstance s in statBases ) {
            			t.statBases.Add(new StatModifier() {stat = s.get<StatDef>("Stat"), value = s.get<float>("Value")});
            		}
            	}});
			
			props.visualization = new SettingsVisualizationMenuSelectEdit<ThingDef>("Plants","Select Plant",
			                                                                        ()=>DefDatabase<ThingDef>.AllDefsListForReading.Where(t=>t.plant != null).ToList(),
				def => def.defName, DefLabelProducerGeneric<ThingDef>.LabelProducerNotNull,key => DefDatabase<ThingDef>.GetNamed(key),
				f=>"Edit \""+((ISettingsInstance)f.flowScope["edit"]).getLabel()+"\"");
		}
		
		private static IEnumerable<T> getEnumEntriesFromNames<T>(IEnumerable<string> names) {
        	List<T> entriesMatching = new List<T>();
        	if ( names == null || !names.Any() ) { entriesMatching.TrimExcess(); return entriesMatching; }
        	Array entries = Enum.GetValues(typeof(T));
        	foreach ( string name in names ) {
        		foreach ( T entry in entries ) {
        			if ( Enum.GetName(typeof(T),entry).Equals(name) ) {
        				entriesMatching.Add(entry);
        				break;
        			}
        		}
        	}
    		entriesMatching.TrimExcess();
        	return entriesMatching;
		}
	}
	
	static class DefLabelProducerGeneric<T> where T : Def {
		static public Func<T, String> LabelProducerNotNull = o=>((Def)o).label.NullOrEmpty() ? ((Def)o).defName : ((Def)o).label;
		static public Func<T, String> LabelProducer = o=>o != null ? ((Def)o).label.NullOrEmpty() ? ((Def)o).defName : ((Def)o).label : "NONE";
	}
}

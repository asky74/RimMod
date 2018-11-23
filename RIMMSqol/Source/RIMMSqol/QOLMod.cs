/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 31.05.2017
 * Time: 15:57
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RIMMSqol.genericSettings;
using RIMMSqol.pathfinding;
using RIMMSqol.renderers;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;

namespace RIMMSqol
{
	/// <summary>
	/// Description of QOLMod.
	/// </summary>
	public class QOLMod : Mod
	{
		//utilities that are missing in the native code
		protected static float[] lineHeights;
		public static float LineHeight(GameFont font) {
			return lineHeights[(int)font];
		}
		public static GUIStyle FontStyle(GameFont font, TextAnchor anchor = TextAnchor.UpperLeft, bool wordWrap = true) {
			GUIStyle gUIStyle = Text.fontStyles[(int)font];
			gUIStyle.alignment = anchor;
			gUIStyle.wordWrap = wordWrap;
			return gUIStyle;
		}
		protected static GUIContent tmpTextGUIContent;
		public static Vector2 CalcSize(string text, GameFont font, TextAnchor anchor = TextAnchor.UpperLeft, bool wordWrap = true)
		{
			tmpTextGUIContent.text = text;
			return FontStyle(font, anchor, wordWrap).CalcSize(tmpTextGUIContent);
		}
		protected static MethodInfo _GUIStyleGetNumCharactersThatFitWithinWidth = typeof(GUIStyle).GetMethod("GetNumCharactersThatFitWithinWidth",BindingFlags.NonPublic|BindingFlags.Instance);
		public static string CapString(string text, float width, string suffix, out bool wasCapped, GameFont font, TextAnchor anchor = TextAnchor.UpperLeft) {
			if ( text == null ) {
				wasCapped = false;
				return null;
			}
			float widthForText = CalcSize(text,font,anchor,false).x;
			if ( width >= widthForText ) {
				wasCapped = false;
				return text;
			}
			float widthForSuffix;
			if ( suffix.NullOrEmpty() ) widthForSuffix = 0;
			else widthForSuffix = CalcSize(suffix,font,anchor,false).x;
			int charsToKeep = (int)_GUIStyleGetNumCharactersThatFitWithinWidth.Invoke(FontStyle(font, anchor, false),new object[]{text,Mathf.Max(0,width-widthForSuffix)});
			wasCapped = text.Length != charsToKeep;
			if ( widthForSuffix > 0 ) return text.Substring(0,charsToKeep)+suffix;
			else return text.Substring(0,charsToKeep);
		}
		private static float _verticalScrollbarWidth = -1;
		public static float VerticalScrollbarWidth() {
			if ( _verticalScrollbarWidth > 0 ) return _verticalScrollbarWidth;
			try {
				GUIStyle vScrollStyle = GUI.skin.verticalScrollbar;
				//UnityEngine ignores margin right for scrollbar, this is the exact width taken up by a vertical scrollbar.
				_verticalScrollbarWidth = vScrollStyle.fixedWidth + (float)vScrollStyle.margin.left;
				return _verticalScrollbarWidth;
			} catch {
				//In case this is called outside of a GUI context the skin can not be retrieved
				return 16f;
			}
		}
		private static float _horizontalScrollbarHeight = -1;
		public static float HorizontalScrollbarHeight() {
			if ( _horizontalScrollbarHeight > 0 ) return _horizontalScrollbarHeight;
			try {
				GUIStyle hScrollStyle = GUI.skin.horizontalScrollbar;
				_horizontalScrollbarHeight = hScrollStyle.fixedHeight + (float)hScrollStyle.margin.top;
				return _horizontalScrollbarHeight;
			} catch {
				//In case this is called outside of a GUI context the skin can not be retrieved
				return 16f;
			}
		}
		private static Dictionary<Color,Texture2D> cachedSolidColorTextures = new Dictionary<Color, Texture2D>();
		public static Texture2D getSolidColorTexture(Color color) {
			Texture2D tex;
			if ( !cachedSolidColorTextures.TryGetValue(color, out tex) ) {
				tex = SolidColorMaterials.NewSolidColorTexture(color);
				cachedSolidColorTextures.Add(color,tex);
			}
			return tex;
		}
		private QOLModSettings settings;
		private static List<Action<Mod>> applySettingsListeners = new List<Action<Mod>>();
		private static QOLMod singleton;
		private Flow flow;
		private Pawn dummyPawn;
		private bool skipFlowInitOnce = false;
		
		public QOLMod(ModContentPack content) : base(content)
		{
			settings = GetSettings<QOLModSettings>();
			flow = null;
			//minimalistic pawn that is used to generate tooltips
			dummyPawn = new Pawn();
			dummyPawn.gender = Gender.Male;
			dummyPawn.Name = new NameSingle("Malte");
			
			singleton = this;
			System.Reflection.FieldInfo fi = typeof(Text).GetField("lineHeights", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			lineHeights = (float[])fi.GetValue(null);
			tmpTextGUIContent = new GUIContent();
		}

		public override string SettingsCategory()
		{
			return "QoL";
		}
		
		public override void WriteSettings()
		{
			base.WriteSettings();
			if ( flow != null ) {
				flow.destroy();
				flow = null;
				//The way the events are processed a final rendering step takes place before the window is actually removed. We don't wan't to recreate the flow on that step.
				//If this is ever fixed in the base game we would only skip one frame the next time the window is opened.
				skipFlowInitOnce = true;
			}
			try {
				ApplySettings_();
			} catch {
				Log.Error("Broad error while applying settings.");
			}
		}
		
		public static void addApplySettingsListener(Action<Mod> callback) {
			applySettingsListeners.Add(callback);
		}
		
		public static bool stopSkillDecay() {
			return singleton.settings.stopSkillDecay;
		}
		
		public static bool stopTamenessDecay() {
			return singleton.settings.stopTamenessDecay;
		}
		
		public static bool preventAnimalFamilies() {
			return singleton.settings.preventAnimalFamilies;
		}
		
		public static bool useFixedNumericTextfields() {
			return singleton.settings.useFixedNumericTextfields;
		}
		
		public static void ApplySettings() {
			singleton.ApplySettings_();
		}
		
		public static QOLModSettings getSettings() {
			return singleton.settings;
		}
		
		public static float getRemnantOrderPriceFactor() {
			return singleton.settings.remnantOrderPriceFactor;
		}
		
		protected static Dictionary<int,ReflectionMethodInvoker> pfAlgorithms = new Dictionary<int, ReflectionMethodInvoker>();
		
		public static void setPFAlgorithms(Dictionary<int,ReflectionMethodInvoker> pfAlgorithms) {
			if ( pfAlgorithms != null ) QOLMod.pfAlgorithms = pfAlgorithms;
		}
		
		public static void reloadPFAlgorithms() {
			foreach ( ReflectionMethodInvoker rmi in pfAlgorithms.Values ) {
				try {
					MethodInfo miReload = rmi.instance.GetType().GetMethod("Reload");
					if ( miReload != null ) {
						miReload.Invoke(rmi.instance, null);
					}
				} catch {
					Log.Warning("Failed to determine Reload method on pathfinding algorithm \""+rmi.instance.GetType()+"\".");
				}
			}
		}
		
		public static bool hasPFAlgorithm(int algId) {
			return pfAlgorithms.ContainsKey(algId);
		}
		
		public static PawnPath doPFAlgorithm(int algId, object[] parameters) {
			ReflectionMethodInvoker invoker;
			if ( pfAlgorithms.TryGetValue(algId,out invoker) ) {
				return (PawnPath)invoker.invoke(parameters);
			}
			return null;
		}
		
		public static PawnPath doPFAlgorithm(int algId, Map map, IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode, ByteGrid avoidGrid, Area allowedArea,
		                        int costsCardinal, int costsDiagonal) {
			ReflectionMethodInvoker invoker;
			if ( pfAlgorithms.TryGetValue(algId,out invoker) ) {
				return invoker.findPath(map,start,dest,traverseParms,peMode,avoidGrid,allowedArea,costsCardinal,costsDiagonal);
			}
			return null;
		}
		
		public static float getTraitCost(Trait t, bool removal = false) {
			return getTraitCost(t.def.defName,t.CurrentData.degree,removal);
		}
		
		public static float getTraitCost(string traitDefName, int degree, bool removal = false) {
			QOLTraitCostDef defaultCost = DefDatabase<QOLTraitCostDef>.GetNamed("QOLTraitCost"+traitDefName+degree,false);
			if ( defaultCost != null ) {
				if (!removal || Math.Abs(defaultCost.costRemoval) < float.Epsilon ) return defaultCost.cost;
				else return defaultCost.costRemoval;
			}
			return 2;
		}
		
		public static float getCuMRecordFactor(RecordDef def) {
			QOLRecordFactorDef recordFactorDef = DefDatabase<QOLRecordFactorDef>.GetNamed("QOLRecordFactor"+def.defName,false);
			if ( recordFactorDef != null ) return recordFactorDef.factor;
			return 0;
		}
		
		public static float getCuMRecordPoints(Pawn p, RecordDef def) {
			return p.records.GetValue(def) * getCuMRecordFactor(def);
		}
		
		public static float getBaseCost() {
			return singleton.settings.cumBaseCost;
		}
		
		public static float getCumPointPawnToPoolConversionFactor() {
			return singleton.settings.cumPointPawnToPoolConversionFactor;
		}
		
		public static float getCumPointRemnantsToPoolConversionFactor() {
			return singleton.settings.cumPointRemnantsToPoolConversionFactor;
		}
 
		private void ApplySettings_() {
			//in case there is no mod configuration to load we initialize the settings here, so that the mod menu can be used without errors.
			SettingsInit.init();
			if (settings == null) return;
			
			SettingsStorage.ApplySettings();
			
			foreach(Action<Mod> listener in applySettingsListeners ) {
				listener(this);
			}
		}
		
		public override void DoSettingsWindowContents(Rect inRect)
		{
			if ( skipFlowInitOnce ) {
				skipFlowInitOnce = false;
				return;
			}
			if ( flow == null ) {
				List<Pair<string,Action<Flow>>> buttonInstructions = new List<Pair<string, Action<Flow>>>();
				Dictionary<string,PageRenderer> pages = new Dictionary<string, PageRenderer>();
				pages.Add("menu", new PageRenderer((f=>"Menu"), null, null)
				          .AddChild(new FilteredGridListRenderer<Pair<string,Action<Flow>>>(buttonInstructions,-1,-1)
                                     .AddChild(new ButtonTextRenderer(f=>((Pair<string,Action<Flow>>)((IterationItem)f.pageScope["curItem"]).curItem).First,f=>((Pair<string,Action<Flow>>)((IterationItem)f.pageScope["curItem"]).curItem).Second(f), GameFont.Small, 200f))));
				
				foreach ( SettingsProperties props in SettingsStorage.getAllSettingsProperties() ) {
					if ( props.visualization.displayInMenu ) {
						if ( props.visualization.displaySelection ) {
							pages.Add("edit"+props.id,editPage(props,"select"+props.id,pages));
							List<ISettingsInstance> lst = SettingsStorage.generateListForSelection(props);
							pages.Add("select"+props.id,selectPage(((ISettingsVisualizationSelection)props.visualization).selectPageTitle, "menu", lst, "edit"+props.id));
							buttonInstructions.Add(new Pair<string, Action<Flow>>(((ISettingsVisualizationMenu)props.visualization).mainMenuButtonLabel, f => f.navigate("select"+props.id)));
						} else {
							//editing a singleton object means we pass over selection but set the selected object to the singleton instance
							pages.Add("edit"+props.id,editPage(props,"menu",pages));
							ISettingsInstance inst = SettingsStorage.generateSingletonObject(props);
							buttonInstructions.Add(new Pair<string, Action<Flow>>(((ISettingsVisualizationMenu)props.visualization).mainMenuButtonLabel, 
                                      f => {f.flowScope["edit"] = inst; f.navigate("edit"+props.id); }));
						}
					}
				}
				
				flow = new Flow(pages,"menu");
			}
			flow.DoFlowContents(inRect);
		}
		
		protected PageRenderer selectPage(string pageTitle, string backPage, List<ISettingsInstance> items, string editPage) {
			return new PageRenderer(f=>pageTitle, null, ()=>backPage)
				.AddChild(new FilteredGridListRenderer<ISettingsInstance>(items,-1f,-1f,(filterExpr, item) => filterExpr.NullOrEmpty() || item.getLabel().IndexOf(filterExpr, StringComparison.OrdinalIgnoreCase) >= 0)
				          .AddChild(new ButtonTextRenderer(f => ((ISettingsInstance)((IterationItem)f.pageScope["curItem"]).curItem).getLabel(), (f => {f.flowScope["edit"] = ((IterationItem)f.pageScope["curItem"]).curItem; f.navigate(editPage); } ), GameFont.Small, 200f))
				                    .AddChild(new ButtonRenderer(Widgets.CheckboxOffTex, 
				                                       (f => {((ISettingsInstance)((IterationItem)f.pageScope["curItem"]).curItem).setActive(false); ((ISettingsInstance)((IterationItem)f.pageScope["curItem"]).curItem).reset(); }), 24f, 24f)
				                                       .hidden((f => !((ISettingsInstance)((IterationItem)f.pageScope["curItem"]).curItem).getActive())))
				                   );
		}
		
		protected PageRenderer editPage(SettingsProperties props, string backPage, Dictionary<string, PageRenderer> pages, Action<Flow> onBackHandler = null) {
			RowLayoutRenderer rows = new RowLayoutRenderer();
			List<ISettingsFieldProperties> fis = props.fields;
			foreach ( ISettingsFieldProperties p in fis ) {
				if ( !p.isList && p.isSelectable ) {
					ISettingsFieldPropertiesSelectSingle pSelect = (ISettingsFieldPropertiesSelectSingle)p;
					rows.AddChild(new ColumnLayoutRenderer()
					              .AddChild(new LabelRenderer(pSelect.label, 200f).attachTooltip(p.labelTooltip))
				            .AddChild(new ButtonTextRenderer(f => pSelect.labelProducer(((ISettingsInstance)f.flowScope["edit"]).get<object>(p.id)),
				                                            f => Find.WindowStack.Add(new Dialog_Select<object>(pSelect.onSelectItem(f), pSelect.selectableItems(f), pSelect.labelProducer)),
                                                       		GameFont.Small,200f)));
				} else if ( p.type == typeof(bool) ) {
					rows.AddChild(new ColumnLayoutRenderer()
			            	.AddChild(new LabelRenderer(p.label, 200f).attachTooltip(p.labelTooltip))
			            	.AddChild(new CheckboxRenderer(f => ((ISettingsInstance)f.flowScope["edit"]).get<bool>(p.id), (f,v) => ((ISettingsInstance)f.flowScope["edit"]).set(p.id, v))));
				} else if ( p.type == typeof(int) ) {
					rows.AddChild(new ColumnLayoutRenderer()
				            .AddChild(new LabelRenderer(p.label, 200f).attachTooltip(p.labelTooltip))
				            .AddChild(new EditNumericRenderer<int>(f=>((ISettingsInstance)f.flowScope["edit"]).get<int>(p.id),(f,v) => ((ISettingsInstance)f.flowScope["edit"]).set(p.id, v),
																200f,((ISettingsFieldPropertiesPrimitiveInt)p).minValue,((ISettingsFieldPropertiesPrimitiveInt)p).maxValue)));
				} else if ( p.type == typeof(float) ) {
					rows.AddChild(new ColumnLayoutRenderer()
				            .AddChild(new LabelRenderer(p.label, 200f).attachTooltip(p.labelTooltip))
				            .AddChild(new EditNumericRenderer<float>(f=>((ISettingsInstance)f.flowScope["edit"]).get<float>(p.id),(f,v) => ((ISettingsInstance)f.flowScope["edit"]).set(p.id, v),
			                                                    200f,((ISettingsFieldPropertiesPrimitiveFloat)p).minValue,((ISettingsFieldPropertiesPrimitiveFloat)p).maxValue)));
				} else if ( p.type == typeof(string) ) {
					rows.AddChild(new ColumnLayoutRenderer()
				            .AddChild(new LabelRenderer(p.label, 200f).attachTooltip(p.labelTooltip))
				            .AddChild(new EditTextRenderer(f=>((ISettingsInstance)f.flowScope["edit"]).get<string>(p.id),(f,v) => ((ISettingsInstance)f.flowScope["edit"]).set(p.id, v),
				                                           		200f,((ISettingsFieldPropertiesPrimitiveString)p).maxNumOfChars,((ISettingsFieldPropertiesPrimitiveString)p).inputValidator)));
				} else if ( p.type == typeof(Color) ) {
					rows.AddChild(new ColumnLayoutRenderer()
				            .AddChild(new LabelRenderer(p.label, 200f).attachTooltip(p.labelTooltip))
				            .AddChild(new EditColorRenderer(f=>((ISettingsInstance)f.flowScope["edit"]).get<Color>(p.id), 
				                                         (f,c) =>((ISettingsInstance)f.flowScope["edit"]).set<Color>(p.id,c),
				                                         200f)));
				} else if ( p.isList && ((ISettingsFieldPropertiesList)p).isListSettings ) {
					SettingsFieldPropertiesListSettings pList = (SettingsFieldPropertiesListSettings)p;
					SettingsProperties props2 = SettingsStorage.getSettingsProperties(pList.idEnclosedSettings);
					string deepEditPageId = backPage+"edit"+pList.id+props2.id;
					rows.AddChild(new ColumnLayoutRenderer()
				            .AddChild(new LabelRenderer(p.label, 400f, GameFont.Small, TextAnchor.MiddleCenter).attachTooltip(p.labelTooltip)));
					rows.AddChild(new ListRenderer(f => {
					                                                  	List<SettingsInstance> lst = ((ISettingsInstance)f.flowScope["edit"]).get<List<SettingsInstance>>(pList.id);
					                                                  	//avoiding nullpointer if the default value for a list is null or if a save state didnt contain the list
					                                                  	if ( lst == null ) {
					                                                  		lst = new List<SettingsInstance>();
					                                                  		((ISettingsInstance)f.flowScope["edit"]).set<List<SettingsInstance>>(pList.id,lst);
					                                                  	}
					                                                  	return lst;
					                                                  },400f+QOLMod.VerticalScrollbarWidth(),QOLMod.LineHeight(GameFont.Small)*3)
					                                                  
					              .AddChild(new ButtonTextRenderer(f => ((ISettingsInstance)((IterationItem)f.pageScope["curItem"]).curItem).getLabel(),
					                                               f=>{
					                                               	f.flowScope["edit"] = ((IterationItem)f.pageScope["curItem"]).curItem;
					                                               	f.navigate(deepEditPageId);
					                                               }, GameFont.Small, pList.isRemovalAllowed ? 380f : 400f))
					              .AddChild(pList.isRemovalAllowed ? new ButtonTextRenderer("-",f=> {
																	ISettingsInstance setting = (ISettingsInstance)f.flowScope["edit"];
                                                                   	SettingsInstance lstItem = (SettingsInstance)((IterationItem)f.pageScope["curItem"]).curItem;
                                                                   	f.addPostRenderCallback(fl=>{
                                           	                        	if ( setting.get<List<SettingsInstance>>(p.id).Remove(lstItem) ) setting.markAsChanged();
                                           	                        });
					                                                                                                             },GameFont.Small, 20f) : null));
					rows.AddChild(pList.isAddingAllowed ? new ColumnLayoutRenderer()
					              .AddChild(new ButtonTextRenderer("+", 
					                                               f=> {
					                                               	SettingsInstance newItem = (SettingsInstance)pList.createNewEntry();
					                                               	if ( newItem != null ) {
						                                               	((ISettingsInstance)f.flowScope["edit"]).get<List<SettingsInstance>>(p.id).Add(newItem);
						                                               	newItem.attach(((ISettingsInstance)f.flowScope["edit"]));
						                                               	((ISettingsInstance)f.flowScope["edit"]).markAsChanged();
					                                               	}
					                                               },
					                                               GameFont.Small, 400f)) : null);
					pages.Add(deepEditPageId,editPage(props2,"edit"+props.id,pages,f=>{f.flowScope["edit"] = ((ISettingsInstance)f.flowScope["edit"]).getParentObject();}));
				} else if ( p.isList && ((ISettingsFieldPropertiesList)p).isListPrimitive ) {
					SettingsFieldPropertiesListPrimitive pList = (SettingsFieldPropertiesListPrimitive)p;
					Type listItemType = pList.type.GetGenericArguments()[0];
					rows.AddChild(new ColumnLayoutRenderer()
				            .AddChild(new LabelRenderer(p.label, 400f, GameFont.Small, TextAnchor.MiddleCenter).attachTooltip(p.labelTooltip)));
					ListRenderer lstRenderer = new ListRenderer(f => {
					                                                  	IList lst = ((ISettingsInstance)f.flowScope["edit"]).get<IList>(pList.id);
					                                                  	//avoiding nullpointer if the default value for a list is null or if a save state didnt contain the list
					                                                  	if ( lst == null ) {
					                                                  		lst = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listItemType),null);
					                                                  		((ISettingsInstance)f.flowScope["edit"]).set<IList>(pList.id,lst);
					                                                  	}
					                                                  	return lst;
		                                                              },400f+QOLMod.VerticalScrollbarWidth(),QOLMod.LineHeight(GameFont.Small)*3);
					
					float editRendererWidth;
					if ( pList.listLabelProducer != null ) {
						lstRenderer.AddChild(new LabelRenderer(f => pList.listLabelProducer((ISettingsInstance)f.flowScope["edit"],f), 200f));
						editRendererWidth = 200f;
					} else editRendererWidth = 400f;
					//Currently no min max etc for the list item type
					if ( listItemType == typeof(float) ) {
						lstRenderer.AddChild(new EditNumericRenderer<float>(f=>(float)((IterationItem)f.pageScope["curItem"]).curItem,
						                                                    (f,v) => { ((ISettingsInstance)f.flowScope["edit"]).get<IList>(pList.id)[((IterationItem)f.pageScope["curItem"]).index] = v; ((ISettingsInstance)f.flowScope["edit"]).markAsChanged(); },
						                                                    editRendererWidth/*,((ISettingsFieldPropertiesPrimitiveFloat)p).minValue,((ISettingsFieldPropertiesPrimitiveFloat)p).maxValue*/));
					} else if ( listItemType == typeof(int) ) {
						lstRenderer.AddChild(new EditNumericRenderer<int>(f=>(int)((IterationItem)f.pageScope["curItem"]).curItem,
						                                                  (f,v) => { ((ISettingsInstance)f.flowScope["edit"]).get<IList>(pList.id)[((IterationItem)f.pageScope["curItem"]).index] = v; ((ISettingsInstance)f.flowScope["edit"]).markAsChanged(); },
						                                                  editRendererWidth/*,((ISettingsFieldPropertiesPrimitiveInt)p).minValue,((ISettingsFieldPropertiesPrimitiveInt)p).maxValue*/));
					} else if ( listItemType == typeof(bool) ) {
						lstRenderer.AddChild(new CheckboxRenderer(f=>(bool)((IterationItem)f.pageScope["curItem"]).curItem,
						                                          (f,v) => { ((ISettingsInstance)f.flowScope["edit"]).get<IList>(pList.id)[((IterationItem)f.pageScope["curItem"]).index] = v; ((ISettingsInstance)f.flowScope["edit"]).markAsChanged(); }));
					} else if ( listItemType == typeof(string) ) {
						lstRenderer.AddChild(new EditTextRenderer(f=>(string)((IterationItem)f.pageScope["curItem"]).curItem,
						                                          (f,v) => { ((ISettingsInstance)f.flowScope["edit"]).get<IList>(pList.id)[((IterationItem)f.pageScope["curItem"]).index] = v; ((ISettingsInstance)f.flowScope["edit"]).markAsChanged(); },
						                                          editRendererWidth/*,((ISettingsFieldPropertiesPrimitiveString)p).maxNumOfChars,((ISettingsFieldPropertiesPrimitiveString)p).inputValidator*/));
					}
					rows.AddChild(lstRenderer);
				} else if ( p.isList && ((ISettingsFieldPropertiesList)p).isSelectable ) {
					SettingsFieldPropertiesListSelectable pList = (SettingsFieldPropertiesListSelectable)p;
					Type listItemType = pList.type.GetGenericArguments()[0];
					rows.AddChild(new ColumnLayoutRenderer()
				            .AddChild(new LabelRenderer(p.label, 400f, GameFont.Small, TextAnchor.MiddleCenter).attachTooltip(p.labelTooltip)));
					ListRenderer lstRenderer = new ListRenderer(f => {
                      	IList lst = ((ISettingsInstance)f.flowScope["edit"]).get<IList>(pList.id);
                      	//avoiding nullpointer if the default value for a list is null or if a save state didnt contain the list
                      	if ( lst == null ) {
                      		lst = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listItemType),null);
                      		((ISettingsInstance)f.flowScope["edit"]).set<IList>(pList.id,lst);
                      	}
                      	return lst;
                      },400f+QOLMod.VerticalScrollbarWidth(),QOLMod.LineHeight(GameFont.Small)*3);
					
					lstRenderer.AddChild(new ButtonTextRenderer(f => pList.labelProducer(((IterationItem)f.pageScope["curItem"]).curItem),
				                                            f => Find.WindowStack.Add(new Dialog_Select<object>(pList.onSelectItem(f), pList.selectableItems(f), pList.labelProducer, null, pList.tooltipProducer)),
                                                       		GameFont.Small,pList.isRemovalAllowed ? 380f : 400f));
					if ( pList.tooltipProducer != null ) {
						lstRenderer.childs[0].attachTooltip(f => pList.tooltipProducer(((IterationItem)f.pageScope["curItem"]).curItem));
					}
					lstRenderer.AddChild(pList.isRemovalAllowed ? new ButtonTextRenderer("-", f => {
                     	ISettingsInstance setting = (ISettingsInstance)f.flowScope["edit"];
                     	object lstItem = ((IterationItem)f.pageScope["curItem"]).curItem;
                     	f.addPostRenderCallback(fl => {
							IList lst = setting.get<IList>(p.id);
							if (lst.Contains(lstItem)) {
								lst.Remove(lstItem);
								setting.markAsChanged();
							}});},
                     	GameFont.Small, 20f):null);
					
					rows.AddChild(lstRenderer);
					
					rows.AddChild(pList.isAddingAllowed ? new ColumnLayoutRenderer()
			              .AddChild(new ButtonTextRenderer("+", 
	                           f=> {
	                           	object newItem = pList.createNewEntry();
	                           	if ( newItem != null ) {
	                           		ISettingsInstance currentSettings = ((ISettingsInstance)f.flowScope["edit"]);
	                           		IList lst = currentSettings.get<IList>(p.id);
	                           		f.addPostRenderCallback(fl=>{
       		                        		lst.Add(newItem);
			                               	if ( pList.isSettings ) ((ISettingsInstance)newItem).attach(currentSettings);
			                               	currentSettings.markAsChanged();
           		                        });
	                           	}
	                           },
	                           GameFont.Small, 400f)) : null);
				}
			}
			
			return new PageRenderer(((ISettingsVisualizationEdit)props.visualization).getEditPageTitle, null, ()=>backPage, onBackHandler).AddChild(new VerticalScrollViewRenderer().AddChild(rows));
		}
		
		protected void addDefaultTraitCost(Dictionary<string, Dictionary<int, float>> dic, string defName, int degree, float cost) {
			Dictionary<int, float> dicDegrees;
			if ( !dic.TryGetValue(defName, out dicDegrees) ) {
				dicDegrees = new Dictionary<int, float>();
				dic.Add(defName,dicDegrees);
			}
			dicDegrees.Add(degree,cost);
		}
	}
}

/*
 * Created by SharpDevelop.
 * User: Malte Schulze
 * Date: 20.04.2018
 * Time: 02:35
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Harmony;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RIMMSqol
{
	/// <summary>
	/// Description of ThingForbiddenPredicatesFix.
	/// </summary>
	[HarmonyPatch(typeof(ForbidUtility))]
	[HarmonyPatch("IsForbidden")]
	[HarmonyPatch(new[]{typeof(Thing),typeof(Pawn)})]
	[HarmonyPatchNamespace("ForbidByContext")]
	public static class ThingForbiddenPredicatesFix
	{
		static void Postfix(Thing t, Pawn pawn, ref bool __result) {
			//if the normal forbid did not trigger we check custom restrictions
			if ( !__result && ThingForbiddenContext.checkIntent ) {
				__result = ThingForbiddenContext.IsForbidden(t.def);
			}
		}
	}
	
	public interface IIntentContext {}
	
	public class PawnSelector {
		public readonly string selectorId;
		public readonly string[] intents;
		public readonly Func<IIntentContext,bool> selector;
		
		public PawnSelector(string selectorId, string[] intents, Func<IIntentContext,bool> selector)
		{
			this.selectorId = selectorId;
			this.selector = selector;
			this.intents = intents;
		}
		
		public override int GetHashCode()
		{
			int hashCode = 0;
			unchecked {
				if (selectorId != null)
					hashCode += 1000000007 * selectorId.GetHashCode();
			}
			return hashCode;
		}

		public override bool Equals(object obj)
		{
			PawnSelector other = obj as PawnSelector;
			if (other == null)
				return false;
			return this.selectorId == other.selectorId;
		}

		public static bool operator ==(PawnSelector lhs, PawnSelector rhs) {
			if (ReferenceEquals(lhs, rhs))
				return true;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null))
				return false;
			return lhs.Equals(rhs);
		}

		public static bool operator !=(PawnSelector lhs, PawnSelector rhs) {
			return !(lhs == rhs);
		}
	}
	
	public class IntentContext_Food : IIntentContext {
		public Pawn eater;
		public Pawn getter;
		public IntentContext_Food(Pawn eater, Pawn getter) {
			this.eater = eater;
			this.getter = getter;
		}
		public override string ToString()
		{
			return string.Format("[IntentContext_Food Eater={0}, Getter={1}]", eater, getter);
		}

	}
	
	public struct IntentArguments {
		public string intent;
		public IIntentContext intentContext;
		public IntentArguments(string intent, IIntentContext context) {
			this.intentContext = context;
			this.intent = intent;
		}
	}
	
	public static class ThingForbiddenContext {
		[ThreadStatic]
		public static bool checkIntent = false;
		[ThreadStatic]
		static Stack<IntentArguments> p;
		[ThreadStatic]
		static HashSet<ThingDef> forbiddenThingsInThisContext;
		
		const int monitorTimeout = 10000;
		static ReaderWriterLock rwl = new ReaderWriterLock();
		static Dictionary<PawnSelector,IEnumerable<ThingDef>> ruleset = new Dictionary<PawnSelector, IEnumerable<ThingDef>>();
		static Dictionary<string,Dictionary<PawnSelector,IEnumerable<ThingDef>>> lookupRulesetViaIntent = new Dictionary<string, Dictionary<PawnSelector, IEnumerable<ThingDef>>>();
		
		public static bool IsForbidden(ThingDef def) {
			if ( forbiddenThingsInThisContext == null ) LazyInitThings();
			return forbiddenThingsInThisContext.Contains(def);
		}
		
		static void LazyInitThings() {
			forbiddenThingsInThisContext = new HashSet<ThingDef>();
			if (p == null || !p.Any()) return;
			
			try {
				rwl.AcquireReaderLock(monitorTimeout);
				try {
					IntentArguments ia = p.Peek();
					Dictionary<PawnSelector,IEnumerable<ThingDef>> rs;
					if ( lookupRulesetViaIntent.TryGetValue(ia.intent, out rs) ) {
						foreach ( KeyValuePair<PawnSelector,IEnumerable<ThingDef>> e in rs ) {
							if ( e.Key.selector(ia.intentContext) ) {
								forbiddenThingsInThisContext.UnionWith(e.Value);
							}
						}
					}
				} finally {
					rwl.ReleaseReaderLock();
				}
			} catch (ApplicationException) { }
		}
		
		public static void RecordIntent(string intentId, IIntentContext context) {
			if ( p == null ) p = new Stack<IntentArguments>();
			ThingForbiddenContext.p.Push(new IntentArguments(intentId,context));
			forbiddenThingsInThisContext = null;
			checkIntent = true;
		}
		
		public static void ReleaseIntent() {
			forbiddenThingsInThisContext = null;
			if (p != null && p.Any()) ThingForbiddenContext.p.Pop();
			checkIntent = p != null && p.Any();
		}
		
		public static bool SetRule(string selectorId, string[] intents, Func<IIntentContext,bool> pawnSelection, IEnumerable<ThingDef> forbiddenThings) {
			if ( selectorId == null || pawnSelection == null ) return false;
			try {
				rwl.AcquireWriterLock(monitorTimeout);
				try {
					PawnSelector ps = new PawnSelector(selectorId, intents, pawnSelection);
					ruleset[ps] = forbiddenThings ?? Enumerable.Empty<ThingDef>();
					foreach ( string intent in intents ) { 
						Dictionary<PawnSelector, IEnumerable<ThingDef>> r;
						if ( !lookupRulesetViaIntent.TryGetValue(intent, out r) ) {
							r = new Dictionary<PawnSelector, IEnumerable<ThingDef>>();
							lookupRulesetViaIntent[intent] = r;
						}
						r[ps] = ruleset[ps];
					}
					return true;
				} finally {
					rwl.ReleaseWriterLock();
				}
			} catch (ApplicationException) { }
			
			return false;
		}
		
		public static bool RemoveRule(string selectorId) {
			if ( selectorId == null ) return false;
			
			try {
				rwl.AcquireWriterLock(monitorTimeout);
				try {
					PawnSelector ps = new PawnSelector(selectorId, null, null);
					ruleset.Remove(ps);
					foreach ( KeyValuePair<string,Dictionary<PawnSelector, IEnumerable<ThingDef>>> p in lookupRulesetViaIntent ) {
						p.Value.Remove(ps);
					}
					lookupRulesetViaIntent.RemoveAll(p=>p.Value.Count <= 0);
					return true;
				} finally {
					rwl.ReleaseWriterLock();
				}
			} catch (ApplicationException) { }
			
			return false;
		}
		
		public static bool HasRule(string selectorId) {
			if ( selectorId == null ) return false;
			
			try {
				rwl.AcquireReaderLock(monitorTimeout);
				try {
					return ruleset.ContainsKey(new PawnSelector(selectorId, null, null));
				} finally {
					rwl.ReleaseReaderLock();
				}
			} catch (ApplicationException) { }
			
			return false;
		}
	}
	
	[HarmonyPatch(typeof(FoodUtility))]
	[HarmonyPatch("TryFindBestFoodSourceFor")]
	[HarmonyPatchNamespace("ForbidByContext")]
	public static class ThinkResultContextFood1
	{
		static bool Prefix(Pawn getter, Pawn eater) {
			//TryFindBestFoodSourceFor(Pawn getter, Pawn eater, bool desperate, out Thing foodSource, out ThingDef foodDef, bool canRefillDispenser = true, bool canUseInventory = true, bool allowForbidden = false, bool allowCorpse = true, bool allowSociallyImproper = false, bool allowHarvest = false)
			ThingForbiddenContext.RecordIntent("QOL.food", new IntentContext_Food(eater,getter));
			return true;
		}
		
		static void Postfix() {
			ThingForbiddenContext.ReleaseIntent();
		}
	}
	
	[HarmonyPatch(typeof(FoodUtility))]
	[HarmonyPatch("BestFoodSourceOnMap")]
	[HarmonyPatchNamespace("ForbidByContext")]
	public static class ThinkResultContextFood2
	{
		static bool Prefix(Pawn getter, Pawn eater) {
			//public static Thing BestFoodSourceOnMap(Pawn getter, Pawn eater, bool desperate, out ThingDef foodDef, FoodPreferability maxPref = FoodPreferability.MealLavish, bool allowPlant = true, bool allowDrug = true, bool allowCorpse = true, bool allowDispenserFull = true, bool allowDispenserEmpty = true, bool allowForbidden = false, bool allowSociallyImproper = false, bool allowHarvest = false)
			ThingForbiddenContext.RecordIntent("QOL.food", new IntentContext_Food(eater,getter));
			return true;
		}
		
		static void Postfix() {
			ThingForbiddenContext.ReleaseIntent();
		}
	}
}

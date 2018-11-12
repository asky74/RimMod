using UnityEngine; 
using Verse;  
using Verse.Sound;
using RimWorld;

namespace GaussWeapons
{
    public class Projectile_Gauss : Projectile
    {
        // Variables.
        public int tickCounter = 0;
        public Thing hitThing = null;

        // Comps
        public CompExtraDamage compED;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad); 
            compED = this.GetComp<CompExtraDamage>();
        }

        /// <summary>
        /// Impacts a pawn/object or the ground.
        /// </summary>
        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            if (hitThing != null)
            {
                ThingDef equipmentDef = this.equipmentDef;
                DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount, ArmorPenetration, this.ExactRotation.eulerAngles.y, this.launcher, null, equipmentDef);
                hitThing.TakeDamage(dinfo);

                if (hitThing is Pawn pawn && !pawn.Downed && Rand.Value < compED.chanceToProc)
                {
                    MoteMaker.ThrowMicroSparks(this.destination, Map);
                    hitThing.TakeDamage(new DamageInfo(DefDatabase<DamageDef>.GetNamed(compED.damageDef, true), compED.damageAmount, ArmorPenetration, this.ExactRotation.eulerAngles.y, this.launcher, null, null));
                }
            }
            else
            {
                SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(base.Position, map, false));
                MoteMaker.MakeStaticMote(this.ExactPosition, map, ThingDefOf.Mote_ShotHit_Dirt, 1f);
                ThrowMicroSparksBlue(this.ExactPosition, Map);
            }
        }

        public static void ThrowMicroSparksBlue(Vector3 loc, Map map)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDef.Named("Mote_MicroSparksBlue"), null);
            moteThrown.Scale = Rand.Range(0.8f, 1.2f);
            moteThrown.rotationRate = Rand.Range(-12f, 12f);
            moteThrown.exactPosition = loc;
            moteThrown.exactPosition -= new Vector3(0.5f, 0f, 0.5f);
            moteThrown.exactPosition += new Vector3(Rand.Value, 0f, Rand.Value);
            moteThrown.SetVelocity((float)Rand.Range(35, 45), 1.2f);
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }
    }
}

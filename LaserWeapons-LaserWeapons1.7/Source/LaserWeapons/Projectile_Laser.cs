using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace LaserWeapons
{
    public class Projectile_Laser : Projectile
    {
        // Variables.
        public int tickCounter = 0;
        public Thing hitThing = null;

        // Comps
        public CompExtraDamage compED;

        // Draw variables.
        public Material preFiringTexture;
        public Material postFiringTexture;
        public Matrix4x4 drawingMatrix = default(Matrix4x4);
        public Vector3 drawingScale;
        public Vector3 drawingPosition;
        public float drawingIntensity = 0f;
        public Material drawingTexture;

        // Custom XML variables.
        public float preFiringInitialIntensity = 0f;
        public float preFiringFinalIntensity = 0f;
        public float postFiringInitialIntensity = 0f;
        public float postFiringFinalIntensity = 0f;
        public int preFiringDuration = 0;
        public int postFiringDuration = 0;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            drawingTexture = def.DrawMatSingle;
            compED = GetComp<CompExtraDamage>();
        }

        /// <summary>
        /// Get parameters from XML.
        /// </summary>
        public void GetParametersFromXml()
        {
            ThingDef_LaserProjectile additionalParameters = def as ThingDef_LaserProjectile;

            preFiringDuration = additionalParameters.preFiringDuration;
            postFiringDuration = additionalParameters.postFiringDuration;

            // Draw.
            preFiringInitialIntensity = additionalParameters.preFiringInitialIntensity;
            preFiringFinalIntensity = additionalParameters.preFiringFinalIntensity;
            postFiringInitialIntensity = additionalParameters.postFiringInitialIntensity;
            postFiringFinalIntensity = additionalParameters.postFiringFinalIntensity;
        }

        /// <summary>
        /// Save/load data from a savegame file (apparently not used for projectile for now).
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref tickCounter, "tickCounter", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                GetParametersFromXml();
            }
        }

        /// <summary>
        /// Main projectile sequence.
        /// </summary>
        public override void Tick()
        {
            // Directly call the Projectile base Tick function (we want to completely override the Projectile Tick() function).
            //((ThingWithComponents)this).Tick(); // Does not work...

            if (tickCounter == 0)
            {
                GetParametersFromXml();
                PerformPreFiringTreatment();
            }

            // Pre firing.
            if (tickCounter < preFiringDuration)
            {
                GetPreFiringDrawingParameters();
            }
            // Firing.
            else if (tickCounter == preFiringDuration)
            {
                Fire();
                GetPostFiringDrawingParameters();
            }
            // Post firing.
            else
            {
                GetPostFiringDrawingParameters();
            }

            if (tickCounter == (preFiringDuration + postFiringDuration))
            {
                Destroy(DestroyMode.Vanish);
            }
            if (launcher is Pawn)
            {
                Pawn launcherPawn = launcher as Pawn;
                if (((launcherPawn.stances.curStance is Stance_Warmup) == false)
                    && ((launcherPawn.stances.curStance is Stance_Cooldown) == false)
                    || ((launcherPawn.Dead) == true))
                {
                    Destroy(DestroyMode.Vanish);
                }
            }

            tickCounter++;
        }

        /// <summary>
        /// Performs prefiring treatment: data initalization.
        /// </summary>
        public virtual void PerformPreFiringTreatment()
        {
            DetermineImpactExactPosition();
            Vector3 cannonMouthOffset = ((destination - origin).normalized * 0.9f);
            drawingScale = new Vector3(1f, 1f, (destination - origin).magnitude - cannonMouthOffset.magnitude);
            drawingPosition = origin + (cannonMouthOffset / 2) + ((destination - origin) / 2) + Vector3.up * def.Altitude;
            drawingMatrix.SetTRS(drawingPosition, ExactRotation, drawingScale);
        }

        /// <summary>
        /// Gets the prefiring drawing parameters.
        /// </summary>
        public virtual void GetPreFiringDrawingParameters()
        {
            if (preFiringDuration != 0)
            {
                drawingIntensity = preFiringInitialIntensity + (preFiringFinalIntensity - preFiringInitialIntensity) * (float)tickCounter / (float)preFiringDuration;
            }
        }

        /// <summary>
        /// Gets the postfiring drawing parameters.
        /// </summary>
        public virtual void GetPostFiringDrawingParameters()
        {
            if (postFiringDuration != 0)
            {
                drawingIntensity = postFiringInitialIntensity + (postFiringFinalIntensity - postFiringInitialIntensity) * (((float)tickCounter - (float)preFiringDuration) / (float)postFiringDuration);
            }
        }

        /// <summary>
        /// Checks for colateral targets (cover, neutral animal, pawn) along the trajectory.
        /// </summary>
        protected void DetermineImpactExactPosition()
        {
            // We split the trajectory into small segments of approximatively 1 cell size.
            Vector3 trajectory = (destination - origin);
            int numberOfSegments = (int)trajectory.magnitude;
            Vector3 trajectorySegment = (trajectory / trajectory.magnitude);

            Vector3 temporaryDestination = origin; // Last valid tested position in case of an out of boundaries shot.
            Vector3 exactTestedPosition = origin;
            IntVec3 testedPosition = exactTestedPosition.ToIntVec3();

            for (int segmentIndex = 1; segmentIndex <= numberOfSegments; segmentIndex++)
            {
                exactTestedPosition += trajectorySegment;
                testedPosition = exactTestedPosition.ToIntVec3();

                if (!exactTestedPosition.InBounds(Map))
                {
                    destination = temporaryDestination;
                    break;
                }

                if (!def.projectile.flyOverhead && segmentIndex >= 4)
                {
                    List<Thing> list = Map.thingGrid.ThingsListAt(base.Position);
                    for (int i = 0; i < list.Count; i++)
                    {
                        Thing current = list[i];

                        // Check impact on a wall.
                        if (current.def.Fillage == FillCategory.Full)
                        {
                            destination = testedPosition.ToVector3Shifted() + new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
                            hitThing = current;
                            break;
                        }

                        // Check impact on a pawn.
                        if (current.def.category == ThingCategory.Pawn)
                        {
                            Pawn pawn = current as Pawn;
                            float chanceToHitCollateralTarget = 0.45f;
                            if (pawn.Downed)
                            {
                                chanceToHitCollateralTarget *= 0.1f;
                            }
                            float targetDistanceFromShooter = (ExactPosition - origin).MagnitudeHorizontal();
                            if (targetDistanceFromShooter < 4f)
                            {
                                chanceToHitCollateralTarget *= 0f;
                            }
                            else
                            {
                                if (targetDistanceFromShooter < 7f)
                                {
                                    chanceToHitCollateralTarget *= 0.5f;
                                }
                                else
                                {
                                    if (targetDistanceFromShooter < 10f)
                                    {
                                        chanceToHitCollateralTarget *= 0.75f;
                                    }
                                }
                            }
                            chanceToHitCollateralTarget *= pawn.RaceProps.baseBodySize;

                            if (Rand.Value < chanceToHitCollateralTarget)
                            {
                                destination = testedPosition.ToVector3Shifted() + new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
                                hitThing = (Thing)pawn;
                                break;
                            }
                        }
                    }
                }

                temporaryDestination = exactTestedPosition;
            }
        }

        /// <summary>
        /// Manages the projectile damage application.
        /// </summary>
        public virtual void Fire()
        {
            ApplyDamage(hitThing);
        }

        /// <summary>
        /// Applies damage on a collateral pawn or an object.
        /// </summary>
        protected void ApplyDamage(Thing hitThing)
        {
            if (hitThing != null)
            {
                // Impact collateral target.
                Impact(hitThing);
            }
            else
            {
                ImpactSomething();
            }
        }

        /// <summary>
        /// Computes what should be impacted in the DestinationCell.
        /// </summary>
        protected void ImpactSomething() 
        {
            // Impact the initial targeted pawn.
            if (usedTarget.Thing != null)
            {
                if (usedTarget.Thing is Pawn pawn && pawn.Downed && (origin - destination).magnitude > 5f && Rand.Value < 0.2f)
                {
                    Impact(null);
                    return;
                }
                this.Impact(usedTarget.Thing);
                return;
            }
            else
            {
                // Impact a pawn in the destination cell if present.
                Thing thing = Map.thingGrid.ThingAt(DestinationCell, ThingCategory.Pawn);
                if (thing != null)
                {
                    Impact(thing);
                    return;
                }
                // Impact any cover object.
                foreach (Thing current in Map.thingGrid.ThingsAt(DestinationCell))
                {
                    if (current.def.fillPercent > 0f || current.def.passability != Traversability.Standable)
                    {
                        Impact(current);
                        return;
                    }
                }
                Impact(null);
                return;
            }
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
                DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef);
                hitThing.TakeDamage(dinfo);
                if (hitThing is Pawn pawn && !pawn.Downed && Rand.Value < compED.chanceToProc)
                {
                    MoteMaker.ThrowMicroSparks(destination, Map);
                    hitThing.TakeDamage(new DamageInfo(DefDatabase<DamageDef>.GetNamed(compED.damageDef, true), compED.damageAmount, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, null));
                }
            }
            else
            {
                SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(base.Position, map, false));
                MoteMaker.MakeStaticMote(ExactPosition, map, ThingDefOf.Mote_ShotHit_Dirt, 1f);
                ThrowMicroSparksRed(ExactPosition, Map);
            }
        }

        /// <summary>
        /// Draws the laser ray.
        /// </summary>
        public override void Draw()
        {
            Comps_PostDraw();
            UnityEngine.Graphics.DrawMesh(MeshPool.plane10, drawingMatrix, FadedMaterialPool.FadedVersionOf(drawingTexture, drawingIntensity), 0);
        }

        public static void ThrowMicroSparksRed(Vector3 loc, Map map)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDef.Named("Mote_MicroSparksRed"), null);
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

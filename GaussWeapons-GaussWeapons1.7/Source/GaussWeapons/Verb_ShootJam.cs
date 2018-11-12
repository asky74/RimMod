using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GaussWeapons
{
    public class Verb_ShootJam : Verb_LaunchProjectile
    {
        public bool isJammed = false;
        public SoundDef jamSound = SoundDef.Named("Misfire");

        protected override int ShotsPerBurst
        {
            get
            {
                return this.verbProps.burstShotCount;
            }
        }

        public override void WarmupComplete()
        {
            if (isJammed)
            {
                System.Random random = new System.Random();
                int num = random.Next(0, 2);
                if (num == 1)
                {
                    isJammed = false;
                    Vector3 loc = new Vector3((float)this.caster.Position.x + 1f, (float)this.caster.Position.y, (float)this.caster.Position.z + 1f);
                    MoteMaker.ThrowText(loc, caster.Map, "Jam Cleared", Color.white);

                    this.CasterPawn.def.soundInteract.PlayOneShot(new TargetInfo(caster.Position, caster.Map, false));
                    return;
                }
                else
                {
                    return;
                }
            }
            int jamChance = 0;
            this.caster.TryGetQuality(out QualityCategory qual);
            switch (qual)
            {
                case QualityCategory.Awful:
                    jamChance = 50;
                    break;
                case QualityCategory.Poor:
                    jamChance = 60;
                    break;
                case QualityCategory.Normal:
                    jamChance = 70;
                    break;
                case QualityCategory.Good:
                    jamChance = 80;
                    break;
                case QualityCategory.Excellent:
                    jamChance = 90;
                    break;
                case QualityCategory.Masterwork:
                    jamChance = 100;
                    break;
                case QualityCategory.Legendary:
                    jamChance = 10000;
                    break;
                default:
                    jamChance = 60;
                    break;
            }

            if(Rand.Range(1, jamChance) == 1)
            {
                Vector3 loc = new Vector3((float)this.caster.Position.x + 1f, (float)this.caster.Position.y, (float)this.caster.Position.z + 1f);
                MoteMaker.ThrowText(loc, caster.Map, "Jammed", Color.white);
                jamSound.PlayOneShot(new TargetInfo(caster.Position, caster.Map, false));
                isJammed = true;
                return;
            }

            base.WarmupComplete();
            if (base.CasterIsPawn && base.CasterPawn.skills != null)
            {
                float xp = 10f;
                if (this.currentTarget.Thing != null && this.currentTarget.Thing.def.category == ThingCategory.Pawn)
                {
                    xp = this.currentTarget.Thing.HostileTo(this.caster) ? 240f : 50f;
                }
                base.CasterPawn.skills.Learn(SkillDefOf.Shooting, xp);
            }
        }
    }
}

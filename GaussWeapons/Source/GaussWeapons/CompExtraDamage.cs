using Verse;

namespace GaussWeapons
{
    public class CompExtraDamage : ThingComp
    {
        public CompProperties_ExtraDamage compProp;
        public string damageDef;
        public int damageAmount;
        public float chanceToProc;
        public int count;
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            this.compProp = (props as CompProperties_ExtraDamage);
            if (this.compProp != null)
            {
                this.damageDef = this.compProp.damageDef;
                this.damageAmount = this.compProp.damageAmount;
                this.chanceToProc = this.compProp.chanceToProc;
            }
            else
            {
                Log.Warning("Could not find a CompProperties_ExtraDamage for CompExtraDamage.");
                this.count = 9876;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.count, "count", 1, false);
        }
    }
}

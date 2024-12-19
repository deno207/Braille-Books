using RimWorld;
using Verse;

namespace BrailleBooks {
    [DefOf]
    public static class BrailleDefOf {

        static BrailleDefOf() {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf));
            DefOfHelper.EnsureInitializedInCtor(typeof(StatDefOf));
        }

        public static JobDef BrailleReading;

        public static StatDef BrailleReadingSpeed;
    }
}
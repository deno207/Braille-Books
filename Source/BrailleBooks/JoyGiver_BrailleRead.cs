using RimWorld;
using Verse;
using Verse.AI;

namespace BrailleBooks {
    public class JoyGiver_BrailleRead : JoyGiver {

        public override bool CanBeGivenTo(Pawn pawn)
        {
            return BrailleBookUtility.CanReadNow(pawn) && !PawnUtility.WillSoonHaveBasicNeed(pawn, 0.05f) && base.CanBeGivenTo(pawn);
        }

        public override Job TryGiveJob(Pawn pawn) {
            BrailleBook t;
            if (BrailleBookUtility.TryGetRandomBookToRead(pawn, out t)) {
                return JobMaker.MakeJob(this.def.jobDef, t);
            }
            return null;
        }
    }
}

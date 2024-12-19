using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BrailleBooks {

    public class BrailleBook : Book {

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn) {
            FloatMenuOption floatMenuOption = new FloatMenuOption("AssignReadNow".Translate(this.Label), delegate () {
                this.PawnReadNow(selPawn);
            }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
            string arg;
            if (!BrailleBookUtility.CanReadBook(this, selPawn, out arg)) {
                floatMenuOption.Label = string.Format("{0}: {1}", "AssignCannotReadNow".Translate(this.Label), arg);
                floatMenuOption.Disabled = true;
            }
            Pawn pawn = selPawn.Map.reservationManager.FirstRespectedReserver(this, selPawn, null) ?? selPawn.Map.physicalInteractionReservationManager.FirstReserverOf(this);
            if (pawn != null) {
                FloatMenuOption floatMenuOption2 = floatMenuOption;
                floatMenuOption2.Label += " (" + "ReservedBy".Translate(pawn.LabelShort, pawn) + ")";
            }
            yield return floatMenuOption;
            foreach (FloatMenuOption floatMenuOption3 in base.GetFloatMenuOptions(selPawn)) {
                String compareString = floatMenuOption3.Label.Split(":".ToCharArray())[0];
                if (compareString.Equals("AssignReadNow".Translate(this.Label)) || compareString.Equals("AssignCannotReadNow".Translate(this.Label))) {
                    continue;
                }
                yield return floatMenuOption3;
            }
            yield break;
        }

        public new void PawnReadNow(Pawn pawn) {
            Job job = JobMaker.MakeJob(BrailleDefOf.BrailleReading, this);
            pawn.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
        }

        public new void OnBookReadTick(Pawn pawn, float roomBonusFactor)
        {
            float factor = pawn.GetStatValue(BrailleDefOf.BrailleReadingSpeed, true, -1) * roomBonusFactor;
            foreach (BookOutcomeDoer bookOutcomeDoer in this.BookComp.Doers) {
                bookOutcomeDoer.OnReadingTick(pawn, factor);
            }
            MentalBreakDef breakDef;
            if (ModsConfig.AnomalyActive && this.MentalBreakChancePerHour > 0f && Rand.MTBEventOccurs(1f / this.MentalBreakChancePerHour, 2500f, 1f) && pawn.mindState.mentalBreaker.TryGetRandomMentalBreak(this.BookComp.Props.mentalBreakIntensity, out breakDef)) {
                TaggedString taggedString = "BookMentalBreakMessage".Translate(this.Label);
                pawn.mindState.mentalBreaker.TryDoMentalBreak(taggedString, breakDef);
            }
        }
    }
}
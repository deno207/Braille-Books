using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace BrailleBooks {

    public static class BrailleBookUtility {

        private static readonly List<Thing> TmpCandidates = new List<Thing>();
        private static readonly List<Thing> TmpOutcomeCandidates = new List<Thing>();

        public static bool CanReadEver(Pawn reader) {
            return reader.DevelopmentalStage != DevelopmentalStage.Baby && !BrailleDefOf.BrailleReadingSpeed.Worker.IsDisabledFor(reader) && reader.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
        }

        public static bool CanReadNow(Pawn reader) {
            return BrailleBookUtility.CanReadEver(reader) && BrailleBookUtility.GetReadingModifier(reader) > 0f;
        }

        public static bool CanReadBook(Book book, Pawn reader, out string reason) {
            if (!book.IsReadable) {
                reason = "BookNotReadable".Translate(book.Named("BOOK"));
                return false;
            }
            DevelopmentalStage developmentalStageFilter = book.BookComp.Props.developmentalStageFilter;
            if (!developmentalStageFilter.HasAny(reader.DevelopmentalStage)) {
                string arg = developmentalStageFilter.ToCommaList(false);
                reason = "BookCantBeStage".Translate(reader.Named("PAWN"), arg.Named("STAGES"));
                return false;
            }
            if (!reader.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) {
                reason = "BookCantHold".Translate(reader.Named("PAWN"));
                return false;
            }
            if (!BrailleBookUtility.CanReadEver(reader)) {
                reason = "BookCantRead".Translate(reader.Named("PAWN"));
                return false;
            }
            reason = null;
            return true;
        }

        public static float GetReadingModifier(Pawn reader) {
            if (reader == null || BrailleDefOf.BrailleReadingSpeed.Worker.IsDisabledFor(reader)) {
                return 1f;
            }
            return reader.GetStatValue(BrailleDefOf.BrailleReadingSpeed, true, -1);
        }

        public static BrailleBook MakeBook(ArtGenerationContext context) {
            return BrailleBookUtility.MakeBook(BrailleBookUtility.GetBookDefs().RandomElementByWeight((ThingDef x) => x.GetCompProperties<CompProperties_Book>().pickWeight), context);
        }

        public static BrailleBook MakeBook(ThingDef def, ArtGenerationContext context) {
            ThingDef stuff = GenStuff.RandomStuffFor(def);
            Thing thing = ThingMaker.MakeThing(def, stuff);
            CompQuality compQuality = thing.TryGetComp<CompQuality>();
            if (compQuality != null) {
                compQuality.SetQuality(QualityUtility.GenerateQualityRandomEqualChance(), new ArtGenerationContext?(context));
            }
            return thing as BrailleBook;
        }

        private static List<ThingDef> GetBookDefs() {
            return (from x in DefDatabase<ThingDef>.AllDefsListForReading
                    where x.HasComp<CompBook>()
                    select x).ToList<ThingDef>();
        }

        public static float GetReadingBonus(Thing thing) {
            Room room = thing.GetRoom(RegionType.Set_All);
            if (room != null && room.ProperRoom && !room.PsychologicallyOutdoors)
            {
                return room.GetStat(RoomStatDefOf.ReadingBonus);
            }
            return 1f;
        }

        public static bool TryGetRandomBookToRead(Pawn pawn, out BrailleBook book) {
            book = null;
            BrailleBookUtility.TmpCandidates.Clear();
            BrailleBookUtility.TmpOutcomeCandidates.Clear();
            BrailleBookUtility.TmpCandidates.AddRange(from thing in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Book)
                                               where BrailleBookUtility.IsValidBook(thing, pawn)
                                               select thing);
            BrailleBookUtility.TmpCandidates.AddRange(from thing in pawn.Map.listerThings.GetThingsOfType<Building_Bookcase>().SelectMany((Building_Bookcase x) => x.HeldBooks)
                                               where BrailleBookUtility.IsValidBook(thing, pawn)
                                               select thing);
            if (BrailleBookUtility.TmpCandidates.Empty<Thing>()) {
                return false;
            }
            foreach (Thing thing2 in BrailleBookUtility.TmpCandidates) {
                BrailleBook book2;
                if ((book2 = (thing2 as BrailleBook)) != null && book2.ProvidesOutcome(pawn)) {
                    BrailleBookUtility.TmpOutcomeCandidates.Add(thing2);
                }
            }
            book = (BrailleBook)(BrailleBookUtility.TmpOutcomeCandidates.Any<Thing>() ? BrailleBookUtility.TmpOutcomeCandidates.RandomElement<Thing>() : BrailleBookUtility.TmpCandidates.RandomElement<Thing>());
            BrailleBookUtility.TmpCandidates.Clear();
            BrailleBookUtility.TmpOutcomeCandidates.Clear();
            return true;
        }

        private static bool IsValidBook(Thing thing, Pawn pawn) {
            if (thing is BrailleBook && !thing.IsForbiddenHeld(pawn)) {
                Pawn_ReadingTracker reading = pawn.reading;
                if (((reading != null) ? reading.CurrentPolicy : null) != null && pawn.reading.CurrentPolicy.defFilter.Allows(thing) && pawn.reading.CurrentPolicy.effectFilter.Allows(thing) && pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.None, 1, -1, null, false)) {
                    return thing.IsPoliticallyProper(pawn);
                }
            }
            return false;
        }

        public static float GetResearchExpForQuality(QualityCategory quality) {
            return BrailleBookUtility.QualityResearchExpTick.Evaluate((float)quality);
        }

        public static float GetAnomalyExpForQuality(QualityCategory quality) {
            return BrailleBookUtility.QualityAnomalyExpTick.Evaluate((float)quality);
        }

        public static float GetSkillExpForQuality(QualityCategory quality) {
            return BrailleBookUtility.QualitySkillExpTick.Evaluate((float)quality);
        }

        public static float GetNovelJoyFactorForQuality(QualityCategory quality) {
            return BrailleBookUtility.QualityJoyFactor.Evaluate((float)quality);
        }

        private static readonly SimpleCurve QualityResearchExpTick = new SimpleCurve {
            {
                new CurvePoint(0f, 0.008f),
                true
            },
            {
                new CurvePoint(1f, 0.012f),
                true
            },
            {
                new CurvePoint(2f, 0.016f),
                true
            },
            {
                new CurvePoint(3f, 0.02f),
                true
            },
            {
                new CurvePoint(4f, 0.024f),
                true
            },
            {
                new CurvePoint(5f, 0.028f),
                true
            },
            {
                new CurvePoint(6f, 0.032f),
                true
            }
        };

        private static readonly SimpleCurve QualityAnomalyExpTick = new SimpleCurve {
            {
                new CurvePoint(0f, 3E-05f),
                true
            },
            {
                new CurvePoint(1f, 6E-05f),
                true
            },
            {
                new CurvePoint(2f, 9E-05f),
                true
            },
            {
                new CurvePoint(3f, 0.00012f),
                true
            },
            {
                new CurvePoint(4f, 0.00015f),
                true
            },
            {
                new CurvePoint(5f, 0.00018f),
                true
            },
            {
                new CurvePoint(6f, 0.00021f),
                true
            }
        };

        private static readonly SimpleCurve QualitySkillExpTick = new SimpleCurve {
            {
                new CurvePoint(0f, 0.05f),
                true
            },
            {
                new CurvePoint(1f, 0.075f),
                true
            },
            {
                new CurvePoint(2f, 0.1f),
                true
            },
            {
                new CurvePoint(3f, 0.125f),
                true
            },
            {
                new CurvePoint(4f, 0.15f),
                true
            },
            {
                new CurvePoint(5f, 0.175f),
                true
            },
            {
                new CurvePoint(6f, 0.2f),
                true
            }
        };

        private static readonly SimpleCurve QualityJoyFactor = new SimpleCurve {
            {
                new CurvePoint(0f, 1.2f),
                true
            },
            {
                new CurvePoint(1f, 1.4f),
                true
            },
            {
                new CurvePoint(2f, 1.6f),
                true
            },
            {
                new CurvePoint(3f, 1.8f),
                true
            },
            {
                new CurvePoint(4f, 2f),
                true
            },
            {
                new CurvePoint(5f, 2.25f),
                true
            },
            {
                new CurvePoint(6f, 2.5f),
                true
            }
        };
    }
}
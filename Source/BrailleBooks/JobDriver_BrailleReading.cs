using RimWorld;
using Verse;
using Verse.AI;

namespace BrailleBooks {

    public class JobDriver_BrailleReading : JobDriver_Reading {
        private bool isLearningDesire = false;
        private bool isReading = false;

        public new bool IsReading {
            get {
                return this.isReading;
            }
        }

        public override void Notify_Starting() {
            base.Notify_Starting();
            Pawn pawn2 = this.pawn;
            this.isLearningDesire = ((pawn2 != null) ? pawn2.learning : null) != null && this.pawn.learning.ActiveLearningDesires.Contains(LearningDesireDefOf.Reading);
        }

        private Toil ReadBook(int duration) {
            Log.Warning("Creating Read Book Toil");
            Toil toil = Toils_General.Wait(duration, TargetIndex.None);
            toil.debugName = "Reading";
            toil.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            toil.handlingFacing = true;
            toil.initAction = delegate () {
                Log.Warning("Starting Read Book Toil");
                this.Book.IsOpen = true;
                this.pawn.pather.StopDead();
                this.job.showCarryingInspectLine = false;
            };
            toil.tickAction = delegate () {
                Log.Warning("Starting Reading Tick");
                if (this.job.GetTarget(TargetIndex.B).IsValid) {
                    this.pawn.rotationTracker.FaceCell(this.job.GetTarget(TargetIndex.B).Cell);
                }
                else if (this.Book.Spawned) {
                    this.pawn.rotationTracker.FaceCell(this.Book.Position);
                }
                else if (this.pawn.Rotation == Rot4.North) {
                    this.pawn.Rotation = new Rot4(Rand.Range(1, 4));
                }
                Log.Warning("Getting Reading Bonus");
                float readingBonus = BrailleBookUtility.GetReadingBonus(this.pawn);
                this.isReading = true;
                Log.Warning("Doing On Book Tick");
                this.Book.OnBookReadTick(this.pawn, readingBonus);
                Log.Warning("Checking Intellectual Skill");
                Pawn_SkillTracker skills = this.pawn.skills;
                if (skills != null) {
                    skills.Learn(SkillDefOf.Intellectual, 0.1f, false, false);
                }
                Log.Warning("Adding Comfort");
                this.pawn.GainComfortFromCellIfPossible(false);
                if (this.pawn.CurJob != null) {
                    Pawn_NeedsTracker needs = this.pawn.needs;
                    if (((needs != null) ? needs.joy : null) != null) {
                        JoyTickFullJoyAction fullJoyAction = JoyTickFullJoyAction.GoToNextToil;
                        if (this.pawn.CurJob.playerForced || this.pawn.learning != null) {
                            fullJoyAction = JoyTickFullJoyAction.None;
                        }
                        JoyUtility.JoyTickCheckEnd(this.pawn, fullJoyAction, this.Book.JoyFactor * readingBonus, null);
                    }
                }
                Log.Warning("Checking Learning Desire");
                if (this.isLearningDesire && this.job != null) {
                    Pawn_NeedsTracker needs2 = this.pawn.needs;
                    if (((needs2 != null) ? needs2.learning : null) != null) {
                        LearningUtility.LearningTickCheckEnd(this.pawn, this.job.playerForced);
                    }
                    else {
                        this.pawn.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
                    }
                }
                Log.Warning("checking for job override");
                if (this.pawn.IsHashIntervalTick(600)) {
                    this.pawn.jobs.CheckForJobOverride(9.1f);
                }
            };
            toil.AddEndCondition(delegate {
                string text;
                Log.Warning("Checking End Condition");
                if (!BrailleBookUtility.CanReadBook(this.Book, this.pawn, out text))
                {
                    return JobCondition.InterruptForced;
                }
                return JobCondition.Ongoing;
            });
            toil.AddFinishAction(delegate {
                Log.Warning("Finishing Reading action");
                this.Book.IsOpen = false;
                TaleRecorder.RecordTale(TaleDefOf.ReadBook, new object[]
                {
                    this.pawn,
                    this.Book
                });
                JoyUtility.TryGainRecRoomThought(this.pawn);
            });
            if (this.isLearningDesire && !this.job.playerForced) {
                toil.defaultCompleteMode = ToilCompleteMode.Never;
            }
            return toil;
        }
    }
}
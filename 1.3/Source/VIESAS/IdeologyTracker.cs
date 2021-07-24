using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace VIESAS
{
    public class IdeologyTracker : GameComponent
    {
        public Ideo originIdeo;
        public Ideo splittedIdeo;
        public int nextConversionTickCheck;
        public IdeologyTracker(Game game)
        {

        }
        private IEnumerable<Pawn> GetConvertablePawns(Ideo ideo, List<Pawn> pawns)
        {
            foreach (var pawn in pawns)
            {
                if (ideo.GetRole(pawn) is null)
                {
                    yield return pawn;
                }
            }
        }
        private IEnumerable<Pawn> GetBelievers(Ideo ideo)
        {
            foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists)
            {
                if (pawn.IsColonist && pawn.Ideo == ideo)
                {
                    yield return pawn;
                }
            }
        }
        private int GetNextConversionTickCheck()
        {
            return (int)(Find.TickManager.TicksGame + (GenDate.TicksPerDay * (VIESASMod.settings.ideologyConversionCheckDaysInterval + Rand.Range(-0.5f, 0.5f))));
        }
        public override void LoadedGame()
        {
            base.LoadedGame();
            if (nextConversionTickCheck == 0)
            {
                nextConversionTickCheck = GetNextConversionTickCheck();
            }
        }
        public override void StartedNewGame()
        {
            base.StartedNewGame();
            if (nextConversionTickCheck == 0)
            {
                nextConversionTickCheck = GetNextConversionTickCheck();
            }
        }
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager.TicksGame >= nextConversionTickCheck)
            {
                if (IdeoSplitCanOccur())
                {
                    var believers = GetBelievers(Faction.OfPlayer.ideos.PrimaryIdeo).ToList();
                    if (believers.Count >= VIESASMod.settings.minimumColonistCountForSchismToOccur)
                    {
                        var convertablePawns = GetConvertablePawns(Faction.OfPlayer.ideos.PrimaryIdeo, believers).ToList();
                        var countToConvert = Rand.RangeInclusive(1, (int)(convertablePawns.Count * VIESASMod.settings.pctOfColonistsToTurnToNewIdeology));
                        var colonistsToConvert = convertablePawns.InRandomOrder().Take(countToConvert).ToList();
                        var newIdeo = GenerateNewSplittedIdeoFrom(Faction.OfPlayer.ideos.PrimaryIdeo);
                        Find.IdeoManager.Add(newIdeo);

                        StringBuilder stringBuilder = new StringBuilder();
                        foreach (var pawn in colonistsToConvert)
                        {
                            var oldCertainty = pawn.ideo.Certainty;
                            pawn.ideo.SetIdeo(newIdeo);
                            Traverse.Create(pawn.ideo).Field("certainty").SetValue(oldCertainty);
                            stringBuilder.AppendLine("  - " + pawn.LabelShort);
                        }
                        Find.LetterStack.ReceiveLetter("VIESAS.IdeoSplit".Translate(newIdeo.name), "VIESAS.IdeoSplitDesc".Translate(newIdeo.name, stringBuilder.ToString().TrimEndNewlines()), 
                            LetterDefOf.NegativeEvent, colonistsToConvert);
                        originIdeo = Faction.OfPlayer.ideos.PrimaryIdeo;
                        splittedIdeo = newIdeo;
                        nextConversionTickCheck = GetNextConversionTickCheck();
                        Faction.OfPlayer.ideos.Notify_ColonistChangedIdeo();
                    }
                }
                else
                {
                    nextConversionTickCheck = GetNextConversionTickCheck();
                }
            }
        }

        private bool IdeoSplitCanOccur()
        {
            if (originIdeo != null)
            {
                if (!NoBelieversIn(originIdeo))
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
            if (splittedIdeo != null)
            {
                if (!NoBelieversIn(splittedIdeo))
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
            return false;
        }

        private bool NoBelieversIn(Ideo ideo)
        {
            if (!GetBelievers(ideo).Any())
            {
                Log.Message("No believers in " + ideo);
                Find.IdeoManager.Remove(ideo);
                return true;
            }
            return false;
        }
        private Ideo GenerateNewSplittedIdeoFrom(Ideo oldIdeo)
        {
            var newIdeo = IdeoGenerator.MakeIdeo(oldIdeo.foundation.def);
            var parms = new IdeoGenerationParms(Faction.OfPlayer.def);
            RandomizeMemes(newIdeo, oldIdeo, parms);
            newIdeo.foundation.RandomizeCulture(parms);
            newIdeo.foundation.RandomizePlace();
            if (newIdeo.foundation is IdeoFoundation_Deity ideoFoundation_Deity)
            {
                ideoFoundation_Deity.GenerateDeities();
            }
            newIdeo.foundation.GenerateTextSymbols();
            newIdeo.foundation.RandomizePrecepts(init: false, parms);
            newIdeo.foundation.GenerateLeaderTitle();
            newIdeo.foundation.RandomizeIcon();
            newIdeo.foundation.InitPrecepts(parms);
            newIdeo.RecachePrecepts();
            newIdeo.foundation.ideo.RegenerateDescription(force: true);
            newIdeo.foundation.RandomizeStyles();

            newIdeo.primaryFactionColor = oldIdeo.primaryFactionColor;
            return newIdeo;
        }

        private void RandomizeMemes(Ideo newIdeo, Ideo oldIdeo, IdeoGenerationParms parms)
        {
            var memesCount = VIESASMod.settings.amountOfMemesChangedDuringSchism;
            var memesToAdd = IdeoUtilityCustom.GenerateRandomMemes(memesCount, parms).Where(x => !oldIdeo.HasMeme(x));
            newIdeo.memes.AddRange(oldIdeo.memes);
            for (var i = 0; i < memesCount; i++)
            {
                var memeToRemove = newIdeo.memes.RandomElement();
                newIdeo.memes.Remove(memeToRemove);
                newIdeo.memes.Add(memesToAdd.RandomElement());
            }
            newIdeo.SortMemesInDisplayOrder();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextConversionTickCheck, "nextConversionTickCheck");
            Scribe_References.Look(ref originIdeo, "originIdeo");
            Scribe_References.Look(ref splittedIdeo, "splittedIdeo");
        }
    }
}

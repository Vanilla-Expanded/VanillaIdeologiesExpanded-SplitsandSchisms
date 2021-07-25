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
                    TrySplitIdeo();
                }
                else
                {
                    nextConversionTickCheck = GetNextConversionTickCheck();
                }
            }
        }

        public void TrySplitIdeo()
        {
            var believers = GetBelievers(Faction.OfPlayer.ideos.PrimaryIdeo).ToList();
            if (believers.Count >= VIESASMod.settings.minimumColonistCountForSchismToOccur)
            {
                var convertablePawns = GetConvertablePawns(Faction.OfPlayer.ideos.PrimaryIdeo, believers).ToList();
                var countToConvert = Rand.RangeInclusive(1, (int)(convertablePawns.Count * VIESASMod.settings.pctOfColonistsToTurnToNewIdeology));
                var colonistsToConvert = convertablePawns.InRandomOrder().Take(countToConvert).ToList();
                StringBuilder ideoChanges = new StringBuilder();
                var newIdeo = GenerateNewSplittedIdeoFrom(Faction.OfPlayer.ideos.PrimaryIdeo, ideoChanges);
                Find.IdeoManager.Add(newIdeo);
                StringBuilder joinerDesc = new StringBuilder();

                foreach (var pawn in colonistsToConvert)
                {
                    var oldCertainty = pawn.ideo.Certainty;
                    pawn.ideo.SetIdeo(newIdeo);
                    Traverse.Create(pawn.ideo).Field("certainty").SetValue(oldCertainty);
                    joinerDesc.AppendLine("  - " + pawn.LabelShort);
                }
                Find.LetterStack.ReceiveLetter("VIESAS.IdeoSplit".Translate(newIdeo.name), "VIESAS.IdeoSplitDesc".Translate(newIdeo.name, 
                    ideoChanges.ToString().TrimEndNewlines(), joinerDesc.ToString().TrimEndNewlines()), LetterDefOf.NegativeEvent, colonistsToConvert);
                originIdeo = Faction.OfPlayer.ideos.PrimaryIdeo;
                splittedIdeo = newIdeo;
                nextConversionTickCheck = GetNextConversionTickCheck();
                Faction.OfPlayer.ideos.Notify_ColonistChangedIdeo();
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
        private Ideo GenerateNewSplittedIdeoFrom(Ideo oldIdeo, StringBuilder ideoChanges)
        {
            Log.Message(" - GenerateNewSplittedIdeoFrom - var newIdeo = IdeoGenerator.MakeIdeo(oldIdeo.foundation.def); - 1", true);
            var newIdeo = IdeoGenerator.MakeIdeo(oldIdeo.foundation.def);
            Log.Message(" - GenerateNewSplittedIdeoFrom - var parms = new IdeoGenerationParms(Faction.OfPlayer.def); - 2", true);
            var parms = new IdeoGenerationParms(Faction.OfPlayer.def);
            Log.Message(" - GenerateNewSplittedIdeoFrom - RandomizeMemes(newIdeo, oldIdeo, parms, ideoChanges); - 3", true);
            RandomizeMemes(newIdeo, oldIdeo, parms, ideoChanges);
            Log.Message(" - GenerateNewSplittedIdeoFrom - newIdeo.foundation.RandomizeCulture(parms); - 4", true);
            newIdeo.foundation.RandomizeCulture(parms);
            Log.Message(" - GenerateNewSplittedIdeoFrom - newIdeo.foundation.RandomizePlace(); - 5", true);
            newIdeo.foundation.RandomizePlace();
            Log.Message(" - GenerateNewSplittedIdeoFrom - if (newIdeo.foundation is IdeoFoundation_Deity ideoFoundation_Deity) - 6", true);
            if (newIdeo.foundation is IdeoFoundation_Deity ideoFoundation_Deity)
            {
                Log.Message(" - GenerateNewSplittedIdeoFrom - ideoFoundation_Deity.GenerateDeities(); - 7", true);
                ideoFoundation_Deity.GenerateDeities();
            }
            newIdeo.foundation.GenerateTextSymbols();
            Log.Message(" - GenerateNewSplittedIdeoFrom - newIdeo.foundation.RandomizePrecepts(init: false, parms); - 9", true);
            newIdeo.foundation.RandomizePrecepts(init: false, parms);
            Log.Message(" - GenerateNewSplittedIdeoFrom - newIdeo.foundation.GenerateLeaderTitle(); - 10", true);
            newIdeo.foundation.GenerateLeaderTitle();
            Log.Message(" - GenerateNewSplittedIdeoFrom - newIdeo.foundation.RandomizeIcon(); - 11", true);
            newIdeo.foundation.RandomizeIcon();
            Log.Message(" - GenerateNewSplittedIdeoFrom - newIdeo.foundation.InitPrecepts(parms); - 12", true);
            newIdeo.foundation.InitPrecepts(parms);
            Log.Message(" - GenerateNewSplittedIdeoFrom - newIdeo.RecachePrecepts(); - 13", true);
            newIdeo.RecachePrecepts();
            Log.Message(" - GenerateNewSplittedIdeoFrom - newIdeo.foundation.ideo.RegenerateDescription(force: true); - 14", true);
            newIdeo.foundation.ideo.RegenerateDescription(force: true);
            Log.Message(" - GenerateNewSplittedIdeoFrom - newIdeo.foundation.RandomizeStyles(); - 15", true);
            newIdeo.foundation.RandomizeStyles();

            newIdeo.primaryFactionColor = oldIdeo.primaryFactionColor;
            Log.Message(" - GenerateNewSplittedIdeoFrom - return newIdeo; - 17", true);
            return newIdeo;
        }

        private void RandomizeMemes(Ideo newIdeo, Ideo oldIdeo, IdeoGenerationParms parms, StringBuilder ideoChanges)
        {
            var memesCount = VIESASMod.settings.amountOfMemesChangedDuringSchism;
            var memesToAdd = IdeoUtilityCustom.GenerateRandomMemes(memesCount, parms).Where(x => !oldIdeo.HasMeme(x));
            var structureMemes = memesToAdd.Where(x => x.category == MemeCategory.Structure).ToList();
            var normalMemes = memesToAdd.Where(x => x.category == MemeCategory.Normal).ToList();

            foreach (var meme in memesToAdd)
            {
                Log.Message("About to add " + meme + " - " + meme.category);
            }

            foreach (var meme in oldIdeo.memes)
            {
                Log.Message("Old ideology has " + meme + " - " + meme.category);
            }
            newIdeo.memes.AddRange(oldIdeo.memes);
            bool canRemoveStructure = true;
            List<MemeDef> removedMemes = new List<MemeDef>();

            Predicate<MemeDef> oldMemesToRemoveValidator = delegate (MemeDef x)
            {
                if (x.category == MemeCategory.Structure && (!canRemoveStructure || !memesToAdd.Any(meme => meme.category == MemeCategory.Structure)))
                {
                    return false;
                }
                return true;
            };

            Predicate<MemeDef> newMemeToAddValidator = delegate (MemeDef x)
            {
                if (removedMemes.Contains(x))
                {
                    return false;
                }
                if (x.category == MemeCategory.Structure && newIdeo.memes.Any(meme => meme.category == MemeCategory.Structure))
                {
                    return false;
                }
                return true;
            };
            for (var i = 0; i < memesCount; i++)
            {
                if (oldIdeo.memes.Where(x => x.category == MemeCategory.Normal).Count() >= 3)
                {
                    var memeToRemove = oldIdeo.memes.Where(x => oldMemesToRemoveValidator(x)).RandomElement();
                    removedMemes.Add(memeToRemove);
                    if (memeToRemove.category == MemeCategory.Structure)
                    {
                        newIdeo.memes.Remove(memeToRemove);
                        Log.Message("1 Removing " + memeToRemove);
                        var newStructure = memesToAdd.Where(x => x.category == MemeCategory.Structure && newMemeToAddValidator(x)).RandomElement();
                        Log.Message("1 Adding " + newStructure + " to " + newIdeo);
                        newIdeo.memes.Add(newStructure);
                        ideoChanges.AppendLine("VIESAS.StructureChanged".Translate() + ": " + memeToRemove.LabelCap + " -> " + newStructure.LabelCap);
                        canRemoveStructure = false;
                    }
                    else
                    {
                        newIdeo.memes.Remove(memeToRemove);
                        Log.Message("2 Removing " + memeToRemove);
                        var newMeme = memesToAdd.Where(x => newMemeToAddValidator(x) && x.category == MemeCategory.Normal).RandomElement();
                        Log.Message("2 Adding " + newMeme + " to " + newIdeo);
                        newIdeo.memes.Add(newMeme);
                        ideoChanges.AppendLine("VIESAS.MemeChanged".Translate() + ": " + memeToRemove.LabelCap + " -> " + newMeme.LabelCap);

                    }
                }
                else
                {
                    var newMeme = memesToAdd.Where(x => newMemeToAddValidator(x)).RandomElement();
                    Log.Message("3 Adding " + newMeme + " to " + newIdeo);
                    newIdeo.memes.Add(newMeme);
                    if (newMeme.category == MemeCategory.Structure)
                    {
                        ideoChanges.AppendLine("VIESAS.NewStructure".Translate() + ": " + newMeme.LabelCap);
                    }
                    else
                    {
                        ideoChanges.AppendLine("VIESAS.NewMeme".Translate() + ": " + newMeme.LabelCap);
                    }
                }
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

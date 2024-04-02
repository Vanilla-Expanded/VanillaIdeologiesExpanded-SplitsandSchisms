using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace VIESAS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }

    [HotSwappable]
    public class Window_ConfigureIdeo : Window
    {
        public Ideo newIdeo;

        public List<Pawn> colonistsToConvert;

        public StringBuilder ideoChanges;

        public override Vector2 InitialSize => new Vector2(750, 450);

        public IEnumerable<DiaOption> Choices
        {
            get
            {
                yield return new DiaOption(IdeoPresetCategoryDefOf.Fluid.LabelCap)
                {
                    action = () =>
                    {
                        DoCustomize(fluid: true);
                    },
                };
                yield return new DiaOption(IdeoPresetCategoryDefOf.Custom.LabelCap)
                {
                    action = () =>
                    {
                        DoCustomize(fluid: false);
                    },
                };
                yield return new DiaOption("Accept".Translate())
                {
                    resolveTree = true
                };
            }
        }

        public Window_ConfigureIdeo()
        {
            this.forcePause = true;
            this.doCloseX = false;
            this.absorbInputAroundWindow = true;
        }

        private Vector2 scrollPos;
        private float scrollHeight = 99999999;
        
        public override void DoWindowContents(Rect inRect)
        {
            var viewRect = new Rect(inRect.x, inRect.y, inRect.width - 16, scrollHeight);
            scrollHeight = 0;
            var textRect = new Rect(inRect.x, inRect.y, inRect.width, 240);
            Widgets.BeginScrollView(textRect, ref scrollPos, viewRect);
            StringBuilder joinerDesc = new StringBuilder();
            foreach (var pawn in colonistsToConvert)
            {
                joinerDesc.AppendLine("  - " + pawn.LabelShort);
            }
            Text.Font = GameFont.Medium;
            var titleRect = new Rect(inRect.x, inRect.y, viewRect.width, 24);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, "VIESAS.IdeoSplit".Translate(newIdeo.name));
            scrollHeight += 24;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            var text = "VIESAS.IdeoSplitDesc".Translate(newIdeo.name,
                    ideoChanges.ToString().TrimEndNewlines(), joinerDesc.ToString().TrimEndNewlines());
            var textHeight = Text.CalcHeight(text, viewRect.width);
            var desc = new Rect(inRect.x, titleRect.yMax + 15, viewRect.width, textHeight);
            scrollHeight += 15 + textHeight;
            Widgets.Label(desc, text);
            Widgets.EndScrollView();

            var nodeY = inRect.height - (24 * 3);
            var optionsText = "VIESAS.IdeoSplitDescOptions".Translate();
            var optionsHeight = Text.CalcHeight(optionsText, inRect.width);
            var optionsRect = new Rect(desc.x, inRect.height - ((24 * 3) + optionsHeight + 15), inRect.width, optionsHeight);
            Widgets.Label(optionsRect, optionsText);
            var nodeRect = new Rect(desc.x, nodeY, inRect.width, 24);
            foreach (var choices in Choices)
            {
                OptOnGUI(choices, nodeRect);
                nodeRect.y += 24;
            }
        }

        public override void Close(bool doCloseSound = true)
        {
            base.Close(doCloseSound);
            foreach (var colonist in colonistsToConvert)
            {
                colonist.ideo.SetIdeo(newIdeo);
                colonist.ideo.certaintyInt = Rand.Range(0.75f, 1f);
            }
            var tracker = IdeologyTracker.Instance;
            tracker.originIdeo = Faction.OfPlayer.ideos.PrimaryIdeo;
            tracker.splittedIdeo = newIdeo;
            tracker.nextConversionTickCheck = tracker.GetNextConversionTickCheck();
            Find.IdeoManager.Add(newIdeo);
            Faction.OfPlayer.ideos.Notify_ColonistChangedIdeo();
        }

        public float OptOnGUI(DiaOption option, Rect rect, bool active = true)
        {
            Color textColor = Widgets.NormalOptionColor;
            string text = option.text;
            if (option.disabled)
            {
                textColor = option.DisabledOptionColor;
                if (option.disabledReason != null)
                {
                    text = text + " (" + option.disabledReason + ")";
                }
            }
            rect.height = Text.CalcHeight(text, rect.width);
            if (option.hyperlink.def != null)
            {
                Widgets.HyperlinkWithIcon(rect, option.hyperlink, text);
            }
            else if (Widgets.ButtonTextWorker(rect, text, drawBackground: false, !option.disabled, textColor, active && !option.disabled, false) == Widgets.DraggableResult.Pressed)
            {
                Activate(option);
            }
            return rect.height;
        }

        protected void Activate(DiaOption option)
        {
            if (option.clickSound != null && !option.resolveTree)
            {
                option.clickSound.PlayOneShotOnCamera();
            }
            if (option.resolveTree)
            {
                this.Close();
            }
            if (option.action != null)
            {
                option.action();
            }
        }

        private void DoCustomize(bool fluid = false)
        {
            Page_ConfigureIdeo page_ConfigureIdeo;
            if (fluid)
            {
                page_ConfigureIdeo = new Page_ConfigureFluidIdeo_Colonists
                {
                    window = this
                };
                page_ConfigureIdeo.ideo = newIdeo;
                page_ConfigureIdeo.ideo.Fluid = true;
            }
            else
            {
                page_ConfigureIdeo = new Page_ConfigureIdeo_Colonists
                {
                    window = this
                }; 
                page_ConfigureIdeo.ideo = newIdeo;
                page_ConfigureIdeo.ideo.Fluid = false;

            }
            Find.WindowStack.Add(page_ConfigureIdeo);
        }
    }
}

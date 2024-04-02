using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static RimWorld.IdeoUIUtility;
namespace VIESAS
{
    [HarmonyPatch(typeof(IdeoUIUtility), "DoIdeoListAndDetails")]
    public static class IdeoUIUtility_DoIdeoListAndDetails_Patch
    {
        public static bool Prefix(Rect fillRect, ref Vector2 scrollPosition_list, ref float scrollViewHeight_list, ref Vector2 scrollPosition_details, ref float scrollViewHeight_details, bool editMode = false, bool showCreateIdeoButton = false, List<Pawn> pawns = null, Ideo onlyEditIdeo = null, Action createCustomBtnActOverride = null, bool forArchonexusRestart = false, Func<Pawn, Ideo> pawnIdeoGetter = null, Action<Ideo> ideoLoadedFromFile = null, bool showLoadExistingIdeoBtn = false, bool allowLoad = true, Action createFluidBtnAct = null)
        {
            if (Find.WindowStack.IsOpen<Page_ConfigureIdeo_Colonists>()
                || Find.WindowStack.IsOpen<Page_ConfigureFluidIdeo_Colonists>())
            {
                var ideo = Find.WindowStack.WindowOfType<Window_ConfigureIdeo>().newIdeo;
                DoIdeoListAndDetails(ideo, fillRect, ref scrollPosition_list, ref scrollViewHeight_list, ref scrollPosition_details, ref scrollViewHeight_details, editMode, showCreateIdeoButton, pawns, onlyEditIdeo, createCustomBtnActOverride, forArchonexusRestart, pawnIdeoGetter, ideoLoadedFromFile, showLoadExistingIdeoBtn, allowLoad, createFluidBtnAct);
                return false;
            }
            return true;
        }

        public static void DoIdeoListAndDetails(Ideo ideo, Rect fillRect, ref Vector2 scrollPosition_list, ref float scrollViewHeight_list,
            ref Vector2 scrollPosition_details, ref float scrollViewHeight_details, bool editMode = false, 
            bool showCreateIdeoButton = false, List<Pawn> pawns = null, Ideo onlyEditIdeo = null, 
            Action createCustomBtnActOverride = null, bool forArchonexusRestart = false, 
            Func<Pawn, Ideo> pawnIdeoGetter = null, Action<Ideo> ideoLoadedFromFile = null,
            bool showLoadExistingIdeoBtn = false, bool allowLoad = true, Action createFluidBtnAct = null)
        {
            Text.Font = GameFont.Small;
            showAll = false;
            selected = ideo;
            Rect rect = new Rect(fillRect.x, fillRect.y, fillRect.width, fillRect.height);
            if (ideo != null)
            {
                Rect inRect = rect.ContractedBy(17f);
                inRect.yMax += 8f;
                bool editMode2 = editMode && (onlyEditIdeo == null || onlyEditIdeo == ideo);
                DoIdeoDetails(inRect, ideo, ref scrollPosition_details, ref scrollViewHeight_details, editMode2, ideoLoadedFromFile, allowLoad: false, allowSave: false, reform: false, forArchonexusRestart);
            }
        }
    }
}

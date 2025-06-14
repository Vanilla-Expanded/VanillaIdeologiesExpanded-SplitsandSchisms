using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace VIESAS
{
    public class Page_ConfigureFluidIdeo_Colonists : Page_ConfigureFluidIdeo
    {
        public Window_ConfigureIdeo window;

        public override Vector2 InitialSize => new Vector2(1020f * 0.75f, 764f);

        public Page_ConfigureFluidIdeo_Colonists() : base()
        {
            grayOutIfOtherDialogOpen = false;
        }

        public override bool CanDoNext()
        {
            return true;
        }

        public override void DoNext()
        {
            window.Close();
            this.Close();
            Find.FactionManager.OfPlayer.ideos.RecalculateIdeosBasedOnPlayerPawns();
        }
    }
}

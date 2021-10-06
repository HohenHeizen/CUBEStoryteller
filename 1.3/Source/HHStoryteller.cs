
using System.Linq;
using HugsLib;
using RimWorld;

using Verse;

using HarmonyLib;



namespace HHStoryteller
{
    [StaticConstructorOnStartup]
    public class HHStoryteller : ModBase
    {

        public static HugsLib.Utils.ModLogger instLogger;

        public static StorytellerDef CUBE = DefDatabase<StorytellerDef>.GetNamed("CUBE");
        public override string ModIdentifier
        {
            get { return "HHStoryteller"; }
        }
    }
    [HarmonyPatch(typeof(Map), "get_PlayerWealthForStoryteller")]
    public static class TechIsWealth
    {
        static SimpleCurve wealthCurve = new SimpleCurve(new CurvePoint[] { new CurvePoint(0, 0), new CurvePoint(3800, 0), new CurvePoint(150000, 400000f), new CurvePoint(420000, 700000f), new CurvePoint(666666, 1000000f) });
        static SimpleCurve componentCurve = new SimpleCurve(new CurvePoint[] { new CurvePoint(0, 0), new CurvePoint(10, 5000), new CurvePoint(100, 25000), new CurvePoint(1000, 150000) });

        public static void Postfix(Map __instance, ref float __result)
        {
            if (Find.Storyteller.def != HHStoryteller.CUBE)
                return;
            float num = ResearchToWealth();
            int numComponents = 0;
            foreach (Building building in __instance.listerBuildings.allBuildingsColonist)
            {
                if (building.def.costList?.Where(tdc => tdc.thingDef == ThingDefOf.ComponentIndustrial).Count() > 0)
                    numComponents++;
                if (building.def.costList?.Where(tdc => tdc.thingDef == ThingDefOf.ComponentSpacer).Count() > 0)
                    numComponents += 10;
            }
            num += componentCurve.Evaluate(numComponents);
            //Log.Message("CUBE calculates threat points should be " + wealthCurve.Evaluate(num) + " based on " + ResearchToWealth() + " research and " + numComponents + " component-based buildings");
            __result = wealthCurve.Evaluate(num);
        }

        static float ResearchToWealth()
        {
            float num = 0;
            foreach (ResearchProjectDef proj in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                if (proj.IsFinished)
                    num += proj.baseCost;
            }
            return num;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using HugsLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.Sound;
using HarmonyLib;
using System.Text;
using UnityEngine;
using HugsLib.Utils;
using Verse.AI.Group;
using HugsLib.Settings;
using RimWorld.QuestGen;
using System.Net;
using System.IO;
using System.Collections;
using System.Reflection.Emit;


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


    [HarmonyPatch(typeof(PawnGroupMakerUtility), "TryGetRandomFactionForCombatPawnGroup")]
    public static class NoDeadFactions
    {
        [HarmonyPrefix]
        public static bool DisableMethod()
        {
            return false;
        }

        [HarmonyPostfix]
        public static void Replace(ref bool __result, float points, out Faction faction,
            Predicate<Faction> validator = null, bool allowNonHostileToPlayer = false, bool allowHidden = false,
            bool allowDefeated = false, bool allowNonHumanlike = true)
        {
            List<Faction> source = WorldSwitchUtility.FactionsOnCurrentWorld(Find.FactionManager.AllFactions).Where(
                delegate (Faction f) {
                    int arg_E3_0;
                    if ((allowHidden || !f.def.hidden) && (allowDefeated || !f.defeated) &&
                        (allowNonHumanlike || f.def.humanlikeFaction) &&
                        (allowNonHostileToPlayer || f.HostileTo(Faction.OfPlayer)) &&
                        f.def.pawnGroupMakers != null)
                    {
                        if (f.def.pawnGroupMakers.Any((PawnGroupMaker x) =>
                            x.kindDef == PawnGroupKindDefOf.Combat) && (validator == null || validator(f)))
                        {
                            arg_E3_0 = ((points >= f.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat))
                                ? 1
                                : 0);
                            return arg_E3_0 != 0;
                        }
                    }

                    arg_E3_0 = 0;
                    return arg_E3_0 != 0;
                }).ToList<Faction>();
            __result = source.TryRandomElementByWeight((Faction f) => f.def.RaidCommonalityFromPoints(points),
                out faction);
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
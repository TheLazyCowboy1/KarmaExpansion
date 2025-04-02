using System;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using RWCustom;
using BepInEx;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, SkipVerification = true)]

namespace KarmaExpansion;

[BepInDependency("rwmodding.coreorg.rk", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("lb-fgf-m4r-ik.chatoyant-waterfalls-but-real", BepInDependency.DependencyFlags.SoftDependency)] //chasing wind

[BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
public partial class KarmaExpansion : BaseUnityPlugin
{
    public const string MOD_ID = "LazyCowboy.KarmaExpansion";
    public const string MOD_NAME = "Karma Expansion";
    public const string MOD_VERSION = "1.1.2";

    //public static RegionRandomizerOptions Options;

    public static KarmaExpansion Instance;

    public KarmaExpansion()
    {
        try
        {
            Instance = this;
            //Options = new RegionRandomizerOptions(this, Logger);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }

    private void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        //RegionLoader.Enable();
    }

    private void OnDisable()
    {
        //RegionLoader.Disable();
        if (IsInit)
        {
            On.RainWorldGame.GhostShutDown -= RainWorldGame_GhostShutDown;
            On.SaveState.IncreaseKarmaCapOneStep -= SaveState_IncreaseKarmaCapOneStep;
            On.SaveState.GhostEncounter -= SaveState_GhostEncounter;
            On.SSOracleBehavior.Update -= SSOracleBehavior_Update;
            On.HUD.Map.GateMarker.ctor -= GateMarker_ctor;
            On.GateKarmaGlyph.DrawSprites -= GateKarmaGlyph_DrawSprites;
            On.HUD.KarmaMeter.KarmaSymbolSprite -= KarmaMeter_KarmaSymbolSprite;
        }
    }

    //private static RainWorldGame game;


    private bool IsInit;
    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        try
        {
            if (IsInit) return;

            //Your hooks go here

            //On.RainWorld.LoadResources += RainWorld_LoadResources;

            On.RainWorldGame.GhostShutDown += RainWorldGame_GhostShutDown;
            On.SaveState.IncreaseKarmaCapOneStep += SaveState_IncreaseKarmaCapOneStep;
            On.SaveState.GhostEncounter += SaveState_GhostEncounter;

            On.SSOracleBehavior.Update += SSOracleBehavior_Update;

            On.HUD.Map.GateMarker.ctor += GateMarker_ctor;
            On.GateKarmaGlyph.DrawSprites += GateKarmaGlyph_DrawSprites;

            On.HUD.KarmaMeter.KarmaSymbolSprite += KarmaMeter_KarmaSymbolSprite;

            //register all custom karma gate requirements
            for (int i = 11; i <= 34; i++)
                new RegionGate.GateRequirement(i.ToString(), true);

            //load custom atlases
            try
            {
                //Futile.atlasManager.LoadAtlas(AssetManager.ResolveDirectory("assets\\KarmaExpansion") + "\\ExtraKarmaSymbols");
                Futile.atlasManager.LoadAtlas(AssetManager.ResolveDirectory("assets\\KarmaExpansion") + "\\Karma 11-22");
                Futile.atlasManager.LoadAtlas(AssetManager.ResolveDirectory("assets\\KarmaExpansion") + "\\Karma 23-34");
            }
            catch (Exception ex)
            {
                Instance.Logger.LogError(ex);
            }

            //MachineConnector.SetRegisteredOI("LazyCowboy.KarmaExpansion", Options);
            IsInit = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }

    #region HOOKS
    public static void RainWorldGame_GhostShutDown(On.RainWorldGame.orig_GhostShutDown orig, RainWorldGame self, GhostWorldPresence.GhostID ghostID)
    {
        if (self.GetStorySession.saveState.deathPersistentSaveData.karmaCap >= 9 && self.GetStorySession.saveState.deathPersistentSaveData.karmaCap < 33)
        {
            if (self.manager.upcomingProcess != null)
            {
                return;
            }
            self.sawAGhost = ghostID;
            self.GetStorySession.AppendTimeOnCycleEnd(true);
            if (ModManager.Expedition && self.manager.rainWorld.ExpeditionMode)
            {
                global::Expedition.Expedition.coreFile.Save(false);
            }
            if (ModManager.MSC && self.GetStorySession.saveStateNumber == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer && self.GetStorySession.saveState.deathPersistentSaveData.altEnding)
            {
                orig(self, ghostID);
                return;
            }

            //main point of interest: originally only applied if karmaCap < 9
            self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.GhostScreen);
            return;

        }
        else
            orig(self, ghostID);
    }

    public static void SaveState_IncreaseKarmaCapOneStep(On.SaveState.orig_IncreaseKarmaCapOneStep orig, SaveState self)
    {
        if (self.deathPersistentSaveData.karmaCap >= 9 && self.deathPersistentSaveData.karmaCap < 33) //raise karma if >= 10 and less than 22
            self.deathPersistentSaveData.karmaCap++;
        orig(self);
    }

    public static void SaveState_GhostEncounter(On.SaveState.orig_GhostEncounter orig, SaveState self, GhostWorldPresence.GhostID ghost, RainWorld rainWorld)
    {
        //copied from original
        /*
        self.deathPersistentSaveData.ghostsTalkedTo[ghost] = 2;
        int num = 0;
        foreach (KeyValuePair<GhostWorldPresence.GhostID, int> keyValuePair in self.deathPersistentSaveData.ghostsTalkedTo)
        {
            if (keyValuePair.Value > 1)
            {
                num++;
            }
        }
        if (self.deathPersistentSaveData.pebblesHasIncreasedRedsKarmaCap)
        {
            num++;
        }
        int num2 = SlugcatStats.SlugcatStartingKarma(self.saveStateNumber);
        while (num2 < 21 && num > 0) //modified 9 to 21
        {
            num2++;
            if (num2 == 5)
            {
                num2++;
            }
            num--;
        }
        if (num2 >= self.deathPersistentSaveData.karmaCap)
        {
            self.deathPersistentSaveData.karmaCap = num2;
        }
        */
        if (self.deathPersistentSaveData.karmaCap < 33)
            self.deathPersistentSaveData.karmaCap++;

        orig(self, ghost, rainWorld);
    }

    //Raises karma to 10, or raises it by 1
    public static void SSOracleBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
    {
        int k = (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma;
        int c = (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap;

        orig(self, eu);

        if (c < 9) //if below 10, Pebbles raises to 10
            return;

        //if karma or karmaCap changed
        if ((self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma != k || (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap != c)
        {
            (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap = c + ((c < 33) ? 1 : 0); //increase karma cap by 1, unless already at max
            //(self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma = 21;

            //copied from original code: code to update karma meter graphics
            (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma = (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap;
            for (int num2 = 0; num2 < self.oracle.room.game.cameras.Length; num2++)
            {
                if (self.oracle.room.game.cameras[num2].hud.karmaMeter != null)
                {
                    self.oracle.room.game.cameras[num2].hud.karmaMeter.UpdateGraphic();
                }
            }
        }
    }

    public static string KarmaMeter_KarmaSymbolSprite(On.HUD.KarmaMeter.orig_KarmaSymbolSprite orig, bool small, IntVector2 k)
    {
        if (k.y > 9)
        {
            if (k.x < 5)
            {
                return orig(small, k);
            }
            else if (k.x <= 9)
                return (small ? "smallKarma" : "karma") + k.x.ToString() + "-9";
            else
                return (small ? "smallKarma" : "karma") + Mathf.Clamp(k.x, 0, 33);
                //return "smallKarma" + Mathf.Clamp(k.x, 0, 21);
        }
        else
            return orig(small, k);
    }

    private static void GateMarker_ctor(On.HUD.Map.GateMarker.orig_ctor orig, HUD.Map.GateMarker self, HUD.Map map, int room, RegionGate.GateRequirement karma, bool showAsOpen)
    {
        try
        {
            if (Int32.TryParse(karma.value, out int k) && k > 5) // above 10 karma support
            {
                orig(self, map, room, new RegionGate.GateRequirement("0"), showAsOpen);
                if (k > 10)
                    self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarma" + (k-1).ToString()); // Vanilla, zero-indexed
                else //karma (5,10]
                {
                    k--; //convert from 1-indexed to 0-indexed
                    int? cap = map.hud.rainWorld.progression?.currentSaveState?.deathPersistentSaveData?.karmaCap;
                    if (!cap.HasValue || cap.Value < k) cap = Mathf.Max(6, k);
                    cap = Math.Min(cap.Value, 9);
                    self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarma" + k.ToString() + "-" + cap.Value.ToString());
                }
            }
            else
            {
                orig(self, map, room, karma, showAsOpen);
            }
        }
        catch (Exception ex)
        {
            orig(self, map, room, karma, showAsOpen);
        }
    }

    private static void GateKarmaGlyph_DrawSprites(On.GateKarmaGlyph.orig_DrawSprites orig, GateKarmaGlyph self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
    {
        try
        {
            if (self.symbolDirty) // redraw
            {
                //list all symbol names
                //Instance.Logger.LogDebug("All atlas elements: " + String.Join(", ", Futile.atlasManager._allElementsByName.Keys));
                if (Int32.TryParse(self.requirement.value, out int parseTest))
                {
                    if (parseTest > 10) // if custom symbol
                    {
                        self.room.game.rainWorld.HandleLog("KarmaExpansion: Drawing gate symbol: " + self.requirement.value, "stuff", LogType.Log);
                        sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbol" + parseTest.ToString()); // Alt art for vanilla gates
                        self.symbolDirty = false;
                    }
                    else if (parseTest > 5)
                    {
                        int cap = (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap + 1;
                        if (cap < parseTest) cap = Mathf.Max(7, parseTest);
                        cap = Math.Min(cap, 10);
                        sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbol" + parseTest.ToString() + "-" + cap.ToString()); // Custom, 1-indexed
                        self.symbolDirty = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Instance.Logger.LogError(ex);
        }
        orig(self, sLeaser, rCam, timeStacker, camPos);
    }
    #endregion
}
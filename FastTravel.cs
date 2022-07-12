using GlobalEnums;
using Modding;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UObject = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using USceneUtility = UnityEngine.SceneManagement.SceneUtility;

namespace FastTravel
{
    internal class FastTravel : Mod, IMenuMod, IGlobalSettings<Settings>
    {
        public FastTravel() : base("Fast Travel") { }
        internal static Settings settings = new();
        void IGlobalSettings<Settings>.OnLoadGlobal(Settings s) => settings = s;
        Settings IGlobalSettings<Settings>.OnSaveGlobal() => settings;
        bool IMenuMod.ToggleButtonInsideMenu => false;
        List<IMenuMod.MenuEntry> IMenuMod.GetMenuData(Modding.IMenuMod.MenuEntry? toggleButtonEntry)
        {
            return new()
            {
                new("Enable precise teleport", new string[]{ "False", "True" }, "",
                    id => settings.PreciseTeleport = id == 1, () => settings.PreciseTeleport ? 1 : 0)
            };
        }
        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public static TeleportCursor teleportCursor;
        private static Vector2 tpPosition;
        private static readonly FastReflectionDelegate SetState =
            typeof(HeroController)
            .GetMethod("SetState", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetFastDelegate();

        public override void Initialize()
        {
            On.GameManager.EnterHero += OnHeroEnter;
            On.GameMap.Start += OnMapStart;
            On.PlayMakerFSM.Start += OnPFSMStart;
        }

        private void OnHeroEnter(On.GameManager.orig_EnterHero orig, GameManager self, bool additiveGateSearch)
        {
            orig(self, additiveGateSearch);

            if (self.entryGateName == "none")
            {
                if (settings.PreciseTeleport)
                {
                    HeroController.instance.SetHazardRespawn(GetTeleportPos(), false);
                }
                else
                {
                    var teleportPos = GetTeleportPos();
                    HeroController.instance.SetHazardRespawn(UObject.FindObjectsOfType<HazardRespawnMarker>()
                        .OrderBy(x => (x.transform.position - teleportPos).sqrMagnitude)
                        .First()
                        .transform.position, false);
                }
                GameManager.instance.HazardRespawn();

            }
        }

        private void OnMapStart(On.GameMap.orig_Start orig, GameMap self)
        {
            orig(self);

            for (int i = 0; i < USceneManager.sceneCountInBuildSettings; i++)
            {
                string sceneName = Path.GetFileNameWithoutExtension(USceneUtility.GetScenePathByBuildIndex(i));
                GameObject sceneMap = self.gameObject.Find(sceneName);

                if (sceneMap == null) continue;

                var sr = sceneMap.GetComponent<SpriteRenderer>();

                if (sr == null) continue;

                sceneMap.AddComponent<TeleportButton>();

                var mapCol = sceneMap.AddComponent<BoxCollider2D>();
                mapCol.isTrigger = true;
                mapCol.size = sr.sprite.bounds.size;
            }
        }

        private void OnPFSMStart(On.PlayMakerFSM.orig_Start orig, PlayMakerFSM self)
        {
            orig(self);

            GameObject tpCursor = null;
            if (self.name == "World Map" && self.FsmName == "UI Control")
            {
                tpCursor = UObject.Instantiate(self.transform.Find("Map Markers/Placement Cursor").gameObject, self.transform.root);
                tpCursor.SetActive(true);
                teleportCursor = tpCursor.AddComponent<TeleportCursor>();
            }
        }

        private static Vector3 GetTeleportPos()
        {
            var tilp = GameManager.instance.tilemap;
            var gm = GameManager.instance.gameMap.GetComponent<GameMap>();
            float originOffsetX = Modding.ReflectionHelper.GetField<GameMap, float>(gm, "originOffsetX");
            float originOffsetY = Modding.ReflectionHelper.GetField<GameMap, float>(gm, "originOffsetY");
            return new Vector3(tpPosition.x * tilp.width - originOffsetX, tpPosition.y * tilp.height - originOffsetY, HeroController.instance.transform.position.z);
        }

        internal static void StartTransition(string sceneName, GameObject icon)
        {
            GameManager.instance.StartCoroutine(DoTransition(sceneName, icon));
        }

        private static IEnumerator DoTransition(string sceneName, GameObject icon)
        {
            var spriteSize = (Vector2)icon.GetComponent<SpriteRenderer>().sprite.bounds.size;
            var spritePos = (Vector2)icon.transform.position - spriteSize / 2;
            var hudCam = GameCameras.instance.hudCamera;
            var mousePos = Input.mousePosition;
            mousePos.z = hudCam.ScreenToWorldPoint(Vector3.zero).z;
            var offset = (Vector2)hudCam.ScreenToWorldPoint(mousePos) - spritePos;
            tpPosition.x = offset.x / (spriteSize.x * GameManager.instance.gameMap.transform.localScale.x) * GameManager.instance.gameMap.transform.localScale.x;
            tpPosition.y = offset.y / (spriteSize.y * GameManager.instance.gameMap.transform.localScale.y) * GameManager.instance.gameMap.transform.localScale.y;
            // Close the map UI
            var invFSM = GameManager.instance.inventoryFSM;
            invFSM.SendEvent("INVENTORY CANCEL");
            yield return new WaitUntil(() => invFSM.ActiveStateName == "Closed");

            // Get off bench before doing scene transition or else
            // map shortcut becomes disabled until benching again
            var bench = UObject.FindObjectOfType<RestBench>();
            var benchCtrl = bench?.GetComponents<PlayMakerFSM>().FirstOrDefault(fsm => fsm.ActiveStateName == "Resting");
            benchCtrl?.SendEvent("GET UP");
            if (benchCtrl != null)
            {
                yield return new WaitUntil(() => benchCtrl?.ActiveStateName == "Idle");
            }

            GameManager.SceneTransitionBegan += Began;

            GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
            {
                SceneName = sceneName,
                EntryGateName = "none",
                EntryDelay = 0,
                Visualization = GameManager.SceneLoadVisualizations.Default,
                PreventCameraFadeOut = false,
                WaitForSceneTransitionCameraFade = true,
                AlwaysUnloadUnusedAssets = false,
            });
        }

        private static void Began(SceneLoad load)
        {
            load.Finish += () => GameManager.instance.StartCoroutine(RemoveBlankers());
        }

        private static IEnumerator RemoveBlankers()
        {
            yield return new WaitUntil(() => GameManager.instance.gameState == GameState.PLAYING);

            GameManager.instance.FadeSceneIn();

            PlayMakerFSM.BroadcastEvent("BOX DOWN");
            PlayMakerFSM.BroadcastEvent("BOX DOWN DREAM");

            var hc = HeroController.instance;

            yield return new WaitUntil(() => hc.transitionState == HeroTransitionState.EXITING_SCENE);

            // Force being able to input to avoid having to wait for the decade long walk-in anim
            hc.AcceptInput();

            SetState(hc, ActorStates.idle);

            GameManager.SceneTransitionBegan -= Began;
        }
    }
}
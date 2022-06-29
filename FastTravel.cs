using GlobalEnums;
using Modding;
using MonoMod.Utils;
using System.Collections;
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
    internal class FastTravel : Mod
    {
        public FastTravel() : base("Fast Travel") { }

        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

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
                HeroController.instance.SetHazardRespawn(UObject.FindObjectOfType<HazardRespawnMarker>().transform.position, false);
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
                tpCursor.AddComponent<TeleportCursor>();
            }
        }

        internal static void StartTransition(string sceneName)
        {
            GameManager.instance.StartCoroutine(DoTransition(sceneName));
        }

        private static IEnumerator DoTransition(string sceneName)
        {
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
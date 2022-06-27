using GlobalEnums;
using Modding;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using Vasi;
using UObject = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using USceneUtility = UnityEngine.SceneManagement.SceneUtility;

namespace FastTravel
{
    internal class FastTravel : Mod
    {
        internal static FastTravel Instance { get; private set; }

        public FastTravel() : base("Fast Travel") { }

        public override string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public override void Initialize()
        {
            Instance = this;

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
    }
}
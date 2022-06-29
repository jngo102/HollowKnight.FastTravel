using System.Collections;
using System.Reflection;
using GlobalEnums;
using MonoMod.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace FastTravel
{
    internal class TeleportButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly FastReflectionDelegate SetState = 
            typeof(HeroController)
            .GetMethod("SetState", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetFastDelegate();
        
        private TextMeshPro _tmpHighlight;

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => GameCameras.instance.transform.Find("Teleport Cursor/Text") != null);
            _tmpHighlight = GameCameras.instance.transform.Find("Teleport Cursor/Text").GetComponent<TextMeshPro>();
        }

        private static void Began(SceneLoad load) => 
            load.Finish += () => GameManager.instance.StartCoroutine(RemoveBlankers());

        private static IEnumerator RemoveBlankers()
        {
            while (GameManager.instance.gameState != GameState.PLAYING)
                yield return null;

            Modding.Logger.Log("feelsdayman");
            
            GameManager.instance.FadeSceneIn();
            
            PlayMakerFSM.BroadcastEvent("BOX DOWN");
            PlayMakerFSM.BroadcastEvent("BOX DOWN DREAM");

            var hc = HeroController.instance;

            while (hc.transitionState != HeroTransitionState.EXITING_SCENE)
                yield return null;

            // Force being able to input to avoid having to wait for the decade long walk-in anim
            hc.AcceptInput();
            
            SetState(hc, ActorStates.idle);
            
            GameManager.SceneTransitionBegan -= Began;
        }

        public void OnPointerClick(PointerEventData _) 
        {
            if (USceneManager.GetActiveScene().name == name) 
                return;

            PlayMakerFSM.BroadcastEvent("INVENTORY CANCEL");

            GameManager.SceneTransitionBegan += Began;
            
            GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
            {
                SceneName = _tmpHighlight.text,
                EntryGateName = "none",
                EntryDelay = 0,
                Visualization = GameManager.SceneLoadVisualizations.Default,
                PreventCameraFadeOut = false,
                WaitForSceneTransitionCameraFade = true,
                AlwaysUnloadUnusedAssets = false,
            });
        }

        public void OnPointerEnter(PointerEventData data)
        {
            if (_tmpHighlight != null) _tmpHighlight.text = name;
        }

        public void OnPointerExit(PointerEventData data)
        {
            if (_tmpHighlight != null) _tmpHighlight.text = "";
        }
    }
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace FastTravel
{
    class TeleportButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private TextMeshPro _tmpHighlight;

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => GameCameras.instance.transform.Find("Teleport Cursor/Text") != null);
            _tmpHighlight = GameCameras.instance.transform.Find("Teleport Cursor/Text").GetComponent<TextMeshPro>();
        }

        public void OnPointerClick(PointerEventData data)
        {
            if (USceneManager.GetActiveScene().name == name) return;

            PlayMakerFSM.BroadcastEvent("INVENTORY CANCEL");
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

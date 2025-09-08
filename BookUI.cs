using Photon.Pun;
using UnityEngine;
using System.Collections;
using System.Text;
using TMPro;

namespace MyCoolMod
{
    public static class GuidebookMods
    {
        private static Guidebook GetHeldGuidebook(Character target)
        {
            if (target == null || target.data.currentItem == null) return null;
            return target.data.currentItem.GetComponent<Guidebook>();
        }

        public static IEnumerator ScramblePlayerBook(Character target)
        {
            Guidebook book = GetHeldGuidebook(target);
            if (book == null || !book.isOpen)
            {
                Plugin.Log.LogInfo($"{target.characterName} is not holding an open guidebook.");
                yield break;
            }

            Plugin.Log.LogInfo($"Scrambling {target.characterName}'s book...");
            for (int i = 0; i < 15; i++)
            {
                if (i % 2 == 0)
                {
                    book.photonView.RPC("FlipPageRight_RPC", RpcTarget.All, book.currentPageSet + 1);
                }
                else
                {
                    book.photonView.RPC("FlipPageLeft_RPC", RpcTarget.All, book.currentPageSet - 1);
                }
                yield return new WaitForSeconds(0.2f);
            }
            Plugin.Log.LogInfo("Book scramble finished.");
        }

        public static void TurnBookIntoSpyCamera(Character bookOwner, Character targetToSpyOn)
        {
            Guidebook book = GetHeldGuidebook(bookOwner);
            if (book == null || targetToSpyOn == null) return;

            foreach (var spread in book.pageSpreads)
            {
                spread.gameObject.SetActive(false);
            }

            Camera spyCam = book.renderCamera;
            spyCam.transform.SetParent(targetToSpyOn.refs.head.transform);
            spyCam.transform.localPosition = new Vector3(0, 0.5f, -1.5f);
            spyCam.transform.LookAt(targetToSpyOn.refs.head.transform);
            spyCam.fieldOfView = 75;

            Plugin.Log.LogInfo($"Turned {bookOwner.characterName}'s book into a spy cam watching {targetToSpyOn.characterName}.");
        }
        public static void SpawnGuidebookInFront()
        {
            SpawnItemInFront("Guidebook");
        }
        public static void SpawnItemInFront(string prefabName, float distance = 1.5f)
        {
            if (Character.localCharacter == null || Camera.main == null)
            {
                Plugin.Log.LogWarning("Cannot spawn item: Local player or main camera not found.");
                return;
            }
            Transform cameraTransform = Camera.main.transform;
            Vector3 spawnPosition = cameraTransform.position + (cameraTransform.forward * distance);

            try
            {
                GameObject spawnedItem = PhotonNetwork.Instantiate(prefabName, spawnPosition, Quaternion.identity);
                if (spawnedItem != null)
                {
                    Plugin.Log.LogInfo($"Successfully spawned '{prefabName}' in front of the player.");
                }
                else
                {
                    Plugin.Log.LogError($"PhotonNetwork.Instantiate failed to spawn '{prefabName}'. Is the prefab name correct and is it in a Resources folder?");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"An exception occurred while trying to spawn '{prefabName}': {ex.Message}");
            }
        }
        public static void SetBookMessage(Character bookOwner, string header, string body)
        {
            Guidebook book = GetHeldGuidebook(bookOwner);
            if (book == null) return;
            foreach (var spread in book.pageSpreads)
            {
                Object.Destroy(spread.gameObject);
            }
            book.pageSpreads.Clear();
            GameObject newPageObject = new GameObject("CustomSpread");
            newPageObject.transform.SetParent(book.transform);
            newPageObject.transform.localPosition = Vector3.zero;
            newPageObject.transform.localRotation = Quaternion.identity;
            GuidebookSpread newSpread = newPageObject.AddComponent<GuidebookSpread>();
            Canvas canvas = newPageObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1000, 1000);
            canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            GameObject headerObj = new GameObject("HeaderText");
            headerObj.transform.SetParent(canvas.transform);
            TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
            headerText.text = header;
            headerText.fontSize = 80;
            headerText.color = Color.black;
            headerText.alignment = TextAlignmentOptions.Center;
            RectTransform headerRect = headerText.GetComponent<RectTransform>();
            headerRect.localPosition = new Vector3(0, 200, 0);
            headerRect.sizeDelta = new Vector2(800, 100);
            GameObject bodyObj = new GameObject("BodyText");
            bodyObj.transform.SetParent(canvas.transform);
            TextMeshProUGUI bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            bodyText.text = body;
            bodyText.fontSize = 50;
            bodyText.color = Color.black;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            RectTransform bodyRect = bodyText.GetComponent<RectTransform>();
            bodyRect.localPosition = new Vector3(0, 0, 0);
            bodyRect.sizeDelta = new Vector2(800, 700);
            book.pageSpreads.Add(newSpread);
            book.currentPageSet = 0;
            book.photonView.RPC("ToggleGuidebook_RPC", RpcTarget.All, true);
        }
        public static IEnumerator ScrambleMyBook()
        {
            yield return ScramblePlayerBook(Character.localCharacter);
        }
        public static void TurnMyBookIntoSpyCamera(Character targetToSpyOn)
        {
            TurnBookIntoSpyCamera(Character.localCharacter, targetToSpyOn);
        }
        public static void SetMyBookMessage(string header, string body)
        {
            SetBookMessage(Character.localCharacter, header, body);
        }
        public static void DisplayPlayerListInMyBook()
        {
            var allPlayers = PlayerManager.GetAllCharacters();
            if (allPlayers == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("--- SESSION PLAYER LIST ---");
            foreach (var player in allPlayers)
            {
                if (player == null) continue;
                string status = "";
                if (player.data.dead) status = "<color=red>DEAD</color>";
                else if (player.data.fullyPassedOut) status = "<color=yellow>DOWNED</color>";
                else status = "<color=green>ALIVE</color>";
                sb.AppendLine($"\n- {player.characterName} [{status}]");
            }
            SetMyBookMessage("SYSTEM INFO", sb.ToString());
        }
    }
}
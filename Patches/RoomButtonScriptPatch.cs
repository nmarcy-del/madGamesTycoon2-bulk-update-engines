using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace BulkEngineUpdateMod.Patches
{
    [HarmonyPatch(typeof(roomButtonScript))]
    [HarmonyPatch("BUTTON_Dev_Engines")]
    public class ButtonDevEnginesPatch
    {
        static void Postfix(roomButtonScript __instance)
        {
            Debug.Log("Patch: BUTTON_Dev_Engines executed !");

            GUI_Main guiMain = AccessTools.FieldRefAccess<roomButtonScript, GUI_Main>(__instance, "guiMain_");
            roomScript rS = AccessTools.FieldRefAccess<roomButtonScript, roomScript>(__instance, "rS_");
            GameObject menu = guiMain.uiObjects[36];

            if (menu != null)
            {
                Debug.Log("Menu found: Menu_Dev_EngineMain");
                AdjustMenuHeight(menu);
                AddCustomButton(menu, guiMain, rS);
            }
            else
            {
                Debug.LogError("Menu not found.");
            }
        }

        private static void AdjustMenuHeight(GameObject menue)
        {
            Transform menuTransform = menue.transform.Find("Menue");
            RectTransform menuRect = menuTransform.GetComponent<RectTransform>();
            if (menuRect != null)
            {
                menuRect.sizeDelta = new Vector2(menuRect.sizeDelta.x, menuRect.sizeDelta.y + 35);
                Debug.Log("Increased menu height.");
            }
        }

        private static void AddCustomButton(GameObject menu, GUI_Main guiMain, roomScript rS)
        {
            Transform menueTransform = menu.transform.Find("Menue");
            if (menueTransform == null)
            {
                Debug.LogError("The 'Menue' child cannot be found in the menu.");
                return;
            }

            Transform originalButton = menueTransform.Find("Button_UpdateEngine");
            if (originalButton == null)
            {
                Debug.LogError("Button_UpdateEngine not found in 'Menue'.");
                return;
            }

            GameObject clonedButton = GameObject.Instantiate(originalButton.gameObject, menueTransform);
            clonedButton.name = "Button_CustomAction";

            RectTransform rectTransform = clonedButton.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y - 63);
                Debug.Log("Clone button position adjusted.");
            }

            Text buttonText = clonedButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = "Update all engines";
                Debug.Log("Modified clone button text.");
            }

            Button buttonComponent = clonedButton.GetComponent<Button>();
            if (buttonComponent != null)
            {
                ReassignButtonAction(buttonComponent, guiMain, rS);
            }
        }

        private static void ReassignButtonAction(Button buttonComponent, GUI_Main guiMain, roomScript rS)
        {
            buttonComponent.onClick = new Button.ButtonClickedEvent();

            buttonComponent.onClick.AddListener(() =>
            {
                Debug.Log("New action executed from the cloned button!");
                ProcessEnginesForUpdate(guiMain, rS);
            });

            Debug.Log("Actions successfully reassigned.");
        }

        private static void ProcessEnginesForUpdate(GUI_Main guiMain, roomScript rS)
        {
            Debug.Log("Processing engines for update.");

            mainScript mainScriptInstance = AccessTools.FieldRefAccess<GUI_Main, mainScript>(guiMain, "mS_");
            engineFeatures engineFeaturesInstance = AccessTools.FieldRefAccess<mainScript, engineFeatures>(mainScriptInstance, "eF_");

            engineScript[] allEngines = Object.FindObjectsOfType<engineScript>();
            var playerEngines = allEngines.Where(engine => engine.ownerID == mainScriptInstance.myID).ToList();

            if (playerEngines.Count == 0)
            {
                Debug.LogWarning("No engines found for the player!");
                guiMain.MessageBox("No engines available for update.", false);
                return;
            }

            foreach (var engine in playerEngines)
            {
                if (engine.features.SequenceEqual(engineFeaturesInstance.engineFeatures_UNLOCK))
                {
                    Debug.Log($"Engine '{engine.myName}' already has all features unlocked. Skipping.");
                    continue;
                }

                AssignNewFeatures(engine, engineFeaturesInstance);
                ConfigureDevelopmentPoints(engine, engineFeaturesInstance);
                StartUpdateTask(engine, guiMain, rS);
            }

            guiMain.MessageBox("All engine updates have been successfully queued!", false);
        }

        private static void AssignNewFeatures(engineScript engine, engineFeatures eF)
        {
            for (int i = 0; i < eF.engineFeatures_UNLOCK.Length; i++)
            {
                if (eF.engineFeatures_UNLOCK[i])
                {
                    engine.features[i] = true;
                    engine.featuresInDev[i] = true;
                }
            }

            Debug.Log($"Assigned new features to engine '{engine.myName}'.");
        }

        private static void ConfigureDevelopmentPoints(engineScript engine, engineFeatures eF)
        {
            engine.devPoints = 0;

            for (int i = 0; i < engine.featuresInDev.Length; i++)
            {
                if (engine.featuresInDev[i])
                {
                    engine.devPoints += eF.GetDevPointsForEngine(i);
                }
            }

            engine.devPointsStart = engine.devPoints;
            engine.updating = true;

            Debug.Log($"Configured development points for engine '{engine.myName}'. Total points: {engine.devPoints}.");
        }

        private static void StartUpdateTask(engineScript engine, GUI_Main guiMain, roomScript rS)
        {
            taskEngine task = guiMain.AddTask_Engine();
            task.Init(false);
            task.engineID = engine.myID;

            rS.taskID = task.myID;

            Debug.Log($"Started update task for engine '{engine.myName}'. Task ID: {task.myID}.");
        }
    }
}

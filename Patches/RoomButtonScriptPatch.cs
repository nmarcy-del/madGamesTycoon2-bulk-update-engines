using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BulkEngineUpdateMod.Patches
{
    [HarmonyPatch(typeof(roomButtonScript))]
    [HarmonyPatch("BUTTON_Dev_Engines")]
    public class ButtonDevEnginesPatch
    {
        private static bool _isMenuAdjusted = false;
        private static Queue<engineScript> engineUpdateQueue = new Queue<engineScript>();

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
                if (_isMenuAdjusted)
                {
                    AddCustomButton(menu, guiMain, rS);
                }
            }
            else
            {
                Debug.LogError("Menu not found.");
            }
        }

        private static void AdjustMenuHeight(GameObject menue)
        {

            if (_isMenuAdjusted)
            {
                Debug.Log("Menu height adjustment skipped.");
                return;
            }

            Transform menuTransform = menue.transform.Find("Menue");
            RectTransform menuRect = menuTransform.GetComponent<RectTransform>();
            if (menuRect != null)
            {
                menuRect.sizeDelta = new Vector2(menuRect.sizeDelta.x, menuRect.sizeDelta.y + 35);
                Debug.Log("Increased menu height.");
            }

            _isMenuAdjusted = true;
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
                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x,
                    rectTransform.anchoredPosition.y - 63);
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

            var enginesToUpdate = new List<engineScript>();

            foreach (var engine in playerEngines) {
                // Vérification des fonctionnalités déjà débloquées
                if (engine.features.Zip(engineFeaturesInstance.engineFeatures_UNLOCK, (engineFeature, unlockFeature) => engineFeature == unlockFeature).All(x => x))
                {
                    Debug.Log($"Engine '{engine.myName}' already has all features unlocked. Skipping.");
                    continue;
                }

                // Valider les informations du moteur
                if (engine.features == null || engine.featuresInDev == null)
                {
                    Debug.LogError($"Engine '{engine.myName}' has invalid feature data. Skipping.");
                    continue;
                }

                AssignNewFeatures(engine, engineFeaturesInstance);
                SetNewVersionName(engine);
                ConfigureDevelopmentPoints(engine, engineFeaturesInstance);
                checkEngineInformations(engine);

                enginesToUpdate.Add(engine);

                Debug.Log($"Prepared engine '{engine.myName}' for update.");
            }

            if (enginesToUpdate.Count == 0)
            {
                Debug.LogWarning("No engines to update!");
                guiMain.MessageBox("No engines available for update.", false);
                return;
            }

            TaskEngineCompletePatch.AddEnginesToQueue(enginesToUpdate);
            TaskEngineCompletePatch.StartNextUpdateTask(guiMain, rS);
            guiMain.MessageBox("All engine updates have been successfully queued!", false);
        }

        private static void checkEngineInformations(engineScript engine)
        {
            Debug.LogWarning($"{engine.ownerID}");
            Debug.LogWarning($"{engine.features}");
            Debug.LogWarning($"{engine.featuresInDev}");
            Debug.LogWarning($"{engine.spezialgenre}");
            Debug.LogWarning($"{engine.spezialplatform}");
            Debug.LogWarning($"{engine.updating}");
        }
        
        private static void AssignNewFeatures(engineScript engine, engineFeatures eF)
        {
            if (engine.features.Length != eF.engineFeatures_UNLOCK.Length ||
                engine.featuresInDev.Length != eF.engineFeatures_UNLOCK.Length)
            {
                Debug.LogError(
                    $"Mismatch in feature arrays size for engine '{engine.myName}'. Engine features: {engine.features.Length}, Unlock features: {eF.engineFeatures_UNLOCK.Length}");
                return;
            }

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

        private static void SetNewVersionName(engineScript engine)
        {
            if (string.IsNullOrEmpty(engine.myName))
            {
                Debug.LogError("Engine name is null or empty. Skipping version update.");
                return;
            }
            
            var baseName = GetBaseNameWithoutVersion(engine.myName);
                
            string newVersion = engine.GetVersionString();

            if (string.IsNullOrEmpty(newVersion))
            {
                Debug.LogError($"Failed to retrieve version for engine '{engine.myName}'. Skipping name update.");
                return;
            }

            engine.myName = $"{baseName} {newVersion}";

            Debug.Log($"Updated engine name to: {engine.myName}");
        }


        private static void ConfigureDevelopmentPoints(engineScript engine, engineFeatures eF)
        {
            if (engine.featuresInDev.Length != eF.engineFeatures_UNLOCK.Length)
            {
                Debug.LogError(
                    $"Mismatch in feature arrays size for engine '{engine.myName}'. Cannot configure development points.");
                return;
            }

            engine.devPoints = 0;

            for (int i = 0; i < engine.featuresInDev.Length; i++)
            {
                if (engine.featuresInDev[i])
                {
                    float devPoints = eF.GetDevPointsForEngine(i);
                    if (devPoints <= 0)
                    {
                        Debug.LogWarning($"Feature index {i} has invalid devPoints: {devPoints}. Skipping.");
                        continue;
                    }

                    engine.devPoints += devPoints;
                }
            }

            engine.devPointsStart = engine.devPoints;
            engine.updating = true;

            Debug.Log($"Configured development points for engine '{engine.myName}'. Total points: {engine.devPoints}.");
        }
        
        public static string GetBaseNameWithoutVersion(string engineName)
        {
            if (string.IsNullOrWhiteSpace(engineName))
            {
                Debug.LogWarning("Engine name is empty or null.");
                return string.Empty;
            }
            
            var versionRegex = new Regex(@"^\d+\.\d+$");
            
            var filteredParts = engineName
                .Split(' ') // Diviser le nom par espaces
                .Where(part => !versionRegex.IsMatch(part))
                .ToArray();
            
            string baseName = string.Join(" ", filteredParts);

            Debug.Log($"Processed name: '{baseName}' (original: '{engineName}')");
            return baseName;
        }
    }
}

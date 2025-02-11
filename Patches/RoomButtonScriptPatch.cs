﻿using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using BulkEngineUpdateMod.Config;

namespace BulkEngineUpdateMod.Patches
{
    [HarmonyPatch(typeof(roomButtonScript))]
    [HarmonyPatch("BUTTON_Dev_Engines")]
    public class ButtonDevEnginesPatch
    {
        private static bool _isMenuAdjusted = false;
        private static int _enginePrice;
        private static int _engineProfitShare;

        static void Postfix(roomButtonScript __instance)
        {
            Debug.Log("Patch: BUTTON_Dev_Engines executed !");

            GUI_Main guiMain = AccessTools.FieldRefAccess<roomButtonScript, GUI_Main>(__instance, "guiMain_");
            roomScript rS = AccessTools.FieldRefAccess<roomButtonScript, roomScript>(__instance, "rS_");
            GameObject menu = guiMain.uiObjects[36];

            ValidateAndSetConfig();
            
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
            engineFeatures engineFeaturesInstance =
                AccessTools.FieldRefAccess<mainScript, engineFeatures>(mainScriptInstance, "eF_");

            engineScript[] allEngines = Object.FindObjectsOfType<engineScript>();
            var playerEngines = allEngines.Where(engine => engine.ownerID == mainScriptInstance.myID).ToList();

            if (playerEngines.Count == 0)
            {
                Debug.LogWarning("No engines found for the player!");
                guiMain.MessageBox("No engines available for update.", false);
                return;
            }

            var enginesToUpdate = new List<engineScript>();
            var maxTechLevel = 0;
            var maxTechPlatformId = 0;
            var engineTechLevel = 0;

            foreach (var engine in playerEngines)
            {
                // Vérification si toutes les fonctionnalités sont déjà débloquées
                if (engine.features.Zip(engineFeaturesInstance.engineFeatures_UNLOCK,
                        (engineFeature, unlockFeature) => engineFeature == unlockFeature).All(x => x))
                {
                    Debug.Log($"Engine '{engine.myName}' already has all features unlocked. Skipping.");
                    continue;
                }

                // Validation des données du moteur
                if (engine.features == null || !engine.features.Any() || engine.featuresInDev == null ||
                    !engine.featuresInDev.Any())
                {
                    Debug.LogError($"Engine '{engine.myName}' has invalid or empty feature data. Skipping.");
                    continue;
                }

                engineTechLevel = Mathf.Max(engineTechLevel, engine.GetTechLevel());
                
                Debug.Log($"Engine {engine.myName} has {engine.GetTechLevel()} tech level.");
                
                var spezialPlatformScript = engine.GetSpezialPlatformScript();
                
                if (spezialPlatformScript != null && spezialPlatformScript.tech >= maxTechLevel)
                {
                    maxTechLevel = spezialPlatformScript.tech;
                    maxTechPlatformId = spezialPlatformScript.myID;
                    Debug.Log($"Engine platform {spezialPlatformScript.myName} has {spezialPlatformScript.tech} tech level.");
                }
                
                AssignNewFeatures(engine, engineFeaturesInstance);
                SetNewVersionName(engine);
                ConfigureDevelopmentPoints(engine, engineFeaturesInstance);
                
                if (engine.sellEngine)
                {
                    if (_enginePrice > 0 &&
                        _enginePrice <= 100000)
                    {
                        engine.preis = _enginePrice;
                    }

                    if (_engineProfitShare > 0 &&
                        _engineProfitShare <= 50)
                    {
                        engine.gewinnbeteiligung = _engineProfitShare;
                    }
                }
                
                Debug.Log($"Prepared engine '{engine.myName}' for update.");
                enginesToUpdate.Add(engine);
            }
            
            if (enginesToUpdate.Count > 0 && maxTechLevel > 0 && maxTechPlatformId != 0)
            {
                foreach (var engine in enginesToUpdate)
                {
                    engine.spezialplatform = maxTechPlatformId;
                }

                if (enginesToUpdate.Count == 0)
                {
                    Debug.LogWarning("No engines to update!");
                    guiMain.MessageBox("No engines available for update.", false);
                    return;
                }

                // Ajout et démarrage des tâches
                TaskEngineCompletePatch.AddEnginesToQueue(enginesToUpdate);
                TaskEngineCompletePatch.StartNextUpdateTask(guiMain, rS);
                guiMain.MessageBox("All engine updates have been successfully queued!", false);
            } else if (enginesToUpdate.Count > 0) {
                Debug.LogWarning("No engines to update!");
                guiMain.MessageBox("No engines available for update.", false);
                return;
            }
            else
            {
                Debug.LogWarning($"techLevl : {maxTechLevel}");
                Debug.LogWarning($"Tech platform : {maxTechPlatformId}");
                Debug.LogWarning($"Technology : {engineTechLevel}");
                guiMain.MessageBox(
                    "TechLevel is too low! Please update manually at least one engine with a higher tech level.",
                    false);
                return;
            }
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
        
        public static bool ValidateAndSetConfig()
        {
            var priceConfig = ModConfig.EnginePrice.Value;
            var profitConfig = ModConfig.EngineProfitShare.Value;

            if (priceConfig < 1 || priceConfig > 100000)
            {
                Debug.LogError("EnginePrice est hors des limites autorisées (1 à 100,000). Veuillez corriger la configuration.");
                return false;
            }

            if (profitConfig < 1 || profitConfig > 50)
            {
                Debug.LogError("EngineProfitShare est hors des limites autorisées (1 à 50). Veuillez corriger la configuration.");
                return false;
            }
            
            _enginePrice = priceConfig;
            _engineProfitShare = profitConfig;

            Debug.Log($"Configuration validée : EnginePrice={_enginePrice}, EngineProfitShare={_engineProfitShare}");
            return true;
        }
    }
}

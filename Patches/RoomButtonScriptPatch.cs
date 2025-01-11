using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

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
            GameObject menu = guiMain.uiObjects[36];

            if (menu != null)
            {
                Debug.Log("Menu found: Menu_Dev_EngineMain");
                AdjustMenuHeight(menu);
                AddCustomButton(menu, guiMain);
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

        private static void AddCustomButton(GameObject menu, GUI_Main guiMain)
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

            // Cloner le bouton existant
            GameObject clonedButton = GameObject.Instantiate(originalButton.gameObject, menueTransform);
            clonedButton.name = "Button_CustomAction";

            // Ajuster la position du bouton cloné (50px en dessous)
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
                ReassignButtonAction(buttonComponent, guiMain);
            }
        }

        private static void ReassignButtonAction(Button buttonComponent, GUI_Main guiMain)
        {
            buttonComponent.onClick = new Button.ButtonClickedEvent();
            
            buttonComponent.onClick.AddListener(() =>
            {
                Debug.Log("New action executed from the cloned button!");
                PerformCustomAction(guiMain);
            });

            Debug.Log("Actions successfully reassigned.");
        }

        private static void PerformCustomAction(GUI_Main guiMain)
        {
            Debug.Log("New custom action triggered.");
        }
    }
}

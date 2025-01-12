using System;
using HarmonyLib;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BulkEngineUpdateMod.Patches
{
    [HarmonyPatch(typeof(taskEngine))]
    [HarmonyPatch("Complete")]
    public class TaskEngineCompletePatch
    {
        // Queue pour gérer les moteurs à mettre à jour
        private static Queue<engineScript> engineQueue = new Queue<engineScript>();

        // Préparation avant que la tâche ne soit marquée comme complète
        static void Prefix(taskEngine __instance)
        {
            if (__instance.eS_ != null)
            {
                // Mettre à jour le nom du moteur avant de compléter la tâche
                UpdateEngineName(__instance.eS_);
            }
        }

        // Actions après que la tâche a été complétée
        static void Postfix(taskEngine __instance)
        {
            Debug.Log("Task complete logic executed.");

            GUI_Main guiMain = null;
            roomScript rS = null;

            try
            {
                // Accès à GUI_Main
                guiMain = AccessTools.FieldRefAccess<taskEngine, GUI_Main>(__instance, "guiMain_");

                // Tentative d'accès à rdS_
                try
                {
                    rS = AccessTools.FieldRefAccess<taskEngine, roomScript>(__instance, "rdS_");
                }
                catch
                {
                    Debug.LogWarning("Field 'rdS_' is not accessible. Attempting to find room via alternative method.");
                    var mS = AccessTools.FieldRefAccess<taskEngine, mainScript>(__instance, "mS_");
                    rS = FindRoomByTaskID(mS, __instance.myID);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error accessing fields in taskEngine: {ex.Message}");
                return;
            }

            if (guiMain == null || rS == null)
            {
                Debug.LogError("guiMain or roomScript is null.");
                return;
            }

            Debug.Log($"Engines remaining in queue: {engineQueue.Count}");

            // Vérifiez s'il reste des moteurs dans la queue
            if (engineQueue.Count > 0)
            {
                Debug.Log("Starting the next engine update task.");
                StartNextUpdateTask(guiMain, rS);
            }
            else
            {
                Debug.Log("All queued engines have been updated!");
                guiMain.MessageBox("All engines have been updated!", false);
            }
        }
        
        // Mise à jour du nom du moteur après la tâche
        private static void UpdateEngineName(engineScript engine)
        {
            var baseName = GetBaseNameWithoutVersion(engine.myName);
            string newVersion = engine.GetVersionString();
            engine.myName = $"{baseName} {newVersion}";

            Debug.Log($"Engine name updated to: {engine.myName}");
        }

        // Ajout des moteurs dans la queue pour mise à jour
        public static void AddEnginesToQueue(List<engineScript> engines)
        {
            foreach (var engine in engines)
            {
                engineQueue.Enqueue(engine);
                Debug.Log($"Added engine '{engine.myName}' to the update queue.");
            }

            Debug.Log("Current engine queue:");
            foreach (var engine in engineQueue)
            {
                Debug.Log($"- {engine.myName}");
            }
        }
        
        // Démarrer la tâche pour le prochain moteur dans la queue
        public static void StartNextUpdateTask(GUI_Main guiMain, roomScript rS)
        {
            if (engineQueue.Count == 0)
            {
                Debug.Log("No engines left in the queue.");
                return;
            }

            var nextEngine = engineQueue.Dequeue();

            // Création de la tâche
            taskEngine task = guiMain.AddTask_Engine();
            task.engineID = nextEngine.myID;
            task.Init(false);
            
            Debug.Log($"Engine price : {nextEngine.preis}" );
            Debug.Log($"Engine price : {nextEngine.gewinnbeteiligung}" );
            // Associez la tâche à la salle
            if (rS != null)
            {
                rS.taskID = task.myID;
                Debug.Log($"Associated task ID {task.myID} with room ID {rS.myID}");
            }
            else
            {
                Debug.LogError("roomScript is null. Task cannot be associated with a room.");
            }

            Debug.Log($"Started update task for engine '{nextEngine.myName}'. Task ID: {task.myID}. Task name: {task.name}");
        }
        
        private static roomScript FindRoomByTaskID(mainScript mS, int taskID)
        {
            foreach (var room in mS.arrayRoomScripts)
            {
                if (room != null && room.taskID == taskID)
                {
                    Debug.Log($"Room found for task ID {taskID}: Room ID {room.myID}");
                    return room;
                }
            }

            Debug.LogError($"No room found for task ID {taskID}.");
            return null;
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

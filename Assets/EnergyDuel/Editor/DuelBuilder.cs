using System;
using System.IO;
using System.Linq;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EnergyDuel.Editor
{
    public static class DuelBuilder
    {
        [MenuItem("Herramientas/Construir Energy Duel AR",false,100)]
        public static void Construir()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Sal del modo Play para construir la escena.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var paquete=PackageInfo.FindForAssetPath("Packages/com.github.homuler.mediapipe/package.json");
            if (paquete==null || paquete.version!="0.16.3")
                throw new InvalidOperationException("Se necesita el paquete local MediaPipe Unity Plugin 0.16.3.");
            DuelAssets.Create(paquete.resolvedPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var escena=EditorSceneManager.OpenScene(DuelAssets.Scene);
            DuelSceneCheck.Reparar(escena);
            DuelSceneCheck.Validar(escena);
            EditorSceneManager.MarkSceneDirty(escena);
            if (!EditorSceneManager.SaveScene(escena)) throw new IOException("No se pudo guardar "+escena.path);
            AssetDatabase.SaveAssets();
            // Comprueba lo que leerá Play, incluida la resolución de las instancias de prefab.
            var juego=DuelSceneCheck.Validar(EditorSceneManager.OpenScene(DuelAssets.Scene));
            var otras=EditorBuildSettings.scenes.Where(s=>s.path!=DuelAssets.Scene);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(DuelAssets.Scene,true)}.Concat(otras).ToArray();
            Selection.activeObject=juego.gameObject;
            Debug.Log("Energy Duel AR · escena guardada, referencias comprobadas y escena de inicio configurada.");
        }
    }
}

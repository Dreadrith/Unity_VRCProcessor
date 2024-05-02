using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace DreadScripts.VRCProcessor
{
    public class VRCProcessor : AssetPostprocessor
    {
        private static ModelImporter importer;
        private static bool willProcess;
        private static bool isHuman;
        private void OnPreprocessModel()
        {
            importer = assetImporter as ModelImporter;
            willProcess = isHuman = false;
            string guid = AssetDatabase.AssetPathToGUID(importer.assetPath);
            if (importer && !SessionState.GetBool(guid, false))
            {
                SessionState.SetBool(guid, true);
                willProcess = true;
                importer.isReadable = true;
                importer.importBlendShapeNormals = ModelImporterNormals.Import;
            }
        }

        private void OnPostprocessMeshHierarchy(GameObject g)
        {
            if (willProcess && !isHuman)
            {

                Transform[] allTransforms = g.GetComponentsInChildren<Transform>();
                if (allTransforms.Length < 15) return;

                int hits = allTransforms.Count(t => t.name.IndexOf("Armature", System.StringComparison.OrdinalIgnoreCase) >= 0
                                                    || t.name.IndexOf("Hand", System.StringComparison.OrdinalIgnoreCase) >= 0
                                                    || t.name.IndexOf("Head", System.StringComparison.OrdinalIgnoreCase) >= 0);

                if (hits >= 2)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    isHuman = true;
                }
            }
        }

        private void OnPostprocessModel(GameObject g)
        {
            if (willProcess && isHuman)
            {
                var description = importer.humanDescription;
                var human = description.human.ToList();
                for (int i = human.Count - 1; i >= 0; i--)
                {
                    switch (human[i].humanName)
                    {
                        case "Jaw" when human[i].boneName.IndexOf("jaw", System.StringComparison.OrdinalIgnoreCase) < 0:
                        case "Upper Chest":
                            human.RemoveAt(i);
                            break;
                    }
                }

                description.human = human.ToArray();
                importer.humanDescription = description;
            }
        }

        private static void OnPostprocessAllAssets(string[] ignored, string[] deletedAssets, string[] ignored2, string[] ignored3)
        {
            foreach (string s in deletedAssets)
                SessionState.EraseBool(AssetDatabase.AssetPathToGUID(s));
        }


        [UnityEditor.Callbacks.DidReloadScripts]
        private static void CollectExistingModels()
        {
            foreach (var g in AssetDatabase.FindAssets("t:model"))
                SessionState.SetBool(g, true);
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class QuickLODUtilsEditor
{

    #region CONSTANTS

    public static Color DEFAULT_BUTTON_COLOR = new Color(0.0f, 162.0f / 255.0f, 232.0f / 255.0f);

    #endregion

    public static bool DrawButton(string label, params GUILayoutOption[] options)
    {
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = DEFAULT_BUTTON_COLOR;

        bool result = GUILayout.Button(label, options);

        GUI.backgroundColor = originalColor;

        return result;
    }

    public static bool DrawButton(GUIContent content, params GUILayoutOption[] options)
    {
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = DEFAULT_BUTTON_COLOR;

        bool result = GUILayout.Button(content, options);

        GUI.backgroundColor = originalColor;

        return result;
    }

    public static string CreateAssetFolder(string path)
    {
        string[] folders = path.Split('/');
        string assetFolderPath;

        if (folders.Length > 1)
        {
            string parentFolder = folders[0];

            //Create all the intermediate parent folders if necessary
            for (int i = 1; i < folders.Length - 1; i++)
            {
                string s = folders[i];
                string tmp = parentFolder + '/' + s;
                if (!AssetDatabase.IsValidFolder(parentFolder + '/' + s))
                {
                    AssetDatabase.CreateFolder(parentFolder, s);
                }
                parentFolder = tmp;
            }

            //Create the last folder in the path
            assetFolderPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(parentFolder, folders[folders.Length - 1]));
        }
        else
        {
            assetFolderPath = path;
        }

        return assetFolderPath;
    }

}

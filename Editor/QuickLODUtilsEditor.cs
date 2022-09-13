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
        Debug.Log("path = " + path);
        string[] folders = path.Split('/');
        string assetFolderPath;

        Debug.Log("numFolders = " + folders.Length);

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
                    Debug.Log(AssetDatabase.CreateFolder(parentFolder, s));
                    AssetDatabase.Refresh();
                }
                parentFolder = tmp;
            }

            //Create the last folder in the path
            string guid = AssetDatabase.CreateFolder(parentFolder, folders[folders.Length - 1]);
            assetFolderPath = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.Refresh();

            //Debug.Log(guid);
            //Debug.Log(parentFolder);
            //Debug.Log(folders[folders.Length - 1]);
            //Debug.Log("assetFolderPath = " + assetFolderPath);
        }
        else
        {
            assetFolderPath = path;
        }

        return assetFolderPath;
    }

}

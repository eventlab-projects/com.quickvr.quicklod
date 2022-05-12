using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace QuickVR.QuickLOD
{

    public class QuickDataTransferWindow : EditorWindow
    {

        #region PROTECTED ATTRIBUTES

        protected GameObject _source = null;
        protected GameObject _target = null;

        protected Vector2 _scrollPos;

        protected QuickLOD _simplifier
        {
            get
            {
                if (m_Simplifier == null)
                {
                    m_Simplifier = QuickLOD._instance;
                }

                return m_Simplifier;
            }
        }
        [SerializeField]
        QuickLOD m_Simplifier = null;

        #endregion

        #region CREATION AND DESTRUCTION

        [MenuItem("QuickVR/QuickLOD/DataTransfer")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            QuickDataTransferWindow window = (QuickDataTransferWindow)GetWindow(typeof(QuickDataTransferWindow));
            window.Show();
        }

        #endregion

        #region ONGUI

        protected virtual void OnGUI()
        {
            minSize = new Vector2(480, 100);

            EditorGUILayout.BeginVertical(GUILayout.Width(384));
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            titleContent.text = "QuickLOD";

            EditorGUILayout.Space();

            DrawInput();

            if (_source == null || _target == null)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Please, define the Source and Target GameObjects to start the Data Transfer process. ");
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical("box");

                if (QuickLODUtilsEditor.DrawButton("Transfer Blendshapes"))
                {
                    //_simplifier.Simplify(_target, _reductionFactor, renderGroups);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        protected virtual void DrawInput()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUI.BeginChangeCheck();
            _source = EditorGUILayout.ObjectField("Source", _source, typeof(GameObject), true) as GameObject;
            _target = EditorGUILayout.ObjectField("Target", _target, typeof(GameObject), true) as GameObject;
            EditorGUILayout.EndVertical();
        }

        #endregion

    }

}


using UnityEditor;

using UnityEngine;

using System.Collections.Generic;

namespace QuickVR.QuickLOD
{

    public class QuickMeshSplitterWindow : EditorWindow
    {

        #region PROTECTED ATTRIBUTES

        protected SerializedObject _serializedObject
        {
            get
            {
                if (m_SerializedObject == null)
                {
                    m_SerializedObject = new SerializedObject(this);
                }

                return m_SerializedObject;
            }
        }
        protected SerializedObject m_SerializedObject = null;

        [SerializeField]
        protected List<GameObject> _sources = new List<GameObject>();

        [SerializeField]
        protected TextAsset _polygonsHead = null;

        [SerializeField]
        protected TextAsset _polygonsMouth = null;

        [SerializeField]
        protected TextAsset _polygonsHands = null;

        [SerializeField]
        protected TextAsset _polygonsFeet = null;

        [SerializeField]
        protected TextAsset _polygonsUpperBody = null;

        [SerializeField]
        protected TextAsset _polygonsLowerBody = null;

        public int _bodyPartsMask = -1;

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

        protected string[] _bodyPartsValues
        {
            get
            {
                if (m_BodyPartsValues == null || m_BodyPartsValues.Length == 0)
                {
                    m_BodyPartsValues = QuickLODUtils.GetEnumValuesToString<QuickMeshSplitter.BodyParts>().ToArray();
                }

                return m_BodyPartsValues;
            }
        }
        protected string[] m_BodyPartsValues = null;

        #endregion

        #region CREATION AND DESTRUCTION

        [MenuItem("QuickVR/QuickLOD/MeshSplitter")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            QuickMeshSplitterWindow window = (QuickMeshSplitterWindow)GetWindow(typeof(QuickMeshSplitterWindow));

            window._polygonsHead = Resources.Load<TextAsset>("headFusePolygons");
            window._polygonsMouth = Resources.Load<TextAsset>("mouthFusePolygons");
            window._polygonsHands = Resources.Load<TextAsset>("handsFusePolygons");
            window._polygonsFeet = Resources.Load<TextAsset>("feetFusePolygons");
            window._polygonsUpperBody = Resources.Load<TextAsset>("upperBodyFusePolygons");
            window._polygonsLowerBody = Resources.Load<TextAsset>("lowerBodyFusePolygons");

            window.Show();
        }

        protected virtual QuickMeshSplitter.SplitDescriptor CreateSplitDescriptor()
        {
            QuickMeshSplitter.SplitDescriptor desc = new QuickMeshSplitter.SplitDescriptor();

            desc._polygonsHead = _polygonsHead;
            desc._polygonsMouth = _polygonsMouth;
            desc._polygonsHands = _polygonsHands;
            desc._polygonsFeet = _polygonsFeet;
            desc._polygonsUpperBody = _polygonsUpperBody;
            desc._polygonsLowerBody = _polygonsLowerBody;

            return desc;
        }

        #endregion

        #region ONGUI

        protected virtual void OnGUI()
        {
            minSize = new Vector2(480, 100);

            EditorGUILayout.BeginVertical(GUILayout.Width(384));
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            titleContent.text = "QuickMeshSplitter";

            EditorGUILayout.Space();

            DrawSource();

            if (_sources.Count == 0)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Please, select a GameObject on the scene to start the Split process. ");
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical("box");
                
                _polygonsHead = EditorGUILayout.ObjectField("Polygons Head", _polygonsHead, typeof(TextAsset), true) as TextAsset;
                _polygonsMouth = EditorGUILayout.ObjectField("Polygons Mouth", _polygonsMouth, typeof(TextAsset), true) as TextAsset;
                _polygonsHands = EditorGUILayout.ObjectField("Polygons Hands", _polygonsHands, typeof(TextAsset), true) as TextAsset;
                _polygonsFeet = EditorGUILayout.ObjectField("Polygons Feet", _polygonsFeet, typeof(TextAsset), true) as TextAsset;
                _polygonsUpperBody = EditorGUILayout.ObjectField("Polygons Upper Body", _polygonsUpperBody, typeof(TextAsset), true) as TextAsset;
                _polygonsLowerBody = EditorGUILayout.ObjectField("Polygons Lower Body", _polygonsLowerBody, typeof(TextAsset), true) as TextAsset;

                _bodyPartsMask = EditorGUILayout.MaskField("Body Parts Mask", _bodyPartsMask, _bodyPartsValues);

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();

                if (QuickLODUtilsEditor.DrawButton("Split"))
                {
                    QuickMeshSplitter.SplitDescriptor desc = CreateSplitDescriptor();

                    foreach (GameObject go in _sources)
                    {
                        QuickMeshSplitter._instance.Split(go, desc);
                    }
                }
                if (QuickLODUtilsEditor.DrawButton("Process Fuse Character"))
                {
                    QuickMeshSplitter.SplitDescriptor desc = CreateSplitDescriptor();

                    foreach (GameObject go in _sources)
                    {
                        QuickMeshSplitter._instance.ProcessFuseCharacter(go, desc);
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        protected virtual void DrawSource()
        {
            EditorGUILayout.BeginVertical("box");

            SerializedProperty sProperty = _serializedObject.FindProperty("_sources");
            EditorGUILayout.PropertyField(sProperty);
            _serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.EndVertical();
        }

        #endregion

    }

}


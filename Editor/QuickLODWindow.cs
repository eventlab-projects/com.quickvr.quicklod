using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace QuickVR.QuickLOD
{

    public class QuickLODWindow : EditorWindow
    {

        #region PROTECTED ATTRIBUTES

        protected GameObject _target = null;

        [SerializeField]
        protected Renderer[] _renderers = null;

        [SerializeField]
        protected float[] _reductionFactor = null;

        [SerializeField]
        protected bool _showReduction = true;

        [SerializeField]
        protected List<Renderer> _freeRenderers = new List<Renderer>(); //The list of renderers that has not been assigned to any group yet

        [System.Serializable]
        protected class RenderGroupUI : RenderGroup
        {
            public bool _show = true;
            public bool _showRenderers = true;
        }

        [SerializeField]
        protected List<RenderGroupUI> _renderGroups = new List<RenderGroupUI>();
        protected int _currentRenderGroupID = 0;

        protected bool _showRenderGroups = true;

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

        [MenuItem("QuickVR/QuickLOD")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            QuickLODWindow window = (QuickLODWindow)GetWindow(typeof(QuickLODWindow));
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

            DrawSource();

            if (_target == null)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Please, select a GameObject on the scene to start the LOD process. ");
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical("box");
                _showReduction = EditorGUILayout.Foldout(_showReduction, "Reduction Settings");
                if (_showReduction)
                {
                    EditorGUI.indentLevel++;
                    int totalETC = DrawReductionFactors();
                    DrawTotalETC(totalETC);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("box");
                _showRenderGroups = EditorGUILayout.Foldout(_showRenderGroups, "Render Groups");
                if (_showRenderGroups)
                {
                    EditorGUI.indentLevel++;
                    DrawRenderGroups();
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("box");
                
                if (QuickLODUtilsEditor.DrawButton("Simplify"))
                {
                    //Debug.Log(Resources.Load<Shader>("BakeVertexColorMap"));
                    //Debug.Log(Resources.Load<Shader>("Dilate"));

                    //Create the list of RenderGroups. 
                    List<RenderGroup> renderGroups = new List<RenderGroup>(_renderGroups);

                    //Create a RenderGroup for each of the Renderers that has not been assigned to
                    //any RenderGroup
                    foreach (Renderer r in _freeRenderers)
                    {
                        RenderGroup rGroup = new RenderGroup();
                        rGroup._name = r.name;
                        rGroup._renderers.Add(r);

                        renderGroups.Add(rGroup);
                    }

                    _simplifier.Simplify(_target, _reductionFactor, renderGroups);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        protected virtual void DrawSource()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUI.BeginChangeCheck();
            _target = EditorGUILayout.ObjectField("Source", _target, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck() && _target)
            {
                //The selected gameObject has changed
                _renderers = _target.GetComponentsInChildren<Renderer>();
                _reductionFactor = new float[_renderers.Length];
                for (int i = 0; i < _reductionFactor.Length; i++)
                {
                    _reductionFactor[i] = 0.5f;
                }

                _freeRenderers = new List<Renderer>(_renderers);
                _renderGroups.Clear();
            }
            EditorGUILayout.EndVertical();
        }

        protected virtual int DrawReductionFactors()
        {
            EditorGUILayout.BeginVertical();

            //A GameObject is selected. Expose all its renderers. 
            int totalETC = 0;

            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];

                EditorGUILayout.BeginHorizontal();

                //_reductionFactor[i] = EditorGUILayout.Slider(r.name, _reductionFactor[i], 0.0f, 1.0f);
                EditorGUILayout.LabelField(r.name, GUILayout.Width(96));
                _reductionFactor[i] = EditorGUILayout.Slider(_reductionFactor[i], 0.0f, 1.0f);
                int etc = (int)(r.GetMesh().GetNumTriangles() * _reductionFactor[i]);
                totalETC += etc;

                EditorGUILayout.LabelField(etc.ToString(), GUILayout.Width(64));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            return totalETC;
        }

        protected virtual void DrawTotalETC(int totalETC)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Total ETC");
            EditorGUILayout.LabelField(totalETC.ToString(), GUILayout.Width(64));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        protected virtual void DrawRenderGroups()
        {
            List<RenderGroupUI> rGroupsToRemove = new List<RenderGroupUI>();
            for (int i = 0; i < _renderGroups.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");

                RenderGroupUI rGroup = _renderGroups[i];

                EditorGUILayout.BeginHorizontal();
                rGroup._show = EditorGUILayout.Foldout(rGroup._show, rGroup._name);
                if (QuickLODUtilsEditor.DrawButton("-", GUILayout.Width(24))) 
                {
                    rGroupsToRemove.Add(rGroup);
                }
                EditorGUILayout.EndHorizontal();
                if (rGroup._show)
                {
                    EditorGUI.indentLevel++;
                    rGroup._name = EditorGUILayout.TextField("Name", rGroup._name);
                    rGroup._atlasResolution = EditorGUILayout.IntField("Atlas Resolution", rGroup._atlasResolution);

                    EditorGUILayout.BeginHorizontal();
                    rGroup._showRenderers = EditorGUILayout.Foldout(rGroup._showRenderers, "Renderers");
                    if (QuickLODUtilsEditor.DrawButton("+", GUILayout.Width(24)))
                    {
                        _currentRenderGroupID = i;
                        DrawFreeRendererSelector();
                    }
                    EditorGUILayout.EndHorizontal();

                    if (rGroup._showRenderers)
                    {
                        EditorGUI.indentLevel++;
                        DrawRenderersInRenderGroup(rGroup);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            if (rGroupsToRemove.Count > 0)
            {
                for (int i = rGroupsToRemove.Count - 1; i >= 0; i--)
                {
                    RenderGroupUI rGroup = rGroupsToRemove[i];
                    for (int j = rGroup._renderers.Count - 1; j >= 0; j--)
                    {
                        RemoveRendererFromRenderGroup(rGroup._renderers[j], rGroup);
                    }
                    _renderGroups.Remove(rGroup);
                }

                GUIUtility.keyboardControl = 0; 
            }
                        
            EditorGUILayout.Space();

            if (QuickLODUtilsEditor.DrawButton("Add New Group"))
            {
                RenderGroupUI rGroup = new RenderGroupUI();
                rGroup._name = "New Render Group";
                _renderGroups.Add(rGroup);
            }
        }

        protected virtual void DrawRenderersInRenderGroup(RenderGroup rGroup)
        {
            EditorGUILayout.BeginVertical();
            Renderer rToRemove = null;
            foreach (Renderer r in rGroup._renderers)
            {
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = false;
                EditorGUILayout.ObjectField(r, typeof(Renderer), true);
                GUI.enabled = true;
                if (QuickLODUtilsEditor.DrawButton("-", GUILayout.Width(24)))
                {
                    rToRemove = r;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            if (rToRemove != null)
            {
                RemoveRendererFromRenderGroup(rToRemove, rGroup);
            }
        }

        protected virtual void RemoveRendererFromRenderGroup(Renderer rToRemove, RenderGroup rGroup)
        {
            rGroup._renderers.Remove(rToRemove);
            _freeRenderers.Add(rToRemove);
        }

        protected virtual void DrawFreeRendererSelector()
        {
            GenericMenu rendererSelector = new GenericMenu();
            foreach (Renderer r in _renderers)
            {
                if (_freeRenderers.Contains(r))
                {
                    rendererSelector.AddItem(new GUIContent(r.name), false, OnRendererSelected, r);
                }
            } 
            
            rendererSelector.ShowAsContext();
        }

        protected virtual void OnRendererSelected(object value)
        {
            Renderer r = (Renderer)value;
            _freeRenderers.Remove(r);
            _renderGroups[_currentRenderGroupID]._renderers.Add(r);
        }

        #endregion

    }

}


//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEditor;

//namespace QuickVR
//{

//    [CustomEditor(typeof(TestBlendShapes), true)]
//    public class TestBlendShapesEditor : QuickBaseEditor
//    {

//        protected TestBlendShapes _target
//        {
//            get
//            {
//                if (!m_Target)
//                {
//                    m_Target = target as TestBlendShapes;
//                }

//                return m_Target;
//            }
//        }
//        private TestBlendShapes m_Target = null;

//        protected bool _showRenderMap = true;

//        protected override void DrawGUI()
//        {
//            //base.DrawGUI();

//            EditorGUI.BeginChangeCheck();
//            DrawPropertyField("_source", "Source");
//            DrawPropertyField("_target", "Target");
//            if (EditorGUI.EndChangeCheck())
//            {
//                _target._renderMap = null;
//            }

//            if (_target._renderMap == null || _target._renderMap.Count == 0)
//            {
//                _target.InitRenderMap();
//            }

//            if (_target._renderMap != null && _target._target)
//            {
//                _showRenderMap = EditorGUILayout.Foldout(_showRenderMap, "RenderMap");
//                if (_showRenderMap)
//                {
//                    EditorGUI.indentLevel++;

//                    for (int i = 0; i < _target._renderMap.Count; i++)
//                    {
//                        TestBlendShapes.RenderPair p = _target._renderMap[i];
//                        EditorGUI.BeginChangeCheck();
//                        p._renderDest = EditorGUILayout.ObjectField(_target._target.name + "." + p._renderOrigin.name, p._renderDest, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
//                        if (EditorGUI.EndChangeCheck())
//                        {
//                            _target._renderMap[i] = p;
//                            EditorUtility.SetDirty(target);
//                        }
//                    }

//                    EditorGUI.indentLevel--;
//                }
//            }

//            DrawPropertyField("_drawOctree", "Draw Octree");
//            DrawPropertyField("_drawMissingVertices", "Draw Missing Vertices");
//            DrawPropertyField("_radiusMissingVertices", "Radius Missing Vertices");
//        }

//    }

//}



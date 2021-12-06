using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

using Simplygon;
using Simplygon.Unity.EditorPlugin;

using UnityEngine;
using Unity.Formats.USD;

using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;

//https://documentation.simplygon.com/SimplygonSDK_9.1.14300.0/unity/gettingstarted/scripting.html

namespace QuickVR.QuickLOD
{

    [System.Serializable]
    public abstract class QuickLODBase
    {

        #region PROTECTED ATTRIBUTES

        protected List<string> _blendshapeNames = new List<string>();

        //The blendshapeMap is a dictionary that, given a Mesh, it returns a dictionary where for a given blendShapeName, 
        //it returns the id of such blendshape in that mesh. 
        protected Dictionary<Mesh, Dictionary<string, BlendshapeData>> _blendshapeMap = new Dictionary<Mesh, Dictionary<string, BlendshapeData>>();

        protected Vector3Int _newRegionColor = Vector3Int.zero;

        #endregion

        #region CONSTANTS

        protected const string SIMPLYGON_TMP_FOLDER = "Assets/QuickLOD/SimplygonTMP/";

        #endregion

        #region CREATION AND DESTRUCTION

        #endregion

        #region GET AND SET

        protected virtual Color32 ComputeNewRegionColor()
        {
            if (_newRegionColor.x >= 256)
            {
                Debug.LogWarning("Maxmimum region count reached!!!");
                return new Color32(255, 255, 255, 255);
            }

            _newRegionColor.z++;
            if (_newRegionColor.z == 256)
            {
                _newRegionColor.z = 0;
                _newRegionColor.y++;
            }
            if (_newRegionColor.y == 256)
            {
                _newRegionColor.z = 0;
                _newRegionColor.y = 0;
                _newRegionColor.x++;
            }

            return new Color32((byte)_newRegionColor.x, (byte)_newRegionColor.y, (byte)_newRegionColor.z, 255);
        }

        protected virtual void ComputeBlendshapeMap(Mesh[] mSources)
        {
            _blendshapeNames = new List<string>();
            _blendshapeMap = new Dictionary<Mesh, Dictionary<string, BlendshapeData>>();
            List<string> bsNamesMixamo = QuickLODUtils.GetEnumValuesToString<BlendShapesMixamo>();

            HashSet<string> tmp = new HashSet<string>();

            foreach (Mesh m in mSources)
            {
                _blendshapeMap[m] = new Dictionary<string, BlendshapeData>();
                for (int i = 0; i < m.blendShapeCount; i++)
                {
                    //string bsName = m.GetBlendShapeName(i);
                    string bsName = bsNamesMixamo[i];

                    BlendshapeData bsData = new BlendshapeData(m, i);
                    _blendshapeMap[m][bsName] = bsData;

                    if (!tmp.Contains(bsName))
                    {
                        _blendshapeNames.Add(bsName);
                        tmp.Add(bsName);
                    }
                }
            }
        }

        protected virtual void ComputeSkeleton(SkinnedMeshRenderer[] rSources, out List<Transform> bones, out List<Matrix4x4> bindposes, out Dictionary<Mesh, int[]> boneMap)
        {
            bones = new List<Transform>();
            bindposes = new List<Matrix4x4>();
            boneMap = new Dictionary<Mesh, int[]>();

            foreach (SkinnedMeshRenderer r in rSources)
            {
                Mesh m = r.GetMesh();

                int[] map = new int[r.bones.Length];  //Given the ID of a bone in rSource, it returns the corresponding ID in rResult

                for (int i = 0; i < r.bones.Length; i++)
                {
                    Transform t = r.bones[i];
                    if (!bones.Contains(t))
                    {
                        bones.Add(t);
                        bindposes.Add(m.bindposes[i]);
                    }

                    map[i] = bones.IndexOf(t);
                }

                boneMap[m] = map;
            }
        }

        protected virtual QuickTriangleMesh[] ComputeClosestTriangles(Mesh[] mSources, Mesh mTarget)
        {
            QuickTriangleMesh[] closestTriangles = new QuickTriangleMesh[mTarget.vertexCount];
            for (int j = 0; j < mTarget.vertexCount; j++)
            {
                VertexData vData = new VertexData(mTarget.vertices[j], mTarget.normals[j], mTarget.colors32[j]);
                QuickTriangleMesh t = GetClosestTriangle(vData, mSources);
                closestTriangles[j] = t;
                if (t == null)
                {
                    Debug.Log(j);
                    Debug.Log(mSources[0].name);
                    //Debug.Log(mTarget.vertices[j].ToString("f16"));
                    //if (j == 21)
                    //{
                    //    GetClosestTriangle(mTarget.vertices[j], mTarget.normals[j], true);
                    //    _missedVertices.Add(mTarget.vertices[j]);
                    //}
                }
            }

            return closestTriangles;
        }

        protected abstract QuickTriangleMesh GetClosestTriangle(VertexData vData, Mesh[] mSources, bool print = false);

        #endregion

        #region UPDATE

        public virtual GameObject Simplify(GameObject goSource, float[] reductionFactors, List<RenderGroup> renderGroupsSource)
        {
            Dictionary<string, float> reductionFactorMap = new Dictionary<string, float>();
            Renderer[] renderersSource = goSource.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderersSource.Length; i++)
            {
                reductionFactorMap[renderersSource[i].name] = reductionFactors[i];
            }

            //Create a copy of the original GameObject and replace the meshes by the simplified ones
            GameObject go = UnityEngine.Object.Instantiate(goSource, Vector3.zero, Quaternion.identity);
            go.name = goSource.name + "_MERGED";

            //Create a new list of RenderGroups that points to the Renderers of the cloned GameObject
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            List<RenderGroup> renderGroups = new List<RenderGroup>();
            foreach (RenderGroup rGroupSource in renderGroupsSource)
            {
                RenderGroup rGroup = new RenderGroup();
                rGroup._name = rGroupSource._name;

                foreach (Renderer rSource in rGroupSource._renderers)
                {
                    foreach (Renderer r in renderers)
                    {
                        if (r.name == rSource.name)
                        {
                            rGroup._renderers.Add(r);
                        }
                    }
                }

                renderGroups.Add(rGroup);
            }

            foreach (RenderGroup rGroup in renderGroups)
            {
                using (ISimplygon simplygon = global::Simplygon.Loader.InitSimplygon
                (out EErrorCodes simplygonErrorCode, out string simplygonErrorMessage))
                {
                    if (simplygonErrorCode == Simplygon.EErrorCodes.NoError)
                    {
                        if (rGroup.IsSkinned())
                        {
                            InitRenderGroupData(rGroup);
                        }
                        
                        //Simplify the renderers in the RenderGroup
                        List<GameObject> simplifiedGameObjects = new List<GameObject>();
                        Renderer r = null;
                        for (int i = 0; i < rGroup._renderers.Count; i++)
                        {
                            r = rGroup._renderers[i];
                            GameObject sGO = Simplify(simplygon, r, i, reductionFactorMap[r.name]);
                            simplifiedGameObjects.Add(sGO);
                        }

                        //Merge the materials of the simplified renderers
                        Renderer rMerged = MergeMaterials(simplygon, simplifiedGameObjects, rGroup._atlasResolution, rGroup._name).GetComponent<Renderer>();
                        GameObject mGO = go.transform.CreateChild(rGroup._name, false).gameObject;
                        if (rGroup.IsSkinned())
                        {
                            //The RenderGroup is skinned, so we need to transfer the skinning data to the new Renderer
                            r = mGO.AddComponent<SkinnedMeshRenderer>();
                            r.SetMesh(rMerged.GetMesh());
                            TransferVertexData(rGroup.GetSkinnedMeshrenderers(), (SkinnedMeshRenderer)r);
                        }
                        else
                        {
                            //The RenderGroup does not contain skinning information. So just add the MeshRenderer and MeshFilter components. 
                            r = mGO.AddComponent<MeshRenderer>();
                            r.SetMesh(rMerged.GetMesh());
                        }

                        r.sharedMaterial = rMerged.sharedMaterial;
                        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(r.sharedMaterial), rGroup._name + "Material");
                        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(r.sharedMaterial.mainTexture), rGroup._name + "Albedo");
                        AssetDatabase.Refresh();

                        //Destroy the old renderers of this RenderGroup
                        QuickLODUtils.DestroyImmediate(rGroup._renderers.ToArray());

                        //Destroy the temporal GameObjects
                        UnityEngine.Object.DestroyImmediate(rMerged.gameObject);
                        QuickLODUtils.DestroyImmediate(simplifiedGameObjects.ToArray());
                    }
                    else
                    {
                        Debug.LogError("Simplygon initializing failed!");
                        Debug.LogError(simplygonErrorCode);
                        Debug.LogError(simplygonErrorMessage);
                    }
                }
            }

            go.transform.position = goSource.transform.position;
            go.transform.rotation = goSource.transform.rotation;

            GameObject goResult = Export(goSource.name, go);

            //Destroy go and all the temporal assets
            UnityEngine.Object.DestroyImmediate(go);
            AssetDatabase.DeleteAsset(SIMPLYGON_TMP_FOLDER);
            AssetDatabase.Refresh();

            return goResult;
        }

        protected virtual void InitRenderGroupData(RenderGroup rGroup)
        {

        }

        protected virtual GameObject Simplify(ISimplygon simplygon, Renderer r, int renderID, float reductionFactor)
        {
            GameObject result = null;

            List<GameObject> selectedGameObjects = new List<GameObject>();
            selectedGameObjects.Add(r.gameObject);

            string exportTempDirectory = SimplygonUtils.GetNewTempDirectory();

            using (spScene sgScene = SimplygonExporter.Export(simplygon, exportTempDirectory, selectedGameObjects))
            {

                using (spReductionPipeline reductionPipeline = simplygon.CreateReductionPipeline())
                using (spReductionSettings reductionSettings = reductionPipeline.GetReductionSettings())
                {
                    reductionSettings.SetReductionTargets(EStopCondition.All, true, false, false, false);
                    reductionSettings.SetReductionTargetTriangleRatio(reductionFactor);
                    //reductionSettings.SetGroupImportance(10);
                    reductionSettings.SetSkinningImportance(10);

                    result = ExecuteSimplygonPipeline(simplygon, reductionPipeline, sgScene, r.gameObject.name);
                }

            }

            return result;
        }

        protected GameObject MergeMaterials(ISimplygon simplygon, List<GameObject> selectedGameObjects, int atlasResolution, string resultName)
        {
            GameObject result = null;
            string exportTempDirectory = SimplygonUtils.GetNewTempDirectory();

            using (spScene sgScene = SimplygonExporter.Export(simplygon, exportTempDirectory, selectedGameObjects))
            using (spAggregationPipeline pipeline = simplygon.CreateAggregationPipeline())
            using (spAggregationSettings pipelineSettings = pipeline.GetAggregationSettings())
            {
                //Aggregation settings
                pipelineSettings.SetMergeGeometries(true);
                pipelineSettings.SetEnableGeometryCulling(false);
                    
                // Generates a mapping image which is used after the reduction to cast new materials to the new 
                // reduced object. 
                spMappingImageSettings sgMappingImageSettings = pipeline.GetMappingImageSettings();
                sgMappingImageSettings.SetGenerateMappingImage(true);
                sgMappingImageSettings.SetGenerateTexCoords(false);
                sgMappingImageSettings.SetGenerateTangents(false);
                sgMappingImageSettings.SetUseFullRetexturing(true);
                sgMappingImageSettings.SetApplyNewMaterialIds(true);
                sgMappingImageSettings.SetTexCoordGeneratorType(ETexcoordGeneratorType.ChartAggregator);

                spMappingImageOutputMaterialSettings sgOutputMaterialSettings = sgMappingImageSettings.GetOutputMaterialSettings(0);
                // Setting the size of the output material for the mapping image. This will be the output size of the 
                // textures when we do material casting in a later stage. 
                sgOutputMaterialSettings.SetTextureWidth((uint)atlasResolution);
                sgOutputMaterialSettings.SetTextureHeight((uint)atlasResolution);

                // Add diffuse material caster to pipeline. 
                spColorCaster sgDiffuseCaster = simplygon.CreateColorCaster();
                spColorCasterSettings sgDiffuseCasterSettings = sgDiffuseCaster.GetColorCasterSettings();
                sgDiffuseCasterSettings.SetMaterialChannel("diffuseColor");
                sgDiffuseCasterSettings.SetOpacityChannelComponent(EColorComponent.Alpha);
                sgDiffuseCasterSettings.SetOpacityChannel("diffuseColor");
                sgDiffuseCasterSettings.SetDitherType(EDitherPatterns.FloydSteinberg);
                sgDiffuseCasterSettings.SetFillMode(EAtlasFillMode.Interpolate);
                sgDiffuseCasterSettings.SetDilation(10);
                sgDiffuseCasterSettings.SetUseMultisampling(true);
                sgDiffuseCasterSettings.SetOutputPixelFormat(EPixelFormat.R8G8B8A8);
                sgDiffuseCasterSettings.SetOutputSRGB(true);
                sgDiffuseCasterSettings.SetOutputImageFileFormat(EImageOutputFormat.PNG);
                sgDiffuseCasterSettings.SetBakeOpacityInAlpha(false);
                sgDiffuseCasterSettings.SetSkipCastingIfNoInputChannel(false);
                sgDiffuseCasterSettings.SetOutputOpacityType(EOpacityType.Opacity);

                pipeline.AddMaterialCaster(sgDiffuseCaster, 0);

                result = ExecuteSimplygonPipeline(simplygon, pipeline, sgScene, resultName);
            }

            return result;
        }

        protected virtual GameObject ExecuteSimplygonPipeline(ISimplygon simplygon, spPipeline pipeline, spScene scene, string resultName)
        {
            pipeline.RunScene(scene, EPipelineRunMode.RunInThisProcess);

            string assetFolderPath = QuickLODUtilsEditor.CreateAssetFolder(SIMPLYGON_TMP_FOLDER + resultName);

            int startingLodIndex = 1;
            List<GameObject> processedGameObjects = new List<GameObject>();
            SimplygonImporter.Import(simplygon, pipeline, ref startingLodIndex, assetFolderPath, resultName, processedGameObjects);
            GameObject result = processedGameObjects[0];

            UsdAsset usd = result.GetComponent<UsdAsset>();
            if (usd)
            {
                usd.RemoveAllUsdComponents();
            }

            return result;
        }

        protected virtual void TransferVertexData(SkinnedMeshRenderer[] rSources, SkinnedMeshRenderer rTarget)
        {
            Mesh[] mSources = new Mesh[rSources.Length];
            for (int i = 0; i < rSources.Length; i++)
            {
                mSources[i] = rSources[i].GetMesh();
            }

            Mesh mTarget = rTarget.GetMesh();

            //For each vertex in mTarget, compute its closest triangle in mSources
            float timeStart = Time.realtimeSinceStartup;
            QuickTriangleMesh[] closestTriangles = ComputeClosestTriangles(mSources, mTarget);
            Debug.Log("timeComputeClosestTriangles = " + (Time.realtimeSinceStartup - timeStart).ToString("f3"));

            //Transfer the skinning
            timeStart = Time.realtimeSinceStartup;
            TransferSkinning(rSources, rTarget, closestTriangles);
            Debug.Log("timeTransferSkinning = " + (Time.realtimeSinceStartup - timeStart).ToString("f3"));

            //Transfer the BlendShapes
            timeStart = Time.realtimeSinceStartup;
            TransferBlendShapes(mSources, mTarget, closestTriangles);
            Debug.Log("timeTransferBlendShapes = " + (Time.realtimeSinceStartup - timeStart).ToString("f3"));
        }

        public virtual void TransferSkinning(SkinnedMeshRenderer rSource, SkinnedMeshRenderer rTarget, QuickTriangleMesh[] closestTriangles)
        {
            TransferSkinning(new SkinnedMeshRenderer[] { rSource }, rTarget, closestTriangles);
        }

        public virtual void TransferSkinning(SkinnedMeshRenderer[] rSources, SkinnedMeshRenderer rTarget, QuickTriangleMesh[] closestTriangles)
        {
            Mesh mTarget = rTarget.GetMesh();
            ComputeSkeleton(rSources, out List<Transform> bones, out List<Matrix4x4> bindposes, out Dictionary<Mesh, int[]> boneMap);

            BoneWeight[] boneWeights = new BoneWeight[mTarget.vertexCount];
            for (int j = 0; j < mTarget.vertexCount; j++)
            {
                QuickTriangleMesh t = closestTriangles[j];
                closestTriangles[j] = t;
                if (t != null)
                {
                    int vID = t.GetClosestVertexID(mTarget.vertices[j]);
                    Mesh m = t._mesh;
                    BoneWeight bWeight = m.boneWeights[vID];

                    if (bWeight.weight0 > 0)
                    {
                        bWeight.boneIndex0 = boneMap[m][bWeight.boneIndex0];
                    }
                    if (bWeight.weight1 > 0)
                    {
                        bWeight.boneIndex1 = boneMap[m][bWeight.boneIndex1];
                    }
                    if (bWeight.weight2 > 0)
                    {
                        bWeight.boneIndex2 = boneMap[m][bWeight.boneIndex2];
                    }
                    if (bWeight.weight3 > 0)
                    {
                        bWeight.boneIndex3 = boneMap[m][bWeight.boneIndex3];
                    }

                    boneWeights[j] = bWeight;
                }
            }

            rTarget.bones = bones.ToArray();
            mTarget.boneWeights = boneWeights;
            mTarget.bindposes = bindposes.ToArray();
        }

        public virtual void TransferBlendShapes(Mesh mSource, Mesh mTarget, QuickTriangleMesh[] closestTriangles)
        {
            TransferBlendShapes(new Mesh[] { mSource }, mTarget, closestTriangles);
        }

        public virtual void TransferBlendShapes(Mesh[] mSources, Mesh mTarget, QuickTriangleMesh[] closestTriangles)
        {
            ComputeBlendshapeMap(mSources);

            mTarget.ClearBlendShapes();

            Vector3[] dVerticesTarget = new Vector3[mTarget.vertexCount];
            Vector3[] dNormalsTarget = new Vector3[mTarget.vertexCount];
            Vector3[] dTangentsTarget = new Vector3[mTarget.vertexCount];

            foreach (string bsName in _blendshapeNames)
            {
                for (int j = 0; j < mTarget.vertexCount; j++)
                {
                    Vector3 p = mTarget.vertices[j];
                    QuickTriangleMesh t = closestTriangles[j];

                    if (t != null && _blendshapeMap[t._mesh].ContainsKey(bsName))
                    {
                        Mesh mSource = t._mesh;

                        BlendshapeData bsData = _blendshapeMap[mSource][bsName];

                        Vector3 q = t.GetClosestPoint(t.ComputeProjection(p));
                        Vector3 bCoords = t.ComputeBarycentricCoordinates(q);

                        dVerticesTarget[j] = bsData._dVertices[t._vertexID0] * bCoords.x + bsData._dVertices[t._vertexID1] * bCoords.y + bsData._dVertices[t._vertexID2] * bCoords.z;
                        dNormalsTarget[j] = bsData._dNormals[t._vertexID0] * bCoords.x + bsData._dNormals[t._vertexID1] * bCoords.y + bsData._dNormals[t._vertexID2] * bCoords.z;
                        dTangentsTarget[j] = bsData._dTangents[t._vertexID0] * bCoords.x + bsData._dTangents[t._vertexID1] * bCoords.y + bsData._dTangents[t._vertexID2] * bCoords.z;
                    }
                }

                mTarget.AddBlendShapeFrame(bsName, 100, dVerticesTarget, dNormalsTarget, dTangentsTarget);
            }
        }

        #endregion

        #region FBX EXPORT

        protected virtual GameObject Export(string folderName, GameObject goSimplified)
        {
            //3) Export the GO into a fbx. 
            string assetFolderPath = QuickLODUtilsEditor.CreateAssetFolder("Assets/QuickLOD/" + folderName);
            foreach (Renderer r in goSimplified.GetComponentsInChildren<Renderer>())
            {
                foreach (string tName in r.sharedMaterial.GetTexturePropertyNames())
                {
                    Texture t = r.sharedMaterial.GetTexture(tName);
                    if (t != null)
                    {
                        AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(t), assetFolderPath + "/" + t.name + ".png");
                        AssetDatabase.Refresh();
                    }
                }
            }

            string fileName = goSimplified.name;
            ExportBinaryFBX(QuickLODUtils.projectPath + "/" + assetFolderPath + "/" + fileName + ".fbx", goSimplified);

            //5) Modify the needed import options for the asset
            string assetPath = assetFolderPath + "/" + fileName;
            ModelImporter importer = (ModelImporter)ModelImporter.GetAtPath(assetPath + ".fbx");
            importer.importBlendShapeNormals = ModelImporterNormals.Import;

            Animator animator = goSimplified.GetComponent<Animator>();
            if (animator && animator.isHuman)
            {
                importer.animationType = ModelImporterAnimationType.Human;
            }
            importer.SaveAndReimport();

            //6) Import the asset into the scene create a prefab for that asset. 
            GameObject go = UnityEngine.Object.Instantiate((GameObject)AssetDatabase.LoadAssetAtPath(assetPath + ".fbx", typeof(GameObject)));
            go.name = fileName;
            go.transform.position = goSimplified.transform.position;
            go.transform.rotation = goSimplified.transform.rotation;
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, assetPath + ".prefab", InteractionMode.AutomatedAction);

            return go;
        }

        protected virtual void ExportBinaryFBX(string filePath, UnityEngine.Object singleObject)
        {
            //https://forum.unity.com/threads/fbx-exporter-binary-export-doesnt-work-via-editor-scripting.1114222/

            // Find relevant internal types in Unity.Formats.Fbx.Editor assembly
            Type[] types = AppDomain.CurrentDomain.GetAssemblies().First(x => x.FullName == "Unity.Formats.Fbx.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").GetTypes();
            Type optionsInterfaceType = types.First(x => x.Name == "IExportOptions");
            Type optionsType = types.First(x => x.Name == "ExportOptionsSettingsSerializeBase");

            // Instantiate a settings object instance
            MethodInfo optionsProperty = typeof(ModelExporter).GetProperty("DefaultOptions", BindingFlags.Static | BindingFlags.NonPublic).GetGetMethod(true);
            object optionsInstance = optionsProperty.Invoke(null, null);

            // Change the export setting from ASCII to binary
            FieldInfo exportFormatField = optionsType.GetField("exportFormat", BindingFlags.Instance | BindingFlags.NonPublic);
            exportFormatField.SetValue(optionsInstance, 1);

            // Invoke the ExportObject method with the settings param
            MethodInfo exportObjectMethod = typeof(ModelExporter).GetMethod("ExportObject", BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder, new Type[] { typeof(string), typeof(UnityEngine.Object), optionsInterfaceType }, null);
            exportObjectMethod.Invoke(null, new object[] { filePath, singleObject, optionsInstance });
        }

        #endregion

    }

}

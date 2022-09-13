using System.Collections.Generic;

using Simplygon;
using Simplygon.Unity.EditorPlugin;

using UnityEngine;

namespace QuickVR.QuickLOD
{

    [System.Serializable]
    public class QuickLOD : QuickLODBase
    {

        #region PUBLIC ATTRIBUTES

        public static QuickLOD _instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new QuickLOD();
                }

                return m_Instance;
            }
        }
        private static QuickLOD m_Instance = null;

        #endregion

        #region PROTECTED ATTRIBUTES

        protected List<QuickTriangleTextureMap> _triangleTextureMaps = null;

        #endregion

        #region GET AND SET

        //public virtual void TestComputeTriangleTextureMap(Renderer r)
        //{
        //    using (ISimplygon simplygon = global::Simplygon.Loader.InitSimplygon
        //    (out EErrorCodes simplygonErrorCode, out string simplygonErrorMessage))
        //    {
        //        if (simplygonErrorCode == Simplygon.EErrorCodes.NoError)
        //        {
        //            float timeStart = Time.realtimeSinceStartup;

        //            Mesh m = r.GetMesh();

        //            Color[] colors = new Color[m.vertexCount];
        //            for (int i = 0; i < m.vertexCount; i++)
        //            {
        //                colors[i] = new Color(m.uv[i].x, m.uv[i].y, 0);
        //            }
        //            m.colors = colors;

        //            Mesh tmp = GenerateUV1(simplygon, r, 2048).GetComponent<Renderer>().GetMesh();
        //            Vector2[] uv = new Vector2[tmp.vertexCount];
        //            Vector2[] uv2 = new Vector2[tmp.vertexCount];
        //            for (int i = 0; i < tmp.vertexCount; i++)
        //            {
        //                uv[i] = new Vector2(tmp.colors[i].r, tmp.colors[i].g);
        //                uv2[i] = tmp.uv[i];
        //            }
        //            tmp.uv = uv;
        //            tmp.uv2 = uv2;

        //            int[] vertexMap = CreateVertexMap(m, tmp);
        //            BoneWeight[] boneWeights = new BoneWeight[tmp.vertexCount];
        //            for (int j = 0; j < tmp.vertexCount; j++)
        //            {
        //                BoneWeight bWeight = new BoneWeight();

        //                int vSourceID = vertexMap[j];
        //                if (vSourceID != -1)
        //                {
        //                    bWeight = m.boneWeights[vSourceID];
        //                }
        //                else
        //                {
        //                    bWeight.boneIndex0 = 0;
        //                    bWeight.weight0 = 1;
        //                }

        //                boneWeights[j] = bWeight;
        //            }

        //            tmp.boneWeights = boneWeights;
        //            tmp.bindposes = m.bindposes;
        //            r.SetMesh(tmp);

        //            QuickTriangleTextureMap tMap = new QuickTriangleTextureMap();
        //            tMap.ComputeTriangleTextureMap(r.GetMesh());

        //            Debug.Log("timeComputeTriangleTextureMap = " + (Time.realtimeSinceStartup - timeStart).ToString("f3"));

        //        }
        //    }
        //}

        //protected virtual Texture2D ComputeTriangleTextureMap(Mesh mSource)
        //{
        //    Texture2D result = null;

        //    if (mSource != null)
        //    {
        //        int resolution = 2048;
        //        _newRegionColor = Vector3Int.zero;
        //        RenderTextureDescriptor desc = new RenderTextureDescriptor(resolution, resolution, RenderTextureFormat.ARGB32, 0);
        //        desc.sRGB = false;
        //        RenderTexture renderTexture = RenderTexture.GetTemporary(desc);
        //        RenderTexture currentTexture = RenderTexture.active;
        //        RenderTexture.active = renderTexture;
        //        _materialBake.SetPass(0);

        //        GL.Clear(false, true, Color.black, 1.0f);

        //        HashSet<Vector2Int> test = new HashSet<Vector2Int>();

        //        for (int i = 0; i < mSource.triangles.Length; i += 3)
        //        {
        //            int v0ID = mSource.triangles[i];
        //            int v1ID = mSource.triangles[i + 1];
        //            int v2ID = mSource.triangles[i + 2];

        //            Vector3 c = (mSource.vertices[v0ID] + mSource.vertices[v1ID] + mSource.vertices[v2ID]) / 3.0f;
        //            QuickTriangle t = QuickTriangle.Create(mSource.vertices[v0ID], mSource.vertices[v1ID], mSource.vertices[v2ID]);
        //            Vector3 bCoords = t.ComputeBarycentricCoordinates(c);
        //            Vector2 uv = mSource.uv[v0ID] * bCoords.x + mSource.uv[v1ID] * bCoords.y + mSource.uv[v2ID] * bCoords.z;
        //            Vector2Int pixelCoords = new Vector2Int(Mathf.RoundToInt(resolution * uv.x), Mathf.RoundToInt(resolution * uv.y));
        //            if (test.Contains(pixelCoords))
        //            {
        //                Debug.Log("PIXEL COORDS COLLISION!!!");
        //            }
        //            else
        //            {
        //                test.Add(pixelCoords);
        //            }

        //            Color32 triangleColor = ComputeNewRegionColor();
        //            Mesh m = new Mesh();
        //            m.vertices = new Vector3[] { mSource.vertices[v0ID], mSource.vertices[v1ID], mSource.vertices[v2ID] };
        //            m.normals = new Vector3[] { mSource.normals[v0ID], mSource.normals[v1ID], mSource.normals[v2ID] };
        //            m.uv = new Vector2[] { mSource.uv[v0ID], mSource.uv[v1ID], mSource.uv[v2ID] };
        //            m.colors32 = new Color32[] { triangleColor, triangleColor, triangleColor };
        //            m.triangles = new int[] { 0, 1, 2 };

        //            Graphics.DrawMeshNow(m, Vector3.zero, Quaternion.identity);
        //        }

        //        //SaveTexture(renderTexture, "VertexColors");

        //        // create a second render target 
        //        RenderTexture rt2 = RenderTexture.GetTemporary(desc);
        //        // use the dilate shader on our first render target, output to rt2
        //        for (int i = 0; i < 10; i++)
        //        {
        //            Graphics.Blit(renderTexture, rt2, _materialDilate);
        //            Graphics.Blit(rt2, renderTexture);
        //        }

        //        //SaveTexture(rt2, "VertexColorsDilated");
        //        result = ToTexture2D(rt2);

        //        RenderTexture.active = currentTexture;

        //        RenderTexture.ReleaseTemporary(renderTexture);
        //        RenderTexture.ReleaseTemporary(rt2);

        //    }

        //    return result;
        //}

        protected virtual void ComputeVertexToTriangleMap(Renderer r, int meshID)
        {
            Mesh m = r.GetMesh();
            List<Color> colors = new List<Color>();
            float mID = meshID / 255.0f;

            for (int i = 0; i < m.vertexCount; i++)
            {
                colors.Add(new Color(m.uv[i].x, m.uv[i].y, mID));
            }

            m.colors = colors.ToArray();
        }

        protected override QuickTriangleMesh GetClosestTriangle(VertexData vData, Mesh[] mSources, bool print = false)
        {
            //The UV and Mesh ID is codified in the color component of the vertex. 
            int meshID = Mathf.RoundToInt(vData._color.b * 255.0f);

            //Debug.Log(vData._color.ToString("f3"));
            //Debug.Log("meshID = " + meshID);

            QuickTriangleTextureMap triangleMap = _triangleTextureMaps[meshID];
            triangleMap.Sample(vData._color.r, vData._color.g, out List<Color> triangleColors);

            QuickTriangleMesh result = null;
            Mesh m = mSources[meshID];
            foreach (Color tColor in triangleColors)
            {
                int tID = ToTriangleID(tColor);
                if (tID != -1)
                {
                    QuickTriangleMesh tmp = QuickTriangleMesh.Create(m.triangles[tID * 3 + 0], m.triangles[tID * 3 + 1], m.triangles[tID * 3 + 2], m);
                    if (result == null || tmp.Distance2(vData._position) < result.Distance2(vData._position))
                    {
                        result = tmp;
                    }
                }
            }

            return result;
        }

        protected virtual int ToTriangleID(Color tColor)
        {
            int r = Mathf.RoundToInt(tColor.r * 255);
            int g = Mathf.RoundToInt(tColor.g * 255);
            int b = Mathf.RoundToInt(tColor.b * 255);

            return (r * 256 * 256 + g * 256 + b) - 1;
        }

        #endregion

        #region UPDATE

        protected override void InitRenderGroupData(RenderGroup rGroup)
        {
            _triangleTextureMaps = new List<QuickTriangleTextureMap>();
        }

        protected override GameObject Simplify(ISimplygon simplygon, Renderer r, int renderID, float reductionFactor)
        {
            QuickTriangleTextureMap tMap = new QuickTriangleTextureMap();
            tMap.ComputeTriangleTextureMap(r.GetMesh());

            //SaveTexture(tMap, "TriangleMap_" + i.ToString());
            _triangleTextureMaps.Add(tMap);

            GameObject sGO = base.Simplify(simplygon, r, renderID, reductionFactor);
            ComputeVertexToTriangleMap(sGO.GetComponent<Renderer>(), renderID);

            return sGO;
        }

        public override void TransferBlendShapes(SkinnedMeshRenderer rSource, SkinnedMeshRenderer rTarget)
        {
            _triangleTextureMaps = new List<QuickTriangleTextureMap>();

            QuickTriangleTextureMap tMap = new QuickTriangleTextureMap();
            tMap.ComputeTriangleTextureMap(rSource.GetMesh());

            //SaveTexture(tMap, "TriangleMap_" + i.ToString());
            _triangleTextureMaps.Add(tMap);

            ComputeVertexToTriangleMap(rTarget, 0);

            base.TransferBlendShapes(rSource, rTarget);
        }

        //protected override GameObject Simplify(ISimplygon simplygon, Renderer r, int renderID, float reductionFactor)
        //{
        //    Mesh m = r.GetMesh();

        //    Color[] colors = new Color[m.vertexCount];
        //    for (int i = 0; i < m.vertexCount; i++)
        //    {
        //        colors[i] = new Color(m.uv[i].x, m.uv[i].y, 0);
        //    }
        //    m.colors = colors;

        //    Mesh tmp = GenerateUV1(simplygon, r, 2048).GetComponent<Renderer>().GetMesh();
        //    Vector2[] uv = new Vector2[tmp.vertexCount];
        //    Vector2[] uv2 = new Vector2[tmp.vertexCount];
        //    for (int i = 0; i < tmp.vertexCount; i++)
        //    {
        //        uv[i] = new Vector2(tmp.colors[i].r, tmp.colors[i].g);
        //        uv2[i] = tmp.uv[i];
        //    }
        //    tmp.uv = uv;
        //    tmp.uv2 = uv2;

        //    int[] vertexMap = CreateVertexMap(m, tmp);
        //    BoneWeight[] boneWeights = new BoneWeight[tmp.vertexCount];
        //    for (int j = 0; j < tmp.vertexCount; j++)
        //    {
        //        BoneWeight bWeight = new BoneWeight();

        //        int vSourceID = vertexMap[j];
        //        if (vSourceID != -1)
        //        {
        //            bWeight = m.boneWeights[vSourceID];
        //        }
        //        else
        //        {
        //            bWeight.boneIndex0 = 0;
        //            bWeight.weight0 = 1;
        //        }

        //        boneWeights[j] = bWeight;
        //    }

        //    tmp.boneWeights = boneWeights;
        //    tmp.bindposes = m.bindposes;
        //    r.SetMesh(tmp);

        //    QuickTriangleTextureMap tMap = new QuickTriangleTextureMap();
        //    tMap.ComputeTriangleTextureMap(r.GetMesh());

        //    //SaveTexture(tMap, "TriangleMap_" + i.ToString());
        //    _triangleTextureMaps.Add(tMap);

        //    ComputeVertexToTriangleMap(r, renderID);
        //    GameObject sGO = base.Simplify(simplygon, r, renderID, reductionFactor);


        //    //ComputeVertexToTriangleMap(sGO.GetComponent<Renderer>().GetMesh(), renderID);

        //    return sGO;
        //}

        protected GameObject GenerateUV1(ISimplygon simplygon, Renderer r, int atlasResolution)
        {
            GameObject result = null;

            List<GameObject> selectedGameObjects = new List<GameObject>();
            selectedGameObjects.Add(r.gameObject);

            string exportTempDirectory = SimplygonUtils.GetNewTempDirectory();

            using (spScene sgScene = SimplygonExporter.Export(simplygon, exportTempDirectory, selectedGameObjects))
            using (spAggregationPipeline pipeline = simplygon.CreateAggregationPipeline())
            using (spAggregationSettings pipelineSettings = pipeline.GetAggregationSettings())
            {
                //Create a parameterizer object
                //spParameterizer param = simplygon.CreateParameterizer();

                ////Set the properties for the parameterizer
                //param.SetMaxStretch(0.25f);
                //param.SetTextureWidth((uint)atlasResolution);
                //param.SetTextureHeight((uint)atlasResolution);

                //// Parameterize all the objects in the file.
                //for (uint geomId = 0; geomId < sgScene.GetRootNode().GetChildCount(); ++geomId)
                //{
                //    //Cast the node to mesh node, and fetch geometry pointer from it
                //    spGeometryData geom = spSceneMesh.SafeCast(sgScene.GetRootNode().GetChild((int)geomId)).GetGeometry();
                //    Debug.Log("numVerts = " + geom.GetVertexCount());
                //    Debug.Log("numTris = " + geom.GetTriangleCount());

                //    //If the mesh does not have UVs, create them
                //    if (geom.GetTexCoords(0) == null)
                //    {
                //        geom.AddTexCoords(0);
                //    }

                //    param.Parameterize(geom, geom.GetTexCoords(0));
                //    ////Parameterize the selected geometry and input the generated texture coordinates
                //    ////into the first (index 0) UV field.
                //    //if (!param->Parameterize(geom, geom->GetTexCoords(0)))
                //    //    std::cout << "Couldn't parameterize geometry." << std::endl;

                //    spRealArray tCoords = geom.GetTexCoords(0);
                //    Debug.Log("itemCount = " + tCoords.GetItemCount());
                //    Debug.Log("numTuples = " + tCoords.GetTupleCount());
                //    Debug.Log("tupleSize = " + tCoords.GetTupleSize());

                //    Debug.Log("numVertsAfter = " + geom.GetVertexCount());

                //    Debug.Log("numCoords = " + geom.GetCoords().GetItemCount());

                //    Debug.Log("HOLA DON PEPITO!!!");
                //}

                //spPassthroughPipeline test = simplygon.CreatePassthroughPipeline();



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
                sgMappingImageSettings.SetTexCoordGeneratorType(ETexcoordGeneratorType.Parameterizer);

                spMappingImageOutputMaterialSettings sgOutputMaterialSettings = sgMappingImageSettings.GetOutputMaterialSettings(0);
                // Setting the size of the output material for the mapping image. This will be the output size of the 
                // textures when we do material casting in a later stage. 
                sgOutputMaterialSettings.SetTextureWidth((uint)atlasResolution);
                sgOutputMaterialSettings.SetTextureHeight((uint)atlasResolution);

                spParameterizerSettings paramSettings = sgMappingImageSettings.GetParameterizerSettings();
                paramSettings.SetLargeChartsImportance(1);

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

                result = ExecuteSimplygonPipeline(simplygon, pipeline, sgScene, r.gameObject.name);
            }

            return result;
        }

        #endregion

    }

}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tạo mesh cơ thể nữ 3D mượt mà, mềm mại, liền mạch (SkinnedMesh gắn trực tiếp vào bộ xương Skeleton sẵn có)
/// để thay thế dáng mannequin khớp nối robot ("khúc xương") khi mặc các ngoại trang nữ (Nữ Tử Thần, Nữ Valkyrie).
/// </summary>
public static class SmoothFemaleBodyBuilder
{
    static readonly Dictionary<Mesh, Mesh> cachedBySourceMesh = new Dictionary<Mesh, Mesh>();

    public static Mesh GetOrCreate(SkinnedMeshRenderer smr)
    {
        if (smr == null || smr.sharedMesh == null)
            return null;

        Mesh srcMesh = smr.sharedMesh;
        if (cachedBySourceMesh.TryGetValue(srcMesh, out Mesh existing) && existing != null)
            return existing;

        Transform[] bones = smr.bones;
        Matrix4x4[] bindposes = srcMesh.bindposes;
        if (bones == null || bindposes == null || bones.Length == 0 || bindposes.Length != bones.Length)
            return null;

        Mesh built = BuildFromSkeleton(bones, bindposes);
        if (built != null)
        {
            cachedBySourceMesh[srcMesh] = built;
        }
        return built;
    }

    static int FindBone(Transform[] bones, params string[] names)
    {
        for (int n = 0; n < names.Length; n++)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null && bones[i].name == names[n])
                    return i;
            }
        }
        return -1;
    }

    static Vector3 BoneBindPos(Matrix4x4[] bindposes, int idx, Vector3 fallback)
    {
        if (idx < 0 || idx >= bindposes.Length)
            return fallback;
        return bindposes[idx].inverse.MultiplyPoint3x4(Vector3.zero);
    }

    static Mesh BuildFromSkeleton(Transform[] bones, Matrix4x4[] bindposes)
    {
        int iHips = FindBone(bones, "Hips");
        int iSpine = FindBone(bones, "Spine");
        int iChest = FindBone(bones, "Chest");
        int iUpperChest = FindBone(bones, "UpperChest", "Chest");
        int iNeck = FindBone(bones, "Neck");
        int iHead = FindBone(bones, "Head");

        int iLShoulder = FindBone(bones, "Left_Shoulder");
        int iLUpperArm = FindBone(bones, "Left_UpperArm");
        int iLLowerArm = FindBone(bones, "Left_LowerArm");
        int iLHand = FindBone(bones, "Left_Hand");

        int iRShoulder = FindBone(bones, "Right_Shoulder");
        int iRUpperArm = FindBone(bones, "Right_UpperArm");
        int iRLowerArm = FindBone(bones, "Right_LowerArm");
        int iRHand = FindBone(bones, "Right_Hand");

        int iLUpperLeg = FindBone(bones, "Left_UpperLeg");
        int iLLowerLeg = FindBone(bones, "Left_LowerLeg");
        int iLFoot = FindBone(bones, "Left_Foot");

        int iRUpperLeg = FindBone(bones, "Right_UpperLeg");
        int iRLowerLeg = FindBone(bones, "Right_LowerLeg");
        int iRFoot = FindBone(bones, "Right_Foot");

        if (iHips < 0 || iUpperChest < 0 || iHead < 0 || iLUpperLeg < 0 || iRUpperLeg < 0)
            return null;

        if (iSpine < 0) iSpine = iHips;
        if (iChest < 0) iChest = iSpine;
        if (iNeck < 0) iNeck = iUpperChest;
        if (iLShoulder < 0) iLShoulder = iUpperChest;
        if (iRShoulder < 0) iRShoulder = iUpperChest;

        // Lấy tọa độ chính xác 100% của từng khớp trong không gian Bind-Pose của SkinnedMeshRenderer!
        Vector3 pHips = BoneBindPos(bindposes, iHips, new Vector3(0f, 0.981f, -0.016f));
        Vector3 pSpine = BoneBindPos(bindposes, iSpine, pHips + new Vector3(0f, 0.058f, 0.001f));
        Vector3 pChest = BoneBindPos(bindposes, iChest, pSpine + new Vector3(0f, 0.103f, 0f));
        Vector3 pUpperChest = BoneBindPos(bindposes, iUpperChest, pChest + new Vector3(0f, 0.103f, 0f));
        Vector3 pNeck = BoneBindPos(bindposes, iNeck, pUpperChest + new Vector3(0f, 0.251f, -0.015f));
        Vector3 pHead = BoneBindPos(bindposes, iHead, pNeck + new Vector3(0f, 0.127f, 0f));

        Vector3 pLUpperLeg = BoneBindPos(bindposes, iLUpperLeg, pHips + new Vector3(-0.086f, -0.053f, -0.011f));
        Vector3 pLLowerLeg = BoneBindPos(bindposes, iLLowerLeg, pLUpperLeg + new Vector3(0f, -0.413f, 0f));
        Vector3 pLFoot = BoneBindPos(bindposes, iLFoot, pLLowerLeg + new Vector3(0f, -0.414f, 0f));

        Vector3 pRUpperLeg = BoneBindPos(bindposes, iRUpperLeg, pHips + new Vector3(0.086f, -0.053f, -0.011f));
        Vector3 pRLowerLeg = BoneBindPos(bindposes, iRLowerLeg, pRUpperLeg + new Vector3(0f, -0.413f, 0f));
        Vector3 pRFoot = BoneBindPos(bindposes, iRFoot, pRLowerLeg + new Vector3(0f, -0.414f, 0f));

        Vector3 pLUpperArm = BoneBindPos(bindposes, iLUpperArm, pUpperChest + new Vector3(-0.168f, 0.191f, -0.009f));
        Vector3 pLLowerArm = BoneBindPos(bindposes, iLLowerArm, pLUpperArm + new Vector3(-0.26f, -0.06f, 0f));
        Vector3 pLHand = BoneBindPos(bindposes, iLHand, pLLowerArm + new Vector3(-0.22f, -0.04f, 0f));

        Vector3 pRUpperArm = BoneBindPos(bindposes, iRUpperArm, pUpperChest + new Vector3(0.168f, 0.191f, -0.009f));
        Vector3 pRLowerArm = BoneBindPos(bindposes, iRLowerArm, pRUpperArm + new Vector3(0.26f, -0.06f, 0f));
        Vector3 pRHand = BoneBindPos(bindposes, iRHand, pRLowerArm + new Vector3(0.22f, -0.04f, 0f));

        // Hệ trục chuẩn tự suy luận từ bộ xương Bind-Pose (Unity LH: Cross(right, up) = forward)
        Vector3 up = (pHead - pHips).normalized;
        Vector3 right = (pRUpperLeg - pLUpperLeg).normalized;
        right = (right - up * Vector3.Dot(right, up)).normalized;
        Vector3 fwd = Vector3.Cross(right, up).normalized;

        List<Vector3> verts = new List<Vector3>(3200);
        List<Vector2> uvs = new List<Vector2>(3200);
        List<BoneWeight> weights = new List<BoneWeight>(3200);
        List<int> tris = new List<int>(16000);

        // 1. THÂN HÌNH NỮ MỀM MẠI LIỀN MẠCH: Hông -> Eo thắt đáy lưng ong -> Ngực/Vai thon -> Cổ -> Gương mặt thanh tú
        BuildTorsoNeckHead(
            verts, uvs, weights, tris,
            pHips, pSpine, pChest, pUpperChest, pNeck, pHead,
            iHips, iSpine, iChest, iUpperChest, iNeck, iHead,
            right, up, fwd
        );

        // 2. ĐÔI CHÂN DÀI MỀM MẠI LIỀN MẠCH (không khớp nối robot!)
        if (iLLowerLeg >= 0 && iLFoot >= 0)
        {
            BuildSmoothLeg(
                verts, uvs, weights, tris,
                pHips, pLUpperLeg, pLLowerLeg, pLFoot,
                iHips, iLUpperLeg, iLLowerLeg, iLFoot,
                right, up, fwd, -1f
            );
        }
        if (iRLowerLeg >= 0 && iRFoot >= 0)
        {
            BuildSmoothLeg(
                verts, uvs, weights, tris,
                pHips, pRUpperLeg, pRLowerLeg, pRFoot,
                iHips, iRUpperLeg, iRLowerLeg, iRFoot,
                right, up, fwd, +1f
            );
        }

        // 3. ĐÔI TAY MỀM MẠI LIỀN MẠCH (bờ vai tròn mềm -> cánh tay -> cẳng tay -> bàn tay thon!)
        if (iLUpperArm >= 0 && iLLowerArm >= 0 && iLHand >= 0)
        {
            BuildSmoothArm(
                verts, uvs, weights, tris,
                pUpperChest, pLUpperArm, pLLowerArm, pLHand,
                iUpperChest, iLShoulder, iLUpperArm, iLLowerArm, iLHand,
                right, up, fwd, -1f
            );
        }
        if (iRUpperArm >= 0 && iRLowerArm >= 0 && iRHand >= 0)
        {
            BuildSmoothArm(
                verts, uvs, weights, tris,
                pUpperChest, pRUpperArm, pRLowerArm, pRHand,
                iUpperChest, iRShoulder, iRUpperArm, iRLowerArm, iRHand,
                right, up, fwd, +1f
            );
        }

        Mesh mesh = new Mesh();
        mesh.name = "SmoothFemaleOrganicBody";
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.boneWeights = weights.ToArray();
        mesh.bindposes = bindposes;
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    static BoneWeight MakeWeight(int b0, float w0, int b1 = 0, float w1 = 0f, int b2 = 0, float w2 = 0f)
    {
        float sum = w0 + w1 + w2;
        if (sum <= 0.0001f) { w0 = 1f; w1 = 0f; w2 = 0f; sum = 1f; }
        BoneWeight bw = new BoneWeight
        {
            boneIndex0 = b0,
            weight0 = w0 / sum,
            boneIndex1 = b1,
            weight1 = w1 / sum,
            boneIndex2 = b2,
            weight2 = w2 / sum
        };
        return bw;
    }

    static void AddTubeTriangles(List<int> tris, int baseVert, int rings, int segs)
    {
        int stride = segs + 1;
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < segs; s++)
            {
                int a = baseVert + r * stride + s;
                int b = a + 1;
                int c = a + stride;
                int d = c + 1;

                // Outward-facing winding in Unity LH for angle running CCW around +up from +right toward +fwd?
                // Let's check: pos = cos(a)*right + sin(a)*fwd, next ring goes +up.
                // (c - a) = +up, (b - a) = tangent (-sin*right + cos*fwd).
                // Cross(+up, tangent) = Cross(+Y, +Z) = +X (outward at a=0!).
                // In Unity LH, triangle (a, c, b) has normal Cross(c-a, b-a) = OUTWARD!
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        }
    }

    static void BuildTorsoNeckHead(
        List<Vector3> verts, List<Vector2> uvs, List<BoneWeight> weights, List<int> tris,
        Vector3 pHips, Vector3 pSpine, Vector3 pChest, Vector3 pUpperChest, Vector3 pNeck, Vector3 pHead,
        int iHips, int iSpine, int iChest, int iUpperChest, int iNeck, int iHead,
        Vector3 right, Vector3 up, Vector3 fwd
    )
    {
        float yHips = Vector3.Dot(pHips, up);
        float ySpine = Vector3.Dot(pSpine, up);
        float yChest = Vector3.Dot(pChest, up);
        float yUpper = Vector3.Dot(pUpperChest, up);
        float yNeck = Vector3.Dot(pNeck, up);
        float yHead = Vector3.Dot(pHead, up);

        // Định nghĩa các vòng mặt cắt thân nữ mềm mại (y, rx, rz, zOffset, bustAmp, hipCurve, isHeadRegion)
        // Tọa độ y theo mốc tương đối từ Hips lên đỉnh đầu!
        float[][] profile = new float[][]
        {
            // Bottom pelvis cap -> hip fullness
            new float[] { yHips - 0.112f, 0.008f, 0.008f, -0.004f, 0.000f, 0.0f },
            new float[] { yHips - 0.102f, 0.088f, 0.072f, -0.006f, 0.000f, 0.0f },
            new float[] { yHips - 0.072f, 0.132f, 0.098f, -0.010f, 0.000f, 0.0f },
            new float[] { yHips - 0.032f, 0.144f, 0.104f, -0.010f, 0.000f, 0.0f }, // Vòng hông nữ mềm mại
            new float[] { yHips + 0.015f, 0.134f, 0.098f, -0.006f, 0.000f, 0.0f },
            new float[] { ySpine + 0.012f, 0.114f, 0.086f, -0.002f, 0.000f, 0.0f },
            new float[] { ySpine + 0.056f, 0.101f, 0.079f,  0.001f, 0.000f, 0.0f }, // Eo thon thắt đáy lưng ong!
            new float[] { yChest - 0.012f, 0.105f, 0.081f,  0.002f, 0.000f, 0.0f },
            new float[] { yChest + 0.035f, 0.114f, 0.083f,  0.002f, 0.004f, 0.0f },
            new float[] { yUpper - 0.010f, 0.120f, 0.084f,  0.001f, 0.011f, 0.0f }, // Chân ngực mềm mại (gọn trong corset!)
            new float[] { yUpper + 0.038f, 0.124f, 0.085f,  0.001f, 0.018f, 0.0f }, // Khuôn ngực mềm mại (không xuyên áo!)
            new float[] { yUpper + 0.088f, 0.132f, 0.083f,  0.000f, 0.012f, 0.0f },
            new float[] { yUpper + 0.138f, 0.152f, 0.088f, -0.004f, 0.006f, 0.0f }, // Xương quai xanh & bờ vai rộng mềm
            new float[] { yUpper + 0.184f, 0.146f, 0.078f, -0.008f, 0.000f, 0.0f }, // Đỉnh vai tròn mềm
            new float[] { yNeck - 0.032f,  0.090f, 0.064f, -0.010f, 0.000f, 0.0f }, // Đường dốc cổ thanh thoát
            new float[] { yNeck - 0.005f,  0.052f, 0.054f, -0.008f, 0.000f, 0.0f }, // Gốc cổ
            new float[] { yNeck + 0.045f,  0.046f, 0.048f, -0.005f, 0.000f, 0.0f }, // Cổ thon mềm mại
            new float[] { yNeck + 0.090f,  0.048f, 0.052f,  0.000f, 0.000f, 0.0f },
            // Đầu & khuôn mặt nữ thanh tú (isHead = 1)
            new float[] { yHead - 0.020f,  0.056f, 0.068f,  0.012f, 0.000f, 1.0f }, // Cằm thon gọn
            new float[] { yHead + 0.008f,  0.074f, 0.086f,  0.010f, 0.000f, 1.0f }, // Đường hàm mềm
            new float[] { yHead + 0.042f,  0.084f, 0.095f,  0.007f, 0.000f, 1.0f }, // Má & môi
            new float[] { yHead + 0.080f,  0.087f, 0.097f,  0.004f, 0.000f, 1.0f }, // Vùng mắt
            new float[] { yHead + 0.122f,  0.086f, 0.096f,  0.000f, 0.000f, 1.0f }, // Trán
            new float[] { yHead + 0.162f,  0.072f, 0.082f, -0.004f, 0.000f, 1.0f }, // Đỉnh đầu trên
            new float[] { yHead + 0.190f,  0.040f, 0.046f, -0.006f, 0.000f, 1.0f },
            new float[] { yHead + 0.200f,  0.004f, 0.004f, -0.006f, 0.000f, 1.0f }
        };

        const int segs = 28;
        int baseVert = verts.Count;

        Vector3 spineXZ = pHips - up * yHips;

        for (int r = 0; r < profile.Length; r++)
        {
            float y = profile[r][0];
            float rx = profile[r][1];
            float rz = profile[r][2];
            float zOff = profile[r][3];
            float bust = profile[r][4];
            bool isHead = profile[r][5] > 0.5f;

            // Tính center mềm theo đường cong cột sống
            float headT = Mathf.Clamp01((y - yUpper) / Mathf.Max(0.05f, yHead - yUpper));
            Vector3 centerXZ = Vector3.Lerp(spineXZ, pHead - up * yHead, headT * 0.85f);
            Vector3 ringCenter = centerXZ + up * y + fwd * zOff;

            // Trọng số xương chuyển tiếp cực mượt theo chiều cao y
            BoneWeight bw = EvaluateTorsoBoneWeight(y, yHips, ySpine, yChest, yUpper, yNeck, yHead, iHips, iSpine, iChest, iUpperChest, iNeck, iHead);

            for (int s = 0; s <= segs; s++)
            {
                float uFrac = (float)s / segs;
                float ang = uFrac * Mathf.PI * 2f; // 0 = +right, pi/2 = +fwd, pi = -right, 3pi/2 = -fwd
                float ca = Mathf.Cos(ang);
                float sa = Mathf.Sin(ang);

                float localX = ca * rx;
                float localZ = sa * rz;

                // Tạo đường cong mềm mại cho khuôn ngực phía trước (sa > 0) và đường cong lưng nữ nhẹ (sa < 0)
                if (bust > 0.001f && sa > 0f)
                {
                    float lobeL = Mathf.Exp(-Mathf.Pow((ca + 0.42f) / 0.38f, 2f));
                    float lobeR = Mathf.Exp(-Mathf.Pow((ca - 0.42f) / 0.38f, 2f));
                    float bustShape = Mathf.Clamp01(lobeL + lobeR) * Mathf.Pow(sa, 1.35f);
                    localZ += bust * bustShape;
                }
                else if (!isHead && sa < -0.2f && y < yChest && y > yHips - 0.08f)
                {
                    // Đường cong mông mềm mại phía sau hông (y ~ yHips - 0.04)
                    float gluteT = Mathf.Exp(-Mathf.Pow((y - (yHips - 0.042f)) / 0.055f, 2f));
                    localZ -= 0.024f * gluteT * Mathf.Pow(-sa, 1.4f);
                }

                // Tạo mũi nhẹ & đường cằm V-line thanh tú cho khuôn mặt nữ
                if (isHead && sa > 0.45f)
                {
                    float faceCenter = Mathf.Exp(-Mathf.Pow(ca / 0.22f, 2f));
                    float noseY = Mathf.Exp(-Mathf.Pow((y - (yHead + 0.058f)) / 0.022f, 2f));
                    localZ += 0.011f * faceCenter * noseY;
                }

                Vector3 pos = ringCenter + right * localX + fwd * localZ;
                verts.Add(pos);
                weights.Add(bw);

                // Map UV: mặt trước đầu dùng nửa phải [0.50..1.00] để vẽ mắt/môi/má hồng tinh tế; phần thân dùng nửa trái [0.02..0.46]
                if (isHead && sa > 0.15f)
                {
                    // Front face projection: ca in [-0.95..+0.95] -> u in [0.52..0.98]
                    float fu = 0.75f + Mathf.Clamp(ca, -0.95f, 0.95f) * 0.24f;
                    float fv = Mathf.Clamp01((y - (yHead - 0.025f)) / 0.185f);
                    uvs.Add(new Vector2(fu, fv));
                }
                else
                {
                    float bu = 0.04f + 0.40f * uFrac;
                    float bv = Mathf.Clamp01((y - (yHips - 0.11f)) / 0.95f);
                    uvs.Add(new Vector2(bu, bv));
                }
            }
        }

        AddTubeTriangles(tris, baseVert, profile.Length - 1, segs);
    }

    static BoneWeight EvaluateTorsoBoneWeight(
        float y,
        float yHips, float ySpine, float yChest, float yUpper, float yNeck, float yHead,
        int iHips, int iSpine, int iChest, int iUpperChest, int iNeck, int iHead
    )
    {
        if (y <= yHips)
            return MakeWeight(iHips, 1f);
        if (y <= ySpine)
        {
            float t = Mathf.SmoothStep(0f, 1f, (y - yHips) / Mathf.Max(0.01f, ySpine - yHips));
            return MakeWeight(iHips, 1f - t, iSpine, t);
        }
        if (y <= yChest)
        {
            float t = Mathf.SmoothStep(0f, 1f, (y - ySpine) / Mathf.Max(0.01f, yChest - ySpine));
            return MakeWeight(iSpine, 1f - t, iChest, t);
        }
        if (y <= yUpper)
        {
            float t = Mathf.SmoothStep(0f, 1f, (y - yChest) / Mathf.Max(0.01f, yUpper - yChest));
            return MakeWeight(iChest, 1f - t, iUpperChest, t);
        }
        if (y <= yNeck - 0.02f)
            return MakeWeight(iUpperChest, 1f);
        if (y <= yNeck + 0.045f)
        {
            float t = Mathf.SmoothStep(0f, 1f, (y - (yNeck - 0.02f)) / 0.065f);
            return MakeWeight(iUpperChest, 1f - t, iNeck, t);
        }
        if (y <= yHead - 0.015f)
        {
            float t = Mathf.SmoothStep(0f, 1f, (y - (yNeck + 0.045f)) / Mathf.Max(0.01f, (yHead - 0.015f) - (yNeck + 0.045f)));
            return MakeWeight(iNeck, 1f - t, iHead, t);
        }
        return MakeWeight(iHead, 1f);
    }

    static void BuildSmoothLeg(
        List<Vector3> verts, List<Vector2> uvs, List<BoneWeight> weights, List<int> tris,
        Vector3 pHips, Vector3 pUpper, Vector3 pKnee, Vector3 pFoot,
        int iHips, int iUpperLeg, int iLowerLeg, int iFoot,
        Vector3 right, Vector3 up, Vector3 fwd, float sideSign
    )
    {
        // Một ống chân mềm mại LIỀN MẠCH từ trong hông -> đùi thon đầy đặn -> đầu gối êm -> bắp chân thon -> cổ chân mảnh -> bàn chân!
        const int segs = 22;
        const int rings = 30;
        int baseVert = verts.Count;

        Vector3 thighDir = (pKnee - pUpper).normalized;
        Vector3 shinDir = (pFoot - pKnee).normalized;
        Vector3 topStart = pUpper - thighDir * 0.065f; // ăn sâu nhẹ vào hông để khi chạy không bao giờ hở khe!
        Vector3 soleEnd = pFoot + shinDir * 0.088f;

        // Xác định khung trực chuẩn ổn định cho chân (với trục dọc đi TỪ DƯỚI LÊN TRÊN để winding hướng ra ngoài chuẩn!)
        for (int r = 0; r <= rings; r++)
        {
            // r=0 ở bàn chân (dưới) -> r=rings ở đỉnh hông (trên) để chiều +ring cùng chiều +up -> mặt ngoài hướng chuẩn!
            float tFromTop = 1f - (float)r / rings; // 0 = đỉnh đùi trong hông, 0.50 = đầu gối, 0.92 = cổ chân, 1.0 = lòng bàn chân

            Vector3 center;
            Vector3 segUp;
            float rx, rz;
            float zShift = 0f;
            float xShift = 0f;
            BoneWeight bw;

            if (tFromTop <= 0.50f)
            {
                float lt = tFromTop / 0.50f;
                center = Vector3.Lerp(topStart, pKnee, lt);
                segUp = -thighDir;

                // Đùi nữ mềm mại đầy đặn ở trên, thon dần mượt về đầu gối (KHÔNG bị gồ khớp robot!)
                float thighProfile = Mathf.Lerp(0.080f, 0.053f, Mathf.Pow(lt, 0.88f));
                float softRound = Mathf.Sin(lt * Mathf.PI) * 0.008f;
                rx = thighProfile + softRound * 0.65f;
                rz = (thighProfile + softRound) * 1.06f;

                // Giữ khe đùi trong thanh thoát để 2 đùi không chạm cắt nhau
                xShift = sideSign * Mathf.Sin(lt * Mathf.PI) * 0.004f;
                // Đầu gối cong nhẹ tự nhiên phía trước
                zShift = Mathf.Exp(-Mathf.Pow((lt - 0.96f) / 0.14f, 2f)) * 0.006f;

                if (lt < 0.22f)
                {
                    float blend = Mathf.SmoothStep(0f, 1f, lt / 0.22f);
                    bw = MakeWeight(iHips, (1f - blend) * 0.72f, iUpperLeg, 0.28f + blend * 0.72f);
                }
                else if (lt > 0.84f)
                {
                    float blend = Mathf.SmoothStep(0f, 1f, (lt - 0.84f) / 0.16f) * 0.45f;
                    bw = MakeWeight(iUpperLeg, 1f - blend, iLowerLeg, blend);
                }
                else
                {
                    bw = MakeWeight(iUpperLeg, 1f);
                }
            }
            else if (tFromTop <= 0.92f)
            {
                float lt = (tFromTop - 0.50f) / 0.42f; // 0 = đầu gối -> 1 = cổ chân
                center = Vector3.Lerp(pKnee, pFoot, lt);
                segUp = -shinDir;

                // Bắp chân nữ thon mượt: hơi đầy nhẹ ở 1/3 trên bắp chân rồi thuôn mảnh dần xuống cổ chân!
                float calfBulge = Mathf.Exp(-Mathf.Pow((lt - 0.30f) / 0.22f, 2f));
                float baseR = Mathf.Lerp(0.052f, 0.035f, Mathf.Pow(lt, 0.85f));
                rx = baseR + calfBulge * 0.008f;
                rz = baseR * 1.04f + calfBulge * 0.012f;
                zShift = -calfBulge * 0.009f; // Độ cong bắp chân phía sau tự nhiên

                if (lt < 0.16f)
                {
                    float blend = Mathf.SmoothStep(0f, 1f, lt / 0.16f);
                    bw = MakeWeight(iUpperLeg, (1f - blend) * 0.45f, iLowerLeg, 0.55f + blend * 0.45f);
                }
                else if (lt > 0.84f)
                {
                    float blend = Mathf.SmoothStep(0f, 1f, (lt - 0.84f) / 0.16f) * 0.50f;
                    bw = MakeWeight(iLowerLeg, 1f - blend, iFoot, blend);
                }
                else
                {
                    bw = MakeWeight(iLowerLeg, 1f);
                }
            }
            else
            {
                float lt = (tFromTop - 0.92f) / 0.08f; // 0 = cổ chân -> 1 = đế bàn chân
                center = Vector3.Lerp(pFoot, soleEnd, lt);
                segUp = -shinDir;

                float footCap = Mathf.Sin(Mathf.Clamp01(1f - lt * 0.88f) * Mathf.PI * 0.5f);
                rx = Mathf.Max(0.006f, 0.038f * footCap);
                rz = Mathf.Max(0.008f, (0.044f + lt * 0.048f) * footCap);
                zShift = lt * 0.046f;
                bw = MakeWeight(iFoot, 1f);
            }

            Vector3 locRight = (right - segUp * Vector3.Dot(right, segUp)).normalized;
            Vector3 locFwd = Vector3.Cross(locRight, segUp).normalized;
            Vector3 cPos = center + locRight * xShift + locFwd * zShift;

            for (int s = 0; s <= segs; s++)
            {
                float uFrac = (float)s / segs;
                float ang = uFrac * Mathf.PI * 2f;
                Vector3 pos = cPos + locRight * (Mathf.Cos(ang) * rx) + locFwd * (Mathf.Sin(ang) * rz);
                verts.Add(pos);
                weights.Add(bw);
                uvs.Add(new Vector2(0.06f + 0.36f * uFrac, Mathf.Clamp01(1f - tFromTop)));
            }
        }

        AddTubeTriangles(tris, baseVert, rings, segs);
    }

    static void BuildSmoothArm(
        List<Vector3> verts, List<Vector2> uvs, List<BoneWeight> weights, List<int> tris,
        Vector3 pUpperChest, Vector3 pShoulderOut, Vector3 pElbow, Vector3 pHand,
        int iUpperChest, int iShoulder, int iUpperArm, int iLowerArm, int iHand,
        Vector3 right, Vector3 up, Vector3 fwd, float sideSign
    )
    {
        const int segs = 18;
        const int rings = 24;
        int baseVert = verts.Count;

        Vector3 upperDir = (pElbow - pShoulderOut).normalized;
        Vector3 lowerDir = (pHand - pElbow).normalized;
        Vector3 deltoidStart = pShoulderOut - upperDir * 0.055f; // ăn vào thân vai tạo bờ vai tròn mềm liền mạch!
        Vector3 handTip = pHand + lowerDir * 0.115f;

        for (int r = 0; r <= rings; r++)
        {
            // Để winding luôn hướng ra ngoài: r tăng theo hướng segAxis = -armDir (từ bàn tay về vai),
            // sau đó locFwd = Cross(locSide, segAxis) đúng chiều LH!
            float tFromShoulder = 1f - (float)r / rings; // 0 = bờ vai, 0.45 = khuỷu tay, 0.85 = cổ tay, 1.0 = đầu ngón tay

            Vector3 center;
            Vector3 segAxis;
            float rA, rB;
            BoneWeight bw;

            if (tFromShoulder <= 0.46f)
            {
                float lt = tFromShoulder / 0.46f;
                center = Vector3.Lerp(deltoidStart, pElbow, lt);
                segAxis = -upperDir;

                float deltoid = Mathf.Exp(-Mathf.Pow((lt - 0.14f) / 0.18f, 2f)) * 0.011f;
                float baseR = Mathf.Lerp(0.048f, 0.037f, lt) + deltoid;
                rA = baseR;
                rB = baseR * 0.96f;

                if (lt < 0.20f)
                {
                    float blend = Mathf.SmoothStep(0f, 1f, lt / 0.20f);
                    bw = MakeWeight(iUpperChest, (1f - blend) * 0.55f, iShoulder, (1f - blend) * 0.25f, iUpperArm, 0.20f + blend * 0.80f);
                }
                else if (lt > 0.84f)
                {
                    float blend = Mathf.SmoothStep(0f, 1f, (lt - 0.84f) / 0.16f) * 0.45f;
                    bw = MakeWeight(iUpperArm, 1f - blend, iLowerArm, blend);
                }
                else
                {
                    bw = MakeWeight(iUpperArm, 1f);
                }
            }
            else if (tFromShoulder <= 0.86f)
            {
                float lt = (tFromShoulder - 0.46f) / 0.40f;
                center = Vector3.Lerp(pElbow, pHand, lt);
                segAxis = -lowerDir;

                float forearmFull = Mathf.Exp(-Mathf.Pow((lt - 0.24f) / 0.22f, 2f)) * 0.005f;
                float baseR = Mathf.Lerp(0.036f, 0.025f, lt) + forearmFull;
                rA = baseR;
                rB = baseR * 0.92f;

                if (lt < 0.16f)
                {
                    float blend = Mathf.SmoothStep(0f, 1f, lt / 0.16f);
                    bw = MakeWeight(iUpperArm, (1f - blend) * 0.45f, iLowerArm, 0.55f + blend * 0.45f);
                }
                else if (lt > 0.85f)
                {
                    float blend = Mathf.SmoothStep(0f, 1f, (lt - 0.85f) / 0.15f) * 0.55f;
                    bw = MakeWeight(iLowerArm, 1f - blend, iHand, blend);
                }
                else
                {
                    bw = MakeWeight(iLowerArm, 1f);
                }
            }
            else
            {
                float lt = (tFromShoulder - 0.86f) / 0.14f;
                center = Vector3.Lerp(pHand, handTip, lt);
                segAxis = -lowerDir;

                float handEnv = Mathf.Sin(Mathf.Clamp01(1f - lt * 0.90f) * Mathf.PI * 0.5f);
                rA = Mathf.Max(0.004f, 0.032f * handEnv);
                rB = Mathf.Max(0.004f, 0.017f * handEnv);
                bw = MakeWeight(iHand, 1f);
            }

            // Tạo khung trực chuẩn ổn định (tránh gimbal flip dù cánh tay dang ngang hay thả xuôi)
            Vector3 refVec = (Mathf.Abs(Vector3.Dot(segAxis, fwd)) < 0.85f) ? fwd : up;
            Vector3 locA = Vector3.Cross(segAxis, refVec).normalized;
            Vector3 locB = Vector3.Cross(locA, segAxis).normalized;

            for (int s = 0; s <= segs; s++)
            {
                float uFrac = (float)s / segs;
                float ang = uFrac * Mathf.PI * 2f;
                Vector3 pos = center + locA * (Mathf.Cos(ang) * rA) + locB * (Mathf.Sin(ang) * rB);
                verts.Add(pos);
                weights.Add(bw);
                uvs.Add(new Vector2(0.06f + 0.36f * uFrac, Mathf.Clamp01(1f - tFromShoulder)));
            }
        }

        AddTubeTriangles(tris, baseVert, rings, segs);
    }

    /// <summary>
    /// Tạo texture da mịn trắng hồng tự nhiên (nửa trái) + khuôn mặt nữ thanh tú mềm mại (nửa phải)
    /// để dưới mũ trùm Nữ Tử Thần hoặc tóc Nữ Valkyrie trông dịu dàng, nữ tính và tự nhiên!
    /// </summary>
    public static Texture2D CreateSoftFemaleSkinTexture(Color skinTone, Color eyeColor)
    {
        const int w = 256;
        const int h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color warmBlush = Color.Lerp(skinTone, new Color(0.96f, 0.64f, 0.66f, 1f), 0.36f);
        Color softShade = Color.Lerp(skinTone, new Color(0.76f, 0.56f, 0.58f, 1f), 0.24f);
        Color lipColor = Color.Lerp(skinTone, new Color(0.86f, 0.36f, 0.44f, 1f), 0.68f);
        Color lashDark = new Color(0.14f, 0.09f, 0.16f, 1f);
        Color sclera = new Color(0.98f, 0.97f, 0.99f, 1f);

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / (w - 1);
                Color c = skinTone;

                if (u < 0.50f)
                {
                    // Phần thân & đôi chân: da trắng mịn có đổ bóng ấm cực nhẹ cho cảm giác mềm mại tự nhiên
                    float bodySoft = Mathf.Sin(v * Mathf.PI) * 0.04f;
                    c = Color.Lerp(softShade, skinTone, 0.82f + bodySoft);
                    // Điểm ửng hồng cực nhẹ quanh vùng gối / vai
                    float kneeRose = Mathf.Exp(-Mathf.Pow((v - 0.50f) / 0.09f, 2f)) * 0.10f;
                    c = Color.Lerp(c, warmBlush, kneeRose);
                }
                else
                {
                    // Phần mặt (u trong [0.50..1.00], tâm mặt u=0.75, v=0..1 từ cằm lên trán)
                    float fx = (u - 0.75f) / 0.25f; // -1..+1
                    float fy = v;                   // 0 = cằm, 0.24 = môi, 0.44 = mũi, 0.56 = mắt, 0.67 = chân mày

                    // Ửng hồng gò má dịu nhẹ
                    float cheekL = Mathf.Exp(-(Mathf.Pow((fx + 0.38f) / 0.22f, 2f) + Mathf.Pow((fy - 0.40f) / 0.09f, 2f)));
                    float cheekR = Mathf.Exp(-(Mathf.Pow((fx - 0.38f) / 0.22f, 2f) + Mathf.Pow((fy - 0.40f) / 0.09f, 2f)));
                    c = Color.Lerp(c, warmBlush, Mathf.Clamp01(cheekL + cheekR) * 0.42f);

                    // Đôi môi nhỏ mềm mại hồng tự nhiên
                    float mouthDx = fx / 0.145f;
                    float mouthDy = (fy - 0.225f) / 0.036f;
                    float lipMask = Mathf.Exp(-(mouthDx * mouthDx + mouthDy * mouthDy) * 1.8f);
                    c = Color.Lerp(c, lipColor, Mathf.Clamp01(lipMask) * 0.85f);

                    // Đôi mắt mềm mại thanh tú (đối xứng trái/phải)
                    float absFx = Mathf.Abs(fx);
                    float eyeX = (absFx - 0.29f) / 0.125f;
                    float eyeY = (fy - 0.54f) / 0.068f;
                    float eyeDist2 = eyeX * eyeX + eyeY * eyeY;

                    if (eyeDist2 < 1.0f)
                    {
                        c = Color.Lerp(c, sclera, 0.92f);
                        float irisX = (absFx - 0.285f) / 0.072f;
                        float irisY = (fy - 0.535f) / 0.056f;
                        float irisD2 = irisX * irisX + irisY * irisY;
                        if (irisD2 < 1.0f)
                        {
                            float irisGrad = Mathf.Clamp01((fy - 0.49f) / 0.09f);
                            Color irisCol = Color.Lerp(eyeColor, Color.Lerp(eyeColor, Color.black, 0.45f), irisGrad);
                            c = irisCol;
                            // Đồng tử & đốm sáng long lanh trong mắt
                            if (irisD2 < 0.28f) c = Color.Lerp(c, lashDark, 0.78f);
                            float specX = (absFx - 0.255f) / 0.025f;
                            float specY = (fy - 0.560f) / 0.022f;
                            if (specX * specX + specY * specY < 1.0f) c = Color.white;
                        }
                    }

                    // Hàng mi cong mảnh phía trên mắt
                    float lashY = 0.602f - 0.024f * eyeX * eyeX;
                    if (Mathf.Abs(eyeX) < 1.15f && Mathf.Abs(fy - lashY) < 0.017f)
                    {
                        c = Color.Lerp(c, lashDark, 0.88f);
                    }

                    // Chân mày mảnh thanh thoát
                    float browY = 0.685f - 0.016f * eyeX * eyeX;
                    if (Mathf.Abs(eyeX) < 1.05f && Mathf.Abs(fy - browY) < 0.011f)
                    {
                        c = Color.Lerp(c, Color.Lerp(lashDark, skinTone, 0.35f), 0.75f);
                    }
                }

                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return tex;
    }
}

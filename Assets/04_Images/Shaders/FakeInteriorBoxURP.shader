Shader "Custom/URP/FakeInteriorBox_UShape_Equirect"
{
    Properties
    {
        _InteriorTex ("Interior EXR / Equirect Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Exposure ("Exposure", Float) = 1.0
        _Alpha ("Alpha", Range(0,1)) = 1.0

        _RoomMin ("Room Min", Vector) = (-0.5, -0.5, 0, 0)
        _RoomMax ("Room Max", Vector) = (0.5, 0.5, 1, 0)

        _EdgeFade ("Edge Fade", Range(0,0.2)) = 0.02
        _DepthBias ("Depth Bias", Float) = 0.001

        _MissColor ("Miss Color", Color) = (0,0,0,1)
        _EdgeDarkness ("Edge Darkness", Range(0,1)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "FakeInterior"
            Tags { "LightMode"="UniversalForward" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_InteriorTex);
            SAMPLER(sampler_InteriorTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Exposure;
                float _Alpha;

                float4 _RoomMin;
                float4 _RoomMax;

                float _EdgeFade;
                float _DepthBias;

                float4 _MissColor;
                float _EdgeDarkness;

                float4x4 _WorldToRoom;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;

                return OUT;
            }

            float3 SafeRayDir(float3 d)
            {
                if (abs(d.x) < 1e-5) d.x = d.x < 0.0 ? -1e-5 : 1e-5;
                if (abs(d.y) < 1e-5) d.y = d.y < 0.0 ? -1e-5 : 1e-5;
                if (abs(d.z) < 1e-5) d.z = d.z < 0.0 ? -1e-5 : 1e-5;

                return normalize(d);
            }

            bool RayBoxIntersection(
                float3 ro,
                float3 rd,
                float3 boxMin,
                float3 boxMax,
                out float tNear,
                out float tFar
            )
            {
                rd = SafeRayDir(rd);

                float3 invD = 1.0 / rd;

                float3 t0 = (boxMin - ro) * invD;
                float3 t1 = (boxMax - ro) * invD;

                float3 tMin = min(t0, t1);
                float3 tMax = max(t0, t1);

                tNear = max(max(tMin.x, tMin.y), tMin.z);
                tFar  = min(min(tMax.x, tMax.y), tMax.z);

                return tFar >= max(tNear, 0.0);
            }

            float2 DirectionToEquirectUV(float3 dir)
            {
                dir = normalize(dir);

                float u = atan2(dir.x, dir.z) / (2.0 * PI) + 0.5;
                float v = asin(clamp(dir.y, -1.0, 1.0)) / PI + 0.5;

                return float2(u, v);
            }

            float ComputeEdgeFade(float3 p, float3 boxMin, float3 boxMax)
            {
                if (_EdgeFade <= 0.0001)
                {
                    return 1.0;
                }

                float3 dMin = abs(p - boxMin);
                float3 dMax = abs(boxMax - p);

                float edgeDist = min(
                    min(min(dMin.x, dMax.x), min(dMin.y, dMax.y)),
                    min(dMin.z, dMax.z)
                );

                return smoothstep(0.0, _EdgeFade, edgeDist);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 camWS = GetCameraPositionWS();

                float3 camRoom = mul(_WorldToRoom, float4(camWS, 1.0)).xyz;
                float3 posRoom = mul(_WorldToRoom, float4(IN.positionWS, 1.0)).xyz;

                float3 rayDir = normalize(posRoom - camRoom);

                float3 boxMin = _RoomMin.xyz;
                float3 boxMax = _RoomMax.xyz;

                float tNear;
                float tFar;

                bool hit = RayBoxIntersection(camRoom, rayDir, boxMin, boxMax, tNear, tFar);

                if (!hit)
                {
                    return half4(_MissColor.rgb, 1.0);
                }

                float t = max(tFar - _DepthBias, 0.0);
                float3 hitRoom = camRoom + rayDir * t;

                float3 roomCenter = (boxMin + boxMax) * 0.5;
                float3 sampleDir = normalize(hitRoom - roomCenter);

                float2 uv = DirectionToEquirectUV(sampleDir);
                half4 col = SAMPLE_TEXTURE2D(_InteriorTex, sampler_InteriorTex, uv);

                float edgeFade = ComputeEdgeFade(hitRoom, boxMin, boxMax);

                col.rgb *= _Tint.rgb * _Exposure;

                // EdgeFade를 알파가 아니라 색 보정에 사용
                col.rgb = lerp(col.rgb * (1.0 - _EdgeDarkness), col.rgb, edgeFade);

                col.a = 1.0;

                return col;
            }

            ENDHLSL
        }
    }
}
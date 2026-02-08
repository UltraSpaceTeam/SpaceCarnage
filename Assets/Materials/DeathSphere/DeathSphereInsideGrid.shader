Shader "Custom/DeathSphereInsideGrid"
{
    Properties
    {
        _BaseColor ("Base Color (RGBA)", Color) = (0.6, 0.2, 1, 0.10)
        _GridColor ("Grid Color (RGBA)", Color) = (0.8, 0.7, 1, 0.45)

        _GridScale ("Grid Scale", Range(0.2,200)) = 30
        _LineWidth ("Line Width", Range(0.001,0.2)) = 0.03
        _LineSoft ("Line Softness", Range(0.0001,0.2)) = 0.02

        _EdgeStart ("Edge Start (0..1)", Range(0,1)) = 0.65
        _EdgeWidth ("Edge Width", Range(0.001,0.6)) = 0.25
        _Intensity ("Intensity", Range(0,5)) = 1.5

        _DepthFadeStart ("Depth Fade Start", Range(0,2000)) = 50
        _DepthFadeEnd ("Depth Fade End", Range(1,3000)) = 250
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Front
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            fixed4 _GridColor;

            float _GridScale;
            float _LineWidth;
            float _LineSoft;

            float _EdgeStart;
            float _EdgeWidth;
            float _Intensity;

            float _DepthFadeStart;
            float _DepthFadeEnd;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float gridLines(float3 wp, float scale, float width, float soft)
            {
                float3 p = wp * scale;
                float3 f = abs(frac(p) - 0.5);

                float3 d = f / max(soft, 1e-6);

                float lx = 1.0 - smoothstep(width, width + soft, f.x);
                float ly = 1.0 - smoothstep(width, width + soft, f.y);
                float lz = 1.0 - smoothstep(width, width + soft, f.z);

                return saturate(max(lx, max(ly, lz)));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 center = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;
                float3 right = float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20);
                float radius = length(right) * 0.5;

                float distToCenter = distance(i.worldPos, center);
                float r01 = saturate(distToCenter / max(radius, 1e-5));

                float edge = smoothstep(_EdgeStart, _EdgeStart + _EdgeWidth, r01);

                float camDist = distance(_WorldSpaceCameraPos, i.worldPos);
                float depthFade = 1.0 - saturate((camDist - _DepthFadeStart) / max(_DepthFadeEnd - _DepthFadeStart, 1e-5));

                float g = gridLines(i.worldPos, _GridScale, _LineWidth, _LineSoft);

                fixed4 col = _BaseColor;
                col.a *= edge * _Intensity * depthFade;

                fixed4 gridCol = _GridColor;
                gridCol.a *= edge * _Intensity * depthFade;

                fixed4 outCol = lerp(col, gridCol, g);
                return outCol;
            }
            ENDCG
        }
    }
}
